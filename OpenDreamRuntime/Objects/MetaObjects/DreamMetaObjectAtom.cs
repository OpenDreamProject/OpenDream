using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
using Robust.Shared.Utility;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectAtom : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        public static readonly Dictionary<DreamObject, VerbsList> VerbLists = new();

        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;
        [Dependency] private readonly IAtomManager _atomManager = default!;
        private readonly ServerAppearanceSystem? _appearanceSystem;

        private readonly Dictionary<DreamObject, DreamFilterList> _filterLists = new();
        private readonly Dictionary<DreamObject, DreamOverlaysList> _overlayLists = new();
        private readonly Dictionary<DreamObject, DreamOverlaysList> _underlayLists = new();

        public DreamMetaObjectAtom() {
            IoCManager.InjectDependencies(this);

            _entitySystemManager.TryGetEntitySystem(out _appearanceSystem);
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            VerbLists[dreamObject] = new VerbsList(_objectTree, dreamObject);
            _filterLists[dreamObject] = new DreamFilterList(_objectTree.List.ObjectDefinition, dreamObject);
            _overlayLists[dreamObject] = new DreamOverlaysList(_objectTree.List.ObjectDefinition, dreamObject, _appearanceSystem, false);
            _underlayLists[dreamObject] = new DreamOverlaysList(_objectTree.List.ObjectDefinition, dreamObject, _appearanceSystem, true);

            // TODO: These should use their own special list types
            dreamObject.SetVariableValue("vis_locs", new(_objectTree.CreateList()));
            dreamObject.SetVariableValue("vis_contents", new(_objectTree.CreateList()));

            ParentType?.OnObjectCreated(dreamObject, creationArguments);
        }

        public void OnObjectDeleted(DreamObject dreamObject) {
            VerbLists.Remove(dreamObject);
            _filterLists.Remove(dreamObject);
            _overlayLists.Remove(dreamObject);
            _underlayLists.Remove(dreamObject);

            _atomManager.DeleteMovableEntity(dreamObject);

            ParentType?.OnObjectDeleted(dreamObject);
        }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

            switch (varName) {
                case "transform":  {
                    _atomManager.UpdateAppearance(dreamObject, appearance => {
                        float[] matrixArray = value.TryGetValueAsDreamObjectOfType(_objectTree.Matrix, out var matrix)
                            ? DreamMetaObjectMatrix.MatrixToTransformFloatArray(matrix)
                            : DreamMetaObjectMatrix.IdentityMatrixArray;

                        appearance.Transform = matrixArray;
                    });
                    break;
                }
                case "overlays": {
                    DreamOverlaysList overlaysList = _overlayLists[dreamObject];

                    overlaysList.Cut();

                    if (value.TryGetValueAsDreamList(out var valueList)) {
                        // TODO: This should postpone UpdateAppearance until after everything is added
                        foreach (DreamValue overlayValue in valueList.GetValues()) {
                            overlaysList.AddValue(overlayValue);
                        }
                    } else if (value != DreamValue.Null) {
                        overlaysList.AddValue(value);
                    }

                    break;
                }
                case "underlays": {
                    DreamOverlaysList underlaysList = _underlayLists[dreamObject];

                    underlaysList.Cut();

                    if (value.TryGetValueAsDreamList(out var valueList)) {
                        // TODO: This should postpone UpdateAppearance until after everything is added
                        foreach (DreamValue underlayValue in valueList.GetValues()) {
                            underlaysList.AddValue(underlayValue);
                        }
                    } else if (value != DreamValue.Null) {
                        underlaysList.AddValue(value);
                    }

                    break;
                }
                case "verbs": {
                    VerbsList verbsList = VerbLists[dreamObject];

                    verbsList.Cut();

                    if (value.TryGetValueAsDreamList(out var valueList)) {
                        foreach (DreamValue verbValue in valueList.GetValues()) {
                            verbsList.AddValue(verbValue);
                        }
                    } else if (value != DreamValue.Null) {
                        verbsList.AddValue(value);
                    }

                    break;
                }
                case "filters": {
                    DreamFilterList filterList = _filterLists[dreamObject];

                    filterList.Cut();

                    if (value.TryGetValueAsDreamList(out var valueList)) {
                        // TODO: This should postpone UpdateAppearance until after everything is added
                        foreach (DreamValue filterValue in valueList.GetValues()) {
                            filterList.AddValue(filterValue);
                        }
                    } else if (value != DreamValue.Null) {
                        filterList.AddValue(value);
                    }

                    break;
                }
                default:
                    if (_atomManager.IsValidAppearanceVar(varName)) {
                        _atomManager.UpdateAppearance(dreamObject, appearance => {
                            _atomManager.SetAppearanceVar(appearance, varName, value);
                        });
                    }

                    break;
            }
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) {
            switch (varName) {
                case "appearance": {
                    var appearance = _atomManager.MustGetAppearance(dreamObject);
                    if (appearance == null) // Shouldn't be possible?
                        return DreamValue.Null;

                    var copy = new IconAppearance(appearance); // Return a copy of our appearance
                    return new(copy);
                }
                case "transform":
                    // Clone the matrix
                    DreamObject matrix = _objectTree.CreateObject(_objectTree.Matrix);
                    matrix.InitSpawn(new DreamProcArguments(new() { value }));

                    return new DreamValue(matrix);
                case "verbs":
                    return new DreamValue(VerbLists[dreamObject]);
                case "overlays":
                    return new DreamValue(_overlayLists[dreamObject]);
                case "underlays":
                    return new DreamValue(_underlayLists[dreamObject]);
                case "filters":
                    return new DreamValue(_filterLists[dreamObject]);
                default:
                    if (_atomManager.IsValidAppearanceVar(varName)) {
                        var appearance = _atomManager.MustGetAppearance(dreamObject);

                        return _atomManager.GetAppearanceVar(appearance, varName);
                    }

                    return ParentType?.OnVariableGet(dreamObject, varName, value) ?? value;
            }
        }
    }
}
