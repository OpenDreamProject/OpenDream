using System;
using System.Numerics;

namespace OpenDreamShared.Dream;

/// <summary>A 1D Gaussian kernel using linear sampling, so each tap after the center covers two texels</summary>
public readonly struct BlurKernel {
    public const int MaxTaps = 10;

    /// <summary>The largest sigma a single pass supports</summary>
    public const float MaxSigma = 6f;

    public readonly int Taps;

    private readonly double _falloff;
    private readonly double _normalization;
    private readonly double _sum;

    public BlurKernel(float sigma) {
        const int maxRadius = MaxTaps * 2 - 1;

        double variance = (double)sigma * sigma;
        _falloff = 0.5 / variance;
        _normalization = 1.0 / Math.Sqrt(variance * 2.0 * Math.PI);

        // Truncate the kernel at the first odd offset with a negligible weight
        _sum = _normalization;
        int radius = 1;
        for (; radius < maxRadius; radius++) {
            double weight = Math.Exp(-(radius * radius) * _falloff) * _normalization;
            if ((radius & 1) == 1 && weight < 0.001)
                break;

            _sum += 2 * weight;
        }

        Taps = (radius + 1) / 2;
    }

    public float GetWeight(int tap) {
        if (tap == 0)
            return (float)(_normalization / _sum);

        return (float)(GetDiscreteWeight(tap * 2) + GetDiscreteWeight(tap * 2 - 1));
    }

    public float GetOffset(int tap) {
        if (tap == 0)
            return 0f;

        int near = tap * 2 - 1;
        double far = GetDiscreteWeight(near + 1);
        return (float)(far / (far + GetDiscreteWeight(near)) + near);
    }

    private double GetDiscreteWeight(int distance) {
        return Math.Exp(-(distance * distance) * _falloff) * _normalization / _sum;
    }
}

/// <summary>A single 1D pass of a blur filter</summary>
public readonly record struct BlurPass(Vector2 Direction, double Length) {
    /// <summary>Sigmas past <see cref="BlurKernel.MaxSigma"/> stretch the largest kernel instead</summary>
    public bool IsLowQuality => Length > BlurKernel.MaxSigma;

    public BlurKernel Kernel => new(IsLowQuality ? BlurKernel.MaxSigma : (float)Length);

    public float Spread => IsLowQuality ? (float)Length / 7f : 1f;

    public int Reach {
        get {
            var kernel = Kernel;

            return (int)MathF.Ceiling(kernel.GetOffset(kernel.Taps - 1) * Spread);
        }
    }
}
