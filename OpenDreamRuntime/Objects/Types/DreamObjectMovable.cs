using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace OpenDreamRuntime.Objects.Types;

[Virtual]
public class DreamObjectMovable : DreamObjectAtom {
    public EntityUid Entity;
    public readonly DMISpriteComponent SpriteComponent;

    public Vector2i Position => (Vector2i?)TransformSystem?.GetWorldPosition(_transformComponent) ?? (0, 0);
    public int X => Position.X;
    public int Y => Position.Y;
    public int Z => (int)_transformComponent.MapID;

    private readonly TransformComponent _transformComponent;
    private readonly MetaDataComponent _metaDataComponent;

    private DreamObjectAtom? _loc;

    private string? ScreenLoc {
        get => _screenLoc;
        set {
            _screenLoc = value;
            if (!EntityManager.TryGetComponent<DMISpriteComponent>(Entity, out var sprite))
                return;

            sprite.ScreenLocation = !string.IsNullOrEmpty(value) ?
                                        new ScreenLocation(value) :
                                        new ScreenLocation(0, 0, 0, 0);
        }
    }

    private string? _screenLoc;

    public DreamObjectMovable(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        Entity = AtomManager.CreateMovableEntity(this);
        SpriteComponent = EntityManager.GetComponent<DMISpriteComponent>(Entity);
        _transformComponent = EntityManager.GetComponent<TransformComponent>(Entity);
        _metaDataComponent = EntityManager.GetComponent<MetaDataComponent>(Entity);

        objectDefinition.Variables["screen_loc"].TryGetValueAsString(out var screenLoc);
        ScreenLoc = screenLoc;

        if (IsSubtypeOf(ObjectTree.Obj))
            AtomManager.Objects.Add(this);
        else if (IsSubtypeOf(ObjectTree.Mob))
            AtomManager.Mobs.Add((DreamObjectMob)this);
        else
            AtomManager.Movables.Add(this);
    }

    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);

        args.GetArgument(0).TryGetValueAsDreamObject<DreamObjectAtom>(out var loc);
        SetLoc(loc); //loc is set before /New() is ever called
    }

    protected override void HandleDeletion() {
        if (IsSubtypeOf(ObjectTree.Obj))
            AtomManager.Objects.RemoveSwap(AtomManager.Objects.IndexOf(this));
        else if (IsSubtypeOf(ObjectTree.Mob))
            AtomManager.Mobs.RemoveSwap(AtomManager.Mobs.IndexOf((DreamObjectMob)this));
        else
            AtomManager.Movables.RemoveSwap(AtomManager.Movables.IndexOf(this));

        AtomManager.DeleteMovableEntity(this);
        base.HandleDeletion();
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        switch (varName) {
            case "x":
                value = new(X);
                return true;
            case "y":
                value = new(Y);
                return true;
            case "z":
                value = new(Z);
                return true;
            case "loc":
                value = new(_loc);
                return true;
            case "screen_loc":
                value = (ScreenLoc != null) ? new(ScreenLoc) : DreamValue.Null;
                return true;
            case "contents":
                DreamList contents = ObjectTree.CreateList();

                using (var childEnumerator = _transformComponent.ChildEnumerator) {
                    while (childEnumerator.MoveNext(out EntityUid? child)) {
                        if (!AtomManager.TryGetMovableFromEntity(child.Value, out var childAtom))
                            continue;

                        contents.AddValue(new DreamValue(childAtom));
                    }
                }

                value = new(contents);
                return true;
            case "locs":
                // Unimplemented; just return a list containing src.loc
                DreamList locs = ObjectTree.CreateList();
                locs.AddValue(new(_loc));

                value = new DreamValue(locs);
                return true;
            default:
                return base.TryGetVar(varName, out value);
        }
    }

    protected override void SetVar(string varName, DreamValue value) {
        switch (varName) {
            case "x":
            case "y":
            case "z": {
                int x = (varName == "x") ? value.MustGetValueAsInteger() : X;
                int y = (varName == "y") ? value.MustGetValueAsInteger() : Y;
                int z = (varName == "z") ? value.MustGetValueAsInteger() : Z;

                DreamMapManager.TryGetTurfAt((x, y), z, out var newLoc);
                SetLoc(newLoc);
                break;
            }
            case "loc": {
                if (!value.TryGetValueAsDreamObject<DreamObjectAtom>(out var newLoc) && value != DreamValue.Null)
                    throw new Exception($"Invalid loc {value}");

                SetLoc(newLoc);
                break;
            }
            case "name":
            case "desc": {
                base.SetVar(varName, value); // Let DreamObjectAtom do its own name/desc handling

                if (varName == "name") {
                    _metaDataComponent.EntityName = GetDisplayName();
                } else {
                    value.TryGetValueAsString(out string? valueStr);

                    _metaDataComponent.EntityDescription = valueStr ?? string.Empty;
                }

                break;
            }
            case "screen_loc":
                value.TryGetValueAsString(out var screenLoc);

                ScreenLoc = screenLoc;
                break;
            default:
                base.SetVar(varName, value);
                break;
        }
    }

    private void SetLoc(DreamObjectAtom? loc) {
        _loc = loc;
        if (TransformSystem == null)
            return;

        if (DreamMapManager.TryGetCellAt(Position, Z, out var oldMapCell))
            oldMapCell.Movables.Remove(this);

        switch (loc) {
            case DreamObjectTurf turf: {
                TransformSystem.SetParent(Entity, DreamMapManager.GetZLevelEntity(turf.Z));
                TransformSystem.SetWorldPosition(Entity, (turf.X, turf.Y));

                turf.Cell.Movables.Add(this);
                break;
            }
            case DreamObjectMovable movable:
                TransformSystem.SetParent(Entity, movable.Entity);
                TransformSystem.SetLocalPosition(Entity, Vector2.Zero);
                break;
            case null:
                TransformSystem.SetParent(Entity, MapManager.GetMapEntityId(MapId.Nullspace));
                break;
            default:
                throw new ArgumentException($"Invalid loc {loc}");
        }
    }
}
