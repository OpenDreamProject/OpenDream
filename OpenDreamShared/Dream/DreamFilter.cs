using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace OpenDreamShared.Dream;

/// <summary>
/// An object describing type and vars so the client doesn't have to make a ShaderInstance for shaders with the same params
/// </summary>
[Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
public partial record DreamFilter {
    [ViewVariables(VVAccess.ReadOnly), DataField("type")]
    public string FilterType;

    [ViewVariables(VVAccess.ReadOnly), DataField("name")]
    public string? FilterName;

    private static readonly Dictionary<string, Type> FilterTypes = new() {
        ["alpha"] = typeof(DreamFilterAlpha),
        ["angular_blur"] = typeof(DreamFilterAngularBlur),
        ["bloom"] = typeof(DreamFilterBloom),
        ["blur"] = typeof(DreamFilterBlur),
        ["color"] = typeof(DreamFilterColor),
        ["displace"] = typeof(DreamFilterDisplace),
        ["drop_shadow"] = typeof(DreamFilterDropShadow),
        ["layer"] = typeof(DreamFilterLayer),
        ["motion_blur"] = typeof(DreamFilterMotionBlur),
        ["outline"] = typeof(DreamFilterOutline),
        ["radial_blur"] = typeof(DreamFilterRadialBlur),
        ["rays"] = typeof(DreamFilterRays),
        ["ripple"] = typeof(DreamFilterRipple),
        ["wave"] = typeof(DreamFilterWave),
        ["greyscale"] = typeof(DreamFilterGreyscale),
    };

    public static IEnumerable<Type> AllTypes => FilterTypes.Values;

    public static Type? GetType(string filterType) => FilterTypes.GetValueOrDefault(filterType);

    /// <summary>Whether this filter runs on premultiplied sRGB like BYOND, instead of linear color</summary>
    public virtual bool RendersInSrgb => false;

    /// <summary>
    /// Calculate the size of the canvas this filter needs
    /// </summary>
    /// <param name="baseSize">The size of the canvas before this filter</param>
    /// <param name="textureSizeCallback">A callback that returns the size of a given render source</param>
    public Vector2i CalculateRequiredRenderSpace(Vector2i baseSize, Func<string, Vector2i> textureSizeCallback) {
        Vector2 requiredSpace = baseSize;

        // All the "* 2" in here is because everything is rendered in the center,
        // So every increase in size needs applied to both sides
        switch (this) {
            case DreamFilterAlpha alpha:
                requiredSpace += Vector2.Abs(new(alpha.X, alpha.Y)) * 2;

                if (!string.IsNullOrEmpty(alpha.RenderSource)) {
                    var textureSize = textureSizeCallback(alpha.RenderSource);
                    requiredSpace = Vector2.Max(requiredSpace, textureSize);
                } else if (alpha.Icon != 0) {
                    // TODO
                }

                break;
            case DreamFilterBlur blur:
                requiredSpace += new Vector2(blur.GetExtent()) * 2;
                break;
            case DreamFilterDropShadow dropShadow:
                if (dropShadow.Size - dropShadow.X > 0)
                    requiredSpace.X += (dropShadow.Size + dropShadow.X) * 2;

                if (dropShadow.Size - dropShadow.Y > 0)
                    requiredSpace.Y += (dropShadow.Size + dropShadow.Y) * 2;
                break;
            case DreamFilterMotionBlur motionBlur:
                requiredSpace += new Vector2(motionBlur.GetExtent()) * 2;
                break;
            case DreamFilterOutline outline:
                requiredSpace += new Vector2(outline.Size) * 2;
                break;
        }

        return (Vector2i)requiredSpace;
    }
}

[Serializable, NetSerializable]
public sealed partial record DreamFilterAlpha : DreamFilter {
    [ViewVariables, DataField("x")] public float X;
    [ViewVariables, DataField("y")] public float Y;
    [ViewVariables, DataField("icon")] public int Icon; // Icon resource ID
    [ViewVariables, DataField("render_source")] public string RenderSource = ""; // String that gets special processing in the render loop
    [ViewVariables, DataField("flags")] public short Flags;
}

[Serializable, NetSerializable]
public sealed partial record DreamFilterAngularBlur : DreamFilter {
    [ViewVariables, DataField("x")] public float X;
    [ViewVariables, DataField("y")] public float Y;
    [ViewVariables, DataField("size")] public float Size = 1f;
}

[Serializable, NetSerializable]
public sealed partial record DreamFilterBloom : DreamFilter {
    [ViewVariables, DataField("threshold")] public Color Threshold = Color.Black;
    [ViewVariables, DataField("size")] public float Size = 1f;
    [ViewVariables, DataField("offset")] public float Offset = 1f;
    [ViewVariables, DataField("alpha")] public float Alpha = 255f;
}

[Serializable, NetSerializable]
public sealed partial record DreamFilterBlur : DreamFilter {
    [ViewVariables, DataField("size")] public float Size = 1f;

    /// <summary>The 1D passes of this blur, a horizontal then vertical pass per level</summary>
    public BlurPasses Passes => new(Size);

    public override bool RendersInSrgb => true;

    /// <summary>The radius of the blur, in whole pixels</summary>
    public int GetExtent() {
        int extent = 0;
        foreach (var pass in Passes) {
            if (pass.Direction.Y == 0f) // Only sum the horizontal passes since we'd be double counting due to the paired vertical pass
                extent += Math.Min(pass.Reach, 100);
        }

        return extent;
    }
}

public struct BlurPasses(float size) {
    private const float MaxSizeSquared = BlurKernel.MaxSigma * BlurKernel.MaxSigma;

    private float _size = size;
    private bool _vertical;
    private bool _done = size == 0f || float.IsNaN(size) || float.IsNegativeInfinity(size);

    public BlurPass Current { get; private set; }

    public readonly BlurPasses GetEnumerator() => this;

    public bool MoveNext() {
        if (_vertical) {
            Current = Current with { Direction = Vector2.UnitY };
            _vertical = false;
            return true;
        }

        if (_done)
            return false;

        double length;
        if (_size <= BlurKernel.MaxSigma) {
            // Negative sizes skip planning too, so they're cut off past -6
            length = _size;
            _done = true;
        } else if ((double)_size * _size > MaxSizeSquared * 2) {
            _size = MathF.Min(_size, 100f);
            length = _size;
            _size *= 0.5f;
        } else {
            length = Math.Sqrt(_size * _size - MaxSizeSquared);
            _size = BlurKernel.MaxSigma;
        }

        Current = new(Vector2.UnitX, length);
        _vertical = true;
        return true;
    }
}

[Serializable, NetSerializable]
public sealed partial record DreamFilterColor : DreamFilter {
    [ViewVariables, DataField("color", required: true)] public ColorMatrix Color;
    [ViewVariables, DataField("space")] public float Space; // Default is FILTER_COLOR_RGB = 0
}

[Serializable, NetSerializable]
public sealed partial record DreamFilterDisplace : DreamFilter {
    [ViewVariables, DataField("x")] public float X;
    [ViewVariables, DataField("y")] public float Y;
    [ViewVariables, DataField("size")] public float Size = 1f;
    [ViewVariables, DataField("icon")] public int Icon; // Icon resource ID
    [ViewVariables, DataField("render_source")] public string RenderSource = ""; // String that will require special processing
}

[Serializable, NetSerializable]
public sealed partial record DreamFilterDropShadow : DreamFilter {
    [ViewVariables, DataField("x")] public float X = 1f;
    [ViewVariables, DataField("y")] public float Y = -1f;
    [ViewVariables, DataField("size")] public float Size = 1f;
    [ViewVariables, DataField("offset")] public float Offset;
    [ViewVariables, DataField("color")] public Color Color = Color.Black.WithAlpha(128);
}

[Serializable, NetSerializable]
public sealed partial record DreamFilterLayer : DreamFilter {
    [ViewVariables, DataField("x")] public float X;
    [ViewVariables, DataField("y")] public float Y;
    [ViewVariables, DataField("icon")] public int Icon; // Icon resource ID
    [ViewVariables, DataField("render_source")] public string RenderSource = ""; // String that will require special processing
    [ViewVariables, DataField("flags")] public float Flags; // Default is FILTER_OVERLAY = 0
    [ViewVariables, DataField("color")] public Color Color = Color.Black.WithAlpha(128); // Shit needs to be string or color matrix, because of course one has to be special
    [ViewVariables, DataField("transform")] public Matrix3x2 Transform = Matrix3x2.Identity;
    [ViewVariables, DataField("blend_mode")] public float BlendMode;
}

[Serializable, NetSerializable]
public sealed partial record DreamFilterMotionBlur : DreamFilter {
    [ViewVariables, DataField("x")] public float X;
    [ViewVariables, DataField("y")] public float Y;

    /// <summary>The 1D passes of this blur</summary>
    public MotionBlurPasses Passes => new(X, Y);

    /// <summary>The radius of the blur, in whole pixels, capped so huge blurs don't need huge textures</summary>
    public float GetExtent() {
        const float maxExtent = 512f;

        float extent = 0f;
        foreach (var pass in Passes)
            extent += pass.Reach;

        return MathF.Min(extent, maxExtent);
    }
}

public struct MotionBlurPasses(float x, float y) {
    private const double MaxLengthSquared = BlurKernel.MaxSigma * BlurKernel.MaxSigma;

    private enum Stage { Start, Next, SecondLowQuality, MaxSigma, Remainder, Done }

    // The float/double mix decides which side of sigma 6 a pass falls on
    private float _x = x, _y = y;
    private double _lengthSquared = (double)x * x + (double)y * y;
    private Stage _stage = Stage.Start;

    public BlurPass Current { get; private set; }

    public readonly MotionBlurPasses GetEnumerator() => this;

    public bool MoveNext() {
        switch (_stage) {
            case Stage.Start:
                if (_lengthSquared < 1e-6 || !double.IsFinite(_lengthSquared)) {
                    _stage = Stage.Done;
                    return false;
                }

                goto case Stage.Next;
            case Stage.Next:
                if (_lengthSquared <= MaxLengthSquared) {
                    Current = Pass(_x, _y, _lengthSquared);
                    _stage = Stage.Done;
                } else {
                    Current = Pass(_x, _y);
                    _stage = (_lengthSquared > MaxLengthSquared * 2) ? Stage.SecondLowQuality : Stage.MaxSigma;
                }

                return true;
            case Stage.SecondLowQuality:
                Current = Pass(_x, _y);
                _lengthSquared *= 0.25;
                _x *= 0.5f;
                _y *= 0.5f;
                _stage = Stage.Next;
                return true;
            case Stage.MaxSigma: {
                float toMaxSigma = BlurKernel.MaxSigma / (float)Math.Sqrt(_lengthSquared);
                Current = Pass(_x * toMaxSigma, _y * toMaxSigma);

                double remaining = _lengthSquared - MaxLengthSquared;
                float shrink = (float)Math.Sqrt(remaining / _lengthSquared);
                _x *= shrink;
                _y *= shrink;
                _lengthSquared = remaining;
                _stage = Stage.Remainder;
                return true;
            }
            case Stage.Remainder:
                Current = Pass(_x, _y, _lengthSquared);
                _stage = Stage.Done;
                return true;
            default:
                return false;
        }
    }

    private static BlurPass Pass(float x, float y) => Pass(x, y, (double)x * x + (double)y * y);

    private static BlurPass Pass(float x, float y, double lengthSquared) {
        double length = Math.Sqrt(lengthSquared);
        return new(new Vector2(x, y) / (float)length, length);
    }
}

[Serializable, NetSerializable]
public sealed partial record DreamFilterOutline : DreamFilter {
    [ViewVariables, DataField("size")] public float Size = 1f;
    [ViewVariables, DataField("color")] public Color Color = Color.Black;
    [ViewVariables, DataField("flags")] public float Flags;
}

[Serializable, NetSerializable]
public sealed partial record DreamFilterRadialBlur : DreamFilter {
    [ViewVariables, DataField("x")] public float X;
    [ViewVariables, DataField("y")] public float Y;
    [ViewVariables, DataField("size")] public float Size = 0.01f;
}

[Serializable, NetSerializable]
public sealed partial record DreamFilterRays : DreamFilter {
    [ViewVariables, DataField("x")] public float X;
    [ViewVariables, DataField("y")] public float Y;
    [ViewVariables, DataField("size")] public float Size = 16f; // Defaults to half tile width
    [ViewVariables, DataField("color")] public Color Color = Color.White;
    [ViewVariables, DataField("offset")] public float Offset;
    [ViewVariables, DataField("density")] public float Density = 10f;
    [ViewVariables, DataField("threshold")] public float Threshold = 0.5f;
    [ViewVariables, DataField("factor")] public float Factor;
    [ViewVariables, DataField("flags")] public float Flags = 3f; // Defaults to FILTER_OVERLAY | FILTER_UNDERLAY
}

[Serializable, NetSerializable]
public sealed partial record DreamFilterRipple : DreamFilter {
    [ViewVariables, DataField("x")] public float X;
    [ViewVariables, DataField("y")] public float Y;
    [ViewVariables, DataField("size")] public float Size = 1f;
    [ViewVariables, DataField("repeat")] public float Repeat = 2f;
    [ViewVariables, DataField("radius")] public float Radius;
    [ViewVariables, DataField("falloff")] public float Falloff = 1f;
    [ViewVariables, DataField("flags")] public float Flags;
}

[Serializable, NetSerializable]
public sealed partial record DreamFilterWave : DreamFilter {
    [ViewVariables, DataField("x")] public float X;
    [ViewVariables, DataField("y")] public float Y;
    [ViewVariables, DataField("size")] public float Size = 1f;
    [ViewVariables, DataField("offset")] public float Offset;
    [ViewVariables, DataField("flags")] public float Flags;
}

[Serializable, NetSerializable]
public sealed partial record DreamFilterGreyscale : DreamFilter;
