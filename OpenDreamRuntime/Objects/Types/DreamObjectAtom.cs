namespace OpenDreamRuntime.Objects.Types;

[Virtual]
public class DreamObjectAtom(DreamObjectDefinition objectDefinition) : DreamObject(objectDefinition) {
    private DreamOverlaysList Overlays => _overlays ??= new(ObjectTree.List.ObjectDefinition, this, AppearanceSystem, false);
    private DreamOverlaysList Underlays => _underlays ??= new(ObjectTree.List.ObjectDefinition, this, AppearanceSystem, true);
    private DreamVisContentsList VisContents => _visContents ??= new(ObjectTree.List.ObjectDefinition, PvsOverrideSystem, this);
    private DreamFilterList Filters => _filters ??= new(ObjectTree.List.ObjectDefinition, this);
    private DreamList VisLocs => _visLocs ??= ObjectTree.CreateList();

    private DreamOverlaysList? _overlays;
    private DreamOverlaysList? _underlays;
    private DreamVisContentsList? _visContents;
    private DreamFilterList? _filters;
    private DreamList? _visLocs; // TODO: Implement

    protected string GetRTEntityDesc() {
        if (AtomManager.TryGetAppearance(this, out var appearance) && appearance.Desc != null)
            return appearance.Desc;

        return ObjectDefinition.Type;
    }

    protected override void HandleDeletion() {
        _overlays?.DecRef();
        _underlays?.DecRef();
        _visContents?.DecRef();
        _filters?.DecRef();
        _visLocs?.DecRef();

        base.HandleDeletion();
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
                Overlays.IncRef();
                value = new(Overlays);
                return true;
            case "underlays":
                Underlays.IncRef();
                value = new(Underlays);
                return true;
            case "verbs":
                value = new(new VerbsList(ObjectTree, AtomManager, this));
                return true;
            case "filters":
                Filters.IncRef();
                value = new(Filters);
                return true;
            case "vis_locs":
                VisLocs.IncRef();
                value = new(VisLocs);
                return true;
            case "vis_contents":
                VisContents.IncRef();
                value = new(VisContents);
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
                    foreach (DreamValue overlayValue in valueList.EnumerateValues()) {
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
                    foreach (DreamValue underlayValue in valueList.EnumerateValues()) {
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
                    foreach (DreamValue visContentsValue in valueList.EnumerateValues()) {
                        VisContents.AddValue(visContentsValue);
                    }
                } else if (!value.IsNull) {
                    VisContents.AddValue(value);
                }

                break;
            }
            case "filters": {
                Filters.Cut();

                // filters = list("type"=...) or list(filter(...), filter(...))
                if (value.TryGetValueAsDreamList(out var valueList)) {
                    using var typeArg = valueList.GetValue(new("type"));

                    if (typeArg != DreamValue.Null) { // It's a single filter
                        var filterObject = DreamObjectFilter.TryCreateFilter(ObjectTree, valueList);
                        if (filterObject == null) // list() with invalid "type" is ignored
                            break;

                        Filters.AddValue(new(filterObject));
                        filterObject.DecRef();
                    } else { // It's a list of filters
                        foreach (var filter in valueList.EnumerateValues()) {
                            if (!filter.TryGetValueAsDreamObject<DreamObjectFilter>(out var filterObject)) {
                                if (!filter.TryGetValueAsDreamList(out var filterValues))
                                    continue;

                                filterObject = DreamObjectFilter.TryCreateFilter(ObjectTree, filterValues);
                                if (filterObject == null)
                                    continue;
                            }

                            Filters.AddValue(new(filterObject));
                            filterObject.DecRef();
                        }
                    }
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
