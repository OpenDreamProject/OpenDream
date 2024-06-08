using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace OpenDreamClient.Interface.DMF;

public interface IDMFProperty {
    public bool Equals(string comparison);
    public string AsArg();
    public string AsEscaped();
    public string AsString();
    public string AsParams();
    public string AsJson();
    public string AsJsonDM();
    public string AsRaw();
}

/*
arg
    Value is formatted as if it's an argument on a command line. Numbers are left alone; booleans are 0 or 1; size and position have their X and Y values separated by a space; pretty much everything else is DM-escaped and enclosed in quotes.
escaped
    DM-escape the value as if it's in a quoted string but do not include the quotes. Size and position values both use , to separate their X and Y values.
string
    Value is formatted as a DM-escaped string with surrounding quotes.
params
    Format value for a URL-encoded parameter list (see list2params), escaping characters as needed.
json
    JSON formatting. Numbers are left unchanged; size or position values are turned into objects with x and y items; boolean values are true or false.
json-dm
    JSON formatting, but DM-escaped so it can be included in a quoted string. Quotes are not included.
raw
    Does not change the value's text representation in any way; assumes it's already formatted correctly for the purpose. This is similar to as arg but does no escaping and no quotes.
*/

public struct DMFPropertyString(string value) : IDMFProperty {
    public string? Value = value;

    public string AsArg() {
        return Value != null ? "\""+AsEscaped()+"\"" : "\"\"";
    }

    public string AsEscaped() {
        if(Value == null)
            return "";
        return Value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    public string AsString() {
        return AsArg();
    }

    public string AsParams() {
        if(Value == null)
            return "";
        else
            return System.Web.HttpUtility.UrlEncode(Value);
    }

    public string AsJson() {
        return AsArg();
    }

    public string AsJsonDM() {
        //basically just escaped, quoted, and escaped again
        var orig = Value;
        Value = AsArg();
        var result = AsEscaped();
        Value = orig;
        return result;
    }

    public string AsRaw() {
        return Value ?? "";
    }

    public override string ToString() {
        return AsRaw();
    }

    public bool Equals(string comparison) {
        return comparison.Equals(Value, StringComparison.InvariantCulture);
    }
}

public struct DMFPropertyNum(float value) : IDMFProperty {
    public float Value = value;

    public DMFPropertyNum(string value) : this(0) {
        try {
            Value = float.Parse(value);
        } catch {
            int lastValidPos = value.LastIndexOfAny("0123456789".ToCharArray());
            Value = float.Parse(value.Substring(0, lastValidPos+1));
            Logger.GetSawmill("opendream.interface").Warning($"Invalid value in DMFPropertyNum '{value}'. Parsed as '{Value}'. {lastValidPos}");
        }
    }

    public string AsArg() {
        return AsRaw();
    }

    public string AsEscaped() {
        return AsRaw();
    }

    public string AsString() {
        return "\""+AsRaw()+"\"";
    }

    public string AsParams() {
        return AsRaw();
    }

    public string AsJson() {
        return AsRaw();
    }

    public string AsJsonDM() {
        return AsRaw();
    }

    public string AsRaw() {
        return Value.ToString(CultureInfo.InvariantCulture);
    }

    public override string ToString() {
        return AsRaw();
    }

    public bool Equals(string comparison) {
        DMFPropertyNum comparisonNum = new(comparison);
        return comparisonNum.Value == Value;
    }
}

public struct DMFPropertyVec2 : IDMFProperty {
    public int X;
    public int Y;
    public char delim = ',';

    public DMFPropertyVec2(int x, int y) {
        X = x;
        Y = y;
    }

    public DMFPropertyVec2(string value) {
        if(value.Equals("none",StringComparison.InvariantCultureIgnoreCase)){
            X = 0;
            Y = 0;
            return;
        }

        string[] parts = value.Split([',','x',' ']);

        X = int.Parse(parts[0]);
        Y = int.Parse(parts[1]);
    }

