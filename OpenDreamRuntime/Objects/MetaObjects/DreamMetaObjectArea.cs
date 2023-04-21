using OpenDreamRuntime.Procs;
using Robust.Shared.Utility;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectArea : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly IAtomManager _atomManager = default!;
        [Dependency] private readonly IDreamManager _dreamManager = default!;
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;
        [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        private readonly EntityQuery<TransformComponent> _transformQuery;

        public DreamMetaObjectArea() {
            IoCManager.InjectDependencies(this);

            _transformQuery = _entityManager.GetEntityQuery<TransformComponent>();
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            DreamList contents = _objectTree.CreateList();

            contents.ValueAssigned += (_, _, value) => {
                if (!value.TryGetValueAsDreamObjectOfType(_objectTree.Turf, out var turf))
                    return;

                (Vector2i pos, IDreamMapManager.Level level) = _dreamMapManager.GetTurfPosition(turf);
                level.SetArea(pos, dreamObject);
            };

            _atomManager.Areas.Add(dreamObject);
            _dreamManager.AreaContents.Add(dreamObject, contents);

            ParentType?.OnObjectCreated(dreamObject, creationArguments);
        }

        public void OnObjectDeleted(DreamObject dreamObject) {
            _atomManager.Areas.RemoveSwap(_atomManager.Areas.IndexOf(dreamObject));
            _dreamManager.AreaContents.Remove(dreamObject);

            ParentType?.OnObjectDeleted(dreamObject);
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) {
            if (varName == "contents") {
                return new DreamValue(_dreamManager.AreaContents[dreamObject]);
            } else {
                return ParentType?.OnVariableGet(dreamObject, varName, value) ?? value;
            }
        }

        public ProcStatus? OperatorOutput(DreamValue a, DreamValue b, DMProcState state) {
            if (b.TryGetValueAsDreamObjectOfType(_objectTree.Sound, out _)) {
                // Output the sound to every connection with a mob inside the area
                foreach (var connection in _dreamManager.Connections) {
                    var mob = connection.Mob;
                    if (mob == null)
                        continue;

                    var mobEntity = _atomManager.GetMovableEntity(mob);
                    if (!_transformQuery.TryGetComponent(mobEntity, out var mobTransform))
                        continue;

                    if (!_dreamMapManager.TryGetCellFromTransform(mobTransform, out var cell))
                        continue;

                    if (cell.Area != a.MustGetValueAsDreamObject())
                        continue;

                    connection.OutputDreamValue(b);
                }

            }

            state.Push(DreamValue.Null);
            return null;
        }
    }
}
