using Dependency = Robust.Shared.IoC.DependencyAttribute;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
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
            Appearance = 64,

            // Special types for representing /datum/proc paths
            ProcStub = 128,
            VerbStub = 256
        }

        public static readonly DreamValue Null = new DreamValue((DreamObject?) null);
        public static DreamValue True => new DreamValue(1f);
        public static DreamValue False => new DreamValue(0f);

        public DreamValueType Type { get; private init; }

        private object? _refValue;
        private readonly float _floatValue;

        public DreamValue(string value) {
            Type = DreamValueType.String;
            _refValue = value;
        }

        public DreamValue(float value) {
            Type = DreamValueType.Float;
            _floatValue = value;
        }

        public DreamValue(int value) : this((float)value) { }

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

        public DreamValue(IconAppearance appearance) {
            Type = DreamValueType.Appearance;
            _refValue = appearance;
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
            else if (Type == 0)
                return "<Uninitialized DreamValue>";
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
                value = Unsafe.As<string>(_refValue)!;
                return true;
            } else {
                value = null;
                return false;
            }
        }

        public string MustGetValueAsString() {
            if (Type == DreamValueType.String) {
                return Unsafe.As<string>(_refValue)!;
            }
            throw new InvalidCastException("Value " + this + " was not the expected type of string");
        }

        //Casts a float value to an integer
        [Obsolete("Deprecated. Use TryGetValueAsInteger() or MustGetValueAsInteger() instead.")]
        public int GetValueAsInteger() {
            return MustGetValueAsInteger();
        }

        public bool TryGetValueAsInteger(out int value) {
            if (Type == DreamValueType.Float) {
                value = (int) _floatValue;
                return true;
            } else {
                value = 0;
                return false;
            }
        }

        public int MustGetValueAsInteger() {
            try {
                return (int) _floatValue;
            } catch (InvalidCastException) {
                throw new InvalidCastException($"Value {this} was not the expected type of integer");
            }
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
                value = Unsafe.As<DreamResource>(_refValue)!;
                return true;
            } else {
                value = null;
                return false;
            }
        }

        public DreamResource MustGetValueAsDreamResource() {
            if (Type == DreamValueType.DreamResource) {
                return Unsafe.As<DreamResource>(_refValue)!;
            }
            throw new InvalidCastException("Value " + this + " was not the expected type of DreamResource");
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

        public bool TryGetValueAsDreamObject<T>([NotNullWhen(true)] out T? dreamObject) where T : DreamObject {
            if (_refValue is T dreamObjectValue) {
                dreamObject = dreamObjectValue;
                return true;
            }

            dreamObject = null;
            return false;
        }

        public bool TryGetValueAsDreamList([NotNullWhen(true)] out DreamList? list) {
            return TryGetValueAsDreamObject(out list);
        }

        public DreamList MustGetValueAsDreamList() {
            try {
                return (DreamList) _refValue!;
            } catch (InvalidCastException) {
                throw new InvalidCastException("Value " + this + " was not the expected type of DreamList");
            }
        }

        public bool TryGetValueAsType([NotNullWhen(true)] out IDreamObjectTree.TreeEntry? type) {
            if (Type == DreamValueType.DreamType) {
                type = Unsafe.As<IDreamObjectTree.TreeEntry>(_refValue)!;

                return true;
            } else {
                type = null;

                return false;
            }
        }

        public IDreamObjectTree.TreeEntry MustGetValueAsType() {
            if (Type != DreamValueType.DreamType) // Could be a proc or verb stub, they hold they same value
                throw new InvalidCastException($"Value {this} was not the expected type of DreamPath");

            return Unsafe.As<IDreamObjectTree.TreeEntry>(_refValue)!;
        }

        public bool TryGetValueAsProc([NotNullWhen(true)] out DreamProc? proc) {
            if (Type == DreamValueType.DreamProc) {
                proc = Unsafe.As<DreamProc>(_refValue)!;

                return true;
            } else {
                proc = null;

                return false;
            }
        }

        public DreamProc MustGetValueAsProc() {
            if (Type == DreamValueType.DreamProc) {
                return Unsafe.As<DreamProc>(_refValue)!;
            }
            throw new InvalidCastException("Value " + this + " was not the expected type of DreamProc");
        }

        public bool TryGetValueAsProcStub([NotNullWhen(true)] out IDreamObjectTree.TreeEntry? type) {
            if (Type == DreamValueType.ProcStub) {
                type = Unsafe.As<IDreamObjectTree.TreeEntry>(_refValue)!;

                return true;
            } else {
                type = null;

                return false;
            }
        }

        public bool TryGetValueAsVerbStub([NotNullWhen(true)] out IDreamObjectTree.TreeEntry? type) {
            if (Type == DreamValueType.VerbStub) {
                type = Unsafe.As<IDreamObjectTree.TreeEntry>(_refValue)!;

                return true;
            } else {
                type = null;

                return false;
            }
        }

        public bool TryGetValueAsAppearance([NotNullWhen(true)] out IconAppearance? args) {
            if (Type == DreamValueType.Appearance) {
                args = Unsafe.As<IconAppearance>(_refValue)!;

                return true;
            }

            args = null;
            return false;
        }

        public IconAppearance MustGetValueAsAppearance() {
            if (Type == DreamValueType.Appearance) {
                return Unsafe.As<IconAppearance>(_refValue)!;
            }
            throw new InvalidCastException("Value " + this + " was not the expected type of Appearance");
        }

        public bool IsTruthy() {
            switch (Type) {
                case DreamValueType.DreamObject: {
                    Debug.Assert(_refValue is DreamObject or null, "Failed to cast a DreamValue's DreamObject");
                    return _refValue != null && Unsafe.As<DreamObject>(_refValue).Deleted == false;
                }
                case DreamValueType.DreamResource:
                case DreamValueType.DreamType:
                case DreamValueType.DreamProc:
                case DreamValueType.ProcStub:
                case DreamValueType.VerbStub:
                case DreamValueType.Appearance:
                    return true;
                case DreamValueType.Float:
                    return _floatValue != 0;
                case DreamValueType.String:
                    Debug.Assert(_refValue is string, "Failed to cast a DreamValueType.String as a string");
                    return Unsafe.As<string>(_refValue) != "";
                default:
                    throw new NotImplementedException($"Truthy evaluation for {this} is not implemented");
            }
        }

        public string Stringify() {
            switch (Type) {
                case DreamValueType.String:
                    return MustGetValueAsString();
                case DreamValueType.Float:
                    var floatValue = MustGetValueAsFloat();

                    if (floatValue > 16777216f) {
                        return floatValue.ToString("g6");
                    }

                    if (floatValue >= 1000000 && ((int)floatValue == floatValue)) {
                        return floatValue.ToString("g8");
                    }

                    return floatValue.ToString("g6");

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
                    var owner = Unsafe.As<IDreamObjectTree.TreeEntry>(_refValue)!;
                    var lastElement = (Type == DreamValueType.ProcStub) ? "/proc" : "/verb";

                    return $"{owner.Path}{lastElement}";
                case DreamValueType.DreamObject: {
                    TryGetValueAsDreamObject(out var dreamObject);

                    return dreamObject?.GetDisplayName() ?? string.Empty;
                }
                case DreamValueType.Appearance:
                    return string.Empty;
                default:
                    throw new NotImplementedException("Cannot stringify " + this);
            }
        }

        public override bool Equals(object? other) => other is DreamValue otherValue && Equals(otherValue);

        public bool Equals(DreamValue other) {
            if (Type != other.Type) return false;
            switch (Type) {
                case DreamValueType.Float:
                    return _floatValue.Equals(other._floatValue);
                // Ensure deleted DreamObjects are made null
                case DreamValueType.DreamObject: {
                    Debug.Assert(_refValue is DreamObject or null, "Failed to cast _refValue to DreamObject");
                    Debug.Assert(other._refValue is DreamObject or null, "Failed to cast other._refValue to DreamObject");
                    if (_refValue != null && Unsafe.As<DreamObject>(_refValue).Deleted)
                        _refValue = null;
                    if (other._refValue != null && Unsafe.As<DreamObject>(other._refValue).Deleted)
                        other._refValue = null;
                    break;
                }
            }

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
            writer.WriteNumber("Type", (int) value.Type);

            switch (value.Type) {
                case DreamValue.DreamValueType.String: writer.WriteString("Value", value.MustGetValueAsString()); break;
                case DreamValue.DreamValueType.Float: writer.WriteNumber("Value", value.MustGetValueAsFloat()); break;
                case DreamValue.DreamValueType.DreamObject: {
                    var dreamObject = value.MustGetValueAsDreamObject();

                    if (dreamObject == null) {
                        writer.WriteNull("Value");
                    } else {
                        writer.WriteString("Value", dreamObject.ObjectDefinition.Type.PathString);

                        if (dreamObject is not DreamObjectIcon icon) {
                            throw new NotImplementedException($"Json serialization for {value} is not implemented");
                        }

                        // TODO Check what happens with multiple states
                        var resource = icon.Icon.GenerateDMI();
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
            DreamValue.DreamValueType type = (DreamValue.DreamValueType) reader.GetInt32();
            reader.Read();

            if (reader.GetString() != "Value") throw new Exception("Expected value property");
            reader.Read();

            DreamValue value;
            switch (type) {
                case DreamValue.DreamValueType.String: value = new DreamValue(reader.GetString()); break;
                case DreamValue.DreamValueType.Float: value = new DreamValue(reader.GetSingle()); break;
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
                        var iconObj = _objectTree.CreateObject<DreamObjectIcon>(_objectTree.Icon);

                        iconObj.Icon.InsertStates(resource, DreamValue.Null, DreamValue.Null, DreamValue.Null);
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
            if (!node.Value.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrixObject))
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
            if (node.Value.TryGetValueAsDreamObject<DreamObjectMatrix>(out _))
                return new ValidatedValueNode(node);

            return new ErrorNode(node, $"Value {node.Value} is not a matrix");
        }
    }


    [TypeSerializer]
    public sealed class DreamValueIconSerializer : ITypeReader<int, DreamValueDataNode> {
        private readonly DreamResourceManager _dreamResourceManager = IoCManager.Resolve<DreamResourceManager>();

        public int Read(ISerializationManager serializationManager,
            DreamValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<int>? instanceProvider = null) {
            if (!_dreamResourceManager.TryLoadIcon(node.Value, out var icon))
                throw new Exception($"Value {node.Value} was not a valid IconResource type");

            return icon.Id;
        }

        public ValidationNode Validate(ISerializationManager serializationManager,
            DreamValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null) {
            if (_dreamResourceManager.TryLoadIcon(node.Value, out _))
                return new ValidatedValueNode(node);

            return new ErrorNode(node, $"Value {node.Value} is not an Icon");
        }
    }

    [TypeSerializer]
    public sealed class DreamValueFlagsSerializer : ITypeReader<short, DreamValueDataNode> {
        public short Read(ISerializationManager serializationManager,
            DreamValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<short>? instanceProvider = null) {
            return (short) node.Value.MustGetValueAsInteger();
        }

        public ValidationNode Validate(ISerializationManager serializationManager,
            DreamValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null) {
            if (node.Value.TryGetValueAsInteger(out int val) && val < short.MaxValue)
                return new ValidatedValueNode(node);

            return new ErrorNode(node, $"Value {node.Value} is not a valid flag set");
        }
    }


    [TypeSerializer]
    public sealed class DreamValueColorMatrixSerializer : ITypeReader<ColorMatrix, DreamValueDataNode>, ITypeCopyCreator<ColorMatrix> {
        public ColorMatrix Read(ISerializationManager serializationManager,
            DreamValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<ColorMatrix>? instanceProvider = null) {
            if (node.Value.TryGetValueAsString(out var maybeColorString)) {
                if (ColorHelpers.TryParseColor(maybeColorString, out Color basicColor)) {
                    return new ColorMatrix(basicColor);
                }
            } else if (node.Value.TryGetValueAsDreamList(out var matrixList)) {
                if (DreamProcNativeHelpers.TryParseColorMatrix(matrixList, out ColorMatrix matrix)) {
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

        public ColorMatrix CreateCopy(ISerializationManager serializationManager, ColorMatrix source,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null) {
            return new(source);
        }
    }
    #endregion Serialization
}
