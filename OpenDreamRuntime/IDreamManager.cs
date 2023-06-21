using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using Robust.Server.Player;
using Robust.Shared.Timing;

namespace OpenDreamRuntime {
    public interface IDreamManager {
        public bool Initialized { get; }
        public GameTick InitializedTick { get; }
        public DreamObjectWorld WorldInstance { get; }

        /// <summary>
        /// A black box (as in, on an airplane) variable currently only used by the test suite to help harvest runtime error info.
        /// </summary>
        public Exception? LastDMException { get; set; }
        public event EventHandler<Exception> OnException;

        public List<DreamValue> Globals { get; }
        public IReadOnlyList<string> GlobalNames { get; }
        public Dictionary<DreamObject, int> ReferenceIDs { get; }
        public Dictionary<int, DreamObject> ReferenceIDsToDreamObject { get; }
        public HashSet<DreamObject> Clients { get; set; }
        public HashSet<DreamObject> Datums { get; set; }
        public Random Random { get; set; }
        public Dictionary<string, List<DreamObject>> Tags { get; set; }
        IEnumerable<DreamConnection> Connections { get; }

        public void PreInitialize(string? testingJson);
        public void StartWorld();
        public void Shutdown();
        public bool LoadJson(string? jsonPath);
        public void Update();

        public void WriteWorldLog(string message, LogLevel level, string sawmill = "world.log");

        public string CreateRef(DreamValue value);
        public DreamValue LocateRef(string refString);

        public DreamConnection GetConnectionBySession(IPlayerSession session);

        public void HandleException(Exception e);
    }

    // TODO: Could probably use DreamValueType instead
    public enum RefType : int {
        Null = 0,
        DreamObject = 1,
        String = 2,
        DreamType = 3,
        DreamResource = 4,
        DreamAppearance = 5,
        Proc = 6
    }
}
