using System.Collections.Generic;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamShared.Json {
    public enum JsonVariableType {
        Resource = 0,
        Path = 1,
        List = 2
    }

    public class DreamTypeJson {
        public string Path { get; set; }
        public int? Parent { get; set; }
        public ProcDefinitionJson InitProc { get; set; }
        public Dictionary<string, List<ProcDefinitionJson>> Procs { get; set; }
        public Dictionary<string, object> Variables { get; set; }
        public Dictionary<string, int> GlobalVariables { get; set; }
        public List<int> Children { get; set; }
    }

    public class GlobalListJson
    {
        public int GlobalCount { get; set; }
        public Dictionary<int, object> Globals { get; set; }
    }

    public class ProcDefinitionJson {
        public bool? WaitFor { get; set; }
        public int MaxStackSize { get; set; }
        public List<ProcArgumentJson> Arguments { get; set; }
        public byte[] Bytecode { get; set; }
    }

    public class ProcArgumentJson {
        public string Name { get; set; }
        public DMValueType Type { get; set; }
    }
}
