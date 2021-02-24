using System.Collections.Generic;

namespace OpenDreamShared.Json {
    enum JsonVariableType {
        Resource = 0,
        Null = 1,
        Path = 2
    }

    class DreamObjectJson {
        public string Name { get; set; }
        public string Parent { get; set; }
        public ProcDefinitionJson InitProc { get; set; }
        public Dictionary<string, List<ProcDefinitionJson>> Procs { get; set; }
        public Dictionary<string, object> Variables { get; set; }
        public Dictionary<string, object> GlobalVariables { get; set; }
        public List<DreamObjectJson> Children { get; set; }
    }

    class ProcDefinitionJson {
        public List<string> ArgumentNames { get; set; }
        public byte[] Bytecode { get; set; }
    }
}
