using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamRuntime.Procs;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using OpenDreamRuntime.Procs.Native;

namespace OpenDreamRuntime {
    [JsonConverter(typeof(DreamValueJsonConverter))]
    public struct DreamValue : IEquatable<DreamValue> {
        [Flags]
        public enum DreamValueType {
            String = 1,
            Float = 2,
            DreamResource = 4,
            DreamObject = 8,
            DreamType = 16,
            DreamProc = 32,
            ProcArguments = 64,

            // Special types for representing /datum/proc paths
            ProcStub = 128,
            VerbStub = 256
        }

        public static readonly DreamValue Null = new DreamValue((DreamObject?)null);
        public static DreamValue True => new DreamValue(1f);
        public static DreamValue False => new DreamValue(0f);

        public DreamValueType Type { get; private init; }

        private object? _refValue;
        private readonly float _floatValue;

        public DreamValue(String value) {
            Type = DreamValueType.String;
            _refValue = value;
        }

        public DreamValue(float value) {
            Type = DreamValueType.Float;
            _floatValue = value;
        }

        public DreamValue(int value) : this((float)value) { }

        public DreamValue(UInt32 value) : this((float)value) { }

        public DreamValue(double value) : this((float)value) { }

        public DreamValue(DreamResource value) {
            Type = DreamValueType.DreamResource;
            _refValue = value;
        }

        /// <remarks> This constructor is also how one creates nulls. </remarks>
        public DreamValue(DreamObject? value) {
            Type = DreamValueType.DreamObject;
            _refValue = value;
        }

        public DreamValue(IDreamObjectTree.TreeEntry value) {
            Type = DreamValueType.DreamType;
            _refValue = value;
        }

        public DreamValue(DreamProc value) {
            Type = DreamValueType.DreamProc;
            _refValue = value;
        }

        public DreamValue(DreamProcArguments value) {
            Type = DreamValueType.ProcArguments;
            _refValue = value; // TODO: Remove this boxing. DreamValue probably shouldn't be holding proc args in the first place.
        }

        public DreamValue(object value) {
            if (value is int intValue) {
                _floatValue = intValue;
            } else if (value is float floatValue) {
                _floatValue = floatValue;
            } else {
                _refValue = value;
            }

            Type = value switch {
                string => DreamValueType.String,
                int => DreamValueType.Float,
                float => DreamValueType.Float,
                DreamResource => DreamValueType.DreamResource,
                DreamObject => DreamValueType.DreamObject,
                IDreamObjectTree.TreeEntry => DreamValueType.DreamType,
                DreamProc => DreamValueType.DreamProc,
                DreamProcArguments => DreamValueType.ProcArguments,
                _ => throw new ArgumentException($"Invalid DreamValue value ({value}, {value.GetType()})")
            };
        }

        public static DreamValue CreateProcStub(IDreamObjectTree.TreeEntry type) {
            return new DreamValue {
                Type = DreamValueType.ProcStub,
                _refValue = type
            };
        }

        public static DreamValue CreateVerbStub(IDreamObjectTree.TreeEntry type) {
            return new DreamValue {
                Type = DreamValueType.VerbStub,
                _refValue = type
            };
        }

        public override string ToString() {
            if (Type == DreamValueType.Float)
                return _floatValue.ToString();
            else if (_refValue == null) {
                return "null";
            } else if (Type == DreamValueType.String) {
                return $"\"{_refValue}\"";
            } else {
                return _refValue.ToString() ?? "<ToString() = null>";
            }
        }

        [Obsolete("Deprecated. Use TryGetValueAsString() or MustGetValueAsString() instead.")]
        public string GetValueAsString() {
            return MustGetValueAsString();
        }

        public bool TryGetValueAsString([NotNullWhen(true)] out string? value) {
            if (Type == DreamValueType.String) {
                value = (string)_refValue;
                return true;
            } else {
                value = null;
                return false;
            }
        }

        public string MustGetValueAsString() {
            try {
                return (string)_refValue;
            } catch (InvalidCastException) {
                throw new InvalidCastException("Value " + this + " was not the expected type of string");
            }
        }

        //Casts a float value to an integer
        [Obsolete("Deprecated. Use TryGetValueAsInteger() or MustGetValueAsInteger() instead.")]
        public int GetValueAsInteger() {
            return MustGetValueAsInteger();
        }

