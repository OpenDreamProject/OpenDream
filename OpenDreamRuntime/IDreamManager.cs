using System.Threading.Tasks;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
using Robust.Server.Player;

namespace OpenDreamRuntime {
    public interface IDreamManager {
        public DreamObjectTree ObjectTree { get; }
        public DreamObject WorldInstance { get; }
        public int DMExceptionCount { get; set; }

        public List<DreamValue> Globals { get; set; }
        public Dictionary<string, DreamProc> GlobalProcs { get; set; }
        public DreamList WorldContentsList { get; set; }
        public Dictionary<DreamObject, DreamList> AreaContents { get; set; }
        public Dictionary<DreamObject, int> ReferenceIDs { get; set; }
        public List<DreamObject> Mobs { get; set; }
        public List<DreamObject> Clients { get; set; }
        public List<DreamObject> Datums { get; set; }
        public Random Random { get; set; }
        public Dictionary<string, List<DreamObject>> Tags { get; set; }

        public void Initialize(string? testingJson);
        public void Shutdown();
        public IPlayerSession GetSessionFromClient(DreamObject client);
        DreamConnection GetConnectionFromClient(DreamObject client);
        public DreamObject GetClientFromMob(DreamObject mob);
        DreamConnection GetConnectionFromMob(DreamObject mob);
        DreamConnection GetConnectionBySession(IPlayerSession session);
        public void Update();

        public void SetGlobalNativeProc(NativeProc.HandlerFn func);
        public void SetGlobalNativeProc(Func<AsyncNativeProc.State, Task<DreamValue>> func);
        public void WriteWorldLog(string message, LogLevel level, string sawmill = "world.log");

        IEnumerable<DreamConnection> Connections { get; }
    }
}
