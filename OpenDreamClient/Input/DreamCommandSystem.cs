using OpenDreamShared.Input;
using OpenDreamClient.Interface;
using Robust.Shared.Network;

namespace OpenDreamClient.Input {
    public sealed class DreamCommandSystem : SharedDreamCommandSystem {
        [Dependency] private readonly IDreamInterfaceManager _interfaceManager = default!;

        public void RunCommand(string command) {
            _interfaceManager.RunCommand(command);
        }

        public void SendServerCommand(string command) {
            RaiseNetworkEvent(new CommandEvent(command));
        }

        public void StartRepeatingCommand(string command) {
            RaiseNetworkEvent(new RepeatCommandEvent(command));
        }

        public void StopRepeatingCommand(string command) {
            RaiseNetworkEvent(new StopRepeatCommandEvent(command));
        }
    }
}
