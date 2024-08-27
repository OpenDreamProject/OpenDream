namespace OpenDreamClient.Interface.DMF;

public sealed class DMFWinSet {
    public readonly string? Element;
    public readonly string Attribute;
    public readonly string Value;

    // ":[type]" selects the default control of that type
    public readonly bool SelectDefault;

    /// Winsets that are evaluated if Element.Attribute == Value
    public List<DMFWinSet>? TrueStatements;

    /// Winsets that are evaluated if Element.Attribute != Value
    public List<DMFWinSet>? FalseStatements;

    public DMFWinSet(string? element, string attribute, string value, bool selectDefault, List<DMFWinSet>? trueStatements = null, List<DMFWinSet>? falseStatements = null) {
        Element = element;
        Attribute = attribute;
        Value = value;
        SelectDefault = selectDefault;
        TrueStatements = trueStatements;
        FalseStatements = falseStatements;
    }
}
