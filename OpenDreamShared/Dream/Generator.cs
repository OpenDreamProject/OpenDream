using System;
using System.Numerics;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace OpenDreamShared.Dream;

public enum GeneratorDistribution {
    Constant,
    Uniform,
    Normal,
    Linear,
    Square
}

public enum GeneratorOperation {
    Add,
    Subtract,
    Multiply,
    Divide,
    Power
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

    public static float Operate(GeneratorOperation operation, float left, float right) {
        return operation switch {
            GeneratorOperation.Add => left + right,
            GeneratorOperation.Subtract => left - right,
            GeneratorOperation.Multiply => left * right,
            GeneratorOperation.Divide => left / right,
            GeneratorOperation.Power => MathF.Pow(left, right),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }

    public static Vector3 Operate(GeneratorOperation operation, Vector3 left, Vector3 right) {
        return new Vector3(
            Operate(operation, left.X, right.X),
            Operate(operation, left.Y, right.Y),
            Operate(operation, left.Z, right.Z)
        );
    }

    /// <summary>
    /// Samples a generator being used where a single number is expected.
    /// </summary>
    /// <remarks>
    /// BYOND reduces a vector-producing operand down to its last component here (Y when 2D, Z when 3D).
    /// </remarks>
    public static float GenerateScalar(IGenerator generator, IRobustRandom random) {
        return generator switch {
            IGeneratorNum num => num.Generate(random),
            IGeneratorVector vector => vector.PrefersVector3
                ? vector.GenerateVector3(random).Z
                : vector.GenerateVector2(random).Y,
            _ => 0f
        };
    }

    /// <summary>
    /// Samples a generator being used where a vector is expected.
    /// </summary>
    /// <remarks>A number is broadcast to every component, matching BYOND.</remarks>
    public static Vector3 GenerateVector(IGenerator generator, IRobustRandom random) {
        return generator switch {
            IGeneratorVector vector => vector.GenerateVector3(random),
            IGeneratorNum num => new Vector3(num.Generate(random)),
            _ => Vector3.Zero
        };
    }

    /// <summary>
    /// Applies a color matrix to a vector, mapping x,y,z onto red,green,blue
    /// </summary>
    public static Vector3 Transform(in ColorMatrix matrix, Vector3 vector) {
        return new Vector3(
            matrix.c11 * vector.X + matrix.c21 * vector.Y + matrix.c31 * vector.Z + matrix.c51,
            matrix.c12 * vector.X + matrix.c22 * vector.Y + matrix.c32 * vector.Z + matrix.c52,
            matrix.c13 * vector.X + matrix.c23 * vector.Y + matrix.c33 * vector.Z + matrix.c53
        );
    }
}

public interface IGeneratorNum : IGenerator {
    public float Generate(IRobustRandom random);
}

public interface IGeneratorVector : IGenerator {
    bool PrefersVector3 { get; set; }
    public Vector2 GenerateVector2(IRobustRandom random);
    public Vector3 GenerateVector3(IRobustRandom random);
}

[Serializable, NetSerializable]
public sealed class GeneratorNum(float low, float high, GeneratorDistribution distribution) : IGeneratorNum, IGeneratorVector {
    public bool PrefersVector3 { get; set; } = false;
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

    public override string ToString() {
        return $"generator(\"num\", {low}, {high}, {distribution})";
    }
}

[Serializable, NetSerializable]
public sealed class GeneratorVector2(Vector2 low, Vector2 high, GeneratorDistribution distribution) : IGeneratorVector {
    public bool PrefersVector3 { get; set; } = false;
    public GeneratorVector2(Vector2 value) : this(value, value, GeneratorDistribution.Constant) { }

    public Vector2 GenerateVector2(IRobustRandom random) {
        return Vector2.Lerp(low, high, IGenerator.GenerateNum(random, 0f, 1f, distribution));
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        var vector = GenerateVector2(random);

        return new Vector3(vector.X, vector.Y, 0f);
    }

    public override string ToString() {
        return $"generator(\"vector\", {low}, {high}, {distribution})";
    }
}

[Serializable, NetSerializable]
public sealed class GeneratorVector3(Vector3 low, Vector3 high, GeneratorDistribution distribution) : IGeneratorVector {
    public bool PrefersVector3 { get; set; } = true;
    public GeneratorVector3(Vector3 value) : this(value, value, GeneratorDistribution.Constant) { }

    public Vector2 GenerateVector2(IRobustRandom random) {
        var vector = GenerateVector3(random);

        return new Vector2(vector.X, vector.Y);
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        return Vector3.Lerp(low, high, IGenerator.GenerateNum(random, 0f, 1f, distribution));
    }

    public override string ToString() {
        return $"generator(\"vector\", {low}, {high}, {distribution})";
    }
}

[Serializable, NetSerializable]
public sealed class GeneratorBox2(Vector2 low, Vector2 high, GeneratorDistribution distribution) : IGeneratorVector {
    public bool PrefersVector3 { get; set; } = false;

    public Vector2 GenerateVector2(IRobustRandom random) {
        var x = IGenerator.GenerateNum(random, low.X, high.X, distribution);
        var y = IGenerator.GenerateNum(random, low.Y, high.Y, distribution);

        return new Vector2(x, y);
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        var vector = GenerateVector2(random);

        return new Vector3(vector.X, vector.Y, 0f);
    }

