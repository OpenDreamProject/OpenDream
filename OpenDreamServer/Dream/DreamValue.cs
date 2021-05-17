using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Resources;
using OpenDreamShared.Dream;
using System;

namespace OpenDreamServer.Dream {
    struct DreamValue {
        [Flags]
        public enum DreamValueType {
            String = 1,
            Integer = 2,
            Float = 4,
            Number = Integer | Float,
            DreamResource = 8,
            DreamObject = 16,
            DreamPath = 32,
            DreamProc = 64
        }

        public static readonly DreamValue Null = new DreamValue((DreamObject)null);

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

        public DreamValue(float value) {
            if (Math.Floor(value) == value && value <= Int32.MaxValue && value >= Int32.MinValue) {
                Type = DreamValueType.Integer;
                Value = (Int32)value;
            } else {
                Type = DreamValueType.Float;
                Value = value;
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

        public DreamValue(object value) {
            Value = value;

            Type = value switch {
                string => DreamValueType.String,
                int => DreamValueType.Integer,
                float => DreamValueType.Float,
                DreamResource => DreamValueType.DreamResource,
                DreamObject => DreamValueType.DreamObject,
                DreamPath => DreamValueType.DreamPath,
                DreamProc => DreamValueType.DreamProc,
                _ => throw new ArgumentException("Invalid DreamValue value (" + value + ")")
            };
        }

        public override string ToString() {
            string value;
            if (Value == null) {
                value = "null";
            } else
                value = Type switch {
                    DreamValueType.String => "\"" + Value + "\"",
                    DreamValueType.DreamResource => "'" + ((DreamResource)Value).ResourcePath + "'",
                    _ => Value.ToString()
                };

            return "DreamValue(" + Type + ", " + value + ")";
        }

        public bool IsType(DreamValueType type) {
            return ((int)type & (int)Type) != 0;
        }

        public object GetValueExpectingType(DreamValueType type) {
            if (IsType(type)) {
                return Value;
            }
            throw new Exception("Value " + this + " was not the expected type of " + type + "");
        }

        public string GetValueAsString() {
            return (string)GetValueExpectingType(DreamValueType.String);
        }

        public bool TryGetValueAsString(out string value) {
            if (IsType(DreamValueType.String)) {
                value = (string)Value;
                return true;
            } else {
                value = null;
                return false;
            }
        }

        public int GetValueAsInteger() {
            return (int)GetValueExpectingType(DreamValueType.Integer);
        }

        public bool TryGetValueAsInteger(out int value) {
            if (IsType(DreamValueType.Integer)) {
                value = (int)Value;
                return true;
            } else {
                value = 0;
                return false;
            }
        }

        public float GetValueAsFloat() {
            return (float)GetValueExpectingType(DreamValueType.Float);
        }

        public float GetValueAsNumber() {
            return Convert.ToSingle(GetValueExpectingType(DreamValueType.Integer | DreamValueType.Float));
        }

        public DreamResource GetValueAsDreamResource() {
            return (DreamResource)GetValueExpectingType(DreamValueType.DreamResource);
        }

        public bool TryGetValueAsDreamResource(out DreamResource value) {
            if (IsType(DreamValueType.DreamResource)) {
                value = (DreamResource)Value;
                return true;
            } else {
                value = null;
                return false;
            }
        }

        public DreamObject GetValueAsDreamObject() {
            DreamObject dreamObject = (DreamObject)GetValueExpectingType(DreamValueType.DreamObject);

            if (dreamObject != null && dreamObject.Deleted) {
                Value = null;
            
                return null;
            } else {
                return dreamObject;
            }
        }

        public DreamList GetValueAsDreamList() {
            return (DreamList)GetValueAsDreamObject();
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

        public bool TryGetValueAsDreamList(out DreamList list) {
            if (TryGetValueAsDreamObjectOfType(DreamPath.List, out DreamObject listObject)) {
                list = (DreamList)listObject;

                return true;
            } else {
                list = null;

                return false;
            }
        }

        public DreamPath GetValueAsPath() {
            return (DreamPath)GetValueExpectingType(DreamValueType.DreamPath);
        }

        public bool TryGetValueAsPath(out DreamPath path) {
            if (IsType(DreamValueType.DreamPath)) {
                path = (DreamPath)Value;

                return true;
            } else {
                path = DreamPath.Root;

                return false;
            }
        }

        public DreamProc GetValueAsProc() {
            return (DreamProc)GetValueExpectingType(DreamValueType.DreamProc);
        }

        public bool TryGetValueAsProc(out DreamProc proc) {
            if (IsType(DreamValueType.DreamProc)) {
                proc = (DreamProc)Value;

                return true;
            } else {
                proc = null;

                return false;
            }
        }

        public string Stringify() {
            switch (Type) {
                case DreamValueType.String:
                    return GetValueAsString();
                case DreamValueType.Integer:
                    return GetValueAsInteger().ToString();
                case DreamValueType.Float:
                    return GetValueAsFloat().ToString();
                case DreamValueType.DreamResource:
                    return GetValueAsDreamResource().ResourcePath;
                case DreamValueType.DreamPath:
                    return GetValueAsPath().PathString;
                case DreamValueType.DreamObject when Value == null:
                    return "";
                case DreamValueType.DreamObject: {
                    DreamObject dreamObject = GetValueAsDreamObject();

                    if (dreamObject.IsSubtypeOf(DreamPath.Atom)) {
                        return dreamObject.GetVariable("name").Stringify();
                    } else {
                        return dreamObject.ObjectDefinition.Type.ToString();
                    }
                }
                default:
                    throw new NotImplementedException("Cannot stringify " + this);
            }
        }

        public bool Equals(DreamValue other) {
            return Value == other.Value;
        }

        public override bool Equals(object obj) {
            if (obj is DreamValue b) {
                if (Value == null) return b.Value == null;
                return Value.Equals(b.Value);
            }
            return false;
        }

        public override int GetHashCode() {
            if (Value != null) {
                return Value.GetHashCode();
            }
            return 0;
        }

        public static bool operator ==(DreamValue a, DreamValue b) {
            return a.Equals(b);
        }

        public static bool operator !=(DreamValue a, DreamValue b) {
            return !a.Equals(b);
        }
    }
}
