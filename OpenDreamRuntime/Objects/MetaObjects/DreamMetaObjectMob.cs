using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;
using Robust.Server.Player;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectMob : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly IDreamManager _dreamManager = default!;
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public DreamMetaObjectMob() {
            IoCManager.InjectDependencies(this);
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            ParentType?.OnObjectCreated(dreamObject, creationArguments);
            _dreamManager.Mobs.Add(dreamObject);
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
               //TODO
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