    public DMFPropertyVec2(Vector2 value) {
        X = (int)value.X;
        Y = (int)value.Y;
    }

    public DMFPropertyVec2(Vector2i value) {
        X = value.X;
        Y = value.Y;
    }

    public string AsArg() {
        return X.ToString() + " " + Y.ToString();
    }

    public string AsEscaped() {
        return X.ToString() + delim + Y.ToString();
    }

    public string AsString() {
        return "\"" + AsEscaped() + "\"";
    }

    public string AsParams() {
        return AsEscaped();
    }

    public string AsJson() {
        return "{\"x\":" + X.ToString() + ", \"y\":" + Y.ToString() + "}";
    }

    public string AsJsonDM() {
        return "{\\\"x\\\":" + X.ToString() + ", \\\"y\\\":" + Y.ToString() + "}";
    }

    public string AsRaw() {
        return AsEscaped();
    }

    public override string ToString() {
        return AsRaw();
    }

    public bool Equals(string comparison) {
        DMFPropertyVec2 comparisonVec = new(comparison);
        return comparisonVec.X == X && comparisonVec.Y == Y;
    }
}

public struct DMFPropertySize : IDMFProperty {
    private DMFPropertyVec2 Value;
    public int X {get => Value.X; set => Value.X = value;}
    public int Y {get => Value.Y; set => Value.Y = value;}
    private const char delim = 'x';

    public DMFPropertySize(int x, int y) {
        Value = new(x, y) {
            delim = delim
        };
    }

    public DMFPropertySize(string value) {
        Value = new(value) {
            delim = delim
        };
    }

    public DMFPropertySize(Vector2 value) {
        Value = new(value) {
            delim = delim
        };
    }

    public DMFPropertySize(Vector2i value) {
        Value = new(value) {
            delim = delim
        };
    }
    public string AsArg() {
        return Value.AsArg();
    }

    public string AsEscaped() {
        return Value.AsEscaped();
    }

    public string AsJson() {
        return Value.AsJson();
    }

    public string AsJsonDM() {
        return Value.AsJsonDM();
    }

    public string AsParams() {
        return Value.AsParams();
    }

    public string AsRaw() {
        return Value.AsRaw();
    }

    public string AsString() {
        return Value.AsString();
    }

    public bool Equals(string comparison) {
        return Value.Equals(comparison);
    }
}

public struct DMFPropertyPos : IDMFProperty {
    private DMFPropertyVec2 Value;
    public int X {get => Value.X; set => Value.X = value;}
    public int Y {get => Value.Y; set => Value.Y = value;}
    private const char delim = ',';

    public DMFPropertyPos(int x, int y) {
        Value = new(x, y) {
            delim = delim
        };
    }

    public DMFPropertyPos(string value) {
        Value = new(value) {
            delim = delim
        };
    }

    public DMFPropertyPos(Vector2 value) {
        Value = new(value) {
            delim = delim
        };
    }

    public DMFPropertyPos(Vector2i value) {
        Value = new(value) {
            delim = delim
        };
    }

    public string AsArg() {
        return Value.AsArg();
    }

    public string AsEscaped() {
        return Value.AsEscaped();
    }

    public string AsJson() {
        return Value.AsJson();
    }

    public string AsJsonDM() {
        return Value.AsJsonDM();
    }

    public string AsParams() {
        return Value.AsParams();
    }

    public string AsRaw() {
        return Value.AsRaw();
    }

    public string AsString() {
        return Value.AsString();
    }

    public bool Equals(string comparison) {
        return Value.Equals(comparison);
    }
}

public struct DMFPropertyColor : IDMFProperty {
    public Color Value;

    public DMFPropertyColor(Color value) {
        Value = value;
    }

