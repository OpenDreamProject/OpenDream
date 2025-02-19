using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

[Virtual]
public class DreamObjectAtom : DreamObject {
    public readonly DreamOverlaysList Overlays;
    public readonly DreamOverlaysList Underlays;
    public readonly DreamVisContentsList VisContents;
    public readonly DreamParticlesList Particles;
    public readonly DreamFilterList Filters;
    public DreamList? VisLocs; // TODO: Implement

    public DreamObjectAtom(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        Overlays = new(ObjectTree.List.ObjectDefinition, this, AppearanceSystem, false);
        Underlays = new(ObjectTree.List.ObjectDefinition, this, AppearanceSystem, true);
        VisContents = new(ObjectTree.List.ObjectDefinition, PvsOverrideSystem, this);
        Particles = new(ObjectTree.List.ObjectDefinition, PvsOverrideSystem, this);
        Filters = new(ObjectTree.List.ObjectDefinition, this);

        AtomManager.AddAtom(this);
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

    public string GetRTEntityDesc() {
        if (AtomManager.TryGetAppearance(this, out var appearance) && appearance.Desc != null)
            return appearance.Desc;

        return ObjectDefinition.Type;
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
            case "appearance":
                var appearanceCopy = AtomManager.MustGetAppearance(this).ToMutable();

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
            case "particles":
                value = new(Particles);
                return true;

            default:
                if (AtomManager.IsValidAppearanceVar(varName)) {
                    var appearance = AtomManager.MustGetAppearance(this);

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
            case "appearance":
                if (!AtomManager.TryCreateAppearanceFrom(value, out var newAppearance))
                    return; // Ignore attempts to set an invalid appearance

                // The dir does not get changed
                newAppearance.Direction = AtomManager.MustGetAppearance(this).Direction;

                AtomManager.SetAtomAppearance(this, newAppearance);
                newAppearance.Dispose();
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
            case "particles": {
                Particles.Cut();

                if (value.TryGetValueAsDreamList(out var valueList)) {
                    // TODO: This should postpone UpdateAppearance until after everything is added
                    foreach (DreamValue particlesValue in valueList.GetValues()) {
                        Particles.AddValue(particlesValue);
                    }
                } else if (!value.IsNull) {
                    Particles.AddValue(value);
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
                    using var appearance = AtomManager.MustGetAppearance(this).ToMutable();
                    AtomManager.SetAppearanceVar(appearance, varName, value);
                    AtomManager.SetAtomAppearance(this, appearance);
                    break;
                }

                base.SetVar(varName, value);
                break;
        }
    }
}
