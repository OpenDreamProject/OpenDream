using System;
using System.Collections.Generic;
using OpenDreamRuntime.Objects;
using Robust.Server.Player;

namespace OpenDreamRuntime {
    public interface IDreamManager {
        public DreamObjectTree ObjectTree { get; }
        public DreamObject WorldInstance { get; }
        public int DMExceptionCount { get; set; }

        public DreamList WorldContentsList { get; set; }
        public Dictionary<DreamObject, DreamList> AreaContents { get; set; }
        public Dictionary<DreamObject, int> ReferenceIDs { get; set; }
        List<DreamObject> Mobs { get; set; }
        public Random Random { get; set; }

        public void Initialize();
        public void Shutdown();
        public IPlayerSession GetSessionFromClient(DreamObject client);
        DreamConnection GetConnectionFromClient(DreamObject client);
        public DreamObject GetClientFromMob(DreamObject mob);
        DreamConnection GetConnectionFromMob(DreamObject mob);
        DreamConnection GetConnectionBySession(IPlayerSession session);
        void Update();

        IEnumerable<DreamConnection> Connections { get; }
    }
}
