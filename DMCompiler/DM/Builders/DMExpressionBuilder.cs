using DMCompiler.Compiler;
using Resource = DMCompiler.DM.Expressions.Resource;
using DMCompiler.Compiler.DM.AST;
using DMCompiler.DM.Expressions;
using static DMCompiler.DM.Builders.DMExpressionBuilder.ScopeMode;
using String = DMCompiler.DM.Expressions.String;

namespace DMCompiler.DM.Builders;

internal class DMExpressionBuilder(ExpressionContext ctx, DMExpressionBuilder.ScopeMode scopeMode = Normal) {
    public enum ScopeMode {
        /// All in-scope procs and vars available
        Normal,

        /// Only global vars and procs available
        Static,

        /// Only global procs available
        FirstPassStatic
    }

    // TODO: Remove this terrible global flag
    public static bool ScopeOperatorEnabled = false; // Enabled on the last pass of the code tree

    private UnknownReference? _encounteredUnknownReference;

    private DMCompiler Compiler => ctx.Compiler;
    private DMObjectTree ObjectTree => ctx.ObjectTree;

    // TODO: proc and dmObject can be null, address nullability contract
    public DMExpression Create(DMASTExpression expression, DreamPath? inferredPath = null) {
        var expr = CreateIgnoreUnknownReference(expression, inferredPath);
        if (expr is UnknownReference unknownRef)
            unknownRef.EmitCompilerError(Compiler);

        return expr;
    }

    public DMExpression CreateIgnoreUnknownReference(DMASTExpression expression, DreamPath? inferredPath = null) {
        _encounteredUnknownReference = null;
        return BuildExpression(expression, inferredPath);
    }

    public void Emit(DMASTExpression expression, DreamPath? inferredPath = null) {
        var expr = Create(expression, inferredPath);
        expr.EmitPushValue(ctx);
    }

    public bool TryConstant(DMASTExpression expression, out Constant? constant) {
        var expr = Create(expression);
        return expr.TryAsConstant(Compiler, out constant);
    }

