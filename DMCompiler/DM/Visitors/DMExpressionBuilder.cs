using DMCompiler.Compiler.DM;
using DMCompiler.DM.Expressions;
using System;
using DMCompiler.Compiler;

namespace DMCompiler.DM.Visitors;

internal static class DMExpressionBuilder {
    public enum ScopeMode {
        // All in-scope procs and vars available
        Normal,

        // Only global vars and procs available
        Static,

        // Only global procs available
        FirstPassStatic
    }

    public static ScopeMode CurrentScopeMode = ScopeMode.Normal;

    public static DMExpression BuildExpression(DMASTExpression expression, DMObject dmObject, DMProc proc, DreamPath? inferredPath = null) {
        switch (expression) {
            case DMASTExpressionConstant constant: return BuildConstant(constant, dmObject, proc);
            case DMASTStringFormat stringFormat: return BuildStringFormat(stringFormat, dmObject, proc, inferredPath);
            case DMASTIdentifier identifier: return BuildIdentifier(identifier, dmObject, proc);
            case DMASTGlobalIdentifier globalIdentifier: return BuildGlobalIdentifier(globalIdentifier, dmObject);
            case DMASTCallableSelf: return new ProcSelf(expression.Location);
            case DMASTCallableSuper: return new ProcSuper(expression.Location);
            case DMASTCallableGlobalProc globalProc: return new GlobalProc(expression.Location, globalProc.Identifier);
            case DMASTCallableProcIdentifier procIdentifier: return BuildCallableProcIdentifier(procIdentifier, dmObject);
            case DMASTProcCall procCall: return BuildProcCall(procCall, dmObject, proc, inferredPath);
            case DMASTAssign assign: return BuildAssign(assign, dmObject, proc, inferredPath);
            case DMASTAssignInto assignInto: return BuildAssignInto(assignInto, dmObject, proc, inferredPath);
            case DMASTEqual equal: return BuildEqual(equal, dmObject, proc, inferredPath);
            case DMASTNotEqual notEqual: return BuildNotEqual(notEqual, dmObject, proc, inferredPath);
            case DMASTDereference deref: return BuildDereference(deref, dmObject, proc, inferredPath);
            case DMASTLocate locate: return BuildLocate(locate, dmObject, proc, inferredPath);
            case DMASTImplicitIsType implicitIsType: return BuildImplicitIsType(implicitIsType, dmObject, proc, inferredPath);
            case DMASTList list: return BuildList(list, dmObject, proc);
            case DMASTDimensionalList dimensionalList: return BuildDimensionalList(dimensionalList, dmObject, proc, inferredPath);
            case DMASTNewList newList: return BuildNewList(newList, dmObject, proc, inferredPath);
            case DMASTAddText addText: return BuildAddText(addText, dmObject, proc, inferredPath);
            case DMASTInput input: return BuildInput(input, dmObject, proc);
            case DMASTPick pick: return BuildPick(pick, dmObject, proc);
            case DMASTLog log: return BuildLog(log, dmObject, proc, inferredPath);
            case DMASTCall call: return BuildCall(call, dmObject, proc, inferredPath);
            case DMASTExpressionWrapped wrapped: return BuildExpression(wrapped.Expression, dmObject, proc, inferredPath);

            case DMASTNegate negate:
                return new Negate(negate.Location, BuildExpression(negate.Expression, dmObject, proc, inferredPath));
            case DMASTNot not:
                return new Not(not.Location, BuildExpression(not.Expression, dmObject, proc, inferredPath));
            case DMASTBinaryNot binaryNot:
                return new BinaryNot(binaryNot.Location, BuildExpression(binaryNot.Value, dmObject, proc, inferredPath));
            case DMASTAdd add:
                return new Add(add.Location,
                    BuildExpression(add.A, dmObject, proc, inferredPath),
                    BuildExpression(add.B, dmObject, proc, inferredPath));
            case DMASTSubtract subtract:
                return new Subtract(subtract.Location,
                    BuildExpression(subtract.A, dmObject, proc, inferredPath),
                    BuildExpression(subtract.B, dmObject, proc, inferredPath));
            case DMASTMultiply multiply:
                return new Multiply(multiply.Location,
                    BuildExpression(multiply.A, dmObject, proc, inferredPath),
                    BuildExpression(multiply.B, dmObject, proc, inferredPath));
            case DMASTDivide divide:
                return new Divide(divide.Location,
                    BuildExpression(divide.A, dmObject, proc, inferredPath),
                    BuildExpression(divide.B, dmObject, proc, inferredPath));
            case DMASTModulus modulus:
                return new Modulo(modulus.Location,
                    BuildExpression(modulus.A, dmObject, proc, inferredPath),
                    BuildExpression(modulus.B, dmObject, proc, inferredPath));
            case DMASTModulusModulus modulusModulus:
                return new ModuloModulo(modulusModulus.Location,
                    BuildExpression(modulusModulus.A, dmObject, proc, inferredPath),
                    BuildExpression(modulusModulus.B, dmObject, proc, inferredPath));
            case DMASTPower power:
                return new Power(power.Location,
                    BuildExpression(power.A, dmObject, proc, inferredPath),
                    BuildExpression(power.B, dmObject, proc, inferredPath));
            case DMASTAppend append:
                return new Append(append.Location,
                    BuildExpression(append.A, dmObject, proc, inferredPath),
                    BuildExpression(append.B, dmObject, proc, inferredPath));
            case DMASTCombine combine:
                return new Combine(combine.Location,
                    BuildExpression(combine.A, dmObject, proc, inferredPath),
                    BuildExpression(combine.B, dmObject, proc, inferredPath));
            case DMASTRemove remove:
                return new Remove(remove.Location,
                    BuildExpression(remove.A, dmObject, proc, inferredPath),
                    BuildExpression(remove.B, dmObject, proc, inferredPath));
            case DMASTMask mask:
                return new Mask(mask.Location,
                    BuildExpression(mask.A, dmObject, proc, inferredPath),
                    BuildExpression(mask.B, dmObject, proc, inferredPath));
            case DMASTLogicalAndAssign lAnd:
                var lAndLHS = BuildExpression(lAnd.A, dmObject, proc, inferredPath);
                var lAndRHS = BuildExpression(lAnd.B, dmObject, proc, lAndLHS.NestedPath);
                return new LogicalAndAssign(lAnd.Location,
                    lAndLHS,
                    lAndRHS);
            case DMASTLogicalOrAssign lOr:
                var lOrLHS = BuildExpression(lOr.A, dmObject, proc, inferredPath);
                var lOrRHS = BuildExpression(lOr.B, dmObject, proc, lOrLHS.NestedPath);
                return new LogicalOrAssign(lOr.Location, lOrLHS, lOrRHS);
            case DMASTMultiplyAssign multiplyAssign:
                return new MultiplyAssign(multiplyAssign.Location,
                    BuildExpression(multiplyAssign.A, dmObject, proc, inferredPath),
                    BuildExpression(multiplyAssign.B, dmObject, proc, inferredPath));
            case DMASTDivideAssign divideAssign:
                return new DivideAssign(divideAssign.Location,
                    BuildExpression(divideAssign.A, dmObject, proc, inferredPath),
                    BuildExpression(divideAssign.B, dmObject, proc, inferredPath));
            case DMASTLeftShiftAssign leftShiftAssign:
                return new LeftShiftAssign(leftShiftAssign.Location,
                    BuildExpression(leftShiftAssign.A, dmObject, proc, inferredPath),
                    BuildExpression(leftShiftAssign.B, dmObject, proc, inferredPath));
            case DMASTRightShiftAssign rightShiftAssign:
                return new RightShiftAssign(rightShiftAssign.Location,
                    BuildExpression(rightShiftAssign.A, dmObject, proc, inferredPath),
                    BuildExpression(rightShiftAssign.B, dmObject, proc, inferredPath));
            case DMASTXorAssign xorAssign:
                return new XorAssign(xorAssign.Location,
                    BuildExpression(xorAssign.A, dmObject, proc, inferredPath),
                    BuildExpression(xorAssign.B, dmObject, proc, inferredPath));
            case DMASTModulusAssign modulusAssign:
                return new ModulusAssign(modulusAssign.Location,
                    BuildExpression(modulusAssign.A, dmObject, proc, inferredPath),
                    BuildExpression(modulusAssign.B, dmObject, proc, inferredPath));
            case DMASTModulusModulusAssign modulusModulusAssign:
                var mmAssignLHS = BuildExpression(modulusModulusAssign.A, dmObject, proc, inferredPath);
                var mmAssignRHS = BuildExpression(modulusModulusAssign.B, dmObject, proc, mmAssignLHS.NestedPath);
                return new ModulusModulusAssign(modulusModulusAssign.Location, mmAssignLHS, mmAssignRHS);
            case DMASTLeftShift leftShift:
                return new LeftShift(leftShift.Location,
                    BuildExpression(leftShift.A, dmObject, proc, inferredPath),
                    BuildExpression(leftShift.B, dmObject, proc, inferredPath));
            case DMASTRightShift rightShift:
                return new RightShift(rightShift.Location,
                    BuildExpression(rightShift.A, dmObject, proc, inferredPath),
                    BuildExpression(rightShift.B, dmObject, proc, inferredPath));
            case DMASTBinaryAnd binaryAnd:
                return new BinaryAnd(binaryAnd.Location,
                    BuildExpression(binaryAnd.A, dmObject, proc, inferredPath),
                    BuildExpression(binaryAnd.B, dmObject, proc, inferredPath));
            case DMASTBinaryXor binaryXor:
                return new BinaryXor(binaryXor.Location,
                    BuildExpression(binaryXor.A, dmObject, proc, inferredPath),
                    BuildExpression(binaryXor.B, dmObject, proc, inferredPath));
            case DMASTBinaryOr binaryOr:
                return new BinaryOr(binaryOr.Location,
                    BuildExpression(binaryOr.A, dmObject, proc, inferredPath),
                    BuildExpression(binaryOr.B, dmObject, proc, inferredPath));
            case DMASTEquivalent equivalent:
                return new Equivalent(equivalent.Location,
                    BuildExpression(equivalent.A, dmObject, proc, inferredPath),
                    BuildExpression(equivalent.B, dmObject, proc, inferredPath));
            case DMASTNotEquivalent notEquivalent:
                return new NotEquivalent(notEquivalent.Location,
                    BuildExpression(notEquivalent.A, dmObject, proc, inferredPath),
                    BuildExpression(notEquivalent.B, dmObject, proc, inferredPath));
            case DMASTGreaterThan greaterThan:
                return new GreaterThan(greaterThan.Location,
                    BuildExpression(greaterThan.A, dmObject, proc, inferredPath),
                    BuildExpression(greaterThan.B, dmObject, proc, inferredPath));
            case DMASTGreaterThanOrEqual greaterThanOrEqual:
                return new GreaterThanOrEqual(greaterThanOrEqual.Location,
                    BuildExpression(greaterThanOrEqual.A, dmObject, proc, inferredPath),
                    BuildExpression(greaterThanOrEqual.B, dmObject, proc, inferredPath));
            case DMASTLessThan lessThan:
                return new LessThan(lessThan.Location,
                    BuildExpression(lessThan.A, dmObject, proc, inferredPath),
                    BuildExpression(lessThan.B, dmObject, proc, inferredPath));
            case DMASTLessThanOrEqual lessThanOrEqual:
                return new LessThanOrEqual(lessThanOrEqual.Location,
                    BuildExpression(lessThanOrEqual.A, dmObject, proc, inferredPath),
                    BuildExpression(lessThanOrEqual.B, dmObject, proc, inferredPath));
            case DMASTOr or:
                return new Or(or.Location,
                    BuildExpression(or.A, dmObject, proc, inferredPath),
                    BuildExpression(or.B, dmObject, proc, inferredPath));
            case DMASTAnd and:
                return new And(and.Location,
                    BuildExpression(and.A, dmObject, proc, inferredPath),
                    BuildExpression(and.B, dmObject, proc, inferredPath));
            case DMASTTernary ternary:
                return new Ternary(ternary.Location,
                    BuildExpression(ternary.A, dmObject, proc, inferredPath),
                    BuildExpression(ternary.B, dmObject, proc, inferredPath),
                    BuildExpression(ternary.C ?? new DMASTConstantNull(ternary.Location), dmObject, proc, inferredPath));
            case DMASTNewPath newPath:
                if (BuildExpression(newPath.Path, dmObject, proc, inferredPath) is not Path path) {
                    DMCompiler.Emit(WarningCode.BadExpression, newPath.Path.Location, "Expected a path expression");
                    return new Null(newPath.Location);
                }

                return new NewPath(newPath.Location, path,
                    new ArgumentList(newPath.Location, dmObject, proc, newPath.Parameters, inferredPath));
            case DMASTNewExpr newExpr:
                return new New(newExpr.Location,
                    BuildExpression(newExpr.Expression, dmObject, proc, inferredPath),
                    new ArgumentList(newExpr.Location, dmObject, proc, newExpr.Parameters, inferredPath));
            case DMASTNewInferred newInferred:
                if (inferredPath is null) {
                    DMCompiler.Emit(WarningCode.BadExpression, newInferred.Location, "Could not infer a type");
                    return new Null(newInferred.Location);
                }

                return new NewPath(newInferred.Location, new Path(newInferred.Location, dmObject, inferredPath.Value),
                    new ArgumentList(newInferred.Location, dmObject, proc, newInferred.Parameters, inferredPath));
            case DMASTPreIncrement preIncrement:
                return new PreIncrement(preIncrement.Location, BuildExpression(preIncrement.Expression, dmObject, proc, inferredPath));
            case DMASTPostIncrement postIncrement:
                return new PostIncrement(postIncrement.Location, BuildExpression(postIncrement.Expression, dmObject, proc, inferredPath));
            case DMASTPreDecrement preDecrement:
                return new PreDecrement(preDecrement.Location, BuildExpression(preDecrement.Expression, dmObject, proc, inferredPath));
            case DMASTPostDecrement postDecrement:
                return new PostDecrement(postDecrement.Location, BuildExpression(postDecrement.Expression, dmObject, proc, inferredPath));
            case DMASTGradient gradient:
                return new Gradient(gradient.Location,
                    new ArgumentList(gradient.Location, dmObject, proc, gradient.Parameters));
            case DMASTLocateCoordinates locateCoordinates:
                return new LocateCoordinates(locateCoordinates.Location,
                    BuildExpression(locateCoordinates.X, dmObject, proc, inferredPath),
                    BuildExpression(locateCoordinates.Y, dmObject, proc, inferredPath),
                    BuildExpression(locateCoordinates.Z, dmObject, proc, inferredPath));
            case DMASTIsSaved isSaved:
                return new IsSaved(isSaved.Location, BuildExpression(isSaved.Expression, dmObject, proc, inferredPath));
            case DMASTIsType isType:
                return new IsType(isType.Location,
                    BuildExpression(isType.Value, dmObject, proc, inferredPath),
                    BuildExpression(isType.Type, dmObject, proc, inferredPath));
            case DMASTIsNull isNull:
                return new IsNull(isNull.Location, BuildExpression(isNull.Value, dmObject, proc, inferredPath));
            case DMASTLength length:
                return new Length(length.Location, BuildExpression(length.Value, dmObject, proc, inferredPath));
            case DMASTGetStep getStep:
                return new GetStep(getStep.Location,
                    BuildExpression(getStep.Ref, dmObject, proc, inferredPath),
                    BuildExpression(getStep.Dir, dmObject, proc, inferredPath));
            case DMASTGetDir getDir:
                return new GetDir(getDir.Location,
                    BuildExpression(getDir.Loc1, dmObject, proc, inferredPath),
                    BuildExpression(getDir.Loc2, dmObject, proc, inferredPath));
            case DMASTProb prob:
                return new Prob(prob.Location,
                    BuildExpression(prob.P, dmObject, proc, inferredPath));
            case DMASTInitial initial:
                return new Initial(initial.Location, BuildExpression(initial.Expression, dmObject, proc, inferredPath));
            case DMASTNameof nameof:
                return BuildNameof(nameof, dmObject, proc, inferredPath);
            case DMASTExpressionIn expressionIn:
                return new In(expressionIn.Location,
                    BuildExpression(expressionIn.Value, dmObject, proc, inferredPath),
                    BuildExpression(expressionIn.List, dmObject, proc, inferredPath));
            case DMASTExpressionInRange expressionInRange:
                return new InRange(expressionInRange.Location,
                    BuildExpression(expressionInRange.Value, dmObject, proc, inferredPath),
                    BuildExpression(expressionInRange.StartRange, dmObject, proc, inferredPath),
                    BuildExpression(expressionInRange.EndRange, dmObject, proc, inferredPath));
            case DMASTSin sin:
                return new Sin(sin.Location, BuildExpression(sin.Expression, dmObject, proc, inferredPath));
            case DMASTCos cos:
                return new Cos(cos.Location, BuildExpression(cos.Expression, dmObject, proc, inferredPath));
            case DMASTTan tan:
                return new Tan(tan.Location, BuildExpression(tan.Expression, dmObject, proc, inferredPath));
            case DMASTArcsin arcSin:
                return new ArcSin(arcSin.Location, BuildExpression(arcSin.Expression, dmObject, proc, inferredPath));
            case DMASTArccos arcCos:
                return new ArcCos(arcCos.Location, BuildExpression(arcCos.Expression, dmObject, proc, inferredPath));
            case DMASTArctan arcTan:
                return new ArcTan(arcTan.Location, BuildExpression(arcTan.Expression, dmObject, proc, inferredPath));
            case DMASTArctan2 arcTan2:
                return new ArcTan2(arcTan2.Location,
                    BuildExpression(arcTan2.XExpression, dmObject, proc, inferredPath),
                    BuildExpression(arcTan2.YExpression, dmObject, proc, inferredPath));
            case DMASTSqrt sqrt:
                return new Sqrt(sqrt.Location, BuildExpression(sqrt.Expression, dmObject, proc, inferredPath));
            case DMASTAbs abs:
                return new Abs(abs.Location, BuildExpression(abs.Expression, dmObject, proc, inferredPath));

            case DMASTVarDeclExpression varDeclExpr:
                var declIdentifier = new DMASTIdentifier(expression.Location, varDeclExpr.DeclPath.Path.LastElement);
                return BuildIdentifier(declIdentifier, dmObject, proc);
            case DMASTVoid:
                DMCompiler.Emit(WarningCode.BadExpression, expression.Location, "Attempt to use a void expression");
                return new Null(expression.Location);
        }

        throw new ArgumentException($"Invalid expression {expression}", nameof(expression));
    }

