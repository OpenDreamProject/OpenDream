using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using DreamValueType = OpenDreamRuntime.DreamValue.DreamValueType;

namespace OpenDreamRuntime.Procs.Native;
internal static class DreamProcNativeMatrix {
    public static IDreamObjectTree ObjectTree;

    [DreamProc("Invert")]
    public static DreamValue NativeProc_Invert(DreamObject src, DreamObject usr, DreamProcArguments arguments) {
        if (!DreamMetaObjectMatrix.TryInvert(src)) {
            throw new ArgumentException("Matrix does not have a valid inversion for Invert()");
        }
        return new DreamValue(src);
    }

    [DreamProc("Multiply")]
    [DreamProcParameter("Matrix2")] // or "n"
    public static DreamValue NativeProc_Multiply(DreamObject src, DreamObject usr, DreamProcArguments arguments) {
        DreamValue possibleMatrix = arguments.GetArgument(0, "Matrix2");
        if (possibleMatrix.TryGetValueAsDreamObjectOfType(ObjectTree.Matrix, out var matrixArg)) {
            DreamMetaObjectMatrix.MultiplyMatrix(src, matrixArg);
            return new DreamValue(src);
        }
        // The other valid call is with a number "n"
        if (possibleMatrix.TryGetValueAsFloat(out float n)) {
            DreamMetaObjectMatrix.ScaleMatrix(src, n, n);
            return new DreamValue(src);
        }
        // Special case: If null was passed, return src
        if (possibleMatrix.Equals(DreamValue.Null)) {
            return new DreamValue(src);
        }
        // Give up and turn the input into the zero matrix on invalid input
        DreamMetaObjectMatrix.ScaleMatrix(src, 0, 0);
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

    [DreamProc("Turn")]
    [DreamProcParameter("angle", Type = DreamValueType.Float)]
    public static DreamValue NativeProc_Turn(DreamObject src, DreamObject usr, DreamProcArguments arguments) {
        DreamValue angleArg = arguments.GetArgument(0, "angle");
        if (!angleArg.TryGetValueAsFloat(out float angle)) {
            return new DreamValue(src); // Defaults to input on invalid angle
        }
        return _NativeProc_TurnInternal(src, usr, angle);
    }

    /// <summary> Turns a given matrix a given amount of degrees clockwise. </summary>
    /// <returns> Returns a new matrix which has been rotated </returns>
    public static DreamValue _NativeProc_TurnInternal(DreamObject src, DreamObject usr, float angle) {
        var (angleSin, angleCos) = ((float, float))Math.SinCos(Angle.FromDegrees(angle));
        if (float.IsSubnormal(angleSin)) // FIXME: Think of a better solution to bad results for some angles.
            angleSin = 0;
        if (float.IsSubnormal(angleCos))
            angleCos = 0;

        DreamObject rotationMatrix = DreamMetaObjectMatrix.MakeMatrix(ObjectTree,angleCos, angleSin, 0, -angleSin, angleCos, 0);
        DreamMetaObjectMatrix.MultiplyMatrix(src, rotationMatrix);

        return new DreamValue(src);
    }
}