    /// <remarks>Don't use Create() inside this or anything it calls! It resets _encounteredUnknownReference</remarks>
    private DMExpression BuildExpression(DMASTExpression expression, DreamPath? inferredPath = null) {
        DMExpression result;

        switch (expression) {
            case DMASTInvalidExpression:
                // No error emission here because the parser should have emitted an error when making this
                return new BadExpression(expression.Location);

            case DMASTExpressionConstant constant: result = BuildConstant(constant); break;
            case DMASTStringFormat stringFormat: result = BuildStringFormat(stringFormat, inferredPath); break;
            case DMASTIdentifier identifier: result = BuildIdentifier(identifier, inferredPath); break;
            case DMASTScopeIdentifier globalIdentifier: result = BuildScopeIdentifier(globalIdentifier, inferredPath); break;
            case DMASTCallableSelf: result = new ProcSelf(expression.Location, ctx.Proc.ReturnTypes); break;
            case DMASTCallableSuper: result = new ProcSuper(expression.Location, ctx.Type.GetProcReturnTypes(ctx.Proc.Name)); break;
            case DMASTCallableProcIdentifier procIdentifier: result = BuildCallableProcIdentifier(procIdentifier, ctx.Type); break;
            case DMASTProcCall procCall: result = BuildProcCall(procCall, inferredPath); break;
            case DMASTAssign assign: result = BuildAssign(assign, inferredPath); break;
            case DMASTAssignInto assignInto: result = BuildAssignInto(assignInto, inferredPath); break;
            case DMASTEqual equal: result = BuildEqual(equal, inferredPath); break;
            case DMASTNotEqual notEqual: result = BuildNotEqual(notEqual, inferredPath); break;
            case DMASTDereference deref: result = BuildDereference(deref, inferredPath); break;
            case DMASTLocate locate: result = BuildLocate(locate, inferredPath); break;
            case DMASTImplicitAsType implicitAsType: result = BuildImplicitAsType(implicitAsType, inferredPath); break;
            case DMASTImplicitIsType implicitIsType: result = BuildImplicitIsType(implicitIsType, inferredPath); break;
            case DMASTList list: result = BuildList(list, inferredPath); break;
            case DMASTDimensionalList dimensionalList: result = BuildDimensionalList(dimensionalList, inferredPath); break;
            case DMASTNewList newList: result = BuildNewList(newList, inferredPath); break;
            case DMASTAddText addText: result = BuildAddText(addText, inferredPath); break;
            case DMASTInput input: result = BuildInput(input, inferredPath); break;
            case DMASTPick pick: result = BuildPick(pick, inferredPath); break;
            case DMASTLog log: result = BuildLog(log, inferredPath); break;
            case DMASTCall call: result = BuildCall(call, inferredPath); break;
            case DMASTExpressionWrapped wrapped: result = BuildExpression(wrapped.Value, inferredPath); break;

            case DMASTNegate negate:
                result = new Negate(negate.Location, BuildExpression(negate.Value, inferredPath));
                break;
            case DMASTNot not:
                result = new Not(not.Location, BuildExpression(not.Value, inferredPath));
                break;
            case DMASTBinaryNot binaryNot:
                result = new BinaryNot(binaryNot.Location, BuildExpression(binaryNot.Value, inferredPath));
                break;
            case DMASTAdd add:
                result = new Add(add.Location,
                    BuildExpression(add.LHS, inferredPath),
                    BuildExpression(add.RHS, inferredPath));
                break;
            case DMASTSubtract subtract:
                result = new Subtract(subtract.Location,
                    BuildExpression(subtract.LHS, inferredPath),
                    BuildExpression(subtract.RHS, inferredPath));
                break;
            case DMASTMultiply multiply:
                result = new Multiply(multiply.Location,
                    BuildExpression(multiply.LHS, inferredPath),
                    BuildExpression(multiply.RHS, inferredPath));
                break;
            case DMASTDivide divide:
                result = new Divide(divide.Location,
                    BuildExpression(divide.LHS, inferredPath),
                    BuildExpression(divide.RHS, inferredPath));
                break;
            case DMASTModulus modulus:
                result = new Modulo(modulus.Location,
                    BuildExpression(modulus.LHS, inferredPath),
                    BuildExpression(modulus.RHS, inferredPath));
                break;
            case DMASTModulusModulus modulusModulus:
                result = new ModuloModulo(modulusModulus.Location,
                    BuildExpression(modulusModulus.LHS, inferredPath),
                    BuildExpression(modulusModulus.RHS, inferredPath));
                break;
            case DMASTPower power:
                result = new Power(power.Location,
                    BuildExpression(power.LHS, inferredPath),
                    BuildExpression(power.RHS, inferredPath));
                break;
            case DMASTAppend append:
                result = new Append(append.Location,
                    BuildExpression(append.LHS, inferredPath),
                    BuildExpression(append.RHS, inferredPath));
                break;
            case DMASTCombine combine:
                result = new Combine(combine.Location,
                    BuildExpression(combine.LHS, inferredPath),
                    BuildExpression(combine.RHS, inferredPath));
                break;
            case DMASTRemove remove:
                result = new Remove(remove.Location,
                    BuildExpression(remove.LHS, inferredPath),
                    BuildExpression(remove.RHS, inferredPath));
                break;
            case DMASTMask mask:
                result = new Mask(mask.Location,
                    BuildExpression(mask.LHS, inferredPath),
                    BuildExpression(mask.RHS, inferredPath));
                break;
            case DMASTLogicalAndAssign lAnd:
                var lAndLHS = BuildExpression(lAnd.LHS, inferredPath);
                var lAndRHS = BuildExpression(lAnd.RHS, lAndLHS.NestedPath);

                result = new LogicalAndAssign(lAnd.Location,
                    lAndLHS,
                    lAndRHS);
                break;
            case DMASTLogicalOrAssign lOr:
                var lOrLHS = BuildExpression(lOr.LHS, inferredPath);
                var lOrRHS = BuildExpression(lOr.RHS, lOrLHS.NestedPath);

                result = new LogicalOrAssign(lOr.Location, lOrLHS, lOrRHS);
                break;
            case DMASTMultiplyAssign multiplyAssign:
                result = new MultiplyAssign(multiplyAssign.Location,
                    BuildExpression(multiplyAssign.LHS, inferredPath),
                    BuildExpression(multiplyAssign.RHS, inferredPath));
                break;
            case DMASTDivideAssign divideAssign:
                result = new DivideAssign(divideAssign.Location,
                    BuildExpression(divideAssign.LHS, inferredPath),
                    BuildExpression(divideAssign.RHS, inferredPath));
                break;
            case DMASTLeftShiftAssign leftShiftAssign:
                result = new LeftShiftAssign(leftShiftAssign.Location,
                    BuildExpression(leftShiftAssign.LHS, inferredPath),
                    BuildExpression(leftShiftAssign.RHS, inferredPath));
                break;
            case DMASTRightShiftAssign rightShiftAssign:
                result = new RightShiftAssign(rightShiftAssign.Location,
                    BuildExpression(rightShiftAssign.LHS, inferredPath),
                    BuildExpression(rightShiftAssign.RHS, inferredPath));
                break;
            case DMASTXorAssign xorAssign:
                result = new XorAssign(xorAssign.Location,
                    BuildExpression(xorAssign.LHS, inferredPath),
                    BuildExpression(xorAssign.RHS, inferredPath));
                break;
            case DMASTModulusAssign modulusAssign:
                result = new ModulusAssign(modulusAssign.Location,
                    BuildExpression(modulusAssign.LHS, inferredPath),
                    BuildExpression(modulusAssign.RHS, inferredPath));
                break;
            case DMASTModulusModulusAssign modulusModulusAssign:
                var mmAssignLHS = BuildExpression(modulusModulusAssign.LHS, inferredPath);
                var mmAssignRHS = BuildExpression(modulusModulusAssign.RHS, mmAssignLHS.NestedPath);

                result = new ModulusModulusAssign(modulusModulusAssign.Location, mmAssignLHS, mmAssignRHS);
                break;
            case DMASTLeftShift leftShift:
                result = new LeftShift(leftShift.Location,
                    BuildExpression(leftShift.LHS, inferredPath),
                    BuildExpression(leftShift.RHS, inferredPath));
                break;
            case DMASTRightShift rightShift:
                result = new RightShift(rightShift.Location,
                    BuildExpression(rightShift.LHS, inferredPath),
                    BuildExpression(rightShift.RHS, inferredPath));
                break;
            case DMASTBinaryAnd binaryAnd:
                result = new BinaryAnd(binaryAnd.Location,
                    BuildExpression(binaryAnd.LHS, inferredPath),
                    BuildExpression(binaryAnd.RHS, inferredPath));
                break;
            case DMASTBinaryXor binaryXor:
                result = new BinaryXor(binaryXor.Location,
                    BuildExpression(binaryXor.LHS, inferredPath),
                    BuildExpression(binaryXor.RHS, inferredPath));
                break;
            case DMASTBinaryOr binaryOr:
                result = new BinaryOr(binaryOr.Location,
                    BuildExpression(binaryOr.LHS, inferredPath),
                    BuildExpression(binaryOr.RHS, inferredPath));
                break;
            case DMASTEquivalent equivalent:
                result = new Equivalent(equivalent.Location,
                    BuildExpression(equivalent.LHS, inferredPath),
                    BuildExpression(equivalent.RHS, inferredPath));
                break;
            case DMASTNotEquivalent notEquivalent:
                result = new NotEquivalent(notEquivalent.Location,
                    BuildExpression(notEquivalent.LHS, inferredPath),
                    BuildExpression(notEquivalent.RHS, inferredPath));
                break;
            case DMASTGreaterThan greaterThan:
                result = new GreaterThan(greaterThan.Location,
                    BuildExpression(greaterThan.LHS, inferredPath),
                    BuildExpression(greaterThan.RHS, inferredPath));
                break;
            case DMASTGreaterThanOrEqual greaterThanOrEqual:
                result = new GreaterThanOrEqual(greaterThanOrEqual.Location,
                    BuildExpression(greaterThanOrEqual.LHS, inferredPath),
                    BuildExpression(greaterThanOrEqual.RHS, inferredPath));
                break;
            case DMASTLessThan lessThan:
                result = new LessThan(lessThan.Location,
                    BuildExpression(lessThan.LHS, inferredPath),
                    BuildExpression(lessThan.RHS, inferredPath));
                break;
            case DMASTLessThanOrEqual lessThanOrEqual:
                result = new LessThanOrEqual(lessThanOrEqual.Location,
                    BuildExpression(lessThanOrEqual.LHS, inferredPath),
                    BuildExpression(lessThanOrEqual.RHS, inferredPath));
                break;
            case DMASTOr or:
                result = new Or(or.Location,
                    BuildExpression(or.LHS, inferredPath),
                    BuildExpression(or.RHS, inferredPath));
                break;
            case DMASTAnd and:
                result = new And(and.Location,
                    BuildExpression(and.LHS, inferredPath),
                    BuildExpression(and.RHS, inferredPath));
                break;
            case DMASTTernary ternary:
                var a = BuildExpression(ternary.A, inferredPath);
                var b = BuildExpression(ternary.B, inferredPath);
                var c = BuildExpression(ternary.C ?? new DMASTConstantNull(ternary.Location), inferredPath);

                if (b.ValType.TypePath != null && c.ValType.TypePath != null && b.ValType.TypePath != c.ValType.TypePath) {
                    Compiler.Emit(WarningCode.LostTypeInfo, ternary.Location,
                        $"Ternary has type paths {b.ValType.TypePath} and {c.ValType.TypePath} but a value can only have one type path. Using {b.ValType.TypePath}.");
                }

                result = new Ternary(ternary.Location, a, b, c);
                break;
            case DMASTNewPath newPath:
                if (BuildExpression(newPath.Path, inferredPath) is not IConstantPath path) {
                    result = BadExpression(WarningCode.BadExpression, newPath.Path.Location,
                        "Expected a path expression");
                    break;
                }

                result = new NewPath(Compiler, newPath.Location, path,
                    BuildArgumentList(newPath.Location, newPath.Parameters, inferredPath));
                break;
            case DMASTNewExpr newExpr:
                result = new New(Compiler, newExpr.Location,
                    BuildExpression(newExpr.Expression, inferredPath),
                    BuildArgumentList(newExpr.Location, newExpr.Parameters, inferredPath));
                break;
            case DMASTNewInferred newInferred:
                if (inferredPath is null) {
                    result = BadExpression(WarningCode.BadExpression, newInferred.Location, "Could not infer a type");
                    break;
                }

                var type = BuildPath(newInferred.Location, inferredPath.Value);
                if (type is not IConstantPath inferredType) {
                    result = BadExpression(WarningCode.BadExpression, newInferred.Location,
                        $"Cannot instantiate {type}");
                    break;
                }

                result = new NewPath(Compiler, newInferred.Location, inferredType,
                    BuildArgumentList(newInferred.Location, newInferred.Parameters, inferredPath));
                break;
            case DMASTPreIncrement preIncrement:
                result = new PreIncrement(preIncrement.Location, BuildExpression(preIncrement.Value, inferredPath));
                break;
            case DMASTPostIncrement postIncrement:
                result = new PostIncrement(postIncrement.Location, BuildExpression(postIncrement.Value, inferredPath));
                break;
            case DMASTPreDecrement preDecrement:
                result = new PreDecrement(preDecrement.Location, BuildExpression(preDecrement.Value, inferredPath));
                break;
            case DMASTPostDecrement postDecrement:
                result = new PostDecrement(postDecrement.Location, BuildExpression(postDecrement.Value, inferredPath));
                break;
            case DMASTPointerRef pointerRef:
                result = new PointerRef(pointerRef.Location, BuildExpression(pointerRef.Value, inferredPath));
                break;
            case DMASTPointerDeref pointerDeref:
                result = new PointerDeref(pointerDeref.Location, BuildExpression(pointerDeref.Value, inferredPath));
                break;
            case DMASTGradient gradient:
                result = new Gradient(gradient.Location,
                    BuildArgumentList(gradient.Location, gradient.Parameters, inferredPath));
                break;
            case DMASTRgb rgb:
                result = new Rgb(rgb.Location, BuildArgumentList(rgb.Location, rgb.Parameters, inferredPath));
                break;
            case DMASTLocateCoordinates locateCoordinates:
                result = new LocateCoordinates(locateCoordinates.Location,
                    BuildExpression(locateCoordinates.X, inferredPath),
                    BuildExpression(locateCoordinates.Y, inferredPath),
                    BuildExpression(locateCoordinates.Z, inferredPath));
                break;
            case DMASTAsType asType: {
                var lhs = BuildExpression(asType.LHS, inferredPath);
                var rhs = BuildExpression(asType.RHS, lhs.Path);

                result = new AsType(asType.Location, lhs, rhs);
                break;
            }
            case DMASTIsSaved isSaved:
                result = new IsSaved(isSaved.Location, BuildExpression(isSaved.Value, inferredPath));
                break;
            case DMASTIsType isType: {
                var lhs = BuildExpression(isType.LHS, inferredPath);
                var rhs = BuildExpression(isType.RHS, lhs.Path);

                result = new IsType(isType.Location, lhs, rhs);
                break;
            }

            case DMASTIsNull isNull:
                result = new IsNull(isNull.Location, BuildExpression(isNull.Value, inferredPath));
                break;
            case DMASTLength length:
                result = new Length(length.Location, BuildExpression(length.Value, inferredPath));
                break;
            case DMASTGetStep getStep:
                result = new GetStep(getStep.Location,
                    BuildExpression(getStep.LHS, inferredPath),
                    BuildExpression(getStep.RHS, inferredPath));
                break;
            case DMASTGetDir getDir:
                result = new GetDir(getDir.Location,
                    BuildExpression(getDir.LHS, inferredPath),
                    BuildExpression(getDir.RHS, inferredPath));
                break;
            case DMASTProb prob:
                result = new Prob(prob.Location,
                    BuildExpression(prob.Value, inferredPath));
                break;
            case DMASTInitial initial:
                result = new Initial(initial.Location, BuildExpression(initial.Value, inferredPath));
                break;
            case DMASTNameof nameof:
                result = BuildNameof(nameof, inferredPath);
                break;
            case DMASTExpressionIn expressionIn:
                var exprInLHS = BuildExpression(expressionIn.LHS, inferredPath);
                var exprInRHS = BuildExpression(expressionIn.RHS, inferredPath);
                if ((expressionIn.LHS is not DMASTExpressionWrapped && exprInLHS is UnaryOp or BinaryOp or Ternary) ||
                    (expressionIn.RHS is not DMASTExpressionWrapped && exprInRHS is BinaryOp or Ternary)) {
                    Compiler.Emit(WarningCode.AmbiguousInOrder, expressionIn.Location,
                        "Order of operations for \"in\" may not be what is expected. Use parentheses to be more explicit.");
                }

                result = new In(expressionIn.Location, exprInLHS, exprInRHS);
                break;
            case DMASTExpressionInRange expressionInRange:
                result = new InRange(expressionInRange.Location,
                    BuildExpression(expressionInRange.Value, inferredPath),
                    BuildExpression(expressionInRange.StartRange, inferredPath),
                    BuildExpression(expressionInRange.EndRange, inferredPath));
                break;
            case DMASTSin sin:
                result = new Sin(sin.Location, BuildExpression(sin.Value, inferredPath));
                break;
            case DMASTCos cos:
                result = new Cos(cos.Location, BuildExpression(cos.Value, inferredPath));
                break;
            case DMASTTan tan:
                result = new Tan(tan.Location, BuildExpression(tan.Value, inferredPath));
                break;
            case DMASTArcsin arcSin:
                result = new ArcSin(arcSin.Location, BuildExpression(arcSin.Value, inferredPath));
                break;
            case DMASTArccos arcCos:
                result = new ArcCos(arcCos.Location, BuildExpression(arcCos.Value, inferredPath));
                break;
            case DMASTArctan arcTan:
                result = new ArcTan(arcTan.Location, BuildExpression(arcTan.Value, inferredPath));
                break;
            case DMASTArctan2 arcTan2:
                result = new ArcTan2(arcTan2.Location,
                    BuildExpression(arcTan2.LHS, inferredPath),
                    BuildExpression(arcTan2.RHS, inferredPath));
                break;
            case DMASTSqrt sqrt:
                result = new Sqrt(sqrt.Location, BuildExpression(sqrt.Value, inferredPath));
                break;
            case DMASTAbs abs:
                result = new Abs(abs.Location, BuildExpression(abs.Value, inferredPath));
                break;
            case DMASTVarDeclExpression varDeclExpr:
                var declIdentifier = new DMASTIdentifier(expression.Location, varDeclExpr.DeclPath.Path.LastElement);

                result = BuildIdentifier(declIdentifier, inferredPath);
                break;
            case DMASTVoid:
                result = BadExpression(WarningCode.BadExpression, expression.Location, "Attempt to use a void expression");
                break;
            default:
                throw new ArgumentException($"Invalid expression {expression}", nameof(expression));
        }

        if (_encounteredUnknownReference != null) {
            return _encounteredUnknownReference;
        } else {
            return result;
        }
    }

