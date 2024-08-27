namespace OpenDreamClient.Interface.DMF;

public sealed class DMFWinSet(
    string? element,
    string attribute,
    string value,
    bool selectDefault,
    List<DMFWinSet>? trueStatements = null,
    List<DMFWinSet>? falseStatements = null) {
    public readonly string? Element = element;
    public readonly string Attribute = attribute;
    public readonly string Value = value;

    // ":[type]" selects the default control of that type
    public readonly bool SelectDefault = selectDefault;

    /// Winsets that are evaluated if Element.Attribute == Value
    public List<DMFWinSet>? TrueStatements = trueStatements;

    /// Winsets that are evaluated if Element.Attribute != Value
    public List<DMFWinSet>? FalseStatements = falseStatements;
}
