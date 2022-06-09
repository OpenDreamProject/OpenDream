using System.Collections.Generic;

namespace OpenDreamShared.Json {
    public sealed class DreamCompiledJson {
        public List<string> Strings { get; set; }
        public List<int> GlobalProcs { get; set; }
        public GlobalListJson Globals { get; set; }
        public ProcDefinitionJson GlobalInitProc { get; set; }
        public List<DreamMapJson> Maps { get; set; }
        public string Interface { get; set; }
        public DreamTypeJson[] Types { get; set; }
        public ProcDefinitionJson[] Procs { get; set; }
    }
}
