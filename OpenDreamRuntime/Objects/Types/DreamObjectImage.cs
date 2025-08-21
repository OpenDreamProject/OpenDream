using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
using Robust.Shared.Map;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectImage : DreamObject {
    public EntityUid Entity = EntityUid.Invalid;
    public readonly DMISpriteComponent? SpriteComponent;
    private DreamObject? _loc;
    private DreamList _overlays;
    private DreamList _underlays;
    private readonly DreamList _filters;
    public readonly bool IsMutableAppearance;
    public MutableAppearance? MutableAppearance;

    /// <summary>
    /// All the args in /image/New() after "icon" and "loc", in their correct order
    /// </summary>
    private static readonly string[] IconCreationArgs = {
        "icon_state",
        "layer",
        "dir",
        "pixel_x",
        "pixel_y"
    };

    public DreamObjectImage(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        if (objectDefinition.IsSubtypeOf(ObjectTree.MutableAppearance)) {
            // /mutable_appearance.overlays and /mutable_appearance.underlays are normal lists
            _overlays = ObjectTree.CreateList();
            _underlays = ObjectTree.CreateList();
            _filters = ObjectTree.CreateList();
            IsMutableAppearance = true;
        } else {
            _overlays = new DreamOverlaysList(ObjectTree.List.ObjectDefinition, this, AppearanceSystem, false);
            _underlays = new DreamOverlaysList(ObjectTree.List.ObjectDefinition, this, AppearanceSystem, true);
            _filters = new DreamFilterList(ObjectTree.List.ObjectDefinition, this);
            IsMutableAppearance = false;
            Entity = EntityManager.SpawnEntity(null, new MapCoordinates(0, 0, MapId.Nullspace)); //spawning an entity in nullspace means it never actually gets sent to any clients until it's placed on the map, or it gets a PVS override
            SpriteComponent = EntityManager.AddComponent<DMISpriteComponent>(Entity);
        }

        AtomManager.SetAtomAppearance(this, AtomManager.GetAppearanceFromDefinition(ObjectDefinition));
    }

    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);

        DreamValue icon = args.GetArgument(0);
        if (icon.IsNull || !AtomManager.TryCreateAppearanceFrom(icon, out var mutableAppearance)) {
            // Use a default appearance, but log a warning about it if icon wasn't null
            mutableAppearance = IsMutableAppearance ? MutableAppearance! : AtomManager.MustGetAppearance(this).ToMutable(); //object def appearance is created in the constructor
            if (!icon.IsNull)
                Logger.GetSawmill("opendream.image")
                    .Warning($"Attempted to create an /image from {icon}. This is invalid and a default image was created instead.");
        }

        int argIndex = 1;
        DreamValue loc = args.GetArgument(1);
        if (loc.TryGetValueAsDreamObject(out _loc)) { // If it's not a DreamObject, it's actually icon_state and not loc
            argIndex = 2;
        }

        foreach (string argName in IconCreationArgs) {
            var arg = args.GetArgument(argIndex++);
            if (arg.IsNull)
                continue;

            AtomManager.SetAppearanceVar(mutableAppearance, argName, arg);
            if (argName == "dir" && arg.TryGetValueAsInteger(out var argDir) && argDir > 0) {
                // If a dir is explicitly given in the constructor then overlays using this won't use their owner's dir
                // Setting dir after construction does not affect this
                // This is undocumented and I hate it
                mutableAppearance.InheritsDirection = false;
            }
        }

        AtomManager.SetAtomAppearance(this, mutableAppearance);
        mutableAppearance.Dispose();
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        // TODO: filters, transform
        switch(varName) {
            case "loc": {
                value = new(_loc);
                return true;
            }
            case "overlays":
                value = new(_overlays);
                return true;
            case "underlays":
                value = new(_underlays);
                return true;
            case "filters":
                value = new(_filters);
                return true;
            default: {
                if (AtomManager.IsValidAppearanceVar(varName)) {
                    value = IsMutableAppearance ? AtomManager.GetAppearanceVar(MutableAppearance!, varName) : AtomManager.GetAppearanceVar(AtomManager.MustGetAppearance(this), varName);
                    return true;
                } else {
                    return base.TryGetVar(varName, out value);
                }
            }
        }
    }

    protected override void SetVar(string varName, DreamValue value) {
        switch (varName) {
            case "appearance": // Appearance var is mutable, don't use AtomManager.SetAppearanceVar()
                if (!AtomManager.TryCreateAppearanceFrom(value, out var newAppearance))
                    return; // Ignore attempts to set an invalid appearance

                // The dir does not get changed
                var originalAppearance = AtomManager.MustGetAppearance(this);
                newAppearance.Direction = originalAppearance.Direction;
                AtomManager.SetAtomAppearance(this, newAppearance);
                newAppearance.Dispose();
                break;
            case "loc":
                value.TryGetValueAsDreamObject(out _loc);
                break;
            case "overlays": {
                value.TryGetValueAsDreamList(out var valueList);

                // /mutable_appearance has some special behavior for its overlays and underlays vars
                // They're normal lists, not the special DreamOverlaysList.
                // Setting them to a list will create a copy of that list.
                // Otherwise it attempts to create an appearance and creates a new (normal) list with that appearance
                if (ObjectDefinition.IsSubtypeOf(ObjectTree.MutableAppearance)) {
                    if (valueList != null) {
                        _overlays = valueList.CreateCopy();
                    } else {
                        var overlay = DreamOverlaysList.CreateOverlayAppearance(AtomManager, value, AtomManager.MustGetAppearance(this).Icon);
                        if (overlay == null)
                            return;

                        _overlays.Cut();
                        _overlays.AddValue(new(overlay));
                        overlay.Dispose();
                    }

                    return;
                }

                _overlays.Cut();

                if (valueList != null) {
                    // TODO: This should postpone UpdateAppearance until after everything is added
                    foreach (DreamValue overlayValue in valueList.EnumerateValues()) {
                        _overlays.AddValue(overlayValue);
                    }
                } else if (!value.IsNull) {
                    _overlays.AddValue(value);
                }

                break;
            }
            case "underlays": {
                value.TryGetValueAsDreamList(out var valueList);

                // See the comment in the overlays setter for info on this
                if (ObjectDefinition.IsSubtypeOf(ObjectTree.MutableAppearance)) {
                    if (valueList != null) {
                        _underlays = valueList.CreateCopy();
                    } else {
                        var underlay = DreamOverlaysList.CreateOverlayAppearance(AtomManager, value, AtomManager.MustGetAppearance(this).Icon);
                        if (underlay == null)
                            return;

                        _underlays.Cut();
                        _underlays.AddValue(new(underlay));
                        underlay.Dispose();
                    }

                    return;
                }

                _underlays.Cut();

                if (valueList != null) {
                    // TODO: This should postpone UpdateAppearance until after everything is added
                    foreach (DreamValue underlayValue in valueList.EnumerateValues()) {
                        _underlays.AddValue(underlayValue);
                    }
                } else if (!value.IsNull) {
                    _underlays.AddValue(value);
                }

                break;
            }
            case "filters": {
                value.TryGetValueAsDreamList(out var valueList);

                _filters.Cut();

                if (valueList != null) { // filters = list("type"=...)
                    var filterObject = DreamObjectFilter.TryCreateFilter(ObjectTree, valueList);
                    if (filterObject == null) // list() with invalid "type" is ignored
                        break;

                    _filters.AddValue(new(filterObject));
                } else if (!value.IsNull) {
                    _filters.AddValue(value);
                }

                break;
            }
            case "override": {
                using var mutableAppearance = IsMutableAppearance ? MutableAppearance! : AtomManager.MustGetAppearance(this).ToMutable();
                mutableAppearance.Override = value.IsTruthy();
                AtomManager.SetAtomAppearance(this, mutableAppearance);
                break;
            }
            default:
                if (AtomManager.IsValidAppearanceVar(varName)) {
                    using var mutableAppearance = IsMutableAppearance ? MutableAppearance! : AtomManager.MustGetAppearance(this).ToMutable();
                    AtomManager.SetAppearanceVar(mutableAppearance, varName, value);
                    AtomManager.SetAtomAppearance(this, mutableAppearance);
                    break;
                }

                base.SetVar(varName, value);
                break;
        }
    }

    public DreamObject? GetAttachedLoc(){
        return this._loc;
    }

    protected override void HandleDeletion(bool possiblyThreaded) {
        // SAFETY: Deleting entities is not threadsafe.
        if (possiblyThreaded) {
            EnterIntoDelQueue();
            return;
        }

        if(Entity != EntityUid.Invalid) {
            EntityManager.DeleteEntity(Entity);
        }

        MutableAppearance?.Dispose();
        base.HandleDeletion(possiblyThreaded);
    }
}
