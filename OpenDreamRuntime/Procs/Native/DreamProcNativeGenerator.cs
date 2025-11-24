using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Dream;
using Robust.Shared.Random;

namespace OpenDreamRuntime.Procs.Native;

internal static class DreamProcNativeGenerator {
    [DreamProc("Rand")]
    public static DreamValue NativeProc_Rand(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var genObj = (DreamObjectGenerator)src!;

        switch (genObj.Generator) {
            case IGeneratorNum numGen: {
                var result = numGen.Generate(IoCManager.Resolve<IRobustRandom>());
                return new DreamValue(result);
            }
            case IGeneratorVector vecGen: {
                var rand = IoCManager.Resolve<IRobustRandom>();
                var resultObj = vecGen.PrefersVector3
                    ? DreamObjectVector.CreateFromValue(vecGen.GenerateVector3(rand), bundle.ObjectTree)
                    : DreamObjectVector.CreateFromValue(vecGen.GenerateVector2(rand), bundle.ObjectTree);

                return new DreamValue(resultObj);
            }
            default:
                throw new Exception($"Invalid generator for Rand: {genObj}");
        }
    }
}