    private static DMExpression BuildConstant(DMASTExpressionConstant constant, DMObject dmObject, DMProc proc) {
        switch (constant) {
            case DMASTConstantNull: return new Null(constant.Location);
            case DMASTConstantInteger constInt: return new Number(constant.Location, constInt.Value);
            case DMASTConstantFloat constFloat: return new Number(constant.Location, constFloat.Value);
            case DMASTConstantString constString: return new Expressions.String(constant.Location, constString.Value);
            case DMASTConstantResource constResource: return new Resource(constant.Location, constResource.Path);
            case DMASTConstantPath constPath: return new Path(constant.Location, dmObject, constPath.Value.Path);
            case DMASTUpwardPathSearch upwardSearch:
                DMExpression.TryConstant(dmObject, proc, upwardSearch.Path, out var pathExpr);
                if (pathExpr is not Path expr)
                    throw new CompileErrorException(constant.Location, $"Cannot do an upward path search on {pathExpr}");

                DreamPath path = expr.Value;
                DreamPath? foundPath = DMObjectTree.UpwardSearch(path, upwardSearch.Search.Path);

                if (foundPath == null) {
                    throw new CompileErrorException(constant.Location,$"Invalid path {path}.{upwardSearch.Search.Path}");
                }

                return new Path(constant.Location, dmObject, foundPath.Value);
        }

        throw new ArgumentException($"Invalid constant {constant}", nameof(constant));
    }

