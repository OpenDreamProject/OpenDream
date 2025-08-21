using OpenDreamShared.Rendering;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectGenerator : DreamObject {
    public DreamValue A { get; private set; }
    public DreamValue B { get; private set; }
    public GeneratorOutputType OutputType { get; private set; }
    public GeneratorDistribution Distribution { get; private set; }

    public DreamObjectGenerator(DreamObjectDefinition objectDefinition, DreamValue a, DreamValue b, GeneratorOutputType outputType, GeneratorDistribution dist) : base(objectDefinition) {
        A = a;
        B = b;
        OutputType = outputType;
        Distribution = dist;
    }
}

