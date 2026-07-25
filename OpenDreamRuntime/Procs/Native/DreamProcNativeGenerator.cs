using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamShared.Dream;
using Robust.Shared.Random;
using DreamValueTypeFlag = OpenDreamRuntime.DreamValue.DreamValueTypeFlag;

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

    /// <summary> Rotates the vectors this generator produces around the XY plane </summary>
    /// <remarks> Unlike the global turn(), this modifies the generator in-place and returns it </remarks>
    [DreamProc("Turn")]
    [DreamProcParameter("angle", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_Turn(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var genObj = (DreamObjectGenerator)src!;

        var angleArg = bundle.GetArgument(0, "angle");
        if (!angleArg.TryGetValueAsFloat(out var angle))
            throw new Exception($"number required for 2nd argument: turn({genObj}, {angleArg})");

        genObj.Turn(angle);

        genObj.IncRef();
        return new DreamValue(genObj);
    }
}