    private DMExpression BuildConstant(DMASTExpressionConstant constant) {
        switch (constant) {
            case DMASTConstantNull: return new Null(constant.Location);
            case DMASTConstantInteger constInt: return new Number(constant.Location, constInt.Value);
            case DMASTConstantFloat constFloat: return new Number(constant.Location, constFloat.Value);
            case DMASTConstantString constString: return new String(constant.Location, constString.Value);
            case DMASTConstantResource constResource: return new Resource(Compiler, constant.Location, constResource.Path);
            case DMASTConstantPath constPath: return BuildPath(constant.Location, constPath.Value.Path);
            case DMASTUpwardPathSearch upwardSearch:
                BuildExpression(upwardSearch.Path).TryAsConstant(Compiler, out var pathExpr);
                if (pathExpr is not IConstantPath expr)
                    return BadExpression(WarningCode.BadExpression, constant.Location,
                        $"Cannot do an upward path search on {pathExpr}");

                var path = expr.Path;
                if (path == null)
                    return UnknownReference(constant.Location,
                        $"Cannot search on {expr}");

                DreamPath? foundPath = ObjectTree.UpwardSearch(path.Value, upwardSearch.Search.Path);
                if (foundPath == null)
                    return UnknownReference(constant.Location,
                        $"Could not find path {path}.{upwardSearch.Search.Path}");

                return BuildPath(constant.Location, foundPath.Value);
        }

        throw new ArgumentException($"Invalid constant {constant}", nameof(constant));
    }

