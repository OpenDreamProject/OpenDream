using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.Types;

[Virtual]
public class DreamObjectAtom : DreamObject {
    public string? Name;
    public string? Desc;
    public readonly DreamOverlaysList Overlays;
    public readonly DreamOverlaysList Underlays;
    public readonly VerbsList Verbs;
    public readonly DreamFilterList Filters;
    public DreamList? VisLocs; // TODO: Implement
    public DreamList? VisContents; // TODO: Implement

    public DreamObjectAtom(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        objectDefinition.Variables["name"].TryGetValueAsString(out Name);
        objectDefinition.Variables["desc"].TryGetValueAsString(out Desc);

        Overlays = new(ObjectTree.List.ObjectDefinition, this, AppearanceSystem, false);
        Underlays = new(ObjectTree.List.ObjectDefinition, this, AppearanceSystem, true);
        Verbs = new(ObjectTree, this);
        Filters = new(ObjectTree.List.ObjectDefinition, this);
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        switch (varName) {
            // x/y/z/loc should be overriden by subtypes
            case "x":
            case "y":
            case "z":
                value = new(0);
                return true;
            case "loc":
                value = DreamValue.Null;
                return true;

            case "name":
                value = (Name != null) ? new(Name) : DreamValue.Null;
                return true;
            case "desc":
                value = (Desc != null) ? new(Desc) : DreamValue.Null;
                return true;
            case "appearance":
                var appearanceCopy = new IconAppearance(AtomManager.MustGetAppearance(this)!);

                value = new(appearanceCopy);
                return true;
            case "transform":{
                var appearance = AtomManager.MustGetAppearance(this)!;

                var transform = appearance.Transform;
                var matrix = DreamObjectMatrix.MakeMatrix(ObjectTree,
                    transform[0], transform[2], transform[4],
                    transform[1], transform[3], transform[5]);

                value = new(matrix);
                return true;
            }
            case "overlays":
                value = new(Overlays);
                return true;
            case "underlays":
                value = new(Underlays);
                return true;
            case "verbs":
                value = new(Verbs);
                return true;
            case "filters":
                value = new(Filters);
                return true;
            case "vis_locs":
                VisLocs ??= ObjectTree.CreateList();
                value = new(VisLocs);
                return true;
            case "vis_contents":
                VisContents ??= ObjectTree.CreateList();
                value = new(VisContents);
                return true;

            default:
                if (AtomManager.IsValidAppearanceVar(varName)) {
                    var appearance = AtomManager.MustGetAppearance(this)!;

                    value = AtomManager.GetAppearanceVar(appearance, varName);
                    return true;
                }

                return base.TryGetVar(varName, out value);
        }
    }

    protected override void SetVar(string varName, DreamValue value) {
        switch (varName) {
            // x/y/z/loc should be overriden by subtypes
            case "x":
            case "y":
            case "z":
            case "loc":
                break;

            case "name":
                value.TryGetValueAsString(out Name);
                break;
            case "desc":
                value.TryGetValueAsString(out Desc);
                break;
            case "overlays": {
                Overlays.Cut();

                if (value.TryGetValueAsDreamList(out var valueList)) {
                    // TODO: This should postpone UpdateAppearance until after everything is added
                    foreach (DreamValue overlayValue in valueList.GetValues()) {
                        Overlays.AddValue(overlayValue);
                    }
                } else if (value != DreamValue.Null) {
                    Overlays.AddValue(value);
                }

                break;
            }
            case "underlays": {
                Underlays.Cut();

                if (value.TryGetValueAsDreamList(out var valueList)) {
                    // TODO: This should postpone UpdateAppearance until after everything is added
                    foreach (DreamValue underlayValue in valueList.GetValues()) {
                        Underlays.AddValue(underlayValue);
                    }
                } else if (value != DreamValue.Null) {
                    Underlays.AddValue(value);
                }

                break;
            }
            case "verbs": {
                Verbs.Cut();

                if (value.TryGetValueAsDreamList(out var valueList)) {
                    foreach (DreamValue verbValue in valueList.GetValues()) {
                        Verbs.AddValue(verbValue);
                    }
                } else if (value != DreamValue.Null) {
                    Verbs.AddValue(value);
                }

                break;
            }
            case "filters": {
                Filters.Cut();

                if (value.TryGetValueAsDreamList(out var valueList)) {
                    // TODO: This should postpone UpdateAppearance until after everything is added
                    foreach (DreamValue filterValue in valueList.GetValues()) {
                        Filters.AddValue(filterValue);
                    }
                } else if (value != DreamValue.Null) {
                    Filters.AddValue(value);
                }

                break;
            }
            default:
                if (AtomManager.IsValidAppearanceVar(varName) || varName == "transform") {
                    // Basically AtomManager.UpdateAppearance() but without the performance impact of using actions
                    var appearance = AtomManager.MustGetAppearance(this);

                    // Clone the appearance
                    // TODO: We can probably avoid cloning while the DMISpriteComponent is dirty
                    appearance = (appearance != null) ? new(appearance) : new();

                    if (varName == "transform") {
                        float[] matrixArray = value.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrix)
                            ? DreamObjectMatrix.MatrixToTransformFloatArray(matrix)
                            : DreamObjectMatrix.IdentityMatrixArray;

                        appearance.Transform = matrixArray;
                    } else {
                        AtomManager.SetAppearanceVar(appearance, varName, value);
                    }

                    AtomManager.SetAtomAppearance(this, appearance);
                    break;
                }

                base.SetVar(varName, value);
                break;
        }
    }
}