    public override string ToString() {
        return $"generator(\"box\", {low}, {high}, {distribution})";
    }
}

[Serializable, NetSerializable]
public sealed class GeneratorBox3(Vector3 low, Vector3 high, GeneratorDistribution distribution) : IGeneratorVector {
    public bool PrefersVector3 { get; set; } = true;

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

    public override string ToString() {
        return $"generator(\"box\", {low}, {high}, {distribution})";
    }
}

[Serializable, NetSerializable]
public sealed class GeneratorCircle(float low, float high, GeneratorDistribution distribution) : IGeneratorVector {
    public bool PrefersVector3 { get; set; } = false;

    public Vector2 GenerateVector2(IRobustRandom random) {
        var theta = random.NextFloat(0f, 360f);
        var r = IGenerator.GenerateNum(random, low, high, distribution);

        return new Vector2(MathF.Cos(theta) * r, MathF.Sin(theta) * r);
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        var vector = GenerateVector2(random);

        return new Vector3(vector.X, vector.Y, 0f);
    }

    public override string ToString() {
        return $"generator(\"circle\", {low}, {high}, {distribution})";
    }
}

[Serializable, NetSerializable]
public sealed class GeneratorSphere(float low, float high, GeneratorDistribution distribution) : IGeneratorVector {
    public bool PrefersVector3 { get; set; } = true;

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

    public override string ToString() {
        return $"generator(\"sphere\", {low}, {high}, {distribution})";
    }
}

[Serializable, NetSerializable]
public sealed class GeneratorSquare(Vector2 low, Vector2 high, GeneratorDistribution distribution) : IGeneratorVector {
    public bool PrefersVector3 { get; set; } = false;

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

    public override string ToString() {
        return $"generator(\"square\", {low}, {high}, {distribution})";
    }
}

/// <summary>
/// The result of combining a number-producing generator with another generator or value
/// </summary>
[Serializable, NetSerializable]
public sealed class GeneratorArithmeticNum(IGenerator left, IGenerator right, GeneratorOperation operation) : IGeneratorNum, IGeneratorVector {
    public bool PrefersVector3 { get; set; } = false;

    public float Generate(IRobustRandom random) {
        return IGenerator.Operate(operation, IGenerator.GenerateScalar(left, random), IGenerator.GenerateScalar(right, random));
    }

    public Vector2 GenerateVector2(IRobustRandom random) {
        return new Vector2(Generate(random));
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        return new Vector3(Generate(random));
    }

    public override string ToString() {
        return $"{left} {OperationToString(operation)} {right}";
    }

    internal static string OperationToString(GeneratorOperation operation) {
        return operation switch {
            GeneratorOperation.Add => "+",
            GeneratorOperation.Subtract => "-",
            GeneratorOperation.Multiply => "*",
            GeneratorOperation.Divide => "/",
            GeneratorOperation.Power => "**",
            _ => "?"
        };
    }
}

/// <summary>
/// The result of combining a vector-producing generator with another generator or value
/// </summary>
/// <remarks>These always produce a 3D vector, even when both operands are 2D. That's what BYOND does.</remarks>
[Serializable, NetSerializable]
public sealed class GeneratorArithmeticVector(IGenerator left, IGenerator right, GeneratorOperation operation) : IGeneratorVector {
    public bool PrefersVector3 { get; set; } = true;

    public Vector2 GenerateVector2(IRobustRandom random) {
        var vector = GenerateVector3(random);

        return new Vector2(vector.X, vector.Y);
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        return IGenerator.Operate(operation, IGenerator.GenerateVector(left, random), IGenerator.GenerateVector(right, random));
    }

    public override string ToString() {
        return $"{left} {GeneratorArithmeticNum.OperationToString(operation)} {right}";
    }
}

/// <summary>
/// The result of multiplying a vector-producing generator by a matrix
/// </summary>
[Serializable, NetSerializable]
public sealed class GeneratorMatrixTransform(IGenerator inner, ColorMatrix matrix) : IGeneratorVector {
    public bool PrefersVector3 { get; set; } = true;

    public Vector2 GenerateVector2(IRobustRandom random) {
        var vector = GenerateVector3(random);

        return new Vector2(vector.X, vector.Y);
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        return IGenerator.Transform(matrix, IGenerator.GenerateVector(inner, random));
    }

    public override string ToString() {
        return $"{inner} * matrix";
    }
}

/// <summary>
/// The result of <see langword="/generator/proc/Turn"/>, rotating a generated vector in the XY plane
/// </summary>
[Serializable, NetSerializable]
public sealed class GeneratorTurn(IGenerator inner, float angle) : IGeneratorVector {
    public bool PrefersVector3 { get; set; } = true;

    public Vector2 GenerateVector2(IRobustRandom random) {
        var vector = GenerateVector3(random);

        return new Vector2(vector.X, vector.Y);
    }

    public Vector3 GenerateVector3(IRobustRandom random) {
        var vector = IGenerator.GenerateVector(inner, random);
        var (sin, cos) = MathF.SinCos(angle * MathF.PI / 180f);

        return new Vector3(
            cos * vector.X - sin * vector.Y,
            sin * vector.X + cos * vector.Y,
            vector.Z
        );
    }

    public override string ToString() {
        return $"turn({inner}, {angle})";
    }
}

[Serializable, NetSerializable]
public sealed class GeneratorCube(Vector3 low, Vector3 high, GeneratorDistribution distribution) : IGeneratorVector {
    public bool PrefersVector3 { get; set; } = true;

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

    public override string ToString() {
        return $"generator(\"cube\", {low}, {high}, {distribution})";
    }
}
