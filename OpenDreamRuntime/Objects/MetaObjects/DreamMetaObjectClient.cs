using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Rendering;
using System.Security.Cryptography;
using System.Text;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectClient : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        public static readonly Dictionary<DreamObject, ClientScreenList> ScreenLists = new();
        public static readonly Dictionary<DreamObject, VerbsList> VerbLists = new();

        private readonly ServerScreenOverlaySystem? _screenOverlaySystem;

        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IDreamManager _dreamManager = default!;
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;

        public DreamMetaObjectClient() {
            IoCManager.InjectDependencies(this);

            _entitySystemManager.TryGetEntitySystem(out _screenOverlaySystem);
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            ParentType?.OnObjectCreated(dreamObject, creationArguments);

            var connection = _dreamManager.GetConnectionFromClient(dreamObject);
            ScreenLists.Add(dreamObject, new ClientScreenList(_objectTree, _screenOverlaySystem, connection));
            VerbLists.Add(dreamObject, new VerbsList(_objectTree, dreamObject));

            _dreamManager.Clients.Add(dreamObject);
        }

        public void OnObjectDeleted(DreamObject dreamObject) {
            ParentType?.OnObjectDeleted(dreamObject);
            VerbLists.Remove(dreamObject);
            _dreamManager.Clients.Remove(dreamObject);
        }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

            switch (varName) {
                case "mob": {
                    value.TryGetValueAsDreamObjectOfType(_objectTree.Mob, out var newMob);

                    _dreamManager.GetConnectionFromClient(dreamObject).Mob = newMob;
                    break;
                }
                case "screen": {
                    ClientScreenList screenList = ScreenLists[dreamObject];

                    screenList.Cut();

                    if (value.TryGetValueAsDreamList(out var valueList)) {
                        foreach (DreamValue screenValue in valueList.GetValues()) {
                            screenList.AddValue(screenValue);
                        }
                    } else if (value != DreamValue.Null) {
                        screenList.AddValue(value);
                    }

                    break;
                }
                case "images": {
                    //TODO properly implement this var
                    if (oldValue.TryGetValueAsDreamList(out var oldList)) {
                        oldList.Cut();
                    }

                    if (!value.TryGetValueAsDreamList(out var imageList)) {
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
                case "ckey":
                    return new(DreamProcNativeHelpers.Ckey(_dreamManager.GetConnectionFromClient(dreamObject).Session!.Name));
                case "key":
                    return new(_dreamManager.GetConnectionFromClient(dreamObject).Session!.Name);
                case "mob":
                    return new(_dreamManager.GetConnectionFromClient(dreamObject).Mob);
                case "computer_id": // FIXME: This is not secure! Whenever RT implements a more robust (heh) method of uniquely identifying computers, replace this impl with that.
                    MD5 md5 = MD5.Create();
                    // Check on Robust.Shared.Network.NetUserData.HWId" if you want to seed from how RT does user identification.
                    // We don't use it here because it is probably not enough to ensure security, and (as of time of writing) only works on Windows machines.
                    byte[] brown = Encoding.UTF8.GetBytes(_dreamManager.GetConnectionFromClient(dreamObject).Session!.Name);
                    byte[] hash = md5.ComputeHash(brown);
                    string hashStr = BitConverter.ToString(hash).Replace("-", "").ToLower().Substring(0,15); // Extracting the first 15 digits to ensure it'll fit in a 64-bit number
                    return new(long.Parse(hashStr, System.Globalization.NumberStyles.HexNumber).ToString()); // Converts from hex to decimal. Output is in analogous format to BYOND's.
                case "address":
                    return new(_dreamManager.GetConnectionFromClient(dreamObject).Session!.ConnectedClient.RemoteEndPoint.Address.ToString());
                case "inactivity":
                    return new DreamValue(0);
                case "timezone": {
                    //DreamConnection connection = Runtime.Server.GetConnectionFromClient(dreamObject);
                    //return new DreamValue((float)connection.ClientData.Timezone.BaseUtcOffset.TotalHours);
                    return new(0);
                }
                case "statpanel": {
                    DreamConnection connection = _dreamManager.GetConnectionFromClient(dreamObject);

                    return new DreamValue(connection.SelectedStatPanel);
                }
                case "connection":
                    return new DreamValue("seeker");
                case "screen":
                    return new DreamValue(ScreenLists[dreamObject]);
                case "verbs":
                    return new DreamValue(VerbLists[dreamObject]);
                case "vars": // /client has this too!
                    return new DreamValue(new DreamListVars(_objectTree.List.ObjectDefinition, dreamObject));
                default:
                    return ParentType?.OnVariableGet(dreamObject, varName, value) ?? value;
            }
        }

        public void OperatorOutput(DreamObject client, DreamValue b) {
            DreamConnection connection = _dreamManager.GetConnectionFromClient(client);
            connection.OutputDreamValue(b);
        }
    }
}
