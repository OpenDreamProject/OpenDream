using System.Collections.Immutable;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectMatrix(DreamObjectDefinition objectDefinition) : DreamObject(objectDefinition) {
    public static readonly float[] IdentityMatrixArray = {1f, 0f, 0f, 0f, 1f, 0f};

    public float A { get=> _aInner.UnsafeGetValueAsFloat(); set { _aInner.DecRef(); _aInner = new(value); } }
    public float B { get=> _bInner.UnsafeGetValueAsFloat(); set { _bInner.DecRef(); _bInner = new(value); } }
    public float C { get=> _cInner.UnsafeGetValueAsFloat(); set { _cInner.DecRef(); _cInner = new(value); } }
    public float D { get=> _dInner.UnsafeGetValueAsFloat(); set { _dInner.DecRef(); _dInner = new(value); } }
    public float E { get=> _eInner.UnsafeGetValueAsFloat(); set { _eInner.DecRef(); _eInner = new(value); } }
    public float F { get=> _fInner.UnsafeGetValueAsFloat(); set { _fInner.DecRef(); _fInner = new(value); } }

    private DreamValue _aInner, _bInner, _cInner, _dInner, _eInner, _fInner;

    public override void Initialize(DreamProcArguments args) {
        if (args.Count == 0) {
            A = 1f;
            B = 0f;
            C = 0f;
            D = 0f;
            E = 1f;
            F = 0f;
        } else {
            DreamValue copyMatrixOrA = args.GetArgument(0);
            if (copyMatrixOrA.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrixToCopy)) {
                A = matrixToCopy.A;
                B = matrixToCopy.B;
                C = matrixToCopy.C;
                D = matrixToCopy.D;
                E = matrixToCopy.E;
                F = matrixToCopy.F;
            } else {
                DreamValue b = args.GetArgument(1);
                DreamValue c = args.GetArgument(2);
                DreamValue d = args.GetArgument(3);
                DreamValue e = args.GetArgument(4);
                DreamValue f = args.GetArgument(5);
                try { // BYOND runtimes if args are of the wrong type
                    A = copyMatrixOrA.MustGetValueAsFloat();
                    B = b.MustGetValueAsFloat();
                    C = c.MustGetValueAsFloat();
                    D = d.MustGetValueAsFloat();
                    E = e.MustGetValueAsFloat();
                    F = f.MustGetValueAsFloat();
                } catch (InvalidCastException) {
                    throw new ArgumentException($"Invalid arguments used to create matrix {copyMatrixOrA} {b} {c} {d} {e} {f}");
                }
            }
        }

        base.Initialize(args);
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        switch (varName) {
            case "a": value = _aInner; return true;
            case "b": value = _bInner; return true;
            case "c": value = _cInner; return true;
            case "d": value = _dInner; return true;
            case "e": value = _eInner; return true;
            case "f": value = _fInner; return true;
            default: return base.TryGetVar(varName, out value);
        }
    }

    protected override void SetVar(string varName, DreamValue value) {
        switch (varName) {
            case "a": _aInner.DecRef(); _aInner = value; _aInner.IncRef(); break;
            case "b": _bInner.DecRef(); _bInner = value; _bInner.IncRef(); break;
            case "c": _cInner.DecRef(); _cInner = value; _cInner.IncRef(); break;
            case "d": _dInner.DecRef(); _dInner = value; _dInner.IncRef(); break;
            case "e": _eInner.DecRef(); _eInner = value; _eInner.IncRef(); break;
            case "f": _fInner.DecRef(); _fInner = value; _fInner.IncRef(); break;
            default:
                base.SetVar(varName, value);
                break;
        }
    }

    #region Operators

    public override DreamValue OperatorAdd(DreamValue b, DMProcState state) {
        if (b.TryGetValueAsDreamObject<DreamObjectMatrix>(out var right)) {
            DreamObject output = MakeMatrix(ObjectTree,
                A + right.A, // a
                B + right.B, // b
                C + right.C, // c
                D + right.D, // d
                E + right.E, // e
                F + right.F  // f
            );

            return new DreamValue(output);
        }

        return base.OperatorAdd(b, state);
    }

    public override DreamValue OperatorSubtract(DreamValue b, DMProcState state) {
        if (b.TryGetValueAsDreamObject<DreamObjectMatrix>(out var right)) {
            DreamObject output = MakeMatrix(ObjectTree,
                A - right.A, // a
                B - right.B, // b
                C - right.C, // c
                D - right.D, // d
                E - right.E, // e
                F - right.F  // f
            );

            return new DreamValue(output);
        }

        return base.OperatorSubtract(b, state);
    }

    public override DreamValue OperatorMultiply(DreamValue b, DMProcState state) {
        if (b.TryGetValueAsFloat(out float bFloat)) {
            DreamObjectMatrix output = MakeMatrix(ObjectTree,
                A * bFloat, B * bFloat, C * bFloat,
                D * bFloat, E * bFloat, F * bFloat
            );

            return new DreamValue(output);
        } else if (b.TryGetValueAsDreamObject<DreamObjectMatrix>(out var right)) {
            DreamObjectMatrix output = MakeMatrix(ObjectTree,
                right.A * A + right.D * B, // a
                right.B * A + right.E * B, // b
                right.C * A + right.F * B + C, // c
                right.A * D + right.D * E, // d
                right.B * D + right.E * E, // e
                right.C * D + right.F * E + F // f
            );

            return new DreamValue(output);
        }

        return base.OperatorMultiply(b, state);
    }

    public override DreamValue OperatorMultiplyRef(DreamValue b, DMProcState state) {
        return OperatorMultiply(b, state);
    }

    public override DreamValue OperatorDivide(DreamValue b, DMProcState state) {
        if (b.TryGetValueAsFloat(out float bFloat)) {
            DreamObjectMatrix output = MakeMatrix(ObjectTree,
                    A / bFloat, B / bFloat, C / bFloat,
                    D / bFloat, E / bFloat, F / bFloat
                );
            return new DreamValue(output);
        } else if(b.TryGetValueAsDreamObject<DreamObjectMatrix>(out var right)) { //matrix divided by matrix isn't a thing, but in BYOND it's apparently multiplication by the inverse, because of course it is
            var rightCopy = MatrixClone(ObjectTree, right);
            using var rightCopyDreamValue = new DreamValue(rightCopy);
            if (!TryInvert(rightCopy))
                throw new ArgumentException("Matrix does not have a valid inversion for Invert()");

            return OperatorMultiply(rightCopyDreamValue, state);
        }

        return base.OperatorDivide(b, state);
    }

    public override DreamValue OperatorDivideRef(DreamValue b, DMProcState state) {
        return OperatorDivide(b, state);
    }

    public override DreamValue OperatorEquivalent(DreamValue b) {
        if (!b.TryGetValueAsDreamObject<DreamObjectMatrix>(out var right))
            return DreamValue.False;

        return A.Equals(right.A) && B.Equals(right.B) &&
               C.Equals(right.C) && D.Equals(right.D) &&
               E.Equals(right.E) && F.Equals(right.F)
            ? DreamValue.True
            : DreamValue.False;
    }

    public override DreamValue OperatorAppend(DreamValue b) {
        if (b.TryGetValueAsDreamObject<DreamObjectMatrix>(out var right)) {
            A += right.A;
            B += right.B;
            C += right.C;
            D += right.D;
            E += right.E;
            F += right.F;
            IncRef();
            return new(this);
        }

        return base.OperatorAppend(b);
    }

    public override DreamValue OperatorRemove(DreamValue b) {
        if (b.TryGetValueAsDreamObject<DreamObjectMatrix>(out var right)) {
            A -= right.A;
            B -= right.B;
            C -= right.C;
            D -= right.D;
            E -= right.E;
            F -= right.F;
            IncRef();
            return new(this);
        }

        return base.OperatorRemove(b);
    }
    #endregion Operators

    #region Helpers

    /// <summary> Used to create a float array understandable by <see cref="MutableAppearance.Transform"/> to be a transform. </summary>
    /// <returns>The matrix's values in an array, in [a,d,b,e,c,f] order.</returns>
    public static float[] MatrixToTransformFloatArray(DreamObjectMatrix matrix) {
        float[] array = new float[6];
        array[0] = matrix.A;
        array[1] = matrix.D;
        array[2] = matrix.B;
        array[3] = matrix.E;
        array[4] = matrix.C;
        array[5] = matrix.F;
        return array;
    }

    /// <remarks>
    /// This does elect to actually call /matrix/New() with the old matrix's values, <br/>
    /// if anything, for the sake of support for having derived classes of /matrix.
    /// </remarks>
    /// <param name="objectTree">The DM object tree, used to create a new matrix object.</param>
    /// <param name="matrix">The matrix to clone.</param>
    /// <returns>A clone of the given matrix.</returns>
    public static DreamObjectMatrix MatrixClone(DreamObjectTree objectTree, DreamObjectMatrix matrix) {
        var newMatrix = objectTree.CreateObject<DreamObjectMatrix>(matrix.ObjectDefinition.TreeEntry);
        var args = new DreamValue[6];

        int i = 0;
        foreach(float f in EnumerateMatrix(matrix)) {
            args[i++] = new DreamValue(f);
        }

        newMatrix.InitSpawn(new(args));
        return newMatrix;
    }

    /// <summary>
    /// Simple helper for quickly making a basic matrix given its six values, in a-to-f order.
    /// </summary>
    /// <remarks>
    /// Note that this skips over making a New() call, so hopefully you're not doing anything meaningful in there, DM-side. <br/>
    /// <see langword="FIXME:"/> actually call /New(), if necessary, when creating a matrix in this way.
    /// </remarks>
    /// <returns>A matrix created with a to f manually set to the floats given.</returns>
    public static DreamObjectMatrix MakeMatrix(DreamObjectTree objectTree, float a, float b, float c, float d, float e, float f) {
        var newMatrix = objectTree.CreateObject<DreamObjectMatrix>(objectTree.Matrix);
        newMatrix.A = a;
        newMatrix.B = b;
        newMatrix.C = c;
        newMatrix.D = d;
        newMatrix.E = e;
        newMatrix.F = f;
        return newMatrix;
    }

    /// <summary> Helper for the normal MakeMatrix that accepts a list of matrix values. </summary>
    /// <remarks> Be sure that all of the float array are valid values. </remarks>
    /// <seealso cref="MakeMatrix(DreamObjectTree,float,float,float,float,float,float)"/>
    public static DreamObjectMatrix MakeMatrix(DreamObjectTree objectTree, float[] matrixValues) {
        return MakeMatrix(objectTree,
                          matrixValues[0], matrixValues[2], matrixValues[4], //order on these matches the output of MatrixToTransformFloatArray
                          matrixValues[1], matrixValues[3], matrixValues[5]);
    }

    public static float Determinant(DreamObjectMatrix matrix) {
        return matrix.A * matrix.E -
               matrix.D * matrix.B;
    }

    /// <summary>Inverts the given matrix, in-place.</summary>
    /// <returns>true if inversion was possible, false if not.</returns>
    public static bool TryInvert(DreamObjectMatrix matrix) {
        var determinant = Determinant(matrix);
        if (determinant == 0f)
            return false;

        var oldValues = EnumerateMatrix(matrix).ToImmutableArray();

        //Just going by what we used to have as DM code within DMStandard. No clue if the math is right, here
        matrix.A = oldValues[4] / determinant; // a = e
        matrix.B = -oldValues[1] / determinant; // b = -b
        matrix.C = (oldValues[1] * oldValues[5] - oldValues[4] * oldValues[2]) / determinant; // c = b*f - e*c
        matrix.D = -oldValues[3] / determinant; // d = -d
        matrix.E = oldValues[0] / determinant; // e = a
        matrix.F = (oldValues[3] * oldValues[2] - oldValues[0] * oldValues[5]) / determinant; // f = d*c - a*f
        return true;
    }

    /// <summary>
    /// Used when printing this matrix to enumerate its values in order.
    /// </summary>
    /// <returns>The matrix's values in [a,b,c,d,e,f] order.</returns>
    public static IEnumerable<float> EnumerateMatrix(DreamObjectMatrix matrix) {
        yield return matrix.A;
        yield return matrix.B;
        yield return matrix.C;
        yield return matrix.D;
        yield return matrix.E;
        yield return matrix.F;
    }

    /// <summary> Translates a given matrix by the two translation factors given.</summary>
    /// <remarks> Note that this does use <see cref="DreamObject.SetVariableValue"/>.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if the matrix has non-float members.</exception>
    public static void TranslateMatrix(DreamObjectMatrix matrix, float x, float y) {
        matrix.C += x;
        matrix.F += y;
    }

    /// <summary> Scales a given matrix by the two scaling factors given.</summary>
    /// <remarks> Note that this does use <see cref="DreamObject.SetVariableValue"/>.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if the matrix has non-float members.</exception>
    public static void ScaleMatrix(DreamObjectMatrix matrix, float x, float y) {
        matrix.A *= x;
        matrix.B *= x;
        matrix.C *= x;

        matrix.D *= y;
        matrix.E *= y;
        matrix.F *= y;
    }

    /// <summary> Adds the second given matrix to the first given matrix. </summary>
    public static void AddMatrix(DreamObjectMatrix lMatrix, DreamObjectMatrix rMatrix) {
        lMatrix.A += rMatrix.A;
        lMatrix.B += rMatrix.B;
        lMatrix.C += rMatrix.C;
        lMatrix.D += rMatrix.D;
        lMatrix.E += rMatrix.E;
        lMatrix.F += rMatrix.F;
    }

    /// <summary> Subtracts the second given matrix from the first given matrix. </summary>
    public static void SubtractMatrix(DreamObjectMatrix lMatrix, DreamObjectMatrix rMatrix) {
        lMatrix.A -= rMatrix.A;
        lMatrix.B -= rMatrix.B;
        lMatrix.C -= rMatrix.C;
        lMatrix.D -= rMatrix.D;
        lMatrix.E -= rMatrix.E;
        lMatrix.F -= rMatrix.F;
    }

    /// <summary> Multiplies the first given matrix by the other given matrix. </summary>
    public static void MultiplyMatrix(DreamObjectMatrix lMatrix, DreamObjectMatrix rMatrix) {
        float lA = lMatrix.A, lB = lMatrix.B, lC = lMatrix.C, lD = lMatrix.D, lE = lMatrix.E, lF = lMatrix.F;
        float rA = rMatrix.A, rB = rMatrix.B, rC = rMatrix.C, rD = rMatrix.D, rE = rMatrix.E, rF = rMatrix.F;

        lMatrix.A = lA * rA + lD * rB;
        lMatrix.B = lB * rA + lE * rB;
        lMatrix.C = lC * rA + lF * rB + rC;
        lMatrix.D = lA * rD + lD * rE;
        lMatrix.E = lB * rD + lE * rE;
        lMatrix.F = lC * rD + lF * rE + rF;
    }
    #endregion Helpers
}
