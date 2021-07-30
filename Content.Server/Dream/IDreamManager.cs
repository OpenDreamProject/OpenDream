using Robust.Server.Player;
using System.Collections.Generic;

namespace Content.Server.Dream {
    interface IDreamManager {
        public DreamObjectTree ObjectTree { get; }
        public DreamObject WorldInstance { get; }
        public int DMExceptionCount { get; set; }

        public DreamList WorldContentsList { get; set; }
        public Dictionary<DreamObject, DreamList> AreaContents { get; set; }

        public void Initialize();
        public void Shutdown();
        public IPlayerSession GetSessionFromClient(DreamObject client);
    }
}
