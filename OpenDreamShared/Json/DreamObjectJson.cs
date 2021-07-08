using OpenDreamShared.Dream.Procs;
using System.Collections.Generic;

namespace OpenDreamShared.Json {
    public enum JsonVariableType {
        Resource = 0,
        Path = 1
    }

    public class DreamObjectJson {
        public string Name { get; set; }
        public string Parent { get; set; }
        public ProcDefinitionJson InitProc { get; set; }
        public Dictionary<string, List<ProcDefinitionJson>> Procs { get; set; }
        public Dictionary<string, object> Variables { get; set; }
        public Dictionary<string, object> GlobalVariables { get; set; }
        public List<DreamObjectJson> Children { get; set; }
    }

    public class ProcDefinitionJson {
        public bool WaitFor { get; set; }
        public List<ProcArgumentJson> Arguments { get; set; }
        public byte[] Bytecode { get; set; }
    }

    public class ProcArgumentJson {
        public string Name { get; set; }
        public DMValueType Type { get; set; }
    }
}
