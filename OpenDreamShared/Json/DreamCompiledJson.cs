using System.Collections.Generic;

namespace OpenDreamShared.Json {
    public class DreamCompiledJson {
        public List<string> Strings { get; set; }
        public List<object> Globals { get; set; }
        public List<ProcDefinitionJson> GlobalProcs { get; set; }
        public List<KeyValuePair<string,int>> InternalNameToGlobalProcId { get; set; }
        public ProcDefinitionJson GlobalInitProc { get; set; }
        public List<DreamMapJson> Maps { get; set; }
        public string Interface { get; set; }
        public DreamTypeJson[] Types { get; set; }
    }
}
