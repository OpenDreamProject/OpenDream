using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;

namespace OpenDreamShared.Dream;

/// <summary>
/// This is supposed to be used to describe a string that represents a range of tiles, like "11x4" or whatever. <br/>
/// Used as a possible argument for some functionality, like world.view or orange()
/// </summary>
public readonly struct ViewRange {
    public readonly int Width, Height;
    public int BiggestAxis => Math.Max(Width, Height);

    public int CenterX => Width / 2;
    public int CenterY => Height / 2;
    public Vector2i Center => (CenterX, CenterY);

    public bool IsSquare => Width == Height;
    public bool IsCenterable => (Width % 2 == 1) && (Height % 2 == 1);

    [MemberNotNullWhen(true, nameof(SquareRange))]
    public bool CanSquareRange => IsSquare && IsCenterable;

    /// <summary>
    /// The distance this ViewRange covers in every direction
    /// if <see cref="CanSquareRange"/> is true
    /// </summary>
    public int? SquareRange => CanSquareRange ? (Width - 1) / 2 : null;

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
