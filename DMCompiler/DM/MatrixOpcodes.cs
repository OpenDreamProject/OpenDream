namespace DMCompiler.DM;

/// <summary>
/// These are the values associated with the wacky, undocumented /matrix() signatures. <br/>
/// See: https://www.byond.com/forum/post/1881375
/// </summary>
public enum MatrixOpcode {
    Copy = 0,
    Multiply = 1,
    Add = 2,
    Subtract = 3,
    Invert = 4,
    Rotate = 5,
    Scale = 6,
    Translate = 7,
    Interpolate = 8,
    Modify = 128
}
