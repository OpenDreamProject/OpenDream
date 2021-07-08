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

        public object GetJsonRepresentation() {
            switch (Value) {
                case Expressions.Number number: return number.Value;
                case Expressions.String stringValue: return stringValue.Value;
                case Expressions.Null: return null;
                case Expressions.Resource resource: {
                    return new Dictionary<string, object>() {
                        { "type", JsonVariableType.Resource },
                        { "resourcePath", resource.Value }
                    };
                }
                case Expressions.Path path: {
                    return new Dictionary<string, object>() {
                        { "type", JsonVariableType.Path },
                        { "value", path.Value.PathString}
                    };
                }
                default: throw new Exception("Cannot create a json representation of expression");
            }
        }
    }
}