        public bool TryGetValueAsInteger(out int value) {
            if (Type == DreamValueType.Float) {
                value = (int)_floatValue;
                return true;
            } else {
                value = 0;
                return false;
            }
        }

        public int MustGetValueAsInteger() {
            try {
                return (int)_floatValue;
            } catch (InvalidCastException) {
                throw new InvalidCastException($"Value {this} was not the expected type of integer");
            }
        }

        [Obsolete("Deprecated. Use TryGetValueAsFloat() or MustGetValueAsFloat() instead.")]
        public float GetValueAsFloat() {
            return MustGetValueAsFloat();
        }

        public bool TryGetValueAsFloat(out float value) {
            if (Type == DreamValueType.Float) {
                value = _floatValue;
                return true;
            } else {
                value = 0;
                return false;
            }
        }

        public float MustGetValueAsFloat() {
            if (Type != DreamValueType.Float)
                throw new InvalidCastException($"Value {this} was not the expected type of float");

            return _floatValue;
        }

        public bool TryGetValueAsDreamResource([NotNullWhen(true)] out DreamResource? value) {
            if (Type == DreamValueType.DreamResource) {
                value = (DreamResource)_refValue;
                return true;
            } else {
                value = null;
                return false;
            }
        }

        public DreamResource MustGetValueAsDreamResource() {
            try {
                return (DreamResource)_refValue;
            } catch (InvalidCastException) {
                throw new InvalidCastException("Value " + this + " was not the expected type of DreamResource");
            }
        }

        [Obsolete("Deprecated. Use TryGetValueAsDreamObject() or MustGetValueAsDreamObject() instead.")]
        public DreamObject? GetValueAsDreamObject() {
            DreamObject? dreamObject = MustGetValueAsDreamObject();

            if (dreamObject?.Deleted == true) {
                _refValue = null;

                return null;
            } else {
                return dreamObject;
            }
        }

        public bool TryGetValueAsDreamObject(out DreamObject? dreamObject) {
            if (Type == DreamValueType.DreamObject) {
                dreamObject = MustGetValueAsDreamObject();
                return true;
            } else {
                dreamObject = null;
                return false;
            }
        }

        public DreamObject? MustGetValueAsDreamObject() {
            try {
                DreamObject? dreamObject = (DreamObject?) _refValue;
                if (dreamObject?.Deleted == true) {
                    _refValue = null;
                    return null;
                }

                return dreamObject;
            } catch (InvalidCastException) {
                throw new InvalidCastException($"Value {this} was not the expected type of DreamObject");
            }
        }

        public bool TryGetValueAsDreamObjectOfType(IDreamObjectTree.TreeEntry type, [NotNullWhen(true)] out DreamObject? dreamObject) {
            return TryGetValueAsDreamObject(out dreamObject) && dreamObject != null && dreamObject.IsSubtypeOf(type);
        }

        [Obsolete("Deprecated. Use TryGetValueAsDreamList() or MustGetValueAsDreamList() instead.")]
        public DreamList GetValueAsDreamList() {
            return MustGetValueAsDreamList();
        }

        public bool TryGetValueAsDreamList([NotNullWhen(true)] out DreamList? list) {
            if (TryGetValueAsDreamObject(out var obj) && obj is DreamList listObject) {
                list = listObject;

                return true;
            } else {
                list = null;

                return false;
            }
        }

        public DreamList MustGetValueAsDreamList() {
            try {
                return (DreamList)_refValue;
            } catch (InvalidCastException) {
                throw new InvalidCastException("Value " + this + " was not the expected type of DreamList");
            }
        }

        public bool TryGetValueAsType(out IDreamObjectTree.TreeEntry type) {
            if (Type == DreamValueType.DreamType) {
                type = (IDreamObjectTree.TreeEntry)_refValue;

                return true;
            } else {
                type = null;

                return false;
            }
        }

        public IDreamObjectTree.TreeEntry MustGetValueAsType() {
            if (Type != DreamValueType.DreamType) // Could be a proc or verb stub, they hold they same value
                throw new InvalidCastException($"Value {this} was not the expected type of DreamPath");

            return (IDreamObjectTree.TreeEntry)_refValue;
        }