    private StringFormat BuildStringFormat(DMASTStringFormat stringFormat, DreamPath? inferredPath) {
        var expressions = new DMExpression[stringFormat.InterpolatedValues.Length];

        for (int i = 0; i < stringFormat.InterpolatedValues.Length; i++) {
            var interpolatedValue = stringFormat.InterpolatedValues[i];

            if (interpolatedValue == null) {
                expressions[i] = new Null(stringFormat.Location);
            } else {
                expressions[i] = BuildExpression(interpolatedValue, inferredPath);
            }
        }

        return new StringFormat(stringFormat.Location, stringFormat.Value, expressions);
    }

    private DMExpression BuildPath(Location location, DreamPath path) {
        // An upward search with no left-hand side
        if (path.Type == DreamPath.PathType.UpwardSearch) {
            DreamPath? foundPath = Compiler.DMCodeTree.UpwardSearch(ctx.Type, path);
            if (foundPath == null)
                return UnknownReference(location, $"Could not find path {path}");

            path = foundPath.Value;
        }

        // /datum/proc or /datum/verb
        if (path.LastElement is "proc" or "verb") {
            DreamPath typePath = path.FromElements(0, -2);
            if (!ObjectTree.TryGetDMObject(typePath, out var stubOfType))
                return UnknownReference(location, $"Type {typePath} does not exist");

            return new ConstantProcStub(location, stubOfType, path.LastElement is "verb");
        }

        // /datum
        if (ObjectTree.TryGetDMObject(path, out var referencing)) {
            return new ConstantTypeReference(location, referencing);
        }

        // /datum/proc/foo
        int procIndex = path.FindElement("proc");
        if (procIndex == -1) procIndex = path.FindElement("verb");
        if (procIndex != -1) {
            DreamPath withoutProcElement = path.RemoveElement(procIndex);
            DreamPath ownerPath = withoutProcElement.FromElements(0, -2);
            string procName = path.LastElement!;

            if (!ObjectTree.TryGetDMObject(ownerPath, out var owner))
                return UnknownReference(location, $"Type {ownerPath} does not exist");

            int? procId;
            if (owner == ObjectTree.Root && ObjectTree.TryGetGlobalProc(procName, out var globalProc)) {
                procId = globalProc.Id;
            } else {
                var procs = owner.GetProcs(procName);

                procId = procs?[^1];
            }

            if (procId == null || ObjectTree.AllProcs.Count < procId) {
                return UnknownReference(location, $"Could not find proc {procName}() on {ownerPath}");
            }

            return new ConstantProcReference(location, path, ObjectTree.AllProcs[procId.Value]);
        }

        return UnknownReference(location, $"Path {path} does not exist");
    }

