using System.Collections.Generic;

namespace OpenDreamShared.Interface.DMF;

public sealed class DMFWinSet(
    string? element,
    string attribute,
    string value,
    List<DMFWinSet>? trueStatements = null,
    List<DMFWinSet>? falseStatements = null) {
    public readonly string? Element = element;
    public readonly string Attribute = attribute;
    public readonly string Value = value;

    /// Winsets that are evaluated if Element.Attribute == Value
    public List<DMFWinSet>? TrueStatements = trueStatements;

    /// Winsets that are evaluated if Element.Attribute != Value
    public List<DMFWinSet>? FalseStatements = falseStatements;
}
