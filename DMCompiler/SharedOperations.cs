using System.Runtime.CompilerServices;

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
}