    private DMExpression BuildIdentifier(DMASTIdentifier identifier, DreamPath? inferredPath = null) {
        var name = identifier.Identifier;

        switch (name) {
            case "src":
                return new Src(identifier.Location, ctx.Type.Path);
            case "usr":
                return new Usr(identifier.Location);
            case "args":
                return new Args(identifier.Location);
            case "world":
                if (scopeMode == FirstPassStatic) // world is not available on the first pass
                    return UnknownIdentifier(identifier.Location, "world");

                return new World(identifier.Location);
            case "__TYPE__":
                return new ProcOwnerType(identifier.Location, ctx.Type);
            case "__IMPLIED_TYPE__":
                if (inferredPath == null)
                    return BadExpression(WarningCode.BadExpression, identifier.Location,
                        "__IMPLIED_TYPE__ cannot be used here, there is no type being implied");

                return BuildPath(identifier.Location, inferredPath.Value);
            case "__PROC__": // The saner alternative to "....."
                var path = ctx.Type.Path.AddToPath("proc/" + ctx.Proc.Name);

                return new ConstantProcReference(identifier.Location, path, ctx.Proc);
            case "global":
                return new Global(identifier.Location);
            default: {
                if (scopeMode == Normal) {
                    var localVar = ctx.Proc?.GetLocalVariable(name);
                    if (localVar != null)
                        return new Local(identifier.Location, localVar, localVar.ExplicitValueType);
                }

                var field = ctx.Type.GetVariable(name);
                if (field != null && (scopeMode == Normal || field.IsConst)) {
                    return new Field(identifier.Location, field, field.ValType);
                }

                var globalId = ctx.Proc?.GetGlobalVariableId(name) ?? ctx.Type.GetGlobalVariableId(name);

                if (globalId != null) {
                    if (field is not null)
                        Compiler.Emit(WarningCode.AmbiguousVarStatic, identifier.Location, $"Static var definition cannot reference instance variable \"{name}\" but a global exists");

                    var globalVar = ObjectTree.Globals[globalId.Value];
                    var global = new GlobalField(identifier.Location, globalVar.Type, globalId.Value, globalVar.ValType);
                    return global;
                }

                return UnknownIdentifier(identifier.Location, name);
            }
        }
    }

