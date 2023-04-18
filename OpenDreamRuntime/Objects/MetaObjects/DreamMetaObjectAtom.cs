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
        [Dependency] private readonly IDreamMapManager _mapManager = default!;
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;
        [Dependency] private readonly IAtomManager _atomManager = default!;
        private ServerAppearanceSystem? _appearanceSystem;

        private readonly Dictionary<DreamObject, DreamFilterList> _filterLists = new();

        public DreamMetaObjectAtom() {
            IoCManager.InjectDependencies(this);
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            VerbLists[dreamObject] = new VerbsList(_objectTree, dreamObject);
            _filterLists[dreamObject] = new DreamFilterList(_objectTree.List.ObjectDefinition, dreamObject);

            // TODO: These should use their own special list types
            dreamObject.SetVariable("overlays", new(_objectTree.CreateList()));
            dreamObject.SetVariable("underlays", new(_objectTree.CreateList()));
            dreamObject.SetVariableValue("vis_locs", new(_objectTree.CreateList()));
            dreamObject.SetVariableValue("vis_contents", new(_objectTree.CreateList()));

            ParentType?.OnObjectCreated(dreamObject, creationArguments);
        }

        public void OnObjectDeleted(DreamObject dreamObject) {
            VerbLists.Remove(dreamObject);
            _filterLists.Remove(dreamObject);

            _atomManager.DeleteMovableEntity(dreamObject);

            _atomManager.OverlaysListToAtom.Remove(dreamObject.GetVariable("overlays").GetValueAsDreamList());
            _atomManager.UnderlaysListToAtom.Remove(dreamObject.GetVariable("underlays").GetValueAsDreamList());
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
                    if (oldValue.TryGetValueAsDreamList(out var oldList)) {
                        oldList.Cut();
                        oldList.ValueAssigned -= OverlayValueAssigned;
                        oldList.BeforeValueRemoved -= OverlayBeforeValueRemoved;
                        _atomManager.OverlaysListToAtom.Remove(oldList);
                    }

                    if (!value.TryGetValueAsDreamList(out var overlayList)) {
                        overlayList = _objectTree.CreateList();
                    }

                    overlayList.ValueAssigned += OverlayValueAssigned;
                    overlayList.BeforeValueRemoved += OverlayBeforeValueRemoved;
                    _atomManager.OverlaysListToAtom[overlayList] = dreamObject;
                    dreamObject.SetVariableValue(varName, new DreamValue(overlayList));
                    break;
                }
                case "underlays": {
                    if (oldValue.TryGetValueAsDreamList(out var oldList)) {
                        oldList.Cut();
                        oldList.ValueAssigned -= UnderlayValueAssigned;
                        oldList.BeforeValueRemoved -= UnderlayBeforeValueRemoved;
                        _atomManager.UnderlaysListToAtom.Remove(oldList);
                    }

                    if (!value.TryGetValueAsDreamList(out var underlayList)) {
                        underlayList = _objectTree.CreateList();
                    }

                    underlayList.ValueAssigned += UnderlayValueAssigned;
                    underlayList.BeforeValueRemoved += UnderlayBeforeValueRemoved;
                    _atomManager.UnderlaysListToAtom[underlayList] = dreamObject;
                    dreamObject.SetVariableValue(varName, new DreamValue(underlayList));
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
                        // TODO: This should maybe postpone UpdateAppearance until after everything is added
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

        private IconAppearance CreateOverlayAppearance(DreamObject atom, DreamValue value) {
            IconAppearance overlay;

            if (value.TryGetValueAsString(out var iconState)) {
                overlay = new IconAppearance() {
                    IconState = iconState
                };
            } else if (_atomManager.TryCreateAppearanceFrom(value, out var overlayAppearance)) {
                overlay = overlayAppearance;
            } else {
                return new IconAppearance(); // Not a valid overlay, use a default appearance
            }

            if (overlay.Icon == null) {
                overlay.Icon = _atomManager.MustGetAppearance(atom)?.Icon;
            }

            return overlay;
        }

        private void OverlayValueAssigned(DreamList overlayList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;
            if (_appearanceSystem == null && !_entitySystemManager.TryGetEntitySystem(out _appearanceSystem)) return;

            DreamObject atom = _atomManager.OverlaysListToAtom[overlayList];

            _atomManager.UpdateAppearance(atom, appearance => {
                IconAppearance overlay = CreateOverlayAppearance(atom, value);
                uint id = _appearanceSystem.AddAppearance(overlay);

                appearance.Overlays.Add(id);
            });
        }

        private void OverlayBeforeValueRemoved(DreamList overlayList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;
            if (_appearanceSystem == null && !_entitySystemManager.TryGetEntitySystem(out _appearanceSystem)) return;

            DreamObject atom = _atomManager.OverlaysListToAtom[overlayList];
            IconAppearance overlayAppearance = CreateOverlayAppearance(atom, value);
            uint? overlayAppearanceId = _appearanceSystem.GetAppearanceId(overlayAppearance);
            if (overlayAppearanceId == null) return;

            _atomManager.UpdateAppearance(atom, appearance => {
                appearance.Overlays.Remove(overlayAppearanceId.Value);
            });
        }

        private void UnderlayValueAssigned(DreamList underList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;
            if (_appearanceSystem == null && !_entitySystemManager.TryGetEntitySystem(out _appearanceSystem)) return;

            DreamObject atom = _atomManager.UnderlaysListToAtom[underList];

            _atomManager.UpdateAppearance(atom, appearance => {
                IconAppearance underlay = CreateOverlayAppearance(atom, value);
                uint id = _appearanceSystem.AddAppearance(underlay);

                appearance.Underlays.Add(id);
            });
        }

        private void UnderlayBeforeValueRemoved(DreamList underlayList, DreamValue key, DreamValue value) {
            if (value == DreamValue.Null) return;
            if (_appearanceSystem == null && !_entitySystemManager.TryGetEntitySystem(out _appearanceSystem)) return;

            DreamObject atom = _atomManager.UnderlaysListToAtom[underlayList];
            IconAppearance underlayAppearance = CreateOverlayAppearance(atom, value);
            uint? underlayAppearanceId = _appearanceSystem.GetAppearanceId(underlayAppearance);
            if (underlayAppearanceId == null) return;

            _atomManager.UpdateAppearance(atom, appearance => {
                appearance.Underlays.Remove(underlayAppearanceId.Value);
            });
        }
    }
}