    public DMFPropertyColor(string stringValue) {
        if (stringValue.Equals("none", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(stringValue)) {
            Value = Color.Transparent;
        } else {
            var deserializedColor = Color.TryFromName(stringValue, out var color)
                    ? color :
                    Color.TryFromHex(stringValue);

            if (deserializedColor is null)
                throw new Exception($"Value {stringValue} was not a valid DMF color value!");
            else
                Value = deserializedColor.Value;
        }
    }

    public string AsArg() {
        return AsString();
    }

    public string AsEscaped() {
        return AsRaw();
    }

    public string AsString() {
        return "\""+AsRaw()+"\"";
    }

    public string AsParams() {
        return AsRaw();
    }

    public string AsJson() {
        if(Value == Color.Transparent)
            return "\"null\"";
        return AsString();
    }

    public string AsJsonDM() {
        if(Value == Color.Transparent)
            return "\"null\"";
        return AsString();
    }

    public string AsRaw() {
        if(Value == Color.Transparent)
            return "";
        return Value.ToHexNoAlpha();
    }

    public override string ToString() {
        return AsRaw();
    }

    public bool Equals(string comparison) {
        DMFPropertyColor comparisonColor = new(comparison);
        return comparisonColor.Value == Value;
    }
}

public struct DMFPropertyBool(bool value) : IDMFProperty {
    public bool Value = value;

    public DMFPropertyBool(string value) : this(value.Equals("1") || value.Equals("true", StringComparison.OrdinalIgnoreCase)) {
    }

    public string AsArg() {
        return Value ? "1" : "0";
    }

    public string AsEscaped() {
        return AsArg();
    }

    public string AsString() {
        return Value ? "\"true\"" : "\"false\"";
    }

    public string AsParams() {
        return AsArg();
    }

    public string AsJson() {
        return Value ? "true" : "false";
    }

    public string AsJsonDM() {
        return Value ? "true" : "false";
    }

    public string AsRaw() {
        return Value ? "1" : "0";
    }

    public override string ToString() {
        return AsRaw();
    }

    public bool Equals(string comparison) {
        DMFPropertyBool comparisonBool = new(comparison);
        return comparisonBool.Value == Value;
    }
}

#region Serializers
/// TLDR everything is a string passed to the constructor

[TypeSerializer]
public sealed class DMFPropertyStringSerializer : ITypeSerializer<DMFPropertyString, ValueDataNode>, ITypeCopyCreator<DMFPropertyString> {
    public DMFPropertyString Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<DMFPropertyString>? instanceProvider = null) {
        return new(node.Value);
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null) {
        try {
            _ = new DMFPropertyString(node.Value);
            return new ValidatedValueNode(node);
        } catch (Exception e) {
            return new ErrorNode(node, e.Message);
        }
    }

    public DataNode Write(ISerializationManager serializationManager, DMFPropertyString value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null) {
        return new ValueDataNode(value.AsRaw());
    }

    [MustUseReturnValue]
    public DMFPropertyString CreateCopy(ISerializationManager serializationManager, DMFPropertyString source,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null) {
        return new(source.AsRaw());
    }
}

[TypeSerializer]
public sealed class DMFPropertyNumSerializer : ITypeSerializer<DMFPropertyNum, ValueDataNode>, ITypeCopyCreator<DMFPropertyNum> {
    public DMFPropertyNum Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<DMFPropertyNum>? instanceProvider = null) {
        return new(node.Value);
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null) {
        try {
            _ = new DMFPropertyNum(node.Value);
            return new ValidatedValueNode(node);
        } catch (Exception e) {
            return new ErrorNode(node, e.Message);
        }
    }

    public DataNode Write(ISerializationManager serializationManager, DMFPropertyNum value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null) {
        return new ValueDataNode(value.AsRaw());
    }

    [MustUseReturnValue]
    public DMFPropertyNum CreateCopy(ISerializationManager serializationManager, DMFPropertyNum source,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null) {
        return new(source.AsRaw());
    }
}

