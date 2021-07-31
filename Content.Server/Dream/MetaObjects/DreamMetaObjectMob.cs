using Content.Server.DM;
using Content.Shared.Dream;
using Robust.Server.Player;
using Robust.Shared.IoC;

namespace Content.Server.Dream.MetaObjects {
    class DreamMetaObjectMob : DreamMetaObjectMovable {
        private IDreamManager _dreamManager = IoCManager.Resolve<IDreamManager>();
        private IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);
            //Runtime.Mobs.Add(dreamObject);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            base.OnObjectDeleted(dreamObject);
            //Runtime.Mobs.Remove(dreamObject);
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            if (variableName == "key" || variableName == "ckey") {
                if (_playerManager.TryGetPlayerDataByUsername(variableValue.GetValueAsString(), out IPlayerData data)) {
                    PlayerSessionData sessionData = (PlayerSessionData)data.ContentDataUncast;

                    dreamObject.SetVariable("client", new DreamValue(sessionData.Client));
                }
            } else if (variableName == "see_invisible") {
               //TODO
            } else if (variableName == "client" && variableValue != oldVariableValue) {
                if (oldVariableValue.TryGetValueAsDreamObjectOfType(DreamPath.Client, out var oldClient)) {
                    oldClient.SetVariable("mob", DreamValue.Null);
                }

                if (variableValue.TryGetValueAsDreamObjectOfType(DreamPath.Client, out var client)) {
                    client.SetVariable("mob", new DreamValue(dreamObject));
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
            //DreamObject client = a.GetValueAsDreamObjectOfType(DreamPath.Mob).GetVariable("client").GetValueAsDreamObjectOfType(DreamPath.Client);
            //DreamConnection connection = Runtime.Server.GetConnectionFromClient(client);
            
            //connection.OutputDreamValue(b);
            return new DreamValue(0);
        }
    }
}
