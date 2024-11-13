using DMCompiler.Compiler;
using Resource = DMCompiler.DM.Expressions.Resource;
using DMCompiler.Compiler.DM.AST;
using DMCompiler.DM.Expressions;
using String = DMCompiler.DM.Expressions.String;

namespace DMCompiler.DM.Builders;

internal class DMExpressionBuilder(DMCompiler compiler) {
    public DMCompiler Compiler => compiler;

    public enum ScopeMode {
        /// All in-scope procs and vars available
        Normal,

        /// Only global vars and procs available
        Static,

        /// Only global procs available
        FirstPassStatic
    }

    // TODO: Remove these terrible global flags
    public ScopeMode CurrentScopeMode = ScopeMode.Normal;
    public bool ScopeOperatorEnabled = false; // Enabled on the last pass of the code tree
    public static UnknownReference? EncounteredUnknownReference;

    /// <remarks>Don't use DMExpression.Create() inside this or anything it calls! It resets EncounteredUnknownReference</remarks>
    internal DMExpression BuildExpression(DMASTExpression expression, DMObject dmObject, DMProc proc, DreamPath? inferredPath = null) {
        DMExpression result;

        switch (expression) {
            case DMASTInvalidExpression:
                // No  Compiler.Emit() here because the parser should have emitted an error when making this
                return new BadExpression(Compiler, expression.Location);

            case DMASTExpressionConstant constant: result = BuildConstant(constant, dmObject, proc); break;
            case DMASTStringFormat stringFormat: result = BuildStringFormat(stringFormat, dmObject, proc, inferredPath); break;
            case DMASTIdentifier identifier: result = BuildIdentifier(identifier, dmObject, proc, inferredPath); break;
            case DMASTScopeIdentifier globalIdentifier: result = BuildScopeIdentifier(globalIdentifier, dmObject, proc, inferredPath); break;
            case DMASTCallableSelf: result = new ProcSelf(compiler, expression.Location, null, proc); break;
            case DMASTCallableSuper: result = new ProcSuper(compiler, expression.Location, dmObject, proc); break;
            case DMASTCallableProcIdentifier procIdentifier: result = BuildCallableProcIdentifier(procIdentifier, dmObject); break;
            case DMASTProcCall procCall: result = BuildProcCall(procCall, dmObject, proc, inferredPath); break;
            case DMASTAssign assign: result = BuildAssign(assign, dmObject, proc, inferredPath); break;
            case DMASTAssignInto assignInto: result = BuildAssignInto(assignInto, dmObject, proc, inferredPath); break;
            case DMASTEqual equal: result = BuildEqual(equal, dmObject, proc, inferredPath); break;
            case DMASTNotEqual notEqual: result = BuildNotEqual(notEqual, dmObject, proc, inferredPath); break;
            case DMASTDereference deref: result = BuildDereference(deref, dmObject, proc, inferredPath); break;
            case DMASTLocate locate: result = BuildLocate(locate, dmObject, proc, inferredPath); break;
            case DMASTImplicitIsType implicitIsType: result = BuildImplicitIsType(implicitIsType, dmObject, proc, inferredPath); break;
            case DMASTList list: result = BuildList(list, dmObject, proc); break;
            case DMASTDimensionalList dimensionalList: result = BuildDimensionalList(dimensionalList, dmObject, proc, inferredPath); break;
            case DMASTNewList newList: result = BuildNewList(newList, dmObject, proc, inferredPath); break;
            case DMASTAddText addText: result = BuildAddText(addText, dmObject, proc, inferredPath); break;
            case DMASTInput input: result = BuildInput(input, dmObject, proc); break;
            case DMASTPick pick: result = BuildPick(pick, dmObject, proc); break;
            case DMASTLog log: result = BuildLog(log, dmObject, proc, inferredPath); break;
            case DMASTCall call: result = BuildCall(call, dmObject, proc, inferredPath); break;
            case DMASTExpressionWrapped wrapped: result = BuildExpression(wrapped.Value, dmObject, proc, inferredPath); break;

            case DMASTNegate negate:
                result = new Negate(negate.Location, BuildExpression(negate.Value, dmObject, proc, inferredPath));
                break;
            case DMASTNot not:
                result = new Not(not.Location, BuildExpression(not.Value, dmObject, proc, inferredPath));
                break;
            case DMASTBinaryNot binaryNot:
                result = new BinaryNot(binaryNot.Location, BuildExpression(binaryNot.Value, dmObject, proc, inferredPath));
                break;
            case DMASTAdd add:
                result = new Add(add.Location,
                    BuildExpression(add.LHS, dmObject, proc, inferredPath),
                    BuildExpression(add.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTSubtract subtract:
                result = new Subtract(subtract.Location,
                    BuildExpression(subtract.LHS, dmObject, proc, inferredPath),
                    BuildExpression(subtract.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTMultiply multiply:
                result = new Multiply(multiply.Location,
                    BuildExpression(multiply.LHS, dmObject, proc, inferredPath),
                    BuildExpression(multiply.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTDivide divide:
                result = new Divide(divide.Location,
                    BuildExpression(divide.LHS, dmObject, proc, inferredPath),
                    BuildExpression(divide.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTModulus modulus:
                result = new Modulo(modulus.Location,
                    BuildExpression(modulus.LHS, dmObject, proc, inferredPath),
                    BuildExpression(modulus.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTModulusModulus modulusModulus:
                result = new ModuloModulo(modulusModulus.Location,
                    BuildExpression(modulusModulus.LHS, dmObject, proc, inferredPath),
                    BuildExpression(modulusModulus.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTPower power:
                result = new Power(power.Location,
                    BuildExpression(power.LHS, dmObject, proc, inferredPath),
                    BuildExpression(power.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTAppend append:
                result = new Append(append.Location,
                    BuildExpression(append.LHS, dmObject, proc, inferredPath),
                    BuildExpression(append.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTCombine combine:
                result = new Combine(combine.Location,
                    BuildExpression(combine.LHS, dmObject, proc, inferredPath),
                    BuildExpression(combine.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTRemove remove:
                result = new Remove(remove.Location,
                    BuildExpression(remove.LHS, dmObject, proc, inferredPath),
                    BuildExpression(remove.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTMask mask:
                result = new Mask(mask.Location,
                    BuildExpression(mask.LHS, dmObject, proc, inferredPath),
                    BuildExpression(mask.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTLogicalAndAssign lAnd:
                var lAndLHS = BuildExpression(lAnd.LHS, dmObject, proc, inferredPath);
                var lAndRHS = BuildExpression(lAnd.RHS, dmObject, proc, lAndLHS.NestedPath);

                result = new LogicalAndAssign(lAnd.Location,
                    lAndLHS,
                    lAndRHS);
                break;
            case DMASTLogicalOrAssign lOr:
                var lOrLHS = BuildExpression(lOr.LHS, dmObject, proc, inferredPath);
                var lOrRHS = BuildExpression(lOr.RHS, dmObject, proc, lOrLHS.NestedPath);

                result = new LogicalOrAssign(lOr.Location, lOrLHS, lOrRHS);
                break;
            case DMASTMultiplyAssign multiplyAssign:
                result = new MultiplyAssign(multiplyAssign.Location,
                    BuildExpression(multiplyAssign.LHS, dmObject, proc, inferredPath),
                    BuildExpression(multiplyAssign.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTDivideAssign divideAssign:
                result = new DivideAssign(divideAssign.Location,
                    BuildExpression(divideAssign.LHS, dmObject, proc, inferredPath),
                    BuildExpression(divideAssign.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTLeftShiftAssign leftShiftAssign:
                result = new LeftShiftAssign(leftShiftAssign.Location,
                    BuildExpression(leftShiftAssign.LHS, dmObject, proc, inferredPath),
                    BuildExpression(leftShiftAssign.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTRightShiftAssign rightShiftAssign:
                result = new RightShiftAssign(rightShiftAssign.Location,
                    BuildExpression(rightShiftAssign.LHS, dmObject, proc, inferredPath),
                    BuildExpression(rightShiftAssign.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTXorAssign xorAssign:
                result = new XorAssign(xorAssign.Location,
                    BuildExpression(xorAssign.LHS, dmObject, proc, inferredPath),
                    BuildExpression(xorAssign.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTModulusAssign modulusAssign:
                result = new ModulusAssign(modulusAssign.Location,
                    BuildExpression(modulusAssign.LHS, dmObject, proc, inferredPath),
                    BuildExpression(modulusAssign.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTModulusModulusAssign modulusModulusAssign:
                var mmAssignLHS = BuildExpression(modulusModulusAssign.LHS, dmObject, proc, inferredPath);
                var mmAssignRHS = BuildExpression(modulusModulusAssign.RHS, dmObject, proc, mmAssignLHS.NestedPath);

                result = new ModulusModulusAssign(modulusModulusAssign.Location, mmAssignLHS, mmAssignRHS);
                break;
            case DMASTLeftShift leftShift:
                result = new LeftShift(leftShift.Location,
                    BuildExpression(leftShift.LHS, dmObject, proc, inferredPath),
                    BuildExpression(leftShift.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTRightShift rightShift:
                result = new RightShift(rightShift.Location,
                    BuildExpression(rightShift.LHS, dmObject, proc, inferredPath),
                    BuildExpression(rightShift.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTBinaryAnd binaryAnd:
                result = new BinaryAnd(binaryAnd.Location,
                    BuildExpression(binaryAnd.LHS, dmObject, proc, inferredPath),
                    BuildExpression(binaryAnd.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTBinaryXor binaryXor:
                result = new BinaryXor(binaryXor.Location,
                    BuildExpression(binaryXor.LHS, dmObject, proc, inferredPath),
                    BuildExpression(binaryXor.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTBinaryOr binaryOr:
                result = new BinaryOr(binaryOr.Location,
                    BuildExpression(binaryOr.LHS, dmObject, proc, inferredPath),
                    BuildExpression(binaryOr.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTEquivalent equivalent:
                result = new Equivalent(equivalent.Location,
                    BuildExpression(equivalent.LHS, dmObject, proc, inferredPath),
                    BuildExpression(equivalent.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTNotEquivalent notEquivalent:
                result = new NotEquivalent(notEquivalent.Location,
                    BuildExpression(notEquivalent.LHS, dmObject, proc, inferredPath),
                    BuildExpression(notEquivalent.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTGreaterThan greaterThan:
                result = new GreaterThan(greaterThan.Location,
                    BuildExpression(greaterThan.LHS, dmObject, proc, inferredPath),
                    BuildExpression(greaterThan.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTGreaterThanOrEqual greaterThanOrEqual:
                result = new GreaterThanOrEqual(greaterThanOrEqual.Location,
                    BuildExpression(greaterThanOrEqual.LHS, dmObject, proc, inferredPath),
                    BuildExpression(greaterThanOrEqual.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTLessThan lessThan:
                result = new LessThan(lessThan.Location,
                    BuildExpression(lessThan.LHS, dmObject, proc, inferredPath),
                    BuildExpression(lessThan.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTLessThanOrEqual lessThanOrEqual:
                result = new LessThanOrEqual(lessThanOrEqual.Location,
                    BuildExpression(lessThanOrEqual.LHS, dmObject, proc, inferredPath),
                    BuildExpression(lessThanOrEqual.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTOr or:
                result = new Or(or.Location,
                    BuildExpression(or.LHS, dmObject, proc, inferredPath),
                    BuildExpression(or.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTAnd and:
                result = new And(and.Location,
                    BuildExpression(and.LHS, dmObject, proc, inferredPath),
                    BuildExpression(and.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTTernary ternary:
                result = new Ternary(ternary.Location,
                    BuildExpression(ternary.A, dmObject, proc, inferredPath),
                    BuildExpression(ternary.B, dmObject, proc, inferredPath),
                    BuildExpression(ternary.C ?? new DMASTConstantNull(ternary.Location), dmObject, proc, inferredPath));
                break;
            case DMASTNewPath newPath:
                if (BuildExpression(newPath.Path, dmObject, proc, inferredPath) is not IConstantPath path) {
                    result = BadExpression(WarningCode.BadExpression, newPath.Path.Location,
                        "Expected a path expression");
                    break;
                }

                result = new NewPath(Compiler, newPath.Location, path,
                    BuildArgumentList(newPath.Location, dmObject, proc, newPath.Parameters, inferredPath));
                break;
            case DMASTNewExpr newExpr:
                result = new New(Compiler, newExpr.Location,
                    BuildExpression(newExpr.Expression, dmObject, proc, inferredPath),
                    BuildArgumentList(newExpr.Location, dmObject, proc, newExpr.Parameters, inferredPath));
                break;
            case DMASTNewInferred newInferred:
                if (inferredPath is null) {
                    result = BadExpression(WarningCode.BadExpression, newInferred.Location, "Could not infer a type");
                    break;
                }

                var type = BuildPath(newInferred.Location, dmObject, inferredPath.Value);
                if (type is not IConstantPath inferredType) {
                    result = BadExpression(WarningCode.BadExpression, newInferred.Location,
                        $"Cannot instantiate {type}");
                    break;
                }

                result = new NewPath(Compiler, newInferred.Location, inferredType,
                    BuildArgumentList(newInferred.Location, dmObject, proc, newInferred.Parameters, inferredPath));
                break;
            case DMASTPreIncrement preIncrement:
                result = new PreIncrement(preIncrement.Location, BuildExpression(preIncrement.Value, dmObject, proc, inferredPath));
                break;
            case DMASTPostIncrement postIncrement:
                result = new PostIncrement(postIncrement.Location, BuildExpression(postIncrement.Value, dmObject, proc, inferredPath));
                break;
            case DMASTPreDecrement preDecrement:
                result = new PreDecrement(preDecrement.Location, BuildExpression(preDecrement.Value, dmObject, proc, inferredPath));
                break;
            case DMASTPostDecrement postDecrement:
                result = new PostDecrement(postDecrement.Location, BuildExpression(postDecrement.Value, dmObject, proc, inferredPath));
                break;
            case DMASTPointerRef pointerRef:
                result = new PointerRef(pointerRef.Location, BuildExpression(pointerRef.Value, dmObject, proc, inferredPath));
                break;
            case DMASTPointerDeref pointerDeref:
                result = new PointerDeref(pointerDeref.Location, BuildExpression(pointerDeref.Value, dmObject, proc, inferredPath));
                break;
            case DMASTGradient gradient:
                result = new Gradient(Compiler, gradient.Location,
                    BuildArgumentList(gradient.Location, dmObject, proc, gradient.Parameters));
                break;
            case DMASTRgb rgb:
                result = new Rgb(Compiler, rgb.Location, BuildArgumentList(rgb.Location, dmObject, proc, rgb.Parameters));
                break;
            case DMASTLocateCoordinates locateCoordinates:
                result = new LocateCoordinates(Compiler, locateCoordinates.Location,
                    BuildExpression(locateCoordinates.X, dmObject, proc, inferredPath),
                    BuildExpression(locateCoordinates.Y, dmObject, proc, inferredPath),
                    BuildExpression(locateCoordinates.Z, dmObject, proc, inferredPath));
                break;
            case DMASTIsSaved isSaved:
                result = new IsSaved(Compiler, isSaved.Location, BuildExpression(isSaved.Value, dmObject, proc, inferredPath));
                break;
            case DMASTIsType isType: {
                if (isType.RHS is DMASTIdentifier { Identifier: "__IMPLIED_TYPE__" }) {
                    var expr = BuildExpression(isType.LHS, dmObject, proc, inferredPath);
                    if (expr.Path is null) {
                        result = BadExpression(WarningCode.BadExpression, isType.Location,
                            "A type could not be inferred!");
                        break;
                    }

                    result = new IsTypeInferred(Compiler, isType.Location, expr, expr.Path.Value);
                    break;
                }

                result = new IsType(Compiler, isType.Location,
                    BuildExpression(isType.LHS, dmObject, proc, inferredPath),
                    BuildExpression(isType.RHS, dmObject, proc, inferredPath));
                break;
            }

            case DMASTIsNull isNull:
                result = new IsNull(Compiler, isNull.Location, BuildExpression(isNull.Value, dmObject, proc, inferredPath));
                break;
            case DMASTLength length:
                result = new Length(Compiler, length.Location, BuildExpression(length.Value, dmObject, proc, inferredPath));
                break;
            case DMASTGetStep getStep:
                result = new GetStep(Compiler, getStep.Location,
                    BuildExpression(getStep.LHS, dmObject, proc, inferredPath),
                    BuildExpression(getStep.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTGetDir getDir:
                result = new GetDir(Compiler, getDir.Location,
                    BuildExpression(getDir.LHS, dmObject, proc, inferredPath),
                    BuildExpression(getDir.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTProb prob:
                result = new Prob(Compiler, prob.Location,
                    BuildExpression(prob.Value, dmObject, proc, inferredPath));
                break;
            case DMASTInitial initial:
                result = new Initial(Compiler, initial.Location, BuildExpression(initial.Value, dmObject, proc, inferredPath));
                break;
            case DMASTNameof nameof:
                result = BuildNameof(nameof, dmObject, proc, inferredPath);
                break;
            case DMASTExpressionIn expressionIn:
                var exprInLHS = BuildExpression(expressionIn.LHS, dmObject, proc, inferredPath);
                var exprInRHS = BuildExpression(expressionIn.RHS, dmObject, proc, inferredPath);
                if ((expressionIn.LHS is not DMASTExpressionWrapped && exprInLHS is UnaryOp or BinaryOp or Ternary) ||
                    (expressionIn.RHS is not DMASTExpressionWrapped && exprInRHS is BinaryOp or Ternary)) {
                    Compiler.Emit(WarningCode.AmbiguousInOrder, expressionIn.Location,
                        "Order of operations for \"in\" may not be what is expected. Use parentheses to be more explicit.");
                }

                result = new In(expressionIn.Location, exprInLHS, exprInRHS);
                break;
            case DMASTExpressionInRange expressionInRange:
                result = new InRange(expressionInRange.Location,
                    BuildExpression(expressionInRange.Value, dmObject, proc, inferredPath),
                    BuildExpression(expressionInRange.StartRange, dmObject, proc, inferredPath),
                    BuildExpression(expressionInRange.EndRange, dmObject, proc, inferredPath));
                break;
            case DMASTSin sin:
                result = new Sin(Compiler, sin.Location, BuildExpression(sin.Value, dmObject, proc, inferredPath));
                break;
            case DMASTCos cos:
                result = new Cos(Compiler, cos.Location, BuildExpression(cos.Value, dmObject, proc, inferredPath));
                break;
            case DMASTTan tan:
                result = new Tan(Compiler, tan.Location, BuildExpression(tan.Value, dmObject, proc, inferredPath));
                break;
            case DMASTArcsin arcSin:
                result = new ArcSin(Compiler, arcSin.Location, BuildExpression(arcSin.Value, dmObject, proc, inferredPath));
                break;
            case DMASTArccos arcCos:
                result = new ArcCos(Compiler, arcCos.Location, BuildExpression(arcCos.Value, dmObject, proc, inferredPath));
                break;
            case DMASTArctan arcTan:
                result = new ArcTan(Compiler, arcTan.Location, BuildExpression(arcTan.Value, dmObject, proc, inferredPath));
                break;
            case DMASTArctan2 arcTan2:
                result = new ArcTan2(Compiler, arcTan2.Location,
                    BuildExpression(arcTan2.LHS, dmObject, proc, inferredPath),
                    BuildExpression(arcTan2.RHS, dmObject, proc, inferredPath));
                break;
            case DMASTSqrt sqrt:
                result = new Sqrt(Compiler, sqrt.Location, BuildExpression(sqrt.Value, dmObject, proc, inferredPath));
                break;
            case DMASTAbs abs:
                result = new Abs(Compiler, abs.Location, BuildExpression(abs.Value, dmObject, proc, inferredPath));
                break;
            case DMASTVarDeclExpression varDeclExpr:
                var declIdentifier = new DMASTIdentifier(expression.Location, varDeclExpr.DeclPath.Path.LastElement);

                result = BuildIdentifier(declIdentifier, dmObject, proc);
                break;
            case DMASTVoid:
                result = BadExpression(WarningCode.BadExpression, expression.Location, "Attempt to use a void expression");
                break;
            default:
                throw new ArgumentException($"Invalid expression {expression}", nameof(expression));
        }

        if (EncounteredUnknownReference != null) {
            return EncounteredUnknownReference;
        } else {
            return result;
        }
    }

    private DMExpression BuildConstant(DMASTExpressionConstant constant, DMObject dmObject, DMProc proc) {
        switch (constant) {
            case DMASTConstantNull: return new Null(compiler, constant.Location);
            case DMASTConstantInteger constInt: return new Number(compiler, constant.Location, constInt.Value);
            case DMASTConstantFloat constFloat: return new Number(compiler, constant.Location, constFloat.Value);
            case DMASTConstantString constString: return new String(compiler, constant.Location, constString.Value);
            case DMASTConstantResource constResource: return new Resource(compiler, constant.Location, constResource.Path);
            case DMASTConstantPath constPath: return BuildPath(constant.Location, dmObject, constPath.Value.Path);
            case DMASTUpwardPathSearch upwardSearch:
                BuildExpression(upwardSearch.Path, dmObject, proc).TryAsConstant(out var pathExpr);
                if (pathExpr is not IConstantPath expr)
                    return BadExpression(WarningCode.BadExpression, constant.Location,
                        $"Cannot do an upward path search on {pathExpr}");

                var path = expr.Path;
                if (path == null)
                    return UnknownReference(constant.Location,
                        $"Cannot search on {expr}");

                DreamPath? foundPath = Compiler.DMObjectTree.UpwardSearch(path.Value, upwardSearch.Search.Path);
                if (foundPath == null)
                    return UnknownReference(constant.Location,
                        $"Could not find path {path}.{upwardSearch.Search.Path}");

                return BuildPath(constant.Location, dmObject, foundPath.Value);
        }

        throw new ArgumentException($"Invalid constant {constant}", nameof(constant));
    }

    private StringFormat BuildStringFormat(DMASTStringFormat stringFormat, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var expressions = new DMExpression[stringFormat.InterpolatedValues.Length];

        for (int i = 0; i < stringFormat.InterpolatedValues.Length; i++) {
            var interpolatedValue = stringFormat.InterpolatedValues[i];

            if (interpolatedValue == null) {
                expressions[i] = new Null(compiler, stringFormat.Location);
            } else {
                expressions[i] = BuildExpression(interpolatedValue, dmObject, proc, inferredPath);
            }
        }

        return new StringFormat(Compiler, stringFormat.Location, stringFormat.Value, expressions);
    }

    private DMExpression BuildPath(Location location, DMObject dmObject, DreamPath path) {
        // An upward search with no left-hand side
        if (path.Type == DreamPath.PathType.UpwardSearch) {
            DreamPath? foundPath = compiler.DMCodeTree.UpwardSearch(dmObject, path);
            if (foundPath == null)
                return UnknownReference(location, $"Could not find path {path}");

            path = foundPath.Value;
        }

        // /datum/proc or /datum/verb
        if (path.LastElement is "proc" or "verb") {
            DreamPath typePath = path.FromElements(0, -2);
            if (!Compiler.DMObjectTree.TryGetDMObject(typePath, out var stubOfType))
                return UnknownReference(location, $"Type {typePath} does not exist");

            return new ConstantProcStub(Compiler, location, stubOfType, path.LastElement is "verb");
        }

        // /datum
        if (Compiler.DMObjectTree.TryGetDMObject(path, out var referencing)) {
            return new ConstantTypeReference(Compiler, location, referencing);
        }

        // /datum/proc/foo
        int procIndex = path.FindElement("proc");
        if (procIndex == -1) procIndex = path.FindElement("verb");
        if (procIndex != -1) {
            DreamPath withoutProcElement = path.RemoveElement(procIndex);
            DreamPath ownerPath = withoutProcElement.FromElements(0, -2);
            string procName = path.LastElement!;

            if (!Compiler.DMObjectTree.TryGetDMObject(ownerPath, out var owner))
                return UnknownReference(location, $"Type {ownerPath} does not exist");

            int? procId;
            if (owner == Compiler.DMObjectTree.Root && Compiler.DMObjectTree.TryGetGlobalProc(procName, out var globalProc)) {
                procId = globalProc.Id;
            } else {
                var procs = owner.GetProcs(procName);

                procId = procs?[^1];
            }

            if (procId == null || Compiler.DMObjectTree.AllProcs.Count < procId) {
                return UnknownReference(location, $"Could not find proc {procName}() on {ownerPath}");
            }

            return new ConstantProcReference(Compiler, location, path, Compiler.DMObjectTree.AllProcs[procId.Value]);
        }

        return UnknownReference(location, $"Path {path} does not exist");
    }

    private DMExpression BuildIdentifier(DMASTIdentifier identifier, DMObject dmObject, DMProc proc, DreamPath? inferredPath = null) {
        var name = identifier.Identifier;

        switch (name) {
            case "src":
                return new Src(Compiler, identifier.Location, dmObject.Path);
            case "usr":
                return new Usr(Compiler, identifier.Location);
            case "args":
                return new Args(Compiler, identifier.Location);
            case "world":
                if (CurrentScopeMode == ScopeMode.FirstPassStatic) // world is not available on the first pass
                    return UnknownIdentifier(identifier.Location, "world");

                return new World(Compiler, identifier.Location);
            case "__TYPE__":
                return new ProcOwnerType(Compiler, identifier.Location, dmObject);
            case "__IMPLIED_TYPE__":
                if (inferredPath == null)
                    return BadExpression(WarningCode.BadExpression, identifier.Location,
                        "__IMPLIED_TYPE__ cannot be used here, there is no type being implied");

                return BuildPath(identifier.Location, dmObject, inferredPath.Value);
            case "__PROC__": // The saner alternative to "....."
                var path = dmObject.Path.AddToPath("proc/" + proc.Name);

                return new ConstantProcReference(compiler, identifier.Location, path, proc);
            case "global":
                return new Global(Compiler, identifier.Location);
            default: {
                if (CurrentScopeMode == ScopeMode.Normal) {
                    var localVar = proc?.GetLocalVariable(name);
                    if (localVar != null)
                        return new Local(Compiler, identifier.Location, localVar);
                }

                var field = dmObject.GetVariable(name);
                if (field != null) {
                    return new Field(Compiler, identifier.Location, field, field.ValType);
                }

                var globalId = proc?.GetGlobalVariableId(name) ?? dmObject.GetGlobalVariableId(name);

                if (globalId != null) {
                    var globalVar = Compiler.DMObjectTree.Globals[globalId.Value];
                    var global = new GlobalField(Compiler, identifier.Location, globalVar.Type, globalId.Value, globalVar.ValType);
                    return global;
                }

                return UnknownIdentifier(identifier.Location, name);
            }
        }
    }

    private DMExpression BuildScopeIdentifier(
        DMASTScopeIdentifier scopeIdentifier,
        DMObject dmObject, DMProc proc,
        DreamPath? inferredPath) {
        var location = scopeIdentifier.Location;
        var bIdentifier = scopeIdentifier.Identifier;

        if (scopeIdentifier.Expression == null) { // ::A, shorthand for global.A
            if (scopeIdentifier.IsProcRef) { // ::A(), global proc ref
                if (!Compiler.DMObjectTree.TryGetGlobalProc(bIdentifier, out var globalProc))
                    return UnknownReference(location, $"No global proc named \"{bIdentifier}\" exists");

                var arguments = BuildArgumentList(location, dmObject, proc, scopeIdentifier.CallArguments, inferredPath);
                return new ProcCall(location, new GlobalProc(Compiler, location, globalProc), arguments, DMValueType.Anything);
            }

            // ::vars, special case
            if (bIdentifier == "vars")
                return new GlobalVars(Compiler, location);

            // ::A, global var ref
            var globalId = Compiler.DMObjectTree.Root.GetGlobalVariableId(bIdentifier);
            if (globalId == null)
                return UnknownIdentifier(location, bIdentifier);

            var globalVar = Compiler.DMObjectTree.Globals [globalId.Value];
            return new GlobalField(Compiler, location,
                Compiler.DMObjectTree.Globals[globalId.Value].Type,
                globalId.Value,
                globalVar.ValType);
        }

        // Other uses should wait until the scope operator pass
        if (!ScopeOperatorEnabled)
            return UnknownIdentifier(location, bIdentifier);

        DMExpression? expression;

        // "type" and "parent_type" cannot resolve in a context, but it's still valid with scope identifiers
        if (scopeIdentifier.Expression is DMASTIdentifier { Identifier: "type" or "parent_type" } identifier) {
            // This is the same behaviour as in BYOND, but BYOND simply raises an undefined var error.
            // We want to give end users an explanation at least.
            if (CurrentScopeMode is ScopeMode.Normal && proc != null)
                return BadExpression(WarningCode.BadExpression, identifier.Location,
                    "Use of \"type::\" and \"parent_type::\" outside of a context is forbidden");

            if (identifier.Identifier == "parent_type") {
                if (dmObject.Parent == null)
                    return BadExpression(WarningCode.ItemDoesntExist, identifier.Location,
                        $"Type {dmObject.Path} does not have a parent");

                expression = BuildPath(location, dmObject, dmObject.Parent.Path);
            } else { // "type"
                expression = BuildPath(location, dmObject, dmObject.Path);
            }
        } else {
            expression = BuildExpression(scopeIdentifier.Expression, dmObject, proc, inferredPath);
        }

        // A needs to have a type
        if (expression.Path == null)
            return BadExpression(WarningCode.BadExpression, expression.Location,
                $"Identifier \"{expression.GetNameof(dmObject)}\" does not have a type");

        if (!compiler.DMObjectTree.TryGetDMObject(expression.Path.Value, out var owner)) {
            if (expression is ConstantProcReference procReference) {
                if (bIdentifier == "name")
                    return new String(compiler, expression.Location, procReference.Value.Name);

                return BadExpression(WarningCode.PointlessScopeOperator, expression.Location,
                    "scope operator returns null on proc variables other than \"name\"");
            }

            return BadExpression(WarningCode.ItemDoesntExist, expression.Location,
                $"Type {expression.Path.Value} does not exist");
        }

        if (scopeIdentifier.IsProcRef) { // A::B()
            var procs = owner.GetProcs(bIdentifier);
            if (procs == null)
                return BadExpression(WarningCode.ItemDoesntExist, location,
                    $"Type {owner.Path} does not have a proc named \"{bIdentifier}\"");

            var referencedProc = Compiler.DMObjectTree.AllProcs[procs[^1]];
            var path = owner.Path.AddToPath("proc/" + referencedProc.Name);
            return new ConstantProcReference(compiler, location, path, referencedProc);
        } else { // A::B
            var globalVarId = owner.GetGlobalVariableId(bIdentifier);
            if (globalVarId != null) {
                // B is a var.
                // This is the only case a ScopeIdentifier can be an LValue.
                var globalVar = Compiler.DMObjectTree.Globals [globalVarId.Value];
                return new GlobalField(Compiler, location, globalVar.Type, globalVarId.Value, globalVar.ValType);
            }

            var variable = owner.GetVariable(bIdentifier);
            if (variable == null)
                return UnknownIdentifier(location, bIdentifier);

            return new ScopeReference(location, expression, bIdentifier, variable);
        }
    }

    private DMExpression BuildCallableProcIdentifier(DMASTCallableProcIdentifier procIdentifier, DMObject dmObject) {
        if (CurrentScopeMode is ScopeMode.Static or ScopeMode.FirstPassStatic) {
            if (!Compiler.DMObjectTree.TryGetGlobalProc(procIdentifier.Identifier, out var staticScopeGlobalProc))
                return UnknownReference(procIdentifier.Location,
                    $"Type {dmObject.Path} does not have a proc named \"{procIdentifier.Identifier}\"");

            return new GlobalProc(Compiler, procIdentifier.Location, staticScopeGlobalProc);
        }

        if (dmObject.HasProc(procIdentifier.Identifier)) {
            return new Proc(Compiler, procIdentifier.Location, procIdentifier.Identifier);
        }

        if (Compiler.DMObjectTree.TryGetGlobalProc(procIdentifier.Identifier, out var globalProc)) {
            return new GlobalProc(Compiler, procIdentifier.Location, globalProc);
        }

        return UnknownReference(procIdentifier.Location,
            $"Type {dmObject.Path} does not have a proc named \"{procIdentifier.Identifier}\"");
    }

    private DMExpression BuildProcCall(DMASTProcCall procCall, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        // arglist hack
        if (procCall.Callable is DMASTCallableProcIdentifier { Identifier: "arglist" }) {
            switch (procCall.Parameters.Length) {
                case 0:
                    Compiler.Emit(WarningCode.BadArgument, procCall.Location, "arglist() requires 1 argument");
                    break;
                case 1:
                    break;
                default:
                    Compiler.Emit(
                        WarningCode.InvalidArgumentCount,
                        procCall.Location,
                        $"arglist() given {procCall.Parameters.Length} arguments, expecting 1");
                    break;
            }

            var expr = BuildExpression(procCall.Parameters[0].Value, dmObject, proc, inferredPath);
            return new Arglist(Compiler, procCall.Location, expr);
        }

        var target = BuildExpression((DMASTExpression)procCall.Callable, dmObject, proc, inferredPath);
        var args = BuildArgumentList(procCall.Location, dmObject, proc, procCall.Parameters);
        if (target is Proc targetProc) { // GlobalProc handles returnType itself
            var returnType = targetProc.GetReturnType(dmObject);

            return new ProcCall(procCall.Location, target, args, returnType);
        }

        return new ProcCall(procCall.Location, target, args, DMValueType.Anything);
    }

    private ArgumentList BuildArgumentList(Location location, DMObject dmObject, DMProc proc, DMASTCallParameter[]? arguments, DreamPath? inferredPath = null) {
        if (arguments == null || arguments.Length == 0)
            return new ArgumentList(location, [], false);

        var expressions = new (string?, DMExpression)[arguments.Length];
        bool isKeyed = false;

        int idx = 0;
        foreach(var arg in arguments) {
            var value = BuildExpression(arg.Value, dmObject, proc, inferredPath);
            var key = (arg.Key != null) ? BuildExpression(arg.Key, dmObject, proc, inferredPath) : null;
            int argIndex = idx++;
            string? name = null;

            switch (key) {
                case String keyStr:
                    name = keyStr.Value;
                    break;
                case Number keyNum:
                    //Replaces an ordered argument
                    var newIdx = (int)keyNum.Value - 1;

                    if (newIdx == argIndex) {
                        Compiler.Emit(WarningCode.PointlessPositionalArgument, key.Location,
                            $"The argument at index {argIndex + 1} is a positional argument with a redundant index (\"{argIndex + 1} = value\" at argument {argIndex + 1}). This does not function like a named argument and is likely a mistake.");
                    }

                    argIndex = newIdx;
                    break;
                case Resource:
                case IConstantPath:
                    //The key becomes the value
                    value = key;
                    break;

                default:
                    if (key != null && key is not Expressions.UnknownReference) {
                        Compiler.Emit(WarningCode.InvalidArgumentKey, key.Location, $"Invalid argument key {key}");
                    }

                    break;
            }

            if (name != null)
                isKeyed = true;

            expressions[argIndex] = (name, value);
        }

        return new ArgumentList(location, expressions, isKeyed);
    }

    private DMExpression BuildAssign(DMASTAssign assign, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var lhs = BuildExpression(assign.LHS, dmObject, proc, inferredPath);
        var rhs = BuildExpression(assign.RHS, dmObject, proc, lhs.NestedPath);
        if(lhs.TryAsConstant(out _)) {
            Compiler.Emit(WarningCode.WriteToConstant, assign.LHS.Location, "Cannot write to const var");
        }

        return new Assignment(assign.Location, lhs, rhs);
    }

    private DMExpression BuildAssignInto(DMASTAssignInto assign, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var lhs = BuildExpression(assign.LHS, dmObject, proc, inferredPath);
        var rhs = BuildExpression(assign.RHS, dmObject, proc, lhs.NestedPath);
        if(lhs.TryAsConstant(out _)) {
            Compiler.Emit(WarningCode.WriteToConstant, assign.LHS.Location, "Cannot write to const var");
        }

        return new AssignmentInto(assign.Location, lhs, rhs);
    }

    private DMExpression BuildEqual(DMASTEqual equal, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var lhs = BuildExpression(equal.LHS, dmObject, proc, inferredPath);
        var rhs = BuildExpression(equal.RHS, dmObject, proc, inferredPath);

        // (x == null) can be changed to isnull(x) which compiles down to an opcode
        // TODO: Bytecode optimizations instead
        if (rhs is Null)
            return new IsNull(Compiler, equal.Location, lhs);

        return new Equal(equal.Location, lhs, rhs);
    }

    private DMExpression BuildNotEqual(DMASTNotEqual notEqual, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var lhs = BuildExpression(notEqual.LHS, dmObject, proc, inferredPath);
        var rhs = BuildExpression(notEqual.RHS, dmObject, proc, inferredPath);

        // (x != null) can be changed to !isnull(x) which compiles down to two opcodes
        // TODO: Bytecode optimizations instead
        if (rhs is Null)
            return new Not(notEqual.Location, new IsNull(Compiler, notEqual.Location, lhs));

        return new NotEqual(notEqual.Location, lhs, rhs);
    }

    private DMExpression BuildDereference(DMASTDereference deref, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var astOperations = deref.Operations;

        // The base expression and list of operations to perform on it
        // These may be redefined if we encounter a global access mid-operation
        var expr = BuildExpression(deref.Expression, dmObject, proc, inferredPath);
        var operations = new Dereference.Operation[deref.Operations.Length];
        int astOperationOffset = 0;

        // Path of the previous operation that was iterated over (starting as the base expression)
        DreamPath? prevPath = expr.Path;
        var pathIsFuzzy = expr.PathIsFuzzy;

        // Special behaviour for `global.x`, `global.vars`, and `global.f()`
        if (expr is Global) {
            DMASTDereference.Operation firstOperation = astOperations[0];

            if (firstOperation is DMASTDereference.NamedOperation namedOperation) {
                prevPath = null;
                pathIsFuzzy = true;

                switch (namedOperation) {
                    // global.f()
                    case DMASTDereference.CallOperation callOperation:
                        if (!Compiler.DMObjectTree.TryGetGlobalProc(callOperation.Identifier, out var globalProc))
                            return UnknownReference(callOperation.Location,
                                $"Could not find a global proc named \"{callOperation.Identifier}\"");

                        var argumentList = BuildArgumentList(deref.Expression.Location, dmObject, proc,
                            callOperation.Parameters);

                        var globalProcExpr = new GlobalProc(compiler, expr.Location, globalProc);
                        expr = new ProcCall(expr.Location, globalProcExpr, argumentList, DMValueType.Anything);
                        break;

                    case DMASTDereference.FieldOperation:
                        // global.vars
                        if (namedOperation is { Identifier: "vars" }) {
                            expr = new GlobalVars(Compiler, expr.Location);
                            break;
                        }

                        // global.variable
                        var globalId = dmObject.GetGlobalVariableId(namedOperation.Identifier);
                        if (globalId == null)
                            return UnknownIdentifier(deref.Location, $"global.{namedOperation.Identifier}");

                        var property = Compiler.DMObjectTree.Globals [globalId.Value];
                        expr = new GlobalField(Compiler, expr.Location, property.Type, globalId.Value, property.ValType);

                        prevPath = property.Type;
                        pathIsFuzzy = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"Missing implementation for {namedOperation}");
                }

                var newOperationCount = operations.Length - 1;
                if (newOperationCount == 0) {
                    return expr;
                }

                operations = new Dereference.Operation[newOperationCount];
                astOperationOffset = 1;
            } else {
                 Compiler.Emit(WarningCode.BadExpression, firstOperation.Location,
                    "Invalid dereference operation performed on global");
                expr = new Null(compiler, firstOperation.Location);
            }
        }

        for (int i = 0; i < operations.Length; i++) {
            DMASTDereference.Operation astOperation = astOperations[i + astOperationOffset];
            Dereference.Operation operation;

            switch (astOperation) {
                case DMASTDereference.FieldOperation fieldOperation: {
                    var field = fieldOperation.Identifier;

                    DMVariable? property = null;

                    // If the last operation evaluated as an ambiguous type, we force the next operation to be a search
                    if (!fieldOperation.NoSearch && !pathIsFuzzy) {
                        if (prevPath == null)
                            return UnknownIdentifier(deref.Location, field);
                        if (!Compiler.DMObjectTree.TryGetDMObject(prevPath.Value, out var fromObject))
                            return UnknownReference(fieldOperation.Location,
                                $"Type {prevPath.Value} does not exist");

                        property = fromObject.GetVariable(field);
                        if (!fieldOperation.Safe && fromObject.IsSubtypeOf(DreamPath.Client)) {
                             Compiler.Emit(WarningCode.UnsafeClientAccess, deref.Location,
                                "Unsafe \"client\" access. Use the \"?.\" operator instead");
                        }

                        if (property == null && fromObject.GetGlobalVariableId(field) is { } globalId) {
                            property = Compiler.DMObjectTree.Globals [globalId];

                            expr = new GlobalField(Compiler, expr.Location, property.Type, globalId, property.ValType);

                            var newOperationCount = operations.Length - i - 1;
                            if (newOperationCount == 0) {
                                return expr;
                            }

                            if (property.ValType.IsUnimplemented) {
                                Compiler.UnimplementedWarning(deref.Location,
                                    $"{prevPath}.{field} is not implemented and will have unexpected behavior");
                            }

                            operations = new Dereference.Operation[newOperationCount];
                            astOperationOffset += i + 1;
                            i = -1;
                            prevPath = property.Type;
                            pathIsFuzzy = prevPath == null;
                            continue;
                        }

                        if (property == null) {
                            return UnknownIdentifier(deref.Location, field);
                        }
                    }

                    operation = new Dereference.FieldOperation {
                        Safe = fieldOperation.Safe,
                        Identifier = fieldOperation.Identifier,
                        Path = property?.Type
                    };

                    prevPath = property?.Type;
                    pathIsFuzzy = property == null;
                    break;
                }

                case DMASTDereference.IndexOperation indexOperation:
                    operation = new Dereference.IndexOperation {
                        // var/type1/result = new /type2()[new()] changes the inferred new to "new /type1()"
                        // L[new()] = new() uses the type of L however
                        Index = BuildExpression(indexOperation.Index, dmObject, proc, inferredPath ?? prevPath),
                        Safe = indexOperation.Safe,
                        Path = prevPath
                    };
                    prevPath = null;
                    pathIsFuzzy = true;
                    break;

                case DMASTDereference.CallOperation callOperation: {
                    var field = callOperation.Identifier;
                    var argumentList = BuildArgumentList(deref.Expression.Location, dmObject, proc,
                        callOperation.Parameters);

                    if (!callOperation.NoSearch && !pathIsFuzzy) {
                        if (prevPath == null) {
                            return UnknownIdentifier(deref.Location, field);
                        }

                        if (!Compiler.DMObjectTree.TryGetDMObject(prevPath.Value, out var fromObject))
                            return UnknownReference(callOperation.Location, $"Type {prevPath.Value} does not exist");
                        if (!fromObject.HasProc(field))
                            return UnknownIdentifier(callOperation.Location, field);
                    }

                    operation = new Dereference.CallOperation {
                        Parameters = argumentList,
                        Safe = callOperation.Safe,
                        Identifier = field,
                        Path = null
                    };
                    prevPath = null;
                    pathIsFuzzy = true;
                    break;
                }

                default:
                    throw new InvalidOperationException("unhandled deref operation kind");
            }

            operations[i] = operation;
        }

        // The final value in prevPath is our expression's path!
        return new Dereference(deref.Location, prevPath, expr, operations);
    }

    private DMExpression BuildLocate(DMASTLocate locate, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var container = locate.Container != null ? BuildExpression(locate.Container, dmObject, proc, inferredPath) : null;

        if (locate.Expression == null) {
            if (inferredPath == null)
                return BadExpression(WarningCode.BadExpression, locate.Location, "inferred locate requires a type");

            return new LocateInferred(Compiler, locate.Location, inferredPath.Value, container);
        }

        var pathExpr = BuildExpression(locate.Expression, dmObject, proc, inferredPath);
        return new Locate(Compiler, locate.Location, pathExpr, container);
    }

    private DMExpression BuildImplicitIsType(DMASTImplicitIsType isType, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var expr = BuildExpression(isType.Value, dmObject, proc, inferredPath);

        if (expr.Path is null)
            return BadExpression(WarningCode.BadExpression, isType.Location, "An inferred istype requires a type!");

        return new IsTypeInferred(Compiler, isType.Location, expr, expr.Path.Value);
    }

    private DMExpression BuildList(DMASTList list, DMObject dmObject, DMProc proc) {
        (DMExpression? Key, DMExpression Value)[] values = [];

        if (list.Values != null) {
            values = new (DMExpression?, DMExpression)[list.Values.Length];

            for (int i = 0; i < list.Values.Length; i++) {
                DMASTCallParameter value = list.Values[i];
                DMExpression? key = (value.Key != null) ? BuildExpression(value.Key, dmObject, proc) : null;
                DMExpression listValue = BuildExpression(value.Value, dmObject, proc);

                values[i] = (key, listValue);
            }
        }

        return new List(Compiler, list.Location, values);
    }

    private DMExpression BuildDimensionalList(DMASTDimensionalList list, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var sizes = new DMExpression[list.Sizes.Count];
        for (int i = 0; i < sizes.Length; i++) {
            var sizeExpr = BuildExpression(list.Sizes[i], dmObject, proc, inferredPath);

            sizes[i] = sizeExpr;
        }

        return new DimensionalList(Compiler, list.Location, sizes);
    }

    // nameof(x)
    private DMExpression BuildNameof(DMASTNameof nameof, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var expr = BuildExpression(nameof.Value, dmObject, proc, inferredPath);
        if (expr.GetNameof(dmObject) is { } name) {
            return new String(compiler, nameof.Location, name);
        }

        return BadExpression(WarningCode.BadArgument, nameof.Location, "nameof() requires a var, proc reference, or type path");
    }

    private DMExpression BuildNewList(DMASTNewList newList, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        DMExpression[] expressions = new DMExpression[newList.Parameters.Length];

        for (int i = 0; i < newList.Parameters.Length; i++) {
            DMASTCallParameter parameter = newList.Parameters[i];
            if (parameter.Key != null)
                return BadExpression(WarningCode.InvalidArgumentKey, parameter.Location,
                    "newlist() does not take named arguments");

            expressions[i] = BuildExpression(parameter.Value, dmObject, proc, inferredPath);
        }

        return new NewList(Compiler, newList.Location, expressions);
    }

    private DMExpression BuildAddText(DMASTAddText addText, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        if (addText.Parameters.Length < 2)
            return BadExpression(WarningCode.InvalidArgumentCount, addText.Location, "Invalid addtext() parameter count; expected 2 or more arguments");

        DMExpression[] expArr = new DMExpression[addText.Parameters.Length];
        for (int i = 0; i < expArr.Length; i++) {
            DMASTCallParameter parameter = addText.Parameters[i];
            if(parameter.Key != null)
                 Compiler.Emit(WarningCode.InvalidArgumentKey, parameter.Location, "addtext() does not take named arguments");

            expArr[i] = BuildExpression(parameter.Value, dmObject, proc, inferredPath);
        }

        return new AddText(Compiler, addText.Location, expArr);
    }

    private DMExpression BuildInput(DMASTInput input, DMObject dmObject, DMProc proc) {
        DMExpression[] arguments = new DMExpression[input.Parameters.Length];
        for (int i = 0; i < input.Parameters.Length; i++) {
            DMASTCallParameter parameter = input.Parameters[i];

            if (parameter.Key != null) {
                 Compiler.Emit(WarningCode.InvalidArgumentKey, parameter.Location,
                    "input() does not take named arguments");
            }

            arguments[i] = BuildExpression(parameter.Value, dmObject, proc);
        }

        DMExpression? list = null;
        if (input.List != null) {
            list = BuildExpression(input.List, dmObject, proc);

            DMValueType objectTypes = DMValueType.Null |DMValueType.Obj | DMValueType.Mob | DMValueType.Turf |
                                      DMValueType.Area;

            // Default filter is "as anything" when there's a list
            input.Types ??= DMValueType.Anything;
            if (input.Types != DMValueType.Anything && (input.Types & objectTypes) == 0x0) {
                 Compiler.Emit(WarningCode.BadArgument, input.Location,
                    $"Invalid input() filter \"{input.Types}\". Filter must be \"{DMValueType.Anything}\" or at least one of \"{objectTypes}\"");
            }
        } else {
            // Default filter is "as text" when there's no list
            input.Types ??= DMValueType.Text;
        }

        if (arguments.Length is 0 or > 4)
            return BadExpression(WarningCode.InvalidArgumentCount, input.Location, "input() must have 1 to 4 arguments");

        return new Input(Compiler, input.Location, arguments, input.Types.Value, list);
    }

    private DMExpression BuildPick(DMASTPick pick, DMObject dmObject, DMProc proc) {
        Pick.PickValue[] pickValues = new Pick.PickValue[pick.Values.Length];

        for (int i = 0; i < pickValues.Length; i++) {
            DMASTPick.PickValue pickValue = pick.Values[i];
            DMExpression? weight = (pickValue.Weight != null) ? BuildExpression(pickValue.Weight, dmObject, proc) : null;
            DMExpression value = BuildExpression(pickValue.Value, dmObject, proc);

            if (weight is Prob prob) // pick(prob(50);x, prob(200);y) format
                weight = prob.P;

            pickValues[i] = new Pick.PickValue(weight, value);
        }

        return new Pick(Compiler, pick.Location, pickValues);
    }

    private DMExpression BuildLog(DMASTLog log, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var expr = BuildExpression(log.Expression, dmObject, proc, inferredPath);
        DMExpression? baseExpr = null;

        if (log.BaseExpression != null) {
            baseExpr = BuildExpression(log.BaseExpression, dmObject, proc, inferredPath);
        }

        return new Log(Compiler, log.Location, expr, baseExpr);
    }

    private DMExpression BuildCall(DMASTCall call, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var procArgs = BuildArgumentList(call.Location, dmObject, proc, call.ProcParameters, inferredPath);

        switch (call.CallParameters.Length) {
            default:
                 Compiler.Emit(WarningCode.InvalidArgumentCount, call.Location, "Too many arguments for call()");
                goto case 2; // Fallthrough!
            case 2: {
                var a = BuildExpression(call.CallParameters[0].Value, dmObject, proc, inferredPath);
                var b = BuildExpression(call.CallParameters[1].Value, dmObject, proc, inferredPath);
                return new CallStatement(Compiler, call.Location, a, b, procArgs);
            }
            case 1: {
                var a = BuildExpression(call.CallParameters[0].Value, dmObject, proc, inferredPath);
                return new CallStatement(Compiler, call.Location, a, procArgs);
            }
            case 0:
                 Compiler.Emit(WarningCode.InvalidArgumentCount, call.Location, "Not enough arguments for call()");
                return new CallStatement(Compiler, call.Location, new Null(compiler, Location.Internal), procArgs);
        }
    }

    /// <summary>
    /// Emits an error and returns a <see cref="BadExpression"/><br/>
    /// </summary>
    private BadExpression BadExpression(WarningCode code, Location location, string errorMessage) {
        if (EncounteredUnknownReference == null)
            Compiler.Emit(code, location, errorMessage);
        return new BadExpression(Compiler, location);
    }

    /// <summary>
    /// Creates an UnknownReference expression that should be returned at the end of the expression building.<br/>
    /// Always use this to return an UnknownReference!
    /// </summary>
    private UnknownReference UnknownReference(Location location, string errorMessage) {
        EncounteredUnknownReference = new UnknownReference(Compiler, location, errorMessage);
        return EncounteredUnknownReference;
    }

    /// <summary>
    /// <see cref="UnknownReference"/> but with a common message
    /// </summary>
    private UnknownReference UnknownIdentifier(Location location, string identifier) =>
        UnknownReference(location, $"Unknown identifier \"{identifier}\"");
}
