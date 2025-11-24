using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Dream;
using Robust.Shared.Random;

namespace OpenDreamRuntime.Procs.Native;

internal static class DreamProcNativeGenerator {
    [DreamProc("Rand")]
    public static DreamValue NativeProc_Rand(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamObjectGenerator genObj = (DreamObjectGenerator)src!;

        switch (genObj.Generator) {
            case GeneratorNum numGen: {
                var result = numGen.Generate(IoCManager.Resolve<IRobustRandom>());
                return new DreamValue(result);
            }
            case GeneratorBox2:
            case GeneratorVector2:
            case GeneratorCircle:
            case GeneratorSquare: {
                var vecGen = (IGeneratorVector)genObj.Generator;
                var resultVector = vecGen.GenerateVector2(IoCManager.Resolve<IRobustRandom>());
                var resultObj = DreamObjectVector.CreateFromValue(resultVector, bundle.ObjectTree);
                return new DreamValue(resultObj);
            }
            case GeneratorBox3:
            case GeneratorVector3:
            case GeneratorSphere:
            case GeneratorCube: {
                var vecGen = (IGeneratorVector)genObj.Generator;
                var resultVector = vecGen.GenerateVector3(IoCManager.Resolve<IRobustRandom>());
                var resultObj = DreamObjectVector.CreateFromValue(resultVector, bundle.ObjectTree);
                return new DreamValue(resultObj);
            }
            default:
                throw new Exception($"Invalid generator for Rand: {genObj}");
        }
    }
}
