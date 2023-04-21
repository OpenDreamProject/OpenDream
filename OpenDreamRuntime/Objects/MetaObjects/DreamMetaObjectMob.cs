using OpenDreamRuntime.Procs;
using Robust.Server.Player;
using OpenDreamShared.Rendering;
using Robust.Shared.Utility;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectMob : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly IDreamManager _dreamManager = default!;
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IAtomManager _atomManager = default!;

        public DreamMetaObjectMob() {
            IoCManager.InjectDependencies(this);
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            _atomManager.Mobs.Add(dreamObject);

            ParentType?.OnObjectCreated(dreamObject, creationArguments);

            EntityUid entity = _atomManager.GetMovableEntity(dreamObject);
            DreamMobSightComponent mobSightComponent = _entityManager.AddComponent<DreamMobSightComponent>(entity);
            dreamObject.TryGetVariable("see_invisible", out DreamValue seeVis);
            mobSightComponent.SeeInvisibility = (sbyte)seeVis.MustGetValueAsInteger();
        }

        public void OnObjectDeleted(DreamObject dreamObject) {
            _atomManager.Mobs.RemoveSwap(_atomManager.Mobs.IndexOf(dreamObject));

            ParentType?.OnObjectDeleted(dreamObject);
        }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

            if (varName == "key" || varName == "ckey") {
                if (_playerManager.TryGetSessionByUsername(value.GetValueAsString(), out var session)) {
                    var connection = _dreamManager.GetConnectionBySession(session);

                    connection.Mob = dreamObject;
                }
            } else if (varName == "see_invisible") {
                value.TryGetValueAsInteger(out int seeVis);
                EntityUid entity = _atomManager.GetMovableEntity(dreamObject);
                DreamMobSightComponent mobSightComponent = _entityManager.GetComponent<DreamMobSightComponent>(entity);
                mobSightComponent.SeeInvisibility = (sbyte)seeVis;
                mobSightComponent.Dirty();
                dreamObject.SetVariableValue("see_invisible", new DreamValue(seeVis));
            } else if (varName == "client" && value != oldValue) {
                var newClient = value.GetValueAsDreamObject();
                var oldClient = oldValue.GetValueAsDreamObject();

                if (newClient != null) {
                    _dreamManager.GetConnectionFromClient(newClient).Mob = dreamObject;
                } else if (oldClient != null) {
                    _dreamManager.GetConnectionFromClient(oldClient).Mob = null;
                }
            }
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) {
            if (varName == "client") {
                _dreamManager.TryGetConnectionFromMob(dreamObject, out var connection);

                return new(connection?.Client);
            } else {
                return ParentType?.OnVariableGet(dreamObject, varName, value) ?? value;
            }
        }

        public ProcStatus? OperatorOutput(DreamValue a, DreamValue b, DMProcState state) {
            if (!a.TryGetValueAsDreamObjectOfType(_objectTree.Mob, out var mob))
                throw new ArgumentException($"Left-hand value was not the expected type {_objectTree.Mob}");
            if (_dreamManager.TryGetConnectionFromMob(mob, out var connection))
                connection.OutputDreamValue(b);

            state.Push(DreamValue.Null);
            return null;
        }
    }
}
