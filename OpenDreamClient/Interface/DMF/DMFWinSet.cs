namespace OpenDreamClient.Interface.DMF;

public sealed class DMFWinSet {
    public readonly string? Element;
    public readonly string Attribute;
    public readonly string Value;
    /// Winsets that are evaluated if Element.Attribute == Value
    public List<DMFWinSet>? TrueStatements;
    /// Winsets that are evaluated if Element.Attribute != Value
    public List<DMFWinSet>? FalseStatements;

    public DMFWinSet(string? element, string attribute, string value, List<DMFWinSet>? trueStatements = null, List<DMFWinSet>? falseStatements = null) {
        Element = element;
        Attribute = attribute;
        Value = value;
        TrueStatements = trueStatements;
        FalseStatements = falseStatements;
    }
}