[TypeSerializer]
public sealed class DMFPropertyVec2Serializer : ITypeSerializer<DMFPropertyVec2, ValueDataNode>, ITypeCopyCreator<DMFPropertyVec2> {
    public DMFPropertyVec2 Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<DMFPropertyVec2>? instanceProvider = null) {
        return new(node.Value);
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null) {
        try {
            _ = new DMFPropertyVec2(node.Value);
            return new ValidatedValueNode(node);
        } catch (Exception e) {
            return new ErrorNode(node, e.Message);
        }
    }

    public DataNode Write(ISerializationManager serializationManager, DMFPropertyVec2 value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null) {
        return new ValueDataNode(value.AsRaw());
    }

    [MustUseReturnValue]
    public DMFPropertyVec2 CreateCopy(ISerializationManager serializationManager, DMFPropertyVec2 source,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null) {
        return new(source.AsRaw());
    }

}

[TypeSerializer]
public sealed class DMFPropertySizeSerializer : ITypeSerializer<DMFPropertySize, ValueDataNode>, ITypeCopyCreator<DMFPropertySize> {
    public DMFPropertySize Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<DMFPropertySize>? instanceProvider = null) {
        return new(node.Value);
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null) {
        try {
            _ = new DMFPropertySize(node.Value);
            return new ValidatedValueNode(node);
        } catch (Exception e) {
            return new ErrorNode(node, e.Message);
        }
    }

    public DataNode Write(ISerializationManager serializationManager, DMFPropertySize value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null) {
        return new ValueDataNode(value.AsRaw());
    }

    [MustUseReturnValue]
    public DMFPropertySize CreateCopy(ISerializationManager serializationManager, DMFPropertySize source,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null) {
        return new(source.AsRaw());
    }

}

[TypeSerializer]
public sealed class DMFPropertyPosSerializer : ITypeSerializer<DMFPropertyPos, ValueDataNode>, ITypeCopyCreator<DMFPropertyPos> {
    public DMFPropertyPos Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<DMFPropertyPos>? instanceProvider = null) {
        return new(node.Value);
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null) {
        try {
            _ = new DMFPropertyPos(node.Value);
            return new ValidatedValueNode(node);
        } catch (Exception e) {
            return new ErrorNode(node, e.Message);
        }
    }

    public DataNode Write(ISerializationManager serializationManager, DMFPropertyPos value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null) {
        return new ValueDataNode(value.AsRaw());
    }

    [MustUseReturnValue]
    public DMFPropertyPos CreateCopy(ISerializationManager serializationManager, DMFPropertyPos source,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null) {
        return new(source.AsRaw());
    }

}

[TypeSerializer]
public sealed class DMFPropertyColorSerializer : ITypeSerializer<DMFPropertyColor, ValueDataNode>, ITypeCopyCreator<DMFPropertyColor> {
    public DMFPropertyColor Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<DMFPropertyColor>? instanceProvider = null) {
        return new(node.Value);
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null) {
        try {
            _ = new DMFPropertyColor(node.Value);
            return new ValidatedValueNode(node);
        } catch (Exception e) {
            return new ErrorNode(node, e.Message);
        }
    }

    public DataNode Write(ISerializationManager serializationManager, DMFPropertyColor value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null) {
        return new ValueDataNode(value.AsRaw());
    }

    [MustUseReturnValue]
    public DMFPropertyColor CreateCopy(ISerializationManager serializationManager, DMFPropertyColor source,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null) {
        return new(source.AsRaw());
    }
}

[TypeSerializer]
public sealed class DMFPropertyBoolSerializer : ITypeSerializer<DMFPropertyBool, ValueDataNode>, ITypeCopyCreator<DMFPropertyBool> {
    public DMFPropertyBool Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<DMFPropertyBool>? instanceProvider = null) {
        return new(node.Value);
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null) {
        try {
            _ = new DMFPropertyBool(node.Value);
            return new ValidatedValueNode(node);
        } catch (Exception e) {
            return new ErrorNode(node, e.Message);
        }
    }

    public DataNode Write(ISerializationManager serializationManager, DMFPropertyBool value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null) {
        return new ValueDataNode(value.AsRaw());
    }

    [MustUseReturnValue]
    public DMFPropertyBool CreateCopy(ISerializationManager serializationManager, DMFPropertyBool source,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null) {
        return new(source.AsRaw());
    }
}

#endregion