    private static StringFormat BuildStringFormat(DMASTStringFormat stringFormat, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var expressions = new DMExpression[stringFormat.InterpolatedValues.Length];

        for (int i = 0; i < stringFormat.InterpolatedValues.Length; i++) {
            var interpolatedValue = stringFormat.InterpolatedValues[i];

            if (interpolatedValue == null) {
                expressions[i] = new Null(stringFormat.Location);
            } else {
                expressions[i] = DMExpression.Create(dmObject, proc, interpolatedValue, inferredPath);
            }
        }

        return new StringFormat(stringFormat.Location, stringFormat.Value, expressions);
    }

    private static DMExpression BuildIdentifier(DMASTIdentifier identifier, DMObject dmObject, DMProc proc) {
        var name = identifier.Identifier;

        switch (name) {
            case "src":
                return new Src(identifier.Location, dmObject.Path);
            case "usr":
                return new Usr(identifier.Location);
            case "args":
                return new Args(identifier.Location);
            case "__TYPE__":
                return new ProcOwnerType(identifier.Location);
            case "__PROC__": // The saner alternative to .....
                return new ProcType(identifier.Location);
            case "global":
                return new Global(identifier.Location);
            default: {
                if (CurrentScopeMode == ScopeMode.Normal) {
                    DMProc.LocalVariable localVar = proc?.GetLocalVariable(name);
                    if (localVar != null)
                        return new Local(identifier.Location, localVar);

                    var field = dmObject?.GetVariable(name);
                    if (field != null)
                        return new Field(identifier.Location, field);
                }

                if (CurrentScopeMode != ScopeMode.FirstPassStatic) {
                    int? globalId = proc?.GetGlobalVariableId(name) ?? dmObject?.GetGlobalVariableId(name);

                    if (globalId != null) {
                        var global = new GlobalField(identifier.Location, DMObjectTree.Globals[globalId.Value].Type, globalId.Value);

                        return global;
                    }
                }

                throw new UnknownIdentifierException(identifier.Location, name);
            }
        }
    }

