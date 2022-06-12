using System;
using OpenDreamShared.Dream.Procs;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace OpenDreamShared.Json {
    public sealed class ProcDefinitionJson {
        public string Name { get; set; }
        public int MaxStackSize { get; set; }
        public List<ProcArgumentJson> Arguments { get; set; }
        public ProcAttributes Attributes { get; set; } = ProcAttributes.None;
        public byte[] Bytecode { get; set; }

        [CanBeNull] public string VerbName { get; set; }
        [CanBeNull] public string VerbCategory { get; set; } = null;
        [CanBeNull] public string VerbDesc { get; set; }
        public sbyte? Invisibility { get; set; }
    }

    public sealed class ProcArgumentJson {
        public string Name { get; set; }
        public DMValueType Type { get; set; }
    }
}
