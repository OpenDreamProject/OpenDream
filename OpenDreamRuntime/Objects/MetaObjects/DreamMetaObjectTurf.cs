using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectTurf : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly IAtomManager _atomManager = default!;
        [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        private readonly EntityLookupSystem _entityLookup;
        private readonly EntityQuery<TransformComponent> _transformQuery;

        public DreamMetaObjectTurf() {
            IoCManager.InjectDependencies(this);

            _entityLookup = _entitySystemManager.GetEntitySystem<EntityLookupSystem>();
            _transformQuery = _entityManager.GetEntityQuery<TransformComponent>();
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            ParentType?.OnObjectCreated(dreamObject, creationArguments);

            IconAppearance turfAppearance = _atomManager.CreateAppearanceFromAtom(dreamObject);
            _dreamMapManager.SetTurfAppearance(dreamObject, turfAppearance);
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) {
            switch (varName) {
                case "x":
                case "y":
                case "z": {
                    (Vector2i pos, DreamMapManager.Level level) = _dreamMapManager.GetTurfPosition(dreamObject);

                    int coord = varName == "x" ? pos.X :
                                varName == "y" ? pos.Y :
                                level.Z;
                    return new(coord);
                }
                case "loc": {
                    return new(_dreamMapManager.GetAreaAt(dreamObject));
                }
                case "contents": {
                    (Vector2i pos, DreamMapManager.Level level) = _dreamMapManager.GetTurfPosition(dreamObject);

                    HashSet<EntityUid> entities = _entityLookup.GetEntitiesIntersecting(level.Grid.Owner, pos, LookupFlags.Uncontained);
                    DreamList contents = DreamList.Create(entities.Count);
                    foreach (EntityUid movableEntity in entities) {
                        if (!_transformQuery.TryGetComponent(movableEntity, out var transform))
                            continue;

                        // Entities on neighboring tiles seem to be caught as well
                        if (transform.WorldPosition != pos)
                            continue;
                        if (transform.ParentUid != level.Grid.Owner)
                            continue;
                        if (!_atomManager.TryGetMovableFromEntity(movableEntity, out var movable))
                            continue;

                        contents.AddValue(new(movable));
                    }

                    return new(contents);
                }
                default:
                    return ParentType?.OnVariableGet(dreamObject, varName, value) ?? value;
            }
        }
    }
}
