using System.Runtime.CompilerServices;
using Robust.Shared.Maths;

namespace DMCompiler;

/// <summary>
/// A class containing operations used by both the compiler and the server.
/// Helps make sure things like sin() and cos() give the same result on both.
/// </summary>
public static class SharedOperations {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sin(float x) {
        return MathF.Sin(x / 180 * MathF.PI);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cos(float x) {
        return MathF.Cos(x / 180 * MathF.PI);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Tan(float x) {
        return MathF.Tan(x / 180 * MathF.PI);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ArcSin(float x) {
        return MathF.Asin(x) / MathF.PI * 180;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ArcCos(float x) {
        return MathF.Acos(x) / MathF.PI * 180;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ArcTan(float a) {
        return MathF.Atan(a) / MathF.PI * 180;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ArcTan(float x, float y) {
        return MathF.Atan2(y, x) / MathF.PI * 180;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sqrt(float a) {
        return MathF.Sqrt(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Log(float y) {
        return MathF.Log(y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Log(float y, float baseValue) {
        return MathF.Log(y, baseValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Abs(float a) {
        return MathF.Abs(a);
    }

    //because BYOND has everything as a 32 bit float with 8 bit mantissa, we need to chop off the
    //top 8 bits when bit shifting for parity
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BitShiftLeft(int left, int right) {
        return (left << right) & 0x00FFFFFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BitShiftRight(int left, int right) {
        return (left & 0x00FFFFFF) >> (right) ;
    }

    public enum ColorSpace {
        RGB = 0,
        HSV = 1,
        HSL = 2
    }

    public static string ParseRgb((string? Name, float? Value)[] arguments) {
        string result;
        float? color1 = null;
        float? color2 = null;
        float? color3 = null;
        float? a = null;
        ColorSpace space = ColorSpace.RGB;

        if (arguments[0].Name is null) {
            if (arguments.Length is < 3 or > 5)
                throw new Exception("Expected 3 to 5 arguments for rgb()");

            color1 = arguments[0].Value;
            color2 = arguments[1].Value;
            color3 = arguments[2].Value;
            a = (arguments.Length >= 4) ? arguments[3].Value : null;
            if (arguments.Length == 5)
                space = arguments[4].Value is null ? ColorSpace.RGB : (ColorSpace)(int)arguments[4].Value!;
        } else {
            foreach (var arg in arguments) {
                var name = arg.Name ?? string.Empty;

                if (name.StartsWith("r", StringComparison.InvariantCultureIgnoreCase) && color1 is null) {
                    color1 = arg.Value;
                    space = ColorSpace.RGB;
                } else if (name.StartsWith("g", StringComparison.InvariantCultureIgnoreCase) && color2 is null) {
                    color2 = arg.Value;
                    space = ColorSpace.RGB;
                } else if (name.StartsWith("b", StringComparison.InvariantCultureIgnoreCase) && color3 is null) {
                    color3 = arg.Value;
                    space = ColorSpace.RGB;
                } else if (name.StartsWith("h", StringComparison.InvariantCultureIgnoreCase) && color1 is null) {
                    color1 = arg.Value;
                    space = ColorSpace.HSV;
                } else if (name.StartsWith("s", StringComparison.InvariantCultureIgnoreCase) && color2 is null) {
                    color2 = arg.Value;
                    space = ColorSpace.HSV;
                } else if (name.StartsWith("v", StringComparison.InvariantCultureIgnoreCase) && color3 is null) {
                    color3 = arg.Value;
                    space = ColorSpace.HSV;
                } else if (name.StartsWith("l", StringComparison.InvariantCultureIgnoreCase) && color3 is null) {
                    color3 = arg.Value;
                    space = ColorSpace.HSL;
                } else if (name.StartsWith("a", StringComparison.InvariantCultureIgnoreCase) && a is null)
                    a = arg.Value;
                else if (name == "space" && space == default)
                    space = (ColorSpace)(int)arg.Value!;
                else
                    throw new Exception($"Invalid or double arg \"{name}\"");
            }
        }

        color1 ??= 0;
        color2 ??= 0;
        color3 ??= 0;
        byte aValue = a is null ? (byte)255 : (byte)Math.Clamp((int)a, 0, 255);
        Color color;

        switch (space) {
            case ColorSpace.RGB: {
                byte r = (byte)Math.Clamp(color1.Value, 0, 255);
                byte g = (byte)Math.Clamp(color2.Value, 0, 255);
                byte b = (byte)Math.Clamp(color3.Value, 0, 255);

                color = new Color(r, g, b, aValue);
                break;
            }
            case ColorSpace.HSV: {
                // TODO: Going beyond the max defined in the docs returns a different value. Don't know why.
                float h = Math.Clamp(color1.Value, 0, 360) / 360f;
                float s = Math.Clamp(color2.Value, 0, 100) / 100f;
                float v = Math.Clamp(color3.Value, 0, 100) / 100f;

                color = Color.FromHsv(new(h, s, v, aValue / 255f));
                break;
            }
            case ColorSpace.HSL: {
                float h = Math.Clamp(color1.Value, 0, 360) / 360f;
                float s = Math.Clamp(color2.Value, 0, 100) / 100f;
                float l = Math.Clamp(color3.Value, 0, 100) / 100f;

                color = Color.FromHsl(new(h, s, l, aValue / 255f));
                break;
            }
            default:
                throw new Exception($"Unimplemented color space {space}");
        }

        // TODO: There is a difference between passing null and not passing a fourth arg at all
        if (a is null) {
            result = $"#{color.RByte:X2}{color.GByte:X2}{color.BByte:X2}".ToLower();
        } else {
            result = $"#{color.RByte:X2}{color.GByte:X2}{color.BByte:X2}{color.AByte:X2}".ToLower();
        }

        return result;
    }
}
