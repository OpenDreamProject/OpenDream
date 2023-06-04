using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using DreamValueType = OpenDreamRuntime.DreamValue.DreamValueType;

namespace OpenDreamRuntime.Procs.Native;
internal static class DreamProcNativeMatrix {

    [DreamProc("Add")]
    [DreamProcParameter("Matrix2", Type = DreamValueType.DreamObject)]
    public static DreamValue NativeProc_Add(NativeProc.State state) {
        DreamValue possibleMatrix = state.GetArgument(0, "Matrix2");
        if (possibleMatrix.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrixArg)) {
            DreamObjectMatrix.AddMatrix((DreamObjectMatrix)state.Src!, matrixArg);
            return new DreamValue(state.Src);
        }
        // On invalid input, throw runtime
        throw new Exception($"Invalid matrix for addition: {possibleMatrix.ToString()}");
    }


    [DreamProc("Invert")]
    public static DreamValue NativeProc_Invert(NativeProc.State state) {
        if (!DreamObjectMatrix.TryInvert((DreamObjectMatrix)state.Src!)) {
            throw new ArgumentException("Matrix does not have a valid inversion for Invert()");
        }

        return new DreamValue(state.Src);
    }

    [DreamProc("Multiply")]
    [DreamProcParameter("Matrix2", Type = DreamValueType.DreamObject | DreamValueType.Float)] // or "n"
    public static DreamValue NativeProc_Multiply(NativeProc.State state) {
        DreamValue possibleMatrix = state.GetArgument(0, "Matrix2");
        if (possibleMatrix.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrixArg)) {
            DreamObjectMatrix.MultiplyMatrix((DreamObjectMatrix)state.Src!, matrixArg);
            return new DreamValue(state.Src);
        }
        // The other valid call is with a number "n"
        if (possibleMatrix.TryGetValueAsFloat(out float n)) {
            DreamObjectMatrix.ScaleMatrix((DreamObjectMatrix)state.Src!, n, n);
            return new DreamValue(state.Src);
        }
        // Special case: If null was passed, return src
        if (possibleMatrix.Equals(DreamValue.Null)) {
            return new DreamValue(state.Src);
        }
        // Give up and turn the input into the zero matrix on invalid input
        DreamObjectMatrix.ScaleMatrix((DreamObjectMatrix)state.Src!, 0, 0);
        return new DreamValue(state.Src);
    }

    [DreamProc("Scale")]
    [DreamProcParameter("x", Type = DreamValueType.Float)]
    [DreamProcParameter("y", Type = DreamValueType.Float)]
    public static DreamValue NativeProc_Scale(NativeProc.State state) {
        state.GetArgument(0, "x").TryGetValueAsFloat(out var horizontalScale);
        if (!state.GetArgument(1, "y").TryGetValueAsFloat(out var verticalScale))
            verticalScale = horizontalScale;

        DreamObjectMatrix.ScaleMatrix((DreamObjectMatrix)state.Src!, horizontalScale, verticalScale);
        return new DreamValue(state.Src);
    }

    [DreamProc("Subtract")]
    [DreamProcParameter("Matrix2", Type = DreamValueType.DreamObject)]
    public static DreamValue NativeProc_Subtract(NativeProc.State state) {
        DreamValue possibleMatrix = state.GetArgument(0, "Matrix2");
        if (possibleMatrix.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrixArg)) {
            DreamObjectMatrix.SubtractMatrix((DreamObjectMatrix)state.Src!, matrixArg);
            return new DreamValue(state.Src);
        }
        // On invalid input, throw runtime
        throw new Exception($"Invalid matrix for subtraction: {possibleMatrix.ToString()}");
    }


    [DreamProc("Translate")]
    [DreamProcParameter("x", Type = DreamValueType.Float)]
    [DreamProcParameter("y", Type = DreamValueType.Float)]
    public static DreamValue NativeProc_Translate(NativeProc.State state) {
        DreamValue xArgument = state.GetArgument(0, "x");
        if (xArgument.Equals(DreamValue.Null) || !xArgument.TryGetValueAsFloat(out float xTranslation)) {
            xTranslation = 0; // Defaults to 0 on an invalid value or a passed null
        }

        float yTranslation;
        // If y is null or not provided, use the value of x. If it is otherwise invalid, treat it as 0.
        DreamValue yArgument = state.GetArgument(1, "y");
        if (yArgument.Equals(DreamValue.Null)) { // Omitted or passed null
            yTranslation = xTranslation;
        } else if (!yArgument.TryGetValueAsFloat(out yTranslation)) { // An otherwise invalid value
            yTranslation = 0;
        }

        // Avoid translating
        if (xTranslation == 0 && yTranslation == 0) {
            return new DreamValue(state.Src);
        }

        DreamObjectMatrix.TranslateMatrix((DreamObjectMatrix)state.Src!, xTranslation, yTranslation);

        return new DreamValue(state.Src);
    }

    [DreamProc("Turn")]
    [DreamProcParameter("angle", Type = DreamValueType.Float)]
    public static DreamValue NativeProc_Turn(NativeProc.State state) {
        DreamValue angleArg = state.GetArgument(0, "angle");
        if (!angleArg.TryGetValueAsFloat(out float angle)) {
            return new DreamValue(state.Src); // Defaults to input on invalid angle
        }
        return _NativeProc_TurnInternal(state.ObjectTree, (DreamObjectMatrix)state.Src!, angle);
    }

    /// <summary> Turns a given matrix a given amount of degrees clockwise. </summary>
    /// <returns> Returns a new matrix which has been rotated </returns>
    public static DreamValue _NativeProc_TurnInternal(IDreamObjectTree objectTree, DreamObjectMatrix src, float angle) {
        var (angleSin, angleCos) = ((float, float))Math.SinCos(Math.PI / 180.0 * angle);
        if (float.IsSubnormal(angleSin)) // FIXME: Think of a better solution to bad results for some angles.
            angleSin = 0;
        if (float.IsSubnormal(angleCos))
            angleCos = 0;

        var rotationMatrix = DreamObjectMatrix.MakeMatrix(objectTree ,angleCos, angleSin, 0, -angleSin, angleCos, 0);
        DreamObjectMatrix.MultiplyMatrix(src, rotationMatrix);

        return new DreamValue(src);
    }
}
