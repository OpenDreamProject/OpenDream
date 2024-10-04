using System.Security.Cryptography;
using System.Text;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectClient : DreamObject {
    public readonly DreamConnection Connection;
    public readonly ClientScreenList Screen;
    public readonly ClientImagesList Images;
    public readonly ClientVerbsList ClientVerbs;
    public ViewRange View { get; private set; }
    public bool ShowPopupMenus { get; private set; } = true;

    public DreamObjectClient(DreamObjectDefinition objectDefinition, DreamConnection connection, ServerScreenOverlaySystem? screenOverlaySystem, ServerClientImagesSystem? clientImagesSystem) : base(objectDefinition) {
        Connection = connection;
        Screen = new(ObjectTree, screenOverlaySystem, Connection);
        ClientVerbs = new(ObjectTree, VerbSystem, this);
        Images = new(ObjectTree, clientImagesSystem, Connection);

        DreamManager.Clients.Add(this);

        View = DreamManager.WorldInstance.DefaultView;
    }

    protected override void HandleDeletion(bool possiblyThreaded) {
        // SAFETY: Client hashset is not threadsafe, this is not a hot path so no reason to change this.
        if (possiblyThreaded) {
            EnterIntoDelQueue();
            return;
        }

        Connection.Session?.Channel.Disconnect("Your client object was deleted");
        DreamManager.Clients.Remove(this);

        base.HandleDeletion(possiblyThreaded);
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
            case "statobj":
                value = Connection.StatObj;
                return true;
            case "eye":
                value = new(Connection.Eye);
                return true;
            case "view":
                // Number if square & centerable, string representation otherwise
                if (View is { IsSquare: true, IsCenterable: true }) {
                    value = new DreamValue(View.Range);
                } else {
                    value = new DreamValue(View.ToString());
                }

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
                value = new(Connection.Session!.Channel.RemoteEndPoint.Address.ToString());
                return true;
            case "inactivity":
                value = new(0); // TODO
                return true;
            case "timezone":
                value = new(0); // TODO
                return true;
            case "statpanel":
                value = Connection.SelectedStatPanel is null ? DreamValue.Null : new(Connection.SelectedStatPanel);
                return true;
            case "connection":
                value = new("seeker");
                return true;
            case "screen":
                value = new(Screen);
                return true;
            case "verbs":
                value = new(ClientVerbs);
                return true;
            case "show_popup_menus":
                value = new(ShowPopupMenus ? 1 : 0);
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
            case "statobj":
                Connection.StatObj = value;
                break;
            case "eye": {
                value.TryGetValueAsDreamObject<DreamObjectAtom>(out var newEye);
                if (newEye is not (DreamObjectMovable or null)) {
                    throw new Exception($"Cannot set eye to non-movable {value}"); // TODO: You can set it to a turf
                }

                Connection.Eye = newEye as DreamObjectMovable;
                break;
            }
            case "view": {
                if (value.TryGetValueAsInteger(out var viewInt)) {
                    View = new(viewInt);
                } else if (value.TryGetValueAsString(out var viewStr)) {
                    View = new(viewStr);
                } else {
                    View = DreamManager.WorldInstance.DefaultView;
                }

                Connection.SendClientInfoUpdate();
                break;
            }
            case "show_popup_menus": {
                // TODO: See what BYOND does with non-integer values. Per the ref only 0 should disable, but this needs to be verified.
                if (value.TryGetValueAsInteger(out var viewInt) && viewInt == 0) {
                    ShowPopupMenus = false;
                } else {
                    ShowPopupMenus = true;
                }

                Connection.SendClientInfoUpdate();
                break;
            }
            case "screen": {
                Screen.Cut();

                if (value.TryGetValueAsDreamList(out var valueList)) {
                    foreach (DreamValue screenValue in valueList.GetValues()) {
                        Screen.AddValue(screenValue);
                    }
                } else if (!value.IsNull) {
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
                } else if (!value.IsNull) {
                    Images.AddValue(value);
                }

                break;
            }
            case "verbs": {
                ClientVerbs.Cut();

                if (value.TryGetValueAsDreamList(out var valueList)) {
                    foreach (DreamValue verbValue in valueList.GetValues()) {
                        ClientVerbs.AddValue(verbValue);
                    }
                } else {
                    ClientVerbs.AddValue(value);
                }

                break;
            }
            case "statpanel":
                if (!value.TryGetValueAsString(out var statPanel))
                    return;

                Connection.SelectedStatPanel = statPanel;
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
