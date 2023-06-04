using System.Security.Cryptography;
using System.Text;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Rendering;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectClient : DreamObject {
    public readonly DreamConnection Connection;
    public readonly ClientScreenList Screen;
    public readonly VerbsList Verbs;
    public readonly DreamList Images; // TODO properly implement /client.images

    public DreamObjectClient(DreamObjectDefinition objectDefinition, DreamConnection connection, ServerScreenOverlaySystem? screenOverlaySystem) : base(objectDefinition) {
        Connection = connection;
        Screen = new(ObjectTree, screenOverlaySystem, Connection);
        Verbs = new(ObjectTree, this);
        Images = ObjectTree.CreateList();

        DreamManager.Clients.Add(this);
    }

    protected override void HandleDeletion() {
        DreamManager.Clients.Remove(this);

        base.HandleDeletion();
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        switch (varName) {
            case "ckey":
                value = new(DreamProcNativeHelpers.Ckey(Connection.Session!.Name));
                return true;
            case "key":
                value = new(Connection.Session!.Name);
                return true;
            case "mob":
                value = new(Connection.Mob);
                return true;
            case "computer_id": // FIXME: This is not secure! Whenever RT implements a more robust (heh) method of uniquely identifying computers, replace this impl with that.
                MD5 md5 = MD5.Create();
                // Check on Robust.Shared.Network.NetUserData.HWId" if you want to seed from how RT does user identification.
                // We don't use it here because it is probably not enough to ensure security, and (as of time of writing) only works on Windows machines.
                byte[] brown = Encoding.UTF8.GetBytes(Connection.Session!.Name);
                byte[] hash = md5.ComputeHash(brown);
                string hashStr = BitConverter.ToString(hash).Replace("-", "").ToLower().Substring(0,15); // Extracting the first 15 digits to ensure it'll fit in a 64-bit number

                value = new(long.Parse(hashStr, System.Globalization.NumberStyles.HexNumber).ToString()); // Converts from hex to decimal. Output is in analogous format to BYOND's.
                return true;
            case "address":
                value = new(Connection.Session!.ConnectedClient.RemoteEndPoint.Address.ToString());
                return true;
            case "inactivity":
                value = new(0); // TODO
                return true;
            case "timezone":
                value = new(0); // TODO
                return true;
            case "statpanel":
                value = new(Connection.SelectedStatPanel);
                return true;
            case "connection":
                value = new("seeker");
                return true;
            case "screen":
                value = new(Screen);
                return true;
            case "verbs":
                value = new(Verbs);
                return true;
            case "images":
                value = new(Images);
                return true;
            default:
                return base.TryGetVar(varName, out value);
        }
    }

    protected override void SetVar(string varName, DreamValue value) {
        switch (varName) {
            case "mob": {
                value.TryGetValueAsDreamObject<DreamObjectMob>(out var newMob);

                Connection.Mob = newMob;
                break;
            }
            case "screen": {
                Screen.Cut();

                if (value.TryGetValueAsDreamList(out var valueList)) {
                    foreach (DreamValue screenValue in valueList.GetValues()) {
                        Screen.AddValue(screenValue);
                    }
                } else if (value != DreamValue.Null) {
                    Screen.AddValue(value);
                }

                break;
            }
            case "images": {
                Images.Cut();

                if (value.TryGetValueAsDreamList(out var valueList)) {
                    foreach (DreamValue screenValue in valueList.GetValues()) {
                        Images.AddValue(screenValue);
                    }
                } else if (value != DreamValue.Null) {
                    Images.AddValue(value);
                }

                break;
            }
            case "statpanel":
                //connection.SelectedStatPanel = variableValue.GetValueAsString();
                break;
            default:
                base.SetVar(varName, value);
                break;
        }
    }

    public override void OperatorOutput(DreamValue b) {
        Connection.OutputDreamValue(b);
    }
}
