using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.Native;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectGenerator(DreamObjectDefinition objectDefinition) : DreamObject(objectDefinition) {
    public IGenerator Generator { get; private set; } = default!;

    public override void Initialize(DreamProcArguments args) {
        var type = args.GetArgument(0);
        if (!type.TryGetValueAsString(out var typeStr))
            throw new Exception($"Invalid generator type {type}");

        var a = args.GetArgument(1);
        var b = args.GetArgument(2);
        var distribution = DistributionNumberToEnum((int)args.GetArgument(3).UnsafeGetValueAsFloat());

        switch (typeStr) {
            case "num":
            case "circle":
            case "sphere": {
                var left = a.UnsafeGetValueAsFloat();
                var right = (b.Type == DreamValue.DreamValueType.Float) ? b.UnsafeGetValueAsFloat() : 1f;
                var (low, high) = left <= right ? (left, right) : (right, left);

                Generator = typeStr switch {
                    "num" => new GeneratorNum(low, high, distribution),
                    "circle" => new GeneratorCircle(low, high, distribution),
                    "sphere" => new GeneratorSphere(low, high, distribution),
                    _ => throw new ArgumentOutOfRangeException()
                };

                break;
            }
            case "vector":
            case "box": {
                var low = DreamObjectVector.CreateFromValue(a, ObjectTree);
                var high = DreamObjectVector.CreateFromValue(b, ObjectTree);

                if (low.Is3D || high.Is3D)
                    Generator = typeStr == "vector"
                        ? new GeneratorVector3(low.AsVector3, high.AsVector3, distribution)
                        : new GeneratorBox3(low.AsVector3, high.AsVector3, distribution);
                else
                    Generator = typeStr == "vector"
                        ? new GeneratorVector2(low.AsVector2, high.AsVector2, distribution)
                        : new GeneratorBox2(low.AsVector2, high.AsVector2, distribution);

                low.DecRef();
                high.DecRef();
                break;
            }
            case "square":
            case "cube": {
                var low = DreamObjectVector.CreateFromValue(a, ObjectTree);
                var high = DreamObjectVector.CreateFromValue(b, ObjectTree);

                try {
                    Generator = typeStr switch {
                        "square" => new GeneratorSquare(low.AsVector2, high.AsVector2, distribution),
                        "cube" => new GeneratorCube(low.AsVector3, high.AsVector3, distribution),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    break;
                } finally {
                    low.DecRef();
                    high.DecRef();
                }
            }
            default:
                throw new Exception($"Invalid generator type {type}");
        }
    }

    public T RequireType<T>() where T : IGenerator {
        if (Generator is not T casted)
            throw new Exception($"Expected generator type {typeof(T)} but got {Generator.GetType()}");

        return casted;
    }

    /// <summary>
    /// Rotates the vectors this generator produces, in-place.
    /// </summary>
    /// <remarks>
    /// A no-op for a generator producing numbers, matching BYOND.
    /// </remarks>
    public void Turn(float angle) {
        if (Generator is IGeneratorNum)
            return;

        Generator = new GeneratorTurn(Generator, angle);
    }

    #region Operators

    public override DreamValue OperatorAdd(DreamValue b, DMProcState state) {
        if (TryOperate(b, GeneratorOperation.Add, out var result))
            return result;

        return base.OperatorAdd(b, state);
    }

    public override DreamValue OperatorSubtract(DreamValue b, DMProcState state) {
        if (TryOperate(b, GeneratorOperation.Subtract, out var result))
            return result;

        return base.OperatorSubtract(b, state);
    }

    public override DreamValue OperatorMultiply(DreamValue b, DMProcState state) {
        if (TryOperate(b, GeneratorOperation.Multiply, out var result))
            return result;

        return base.OperatorMultiply(b, state);
    }

    public override DreamValue OperatorMultiplyRef(DreamValue b, DMProcState state) {
        return OperatorMultiply(b, state);
    }

    /// <remarks>
    /// BYOND only accepts a plain number here; dividing by a generator, vector or matrix is an
    /// "Undefined operation" runtime error over there.
    /// </remarks>
    public override DreamValue OperatorDivide(DreamValue b, DMProcState state) {
        if (!b.TryGetValueAsFloat(out var divisor))
            return base.OperatorDivide(b, state);
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide a generator by zero");

        return Combine(new GeneratorNum(divisor), GeneratorOperation.Divide);
    }

    public override DreamValue OperatorDivideRef(DreamValue b, DMProcState state) {
        return OperatorDivide(b, state);
    }

    public override DreamValue OperatorPower(DreamValue b, DMProcState state) {
        if (!b.TryGetValueAsFloat(out var exponent))
            return base.OperatorPower(b, state);

        return Combine(new GeneratorNum(exponent), GeneratorOperation.Power);
    }

    // -x, equivalent to multiplying by -1
    public override DreamValue OperatorNegate(DMProcState state) {
        return Combine(new GeneratorNum(-1f), GeneratorOperation.Multiply);
    }

    public override DreamValue OperatorAppend(DreamValue b) {
        if (TryOperate(b, GeneratorOperation.Add, out var result))
            return result;

        return base.OperatorAppend(b);
    }

    public override DreamValue OperatorRemove(DreamValue b) {
        if (TryOperate(b, GeneratorOperation.Subtract, out var result))
            return result;

        return base.OperatorRemove(b);
    }

    #endregion Operators

    /// <summary>
    /// Combines this generator with another generator or value, producing a new generator
    /// </summary>
    /// <returns>false if the given value can't be used as an operand</returns>
    private bool TryOperate(DreamValue b, GeneratorOperation operation, [MustDisposeResource] out DreamValue result) {
        // Multiplying a vector by a matrix transforms it. Every other combination treats the matrix as a plain value.
        if (operation == GeneratorOperation.Multiply && Generator is not IGeneratorNum && TryGetTransform(b, out var transform)) {
            result = new DreamValue(CreateChild(new GeneratorMatrixTransform(Generator, transform)));
            return true;
        }

        if (!TryCreateOperand(b, out var operand)) {
            result = DreamValue.Null;
            return false;
        }

        result = Combine(operand, operation);
        return true;
    }

    [MustDisposeResource]
    private DreamValue Combine(IGenerator operand, GeneratorOperation operation) {
        // The left-hand side decides whether the result is a number or a vector
        IGenerator combined = Generator is IGeneratorNum
            ? new GeneratorArithmeticNum(Generator, operand, operation)
            : new GeneratorArithmeticVector(Generator, operand, operation);

        return new DreamValue(CreateChild(combined));
    }

    private DreamObjectGenerator CreateChild(IGenerator generator) {
        return new DreamObjectGenerator(ObjectDefinition) {
            Generator = generator
        };
    }

    /// <summary>
    /// Interprets a value as something a generator can be combined with
    /// </summary>
    private bool TryCreateOperand(DreamValue value, [NotNullWhen(true)] out IGenerator? operand) {
        if (value.IsNull) { // BYOND treats null as 0 here
            operand = new GeneratorNum(0f);
            return true;
        }

        if (value.TryGetValueAsFloat(out var number)) {
            operand = new GeneratorNum(number);
            return true;
        }

        if (value.TryGetValueAsDreamObject<DreamObjectGenerator>(out var generator)) {
            operand = generator.Generator;
            return true;
        }

        if (value.TryGetValueAsDreamObject<DreamObjectVector>(out var vector)) {
            operand = CreateConstant(vector);
            return true;
        }

        // A matrix used as a plain value is the matrix applied to (1,1,1)
        if (TryGetTransform(value, out var transform)) {
            operand = new GeneratorVector3(IGenerator.Transform(transform, Vector3.One));
            return true;
        }

        if (value.TryGetValueAsDreamList(out var list) && list.GetLength() is 2 or 3 &&
            DreamObjectVector.TryCreateFromValue(value, ObjectTree, out var listVector)) {
            operand = CreateConstant(listVector);
            listVector.DecRef();
            return true;
        }

        operand = null;
        return false;
    }

    /// <summary>
    /// Interprets a value as a matrix to transform a vector by, either a /matrix or a color matrix
    /// </summary>
    private static bool TryGetTransform(DreamValue value, out ColorMatrix matrix) {
        if (value.TryGetValueAsDreamObject<DreamObjectMatrix>(out var transform)) {
            // A /matrix is a 2D transform; it leaves nothing behind on the Z axis
            matrix = new ColorMatrix(
                transform.A, transform.D, 0f, 0f,
                transform.B, transform.E, 0f, 0f,
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, 0f,
                transform.C, transform.F, 0f, 0f);
            return true;
        }

        // Lengths 2 and 3 are a vector, not a color matrix
        if (value.TryGetValueAsDreamList(out var list) && list.GetLength() is 9 or 12 or 16 or 20)
            return DreamProcNativeHelpers.TryParseColorMatrix(list, out matrix);

        matrix = default;
        return false;
    }

    private static IGenerator CreateConstant(DreamObjectVector vector) {
        return vector.Is3D ? new GeneratorVector3(vector.AsVector3) : new GeneratorVector2(vector.AsVector2);
    }

    private GeneratorDistribution DistributionNumberToEnum(int number) {
        return number switch {
            0 => GeneratorDistribution.Uniform,
            1 => GeneratorDistribution.Normal,
            2 => GeneratorDistribution.Linear,
            3 => GeneratorDistribution.Square,
            _ => GeneratorDistribution.Uniform // Default to UNIFORM_RAND
        };
    }
}
