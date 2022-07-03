using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime {
    [JsonConverter(typeof(DreamValueJsonConverter))]
    public struct DreamValue : IEquatable<DreamValue> {
        [Flags]
        public enum DreamValueType {
            String = 1,
            Float = 2,
            DreamResource = 4,
            DreamObject = 8,
            DreamPath = 16,
            DreamProc = 32,
            Reference = 64
        }

        public static readonly DreamValue Null = new DreamValue((DreamObject)null);

        public DreamValueType Type { get; private set; }
        public object Value { get; private set; }

        public DreamValue(String value) {
            Type = DreamValueType.String;
            Value = value;
        }

        public DreamValue(float value) {
            Type = DreamValueType.Float;
            Value = value;
        }

        public DreamValue(int value) : this((float)value) { }

        public DreamValue(UInt32 value) : this((float)value) { }

        public DreamValue(double value) : this((float)value) { }

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
            if (value is int) {
                Value = (float)(int)value;
            } else {
                Value = value;
            }

            Type = value switch {
                string => DreamValueType.String,
                int => DreamValueType.Float,
                float => DreamValueType.Float,
                DreamResource => DreamValueType.DreamResource,
                DreamObject => DreamValueType.DreamObject,
                DreamPath => DreamValueType.DreamPath,
                DreamProc => DreamValueType.DreamProc,
                DreamProcArguments => DreamValueType.Reference,
                _ => throw new ArgumentException("Invalid DreamValue value (" + value + ", " + value.GetType() + ")")
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

        [Obsolete("Deprecated. Use the relevant TryGetValueAs[Type]() instead.")]
        private object GetValueExpectingType(DreamValueType type) {
            if (Type == type) {
                return Value;
            }

            throw new Exception("Value " + this + " was not the expected type of " + type + "");
        }

        [Obsolete("Deprecated. Use TryGetValueAsString() instead.")]
        public string GetValueAsString() {
            return (string)GetValueExpectingType(DreamValueType.String);
        }

        public bool TryGetValueAsString(out string value) {
            if (Type == DreamValueType.String) {
                value = (string)Value;
                return true;
            } else {
                value = null;
                return false;
            }
        }

        //Casts a float value to an integer
        [Obsolete("Deprecated. Use TryGetValueAsInteger() instead.")]
        public int GetValueAsInteger() {
            return (int)(float)GetValueExpectingType(DreamValueType.Float);
        }

        public bool TryGetValueAsInteger(out int value) {
            if (Type == DreamValueType.Float) {
                value = (int)(float)Value;
                return true;
            } else {
                value = 0;
                return false;
            }
        }

        [Obsolete("Deprecated. Use TryGetValueAsFloat() instead.")]
        public float GetValueAsFloat() {
            return (float)GetValueExpectingType(DreamValueType.Float);
        }

        public bool TryGetValueAsFloat(out float value) {
            if (Type == DreamValueType.Float) {
                value = (float)Value;
                return true;
            } else {
                value = 0;
                return false;
            }
        }

        [Obsolete("Deprecated. Use TryGetValueAsDreamResource() instead.")]
        public DreamResource GetValueAsDreamResource() {
            return (DreamResource)GetValueExpectingType(DreamValueType.DreamResource);
        }

        public bool TryGetValueAsDreamResource(out DreamResource value) {
            if (Type == DreamValueType.DreamResource) {
                value = (DreamResource)Value;
                return true;
            } else {
                value = null;
                return false;
            }
        }

        [Obsolete("Deprecated. Use TryGetValueAsDreamObject() instead.")]
        public DreamObject GetValueAsDreamObject() {
            DreamObject dreamObject = (DreamObject)GetValueExpectingType(DreamValueType.DreamObject);

            if (dreamObject?.Deleted == true) {
                Value = null;

                return null;
            } else {
                return dreamObject;
            }
        }

        public bool TryGetValueAsDreamObject(out DreamObject dreamObject) {
            if (Type == DreamValueType.DreamObject) {
                dreamObject = GetValueAsDreamObject();
                return true;
            } else {
                dreamObject = null;
                return false;
            }
        }

        [Obsolete("Deprecated. Use TryGetValueAsDreamObjectOfType() instead.")]
        public DreamObject GetValueAsDreamObjectOfType(DreamPath type) {
            DreamObject value = GetValueAsDreamObject();

            if (value?.IsSubtypeOf(type) == true) {
                return value;
            } else {
                throw new Exception("Value " + this + " was not of type '" + type + "'");
            }
        }

        public bool TryGetValueAsDreamObjectOfType(DreamPath type, out DreamObject dreamObject) {
            return TryGetValueAsDreamObject(out dreamObject) && dreamObject != null && dreamObject.IsSubtypeOf(type);
        }

        [Obsolete("Deprecated. Use TryGetValueAsDreamList() instead.")]
        public DreamList GetValueAsDreamList() {
            return (DreamList)GetValueAsDreamObject();
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

        [Obsolete("Deprecated. Use TryGetValueAsPath() instead.")]
        public DreamPath GetValueAsPath() {
            return (DreamPath)GetValueExpectingType(DreamValueType.DreamPath);
        }

        public bool TryGetValueAsPath(out DreamPath path) {
            if (Type == DreamValueType.DreamPath) {
                path = (DreamPath)Value;

                return true;
            } else {
                path = DreamPath.Root;

                return false;
            }
        }

        public bool TryGetValueAsProc(out DreamProc proc) {
            if (Type == DreamValueType.DreamProc) {
                proc = (DreamProc)Value;

                return true;
            } else {
                proc = null;

                return false;
            }
        }

        public bool IsTruthy() {
            switch (Type) {
                case DreamValue.DreamValueType.DreamObject:
                    return Value != null && ((DreamObject)Value).Deleted == false;
                case DreamValue.DreamValueType.DreamProc:
                    return Value != null;
                case DreamValue.DreamValueType.DreamResource:
                case DreamValue.DreamValueType.DreamPath:
                    return true;
                case DreamValue.DreamValueType.Float:
                    return (float)Value != 0;
                case DreamValue.DreamValueType.String:
                    return (string)Value != "";
                default:
                    throw new NotImplementedException("Truthy evaluation for " + this + " is not implemented");
            }
        }

        public string Stringify() {
            switch (Type) {
                case DreamValueType.String:
                    TryGetValueAsString(out var stringString);
                    return stringString;
                case DreamValueType.Float:
                    TryGetValueAsFloat(out var floatString);
                    return floatString.ToString();
                case DreamValueType.DreamResource:
                    TryGetValueAsDreamResource(out var rscPath);
                    return rscPath.ResourcePath;
                case DreamValueType.DreamPath:
                    TryGetValueAsPath(out var path);
                    return path.PathString;
                case DreamValueType.DreamObject when Value == null:
                    return "";
                case DreamValueType.DreamObject: {
                    TryGetValueAsDreamObject(out var dreamObject);

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

        public override bool Equals(object obj) => obj is DreamValue other && Equals(other);

        public bool Equals(DreamValue other) {
            if (Type != other.Type) return false;
            if (Value == null) return other.Value == null;
            return Value.Equals(other.Value);
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

    public sealed class DreamValueJsonConverter : JsonConverter<DreamValue> {
        public override void Write(Utf8JsonWriter writer, DreamValue value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteNumber("Type", (int)value.Type);

            switch (value.Type) {
                case DreamValue.DreamValueType.String: writer.WriteString("Value", (string)value.Value); break;
                case DreamValue.DreamValueType.Float: writer.WriteNumber("Value", (float)value.Value); break;
                case DreamValue.DreamValueType.DreamObject when value == DreamValue.Null: writer.WriteNull("Value"); break;
                case DreamValue.DreamValueType.DreamObject
                    when value.TryGetValueAsDreamObjectOfType(DreamPath.Icon, out var iconObj):
                {
                    // TODO Check what happens with multiple states
                    var icon = DreamMetaObjectIcon.ObjectToDreamIcon[iconObj];
                    var rscMan = IoCManager.Resolve<DreamResourceManager>();
                    var resource = rscMan.LoadResource(icon.Icon);
                    var base64 = Convert.ToBase64String(resource.ResourceData);
                    writer.WriteString("Value",base64); break;
                }
                default: throw new NotImplementedException("Json serialization for " + value + " is not implemented");
            }

            writer.WriteEndObject();
        }

        public override DreamValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) throw new Exception("Expected StartObject token");
            reader.Read();

            if (reader.GetString() != "Type") throw new Exception("Expected type property");
            reader.Read();
            DreamValue.DreamValueType type = (DreamValue.DreamValueType)reader.GetInt32();
            reader.Read();

            if (reader.GetString() != "Value") throw new Exception("Expected value property");
            reader.Read();

            DreamValue value;
            switch (type) {
                case DreamValue.DreamValueType.String: value = new DreamValue(reader.GetString()); break;
                case DreamValue.DreamValueType.Float: value = new DreamValue((float)reader.GetSingle()); break;
                case DreamValue.DreamValueType.DreamObject when reader.TokenType == JsonTokenType.Null: {
                    if (reader.TokenType == JsonTokenType.Null) {
                        value = DreamValue.Null;
                    } else {
                        throw new NotImplementedException("Json deserialization for DreamObjects are not implemented");
                    }

                    break;
                }
                default: throw new NotImplementedException("Json deserialization for type " + type + " is not implemented");
            }
            reader.Read();

            if (reader.TokenType != JsonTokenType.EndObject) throw new Exception("Expected EndObject token");

            return value;
        }
    }
}