    private static DMExpression BuildGlobalIdentifier(DMASTGlobalIdentifier globalIdentifier, DMObject dmObject) {
        string name = globalIdentifier.Identifier;

        if (CurrentScopeMode != ScopeMode.FirstPassStatic) {
            int? globalId = dmObject?.GetGlobalVariableId(name);
            if (globalId != null) {
                return new GlobalField(globalIdentifier.Location, DMObjectTree.Globals[globalId.Value].Type,
                    globalId.Value);
            } else if (name == "vars") {
                return new GlobalVars(globalIdentifier.Location);
            }
        }

        throw new CompileErrorException(globalIdentifier.Location, $"Unknown global \"{name}\"");
    }

    private static DMExpression BuildCallableProcIdentifier(DMASTCallableProcIdentifier procIdentifier, DMObject dmObject) {
        if (CurrentScopeMode is ScopeMode.Static or ScopeMode.FirstPassStatic)
            return new GlobalProc(procIdentifier.Location, procIdentifier.Identifier);
        if (dmObject.HasProc(procIdentifier.Identifier))
            return new Proc(procIdentifier.Location, procIdentifier.Identifier);
        if (DMObjectTree.TryGetGlobalProc(procIdentifier.Identifier, out _))
            return new GlobalProc(procIdentifier.Location, procIdentifier.Identifier);

        throw new CompileErrorException(procIdentifier.Location, $"Type {dmObject.Path} does not have a proc named \"{procIdentifier.Identifier}\"");
    }

