using System.Collections.Immutable;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectMatrix : DreamObject {
    public static readonly float[] IdentityMatrixArray = {1f, 0f, 0f, 0f, 1f, 0f};

    // TODO: Store a/b/c/d/e/f as fields instead of as DM vars

    public DreamObjectMatrix(DreamObjectDefinition objectDefinition) : base(objectDefinition) {

    }

    public override void Initialize(DreamProcArguments args) {
        if (args.Count > 0) {
            DreamValue copyMatrixOrA = args.GetArgument(0);
            if (copyMatrixOrA.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrixToCopy)) {
                SetVariableValue("a", matrixToCopy.GetVariable("a"));
                SetVariableValue("b", matrixToCopy.GetVariable("b"));
                SetVariableValue("c", matrixToCopy.GetVariable("c"));
                SetVariableValue("d", matrixToCopy.GetVariable("d"));
                SetVariableValue("e", matrixToCopy.GetVariable("e"));
                SetVariableValue("f", matrixToCopy.GetVariable("f"));
            } else {
                DreamValue b = args.GetArgument(1);
                DreamValue c = args.GetArgument(2);
                DreamValue d = args.GetArgument(3);
                DreamValue e = args.GetArgument(4);
                DreamValue f = args.GetArgument(5);
                try { // BYOND runtimes if args are of the wrong type
                    copyMatrixOrA.MustGetValueAsFloat();
                    b.MustGetValueAsFloat();
                    c.MustGetValueAsFloat();
                    d.MustGetValueAsFloat();
                    e.MustGetValueAsFloat();
                    f.MustGetValueAsFloat();
                } catch (InvalidCastException) {
                    throw new ArgumentException($"Invalid arguments used to create matrix {copyMatrixOrA} {b} {c} {d} {e} {f}");
                }

                SetVariableValue("a", copyMatrixOrA);
                SetVariableValue("b", b);
                SetVariableValue("c", c);
                SetVariableValue("d", d);
                SetVariableValue("e", e);
                SetVariableValue("f", f);
            }
        }

        base.Initialize(args);
    }

    #region Operators

    public override DreamValue OperatorAdd(DreamValue b, DMProcState state) {
        GetVariable("a").TryGetValueAsFloat(out float lA);
        GetVariable("b").TryGetValueAsFloat(out float lB);
        GetVariable("c").TryGetValueAsFloat(out float lC);
        GetVariable("d").TryGetValueAsFloat(out float lD);
        GetVariable("e").TryGetValueAsFloat(out float lE);
        GetVariable("f").TryGetValueAsFloat(out float lF);

        if (b.TryGetValueAsDreamObject<DreamObjectMatrix>(out var right)) {
            right.GetVariable("a").TryGetValueAsFloat(out float rA);
            right.GetVariable("b").TryGetValueAsFloat(out float rB);
            right.GetVariable("c").TryGetValueAsFloat(out float rC);
            right.GetVariable("d").TryGetValueAsFloat(out float rD);
            right.GetVariable("e").TryGetValueAsFloat(out float rE);
            right.GetVariable("f").TryGetValueAsFloat(out float rF);

            DreamObject output = MakeMatrix(ObjectTree,
                lA + rA, // a
                lB + rB, // b
                lC + rC, // c
                lD + rD, // d
                lE + rE, // e
                lF + rF  // f
            );

            return new DreamValue(output);
        }

        return base.OperatorAdd(b, state);
    }

    public override DreamValue OperatorSubtract(DreamValue b, DMProcState state) {
        GetVariable("a").TryGetValueAsFloat(out float lA);
        GetVariable("b").TryGetValueAsFloat(out float lB);
        GetVariable("c").TryGetValueAsFloat(out float lC);
        GetVariable("d").TryGetValueAsFloat(out float lD);
        GetVariable("e").TryGetValueAsFloat(out float lE);
        GetVariable("f").TryGetValueAsFloat(out float lF);

        if (b.TryGetValueAsDreamObject<DreamObjectMatrix>(out var right)) {
            right.GetVariable("a").TryGetValueAsFloat(out float rA);
            right.GetVariable("b").TryGetValueAsFloat(out float rB);
            right.GetVariable("c").TryGetValueAsFloat(out float rC);
            right.GetVariable("d").TryGetValueAsFloat(out float rD);
            right.GetVariable("e").TryGetValueAsFloat(out float rE);
            right.GetVariable("f").TryGetValueAsFloat(out float rF);

            DreamObject output = MakeMatrix(ObjectTree,
                lA - rA, // a
                lB - rB, // b
                lC - rC, // c
                lD - rD, // d
                lE - rE, // e
                lF - rF  // f
            );

            return new DreamValue(output);
        }

        return base.OperatorSubtract(b, state);
    }

    public override DreamValue OperatorMultiply(DreamValue b, DMProcState state) {
        GetVariable("a").TryGetValueAsFloat(out float lA);
        GetVariable("b").TryGetValueAsFloat(out float lB);
        GetVariable("c").TryGetValueAsFloat(out float lC);
        GetVariable("d").TryGetValueAsFloat(out float lD);
        GetVariable("e").TryGetValueAsFloat(out float lE);
        GetVariable("f").TryGetValueAsFloat(out float lF);

        if (b.TryGetValueAsFloat(out float bFloat)) {
            DreamObjectMatrix output = MakeMatrix(ObjectTree,
                    lA * bFloat,lB * bFloat,lC * bFloat,
                    lD * bFloat,lE * bFloat,lF * bFloat
                );
            return new DreamValue(output);
        } else if (b.TryGetValueAsDreamObject<DreamObjectMatrix>(out var right)) {
            right.GetVariable("a").TryGetValueAsFloat(out float rA);
            right.GetVariable("b").TryGetValueAsFloat(out float rB);
            right.GetVariable("c").TryGetValueAsFloat(out float rC);
            right.GetVariable("d").TryGetValueAsFloat(out float rD);
            right.GetVariable("e").TryGetValueAsFloat(out float rE);
            right.GetVariable("f").TryGetValueAsFloat(out float rF);

            DreamObjectMatrix output = MakeMatrix(ObjectTree,
                rA * lA + rD * lB, // a
                rB * lA + rE * lB, // b
                rC * lA + rF * lB + lC, // c
                rA * lD + rD * lE, // d
                rB * lD + rE * lE, // e
                rC * lD + rF * lE + lF // f
            );

            return new DreamValue(output);
        }

        return base.OperatorMultiply(b, state);
    }

    public override DreamValue OperatorMultiplyRef(DreamValue b, DMProcState state) {
        return OperatorMultiply(b, state);
    }

    public override DreamValue OperatorDivide(DreamValue b, DMProcState state) {
        GetVariable("a").TryGetValueAsFloat(out float lA);
        GetVariable("b").TryGetValueAsFloat(out float lB);
        GetVariable("c").TryGetValueAsFloat(out float lC);
        GetVariable("d").TryGetValueAsFloat(out float lD);
        GetVariable("e").TryGetValueAsFloat(out float lE);
        GetVariable("f").TryGetValueAsFloat(out float lF);

        if (b.TryGetValueAsFloat(out float bFloat)) {
            DreamObjectMatrix output = MakeMatrix(ObjectTree,
                    lA / bFloat,lB / bFloat,lC / bFloat,
                    lD / bFloat,lE / bFloat,lF / bFloat
                );
            return new DreamValue(output);
        } else if(b.TryGetValueAsDreamObject<DreamObjectMatrix>(out var right)) { //matrix divided by matrix isn't a thing, but in BYOND it's apparently multiplication by the inverse, because of course it is
            DreamObjectMatrix rightCopy = MatrixClone(ObjectTree, right);
            if (!TryInvert(rightCopy))
                throw new ArgumentException("Matrix does not have a valid inversion for Invert()");
            return OperatorMultiply(new(rightCopy), state);
        }

        return base.OperatorDivide(b, state);
    }

    public override DreamValue OperatorDivideRef(DreamValue b, DMProcState state) {
        return OperatorDivide(b, state);
    }

    public override DreamValue OperatorEquivalent(DreamValue b) {
        if (!b.TryGetValueAsDreamObject<DreamObjectMatrix>(out var right))
            return DreamValue.False;

        const string elements = "abcdef";
        for (int i = 0; i < elements.Length; i++) {
            GetVariable(elements[i].ToString()).TryGetValueAsFloat(out var leftValue); // sets leftValue to 0 if this isn't a float
            right.GetVariable(elements[i].ToString()).TryGetValueAsFloat(out var rightValue); // ditto
            if (!leftValue.Equals(rightValue))
                return DreamValue.False;
        }
        return DreamValue.True;
    }

    public override DreamValue OperatorAppend(DreamValue b) {
        GetVariable("a").TryGetValueAsFloat(out float lA);
        GetVariable("b").TryGetValueAsFloat(out float lB);
        GetVariable("c").TryGetValueAsFloat(out float lC);
        GetVariable("d").TryGetValueAsFloat(out float lD);
        GetVariable("e").TryGetValueAsFloat(out float lE);
        GetVariable("f").TryGetValueAsFloat(out float lF);

        if (b.TryGetValueAsDreamObject<DreamObjectMatrix>(out var right)) {
            right.GetVariable("a").TryGetValueAsFloat(out float rA);
            right.GetVariable("b").TryGetValueAsFloat(out float rB);
            right.GetVariable("c").TryGetValueAsFloat(out float rC);
            right.GetVariable("d").TryGetValueAsFloat(out float rD);
            right.GetVariable("e").TryGetValueAsFloat(out float rE);
            right.GetVariable("f").TryGetValueAsFloat(out float rF);

            SetVariableValue("a", new DreamValue(lA + rA));
            SetVariableValue("b", new DreamValue(lB + rB));
            SetVariableValue("c", new DreamValue(lC + rC));
            SetVariableValue("d", new DreamValue(lD + rD));
            SetVariableValue("e", new DreamValue(lE + rE));
            SetVariableValue("f", new DreamValue(lF + rF));

            return new(this);
        }

        return base.OperatorAppend(b);
    }

    public override DreamValue OperatorRemove(DreamValue b) {
        GetVariable("a").TryGetValueAsFloat(out float lA);
        GetVariable("b").TryGetValueAsFloat(out float lB);
        GetVariable("c").TryGetValueAsFloat(out float lC);
        GetVariable("d").TryGetValueAsFloat(out float lD);
        GetVariable("e").TryGetValueAsFloat(out float lE);
        GetVariable("f").TryGetValueAsFloat(out float lF);

        if (b.TryGetValueAsDreamObject<DreamObjectMatrix>(out var right)) {
            right.GetVariable("a").TryGetValueAsFloat(out float rA);
            right.GetVariable("b").TryGetValueAsFloat(out float rB);
            right.GetVariable("c").TryGetValueAsFloat(out float rC);
            right.GetVariable("d").TryGetValueAsFloat(out float rD);
            right.GetVariable("e").TryGetValueAsFloat(out float rE);
            right.GetVariable("f").TryGetValueAsFloat(out float rF);

            SetVariableValue("a", new DreamValue(lA - rA));
            SetVariableValue("b", new DreamValue(lB - rB));
            SetVariableValue("c", new DreamValue(lC - rC));
            SetVariableValue("d", new DreamValue(lD - rD));
            SetVariableValue("e", new DreamValue(lE - rE));
            SetVariableValue("f", new DreamValue(lF - rF));

            return new(this);
        }

        return base.OperatorRemove(b);
    }
    #endregion Operators

    #region Helpers
    /// <summary> Used to create a float array understandable by <see cref="IconAppearance.Transform"/> to be a transform. </summary>
    /// <returns>The matrix's values in an array, in [a,d,b,e,c,f] order.</returns>
    /// <remarks>This will not verify that this is a /matrix</remarks>
    public static float[] MatrixToTransformFloatArray(DreamObjectMatrix matrix) {
        float[] array = new float[6];
        matrix.GetVariable("a").TryGetValueAsFloat(out array[0]);
        matrix.GetVariable("d").TryGetValueAsFloat(out array[1]);
        matrix.GetVariable("b").TryGetValueAsFloat(out array[2]);
        matrix.GetVariable("e").TryGetValueAsFloat(out array[3]);
        matrix.GetVariable("c").TryGetValueAsFloat(out array[4]);
        matrix.GetVariable("f").TryGetValueAsFloat(out array[5]);
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
        newMatrix.SetVariableValue("a", new(a));
        newMatrix.SetVariableValue("b", new(b));
        newMatrix.SetVariableValue("c", new(c));
        newMatrix.SetVariableValue("d", new(d));
        newMatrix.SetVariableValue("e", new(e));
        newMatrix.SetVariableValue("f", new(f));
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
        try {
            return matrix.GetVariable("a").MustGetValueAsFloat() *
                   matrix.GetVariable("e").MustGetValueAsFloat() -
                   matrix.GetVariable("d").MustGetValueAsFloat() *
                   matrix.GetVariable("b").MustGetValueAsFloat();
        } catch(InvalidCastException) {
            return 0f;
        } catch(KeyNotFoundException) {
            return 0f;
        }
    }

    /// <summary>Inverts the given matrix, in-place.</summary>
    /// <returns>true if inversion was possible, false if not.</returns>
    public static bool TryInvert(DreamObjectMatrix matrix) {
        var determinant = Determinant(matrix);
        if (determinant == 0f)
            return false;
        var oldValues = EnumerateMatrix(matrix).ToImmutableArray();
        //Just going by what we used to have as DM code within DMStandard. No clue if the math is right, here
        matrix.SetVariableValue("a", new DreamValue( // a = e
                oldValues[4] / determinant
        ));
        matrix.SetVariableValue("b", new DreamValue( // b = -b
                -oldValues[1] / determinant
        ));
        matrix.SetVariableValue("c", new DreamValue( // c = b*f - e*c
                (oldValues[1] * oldValues[5] - oldValues[4] * oldValues[2]) / determinant
        ));
        matrix.SetVariableValue("d", new DreamValue( // d = -d
                -oldValues[3] / determinant
        ));
        matrix.SetVariableValue("e", new DreamValue( // e = a
                oldValues[0] / determinant
        ));
        matrix.SetVariableValue("f", new DreamValue( // f = d*c - a*f
                (oldValues[3] * oldValues[2] - oldValues[0] * oldValues[5]) / determinant
        ));
        return true;
    }

    /// <summary>
    /// Used when printing this matrix to enumerate its values in order.
    /// </summary>
    /// <returns>The matrix's values in [a,b,c,d,e,f] order.</returns>
    /// <remarks>This will not verify that this is a /matrix</remarks>
    public static IEnumerable<float> EnumerateMatrix(DreamObjectMatrix matrix) {
        float ret = 0f;
        matrix.GetVariable("a").TryGetValueAsFloat(out ret);
        yield return ret;
        matrix.GetVariable("b").TryGetValueAsFloat(out ret);
        yield return ret;
        matrix.GetVariable("c").TryGetValueAsFloat(out ret);
        yield return ret;
        matrix.GetVariable("d").TryGetValueAsFloat(out ret);
        yield return ret;
        matrix.GetVariable("e").TryGetValueAsFloat(out ret);
        yield return ret;
        matrix.GetVariable("f").TryGetValueAsFloat(out ret);
        yield return ret;
    }

    /// <summary> Translates a given matrix by the two translation factors given.</summary>
    /// <remarks> Note that this does use <see cref="DreamObject.SetVariableValue"/>.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if the matrix has non-float members.</exception>
    public static void TranslateMatrix(DreamObjectMatrix matrix, float x, float y) {
        try {
            matrix.SetVariableValue("c", new DreamValue(matrix.GetVariable("c").MustGetValueAsFloat() + x));
            matrix.SetVariableValue("f", new DreamValue(matrix.GetVariable("f").MustGetValueAsFloat() + y));
        } catch(InvalidCastException) { // If any of these MustGet()s fail, try to give a more descriptive runtime
            throw new InvalidOperationException($"Invalid matrix '{matrix}' cannot be scaled");
        }
    }

    /// <summary> Scales a given matrix by the two scaling factors given.</summary>
    /// <remarks> Note that this does use <see cref="DreamObject.SetVariableValue"/>.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if the matrix has non-float members.</exception>
    public static void ScaleMatrix(DreamObjectMatrix matrix, float x, float y) {
        try {
            matrix.SetVariableValue("a", new DreamValue(matrix.GetVariable("a").MustGetValueAsFloat() * x));
            matrix.SetVariableValue("b", new DreamValue(matrix.GetVariable("b").MustGetValueAsFloat() * x));
            matrix.SetVariableValue("c", new DreamValue(matrix.GetVariable("c").MustGetValueAsFloat() * x));

            matrix.SetVariableValue("d", new DreamValue(matrix.GetVariable("d").MustGetValueAsFloat() * y));
            matrix.SetVariableValue("e", new DreamValue(matrix.GetVariable("e").MustGetValueAsFloat() * y));
            matrix.SetVariableValue("f", new DreamValue(matrix.GetVariable("f").MustGetValueAsFloat() * y));
        } catch(InvalidCastException) { // If any of these MustGet()s fail, try to give a more descriptive runtime
            throw new InvalidOperationException($"Invalid matrix '{matrix}' cannot be scaled");
        }
    }

    /// <summary> Adds the second given matrix to the first given matrix. </summary>
    /// <remarks> Note that this does use <see cref="DreamObject.SetVariableValue"/>.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if either matrix has non-float members.</exception>
    public static void AddMatrix(DreamObjectMatrix lMatrix, DreamObjectMatrix rMatrix) {
        float lA;
        float lB;
        float lC;
        float lD;
        float lE;
        float lF;
        float rA;
        float rB;
        float rC;
        float rD;
        float rE;
        float rF;
        try {
            lA = lMatrix.GetVariable("a").MustGetValueAsFloat();
            lB = lMatrix.GetVariable("b").MustGetValueAsFloat();
            lC = lMatrix.GetVariable("c").MustGetValueAsFloat();
            lD = lMatrix.GetVariable("d").MustGetValueAsFloat();
            lE = lMatrix.GetVariable("e").MustGetValueAsFloat();
            lF = lMatrix.GetVariable("f").MustGetValueAsFloat();
            rA = rMatrix.GetVariable("a").MustGetValueAsFloat();
            rB = rMatrix.GetVariable("b").MustGetValueAsFloat();
            rC = rMatrix.GetVariable("c").MustGetValueAsFloat();
            rD = rMatrix.GetVariable("d").MustGetValueAsFloat();
            rE = rMatrix.GetVariable("e").MustGetValueAsFloat();
            rF = rMatrix.GetVariable("f").MustGetValueAsFloat();
        } catch (InvalidCastException) {
            throw new InvalidOperationException($"Invalid matrices '{lMatrix}' and '{rMatrix}' cannot be added.");
        }
        lMatrix.SetVariableValue("a", new DreamValue(lA + rA));
        lMatrix.SetVariableValue("b", new DreamValue(lB + rB));
        lMatrix.SetVariableValue("c", new DreamValue(lC + rC));
        lMatrix.SetVariableValue("d", new DreamValue(lD + rD));
        lMatrix.SetVariableValue("e", new DreamValue(lE + rE));
        lMatrix.SetVariableValue("f", new DreamValue(lF + rF));
    }

    /// <summary> Subtracts the second given matrix from the first given matrix. </summary>
    /// <remarks> Note that this does use <see cref="DreamObject.SetVariableValue"/>.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if either matrix has non-float members.</exception>
    public static void SubtractMatrix(DreamObjectMatrix lMatrix, DreamObjectMatrix rMatrix) {
        float lA;
        float lB;
        float lC;
        float lD;
        float lE;
        float lF;
        float rA;
        float rB;
        float rC;
        float rD;
        float rE;
        float rF;
        try {
            lA = lMatrix.GetVariable("a").MustGetValueAsFloat();
            lB = lMatrix.GetVariable("b").MustGetValueAsFloat();
            lC = lMatrix.GetVariable("c").MustGetValueAsFloat();
            lD = lMatrix.GetVariable("d").MustGetValueAsFloat();
            lE = lMatrix.GetVariable("e").MustGetValueAsFloat();
            lF = lMatrix.GetVariable("f").MustGetValueAsFloat();
            rA = rMatrix.GetVariable("a").MustGetValueAsFloat();
            rB = rMatrix.GetVariable("b").MustGetValueAsFloat();
            rC = rMatrix.GetVariable("c").MustGetValueAsFloat();
            rD = rMatrix.GetVariable("d").MustGetValueAsFloat();
            rE = rMatrix.GetVariable("e").MustGetValueAsFloat();
            rF = rMatrix.GetVariable("f").MustGetValueAsFloat();
        } catch (InvalidCastException) {
            throw new InvalidOperationException($"Invalid matrices '{lMatrix}' and '{rMatrix}' cannot be subtracted.");
        }
        lMatrix.SetVariableValue("a", new DreamValue(lA - rA));
        lMatrix.SetVariableValue("b", new DreamValue(lB - rB));
        lMatrix.SetVariableValue("c", new DreamValue(lC - rC));
        lMatrix.SetVariableValue("d", new DreamValue(lD - rD));
        lMatrix.SetVariableValue("e", new DreamValue(lE - rE));
        lMatrix.SetVariableValue("f", new DreamValue(lF - rF));
    }

    /// <summary> Multiplies the first given matrix by the other given matrix. </summary>
    /// <remarks> Note that this does use <see cref="DreamObject.SetVariableValue"/>.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if either matrix has non-float members.</exception>
    public static void MultiplyMatrix(DreamObjectMatrix lMatrix, DreamObjectMatrix rMatrix) {
        float lA;
        float lB;
        float lC;
        float lD;
        float lE;
        float lF;
        float rA;
        float rB;
        float rC;
        float rD;
        float rE;
        float rF;
        try {
            lA = lMatrix.GetVariable("a").MustGetValueAsFloat();
            lB = lMatrix.GetVariable("b").MustGetValueAsFloat();
            lC = lMatrix.GetVariable("c").MustGetValueAsFloat();
            lD = lMatrix.GetVariable("d").MustGetValueAsFloat();
            lE = lMatrix.GetVariable("e").MustGetValueAsFloat();
            lF = lMatrix.GetVariable("f").MustGetValueAsFloat();
            rA = rMatrix.GetVariable("a").MustGetValueAsFloat();
            rB = rMatrix.GetVariable("b").MustGetValueAsFloat();
            rC = rMatrix.GetVariable("c").MustGetValueAsFloat();
            rD = rMatrix.GetVariable("d").MustGetValueAsFloat();
            rE = rMatrix.GetVariable("e").MustGetValueAsFloat();
            rF = rMatrix.GetVariable("f").MustGetValueAsFloat();
        } catch (InvalidCastException) {
            throw new InvalidOperationException($"Invalid matrices '{lMatrix}' and '{rMatrix}' cannot be multiplied.");
        }
        lMatrix.SetVariableValue("a", new DreamValue(lA*rA + lD*rB));
        lMatrix.SetVariableValue("b", new DreamValue(lB*rA + lE*rB));
        lMatrix.SetVariableValue("c", new DreamValue(lC*rA + lF*rB + rC));
        lMatrix.SetVariableValue("d", new DreamValue(lA*rD + lD*rE));
        lMatrix.SetVariableValue("e", new DreamValue(lB*rD + lE*rE));
        lMatrix.SetVariableValue("f", new DreamValue(lC*rD + lF*rE + rF));
    }
    #endregion Helpers
}
