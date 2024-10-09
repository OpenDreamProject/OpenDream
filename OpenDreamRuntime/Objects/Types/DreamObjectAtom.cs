using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.Types;

[Virtual]
public class DreamObjectAtom : DreamObject {
    public string? Name;
    public string? Desc;
    public readonly DreamOverlaysList Overlays;
    public readonly DreamOverlaysList Underlays;
    public readonly DreamVisContentsList VisContents;
    public readonly DreamFilterList Filters;
    public DreamList? VisLocs; // TODO: Implement

    public DreamObjectAtom(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        Overlays = new(ObjectTree.List.ObjectDefinition, this, AppearanceSystem, false);
        Underlays = new(ObjectTree.List.ObjectDefinition, this, AppearanceSystem, true);
        VisContents = new(ObjectTree.List.ObjectDefinition, PvsOverrideSystem, this);
        Filters = new(ObjectTree.List.ObjectDefinition, this);

        AtomManager.AddAtom(this);
    }

    public override void Initialize(DreamProcArguments args) {
        ObjectDefinition.Variables["name"].TryGetValueAsString(out Name);
        ObjectDefinition.Variables["desc"].TryGetValueAsString(out Desc);
    }

    protected override void HandleDeletion(bool possiblyThreaded) {
        // SAFETY: RemoveAtom is not threadsafe.
        if (possiblyThreaded) {
            EnterIntoDelQueue();
            return;
        }

        AtomManager.RemoveAtom(this);

        base.HandleDeletion(possiblyThreaded);
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
                var appearanceCopy = AtomManager.MustGetAppearance(this)!.ToMutable();

                value = new(appearanceCopy);
                return true;
            case "overlays":
                value = new(Overlays);
                return true;
            case "underlays":
                value = new(Underlays);
                return true;
            case "verbs":
                value = new(new VerbsList(ObjectTree, AtomManager, VerbSystem, this));
                return true;
            case "filters":
                value = new(Filters);
                return true;
            case "vis_locs":
                VisLocs ??= ObjectTree.CreateList();
                value = new(VisLocs);
                return true;
            case "vis_contents":
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
            case "appearance":
                if (!AtomManager.TryCreateAppearanceFrom(value, out var newAppearance))
                    return; // Ignore attempts to set an invalid appearance

                // The dir does not get changed
                newAppearance.Direction = AtomManager.MustGetAppearance(this)!.Direction;

                AtomManager.SetAtomAppearance(this, newAppearance);
                break;
            case "overlays": {
                Overlays.Cut();

                if (value.TryGetValueAsDreamList(out var valueList)) {
                    // TODO: This should postpone UpdateAppearance until after everything is added
                    foreach (DreamValue overlayValue in valueList.GetValues()) {
                        Overlays.AddValue(overlayValue);
                    }
                } else if (!value.IsNull) {
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
                } else if (!value.IsNull) {
                    Underlays.AddValue(value);
                }

                break;
            }
            case "vis_contents": {
                VisContents.Cut();

                if (value.TryGetValueAsDreamList(out var valueList)) {
                    // TODO: This should postpone UpdateAppearance until after everything is added
                    foreach (DreamValue visContentsValue in valueList.GetValues()) {
                        VisContents.AddValue(visContentsValue);
                    }
                } else if (!value.IsNull) {
                    VisContents.AddValue(value);
                }

                break;
            }
            case "filters": {
                Filters.Cut();

                if (value.TryGetValueAsDreamList(out var valueList)) { // filters = list("type"=...)
                    var filterObject = DreamObjectFilter.TryCreateFilter(ObjectTree, valueList);
                    if (filterObject == null) // list() with invalid "type" is ignored
                        break;

                    Filters.AddValue(new(filterObject));
                } else if (!value.IsNull) {
                    Filters.AddValue(value);
                }

                break;
            }
            default:
                if (AtomManager.IsValidAppearanceVar(varName)) {
                    // Basically AtomManager.UpdateAppearance() but without the performance impact of using actions
                    var immutableAppearance = AtomManager.MustGetAppearance(this);

                    // Clone the appearance
                    // TODO: We can probably avoid cloning while the DMISpriteComponent is dirty
                    IconAppearance appearance = (immutableAppearance  != null) ? immutableAppearance.ToMutable() : new();

                    AtomManager.SetAppearanceVar(appearance, varName, value);
                    AtomManager.SetAtomAppearance(this, appearance);
                    break;
                }

                base.SetVar(varName, value);
                break;
        }
    }
}