    private static DMExpression BuildProcCall(DMASTProcCall procCall, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        // arglist hack
        if (procCall.Callable is DMASTCallableProcIdentifier { Identifier: "arglist" }) {
            switch (procCall.Parameters.Length) {
                case 0:
                    DMCompiler.Emit(WarningCode.BadArgument, procCall.Location, "arglist() requires 1 argument");
                    break;
                case 1:
                    break;
                default:
                    DMCompiler.Emit(
                        WarningCode.TooManyArguments,
                        procCall.Location,
                        $"arglist() given {procCall.Parameters.Length} arguments, expecting 1");
                    break;
            }

            var expr = DMExpression.Create(dmObject, proc, procCall.Parameters[0].Value, inferredPath);
            return new Arglist(procCall.Location, expr);
        }

        var target = DMExpression.Create(dmObject, proc, (DMASTExpression)procCall.Callable, inferredPath);
        var args = new ArgumentList(procCall.Location, dmObject, proc, procCall.Parameters);
        return new ProcCall(procCall.Location, target, args);
    }

    private static DMExpression BuildAssign(DMASTAssign assign, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var lhs = DMExpression.Create(dmObject, proc, assign.Expression, inferredPath);
        var rhs = DMExpression.Create(dmObject, proc, assign.Value, lhs.NestedPath);
        if(lhs.TryAsConstant(out _)) {
            DMCompiler.Emit(WarningCode.WriteToConstant, assign.Expression.Location, "Cannot write to const var");
        }

        return new Assignment(assign.Location, lhs, rhs);
    }

