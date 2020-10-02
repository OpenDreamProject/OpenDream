using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Resources;
using OpenDreamShared.Dream;
using System;

namespace OpenDreamServer.Dream {
    struct DreamValue {
        public enum DreamValueType {
            String = 1,
            Integer = 2,
            Double = 4,
            DreamResource = 8,
            DreamObject = 16,
            DreamPath = 32,
            DreamProc = 64
        }

        public DreamValueType Type { get; private set; }
        public object Value { get; private set; }

        public DreamValue(String value) {
            Type = DreamValueType.String;
            Value = value;
        }

        public DreamValue(int value) {
            Type = DreamValueType.Integer;
            Value = (Int32)value;
        }

        public DreamValue(UInt32 value) {
            Type = DreamValueType.Integer;
            Value = (Int32)value;
        }

        public DreamValue(double value) {
            if (Math.Floor(value) != value) {
                Type = DreamValueType.Double;
                Value = value;
            } else {
                Type = DreamValueType.Integer;
                Value = (Int32)value;
            }
        }

        public DreamValue(DreamResource value) {
            Type = DreamValueType.DreamResource;
            Value = value;
        }

        public DreamValue(DreamObject value) {
            Type = DreamValueType.DreamObject;
            Value = value;
        }
        
        public DreamValue(DreamPath value) {
            Type = DreamValueType.DreamPath;
            Value = value;
        }

        public DreamValue(DreamProc value) {
            Type = DreamValueType.DreamProc;
            Value = value;
        }

        public override string ToString() {
            string value;
            if (Value == null) {
                value = "null";
            } else if (Type == DreamValueType.String) {
                value = "\"" + Value + "\"";
            } else {
                value = Value.ToString();
            }

            return "DreamValue(" + Type + ", " + value + ")";
        }

        public bool IsType(DreamValueType type) {
            return ((int)type & (int)Type) != 0;
        }

        public object GetValueExpectingType(DreamValueType type) {
            if (IsType(type)) {
                return Value;
            } else {
                throw new Exception("Value " + this + " was not the expected type of " + type + "");
            }
        }

        public string GetValueAsString() {
            return (string)GetValueExpectingType(DreamValueType.String);
        }

        public int GetValueAsInteger() {
            return (int)GetValueExpectingType(DreamValueType.Integer);
        }

        public double GetValueAsDouble() {
            return (double)GetValueExpectingType(DreamValueType.Double);
        }

        public double GetValueAsNumber() {
            return Convert.ToDouble(GetValueExpectingType(DreamValueType.Integer | DreamValueType.Double));
        }

        public DreamResource GetValueAsDreamResource() {
            return (DreamResource)GetValueExpectingType(DreamValueType.DreamResource);
        }

        public DreamObject GetValueAsDreamObject() {
            DreamObject dreamObject = (DreamObject)GetValueExpectingType(DreamValueType.DreamObject);

            //return (dreamObject != null && dreamObject.Deleted) ? null : dreamObject;
            return dreamObject;
        }

        public bool TryGetValueAsDreamObject(out DreamObject dreamObject) {
            if (IsType(DreamValueType.DreamObject)) {
                dreamObject = GetValueAsDreamObject();
                return true;
            } else {
                dreamObject = null;
                return false;
            }
        }

        public DreamObject GetValueAsDreamObjectOfType(DreamPath type) {
            DreamObject value = GetValueAsDreamObject();

            if (value != null && !value.Deleted && value.IsSubtypeOf(type)) {
                return value;
            } else {
                throw new Exception("Value " + this + " was not of type '" + type + "'");
            }
        }

        public bool TryGetValueAsDreamObjectOfType(DreamPath type, out DreamObject dreamObject) {
            return TryGetValueAsDreamObject(out dreamObject) && dreamObject != null && dreamObject.IsSubtypeOf(type);
        }

        public DreamPath GetValueAsPath() {
            return (DreamPath)GetValueExpectingType(DreamValueType.DreamPath);
        }

        public DreamProc GetValueAsProc() {
            return (DreamProc)GetValueExpectingType(DreamValueType.DreamProc);
        }

        public string Stringify() {
            if (Type == DreamValueType.String) {
                return GetValueAsString();
            } else if (Type == DreamValueType.Integer) {
                return GetValueAsInteger().ToString();
            } else if (Type == DreamValueType.Double) {
                return GetValueAsDouble().ToString();
            } else if (Type == DreamValueType.DreamPath) {
                return GetValueAsPath().PathString;
            } else if (Type == DreamValueType.DreamObject) {
                return GetValueAsDreamObject().ObjectDefinition.Type.ToString();
            } else {
                throw new NotImplementedException("Cannot stringify " + this);
            }
        }

        public override bool Equals(object obj) {
            if (obj is DreamValue) {
                DreamValue b = (DreamValue)obj;

                if (Type == DreamValueType.DreamPath && b.Type == DreamValueType.DreamPath) {
                    return GetValueAsPath().Equals(b.GetValueAsPath());
                } else if (Value != null) {
                    return Value.Equals(b.Value);
                } else {
                    return b.Value == null;
                }
            } else {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode() {
            return Value.GetHashCode();
        }

        public static bool operator ==(DreamValue a, DreamValue b) {
            return a.Equals(b);
        }

        public static bool operator !=(DreamValue a, DreamValue b) {
            return !a.Equals(b);
        }
    }
}
