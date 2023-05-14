using OpenDreamShared.Input;
using OpenDreamClient.Interface;
using Robust.Shared.Network;

namespace OpenDreamClient.Input {
    public sealed class DreamCommandSystem : SharedDreamCommandSystem {
        [Dependency] private readonly IDreamInterfaceManager _interfaceManager = default!;

        public void RunCommand(string command) {
            string[] split = command.Split(" ");
            string verb = split[0];

            switch (verb) {
                case ".quit":
                    IoCManager.Resolve<IClientNetManager>().ClientDisconnect(".quit used");
                    break;

                case ".screenshot":
                    _interfaceManager.SaveScreenshot(split.Length == 1 || split[1] != "auto");
                    break;

                case ".configure":
                    Logger.WarningS("opendream.commands", ".configure command is not implemented");
                    break;

                case ".winset":
                    // Everything after .winset, excluding the space and quotes
                    string winsetParams = command.Substring(verb.Length + 2, command.Length - verb.Length - 3);

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
