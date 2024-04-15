public abstract class DMFProperty {
    public abstract string AsArg();
    public abstract string AsEscaped();
    public abstract string AsString();
    public abstract string AsParams();
    public abstract string AsJSON();
    public abstract string AsJSONDM();
    public abstract string AsRaw();
    public override string ToString() {
        return AsRaw();
    }
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


public sealed class DMFPropertyString : DMFProperty {
    public string Value;

    public DMFPropertyString(string value) {
        Value = value;
    }

    public override string AsArg() {
        return Value;
    }

    public override string AsEscaped() {
        return Value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    public override string AsString() {
        return Value;
    }

    public override string AsParams() {
        return Value;
    }

    public override string AsJSON() {
        return "\"" + AsEscaped() + "\"";
    }

    public override string AsJSONDM() {
        return AsJSON();
    }

    public override string AsRaw() {
        return Value;
    }
}

public sealed class DMFPropertyNum : DMFProperty {
    public float Value;

    public DMFPropertyNum(float value) {
        Value = value;
    }
    public DMFPropertyNum(string value) {
        Value = float.Parse(value);
    }

    public override string AsArg() {
        return Value.ToString();
    }

    public override string AsEscaped() {
        return Value.ToString();
    }

    public override string AsString() {
        return Value.ToString();
    }

    public override string AsParams() {
        return Value.ToString();
    }

    public override string AsJSON() {
        return Value.ToString();
    }

    public override string AsJSONDM() {
        return Value.ToString();
    }

    public override string AsRaw() {
        return Value.ToString();
    }
}

public sealed class DMFPropertyVec2 : DMFProperty {
    public int X;
    public int Y;

    public DMFPropertyVec2(int x, int y) {
        X = x;
        Y = y;
    }

    public DMFPropertyVec2(string value) {
        string[] parts = value.Split(',');
        X = int.Parse(parts[0]);
        Y = int.Parse(parts[1]);
    }

    public DMFPropertyVec2(Vector2 value) {
        X = (int)value.X;
        Y = (int)value.Y;
    }

    public override string AsArg() {
        return X.ToString() + "," + Y.ToString();
    }

    public override string AsEscaped() {
        return X.ToString() + "," + Y.ToString();
    }

    public override string AsString() {
        return "(" + X.ToString() + ", " + Y.ToString() + ")";
    }

    public override string AsParams() {
        return X.ToString() + ", " + Y.ToString();
    }

    public override string AsJSON() {
        return "[" + X.ToString() + "," + Y.ToString() + "]";
    }

    public override string AsJSONDM() {
        return "[" + X.ToString() + "," + Y.ToString() + "]";
    }

    public override string AsRaw() {
        return X.ToString() + "," + Y.ToString();
    }
}

public sealed class DMFPropertyColor : DMFProperty {
    public string? Value;

    public DMFPropertyColor(string? value) {
        Value = value;
    }

    public override string AsArg() {
        return Value.ToString();
    }

    public override string AsEscaped() {
        return Value.ToString();
    }

    public override string AsString() {
        return Value.ToString();
    }

    public override string AsParams() {
        return Value.ToString();
    }

    public override string AsJSON() {
        return Value.ToString();
    }

    public override string AsJSONDM() {
        return Value.ToString();
    }

    public override string AsRaw() {
        return Value.ToString();
    }

}

public sealed class DMFPropertyBool : DMFProperty {
    public bool Value;

    public DMFPropertyBool(bool value) {
        Value = value;
    }

    public DMFPropertyBool(string value) {
        Value = value.Equals("1") || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public override string AsArg() {
        return Value ? "1" : "0";
    }

    public override string AsEscaped() {
        return Value ? "1" : "0";
    }

    public override string AsString() {
        return Value ? "true" : "false";
    }

    public override string AsParams() {
        return Value ? "true" : "false";
    }

    public override string AsJSON() {
        return Value ? "true" : "false";
    }

    public override string AsJSONDM() {
        return Value ? "true" : "false";
    }

    public override string AsRaw() {
        return Value ? "1" : "0";
    }
}
