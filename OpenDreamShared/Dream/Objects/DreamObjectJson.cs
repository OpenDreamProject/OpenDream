using System;
using System.Collections.Generic;

namespace OpenDreamShared.Dream.Objects {
    enum JsonVariableType {
        Resource = 0,
        Object = 1,
        Path = 2,
        List = 3
    }

    class DreamObjectJson {
        public string Name { get; set; }
        public string Parent { get; set; }
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
