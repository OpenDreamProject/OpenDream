using Content.Server.Dream;

namespace Content.Server {
    class PlayerSessionData {
        public DreamObject Client { get; }

        public PlayerSessionData(DreamObject client) {
            Client = client;
        }
    }
}
