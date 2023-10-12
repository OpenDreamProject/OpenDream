using System;
using Robust.Shared.Maths;

namespace OpenDreamShared.Dream;

/// <summary>
/// This is supposed to be used to describe a string that represents a range of tiles, like "11x4" or whatever. <br/>
/// Used as a possible argument for some functionality, like world.view or orange()
/// </summary>
public readonly struct ViewRange {
    public readonly int Width, Height;
    public bool IsSquare => (Width == Height);

    public int CenterX => Width / 2;
    public int CenterY => Height / 2;
    public Vector2i Center => (CenterX, CenterY);

    //View can be centered in both directions?
    public bool IsCenterable => (Width % 2 == 1) && (Height % 2 == 1);

    /// <summary>
    /// The distance this ViewRange covers in every direction if <see cref="IsSquare"/> and
    /// <see cref="IsCenterable"/> are true
    /// </summary>
    public int Range => (IsSquare && IsCenterable) ? (Width - 1) / 2 : 0;

    public ViewRange(int range) {
        // A square covering "range" cells in each direction
        Width = range * 2 + 1;
        Height = range * 2 + 1;
    }

    public ViewRange(int w, int h) {
        Width = w; Height = h;
    }

    public ViewRange(string range) {
        string[] split = range.Split("x");

        if (split.Length != 2) throw new Exception($"Invalid view range string \"{range}\"");
        Width = int.Parse(split[0]);
        Height = int.Parse(split[1]);
    }

    public override string ToString() {
        return $"{Width}x{Height}";
    }
}
