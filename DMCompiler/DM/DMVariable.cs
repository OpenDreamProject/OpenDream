﻿using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;

namespace DMCompiler.DM {
    class DMVariable {
        public DreamPath? Type;
        public string Name;
        public bool IsGlobal;
        public DMExpression Value;
        public DMASTExpression ValueToEval;

        public DMVariable(DreamPath? type, string name, bool isGlobal) {
            Type = type;
            Name = name;
            IsGlobal = isGlobal;
            Value = null;
        }
    }
}
