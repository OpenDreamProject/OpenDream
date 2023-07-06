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
        public List<string> GlobalNames { get; }
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
    public enum RefType : uint {
        Null = 0x0,
        DreamObjectTurf = 0x1000000,
        DreamObject = 0x2000000,
        DreamObjectMob = 0x3000000,
        DreamObjectArea = 0x4000000,
        DreamObjectClient = 0x5000000,
        DreamObjectImage = 0xD000000,
        DreamObjectList = 0xF000000,
        DreamObjectDatum = 0x21000000,
        String = 0x6000000,
        DreamType = 0x9000000, //in byond type is from 0x8 to 0xb, but fuck that
        DreamResource = 0x27000000, //Equivalent to file
        DreamAppearance = 0x3A000000,
        Proc = 0x26000000
    }
}
