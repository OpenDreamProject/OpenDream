using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
using Robust.Shared.Map;

namespace OpenDreamRuntime.Objects.Types;

[Virtual]
public class DreamObjectMovable : DreamObjectAtom {
    public EntityUid Entity;
    public readonly DMISpriteComponent SpriteComponent;
    public DreamObjectAtom? Loc;

    // TODO: Cache this shit. GetWorldPosition is slow.
    public Vector2i Position => (Vector2i?)TransformSystem?.GetWorldPosition(_transformComponent) ?? (0, 0);
    public int X => Position.X;
    public int Y => Position.Y;
    public int Z => (int)_transformComponent.MapID;

    private readonly TransformComponent _transformComponent;


    private string? ScreenLoc {
        get => _screenLoc;
        set => SetScreenLoc(value);
    }

    private string? _screenLoc;

    public DreamObjectMovable(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        Entity = AtomManager.CreateMovableEntity(this);
        SpriteComponent = EntityManager.GetComponent<DMISpriteComponent>(Entity);
        _transformComponent = EntityManager.GetComponent<TransformComponent>(Entity);
    }

    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);

        ObjectDefinition.Variables["screen_loc"].TryGetValueAsString(out var screenLoc);
        ScreenLoc = screenLoc;

        if (EntityManager.TryGetComponent(Entity, out MetaDataComponent? metaData)) {
            MetaDataSystem?.SetEntityName(Entity, GetDisplayName(), metaData);
            MetaDataSystem?.SetEntityDescription(Entity, Desc ?? string.Empty, metaData);
        }

        args.GetArgument(0).TryGetValueAsDreamObject<DreamObjectAtom>(out var loc);
        SetLoc(loc); //loc is set before /New() is ever called
    }

    protected override void HandleDeletion(bool possiblyThreaded) {
        // SAFETY: Deleting entities is not threadsafe.
        if (possiblyThreaded) {
            EnterIntoDelQueue();
            return;
        }

        WalkManager.StopWalks(this);
        AtomManager.DeleteMovableEntity(this);

        base.HandleDeletion(possiblyThreaded);
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
                value = new(Loc);
                return true;
            case "screen_loc":
                value = (ScreenLoc != null) ? new(ScreenLoc) : DreamValue.Null;
                return true;
            case "contents":
                DreamList contents = ObjectTree.CreateList();

                using (var childEnumerator = _transformComponent.ChildEnumerator) {
                    while (childEnumerator.MoveNext(out EntityUid child)) {
                        if (!AtomManager.TryGetMovableFromEntity(child, out var childAtom))
                            continue;

                        contents.AddValue(new DreamValue(childAtom));
                    }
                }

                value = new(contents);
                return true;
            case "locs":
                // Unimplemented; just return a list containing src.loc
                DreamList locs = ObjectTree.CreateList();
                locs.AddValue(new(Loc));

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
                if (!value.TryGetValueAsDreamObject<DreamObjectAtom>(out var newLoc) && !value.IsNull)
                    throw new Exception($"Invalid loc {value}");

                SetLoc(newLoc);
                break;
            }
            case "name":
            case "desc": {
                base.SetVar(varName, value); // Let DreamObjectAtom do its own name/desc handling

                if (varName == "name") {
                    MetaDataSystem?.SetEntityName(Entity, GetDisplayName());
                } else {
                    value.TryGetValueAsString(out string? valueStr);

                    MetaDataSystem?.SetEntityDescription(Entity, valueStr ?? string.Empty);
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
        Loc = loc;
        if (TransformSystem == null)
            return;

        if (DreamMapManager.TryGetCellAt(Position, Z, out var oldMapCell))
            oldMapCell.Movables.Remove(this);

        if (loc is DreamObjectArea area) { // Puts the atom on the area's first turf
            loc = null; // Nullspace if we can't find a turf

            // We don't actually keep track of area turfs currently
            // So do the classic BYOND trick of looping through every turf and checking its area :)
            // TODO: Remove this monstrosity
            for (int z = 1; z <= DreamMapManager.Levels; z++) {
                for (int x = 1; x <= DreamMapManager.Size.X; x++) {
                    for (int y = 1; y <= DreamMapManager.Size.Y; y++) {
                        if (!DreamMapManager.TryGetCellAt((x, y), z, out var cell))
                            continue;

                        if (cell.Area == area) {
                            loc = cell.Turf;
                            break;
                        }
                    }
                }
            }
        }

        switch (loc) {
            case DreamObjectTurf turf:
                TransformSystem.SetParent(Entity, DreamMapManager.GetZLevelEntity(turf.Z));
                TransformSystem.SetWorldPosition(Entity, new Vector2(turf.X, turf.Y));

                turf.Cell.Movables.Add(this);
                break;
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

    private void SetScreenLoc(string? screenLoc) {
        _screenLoc = screenLoc;
        AtomManager.SetMovableScreenLoc(this, !string.IsNullOrEmpty(screenLoc) ? new ScreenLocation(screenLoc) : new ScreenLocation(0, 0, 0, 0));
    }
}
