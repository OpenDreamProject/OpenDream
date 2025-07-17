using OpenDreamRuntime.Procs;
using OpenDreamShared.Rendering;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectGenerator : DreamObject {

    public DreamValue A { get; private set; }
    public DreamValue B { get; private set; }
    public GeneratorOutputType OutputType { get; private set; }
    public GeneratorDistribution Distribution { get; private set; }


    public DreamObjectGenerator(DreamObjectDefinition objectDefinition, DreamValue A, DreamValue B, GeneratorOutputType outputType, GeneratorDistribution dist) : base(objectDefinition) {
        this.A = A;
        this.B = B;
        this.OutputType = outputType;
        this.Distribution = dist;
    }
    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);
    }
}

