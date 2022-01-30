using OpenDreamShared.Dream.Procs;
using System.Collections.Generic;

namespace OpenDreamShared.Json {
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
