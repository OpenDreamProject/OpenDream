using OpenDreamShared.Input;
using OpenDreamClient.Interface;
using Robust.Shared.Network;

namespace OpenDreamClient.Input {
    public sealed class DreamCommandSystem : SharedDreamCommandSystem {
        [Dependency] private readonly IDreamInterfaceManager _interfaceManager = default!;

        public void RunCommand(string command) {
            switch (command) {
                case string x when x.StartsWith(".quit"):
                    IoCManager.Resolve<IClientNetManager>().ClientDisconnect(".quit used");
                    break;

                case string x when x.StartsWith(".screenshot"):
                    string[] split = command.Split(" ");
                    _interfaceManager.SaveScreenshot(split.Length == 1 || split[1] != "auto");
                    break;

                case string x when x.StartsWith(".configure"):
                    Log.Warning(".configure command is not implemented");
                    break;

                case string x when x.StartsWith(".winset"):
                    // Everything after .winset, excluding the space and quotes
                    string winsetParams = command.Substring(7); //clip .winset
                    winsetParams = winsetParams.Trim(); //clip space
                    winsetParams = winsetParams.Trim('\"'); //clip quotes

                    _interfaceManager.WinSet(null, winsetParams);
                    break;

                default: {
                    // Send the entire command to the server.
                    // It has more info about argument types so it can parse it better than we can.
                    RaiseNetworkEvent(new CommandEvent(command));
                    break;
                }
            }
        }

        public void StartRepeatingCommand(string command) {
            RaiseNetworkEvent(new RepeatCommandEvent(command));
        }

        public void StopRepeatingCommand(string command) {
            RaiseNetworkEvent(new StopRepeatCommandEvent(command));
        }
    }
}
