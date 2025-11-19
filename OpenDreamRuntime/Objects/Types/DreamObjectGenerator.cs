using OpenDreamRuntime.Procs;
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
                var low = a.UnsafeGetValueAsFloat();
                var high = (b.Type == DreamValue.DreamValueType.Float) ? b.UnsafeGetValueAsFloat() : 1f;

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
                break;
            }
            case "square":
            case "cube": {
                var low = DreamObjectVector.CreateFromValue(a, ObjectTree);
                var high = DreamObjectVector.CreateFromValue(b, ObjectTree);

                Generator = typeStr switch {
                    "square" => new GeneratorSquare(low.AsVector2, high.AsVector2, distribution),
                    "cube" => new GeneratorCube(low.AsVector3, high.AsVector3, distribution),
                    _ => throw new ArgumentOutOfRangeException()
                };

                break;
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

    private GeneratorDistribution DistributionNumberToEnum(int number) {
        return number switch {
            0 => GeneratorDistribution.Uniform,
            1 => GeneratorDistribution.Normal,
            2 => GeneratorDistribution.Linear,
            3 => GeneratorDistribution.Square,
            _ => GeneratorDistribution.Normal // Default to NORMAL_RAND
        };
    }
}