        public bool TryGetValueAsProc(out DreamProc proc) {
            if (Type == DreamValueType.DreamProc) {
                proc = (DreamProc)_refValue;

                return true;
            } else {
                proc = null;

                return false;
            }
        }

        public DreamProc MustGetValueAsProc() {
            try {
                return (DreamProc)_refValue;
            } catch (InvalidCastException) {
                throw new InvalidCastException("Value " + this + " was not the expected type of DreamProc");
            }
        }

        public bool TryGetValueAsProcStub(out IDreamObjectTree.TreeEntry type) {
            if (Type == DreamValueType.ProcStub) {
                type = (IDreamObjectTree.TreeEntry) _refValue;

                return true;
            } else {
                type = null;

                return false;
            }
        }

        public bool TryGetValueAsVerbStub(out IDreamObjectTree.TreeEntry type) {
            if (Type == DreamValueType.VerbStub) {
                type = (IDreamObjectTree.TreeEntry) _refValue;

                return true;
            } else {
                type = null;

                return false;
            }
        }

        public bool TryGetValueAsProcArguments(out DreamProcArguments args) {
            if (Type == DreamValueType.ProcArguments) {
                args = (DreamProcArguments)_refValue;

                return true;
            }

            args = default;
            return false;
        }

        public DreamProcArguments MustGetValueAsProcArguments() {
            try {
                return (DreamProcArguments) _refValue;
            } catch (InvalidCastException) {
                throw new InvalidCastException($"Value {this} was not the expected type of ProcArguments");
            }
        }

        public bool IsTruthy() {
            switch (Type) {
                case DreamValue.DreamValueType.DreamObject:
                    return _refValue != null && ((DreamObject)_refValue).Deleted == false;
                case DreamValue.DreamValueType.DreamResource:
                case DreamValue.DreamValueType.DreamType:
                case DreamValue.DreamValueType.DreamProc:
                case DreamValue.DreamValueType.ProcStub:
                case DreamValue.DreamValueType.VerbStub:
                    return true;
                case DreamValue.DreamValueType.Float:
                    return _floatValue != 0;
                case DreamValue.DreamValueType.String:
                    return (string)_refValue != "";
                default:
                    throw new NotImplementedException($"Truthy evaluation for {this} is not implemented");
            }
        }

        public string Stringify() {
            switch (Type) {
                case DreamValueType.String:
                    TryGetValueAsString(out var stringString);
                    return stringString;
                case DreamValueType.Float:
                    return _floatValue.ToString();
                case DreamValueType.DreamResource:
                    TryGetValueAsDreamResource(out var rscPath);
                    return rscPath.ResourcePath;
                case DreamValueType.DreamType:
                    TryGetValueAsType(out var type);
                    return type.Path.PathString;
                case DreamValueType.DreamProc:
                    var proc = MustGetValueAsProc();

                    return proc.ToString();
                case DreamValueType.ProcStub:
                case DreamValueType.VerbStub:
                    var owner = (IDreamObjectTree.TreeEntry) _refValue;
                    var lastElement = (Type == DreamValueType.ProcStub) ? "/proc" : "/verb";

                    return $"{owner.Path}{lastElement}";
                case DreamValueType.DreamObject: {
                    if (TryGetValueAsDreamObject(out var dreamObject) && dreamObject != null) {
                        return dreamObject.GetDisplayName();
                    }

                    return String.Empty;
                }
                default:
                    throw new NotImplementedException("Cannot stringify " + this);
            }
        }

        public override bool Equals(object? obj) => obj is DreamValue other && Equals(other);

        public bool Equals(DreamValue other) {
            if (Type != other.Type) return false;
            if (Type == DreamValueType.Float) return _floatValue == other._floatValue;
            if (_refValue == null) return other._refValue == null;
            return _refValue.Equals(other._refValue);
        }

        public override int GetHashCode() {
            if (_refValue != null) {
                return _refValue.GetHashCode();
            }

            return _floatValue.GetHashCode();
        }

        public static bool operator ==(DreamValue a, DreamValue b) {
            return a.Equals(b);
        }

        public static bool operator !=(DreamValue a, DreamValue b) {
            return !a.Equals(b);
        }
    }

    #region Serialization
    public sealed class DreamValueJsonConverter : JsonConverter<DreamValue> {
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;
        [Dependency] private readonly DreamResourceManager _resourceManager = default!;

