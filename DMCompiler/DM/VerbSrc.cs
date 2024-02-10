namespace DMCompiler.DM;

/// <summary>
/// The value of "set src = ..." in a verb
/// </summary>
public enum VerbSrc {
    View,
    OView,
    Range,
    ORange,
    World,
    WorldContents,
    Usr,
    UsrContents,
    UsrLoc,
    UsrGroup
}
