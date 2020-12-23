using System;
using System.Collections.Generic;
using System.Text.Json;

namespace OpenDreamShared.Dream.Objects {
    public enum DreamObjectJsonVariableType {
        Resource = 0,
        Object = 1,
        Path = 2,
        List = 3
    }

    public class DreamObjectJson {
        public string Name { get; set; }
        public string Parent { get; set; }
        public Dictionary<string, List<ProcDefinitionJson>> Procs { get; set; }
        public Dictionary<string, object> Variables { get; set; }
        public Dictionary<string, object> GlobalVariables { get; set; }
        public List<DreamObjectJson> Children { get; set; }
    }

    public class ProcDefinitionJson {
        public List<string> ArgumentNames { get; set; }
        public byte[] Bytecode { get; set; }
    }
}
