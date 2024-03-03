namespace OpenDreamClient.Interface.DMF;

public sealed class DMFWinSet {
    public readonly string? Element;
    public readonly string Attribute;
    public readonly string Value;
    public DMFWinSet? Condition;
    public DMFWinSet? ElseValue;

    public DMFWinSet(string? element, string attribute, string value, DMFWinSet? condition = null, DMFWinSet? elseValue = null) {
        Element = element;
        Attribute = attribute;
        Value = value;
        Condition = condition;
        ElseValue = elseValue;
    }
}
