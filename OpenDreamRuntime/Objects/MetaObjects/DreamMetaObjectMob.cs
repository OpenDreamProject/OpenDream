using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;
using Robust.Server.Player;
using OpenDreamShared.Rendering;

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
            ParentType?.OnObjectCreated(dreamObject, creationArguments);
            _dreamManager.Mobs.Add(dreamObject);
            EntityUid entity = _atomManager.GetMovableEntity(dreamObject);
            DreamMobSightComponent mobSightComponent = _entityManager.AddComponent<DreamMobSightComponent>(entity);
            dreamObject.TryGetVariable("see_invisible", out DreamValue seevis);
            mobSightComponent.SeeInvisibility = (sbyte)seevis.MustGetValueAsInteger();

        }

        public void OnObjectDeleted(DreamObject dreamObject) {
            ParentType?.OnObjectDeleted(dreamObject);
            _dreamManager.Mobs.Remove(dreamObject);
        }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

            if (varName == "key" || varName == "ckey") {
                if (_playerManager.TryGetSessionByUsername(value.GetValueAsString(), out var session)) {
                    var connection = _dreamManager.GetConnectionBySession(session);

                    connection.MobDreamObject = dreamObject;
                }
            } else if (varName == "see_invisible") {
                value.TryGetValueAsInteger(out int seevis);
                EntityUid entity = _atomManager.GetMovableEntity(dreamObject);
                DreamMobSightComponent mobSightComponent = _entityManager.GetComponent<DreamMobSightComponent>(entity);
                mobSightComponent.SeeInvisibility = (sbyte)seevis;
                mobSightComponent.Dirty();
                dreamObject.SetVariableValue("see_invisible", new DreamValue(seevis));
            } else if (varName == "client" && value != oldValue) {
                var newClient = value.GetValueAsDreamObject();
                var oldClient = oldValue.GetValueAsDreamObject();

                if (newClient != null) {
                    _dreamManager.GetConnectionFromClient(newClient).MobDreamObject = dreamObject;
                } else if (oldClient != null) {
                    _dreamManager.GetConnectionFromClient(oldClient).MobDreamObject = null;
                }
            }
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) {
            if (varName == "key" || varName == "ckey") {
                if (dreamObject.GetVariable("client").TryGetValueAsDreamObjectOfType(_objectTree.Client, out var client)) {
                    return client.GetVariable(varName);
                } else {
                    return DreamValue.Null;
                }
            } else if (varName == "client") {
                return new(_dreamManager.GetClientFromMob(dreamObject));
            } else {
                return ParentType?.OnVariableGet(dreamObject, varName, value) ?? value;
            }
        }

        public void OperatorOutput(DreamValue a, DreamValue b) {
            if (!a.TryGetValueAsDreamObjectOfType(_objectTree.Mob, out var mob))
                throw new ArgumentException($"Left-hand value was not the expected type {_objectTree.Mob}");
            if (!mob.GetVariable("client").TryGetValueAsDreamObjectOfType(_objectTree.Client, out var client))
                return;

            DreamConnection connection = _dreamManager.GetConnectionFromClient(client);
            connection.OutputDreamValue(b);
        }
    }
}
