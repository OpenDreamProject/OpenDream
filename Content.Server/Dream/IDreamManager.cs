using Robust.Server.Player;

namespace Content.Server.Dream {
    interface IDreamManager {
        public DreamObject c { get; set; }
        public DreamObjectTree ObjectTree { get; }
        public int DMExceptionCount { get; set; }

        public DreamList WorldContentsList { get; set; }

        public void Initialize();
        public void Shutdown();
        public IPlayerSession GetSessionFromClient(DreamObject client);
    }
}
