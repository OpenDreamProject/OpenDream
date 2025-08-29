using OpenDreamShared.Rendering;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectGenerator(DreamObjectDefinition objectDefinition, DreamValue a, DreamValue b, GeneratorOutputType outputType, GeneratorDistribution distribution) : DreamObject(objectDefinition) {
    public DreamValue A { get; private set; } = a;
    public DreamValue B { get; private set; } = b;
    public GeneratorOutputType OutputType { get; private set; } = outputType;
    public GeneratorDistribution Distribution { get; private set; } = distribution;
}