    private DMExpression BuildScopeIdentifier(DMASTScopeIdentifier scopeIdentifier, DreamPath? inferredPath) {
        var location = scopeIdentifier.Location;
        var bIdentifier = scopeIdentifier.Identifier;

        if (scopeIdentifier.Expression == null) { // ::A, shorthand for global.A
            if (scopeIdentifier.IsProcRef) { // ::A(), global proc ref
                if (!ObjectTree.TryGetGlobalProc(bIdentifier, out var globalProc))
                    return UnknownReference(location, $"No global proc named \"{bIdentifier}\" exists");

                var arguments = BuildArgumentList(location, scopeIdentifier.CallArguments, inferredPath);
                return new ProcCall(location, new GlobalProc(location, globalProc), arguments, DMValueType.Anything);
            }

            // ::vars, special case
            if (bIdentifier == "vars")
                return new GlobalVars(location);

            // ::A, global var ref
            var globalId = ObjectTree.Root.GetGlobalVariableId(bIdentifier);
            if (globalId == null)
                return UnknownIdentifier(location, bIdentifier);

            var globalVar = ObjectTree.Globals [globalId.Value];
            return new GlobalField(location,
                ObjectTree.Globals[globalId.Value].Type,
                globalId.Value,
                globalVar.ValType);
        }

        // Other uses should wait until the scope operator pass
        if (!ScopeOperatorEnabled)
            return UnknownIdentifier(location, bIdentifier);

        DMExpression? expression;

        // "type" and "parent_type" cannot resolve in a static context, but it's still valid with scope identifiers
        if (scopeIdentifier.Expression is DMASTIdentifier { Identifier: "type" or "parent_type" } identifier) {
            // This is the same behaviour as in BYOND, but BYOND simply raises an undefined var error.
            // We want to give end users an explanation at least.
            if (scopeMode is Normal && ctx.Proc != null)
                return BadExpression(WarningCode.BadExpression, identifier.Location,
                    "Use of \"type::\" and \"parent_type::\" outside of a context is forbidden");

            if (identifier.Identifier == "parent_type") {
                if (ctx.Type.Parent == null)
                    return BadExpression(WarningCode.ItemDoesntExist, identifier.Location,
                        $"Type {ctx.Type.Path} does not have a parent");

                expression = BuildPath(location, ctx.Type.Parent.Path);
            } else { // "type"
                expression = BuildPath(location, ctx.Type.Path);
            }
        } else {
            expression = BuildExpression(scopeIdentifier.Expression, inferredPath);
        }

        // A needs to have a type
        if (expression.Path == null)
            return BadExpression(WarningCode.BadExpression, expression.Location,
                $"Identifier \"{expression.GetNameof(ctx)}\" does not have a type");

        if (!ObjectTree.TryGetDMObject(expression.Path.Value, out var owner)) {
            if (expression is ConstantProcReference procReference) {
                if (bIdentifier == "name")
                    return new String(expression.Location, procReference.Value.Name);

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

            var referencedProc = ObjectTree.AllProcs[procs[^1]];
            var path = owner.Path.AddToPath("proc/" + referencedProc.Name);
            return new ConstantProcReference(location, path, referencedProc);
        } else { // A::B
            var globalVarId = owner.GetGlobalVariableId(bIdentifier);
            if (globalVarId != null) {
                // B is a static var.
                // This is the only case a ScopeIdentifier can be an LValue.
                var globalVar = ObjectTree.Globals [globalVarId.Value];
                return new GlobalField(location, globalVar.Type, globalVarId.Value, globalVar.ValType);
            }

            var variable = owner.GetVariable(bIdentifier);
            if (variable == null)
                return UnknownIdentifier(location, bIdentifier);

            return new ScopeReference(ObjectTree, location, expression, bIdentifier, variable);
        }
    }

    private DMExpression BuildCallableProcIdentifier(DMASTCallableProcIdentifier procIdentifier, DMObject dmObject) {
        if (scopeMode is Static or FirstPassStatic) {
            if (!ObjectTree.TryGetGlobalProc(procIdentifier.Identifier, out var staticScopeGlobalProc))
                return UnknownReference(procIdentifier.Location,
                    $"Type {dmObject.Path} does not have a proc named \"{procIdentifier.Identifier}\"");

            return new GlobalProc(procIdentifier.Location, staticScopeGlobalProc);
        }

        if (dmObject.HasProc(procIdentifier.Identifier)) {
            return new Proc(procIdentifier.Location, procIdentifier.Identifier);
        }

        if (ObjectTree.TryGetGlobalProc(procIdentifier.Identifier, out var globalProc)) {
            return new GlobalProc(procIdentifier.Location, globalProc);
        }

        return UnknownReference(procIdentifier.Location,
            $"Type {dmObject.Path} does not have a proc named \"{procIdentifier.Identifier}\"");
    }

    private DMExpression BuildProcCall(DMASTProcCall procCall, DreamPath? inferredPath) {
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

            var expr = BuildExpression(procCall.Parameters[0].Value, inferredPath);
            return new Arglist(procCall.Location, expr);
        }

        var target = BuildExpression((DMASTExpression)procCall.Callable, inferredPath);
        var args = BuildArgumentList(procCall.Location, procCall.Parameters, inferredPath);
        if (target is Proc targetProc) { // GlobalProc handles returnType itself
            var returnType = targetProc.GetReturnType(ctx.Type);

            return new ProcCall(procCall.Location, target, args, returnType);
        }

        return new ProcCall(procCall.Location, target, args, DMValueType.Anything);
    }

