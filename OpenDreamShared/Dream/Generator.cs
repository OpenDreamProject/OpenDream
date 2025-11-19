using System;
using System.Numerics;
using Robust.Shared.Random;

namespace OpenDreamShared.Dream;

public enum GeneratorDistribution {
    Constant,
    Uniform,
    Normal,
    Linear,
    Square
}

public interface IGenerator {
    public static float GenerateNum(IRobustRandom random, float low, float high, GeneratorDistribution distribution) {
        return distribution switch {
            GeneratorDistribution.Constant => high,
            GeneratorDistribution.Uniform => random.NextFloat(low, high),
            GeneratorDistribution.Normal => (float)Math.Clamp(random.NextGaussian((low + high) / 2f, (high - low) / 6f), low, high),
            GeneratorDistribution.Linear => MathF.Sqrt(random.NextFloat(0f, 1f)) * (high - low) + low,
            GeneratorDistribution.Square => MathF.Cbrt(random.NextFloat(0f, 1f)) * (high - low) + low,
            _ => throw new ArgumentOutOfRangeException(nameof(distribution), distribution, null)
        };
    }
}

public interface IGeneratorNum : IGenerator {
    public float Generate(IRobustRandom random);
}

public interface IGeneratorVector : IGenerator {
    public Vector2 GenerateVector2(IRobustRandom random);
    public Vector3 GenerateVector3(IRobustRandom random);
}

public sealed class GeneratorNum(float low, float high, GeneratorDistribution distribution) : IGeneratorNum, IGeneratorVector {
    public GeneratorNum(float value) : this(value, value, GeneratorDistribution.Constant) { }

    public float Generate(IRobustRandom random) {
        return IGenerator.GenerateNum(random, low, high, distribution);
    }

    public Vector2 GenerateVector2(IRobustRandom random) {
        return new Vector2(Generate(random));
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        return new Vector3(Generate(random));
    }
}

public sealed class GeneratorVector2(Vector2 low, Vector2 high, GeneratorDistribution distribution) : IGeneratorVector {
    public GeneratorVector2(Vector2 value) : this(value, value, GeneratorDistribution.Constant) { }

    public Vector2 GenerateVector2(IRobustRandom random) {
        return Vector2.Lerp(low, high, IGenerator.GenerateNum(random, 0f, 1f, distribution));
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        var vector = GenerateVector2(random);

        return new Vector3(vector.X, vector.Y, 0f);
    }
}

public sealed class GeneratorVector3(Vector3 low, Vector3 high, GeneratorDistribution distribution) : IGeneratorVector {
    public GeneratorVector3(Vector3 value) : this(value, value, GeneratorDistribution.Constant) { }

    public Vector2 GenerateVector2(IRobustRandom random) {
        var vector = GenerateVector3(random);

        return new Vector2(vector.X, vector.Y);
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        return Vector3.Lerp(low, high, IGenerator.GenerateNum(random, 0f, 1f, distribution));
    }
}

public sealed class GeneratorBox2(Vector2 low, Vector2 high, GeneratorDistribution distribution) : IGeneratorVector {
    public Vector2 GenerateVector2(IRobustRandom random) {
        var x = IGenerator.GenerateNum(random, low.X, high.X, distribution);
        var y = IGenerator.GenerateNum(random, low.Y, high.Y, distribution);

        return new Vector2(x, y);
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        var vector = GenerateVector2(random);

        return new Vector3(vector.X, vector.Y, 0f);
    }
}

public sealed class GeneratorBox3(Vector3 low, Vector3 high, GeneratorDistribution distribution) : IGeneratorVector {
    public Vector2 GenerateVector2(IRobustRandom random) {
        var vector = GenerateVector3(random);

        return new Vector2(vector.X, vector.Y);
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        var x = IGenerator.GenerateNum(random, low.X, high.X, distribution);
        var y = IGenerator.GenerateNum(random, low.Y, high.Y, distribution);
        var z = IGenerator.GenerateNum(random, low.Z, high.Z, distribution);

        return new Vector3(x, y, z);
    }
}

public sealed class GeneratorCircle(float low, float high, GeneratorDistribution distribution) : IGeneratorVector {
    public Vector2 GenerateVector2(IRobustRandom random) {
        var theta = random.NextFloat(0f, 360f);
        var r = IGenerator.GenerateNum(random, low, high, distribution);

        return new Vector2(MathF.Cos(theta) * r, MathF.Sin(theta) * r);
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        var vector = GenerateVector2(random);

        return new Vector3(vector.X, vector.Y, 0f);
    }
}

public sealed class GeneratorSphere(float low, float high, GeneratorDistribution distribution) : IGeneratorVector {
    public Vector2 GenerateVector2(IRobustRandom random) {
        var vector = GenerateVector3(random);

        return new Vector2(vector.X, vector.Y);
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        var theta = random.NextFloat(0f, 360f);
        var phi = random.NextFloat(0f, 180f);
        var r = IGenerator.GenerateNum(random, low, high, distribution);

        return new Vector3(
            MathF.Cos(theta) * MathF.Sin(phi) * r,
            MathF.Sin(theta) * MathF.Sin(phi) * r,
            MathF.Cos(phi) * r
        );
    }
}

public sealed class GeneratorSquare(Vector2 low, Vector2 high, GeneratorDistribution distribution) : IGeneratorVector {
    public Vector2 GenerateVector2(IRobustRandom random) {
        var x = IGenerator.GenerateNum(random, -high.X, high.X, distribution);
        var y = IGenerator.GenerateNum(random, -high.Y, high.Y, distribution);

        if (MathF.Abs(x) < low.X)
            y = random.NextByte() > 128
                ? IGenerator.GenerateNum(random, -high.Y, -low.Y, distribution)
                : IGenerator.GenerateNum(random, low.Y, high.Y, distribution);

        return new(x, y);
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        var vector = GenerateVector2(random);

        return new Vector3(vector.X, vector.Y, 0f);
    }
}

public sealed class GeneratorCube(Vector3 low, Vector3 high, GeneratorDistribution distribution) : IGeneratorVector {
    public Vector2 GenerateVector2(IRobustRandom random) {
        var vector = GenerateVector3(random);

        return new Vector2(vector.X, vector.Y);
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        var x = IGenerator.GenerateNum(random, -high.X, high.X, distribution);
        var y = IGenerator.GenerateNum(random, -high.Y, high.Y, distribution);
        var z = IGenerator.GenerateNum(random, -high.Z, high.Z, distribution);

        if (MathF.Abs(x) < low.X)
            y = random.NextByte() > 128
                ? IGenerator.GenerateNum(random, -high.Y, -low.Y, distribution)
                : IGenerator.GenerateNum(random, low.Y, high.Y, distribution);
        if (MathF.Abs(y) < low.Y)
            z = random.NextByte() > 128
                ? IGenerator.GenerateNum(random, -high.Z, -low.Z, distribution)
                : IGenerator.GenerateNum(random, low.Z, high.Z, distribution);

        return new(x, y, z);
    }
}