        public DreamValueJsonConverter() {
            IoCManager.InjectDependencies(this);
        }

        public override void Write(Utf8JsonWriter writer, DreamValue value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteNumber("Type", (int)value.Type);

            switch (value.Type) {
                case DreamValue.DreamValueType.String: writer.WriteString("Value", value.MustGetValueAsString()); break;
                case DreamValue.DreamValueType.Float: writer.WriteNumber("Value", value.MustGetValueAsFloat()); break;
                case DreamValue.DreamValueType.DreamObject: {
                    var dreamObject = value.MustGetValueAsDreamObject();

                    if (dreamObject == null) {
                        writer.WriteNull("Value");
                    } else {
                        writer.WriteString("Value", dreamObject.ObjectDefinition.Type.PathString);

                        if (!dreamObject.IsSubtypeOf(_objectTree.Icon)) {
                            throw new NotImplementedException($"Json serialization for {value} is not implemented");
                        }

                        // TODO Check what happens with multiple states
                        var icon = DreamMetaObjectIcon.ObjectToDreamIcon[dreamObject];
                        var resource = icon.GenerateDMI();
                        var base64 = Convert.ToBase64String(resource.ResourceData);
                        writer.WriteString("icon-data", base64);
                    }

                    break;
                }
                default: throw new NotImplementedException($"Json serialization for {value} is not implemented");
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
                case DreamValue.DreamValueType.DreamObject: {
                    string? objectTypePath = reader.GetString();

                    if (objectTypePath == null) {
                        value = DreamValue.Null;
                    } else {
                        var objectDef = _objectTree.GetTreeEntry(new DreamPath(objectTypePath)).ObjectDefinition;
                        if (!objectDef.IsSubtypeOf(_objectTree.Icon)) {
                            throw new NotImplementedException($"Json deserialization for type {objectTypePath} is not implemented");
                        }

                        reader.Read();
                        if (reader.GetString() != "icon-data") throw new Exception("Expected icon-data property");
                        reader.Read();

                        string? iconDataBase64 = reader.GetString();
                        if (iconDataBase64 == null) throw new Exception("Expected a string for icon-data");

                        byte[] iconData = Convert.FromBase64String(iconDataBase64);
                        IconResource resource = _resourceManager.CreateIconResource(iconData);
                        DreamObject iconObj = _objectTree.CreateObject(_objectTree.Icon);
                        DreamIcon icon = DreamMetaObjectIcon.InitializeIcon(_resourceManager, iconObj);

                        icon.InsertStates(resource, DreamValue.Null, DreamValue.Null, DreamValue.Null);
                        value = new DreamValue(iconObj);
                    }

                    break;
                }
                default: throw new NotImplementedException($"Json deserialization for type {type} is not implemented");
            }
            reader.Read();

            if (reader.TokenType != JsonTokenType.EndObject) throw new Exception("Expected EndObject token");

            return value;
        }
    }

    // The following allows for serializing using DreamValues with ISerializationManager
    // Currently only implemented to the point that they can be used for DreamFilters

    public sealed class DreamValueDataNode : DataNode<DreamValueDataNode>, IEquatable<DreamValueDataNode> {
        public DreamValueDataNode(DreamValue value) : base(NodeMark.Invalid, NodeMark.Invalid) {
            Value = value;
        }

        public DreamValue Value { get; set; }
        public override bool IsEmpty => false;

        public override DreamValueDataNode Copy() {
            return new DreamValueDataNode(Value) {Tag = Tag, Start = Start, End = End};
        }

        public override DreamValueDataNode? Except(DreamValueDataNode node) {
            return Value == node.Value ? null : Copy();
        }

        public override DreamValueDataNode PushInheritance(DreamValueDataNode node) {
            return Copy();
        }

        public bool Equals(DreamValueDataNode? other) {
            return Value == other?.Value;
        }
    }

    [TypeSerializer]
    public sealed class DreamValueStringSerializer : ITypeReader<string, DreamValueDataNode> {
        public string Read(ISerializationManager serializationManager,
            DreamValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<string>? instanceProvider = null) {
            if (!node.Value.TryGetValueAsString(out var strValue))
                throw new Exception($"Value {node.Value} was not a string");

            return strValue;
        }

