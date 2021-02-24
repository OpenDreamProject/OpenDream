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
        public DMProc InitializationProc = null;

        public DMObject(UInt32 id, DreamPath path, DreamPath? parent) {
            Id = id;
            Path = path;
            Parent = parent;
        }

        public DMProc CreateInitializationProc() {
            if (InitializationProc == null) {
                InitializationProc = new DMProc();

                InitializationProc.PushSuperProc();
                InitializationProc.JumpIfFalse("no_super");

                InitializationProc.PushSuperProc();
                InitializationProc.PushArguments(0);
                InitializationProc.Call();

                InitializationProc.AddLabel("no_super");
                InitializationProc.ResolveLabels();
            }

            return InitializationProc;
        }
    }
}