    private static DMExpression BuildAssignInto(DMASTAssignInto assign, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var lhs = DMExpression.Create(dmObject, proc, assign.Expression, inferredPath);
        var rhs = DMExpression.Create(dmObject, proc, assign.Value, lhs.NestedPath);
        if(lhs.TryAsConstant(out _)) {
            DMCompiler.Emit(WarningCode.WriteToConstant, assign.Expression.Location, "Cannot write to const var");
        }

        return new AssignmentInto(assign.Location, lhs, rhs);
    }

    private static DMExpression BuildEqual(DMASTEqual equal, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var lhs = DMExpression.Create(dmObject, proc, equal.A, inferredPath);
        var rhs = DMExpression.Create(dmObject, proc, equal.B, inferredPath);

        // (x == null) can be changed to isnull(x) which compiles down to an opcode
        // TODO: Bytecode optimizations instead
        if (rhs is Null)
            return new IsNull(equal.Location, lhs);

        return new Equal(equal.Location, lhs, rhs);
    }

    private static DMExpression BuildNotEqual(DMASTNotEqual notEqual, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var lhs = DMExpression.Create(dmObject, proc, notEqual.A, inferredPath);
        var rhs = DMExpression.Create(dmObject, proc, notEqual.B, inferredPath);

        // (x != null) can be changed to !isnull(x) which compiles down to two opcodes
        // TODO: Bytecode optimizations instead
        if (rhs is Null)
            return new Not(notEqual.Location, new IsNull(notEqual.Location, lhs));

        return new NotEqual(notEqual.Location, lhs, rhs);
    }

