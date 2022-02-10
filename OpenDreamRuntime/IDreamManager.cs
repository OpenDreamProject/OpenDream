using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using Robust.Server.Player;
using Robust.Shared.Log;

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
        List<DreamObject> Mobs { get; set; }
        public Random Random { get; set; }

        public void Initialize();
        public void Shutdown();
        public IPlayerSession GetSessionFromClient(DreamObject client);
        DreamConnection GetConnectionFromClient(DreamObject client);
        public DreamObject GetClientFromMob(DreamObject mob);
        DreamConnection GetConnectionFromMob(DreamObject mob);
        DreamConnection GetConnectionBySession(IPlayerSession session);
        public void Update();

        public void SetGlobalNativeProc(NativeProc.HandlerFn func);
        public void SetGlobalNativeProc(Func<AsyncNativeProc.State, Task<DreamValue>> func);
        public void WriteWorldLog(string message, LogLevel level);

        IEnumerable<DreamConnection> Connections { get; }
    }
}