        public ValidationNode Validate(ISerializationManager serializationManager, DreamValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null) {
            if (node.Value.TryGetValueAsString(out _))
                return new ValidatedValueNode(node);

            return new ErrorNode(node, $"Value {node.Value} is not a string");
        }
    }

    [TypeSerializer]
    public sealed class DreamValueFloatSerializer : ITypeReader<float, DreamValueDataNode> {
        public float Read(ISerializationManager serializationManager,
            DreamValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<float>? instanceProvider = null) {
            if (!node.Value.TryGetValueAsFloat(out var floatValue))
                throw new Exception($"Value {node.Value} was not a float");

            return floatValue;
        }

        public ValidationNode Validate(ISerializationManager serializationManager,
            DreamValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null) {
            if (node.Value.TryGetValueAsFloat(out _))
                return new ValidatedValueNode(node);

            return new ErrorNode(node, $"Value {node.Value} is not a float");
        }
    }

    [TypeSerializer]
    public sealed class DreamValueColorSerializer : ITypeReader<Color, DreamValueDataNode> {
        public Color Read(ISerializationManager serializationManager,
            DreamValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<Color>? instanceProvider = null) {
            if (!node.Value.TryGetValueAsString(out var strValue) || !ColorHelpers.TryParseColor(strValue, out var color))
                throw new Exception($"Value {node.Value} was not a color");

            return color;
        }

        public ValidationNode Validate(ISerializationManager serializationManager,
            DreamValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null) {
            if (node.Value.TryGetValueAsString(out var strValue) && ColorHelpers.TryParseColor(strValue, out _))
                return new ValidatedValueNode(node);

            return new ErrorNode(node, $"Value {node.Value} is not a color");
        }
    }

    [TypeSerializer]
    public sealed class DreamValueMatrix3Serializer : ITypeReader<Matrix3, DreamValueDataNode> {
        private readonly IDreamObjectTree _objectTree = IoCManager.Resolve<IDreamObjectTree>();

        public Matrix3 Read(ISerializationManager serializationManager,
            DreamValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<Matrix3>? instanceProvider = null) {
            if (!node.Value.TryGetValueAsDreamObjectOfType(_objectTree.Matrix, out var matrixObject))
                throw new Exception($"Value {node.Value} was not a matrix");

            // Matrix3 except not really because DM matrix is actually 3x2
            matrixObject.GetVariable("a").TryGetValueAsFloat(out var a);
            matrixObject.GetVariable("b").TryGetValueAsFloat(out var b);
            matrixObject.GetVariable("c").TryGetValueAsFloat(out var c);
            matrixObject.GetVariable("d").TryGetValueAsFloat(out var d);
            matrixObject.GetVariable("e").TryGetValueAsFloat(out var e);
            matrixObject.GetVariable("f").TryGetValueAsFloat(out var f);
            return new Matrix3(a, d, 0f, b, e, 0f, c, f, 1f);
        }

        public ValidationNode Validate(ISerializationManager serializationManager,
            DreamValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null) {
            if (node.Value.TryGetValueAsDreamObjectOfType(_objectTree.Matrix, out _))
                return new ValidatedValueNode(node);

            return new ErrorNode(node, $"Value {node.Value} is not a matrix");
        }
    }

    [TypeSerializer]
    public sealed class DreamValueColorMatrixSerializer : ITypeReader<ColorMatrix, DreamValueDataNode> {
        public ColorMatrix Read(ISerializationManager serializationManager,
            DreamValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<ColorMatrix>? instanceProvider = null) {
            if (node.Value.TryGetValueAsString(out string maybeColorString)) {
                if (ColorHelpers.TryParseColor(maybeColorString, out Color basicColor)) {
                    return new ColorMatrix(basicColor);
                }
            } else if(node.Value.TryGetValueAsDreamList(out DreamList matrixList)) {
                if(DreamProcNativeHelpers.TryParseColorMatrix(matrixList, out ColorMatrix matrix)) {
                    return matrix;
                }
            }
            throw new Exception($"Value {node.Value} was not a color matrix");
        }

        public ValidationNode Validate(ISerializationManager serializationManager,
            DreamValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null) {

            if (node.Value.TryGetValueAsDreamList(out var _))
                return new ValidatedValueNode(node);
            //TODO: Improve validation
            return new ErrorNode(node, $"Value {node.Value} is not a color matrix");
        }
    }
    #endregion Serialization
}