    private static DMExpression BuildDereference(DMASTDereference deref, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var astOperations = deref.Operations;

        // The base expression and list of operations to perform on it
        // These may be redefined if we encounter a global access mid-operation
        var expr = DMExpression.Create(dmObject, proc, deref.Expression, inferredPath);
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
                        ArgumentList argumentList = new(deref.Expression.Location, dmObject, proc,
                            callOperation.Parameters);

                        var globalProc = new GlobalProc(expr.Location, callOperation.Identifier);
                        expr = new ProcCall(expr.Location, globalProc, argumentList);
                        break;

                    case DMASTDereference.FieldOperation:
                        // global.vars
                        if (namedOperation is { Identifier: "vars" }) {
                            expr = new GlobalVars(expr.Location);
                            break;
                        }

                        // global.variable
                        var globalId = dmObject.GetGlobalVariableId(namedOperation.Identifier);
                        if (globalId == null) {
                            throw new UnknownIdentifierException(deref.Location, $"global.{namedOperation.Identifier}");
                        }

                        var property = DMObjectTree.Globals[globalId.Value];
                        expr = new GlobalField(expr.Location, property.Type, globalId.Value);

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
                DMCompiler.Emit(WarningCode.BadExpression, firstOperation.Location,
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
                        if (prevPath == null) {
                            throw new UnknownIdentifierException(deref.Location, field);
                        }

                        DMObject? fromObject = DMObjectTree.GetDMObject(prevPath.Value, false);
                        if (fromObject == null) {
                            DMCompiler.Emit(WarningCode.ItemDoesntExist, fieldOperation.Location,
                                $"Type {prevPath.Value} does not exist");
                            return new Null(deref.Location);
                        }

                        property = fromObject.GetVariable(field);
                        if (!fieldOperation.Safe && fromObject.IsSubtypeOf(DreamPath.Client)) {
                            DMCompiler.Emit(WarningCode.UnsafeClientAccess, deref.Location,
                                "Unsafe \"client\" access. Use the \"?.\" operator instead");
                        }

                        if (property == null && fromObject.GetGlobalVariableId(field) is { } globalId) {
                            property = DMObjectTree.Globals[globalId];

                            expr = new GlobalField(expr.Location, property.Type, globalId);

                            var newOperationCount = operations.Length - i - 1;
                            if (newOperationCount == 0) {
                                return expr;
                            }

                            if (property == null) {
                                throw new UnknownIdentifierException(deref.Location, field);
                            }

                            if ((property.ValType & DMValueType.Unimplemented) == DMValueType.Unimplemented) {
                                DMCompiler.UnimplementedWarning(deref.Location,
                                    $"{prevPath}.{field} is not implemented and will have unexpected behavior");
                            }

                            operations = new Dereference.Operation[newOperationCount];
                            astOperationOffset = i + 1;
                            i = -1;
                            prevPath = property.Type;
                            pathIsFuzzy = prevPath == null;
                            continue;
                        }

                        if (property == null) {
                            throw new UnknownIdentifierException(deref.Location, field);
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
                        // Passing the path here is cursed, but one of the tests seems to suggest we want that?
                        Index = DMExpression.Create(dmObject, proc, indexOperation.Index, prevPath),
                        Safe = indexOperation.Safe,
                        Path = prevPath
                    };
                    prevPath = null;
                    pathIsFuzzy = true;
                    break;

                case DMASTDereference.CallOperation callOperation: {
                    var field = callOperation.Identifier;
                    ArgumentList argumentList = new(deref.Expression.Location, dmObject, proc, callOperation.Parameters);

                    if (!callOperation.NoSearch && !pathIsFuzzy) {
                        if (prevPath == null) {
                            throw new UnknownIdentifierException(deref.Location, field);
                        }

                        DMObject? fromObject = DMObjectTree.GetDMObject(prevPath.Value, false);
                        if (fromObject == null) {
                            DMCompiler.Emit(WarningCode.ItemDoesntExist, callOperation.Location,
                                $"Type {prevPath.Value} does not exist");
                            return new Null(deref.Location);
                        }

                        if (!fromObject.HasProc(field)) {
                            DMCompiler.Emit(WarningCode.ItemDoesntExist, callOperation.Location,
                                $"Type {prevPath.Value} does not have a proc named \"{field}\"");
                            return new Null(deref.Location);
                        }
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

    private static DMExpression BuildLocate(DMASTLocate locate, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var container = locate.Container != null ? DMExpression.Create(dmObject, proc, locate.Container, inferredPath) : null;

        if (locate.Expression == null) {
            if (inferredPath == null) {
                throw new CompileErrorException(locate.Location, "inferred locate requires a type");
            }

            return new LocateInferred(locate.Location, inferredPath.Value, container);
        }

        var pathExpr = DMExpression.Create(dmObject, proc, locate.Expression, inferredPath);
        return new Locate(locate.Location, pathExpr, container);
    }

    private static DMExpression BuildImplicitIsType(DMASTImplicitIsType isType, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var expr = DMExpression.Create(dmObject, proc, isType.Value, inferredPath);

        if (expr.Path is null)
            throw new CompileErrorException(isType.Location,"An inferred istype requires a type!");

        return new IsTypeInferred(isType.Location, expr, expr.Path.Value);
    }

    private static DMExpression BuildList(DMASTList list, DMObject dmObject, DMProc proc) {
        (DMExpression? Key, DMExpression Value)[] values = Array.Empty<(DMExpression?, DMExpression)>();

        if (list.Values != null) {
            values = new (DMExpression?, DMExpression)[list.Values.Length];

            for (int i = 0; i < list.Values.Length; i++) {
                DMASTCallParameter value = list.Values[i];
                DMExpression? key = (value.Key != null) ? DMExpression.Create(dmObject, proc, value.Key) : null;
                DMExpression listValue = DMExpression.Create(dmObject, proc, value.Value);

                values[i] = (key, listValue);
            }
        }

        return new List(list.Location, values);
    }

    private static DMExpression BuildDimensionalList(DMASTDimensionalList list, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var sizes = new DMExpression[list.Sizes.Count];
        for (int i = 0; i < sizes.Length; i++) {
            var sizeExpr = DMExpression.Create(dmObject, proc, list.Sizes[i], inferredPath);

            sizes[i] = sizeExpr;
        }

        return new DimensionalList(list.Location, sizes);
    }

    // nameof(x)
    private static DMExpression BuildNameof(DMASTNameof nameof, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var expr = BuildExpression(nameof.Expression, dmObject, proc, inferredPath);
        if (expr.GetNameof(dmObject, proc) is { } name) {
            return new Expressions.String(nameof.Location, name);
        }

        DMCompiler.Emit(WarningCode.BadArgument, nameof.Location, "nameof() requires a var, proc reference, or type path");
        return new Null(nameof.Location);
    }

    private static DMExpression BuildNewList(DMASTNewList newList, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        DMExpression[] expressions = new DMExpression[newList.Parameters.Length];

        for (int i = 0; i < newList.Parameters.Length; i++) {
            DMASTCallParameter parameter = newList.Parameters[i];
            if (parameter.Key != null) throw new CompileErrorException(newList.Location,"newlist() does not take named arguments");

            expressions[i] = DMExpression.Create(dmObject, proc, parameter.Value, inferredPath);
        }

        return new NewList(newList.Location, expressions);
    }

    private static DMExpression BuildAddText(DMASTAddText addText, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        if (addText.Parameters.Length < 2)
            throw new CompileErrorException(addText.Location, "Invalid addtext() parameter count; expected 2 or more arguments");

        DMExpression[] expArr = new DMExpression[addText.Parameters.Length];
        for (int i = 0; i < expArr.Length; i++) {
            DMASTCallParameter parameter = addText.Parameters[i];
            if(parameter.Key != null)
                DMCompiler.Emit(WarningCode.TooManyArguments, parameter.Location, "addtext() does not take named arguments");

            expArr[i] = DMExpression.Create(dmObject,proc, parameter.Value, inferredPath);
        }

        return new AddText(addText.Location, expArr);
    }

    private static DMExpression BuildInput(DMASTInput input, DMObject dmObject, DMProc proc) {
        DMExpression[] arguments = new DMExpression[input.Parameters.Length];
        for (int i = 0; i < input.Parameters.Length; i++) {
            DMASTCallParameter parameter = input.Parameters[i];

            if (parameter.Key != null) {
                DMCompiler.Emit(WarningCode.BadArgument, parameter.Location, "input() does not take named arguments");
            }

            arguments[i] = DMExpression.Create(dmObject, proc, parameter.Value);
        }

        DMExpression? list = null;
        if (input.List != null) {
            list = DMExpression.Create(dmObject, proc, input.List);

            DMValueType objectTypes = DMValueType.Null |DMValueType.Obj | DMValueType.Mob | DMValueType.Turf |
                                      DMValueType.Area;

            // Default filter is "as anything" when there's a list
            input.Types ??= DMValueType.Anything;
            if (input.Types != DMValueType.Anything && (input.Types & objectTypes) == 0x0) {
                DMCompiler.Emit(WarningCode.BadArgument, input.Location,
                    $"Invalid input() filter \"{input.Types}\". Filter must be \"{DMValueType.Anything}\" or at least one of \"{objectTypes}\"");
            }
        } else {
            // Default filter is "as text" when there's no list
            input.Types ??= DMValueType.Text;
        }

        return new Input(input.Location, arguments, input.Types.Value, list);
    }

    private static DMExpression BuildPick(DMASTPick pick, DMObject dmObject, DMProc proc) {
        Pick.PickValue[] pickValues = new Pick.PickValue[pick.Values.Length];

        for (int i = 0; i < pickValues.Length; i++) {
            DMASTPick.PickValue pickValue = pick.Values[i];
            DMExpression? weight = (pickValue.Weight != null) ? DMExpression.Create(dmObject, proc, pickValue.Weight) : null;
            DMExpression value = DMExpression.Create(dmObject, proc, pickValue.Value);

            if (weight is Prob prob) // pick(prob(50);x, prob(200);y) format
                weight = prob.P;

            pickValues[i] = new Pick.PickValue(weight, value);
        }

        return new Pick(pick.Location, pickValues);
    }

    private static DMExpression BuildLog(DMASTLog log, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var expr = DMExpression.Create(dmObject, proc, log.Expression, inferredPath);
        DMExpression? baseExpr = null;

        if (log.BaseExpression != null) {
            baseExpr = DMExpression.Create(dmObject, proc, log.BaseExpression, inferredPath);
        }

        return new Log(log.Location, expr, baseExpr);
    }

    private static DMExpression BuildCall(DMASTCall call, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var procArgs = new ArgumentList(call.Location, dmObject, proc, call.ProcParameters, inferredPath);

        switch (call.CallParameters.Length) {
            default:
                DMCompiler.Emit(WarningCode.TooManyArguments, call.Location, "Too many arguments for call()");
                goto case 2; // Fallthrough!
            case 2: {
                var a = DMExpression.Create(dmObject, proc, call.CallParameters[0].Value, inferredPath);
                var b = DMExpression.Create(dmObject, proc, call.CallParameters[1].Value, inferredPath);
                return new CallStatement(call.Location, a, b, procArgs);
            }
            case 1: {
                var a = DMExpression.Create(dmObject, proc, call.CallParameters[0].Value, inferredPath);
                return new CallStatement(call.Location, a, procArgs);
            }
            case 0:
                DMCompiler.Emit(WarningCode.BadArgument, call.Location, "Not enough arguments for call()");
                return new CallStatement(call.Location, new Null(Location.Internal), procArgs);
        }
    }
}
