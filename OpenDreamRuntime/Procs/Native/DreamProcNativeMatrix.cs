using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using DreamValueTypeFlag = OpenDreamRuntime.DreamValue.DreamValueTypeFlag;

namespace OpenDreamRuntime.Procs.Native;
internal static class DreamProcNativeMatrix {

    [DreamProc("Add")]
    [DreamProcParameter("Matrix2", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_Add(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue possibleMatrix = bundle.GetArgument(0, "Matrix2");
        if (possibleMatrix.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrixArg)) {
            DreamObjectMatrix.AddMatrix((DreamObjectMatrix)src!, matrixArg);
            return new DreamValue(src!);
        }
        // On invalid input, throw runtime
        throw new Exception($"Invalid matrix for addition: {possibleMatrix.ToString()}");
    }


    [DreamProc("Invert")]
    public static DreamValue NativeProc_Invert(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!DreamObjectMatrix.TryInvert((DreamObjectMatrix)src!)) {
            throw new ArgumentException("Matrix does not have a valid inversion for Invert()");
        }

        return new DreamValue(src!);
    }

    [DreamProc("Multiply")]
    [DreamProcParameter("Matrix2", Type = DreamValueTypeFlag.DreamObject | DreamValueTypeFlag.Float)] // or "n"
    public static DreamValue NativeProc_Multiply(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue possibleMatrix = bundle.GetArgument(0, "Matrix2");
        if (possibleMatrix.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrixArg)) {
            DreamObjectMatrix.MultiplyMatrix((DreamObjectMatrix)src!, matrixArg);
            return new DreamValue(src!);
        }
        // The other valid call is with a number "n"
        if (possibleMatrix.TryGetValueAsFloat(out float n)) {
            DreamObjectMatrix.ScaleMatrix((DreamObjectMatrix)src!, n, n);
            return new DreamValue(src!);
        }
        // Special case: If null was passed, return src
        if (possibleMatrix.Equals(DreamValue.Null)) {
            return new DreamValue(src!);
        }
        // Give up and turn the input into the zero matrix on invalid input
        DreamObjectMatrix.ScaleMatrix((DreamObjectMatrix)src!, 0, 0);
        return new DreamValue(src!);
    }

    [DreamProc("Scale")]
    [DreamProcParameter("x", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("y", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_Scale(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        bundle.GetArgument(0, "x").TryGetValueAsFloat(out var horizontalScale);
        if (!bundle.GetArgument(1, "y").TryGetValueAsFloat(out var verticalScale))
            verticalScale = horizontalScale;

        DreamObjectMatrix.ScaleMatrix((DreamObjectMatrix)src!, horizontalScale, verticalScale);
        return new DreamValue(src!);
    }

    [DreamProc("Subtract")]
    [DreamProcParameter("Matrix2", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_Subtract(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue possibleMatrix = bundle.GetArgument(0, "Matrix2");
        if (possibleMatrix.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrixArg)) {
            DreamObjectMatrix.SubtractMatrix((DreamObjectMatrix)src!, matrixArg);
            return new DreamValue(src!);
        }
        // On invalid input, throw runtime
        throw new Exception($"Invalid matrix for subtraction: {possibleMatrix.ToString()}");
    }


    [DreamProc("Translate")]
    [DreamProcParameter("x", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("y", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_Translate(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue xArgument = bundle.GetArgument(0, "x");
        if (xArgument.Equals(DreamValue.Null) || !xArgument.TryGetValueAsFloat(out float xTranslation)) {
            xTranslation = 0; // Defaults to 0 on an invalid value or a passed null
        }

        float yTranslation;
        // If y is null or not provided, use the value of x. If it is otherwise invalid, treat it as 0.
        DreamValue yArgument = bundle.GetArgument(1, "y");
        if (yArgument.Equals(DreamValue.Null)) { // Omitted or passed null
            yTranslation = xTranslation;
        } else if (!yArgument.TryGetValueAsFloat(out yTranslation)) { // An otherwise invalid value
            yTranslation = 0;
        }

        // Avoid translating
        if (xTranslation == 0 && yTranslation == 0) {
            return new DreamValue(src!);
        }

        DreamObjectMatrix.TranslateMatrix((DreamObjectMatrix)src!, xTranslation, yTranslation);

        return new DreamValue(src!);
    }

    [DreamProc("Turn")]
    [DreamProcParameter("angle", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_Turn(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue angleArg = bundle.GetArgument(0, "angle");
        if (!angleArg.TryGetValueAsFloat(out float angle)) {
            return new DreamValue(src!); // Defaults to input on invalid angle
        }
        return _NativeProc_TurnInternal(bundle.ObjectTree, (DreamObjectMatrix)src!, angle);
    }

    /// <summary> Turns a given matrix a given amount of degrees clockwise. </summary>
    /// <returns> Returns a new matrix which has been rotated </returns>
    public static DreamValue _NativeProc_TurnInternal(DreamObjectTree objectTree, DreamObjectMatrix src, float angle) {
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
