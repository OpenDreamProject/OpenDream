using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace OpenDreamRuntime.Objects.MetaObjects {
    [Virtual]
    class DreamMetaObjectMovable : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;
        [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
        [Dependency] private readonly IAtomManager _atomManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        private readonly TransformSystem _transformSystem;

        public DreamMetaObjectMovable() {
            IoCManager.InjectDependencies(this);

            _transformSystem = _entitySystemManager.GetEntitySystem<TransformSystem>();
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            ParentType?.OnObjectCreated(dreamObject, creationArguments);

            DreamValue locArgument = creationArguments.GetArgument(0, "loc");
            if (locArgument.TryGetValueAsDreamObjectOfType(_objectTree.Atom, out _)) {
                dreamObject.SetVariable("loc", locArgument); //loc is set before /New() is ever called
            }

            DreamValue screenLocationValue = dreamObject.GetVariable("screen_loc");
            if (screenLocationValue != DreamValue.Null) UpdateScreenLocation(dreamObject, screenLocationValue);
        }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

            switch (varName) {
                case "x":
                case "y":
                case "z": {
                    EntityUid entity = _atomManager.GetMovableEntity(dreamObject);
                    if (!_entityManager.TryGetComponent(entity, out TransformComponent? transform))
                        return;

                    int x = (varName == "x") ? value.MustGetValueAsInteger() : (int)transform.WorldPosition.X;
                    int y = (varName == "y") ? value.MustGetValueAsInteger() : (int)transform.WorldPosition.Y;
                    int z = (varName == "z") ? value.MustGetValueAsInteger() : (int)transform.MapID;

                    _dreamMapManager.TryGetTurfAt((x, y), z, out var newLoc);
                    dreamObject.SetVariable("loc", new DreamValue(newLoc));
                    break;
                }
                case "loc": {
                    EntityUid entity = _atomManager.GetMovableEntity(dreamObject);
                    if (!_entityManager.TryGetComponent(entity, out TransformComponent? transform))
                        return;

                    if (_dreamMapManager.TryGetCellFromTransform(transform, out var oldMapCell)) {
                        oldMapCell.Movables.Remove(dreamObject);
                    }

                    if (value.TryGetValueAsDreamObjectOfType(_objectTree.Turf, out var turfLoc)) {
                        (Vector2i pos, IDreamMapManager.Level level) = _dreamMapManager.GetTurfPosition(turfLoc);
                        _transformSystem.SetParent(entity, level.Grid.Owner);
                        _transformSystem.SetWorldPosition(entity, pos);

                        var newMapCell = _dreamMapManager.GetCellFromTurf(turfLoc);
                        newMapCell.Movables.Add(dreamObject);
                    } else if (value.TryGetValueAsDreamObjectOfType(_objectTree.Movable, out var movableLoc)) {
                        EntityUid locEntity = _atomManager.GetMovableEntity(movableLoc);
                        _transformSystem.SetParent(entity, locEntity);
                        _transformSystem.SetLocalPosition(entity, Vector2.Zero);
                    } else if (value == DreamValue.Null) {
                        _transformSystem.SetParent(entity, _mapManager.GetMapEntityId(MapId.Nullspace));
                    } else {
                        throw new Exception($"Invalid loc {value}");
                    }

                    break;
                }
                case "name": {
                    value.TryGetValueAsString(out string? name);
                    EntityUid entity = _atomManager.GetMovableEntity(dreamObject);
                    if (!_entityManager.TryGetComponent(entity, out MetaDataComponent? metaData))
                        break;

                    metaData.EntityName = name;
                    break;
                }
                case "desc": {
                    value.TryGetValueAsString(out string? desc);
                    EntityUid entity = _atomManager.GetMovableEntity(dreamObject);
                    if (!_entityManager.TryGetComponent(entity, out MetaDataComponent? metaData))
                        break;

                    metaData.EntityDescription = desc;
                    break;
                }
                case "screen_loc":
                    UpdateScreenLocation(dreamObject, value);
                    break;
            }
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) {
            switch (varName) {
                case "x":
                case "y":
                case "z": {
                    EntityUid entity = _atomManager.GetMovableEntity(dreamObject);
                    if (!_entityManager.TryGetComponent(entity, out TransformComponent? transform))
                        return new(0);

                    float coordinate = varName switch {
                        "x" => transform.WorldPosition.X,
                        "y" => transform.WorldPosition.Y,
                        _ => (int)transform.MapID
                    };

                    return new(coordinate);
                }
                case "contents": {
                    DreamList contents = _objectTree.CreateList();
                    EntityUid entity = _atomManager.GetMovableEntity(dreamObject);

                    if (_entityManager.TryGetComponent<TransformComponent>(entity, out var transform)) {
                        using var childEnumerator = transform.ChildEnumerator;

                        while (childEnumerator.MoveNext(out EntityUid? child)) {
                            if (!_atomManager.TryGetMovableFromEntity(child.Value, out var childAtom))
                                continue;

                            contents.AddValue(new DreamValue(childAtom));
                        }
                    }

                    return new(contents);
                }
                case "locs": {
                    // Unimplemented; just return a list containing src.loc
                    DreamList locs = _objectTree.CreateList();
                    locs.AddValue(dreamObject.GetVariable("loc"));

                    return new DreamValue(locs);
                }
                default:
                    return ParentType?.OnVariableGet(dreamObject, varName, value) ?? value;
            }
        }

        private void UpdateScreenLocation(DreamObject movable, DreamValue screenLocationValue) {
            if (!_entityManager.TryGetComponent<DMISpriteComponent>(_atomManager.GetMovableEntity(movable), out var sprite))
                return;

            ScreenLocation screenLocation;
            if (screenLocationValue.TryGetValueAsString(out string? screenLocationString)) {
                screenLocation = new ScreenLocation(screenLocationString);
            } else {
                screenLocation = new ScreenLocation(0, 0, 0, 0);
            }

            sprite.ScreenLocation = screenLocation;
        }
    }
}
