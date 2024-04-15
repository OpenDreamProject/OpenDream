public abstract class DMFProperty {
    public abstract string AsArg();
    public abstract string AsEscaped();
    public abstract string AsString();
    public abstract string AsParams();
    public abstract string AsJSON();
    public abstract string AsJSONDM();
    public abstract string AsRaw();
}

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
