using OpenDreamShared.Input;
using Robust.Server.Player;

namespace OpenDreamRuntime.Input {
    sealed class DreamCommandSystem : SharedDreamCommandSystem
    {
        [Dependency] private readonly IDreamManager _dreamManager;

        private List<(string Command, IPlayerSession session)> _repeatingCommands = new();

        public override void Initialize() {
            SubscribeNetworkEvent<CommandEvent>(OnCommandEvent);
            SubscribeNetworkEvent<RepeatCommandEvent>(OnRepeatCommandEvent);
            SubscribeNetworkEvent<StopRepeatCommandEvent>(OnStopRepeatCommandEvent);
        }

        public void RunRepeatingCommands() {
            foreach (var (command, session) in _repeatingCommands) {
                RunCommand(command, session);
            }
        }

        private void OnCommandEvent(CommandEvent e, EntitySessionEventArgs sessionEvent) {
            RunCommand(e.Command, (IPlayerSession)sessionEvent.SenderSession);
        }

        private void OnRepeatCommandEvent(RepeatCommandEvent e, EntitySessionEventArgs sessionEvent) {
            var tuple = (e.Command, (IPlayerSession)sessionEvent.SenderSession);

            _repeatingCommands.Add(tuple);
        }

        private void OnStopRepeatCommandEvent(StopRepeatCommandEvent e, EntitySessionEventArgs sessionEvent) {
            var tuple = (e.Command, (IPlayerSession)sessionEvent.SenderSession);

            _repeatingCommands.Remove(tuple);
        }

        private void RunCommand(string command, IPlayerSession session)
        {
            var connection = _dreamManager.GetConnectionBySession(session);
            connection.HandleCommand(command);
        }
    }
}
