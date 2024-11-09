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
        FirstPassStatic,
    }

    // TODO: Remove these terrible global flags
    public ScopeMode CurrentScopeMode = ScopeMode.Normal;
    public bool ScopeOperatorEnabled = false; // Enabled once var overrides have been processed

    internal DMExpression BuildExpression(DMASTExpression expression, DMObject dmObject, DMProc proc, DreamPath? inferredPath = null) {
        switch (expression) {
            case DMASTInvalidExpression:
                // No  Compiler.Emit() here because the parser should have emitted an error when making this
                return new BadExpression(Compiler, expression.Location);

            case DMASTExpressionConstant constant: return BuildConstant(constant, dmObject, proc);
            case DMASTStringFormat stringFormat: return BuildStringFormat(stringFormat, dmObject, proc, inferredPath);
            case DMASTIdentifier identifier: return BuildIdentifier(identifier, dmObject, proc, inferredPath);
            case DMASTScopeIdentifier globalIdentifier: return BuildScopeIdentifier(globalIdentifier, dmObject, proc, inferredPath);
            case DMASTCallableSelf: return new ProcSelf(expression.Location, null, proc);
            case DMASTCallableSuper: return new ProcSuper(expression.Location, dmObject, proc);
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
            case DMASTExpressionWrapped wrapped: return BuildExpression(wrapped.Value, dmObject, proc, inferredPath);

            case DMASTNegate negate:
                return new Negate(negate.Location, BuildExpression(negate.Value, dmObject, proc, inferredPath));
            case DMASTNot not:
                return new Not(not.Location, BuildExpression(not.Value, dmObject, proc, inferredPath));
            case DMASTBinaryNot binaryNot:
                return new BinaryNot(binaryNot.Location, BuildExpression(binaryNot.Value, dmObject, proc, inferredPath));
            case DMASTAdd add:
                return new Add(add.Location,
                    BuildExpression(add.LHS, dmObject, proc, inferredPath),
                    BuildExpression(add.RHS, dmObject, proc, inferredPath));
            case DMASTSubtract subtract:
                return new Subtract(subtract.Location,
                    BuildExpression(subtract.LHS, dmObject, proc, inferredPath),
                    BuildExpression(subtract.RHS, dmObject, proc, inferredPath));
            case DMASTMultiply multiply:
                return new Multiply(multiply.Location,
                    BuildExpression(multiply.LHS, dmObject, proc, inferredPath),
                    BuildExpression(multiply.RHS, dmObject, proc, inferredPath));
            case DMASTDivide divide:
                return new Divide(divide.Location,
                    BuildExpression(divide.LHS, dmObject, proc, inferredPath),
                    BuildExpression(divide.RHS, dmObject, proc, inferredPath));
            case DMASTModulus modulus:
                return new Modulo(modulus.Location,
                    BuildExpression(modulus.LHS, dmObject, proc, inferredPath),
                    BuildExpression(modulus.RHS, dmObject, proc, inferredPath));
            case DMASTModulusModulus modulusModulus:
                return new ModuloModulo(modulusModulus.Location,
                    BuildExpression(modulusModulus.LHS, dmObject, proc, inferredPath),
                    BuildExpression(modulusModulus.RHS, dmObject, proc, inferredPath));
            case DMASTPower power:
                return new Power(power.Location,
                    BuildExpression(power.LHS, dmObject, proc, inferredPath),
                    BuildExpression(power.RHS, dmObject, proc, inferredPath));
            case DMASTAppend append:
                return new Append(append.Location,
                    BuildExpression(append.LHS, dmObject, proc, inferredPath),
                    BuildExpression(append.RHS, dmObject, proc, inferredPath));
            case DMASTCombine combine:
                return new Combine(combine.Location,
                    BuildExpression(combine.LHS, dmObject, proc, inferredPath),
                    BuildExpression(combine.RHS, dmObject, proc, inferredPath));
            case DMASTRemove remove:
                return new Remove(remove.Location,
                    BuildExpression(remove.LHS, dmObject, proc, inferredPath),
                    BuildExpression(remove.RHS, dmObject, proc, inferredPath));
            case DMASTMask mask:
                return new Mask(mask.Location,
                    BuildExpression(mask.LHS, dmObject, proc, inferredPath),
                    BuildExpression(mask.RHS, dmObject, proc, inferredPath));
            case DMASTLogicalAndAssign lAnd:
                var lAndLHS = BuildExpression(lAnd.LHS, dmObject, proc, inferredPath);
                var lAndRHS = BuildExpression(lAnd.RHS, dmObject, proc, lAndLHS.NestedPath);
                return new LogicalAndAssign(lAnd.Location,
                    lAndLHS,
                    lAndRHS);
            case DMASTLogicalOrAssign lOr:
                var lOrLHS = BuildExpression(lOr.LHS, dmObject, proc, inferredPath);
                var lOrRHS = BuildExpression(lOr.RHS, dmObject, proc, lOrLHS.NestedPath);
                return new LogicalOrAssign(lOr.Location, lOrLHS, lOrRHS);
            case DMASTMultiplyAssign multiplyAssign:
                return new MultiplyAssign(multiplyAssign.Location,
                    BuildExpression(multiplyAssign.LHS, dmObject, proc, inferredPath),
                    BuildExpression(multiplyAssign.RHS, dmObject, proc, inferredPath));
            case DMASTDivideAssign divideAssign:
                return new DivideAssign(divideAssign.Location,
                    BuildExpression(divideAssign.LHS, dmObject, proc, inferredPath),
                    BuildExpression(divideAssign.RHS, dmObject, proc, inferredPath));
            case DMASTLeftShiftAssign leftShiftAssign:
                return new LeftShiftAssign(leftShiftAssign.Location,
                    BuildExpression(leftShiftAssign.LHS, dmObject, proc, inferredPath),
                    BuildExpression(leftShiftAssign.RHS, dmObject, proc, inferredPath));
            case DMASTRightShiftAssign rightShiftAssign:
                return new RightShiftAssign(rightShiftAssign.Location,
                    BuildExpression(rightShiftAssign.LHS, dmObject, proc, inferredPath),
                    BuildExpression(rightShiftAssign.RHS, dmObject, proc, inferredPath));
            case DMASTXorAssign xorAssign:
                return new XorAssign(xorAssign.Location,
                    BuildExpression(xorAssign.LHS, dmObject, proc, inferredPath),
                    BuildExpression(xorAssign.RHS, dmObject, proc, inferredPath));
            case DMASTModulusAssign modulusAssign:
                return new ModulusAssign(modulusAssign.Location,
                    BuildExpression(modulusAssign.LHS, dmObject, proc, inferredPath),
                    BuildExpression(modulusAssign.RHS, dmObject, proc, inferredPath));
            case DMASTModulusModulusAssign modulusModulusAssign:
                var mmAssignLHS = BuildExpression(modulusModulusAssign.LHS, dmObject, proc, inferredPath);
                var mmAssignRHS = BuildExpression(modulusModulusAssign.RHS, dmObject, proc, mmAssignLHS.NestedPath);
                return new ModulusModulusAssign(modulusModulusAssign.Location, mmAssignLHS, mmAssignRHS);
            case DMASTLeftShift leftShift:
                return new LeftShift(leftShift.Location,
                    BuildExpression(leftShift.LHS, dmObject, proc, inferredPath),
                    BuildExpression(leftShift.RHS, dmObject, proc, inferredPath));
            case DMASTRightShift rightShift:
                return new RightShift(rightShift.Location,
                    BuildExpression(rightShift.LHS, dmObject, proc, inferredPath),
                    BuildExpression(rightShift.RHS, dmObject, proc, inferredPath));
            case DMASTBinaryAnd binaryAnd:
                return new BinaryAnd(binaryAnd.Location,
                    BuildExpression(binaryAnd.LHS, dmObject, proc, inferredPath),
                    BuildExpression(binaryAnd.RHS, dmObject, proc, inferredPath));
            case DMASTBinaryXor binaryXor:
                return new BinaryXor(binaryXor.Location,
                    BuildExpression(binaryXor.LHS, dmObject, proc, inferredPath),
                    BuildExpression(binaryXor.RHS, dmObject, proc, inferredPath));
            case DMASTBinaryOr binaryOr:
                return new BinaryOr(binaryOr.Location,
                    BuildExpression(binaryOr.LHS, dmObject, proc, inferredPath),
                    BuildExpression(binaryOr.RHS, dmObject, proc, inferredPath));
            case DMASTEquivalent equivalent:
                return new Equivalent(equivalent.Location,
                    BuildExpression(equivalent.LHS, dmObject, proc, inferredPath),
                    BuildExpression(equivalent.RHS, dmObject, proc, inferredPath));
            case DMASTNotEquivalent notEquivalent:
                return new NotEquivalent(notEquivalent.Location,
                    BuildExpression(notEquivalent.LHS, dmObject, proc, inferredPath),
                    BuildExpression(notEquivalent.RHS, dmObject, proc, inferredPath));
            case DMASTGreaterThan greaterThan:
                return new GreaterThan(greaterThan.Location,
                    BuildExpression(greaterThan.LHS, dmObject, proc, inferredPath),
                    BuildExpression(greaterThan.RHS, dmObject, proc, inferredPath));
            case DMASTGreaterThanOrEqual greaterThanOrEqual:
                return new GreaterThanOrEqual(greaterThanOrEqual.Location,
                    BuildExpression(greaterThanOrEqual.LHS, dmObject, proc, inferredPath),
                    BuildExpression(greaterThanOrEqual.RHS, dmObject, proc, inferredPath));
            case DMASTLessThan lessThan:
                return new LessThan(lessThan.Location,
                    BuildExpression(lessThan.LHS, dmObject, proc, inferredPath),
                    BuildExpression(lessThan.RHS, dmObject, proc, inferredPath));
            case DMASTLessThanOrEqual lessThanOrEqual:
                return new LessThanOrEqual(lessThanOrEqual.Location,
                    BuildExpression(lessThanOrEqual.LHS, dmObject, proc, inferredPath),
                    BuildExpression(lessThanOrEqual.RHS, dmObject, proc, inferredPath));
            case DMASTOr or:
                return new Or(or.Location,
                    BuildExpression(or.LHS, dmObject, proc, inferredPath),
                    BuildExpression(or.RHS, dmObject, proc, inferredPath));
            case DMASTAnd and:
                return new And(and.Location,
                    BuildExpression(and.LHS, dmObject, proc, inferredPath),
                    BuildExpression(and.RHS, dmObject, proc, inferredPath));
            case DMASTTernary ternary:
                return new Ternary(ternary.Location,
                    BuildExpression(ternary.A, dmObject, proc, inferredPath),
                    BuildExpression(ternary.B, dmObject, proc, inferredPath),
                    BuildExpression(ternary.C ?? new DMASTConstantNull(ternary.Location), dmObject, proc, inferredPath));
            case DMASTNewPath newPath:
                if (BuildExpression(newPath.Path, dmObject, proc, inferredPath) is not ConstantPath path)
                    return BadExpression(WarningCode.BadExpression, newPath.Path.Location,
                        "Expected a path expression");

                return new NewPath(Compiler, newPath.Location, path,
                    new ArgumentList(newPath.Location, dmObject, proc, newPath.Parameters, inferredPath));
            case DMASTNewExpr newExpr:
                return new New(Compiler, newExpr.Location,
                    BuildExpression(newExpr.Expression, dmObject, proc, inferredPath),
                    new ArgumentList(newExpr.Location, dmObject, proc, newExpr.Parameters, inferredPath));
            case DMASTNewInferred newInferred:
                if (inferredPath is null)
                    return BadExpression(WarningCode.BadExpression, newInferred.Location, "Could not infer a type");

                return new NewPath(Compiler, newInferred.Location, new ConstantPath(compiler, newInferred.Location, dmObject, inferredPath.Value),
                    new ArgumentList(newInferred.Location, dmObject, proc, newInferred.Parameters, inferredPath));
            case DMASTPreIncrement preIncrement:
                return new PreIncrement(preIncrement.Location, BuildExpression(preIncrement.Value, dmObject, proc, inferredPath));
            case DMASTPostIncrement postIncrement:
                return new PostIncrement(postIncrement.Location, BuildExpression(postIncrement.Value, dmObject, proc, inferredPath));
            case DMASTPreDecrement preDecrement:
                return new PreDecrement(preDecrement.Location, BuildExpression(preDecrement.Value, dmObject, proc, inferredPath));
            case DMASTPostDecrement postDecrement:
                return new PostDecrement(postDecrement.Location, BuildExpression(postDecrement.Value, dmObject, proc, inferredPath));
            case DMASTPointerRef pointerRef:
                return new PointerRef(pointerRef.Location, BuildExpression(pointerRef.Value, dmObject, proc, inferredPath));
            case DMASTPointerDeref pointerDeref:
                return new PointerDeref(pointerDeref.Location, BuildExpression(pointerDeref.Value, dmObject, proc, inferredPath));
            case DMASTGradient gradient:
                return new Gradient(Compiler, gradient.Location,
                    new ArgumentList(gradient.Location, dmObject, proc, gradient.Parameters));
            case DMASTRgb rgb:
                return new Rgb(Compiler, rgb.Location, new ArgumentList(rgb.Location, dmObject, proc, rgb.Parameters));
            case DMASTLocateCoordinates locateCoordinates:
                return new LocateCoordinates(Compiler, locateCoordinates.Location,
                    BuildExpression(locateCoordinates.X, dmObject, proc, inferredPath),
                    BuildExpression(locateCoordinates.Y, dmObject, proc, inferredPath),
                    BuildExpression(locateCoordinates.Z, dmObject, proc, inferredPath));
            case DMASTIsSaved isSaved:
                return new IsSaved(Compiler, isSaved.Location, BuildExpression(isSaved.Value, dmObject, proc, inferredPath));
            case DMASTIsType isType: {
                if (isType.RHS is DMASTIdentifier { Identifier: "__IMPLIED_TYPE__" }) {
                    var expr  = compiler.DMExpression.Create(dmObject, proc, isType.LHS, inferredPath);
                    if (expr.Path is null)
                        return BadExpression(WarningCode.BadExpression, isType.Location, "A type could not be inferred!");

                    return new IsTypeInferred(Compiler, isType.Location, expr, expr.Path.Value);
                }
                return new IsType(Compiler, isType.Location,
                    BuildExpression(isType.LHS, dmObject, proc, inferredPath),
                    BuildExpression(isType.RHS, dmObject, proc, inferredPath));
            }

            case DMASTIsNull isNull:
                return new IsNull(Compiler, isNull.Location, BuildExpression(isNull.Value, dmObject, proc, inferredPath));
            case DMASTLength length:
                return new Length(Compiler, length.Location, BuildExpression(length.Value, dmObject, proc, inferredPath));
            case DMASTGetStep getStep:
                return new GetStep(Compiler, getStep.Location,
                    BuildExpression(getStep.LHS, dmObject, proc, inferredPath),
                    BuildExpression(getStep.RHS, dmObject, proc, inferredPath));
            case DMASTGetDir getDir:
                return new GetDir(Compiler, getDir.Location,
                    BuildExpression(getDir.LHS, dmObject, proc, inferredPath),
                    BuildExpression(getDir.RHS, dmObject, proc, inferredPath));
            case DMASTProb prob:
                return new Prob(Compiler, prob.Location,
                    BuildExpression(prob.Value, dmObject, proc, inferredPath));
            case DMASTInitial initial:
                return new Initial(Compiler, initial.Location, BuildExpression(initial.Value, dmObject, proc, inferredPath));
            case DMASTNameof nameof:
                return BuildNameof(nameof, dmObject, proc, inferredPath);
            case DMASTExpressionIn expressionIn:
                var exprInLHS = BuildExpression(expressionIn.LHS, dmObject, proc, inferredPath);
                var exprInRHS = BuildExpression(expressionIn.RHS, dmObject, proc, inferredPath);
                if ((expressionIn.LHS is not DMASTExpressionWrapped && exprInLHS is UnaryOp or BinaryOp or Ternary) ||
                    (expressionIn.RHS is not DMASTExpressionWrapped && exprInRHS is BinaryOp or Ternary)) {
                    Compiler.Emit(WarningCode.AmbiguousInOrder, expressionIn.Location,
                        "Order of operations for \"in\" may not be what is expected. Use parentheses to be more explicit.");
                }

                return new In(expressionIn.Location, exprInLHS, exprInRHS);
            case DMASTExpressionInRange expressionInRange:
                return new InRange(expressionInRange.Location,
                    BuildExpression(expressionInRange.Value, dmObject, proc, inferredPath),
                    BuildExpression(expressionInRange.StartRange, dmObject, proc, inferredPath),
                    BuildExpression(expressionInRange.EndRange, dmObject, proc, inferredPath));
            case DMASTSin sin:
                return new Sin(Compiler, sin.Location, BuildExpression(sin.Value, dmObject, proc, inferredPath));
            case DMASTCos cos:
                return new Cos(Compiler, cos.Location, BuildExpression(cos.Value, dmObject, proc, inferredPath));
            case DMASTTan tan:
                return new Tan(Compiler, tan.Location, BuildExpression(tan.Value, dmObject, proc, inferredPath));
            case DMASTArcsin arcSin:
                return new ArcSin(Compiler, arcSin.Location, BuildExpression(arcSin.Value, dmObject, proc, inferredPath));
            case DMASTArccos arcCos:
                return new ArcCos(Compiler, arcCos.Location, BuildExpression(arcCos.Value, dmObject, proc, inferredPath));
            case DMASTArctan arcTan:
                return new ArcTan(Compiler, arcTan.Location, BuildExpression(arcTan.Value, dmObject, proc, inferredPath));
            case DMASTArctan2 arcTan2:
                return new ArcTan2(Compiler, arcTan2.Location,
                    BuildExpression(arcTan2.LHS, dmObject, proc, inferredPath),
                    BuildExpression(arcTan2.RHS, dmObject, proc, inferredPath));
            case DMASTSqrt sqrt:
                return new Sqrt(Compiler, sqrt.Location, BuildExpression(sqrt.Value, dmObject, proc, inferredPath));
            case DMASTAbs abs:
                return new Abs(Compiler, abs.Location, BuildExpression(abs.Value, dmObject, proc, inferredPath));

            case DMASTVarDeclExpression varDeclExpr:
                var declIdentifier = new DMASTIdentifier(expression.Location, varDeclExpr.DeclPath.Path.LastElement);
                return BuildIdentifier(declIdentifier, dmObject, proc);
            case DMASTVoid:
                return BadExpression(WarningCode.BadExpression, expression.Location, "Attempt to use a void expression");
        }

        throw new ArgumentException($"Invalid expression {expression}", nameof(expression));
    }

    private DMExpression BuildConstant(DMASTExpressionConstant constant, DMObject dmObject, DMProc proc) {
        switch (constant) {
            case DMASTConstantNull: return new Null(compiler, constant.Location);
            case DMASTConstantInteger constInt: return new Number(compiler, constant.Location, constInt.Value);
            case DMASTConstantFloat constFloat: return new Number(compiler, constant.Location, constFloat.Value);
            case DMASTConstantString constString: return new String(compiler, constant.Location, constString.Value);
            case DMASTConstantResource constResource: return new Resource(compiler, constant.Location, constResource.Path);
            case DMASTConstantPath constPath: return new ConstantPath(compiler, constant.Location, dmObject, constPath.Value.Path);
            case DMASTUpwardPathSearch upwardSearch:
                compiler.DMExpression.TryConstant(dmObject, proc, upwardSearch.Path, out var pathExpr);
                if (pathExpr is not ConstantPath expr)
                    return BadExpression(WarningCode.BadExpression, constant.Location,
                        $"Cannot do an upward path search on {pathExpr}");

                DreamPath path = expr.Value;
                DreamPath? foundPath = DMObjectTree.UpwardSearch(path, upwardSearch.Search.Path);
                if (foundPath == null)
                    return BadExpression(WarningCode.ItemDoesntExist, constant.Location,
                        $"Could not find path {path}.{upwardSearch.Search.Path}");

                return new ConstantPath(compiler, constant.Location, dmObject, foundPath.Value);
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
                expressions[i]  = compiler.DMExpression.Create(dmObject, proc, interpolatedValue, inferredPath);
            }
        }

        return new StringFormat(Compiler, stringFormat.Location, stringFormat.Value, expressions);
    }

    private DMExpression BuildIdentifier(DMASTIdentifier identifier, DMObject dmObject, DMProc proc, DreamPath? inferredPath = null) {
        var name = identifier.Identifier;

        switch (name) {
            case "src":
                return new Src(identifier.Location, dmObject.Path);
            case "usr":
                return new Usr(identifier.Location);
            case "args":
                return new Args(identifier.Location);
            case "__TYPE__":
                return new ProcOwnerType(Compiler, identifier.Location, dmObject);
            case "__IMPLIED_TYPE__":
                if (inferredPath == null)
                    return BadExpression(WarningCode.BadExpression, identifier.Location,
                        "__IMPLIED_TYPE__ cannot be used here, there is no type being implied");

                return new ConstantPath(compiler, identifier.Location, dmObject, inferredPath.Value);
            case "__PROC__": // The saner alternative to "....."
                return new ConstantProcReference(compiler, identifier.Location, proc);
            case "global":
                return new Global(identifier.Location);
            default: {
                if (CurrentScopeMode == ScopeMode.Normal) {
                    var localVar = proc?.GetLocalVariable(name);
                    if (localVar != null)
                        return new Local(identifier.Location, localVar);

                    var field = dmObject?.GetVariable(name);
                    if (field != null) {
                        return new Field(identifier.Location, field, field.ValType);
                    }
                }

                if (CurrentScopeMode != ScopeMode.FirstPassStatic) {
                    var globalId = proc?.GetGlobalVariableId(name) ?? dmObject?.GetGlobalVariableId(name);

                    if (globalId != null) {
                        var globalVar = DMObjectTree.Globals[globalId.Value];
                        var global = new GlobalField(identifier.Location, globalVar.Type, globalId.Value, globalVar.ValType);
                        return global;
                    }

                    var field = dmObject?.GetVariable(name);
                    if (field != null) {
                        if (field.IsConst)
                            return new Field(identifier.Location, field, field.ValType);

                        return BadExpression(WarningCode.BadExpression, identifier.Location,
                            "Var \"{name}\" cannot be used in this context");
                    }
                }

                throw new UnknownIdentifierException(identifier.Location, name);
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
                if (!DMObjectTree.TryGetGlobalProc(bIdentifier, out _))
                    return BadExpression(WarningCode.ItemDoesntExist, location,
                        $"No global proc named \"{bIdentifier}\" exists");

                var arguments = new ArgumentList(location, dmObject, proc, scopeIdentifier.CallArguments, inferredPath);
                return new ProcCall(location, new GlobalProc(location, bIdentifier), arguments, DMValueType.Anything);
            }

            // ::vars, special case
            if (bIdentifier == "vars")
                return new GlobalVars(location);

            // ::A, global var ref
            var globalId = DMObjectTree.Root.GetGlobalVariableId(bIdentifier);
            if (globalId == null)
                throw new UnknownIdentifierException(location, bIdentifier);

            var globalVar = DMObjectTree.Globals[globalId.Value];
            return new GlobalField(location,
                DMObjectTree.Globals[globalId.Value].Type,
                globalId.Value,
                globalVar.ValType);
        }

        // Other uses should wait until the scope operator pass
        if (!ScopeOperatorEnabled)
            throw new UnknownIdentifierException(location, bIdentifier);

        DMExpression? expression;

        // "type" and "parent_type" cannot resolve in a context but it's still valid with scope identifiers
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

                expression = new ConstantPath(compiler, location, dmObject, dmObject.Parent.Path);
            } else { // "type"
                expression = new ConstantPath(compiler, location, dmObject, dmObject.Path);
            }
        } else {
            expression  = compiler.DMExpression.Create(dmObject, proc, scopeIdentifier.Expression, inferredPath);
        }

        // A must have a type
        if (expression.Path == null)
            return BadExpression(WarningCode.BadExpression, expression.Location,
                $"Identifier \"{expression.GetNameof(dmObject)}\" does not have a type");

        var owner = DMObjectTree.GetDMObject(expression.Path.Value, createIfNonexistent: false);
        if (owner == null) {
            if (expression is ConstantPath path && path.TryResolvePath(out var pathInfo) &&
                pathInfo.Value.Type == ConstantPath.PathType.ProcReference) {
                if (bIdentifier == "name")
                    return new String(compiler, expression.Location, path.Path!.Value.LastElement!);

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

            var referencedProc = DMObjectTree.AllProcs[procs[^1]];
            return new ConstantProcReference(compiler, location, referencedProc);
        } else { // A::B
            var globalVarId = owner.GetGlobalVariableId(bIdentifier);
            if (globalVarId != null) {
                // B is a var.
                // This is the only case a ScopeIdentifier can be an LValue.
                var globalVar = DMObjectTree.Globals[globalVarId.Value];
                return new GlobalField(location, globalVar.Type, globalVarId.Value, globalVar.ValType);
            }

            var variable = owner.GetVariable(bIdentifier);
            if (variable == null)
                throw new UnknownIdentifierException(location, bIdentifier);

            return new ScopeReference(location, expression, bIdentifier, variable);
        }
    }

    private DMExpression BuildCallableProcIdentifier(DMASTCallableProcIdentifier procIdentifier, DMObject dmObject) {
        if (CurrentScopeMode is ScopeMode.Static or ScopeMode.FirstPassStatic)
            return new GlobalProc(procIdentifier.Location, procIdentifier.Identifier);
        if (dmObject.HasProc(procIdentifier.Identifier))
            return new Proc(procIdentifier.Location, procIdentifier.Identifier);
        if (DMObjectTree.TryGetGlobalProc(procIdentifier.Identifier, out _))
            return new GlobalProc(procIdentifier.Location, procIdentifier.Identifier);

        return BadExpression(WarningCode.ItemDoesntExist, procIdentifier.Location,
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

            var expr  = compiler.DMExpression.Create(dmObject, proc, procCall.Parameters[0].Value, inferredPath);
            return new Arglist(Compiler, procCall.Location, expr);
        }

        var target  = compiler.DMExpression.Create(dmObject, proc, (DMASTExpression)procCall.Callable, inferredPath);
        var args = new ArgumentList(procCall.Location, dmObject, proc, procCall.Parameters);
        if (target is Proc targetProc) { // GlobalProc handles returnType itself
            var returnType = targetProc.GetReturnType(dmObject);

            return new ProcCall(procCall.Location, target, args, returnType);
        }

        return new ProcCall(procCall.Location, target, args, DMValueType.Anything);
    }

    private DMExpression BuildAssign(DMASTAssign assign, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var lhs  = compiler.DMExpression.Create(dmObject, proc, assign.LHS, inferredPath);
        var rhs  = compiler.DMExpression.Create(dmObject, proc, assign.RHS, lhs.NestedPath);
        if(lhs.TryAsConstant(out _)) {
            Compiler.Emit(WarningCode.WriteToConstant, assign.LHS.Location, "Cannot write to const var");
        }

        return new Assignment(assign.Location, lhs, rhs);
    }

    private DMExpression BuildAssignInto(DMASTAssignInto assign, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var lhs  = compiler.DMExpression.Create(dmObject, proc, assign.LHS, inferredPath);
        var rhs  = compiler.DMExpression.Create(dmObject, proc, assign.RHS, lhs.NestedPath);
        if(lhs.TryAsConstant(out _)) {
            Compiler.Emit(WarningCode.WriteToConstant, assign.LHS.Location, "Cannot write to const var");
        }

        return new AssignmentInto(assign.Location, lhs, rhs);
    }

    private DMExpression BuildEqual(DMASTEqual equal, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var lhs  = compiler.DMExpression.Create(dmObject, proc, equal.LHS, inferredPath);
        var rhs  = compiler.DMExpression.Create(dmObject, proc, equal.RHS, inferredPath);

        // (x == null) can be changed to isnull(x) which compiles down to an opcode
        // TODO: Bytecode optimizations instead
        if (rhs is Null)
            return new IsNull(Compiler, equal.Location, lhs);

        return new Equal(equal.Location, lhs, rhs);
    }

    private DMExpression BuildNotEqual(DMASTNotEqual notEqual, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var lhs  = compiler.DMExpression.Create(dmObject, proc, notEqual.LHS, inferredPath);
        var rhs  = compiler.DMExpression.Create(dmObject, proc, notEqual.RHS, inferredPath);

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
        var expr  = compiler.DMExpression.Create(dmObject, proc, deref.Expression, inferredPath);
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
                        expr = new ProcCall(expr.Location, globalProc, argumentList, DMValueType.Anything);
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
                        if (prevPath == null) {
                            throw new UnknownIdentifierException(deref.Location, field);
                        }

                        DMObject? fromObject = DMObjectTree.GetDMObject(prevPath.Value, false);
                        if (fromObject == null)
                            return BadExpression(WarningCode.ItemDoesntExist, fieldOperation.Location,
                                $"Type {prevPath.Value} does not exist");

                        property = fromObject.GetVariable(field);
                        if (!fieldOperation.Safe && fromObject.IsSubtypeOf(DreamPath.Client)) {
                             Compiler.Emit(WarningCode.UnsafeClientAccess, deref.Location,
                                "Unsafe \"client\" access. Use the \"?.\" operator instead");
                        }

                        if (property == null && fromObject.GetGlobalVariableId(field) is { } globalId) {
                            property = DMObjectTree.Globals[globalId];

                            expr = new GlobalField(expr.Location, property.Type, globalId, property.ValType);

                            var newOperationCount = operations.Length - i - 1;
                            if (newOperationCount == 0) {
                                return expr;
                            }

                            if (property == null) {
                                throw new UnknownIdentifierException(deref.Location, field);
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
                        // var/type1/result = new /type2()[new()] changes the inferred new to "new /type1()"
                        // L[new()] = new() uses the type of L however
                        Index  = compiler.DMExpression.Create(dmObject, proc, indexOperation.Index, inferredPath ?? prevPath),
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
                        if (fromObject == null)
                            return BadExpression(WarningCode.ItemDoesntExist, callOperation.Location,
                                $"Type {prevPath.Value} does not exist");

                        if (!fromObject.HasProc(field))
                            return BadExpression(WarningCode.ItemDoesntExist, callOperation.Location,
                                $"Type {prevPath.Value} does not have a proc named \"{field}\"");
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
        var container = locate.Container != null ? compiler.DMExpression.Create(dmObject, proc, locate.Container, inferredPath) : null;

        if (locate.Expression == null) {
            if (inferredPath == null)
                return BadExpression(WarningCode.BadExpression, locate.Location, "inferred locate requires a type");

            return new LocateInferred(Compiler, locate.Location, inferredPath.Value, container);
        }

        var pathExpr  = compiler.DMExpression.Create(dmObject, proc, locate.Expression, inferredPath);
        return new Locate(Compiler, locate.Location, pathExpr, container);
    }

    private DMExpression BuildImplicitIsType(DMASTImplicitIsType isType, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var expr  = compiler.DMExpression.Create(dmObject, proc, isType.Value, inferredPath);

        if (expr.Path is null)
            return BadExpression(WarningCode.BadExpression, isType.Location, "An inferred istype requires a type!");

        return new IsTypeInferred(Compiler, isType.Location, expr, expr.Path.Value);
    }

    private DMExpression BuildList(DMASTList list, DMObject dmObject, DMProc proc) {
        (DMExpression? Key, DMExpression Value)[] values = Array.Empty<(DMExpression?, DMExpression)>();

        if (list.Values != null) {
            values = new (DMExpression?, DMExpression)[list.Values.Length];

            for (int i = 0; i < list.Values.Length; i++) {
                DMASTCallParameter value = list.Values[i];
                DMExpression? key = (value.Key != null) ? compiler.DMExpression.Create(dmObject, proc, value.Key) : null;
                DMExpression listValue  = compiler.DMExpression.Create(dmObject, proc, value.Value);

                values[i] = (key, listValue);
            }
        }

        return new List(Compiler, list.Location, values);
    }

    private DMExpression BuildDimensionalList(DMASTDimensionalList list, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var sizes = new DMExpression[list.Sizes.Count];
        for (int i = 0; i < sizes.Length; i++) {
            var sizeExpr  = compiler.DMExpression.Create(dmObject, proc, list.Sizes[i], inferredPath);

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

            expressions[i]  = compiler.DMExpression.Create(dmObject, proc, parameter.Value, inferredPath);
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

            expArr[i]  = compiler.DMExpression.Create(dmObject,proc, parameter.Value, inferredPath);
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

            arguments[i]  = compiler.DMExpression.Create(dmObject, proc, parameter.Value);
        }

        DMExpression? list = null;
        if (input.List != null) {
            list  = compiler.DMExpression.Create(dmObject, proc, input.List);

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
            DMExpression? weight = (pickValue.Weight != null) ? compiler.DMExpression.Create(dmObject, proc, pickValue.Weight) : null;
            DMExpression value  = compiler.DMExpression.Create(dmObject, proc, pickValue.Value);

            if (weight is Prob prob) // pick(prob(50);x, prob(200);y) format
                weight = prob.P;

            pickValues[i] = new Pick.PickValue(weight, value);
        }

        return new Pick(Compiler, pick.Location, pickValues);
    }

    private DMExpression BuildLog(DMASTLog log, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var expr  = compiler.DMExpression.Create(dmObject, proc, log.Expression, inferredPath);
        DMExpression? baseExpr = null;

        if (log.BaseExpression != null) {
            baseExpr  = compiler.DMExpression.Create(dmObject, proc, log.BaseExpression, inferredPath);
        }

        return new Log(Compiler, log.Location, expr, baseExpr);
    }

    private DMExpression BuildCall(DMASTCall call, DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
        var procArgs = new ArgumentList(call.Location, dmObject, proc, call.ProcParameters, inferredPath);

        switch (call.CallParameters.Length) {
            default:
                 Compiler.Emit(WarningCode.InvalidArgumentCount, call.Location, "Too many arguments for call()");
                goto case 2; // Fallthrough!
            case 2: {
                var a  = compiler.DMExpression.Create(dmObject, proc, call.CallParameters[0].Value, inferredPath);
                var b  = compiler.DMExpression.Create(dmObject, proc, call.CallParameters[1].Value, inferredPath);
                return new CallStatement(Compiler, call.Location, a, b, procArgs);
            }
            case 1: {
                var a  = compiler.DMExpression.Create(dmObject, proc, call.CallParameters[0].Value, inferredPath);
                return new CallStatement(Compiler, call.Location, a, procArgs);
            }
            case 0:
                 Compiler.Emit(WarningCode.InvalidArgumentCount, call.Location, "Not enough arguments for call()");
                return new CallStatement(Compiler, call.Location, new Null(compiler, Location.Internal), procArgs);
        }
    }

    /// <summary>
    /// Emits an error and returns a <see cref="BadExpression"/><br/>
    /// Common pattern, so here's a one-line helper
    /// </summary>
    private DMExpression BadExpression(WarningCode code, Location location, string errorMessage) {
         Compiler.Emit(code, location, errorMessage);
        return new BadExpression(Compiler, location);
    }
}
