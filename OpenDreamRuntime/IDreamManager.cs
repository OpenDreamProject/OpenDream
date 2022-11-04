using OpenDreamRuntime.Objects;
using Robust.Server.Player;
using Robust.Shared.Timing;

namespace OpenDreamRuntime {
    public interface IDreamManager {
        public bool Initialized { get; }
        public GameTick InitializedTick { get; }
        public DreamObjectTree ObjectTree { get; }
        public DreamObject WorldInstance { get; }

        /// <summary>
        /// A black box (as in, on an airplane) variable currently only used by the test suite to help harvest runtime error info.
        /// </summary>
        public Exception? LastDMException { get; set; }

        public List<DreamValue> Globals { get; }
        public IReadOnlyList<string> GlobalNames { get; }
        public DreamList WorldContentsList { get; }
        public Dictionary<DreamObject, DreamList> AreaContents { get; set; }
        public Dictionary<DreamObject, int> ReferenceIDs { get; set; }
        public List<DreamObject> Mobs { get; set; }
        public List<DreamObject> Clients { get; set; }
        public List<DreamObject> Datums { get; set; }
        public Random Random { get; set; }
        public Dictionary<string, List<DreamObject>> Tags { get; set; }

        public void PreInitialize(string? testingJson);
        public void StartWorld();
        public void Shutdown();
        public bool LoadJson(string? jsonPath);
        public IPlayerSession GetSessionFromClient(DreamObject client);
        DreamConnection? GetConnectionFromClient(DreamObject client);
        public DreamObject GetClientFromMob(DreamObject mob);
        DreamConnection GetConnectionFromMob(DreamObject mob);
        DreamConnection GetConnectionBySession(IPlayerSession session);
        public void Update();

        public void WriteWorldLog(string message, LogLevel level, string sawmill = "world.log");

        public string CreateRef(DreamValue value);
        public DreamValue LocateRef(string refString);

        IEnumerable<DreamConnection> Connections { get; }
    }

    // TODO: Could probably use DreamValueType instead
    public enum RefType : int {
        Null = 0,
        DreamObject = 1,
        String = 2,
        DreamPath = 3,
        DreamResource = 4
    }
}
