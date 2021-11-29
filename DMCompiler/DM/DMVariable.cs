using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;

namespace DMCompiler.DM {
    class DMVariable {
        public DreamPath? Type;
        public string Name;
        public bool IsGlobal;
        public DMExpression Value;

        public DMVariable(DreamPath? type, string name, bool isGlobal) {
            Type = type;
            Name = name;
            IsGlobal = isGlobal;
            Value = null;
        }

        public object ToJsonRepresentation() {
            Expressions.Constant value = Value as Expressions.Constant;
            if (value == null) throw new Exception($"Value of {Name} must be a constant");

            return value.ToJsonRepresentation();
        }
    }
}
