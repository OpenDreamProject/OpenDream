namespace OpenDreamClient.Interface.DMF;

public sealed class DMFWinSet {
    public readonly string? Element;
    public readonly string Attribute;
    public readonly string Value;
    public List<DMFWinSet>? IfValues;
    public List<DMFWinSet>? ElseValues;

    public DMFWinSet(string? element, string attribute, string value, List<DMFWinSet>? ifValue = null, List<DMFWinSet>? elseValue = null) {
        Element = element;
        Attribute = attribute;
        Value = value;
        IfValues = ifValue;
        ElseValues = elseValue;
    }
}
