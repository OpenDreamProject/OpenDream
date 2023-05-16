using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using DreamValueType = OpenDreamRuntime.DreamValue.DreamValueType;

namespace OpenDreamRuntime.Procs.Native;
internal static class DreamProcNativeMatrix {

    [DreamProc("Add")]
    [DreamProcParameter("Matrix2", Type = DreamValueType.DreamObject)]
    public static DreamValue NativeProc_Add(NativeProc.State state) {
        DreamValue possibleMatrix = state.GetArgument(0, "Matrix2");
        if (possibleMatrix.TryGetValueAsDreamObjectOfType(state.ObjectTree.Matrix, out var matrixArg)) {
            DreamMetaObjectMatrix.AddMatrix(state.Src, matrixArg);
            return new DreamValue(state.Src);
        }
        // On invalid input, throw runtime
        throw new Exception($"Invalid matrix for addition: {possibleMatrix.ToString()}");
    }


    [DreamProc("Invert")]
    public static DreamValue NativeProc_Invert(NativeProc.State state) {
        if (!DreamMetaObjectMatrix.TryInvert(state.Src)) {
            throw new ArgumentException("Matrix does not have a valid inversion for Invert()");
        }

        return new DreamValue(state.Src);
    }

    [DreamProc("Multiply")]
    [DreamProcParameter("Matrix2", Type = DreamValueType.DreamObject | DreamValueType.Float)] // or "n"
    public static DreamValue NativeProc_Multiply(NativeProc.State state) {
        DreamValue possibleMatrix = state.GetArgument(0, "Matrix2");
        if (possibleMatrix.TryGetValueAsDreamObjectOfType(state.ObjectTree.Matrix, out var matrixArg)) {
            DreamMetaObjectMatrix.MultiplyMatrix(state.Src, matrixArg);
            return new DreamValue(state.Src);
        }
        // The other valid call is with a number "n"
        if (possibleMatrix.TryGetValueAsFloat(out float n)) {
            DreamMetaObjectMatrix.ScaleMatrix(state.Src, n, n);
            return new DreamValue(state.Src);
        }
        // Special case: If null was passed, return src
        if (possibleMatrix.Equals(DreamValue.Null)) {
            return new DreamValue(state.Src);
        }
        // Give up and turn the input into the zero matrix on invalid input
        DreamMetaObjectMatrix.ScaleMatrix(state.Src, 0, 0);
        return new DreamValue(state.Src);
    }

    [DreamProc("Scale")]
    [DreamProcParameter("x")]
    [DreamProcParameter("y")]
    public static DreamValue NativeProc_Scale(NativeProc.State state) {
        float horizontalScale;
        float verticalScale;
        state.GetArgument(0, "x").TryGetValueAsFloat(out horizontalScale);
        if (!state.GetArgument(1, "y").TryGetValueAsFloat(out verticalScale))
            verticalScale = horizontalScale;
        DreamMetaObjectMatrix.ScaleMatrix(state.Src, horizontalScale, verticalScale);
        return new DreamValue(state.Src);
    }

    [DreamProc("Subtract")]
    [DreamProcParameter("Matrix2", Type = DreamValueType.DreamObject)]
    public static DreamValue NativeProc_Subtract(NativeProc.State state) {
        DreamValue possibleMatrix = state.GetArgument(0, "Matrix2");
        if (possibleMatrix.TryGetValueAsDreamObjectOfType(state.ObjectTree.Matrix, out var matrixArg)) {
            DreamMetaObjectMatrix.SubtractMatrix(state.Src, matrixArg);
            return new DreamValue(state.Src);
        }
        // On invalid input, throw runtime
        throw new Exception($"Invalid matrix for subtraction: {possibleMatrix.ToString()}");
    }

    [DreamProc("Turn")]
    [DreamProcParameter("angle", Type = DreamValueType.Float)]
    public static DreamValue NativeProc_Turn(NativeProc.State state) {
        DreamValue angleArg = state.GetArgument(0, "angle");
        if (!angleArg.TryGetValueAsFloat(out float angle)) {
            return new DreamValue(state.Src); // Defaults to input on invalid angle
        }
        return _NativeProc_TurnInternal(state.ObjectTree, state.Src, angle);
    }

    /// <summary> Turns a given matrix a given amount of degrees clockwise. </summary>
    /// <returns> Returns a new matrix which has been rotated </returns>
    public static DreamValue _NativeProc_TurnInternal(IDreamObjectTree objectTree, DreamObject src, float angle) {
        var (angleSin, angleCos) = ((float, float))Math.SinCos(Math.PI / 180.0 * angle);
        if (float.IsSubnormal(angleSin)) // FIXME: Think of a better solution to bad results for some angles.
            angleSin = 0;
        if (float.IsSubnormal(angleCos))
            angleCos = 0;

        DreamObject rotationMatrix = DreamMetaObjectMatrix.MakeMatrix(objectTree,angleCos, angleSin, 0, -angleSin, angleCos, 0);
        DreamMetaObjectMatrix.MultiplyMatrix(src, rotationMatrix);

        return new DreamValue(src);
    }
}
