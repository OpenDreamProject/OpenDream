using Content.Server.Dream;
using Content.Shared.Input;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Content.Server.Input {
    class DreamCommandSystem : SharedDreamCommandSystem {
        public override void Initialize() {
            SubscribeNetworkEvent<CommandEvent>(OnCommandEvent);
        }

        private void OnCommandEvent(CommandEvent e, EntitySessionEventArgs sessionEvent) {
            IPlayerSession session = (IPlayerSession)sessionEvent.SenderSession;
            PlayerSessionData sessionData = (PlayerSessionData)session.Data.ContentDataUncast;
            DreamObject client = sessionData.Client;
            string command = e.Command;

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
