using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;

namespace OpenDreamRuntime.Procs.Native;
internal static class DreamProcNativeMatrix {
    [DreamProc("Invert")]
    public static DreamValue NativeProc_Invert(DreamObject src, DreamObject usr, DreamProcArguments arguments) {
        if (!DreamMetaObjectMatrix.TryInvert(src)) {
            throw new ArgumentException("Matrix does not have a valid inversion for Invert()");
        }
        return new DreamValue(src);
    }

    [DreamProc("Scale")]
    [DreamProcParameter("x")]
    [DreamProcParameter("y")]
    public static DreamValue NativeProc_Scale(DreamObject src, DreamObject usr, DreamProcArguments arguments) {
        float horizontalScale;
        float verticalScale;
        arguments.GetArgument(0, "x").TryGetValueAsFloat(out horizontalScale);
        if (!arguments.GetArgument(1, "y").TryGetValueAsFloat(out verticalScale))
            verticalScale = horizontalScale;
        DreamMetaObjectMatrix.ScaleMatrix(src, horizontalScale, verticalScale);
        return new DreamValue(src);
    }
}
