using OpenDreamShared.Input;
using OpenDreamClient.Interface;
using Robust.Shared.Network;

namespace OpenDreamClient.Input {
    sealed class DreamCommandSystem : SharedDreamCommandSystem {
        public void RunCommand(string command) {
            string[] split = command.Split(" ");
            string verb = split[0];

            switch (verb) {
                case ".quit":
                    IoCManager.Resolve<IClientNetManager>().ClientDisconnect(".quit used");
                    break;

                case ".screenshot":
                    var interfaceMgr = IoCManager.Resolve<IDreamInterfaceManager>();
                    interfaceMgr.SaveScreenshot(split.Length == 1 || split[1] != "auto"); break;
                default: {
                    if (split.Length > 1)
                    {
                        Logger.Error("Verb argument parsing is not implemented yet.");
                        return;
                    }

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
