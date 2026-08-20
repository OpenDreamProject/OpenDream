using Dependency = Robust.Shared.IoC.DependencyAttribute;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
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
using Robust.Shared.Utility;

namespace OpenDreamRuntime;

[JsonConverter(typeof(DreamValueJsonConverter))]
public struct DreamValue : IDisposable, IEquatable<DreamValue> {
    public enum DreamValueType {
        // @formatter:off
        String        = 1,
        Float         = 2,
        DreamResource = 3,
        DreamObject   = 4,
        DreamType     = 5,
        DreamProc     = 6,
        Appearance    = 7
        // @formatter:on
    }

    [Flags]
    public enum DreamValueTypeFlag {
        // @formatter:off
        String        = 1 << (DreamValueType.String        - 1),
        Float         = 1 << (DreamValueType.Float         - 1),
        DreamResource = 1 << (DreamValueType.DreamResource - 1),
        DreamObject   = 1 << (DreamValueType.DreamObject   - 1),
        DreamType     = 1 << (DreamValueType.DreamType     - 1),
        DreamProc     = 1 << (DreamValueType.DreamProc     - 1),
        Appearance    = 1 << (DreamValueType.Appearance    - 1)
        // @formatter:on
    }

    public static DreamValue Null {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new DreamValue((DreamObject?) null);
    }

    public static DreamValue True {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new DreamValue(1f);
    }

    public static DreamValue False {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new DreamValue(0f);
    }

    public static DreamValue EmptyString {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new DreamValue("");
    }

    public readonly DreamValueType Type;

    private object? _refValue;
    private readonly float _floatValue;

    #if TOOLS
    //ReSharper disable once NotAccessedField.Local
    private readonly ProfilerMemory? _tracyMemoryId; //only used for strings, since everything else is a value type or handled in DreamObject
    #endif

