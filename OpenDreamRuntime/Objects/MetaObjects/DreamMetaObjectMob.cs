using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;
using Robust.Server.Player;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectMob : DreamMetaObjectMovable {
        private IDreamManager _dreamManager = IoCManager.Resolve<IDreamManager>();
        private IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);
            _dreamManager.Mobs.Add(dreamObject);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            base.OnObjectDeleted(dreamObject);
            _dreamManager.Mobs.Remove(dreamObject);
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            if (variableName == "key" || variableName == "ckey") {
                if (_playerManager.TryGetSessionByUsername(variableValue.GetValueAsString(), out var session)) {
                    var connection = _dreamManager.GetConnectionBySession(session);

                    connection.MobDreamObject = dreamObject;
                }
            } else if (variableName == "see_invisible") {
               //TODO
            } else if (variableName == "client" && variableValue != oldVariableValue) {
                var newClient = variableValue.GetValueAsDreamObject();
                var oldClient = oldVariableValue.GetValueAsDreamObject();

                if (newClient != null) {
                    _dreamManager.GetConnectionFromClient(newClient).MobDreamObject = dreamObject;
                } else if (oldClient != null) {
                    _dreamManager.GetConnectionFromClient(oldClient).MobDreamObject = null;
                }
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "key" || variableName == "ckey") {
                if (dreamObject.GetVariable("client").TryGetValueAsDreamObjectOfType(DreamPath.Client, out var client)) {
                    return client.GetVariable(variableName);
                } else {
                    return DreamValue.Null;
                }
            } else if (variableName == "client") {
                return new(_dreamManager.GetClientFromMob(dreamObject));
            } else {
              return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }

        public override DreamValue OperatorOutput(DreamValue a, DreamValue b) {
            DreamObject client = a.GetValueAsDreamObjectOfType(DreamPath.Mob).GetVariable("client").GetValueAsDreamObjectOfType(DreamPath.Client);
            DreamConnection connection = _dreamManager.GetConnectionFromClient(client);

            connection.OutputDreamValue(b);
            return new DreamValue(0);
        }
    }
}
