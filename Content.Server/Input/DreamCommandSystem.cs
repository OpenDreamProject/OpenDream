using Content.Server.Dream;
using Content.Shared.Input;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using System.Collections.Generic;
using Robust.Shared.IoC;

namespace Content.Server.Input {
    class DreamCommandSystem : SharedDreamCommandSystem
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

        private void RunCommand(string command, IPlayerSession session) {
            var client = _dreamManager.GetConnectionBySession(session).ClientDreamObject;

            switch (command) {
                //TODO: Maybe move these verbs to DM code?
                case ".north": client.SpawnProc("North"); break;
                case ".east": client.SpawnProc("East"); break;
                case ".south": client.SpawnProc("South"); break;
                case ".west": client.SpawnProc("West"); break;
                case ".northeast": client.SpawnProc("Northeast"); break;
                case ".southeast": client.SpawnProc("Southeast"); break;
                case ".southwest": client.SpawnProc("Southwest"); break;
                case ".northwest": client.SpawnProc("Northwest"); break;
                case ".center": client.SpawnProc("Center"); break;

                default: {
                    //TODO: verbs

                    break;
                }
            }
        }
    }
}