    private ArgumentList BuildArgumentList(Location location, DMASTCallParameter[]? arguments, DreamPath? inferredPath = null) {
        if (arguments == null || arguments.Length == 0)
            return new ArgumentList(location, [], false);

        var expressions = new (string?, DMExpression)[arguments.Length];
        bool isKeyed = false;

        int idx = 0;
        foreach(var arg in arguments) {
            var value = BuildExpression(arg.Value, inferredPath);
            var key = (arg.Key != null) ? BuildExpression(arg.Key, inferredPath) : null;
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

    private DMExpression BuildAssign(DMASTAssign assign, DreamPath? inferredPath) {
        var lhs = BuildExpression(assign.LHS, inferredPath);
        var rhs = BuildExpression(assign.RHS, lhs.NestedPath);
        if(lhs.TryAsConstant(Compiler, out _)) {
            Compiler.Emit(WarningCode.WriteToConstant, assign.LHS.Location, "Cannot write to const var");
        }

        return new Assignment(assign.Location, lhs, rhs);
    }

    private DMExpression BuildAssignInto(DMASTAssignInto assign, DreamPath? inferredPath) {
        var lhs = BuildExpression(assign.LHS, inferredPath);
        var rhs = BuildExpression(assign.RHS, lhs.NestedPath);
        if(lhs.TryAsConstant(Compiler, out _)) {
            Compiler.Emit(WarningCode.WriteToConstant, assign.LHS.Location, "Cannot write to const var");
        }

        return new AssignmentInto(assign.Location, lhs, rhs);
    }

    private DMExpression BuildEqual(DMASTEqual equal, DreamPath? inferredPath) {
        var lhs = BuildExpression(equal.LHS, inferredPath);
        var rhs = BuildExpression(equal.RHS, inferredPath);

        // (x == null) can be changed to isnull(x) which compiles down to an opcode
        // TODO: Bytecode optimizations instead
        if (rhs is Null)
            return new IsNull(equal.Location, lhs);

        return new Equal(equal.Location, lhs, rhs);
    }

    private DMExpression BuildNotEqual(DMASTNotEqual notEqual, DreamPath? inferredPath) {
        var lhs = BuildExpression(notEqual.LHS, inferredPath);
        var rhs = BuildExpression(notEqual.RHS, inferredPath);

        // (x != null) can be changed to !isnull(x) which compiles down to two opcodes
        // TODO: Bytecode optimizations instead
        if (rhs is Null)
            return new Not(notEqual.Location, new IsNull(notEqual.Location, lhs));

        return new NotEqual(notEqual.Location, lhs, rhs);
    }

    private DMExpression BuildDereference(DMASTDereference deref, DreamPath? inferredPath) {
        var astOperations = deref.Operations;

        // The base expression and list of operations to perform on it
        // These may be redefined if we encounter a global access mid-operation
        var expr = BuildExpression(deref.Expression, inferredPath);
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
                        if (!ObjectTree.TryGetGlobalProc(callOperation.Identifier, out var globalProc))
                            return UnknownReference(callOperation.Location,
                                $"Could not find a global proc named \"{callOperation.Identifier}\"");

                        var argumentList = BuildArgumentList(deref.Expression.Location, callOperation.Parameters, inferredPath);

                        var globalProcExpr = new GlobalProc(expr.Location, globalProc);
                        expr = new ProcCall(expr.Location, globalProcExpr, argumentList, DMValueType.Anything);
                        break;

                    case DMASTDereference.FieldOperation:
                        // global.vars
                        if (namedOperation is { Identifier: "vars" }) {
                            expr = new GlobalVars(expr.Location);
                            break;
                        }

                        // global.variable
                        var globalId = ctx.Type.GetGlobalVariableId(namedOperation.Identifier);
                        if (globalId == null)
                            return UnknownIdentifier(deref.Location, $"global.{namedOperation.Identifier}");

                        var property = ObjectTree.Globals [globalId.Value];
                        expr = new GlobalField(expr.Location, property.Type, globalId.Value, property.ValType);

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
                expr = new Null(firstOperation.Location);
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
                        if (!ObjectTree.TryGetDMObject(prevPath.Value, out var fromObject))
                            return UnknownReference(fieldOperation.Location,
                                $"Type {prevPath.Value} does not exist");

                        property = fromObject.GetVariable(field);
                        if (!fieldOperation.Safe && fromObject.IsSubtypeOf(DreamPath.Client)) {
                            Compiler.Emit(WarningCode.UnsafeClientAccess, deref.Location,
                                "Unsafe \"client\" access. Use the \"?.\" operator instead");
                        }

                        if (property == null && fromObject.GetGlobalVariableId(field) is { } globalId) {
                            property = ObjectTree.Globals[globalId];

                            expr = new GlobalField(expr.Location, property.Type, globalId, property.ValType);

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
                        } else if (property?.CanConstFold is true && property.Value.TryAsConstant(Compiler, out var derefConst)) {
                            expr = derefConst;

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
                        Index = BuildExpression(indexOperation.Index, inferredPath ?? prevPath),
                        Safe = indexOperation.Safe,
                        Path = prevPath
                    };
                    prevPath = null;
                    pathIsFuzzy = true;
                    break;

                case DMASTDereference.CallOperation callOperation: {
                    var field = callOperation.Identifier;
                    var argumentList = BuildArgumentList(deref.Expression.Location, callOperation.Parameters, inferredPath);
                    DreamPath? nextPath = null;

                    if (!callOperation.NoSearch && !pathIsFuzzy) {
                        if (prevPath == null) {
                            return UnknownIdentifier(deref.Location, field);
                        }

                        if (!ObjectTree.TryGetDMObject(prevPath.Value, out var fromObject))
                            return UnknownReference(callOperation.Location, $"Type {prevPath.Value} does not exist");
                        if (!fromObject.HasProc(field))
                            return UnknownIdentifier(callOperation.Location, field);

                        var returnTypes = fromObject.GetProcReturnTypes(field) ?? DMValueType.Anything;
                        nextPath = returnTypes.IsPath ? returnTypes.TypePath : returnTypes.AsPath();
                    }

                    operation = new Dereference.CallOperation {
                        Parameters = argumentList,
                        Safe = callOperation.Safe,
                        Identifier = field,
                        Path = prevPath
                    };
                    prevPath = nextPath;
                    if(prevPath is null)
                        pathIsFuzzy = true;
                    break;
                }

                default:
                    throw new InvalidOperationException("unhandled deref operation kind");
            }

            operations[i] = operation;
        }

        // The final value in prevPath is our expression's path!
        return new Dereference(ObjectTree, deref.Location, prevPath, expr, operations);
    }

    private DMExpression BuildLocate(DMASTLocate locate, DreamPath? inferredPath) {
        var container = locate.Container != null ? BuildExpression(locate.Container, inferredPath) : null;

        if (locate.Expression == null) {
            if (inferredPath == null)
                return BadExpression(WarningCode.BadExpression, locate.Location, "inferred locate requires a type");

            return new LocateInferred(Compiler, locate.Location, inferredPath.Value, container);
        }

        var pathExpr = BuildExpression(locate.Expression, inferredPath);
        return new Locate(Compiler, locate.Location, pathExpr, container);
    }

    private DMExpression BuildImplicitAsType(DMASTImplicitAsType asType, DreamPath? inferredPath) {
        var expr = BuildExpression(asType.Value, inferredPath);

        if (inferredPath is null) {
            return BadExpression(WarningCode.BadExpression, asType.Location, "Could not infer a type");
        }

        return new AsTypeInferred(asType.Location, expr, inferredPath.Value);
    }

    private DMExpression BuildImplicitIsType(DMASTImplicitIsType isType, DreamPath? inferredPath) {
        var expr = BuildExpression(isType.Value, inferredPath);

        if (expr.Path is null)
            return BadExpression(WarningCode.BadExpression, isType.Location, "An inferred istype requires a type!");

        return new IsTypeInferred(isType.Location, expr, expr.Path.Value);
    }

    private DMExpression BuildList(DMASTList list, DreamPath? inferredPath) {
        (DMExpression? Key, DMExpression Value)[] values = [];

        if (list.Values != null) {
            values = new (DMExpression?, DMExpression)[list.Values.Length];

            for (int i = 0; i < list.Values.Length; i++) {
                DMASTCallParameter value = list.Values[i];
                DMExpression? key = (value.Key != null) ? BuildExpression(value.Key, inferredPath) : null;
                DMExpression listValue = BuildExpression(value.Value, inferredPath);

                values[i] = (key, listValue);
            }
        }

        return new List(list.Location, values);
    }