    public DreamValue(string value) {
        DebugTools.Assert(value != null);
        Type = DreamValueType.String;
        #if TOOLS
        _tracyMemoryId = Profiler.BeginMemoryZone((ulong) (1+value.Length*sizeof(char)), "string");
        #endif
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

    public DreamValue(TreeEntry value) {
        Type = DreamValueType.DreamType;
        _refValue = value;
    }

    public DreamValue(DreamProc value) {
        Type = DreamValueType.DreamProc;
        _refValue = value;
    }

    public DreamValue(MutableAppearance appearance) {
        Type = DreamValueType.Appearance;
        _refValue = appearance;
    }

    public bool IsNull {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Type == DreamValueType.DreamObject && (_refValue == null || Unsafe.As<DreamObject>(_refValue).Deleted);
    }

    public readonly override string ToString() {
        if (Type == DreamValueType.Float)
            return _floatValue.ToString(CultureInfo.InvariantCulture);
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

    public void Dispose() {
        DecRef();
    }

    public void IncRef() {
        if (Type != DreamValueType.DreamObject || _refValue == null)
            return;

        Unsafe.As<DreamObject>(_refValue).IncRef();
    }

    public void DecRef() {
        if (Type != DreamValueType.DreamObject || _refValue == null)
            return;

        Unsafe.As<DreamObject>(_refValue).DecRef();
    }

    [Obsolete("Deprecated. Use TryGetValueAsString() or MustGetValueAsString() instead.")]
    public string GetValueAsString() {
        return MustGetValueAsString();
    }

    public readonly bool TryGetValueAsString([NotNullWhen(true)] out string? value) {
        if (Type == DreamValueType.String) {
            value = Unsafe.As<string>(_refValue)!;
            return true;
        } else {
            value = null;
            return false;
        }
    }

    public string MustGetValueAsString() {
        if (Type != DreamValueType.String)
            ThrowInvalidCastString();

        return Unsafe.As<string>(_refValue)!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowInvalidCastString() {
        throw new InvalidCastException("Value " + this + " was not the expected type of string");
    }

    //Casts a float value to an integer
    [Obsolete("Deprecated. Use TryGetValueAsInteger() or MustGetValueAsInteger() instead.")]
    public int GetValueAsInteger() {
        return MustGetValueAsInteger();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetValueAsInteger(out int value) {
        value = (int)_floatValue;
        return Type == DreamValueType.Float;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int MustGetValueAsInteger() {
        if (Type != DreamValueType.Float)
            ThrowInvalidCastFloat();

        return (int) _floatValue;
    }

    /// <summary>
    /// Casts the DreamValue to a float without throwing exceptions. Useful where BYOND coerces non-numbers to 0.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
    public float UnsafeGetValueAsFloat() {
        return _floatValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetValueAsFloat(out float value) {
        value = _floatValue;
        return Type == DreamValueType.Float;
    }

    /// <summary>
    /// Identical to <see cref="TryGetValueAsFloat"/> except null is treated as zero and returns true
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetValueAsFloatCoerceNull(out float value) {
        value = _floatValue;
        return (Type == DreamValueType.Float || this == Null) ;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float MustGetValueAsFloat() {
        if (Type != DreamValueType.Float)
            ThrowInvalidCastFloat();

        return _floatValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowInvalidCastFloat() {
        throw new InvalidCastException($"Value {this} was not the expected type of float");
    }

    public readonly bool TryGetValueAsDreamResource([NotNullWhen(true)] out DreamResource? value) {
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
        if (Type != DreamValueType.DreamObject) {
            ThrowInvalidCastDreamObject();
        }

        DreamObject? dreamObject = Unsafe.As<DreamObject>(_refValue);
        if (dreamObject == null || dreamObject.Deleted)
            return null;

        return dreamObject;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowInvalidCastDreamObject() {
        throw new InvalidCastException($"Value {this} was not the expected type of DreamObject");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsDreamObject<T>() where T : DreamObject {
        return _refValue is T;
    }

    public readonly bool TryGetValueAsDreamObject<T>([NotNullWhen(true)] out T? dreamObject) where T : DreamObject {
        if (_refValue is T dreamObjectValue) {
            dreamObject = dreamObjectValue;
            return true;
        }

        dreamObject = null;
        return false;
    }

    public readonly bool TryGetValueAsDreamList([NotNullWhen(true)] out DreamList? list) {
        return TryGetValueAsDreamObject(out list);
    }

    public DreamList MustGetValueAsDreamList() {
        if (_refValue is not DreamList dl) {
            ThrowInvalidCastList();
            return null!;
        }

        return dl;
    }

    // TODO: Replace GetValueAsDreamList with GetValueAsIDreamList if possible. IDreamList isn't complete enough yet.
    public readonly bool TryGetValueAsIDreamList([NotNullWhen(true)] out IDreamList? list) {
        if (_refValue is IDreamList idl) {
            list = idl;
            return true;
        }

        list = null;
        return false;
    }

    public IDreamList MustGetValueAsIDreamList() {
        if (_refValue is IDreamList idl)
            return idl;

        ThrowInvalidCastList();
        return null!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowInvalidCastList() {
        throw new InvalidCastException("Value " + this + " was not the expected type of DreamList");
    }

    public readonly bool TryGetValueAsType([NotNullWhen(true)] out TreeEntry? type) {
        if (Type == DreamValueType.DreamType) {
            type = Unsafe.As<TreeEntry>(_refValue)!;

            return true;
        }

        type = null;
        return false;
    }

    public TreeEntry MustGetValueAsType() {
        if (Type != DreamValueType.DreamType) // Could be a proc or verb stub, they hold they same value
            throw new InvalidCastException($"Value {this} was not the expected type of DreamPath");

        return Unsafe.As<TreeEntry>(_refValue)!;
    }

    public readonly bool TryGetValueAsProc([NotNullWhen(true)] out DreamProc? proc) {
        if (Type == DreamValueType.DreamProc) {
            proc = Unsafe.As<DreamProc>(_refValue)!;

            return true;
        }

        proc = null;
        return false;
    }

    public DreamProc MustGetValueAsProc() {
        if (Type == DreamValueType.DreamProc) {
            return Unsafe.As<DreamProc>(_refValue)!;
        }

        throw new InvalidCastException("Value " + this + " was not the expected type of DreamProc");
    }

    public readonly bool TryGetValueAsAppearance([NotNullWhen(true)] out MutableAppearance? args) {
        if (Type == DreamValueType.Appearance) {
            args = Unsafe.As<MutableAppearance>(_refValue)!;

            return true;
        }

        args = null;
        return false;
    }

    public MutableAppearance MustGetValueAsAppearance() {
        if (Type == DreamValueType.Appearance) {
            return Unsafe.As<MutableAppearance>(_refValue)!;
        }

        throw new InvalidCastException("Value " + this + " was not the expected type of Appearance");
    }

    public bool IsTruthy() {
        switch (Type) {
            case DreamValueType.DreamObject: {
                Debug.Assert(_refValue is DreamObject or null, "Failed to cast a DreamValue's DreamObject");
                return _refValue != null && Unsafe.As<DreamObject>(_refValue).Deleted == false;
            }
            case DreamValueType.Float:
                return _floatValue != 0;
            case DreamValueType.String:
                Debug.Assert(_refValue is string, "Failed to cast a DreamValueType.String as a string");
                return Unsafe.As<string>(_refValue) != "";
            case DreamValueType.DreamResource:
            case DreamValueType.DreamType:
            case DreamValueType.DreamProc:
            case DreamValueType.Appearance:
                return true;
            default:
                return false;
        }
    }

    public string Stringify() {
        switch (Type) {
            case DreamValueType.String:
                return MustGetValueAsString();
            case DreamValueType.Float:
                var floatValue = MustGetValueAsFloat();

                if (float.IsInfinity(floatValue)) {
                    var str = float.IsPositiveInfinity(floatValue) ? "inf" : "-inf";
                    return str;
                }

                if (floatValue > 16777216f) {
                    return floatValue.ToString("g6");
                }

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (floatValue >= 1000000 && ((int)floatValue == floatValue)) {
                    return floatValue.ToString("g8");
                }

                if (float.IsNaN(floatValue)) return "nan";

                return floatValue.ToString("g6");

            case DreamValueType.DreamResource:
                var rsc = MustGetValueAsDreamResource();
                return rsc.ResourcePath ?? string.Empty;
            case DreamValueType.DreamType:
                return MustGetValueAsType().Path;
            case DreamValueType.DreamProc:
                var proc = MustGetValueAsProc();

                return proc.ToString();
            case DreamValueType.DreamObject: {
                TryGetValueAsDreamObject(out var dreamObject);

                return dreamObject?.GetDisplayName() ?? string.Empty;
            }
            case DreamValueType.Appearance:
                return string.Empty;
            case 0:
                return "<Uninitialized DreamValue>";
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
                var thisObj = Unsafe.As<DreamObject>(_refValue);
                var otherObj = Unsafe.As<DreamObject>(other._refValue);
                if (thisObj != null && thisObj.Deleted) {
                    _refValue = null;
                    thisObj = null;
                }

                if (otherObj != null && otherObj.Deleted) {
                    other._refValue = null;
                    otherObj = null;
                }

                return thisObj == otherObj;
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


public sealed partial class DreamValueJsonConverter : JsonConverter<DreamValue> {
    [Dependency] private DreamObjectTree _objectTree = default!;
    [Dependency] private DreamResourceManager _resourceManager = default!;

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
                    writer.WriteString("Value", dreamObject.ObjectDefinition.Type);

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
                    var objectDef = _objectTree.GetTreeEntry(objectTypePath).ObjectDefinition;
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

