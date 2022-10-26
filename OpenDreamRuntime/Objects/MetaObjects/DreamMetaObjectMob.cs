using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;
using Robust.Server.Player;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectMob : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly IDreamManager _dreamManager = default!;
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
                if (value.TryGetValueAsString(out var username) && _playerManager.TryGetSessionByUsername(username, out var session)) {
                    var connection = _dreamManager.GetConnectionBySession(session);

                    connection.MobDreamObject = dreamObject;
                }
            } else if (varName == "see_invisible") {
               //TODO
            } else if (varName == "client" && value != oldValue) {
                if(!value.TryGetValueAsDreamObject(out var newClient)) {
                    Logger.Warning("mob's client set to invalid value");
                }
                DreamObject oldClient = oldValue.GetValueAsDreamObject();

                if (newClient != null) {
                    _dreamManager.GetConnectionFromClient(newClient).MobDreamObject = dreamObject;
                } else if (oldClient != null) {
                    _dreamManager.GetConnectionFromClient(oldClient).MobDreamObject = null;
                }
            }
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) {
            if (varName == "key" || varName == "ckey") {
                if (dreamObject.GetVariable("client").TryGetValueAsDreamObjectOfType(DreamPath.Client, out var client)) {
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

        public DreamValue OperatorOutput(DreamValue a, DreamValue b) {
            if (!a.TryGetValueAsDreamObjectOfType(DreamPath.Mob, out var mob))
                throw new ArgumentException($"Left-hand value was not the expected type {DreamPath.Mob}");
            if (!mob.GetVariable("client").TryGetValueAsDreamObjectOfType(DreamPath.Client, out var client))
                throw new Exception($"Failed to get client from {mob}");

            DreamConnection connection = _dreamManager.GetConnectionFromClient(client);
            connection.OutputDreamValue(b);
            return new DreamValue(0);
        }
    }
}