    private DMExpression BuildDimensionalList(DMASTDimensionalList list, DreamPath? inferredPath) {
        var sizes = new DMExpression[list.Sizes.Count];
        for (int i = 0; i < sizes.Length; i++) {
            var sizeExpr = BuildExpression(list.Sizes[i], inferredPath);

            sizes[i] = sizeExpr;
        }

        return new DimensionalList(list.Location, sizes);
    }

    // nameof(x)
    private DMExpression BuildNameof(DMASTNameof nameof, DreamPath? inferredPath) {
        var expr = BuildExpression(nameof.Value, inferredPath);
        if (expr.GetNameof(ctx) is { } name) {
            return new String(nameof.Location, name);
        }

        return BadExpression(WarningCode.BadArgument, nameof.Location, "nameof() requires a var, proc reference, or type path");
    }

    private DMExpression BuildNewList(DMASTNewList newList, DreamPath? inferredPath) {
        DMExpression[] expressions = new DMExpression[newList.Parameters.Length];

        for (int i = 0; i < newList.Parameters.Length; i++) {
            DMASTCallParameter parameter = newList.Parameters[i];
            if (parameter.Key != null)
                return BadExpression(WarningCode.InvalidArgumentKey, parameter.Location,
                    "newlist() does not take named arguments");

            expressions[i] = BuildExpression(parameter.Value, inferredPath);
        }

        return new NewList(newList.Location, expressions);
    }

    private DMExpression BuildAddText(DMASTAddText addText, DreamPath? inferredPath) {
        if (addText.Parameters.Length < 2)
            return BadExpression(WarningCode.InvalidArgumentCount, addText.Location, "Invalid addtext() parameter count; expected 2 or more arguments");

        DMExpression[] expArr = new DMExpression[addText.Parameters.Length];
        for (int i = 0; i < expArr.Length; i++) {
            DMASTCallParameter parameter = addText.Parameters[i];
            if(parameter.Key != null)
                Compiler.Emit(WarningCode.InvalidArgumentKey, parameter.Location, "addtext() does not take named arguments");

            expArr[i] = BuildExpression(parameter.Value, inferredPath);
        }

        return new AddText(addText.Location, expArr);
    }

    private DMExpression BuildInput(DMASTInput input, DreamPath? inferredPath) {
        DMExpression[] arguments = new DMExpression[input.Parameters.Length];
        for (int i = 0; i < input.Parameters.Length; i++) {
            DMASTCallParameter parameter = input.Parameters[i];

            if (parameter.Key != null) {
                 Compiler.Emit(WarningCode.InvalidArgumentKey, parameter.Location,
                    "input() does not take named arguments");
            }

            arguments[i] = BuildExpression(parameter.Value, inferredPath);
        }

        DMExpression? list = null;
        if (input.List != null) {
            list = BuildExpression(input.List, inferredPath);

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

        return new Input(input.Location, arguments, input.Types.Value, list);
    }

    private DMExpression BuildPick(DMASTPick pick, DreamPath? inferredPath) {
        Pick.PickValue[] pickValues = new Pick.PickValue[pick.Values.Length];

        for (int i = 0; i < pickValues.Length; i++) {
            DMASTPick.PickValue pickValue = pick.Values[i];
            DMExpression? weight = (pickValue.Weight != null) ? BuildExpression(pickValue.Weight, inferredPath) : null;
            DMExpression value = BuildExpression(pickValue.Value, inferredPath);

            if (weight is Prob prob) // pick(prob(50);x, prob(200);y) format
                weight = prob.P;

            pickValues[i] = new Pick.PickValue(weight, value);
        }

        return new Pick(pick.Location, pickValues);
    }

    private DMExpression BuildLog(DMASTLog log, DreamPath? inferredPath) {
        var expr = BuildExpression(log.Expression, inferredPath);
        DMExpression? baseExpr = null;

        if (log.BaseExpression != null) {
            baseExpr = BuildExpression(log.BaseExpression, inferredPath);
        }

        return new Log(log.Location, expr, baseExpr);
    }

    private DMExpression BuildCall(DMASTCall call, DreamPath? inferredPath) {
        var procArgs = BuildArgumentList(call.Location, call.ProcParameters, inferredPath);

        switch (call.CallParameters.Length) {
            default:
                 Compiler.Emit(WarningCode.InvalidArgumentCount, call.Location, "Too many arguments for call()");
                goto case 2; // Fallthrough!
            case 2: {
                var a = BuildExpression(call.CallParameters[0].Value, inferredPath);
                var b = BuildExpression(call.CallParameters[1].Value, inferredPath);
                return new CallStatement(call.Location, a, b, procArgs);
            }
            case 1: {
                var a = BuildExpression(call.CallParameters[0].Value, inferredPath);
                return new CallStatement(call.Location, a, procArgs);
            }
            case 0:
                 Compiler.Emit(WarningCode.InvalidArgumentCount, call.Location, "Not enough arguments for call()");
                return new CallStatement(call.Location, new Null(Location.Internal), procArgs);
        }
    }

    /// <summary>
    /// Emits an error and returns a <see cref="BadExpression"/><br/>
    /// </summary>
    private BadExpression BadExpression(WarningCode code, Location location, string errorMessage) {
        if (_encounteredUnknownReference == null)
            Compiler.Emit(code, location, errorMessage);
        return new BadExpression(location);
    }

    /// <summary>
    /// Creates an UnknownReference expression that should be returned at the end of the expression building.<br/>
    /// Always use this to return an UnknownReference!
    /// </summary>
    private UnknownReference UnknownReference(Location location, string errorMessage) {
        _encounteredUnknownReference = new UnknownReference(location, errorMessage);
        return _encounteredUnknownReference;
    }

    /// <summary>
    /// <see cref="UnknownReference"/> but with a common message
    /// </summary>
    private UnknownReference UnknownIdentifier(Location location, string identifier) =>
        UnknownReference(location, $"Unknown identifier \"{identifier}\"");
}
