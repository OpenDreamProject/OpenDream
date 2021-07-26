using Content.Shared.Input;

namespace Content.Client.Input {
    class DreamCommandSystem : SharedDreamCommandSystem {
        public void RunCommand(string command) {
            //TODO: Local client commands (.quit, .winset, .screenshot, etc)
            RaiseNetworkEvent(new CommandEvent(command));
        }
    }
}
