using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;

namespace DMCompiler.DM {
    class DMObject {
        public UInt32 Id;
        public DreamPath Path;
        public DreamPath? Parent;
        public Dictionary<string, List<DMProc>> Procs = new();
        public Dictionary<string, DMVariable> Variables = new();
        public Dictionary<string, DMVariable> GlobalVariables = new();

        public DMObject(UInt32 id, DreamPath path, DreamPath? parent) {
            Id = id;
            Path = path;
            Parent = parent;
        }
    }
}
