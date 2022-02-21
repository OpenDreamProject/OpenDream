using OpenDreamShared.Dream.Procs;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace OpenDreamShared.Json {
    public class ProcDefinitionJson {
        public int MaxStackSize { get; set; }
        public List<ProcArgumentJson> Arguments { get; set; }
        public ProcAttributes Attributes { get; set; }
        public byte[] Bytecode { get; set; }

        [CanBeNull] public string VerbName { get; set; }
        [CanBeNull] public string VerbCategory { get; set; }
        [CanBeNull] public string VerbDesc { get; set; }
        public sbyte? Invisibility { get; set; }
    }

    public class ProcArgumentJson {
        public string Name { get; set; }
        public DMValueType Type { get; set; }
    }
}
