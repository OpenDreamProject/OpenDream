using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
using System.Security.Cryptography;
using System.Text;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectClient : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        public static readonly Dictionary<DreamObject, VerbsList> VerbLists = new();

        private readonly ServerScreenOverlaySystem? _screenOverlaySystem;
        private readonly Dictionary<DreamList, DreamObject> _screenListToClient = new();

        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IDreamManager _dreamManager = default!;
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;

        public DreamMetaObjectClient() {
            IoCManager.InjectDependencies(this);

            _entitySystemManager.TryGetEntitySystem(out _screenOverlaySystem);
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            ParentType?.OnObjectCreated(dreamObject, creationArguments);

            VerbLists.Add(dreamObject, new VerbsList(_objectTree, dreamObject));

            _dreamManager.Clients.Add(dreamObject);

            ClientPerspective perspective = (ClientPerspective)dreamObject.GetVariable("perspective").GetValueAsInteger();
            if (perspective != ClientPerspective.Mob) {
                //Runtime.StateManager.AddClientPerspectiveDelta(connection.CKey, perspective);
            }
        }

        public void OnObjectDeleted(DreamObject dreamObject) {
            ParentType?.OnObjectDeleted(dreamObject);
            VerbLists.Remove(dreamObject);
            _dreamManager.Clients.Remove(dreamObject);
        }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

            switch (varName) {
                case "eye": {
                    string ckey = dreamObject.GetVariable("ckey").GetValueAsString();
                    DreamObject eye = value.GetValueAsDreamObject();

                    //Runtime.StateManager.AddClientEyeIDDelta(ckey, eyeID);
                    break;
                }
                case "perspective": {
                    string ckey = dreamObject.GetVariable("ckey").GetValueAsString();

                    //Runtime.StateManager.AddClientPerspectiveDelta(ckey, (ClientPerspective)variableValue.GetValueAsInteger());
                    break;
                }
                case "mob": {
                    DreamConnection connection = _dreamManager.GetConnectionFromClient(dreamObject);

                    connection.MobDreamObject = value.GetValueAsDreamObject();
                    break;
                }
                case "screen": {
                    if (oldValue.TryGetValueAsDreamList(out DreamList oldList)) {
                        oldList.Cut();
                        oldList.ValueAssigned -= ScreenValueAssigned;
                        oldList.BeforeValueRemoved -= ScreenBeforeValueRemoved;
                        _screenListToClient.Remove(oldList);
                    }

                    DreamList screenList;
                    if (!value.TryGetValueAsDreamList(out screenList)) {
                        screenList = _objectTree.CreateList();
                    }

                    screenList.ValueAssigned += ScreenValueAssigned;
                    screenList.BeforeValueRemoved += ScreenBeforeValueRemoved;
                    _screenListToClient[screenList] = dreamObject;
                    break;
                }
                case "images":
                {
                    //TODO properly implement this var
                    if (oldValue.TryGetValueAsDreamList(out DreamList oldList)) {
                        oldList.Cut();
                    }

                    DreamList imageList;
                    if (!value.TryGetValueAsDreamList(out imageList)) {
                        imageList = _objectTree.CreateList();
                    }

                    dreamObject.SetVariableValue(varName, new DreamValue(imageList));
                    break;
                }
                case "statpanel": {
                    //DreamConnection connection = Runtime.Server.GetConnectionFromClient(dreamObject);

                    //connection.SelectedStatPanel = variableValue.GetValueAsString();
                    break;
                }
            }
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) {
            switch (varName) {
                //TODO actually return the key
                case "key":
                case "ckey":
                    return new(_dreamManager.GetSessionFromClient(dreamObject).Name);
                case "computer_id": // FIXME: This is not secure! Whenever RT implements a more robust (heh) method of uniquely identifying computers, replace this impl with that.
                    MD5 md5 = MD5.Create();
                    /// <remarks>Check on <see cref="Robust.Shared.Network.NetUserData.HWId"/> if you want to seed from how RT does user identification.
                    /// We don't use it here because it is probably not enough to ensure security, and (as of time of writing) only works on Windows machines.</remarks>
                    byte[] brown = Encoding.UTF8.GetBytes(_dreamManager.GetSessionFromClient(dreamObject).Name);
                    byte[] hash = md5.ComputeHash(brown);
                    string hashStr = BitConverter.ToString(hash).Replace("-", "").ToLower().Substring(0,15); // Extracting the first 15 digits to ensure it'll fit in a 64-bit number
                    return new(long.Parse(hashStr, System.Globalization.NumberStyles.HexNumber).ToString()); // Converts from hex to decimal. Output is in analogous format to BYOND's.
                case "address":
                    return new(_dreamManager.GetSessionFromClient(dreamObject).ConnectedClient.RemoteEndPoint.Address.ToString());
                case "inactivity":
                    return new DreamValue(0);
                case "timezone": {
                    //DreamConnection connection = Runtime.Server.GetConnectionFromClient(dreamObject);
                    //return new DreamValue((float)connection.ClientData.Timezone.BaseUtcOffset.TotalHours);
                    return new(0);
                }
                case "statpanel": {
                    //DreamConnection connection = Runtime.Server.GetConnectionFromClient(dreamObject);
                    //return new DreamValue(connection.SelectedStatPanel);
                    return DreamValue.Null;
                }
                case "mob":
                {
                    var connection = _dreamManager.GetConnectionFromClient(dreamObject);
                    return new DreamValue(connection.MobDreamObject);
                }
                case "connection":
                    return new DreamValue("seeker");
                case "verbs":
                    return new DreamValue(VerbLists[dreamObject]);
                case "vars": // /client has this too!
                    return new DreamValue(new DreamListVars(_objectTree.List.ObjectDefinition, dreamObject));
                default:
                    return ParentType?.OnVariableGet(dreamObject, varName, value) ?? value;
            }
        }

        public void OperatorOutput(DreamValue a, DreamValue b) {
            if (!a.TryGetValueAsDreamObjectOfType(_objectTree.Client, out var client))
                throw new ArgumentException($"Left-hand value was not the expected type {_objectTree.Client}");

            DreamConnection connection = _dreamManager.GetConnectionFromClient(client);
            connection.OutputDreamValue(b);
        }

        private void ScreenValueAssigned(DreamList screenList, DreamValue screenKey, DreamValue screenValue) {
            if (!screenValue.TryGetValueAsDreamObjectOfType(_objectTree.Movable, out var movable))
                return;

            var connection = _dreamManager.GetConnectionFromClient(_screenListToClient[screenList]);
            if (connection == null)
                return;

            _screenOverlaySystem?.AddScreenObject(connection, movable);
        }

        private void ScreenBeforeValueRemoved(DreamList screenList, DreamValue screenKey, DreamValue screenValue) {
            if (!screenValue.TryGetValueAsDreamObjectOfType(_objectTree.Movable, out var movable))
                return;

            var connection = _dreamManager.GetConnectionFromClient(_screenListToClient[screenList]);
            if (connection == null)
                return;

            _screenOverlaySystem?.RemoveScreenObject(connection, movable);
        }
    }
}
