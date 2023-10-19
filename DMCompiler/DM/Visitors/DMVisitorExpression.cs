using System;
using DMCompiler.DM.Expressions;
using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using Robust.Shared.Utility;

namespace DMCompiler.DM.Visitors {
    internal sealed class DMVisitorExpression : DMASTVisitor {
        public enum ScopeMode {
            // All in-scope procs and vars available
            Normal,

            // Only global vars and procs available
            Static,

            // Only global procs available
            FirstPassStatic
        }

        public static ScopeMode CurrentScopeMode = ScopeMode.Normal;
        public DMExpression Result { get; private set; }

        private readonly DMObject _dmObject;
        private readonly DMProc _proc;
        private readonly DreamPath? _inferredPath;

        internal DMVisitorExpression(DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
            _dmObject = dmObject;
            _proc = proc;
            _inferredPath = inferredPath;
        }

        public void VisitProcStatementExpression(DMASTProcStatementExpression statement) {
            statement.Expression.Visit(this);
        }

        public void VisitConstantNull(DMASTConstantNull constant) {
            Result = new Expressions.Null(constant.Location);
        }

        public void VisitConstantInteger(DMASTConstantInteger constant) {
            Result = new Expressions.Number(constant.Location, constant.Value);
        }

        public void VisitConstantFloat(DMASTConstantFloat constant) {
            Result = new Expressions.Number(constant.Location, constant.Value);
        }

        public void VisitConstantString(DMASTConstantString constant) {
            Result = new Expressions.String(constant.Location, constant.Value);
        }

        public void VisitConstantResource(DMASTConstantResource constant) {
            Result = new Expressions.Resource(constant.Location, constant.Path);
        }

        public void VisitConstantPath(DMASTConstantPath constant) {
            Result = new Expressions.Path(constant.Location, _dmObject, constant.Value.Path);
        }

        public void VisitUpwardPathSearch(DMASTUpwardPathSearch constant) {
            DMExpression.TryConstant(_dmObject, _proc, constant.Path, out var pathExpr);
            if (pathExpr is not Expressions.Path expr)
                throw new CompileErrorException(constant.Location, $"Cannot do an upward path search on {pathExpr}");

            DreamPath path = expr.Value;
            DreamPath? foundPath = DMObjectTree.UpwardSearch(path, constant.Search.Path);

            if (foundPath == null) {
                throw new CompileErrorException(constant.Location,$"Invalid path {path}.{constant.Search.Path}");
            }

            Result = new Expressions.Path(constant.Location, _dmObject, foundPath.Value);
        }

        public void VisitStringFormat(DMASTStringFormat stringFormat) {
            var expressions = new DMExpression[stringFormat.InterpolatedValues.Length];

            for (int i = 0; i < stringFormat.InterpolatedValues.Length; i++) {
                if (stringFormat.InterpolatedValues[i] is null) {
                    expressions[i] = new Expressions.Null(stringFormat.Location);
                } else {
                    expressions[i] = DMExpression.Create(_dmObject, _proc, stringFormat.InterpolatedValues[i], _inferredPath);
                }
            }

            Result = new Expressions.StringFormat(stringFormat.Location, stringFormat.Value, expressions);
        }

        public void VisitVoid(DMASTVoid voidNode) {
            DMCompiler.Emit(WarningCode.BadExpression, voidNode.Location, "Attempt to use a void expression");
            Result = new Expressions.Null(voidNode.Location);
        }

        public void VisitIdentifier(DMASTIdentifier identifier) {
            var name = identifier.Identifier;

            switch (name) {
                case "src":
                    Result = new Expressions.Src(identifier.Location, _dmObject.Path);
                    break;
                case "usr":
                    Result = new Expressions.Usr(identifier.Location);
                    break;
                case "args":
                    Result = new Expressions.Args(identifier.Location);
                    break;
                case "__TYPE__":
                    Result = new Expressions.ProcOwnerType(identifier.Location);
                    break;
                case "__PROC__": // The saner alternative to .....
                    Result = new Expressions.ProcType(identifier.Location);
                    break;
                case "global":
                    Result = new Expressions.Global(identifier.Location);
                    break;
                default: {
                    if (CurrentScopeMode == ScopeMode.Normal) {
                        DMProc.LocalVariable localVar = _proc?.GetLocalVariable(name);
                        if (localVar != null) {
                            Result = new Expressions.Local(identifier.Location, localVar);
                            return;
                        }

                        var field = _dmObject?.GetVariable(name);
                        if (field != null) {
                            Result = new Expressions.Field(identifier.Location, field);
                            return;
                        }
                    }

                    if (CurrentScopeMode != ScopeMode.FirstPassStatic) {
                        int? globalId = _proc?.GetGlobalVariableId(name) ?? _dmObject?.GetGlobalVariableId(name);

                        if (globalId != null) {
                            var global = new Expressions.GlobalField(identifier.Location, DMObjectTree.Globals[globalId.Value].Type, globalId.Value);

                            Result = global;
                            return;
                        }
                    }

                    throw new UnknownIdentifierException(identifier.Location, name);
                }
            }
        }

        public void VisitVarDeclExpression(DMASTVarDeclExpression declExpr) {
            VisitIdentifier( new DMASTIdentifier(declExpr.Location, declExpr.DeclPath.Path.LastElement) );
        }

        public void VisitGlobalIdentifier(DMASTGlobalIdentifier globalIdentifier) {
            string name = globalIdentifier.Identifier;

            if (CurrentScopeMode != ScopeMode.FirstPassStatic) {
                int? globalId = _dmObject?.GetGlobalVariableId(name);
                if (globalId != null) {
                    Result = new Expressions.GlobalField(globalIdentifier.Location,
                        DMObjectTree.Globals[globalId.Value].Type, globalId.Value);
                    return;
                } else if (name == "vars") {
                    Result = new Expressions.GlobalVars(globalIdentifier.Location);
                    return;
                }
            }

            throw new CompileErrorException(globalIdentifier.Location, $"Unknown global \"{name}\"");
        }

        public void VisitCallableSelf(DMASTCallableSelf self) {
            Result = new Expressions.ProcSelf(self.Location);
        }

        public void VisitCallableSuper(DMASTCallableSuper super) {
            Result = new Expressions.ProcSuper(super.Location);
        }

        public void VisitCallableProcIdentifier(DMASTCallableProcIdentifier procIdentifier) {
            if (CurrentScopeMode is ScopeMode.Static or ScopeMode.FirstPassStatic) {
                Result = new Expressions.GlobalProc(procIdentifier.Location, procIdentifier.Identifier);
            } else {
                if (_dmObject.HasProc(procIdentifier.Identifier)) {
                    Result = new Expressions.Proc(procIdentifier.Location, procIdentifier.Identifier);
                } else if (DMObjectTree.TryGetGlobalProc(procIdentifier.Identifier, out _)) {
                    Result = new Expressions.GlobalProc(procIdentifier.Location, procIdentifier.Identifier);
                } else {
                    throw new CompileErrorException(procIdentifier.Location, $"Type {_dmObject.Path} does not have a proc named \"{procIdentifier.Identifier}\"");
                }
            }
        }

        public void VisitCallableGlobalProc(DMASTCallableGlobalProc globalProc) {
            Result = new Expressions.GlobalProc(globalProc.Location, globalProc.Identifier);
        }

        public void VisitProcCall(DMASTProcCall procCall) {
            // arglist hack
            if (procCall.Callable is DMASTCallableProcIdentifier ident) {
                if (ident.Identifier == "arglist") {
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

                    var expr = DMExpression.Create(_dmObject, _proc, procCall.Parameters[0].Value, _inferredPath);
                    Result = new Expressions.Arglist(procCall.Location, expr);
                    return;
                }
            }

            var target = DMExpression.Create(_dmObject, _proc, (DMASTExpression)procCall.Callable, _inferredPath);
            var args = new ArgumentList(procCall.Location, _dmObject, _proc, procCall.Parameters);
            Result = new Expressions.ProcCall(procCall.Location, target, args);
        }

        public void VisitAssign(DMASTAssign assign) {
            var lhs = DMExpression.Create(_dmObject, _proc, assign.Expression, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, assign.Value, lhs.NestedPath);
            if(lhs.TryAsConstant(out _)) {
                DMCompiler.Emit(WarningCode.WriteToConstant, assign.Expression.Location, "Cannot write to const var");
            }
            Result = new Expressions.Assignment(assign.Location, lhs, rhs);
        }

        public void VisitAssignInto(DMASTAssignInto assign) {
            var lhs = DMExpression.Create(_dmObject, _proc, assign.Expression, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, assign.Value, lhs.NestedPath);
            if(lhs.TryAsConstant(out _)) {
                DMCompiler.Emit(WarningCode.WriteToConstant, assign.Expression.Location, "Cannot write to const var");
            }
            Result = new Expressions.AssignmentInto(assign.Location, lhs, rhs);
        }

        public void VisitNegate(DMASTNegate negate) {
            var expr = DMExpression.Create(_dmObject, _proc, negate.Expression, _inferredPath);
            Result = new Expressions.Negate(negate.Location, expr);
        }

        public void VisitNot(DMASTNot not) {
            var expr = DMExpression.Create(_dmObject, _proc, not.Expression, _inferredPath);
            Result = new Expressions.Not(not.Location, expr);
        }

        public void VisitBinaryNot(DMASTBinaryNot binaryNot) {
            var expr = DMExpression.Create(_dmObject, _proc, binaryNot.Value, _inferredPath);
            Result = new Expressions.BinaryNot(binaryNot.Location, expr);
        }

        public void VisitAdd(DMASTAdd add) {
            var lhs = DMExpression.Create(_dmObject, _proc, add.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, add.B, _inferredPath);
            Result = new Expressions.Add(add.Location, lhs, rhs);
        }

        public void VisitSubtract(DMASTSubtract subtract) {
            var lhs = DMExpression.Create(_dmObject, _proc, subtract.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, subtract.B, _inferredPath);
            Result = new Expressions.Subtract(subtract.Location, lhs, rhs);
        }

        public void VisitMultiply(DMASTMultiply multiply) {
            var lhs = DMExpression.Create(_dmObject, _proc, multiply.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, multiply.B, _inferredPath);
            Result = new Expressions.Multiply(multiply.Location, lhs, rhs);
        }

        public void VisitDivide(DMASTDivide divide) {
            var lhs = DMExpression.Create(_dmObject, _proc, divide.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, divide.B, _inferredPath);
            Result = new Expressions.Divide(divide.Location, lhs, rhs);
        }

        public void VisitModulus(DMASTModulus modulus) {
            var lhs = DMExpression.Create(_dmObject, _proc, modulus.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, modulus.B, _inferredPath);
            Result = new Expressions.Modulo(modulus.Location, lhs, rhs);
        }

        public void VisitModulusModulus(DMASTModulusModulus modulusModulus) {
            var lhs = DMExpression.Create(_dmObject, _proc, modulusModulus.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, modulusModulus.B, _inferredPath);
            Result = new Expressions.ModuloModulo(modulusModulus.Location, lhs, rhs);
        }

        public void VisitPower(DMASTPower power) {
            var lhs = DMExpression.Create(_dmObject, _proc, power.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, power.B, _inferredPath);
            Result = new Expressions.Power(power.Location, lhs, rhs);
        }

        public void VisitAppend(DMASTAppend append) {
            var lhs = DMExpression.Create(_dmObject, _proc, append.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, append.B, _inferredPath);
            Result = new Expressions.Append(append.Location, lhs, rhs);
        }

        public void VisitCombine(DMASTCombine combine) {
            var lhs = DMExpression.Create(_dmObject, _proc, combine.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, combine.B, _inferredPath);
            Result = new Expressions.Combine(combine.Location, lhs, rhs);
        }

        public void VisitRemove(DMASTRemove remove) {
            var lhs = DMExpression.Create(_dmObject, _proc, remove.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, remove.B, _inferredPath);
            Result = new Expressions.Remove(remove.Location, lhs, rhs);
        }

        public void VisitMask(DMASTMask mask) {
            var lhs = DMExpression.Create(_dmObject, _proc, mask.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, mask.B, _inferredPath);
            Result = new Expressions.Mask(mask.Location, lhs, rhs);
        }

        public void VisitLogicalAndAssign(DMASTLogicalAndAssign land) {
            var lhs = DMExpression.Create(_dmObject, _proc, land.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, land.B, lhs.NestedPath);
            Result = new Expressions.LogicalAndAssign(land.Location, lhs, rhs);
        }
        public void VisitLogicalOrAssign(DMASTLogicalOrAssign lor) {
            var lhs = DMExpression.Create(_dmObject, _proc, lor.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, lor.B, lhs.NestedPath);
            Result = new Expressions.LogicalOrAssign(lor.Location, lhs, rhs);
        }

        public void VisitMultiplyAssign(DMASTMultiplyAssign multiplyAssign) {
            var lhs = DMExpression.Create(_dmObject, _proc, multiplyAssign.A);
            var rhs = DMExpression.Create(_dmObject, _proc, multiplyAssign.B);
            Result = new Expressions.MultiplyAssign(multiplyAssign.Location, lhs, rhs);
        }

        public void VisitDivideAssign(DMASTDivideAssign divideAssign) {
            var lhs = DMExpression.Create(_dmObject, _proc, divideAssign.A);
            var rhs = DMExpression.Create(_dmObject, _proc, divideAssign.B);
            Result = new Expressions.DivideAssign(divideAssign.Location, lhs, rhs);
        }

        public void VisitLeftShiftAssign(DMASTLeftShiftAssign leftShiftAssign) {
            var lhs = DMExpression.Create(_dmObject, _proc, leftShiftAssign.A);
            var rhs = DMExpression.Create(_dmObject, _proc, leftShiftAssign.B);
            Result = new Expressions.LeftShiftAssign(leftShiftAssign.Location, lhs, rhs);
        }

        public void VisitRightShiftAssign(DMASTRightShiftAssign rightShiftAssign) {
            var lhs = DMExpression.Create(_dmObject, _proc, rightShiftAssign.A);
            var rhs = DMExpression.Create(_dmObject, _proc, rightShiftAssign.B);
            Result = new Expressions.RightShiftAssign(rightShiftAssign.Location, lhs, rhs);
        }

        public void VisitXorAssign(DMASTXorAssign xorAssign) {
            var lhs = DMExpression.Create(_dmObject, _proc, xorAssign.A);
            var rhs = DMExpression.Create(_dmObject, _proc, xorAssign.B);
            Result = new Expressions.XorAssign(xorAssign.Location, lhs, rhs);
        }

        public void VisitModulusAssign(DMASTModulusAssign modulusAssign) {
            var lhs = DMExpression.Create(_dmObject, _proc, modulusAssign.A);
            var rhs = DMExpression.Create(_dmObject, _proc, modulusAssign.B);
            Result = new Expressions.ModulusAssign(modulusAssign.Location, lhs, rhs);
        }

        public void VisitModulusModulusAssign(DMASTModulusModulusAssign modulusModulusAssign) {
            var lhs = DMExpression.Create(_dmObject, _proc, modulusModulusAssign.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, modulusModulusAssign.B, lhs.NestedPath);
            Result = new Expressions.ModulusModulusAssign(modulusModulusAssign.Location, lhs, rhs);
        }

        public void VisitLeftShift(DMASTLeftShift leftShift) {
            var lhs = DMExpression.Create(_dmObject, _proc, leftShift.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, leftShift.B, _inferredPath);
            Result = new Expressions.LeftShift(leftShift.Location, lhs, rhs);
        }

        public void VisitRightShift(DMASTRightShift rightShift) {
            var lhs = DMExpression.Create(_dmObject, _proc, rightShift.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, rightShift.B, _inferredPath);
            Result = new Expressions.RightShift(rightShift.Location, lhs, rhs);
        }

        public void VisitBinaryAnd(DMASTBinaryAnd binaryAnd) {
            var lhs = DMExpression.Create(_dmObject, _proc, binaryAnd.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, binaryAnd.B, _inferredPath);
            Result = new Expressions.BinaryAnd(binaryAnd.Location, lhs, rhs);
        }

        public void VisitBinaryXor(DMASTBinaryXor binaryXor) {
            var lhs = DMExpression.Create(_dmObject, _proc, binaryXor.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, binaryXor.B, _inferredPath);
            Result = new Expressions.BinaryXor(binaryXor.Location, lhs, rhs);
        }

        public void VisitBinaryOr(DMASTBinaryOr binaryOr) {
            var lhs = DMExpression.Create(_dmObject, _proc, binaryOr.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, binaryOr.B, _inferredPath);
            Result = new Expressions.BinaryOr(binaryOr.Location, lhs, rhs);
        }

        public void VisitEqual(DMASTEqual equal) {
            var lhs = DMExpression.Create(_dmObject, _proc, equal.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, equal.B, _inferredPath);

            // (x == null) can be changed to isnull(x) which compiles down to an opcode
            // TODO: Bytecode optimizations instead
            if (rhs is Null) {
                Result = new IsNull(equal.Location, lhs);

                return;
            }

            Result = new Equal(equal.Location, lhs, rhs);
        }

        public void VisitNotEqual(DMASTNotEqual notEqual) {
            var lhs = DMExpression.Create(_dmObject, _proc, notEqual.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, notEqual.B, _inferredPath);

            // (x != null) can be changed to !isnull(x) which compiles down to two opcodes
            // TODO: Bytecode optimizations instead
            if (rhs is Null) {
                Result = new Not(notEqual.Location, new IsNull(notEqual.Location, lhs));

                return;
            }

            Result = new NotEqual(notEqual.Location, lhs, rhs);
        }

        public void VisitEquivalent(DMASTEquivalent equivalent) {
            var lhs = DMExpression.Create(_dmObject, _proc, equivalent.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, equivalent.B, _inferredPath);
            Result = new Expressions.Equivalent(equivalent.Location, lhs, rhs);
        }

        public void VisitNotEquivalent(DMASTNotEquivalent notEquivalent) {
            var lhs = DMExpression.Create(_dmObject, _proc, notEquivalent.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, notEquivalent.B, _inferredPath);
            Result = new Expressions.NotEquivalent(notEquivalent.Location, lhs, rhs);
        }

        public void VisitGreaterThan(DMASTGreaterThan greaterThan) {
            var lhs = DMExpression.Create(_dmObject, _proc, greaterThan.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, greaterThan.B, _inferredPath);
            Result = new Expressions.GreaterThan(greaterThan.Location, lhs, rhs);
        }

        public void VisitGreaterThanOrEqual(DMASTGreaterThanOrEqual greaterThanOrEqual) {
            var lhs = DMExpression.Create(_dmObject, _proc, greaterThanOrEqual.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, greaterThanOrEqual.B, _inferredPath);
            Result = new Expressions.GreaterThanOrEqual(greaterThanOrEqual.Location, lhs, rhs);
        }

        public void VisitLessThan(DMASTLessThan lessThan) {
            var lhs = DMExpression.Create(_dmObject, _proc, lessThan.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, lessThan.B, _inferredPath);
            Result = new Expressions.LessThan(lessThan.Location, lhs, rhs);
        }

        public void VisitLessThanOrEqual(DMASTLessThanOrEqual lessThanOrEqual) {
            var lhs = DMExpression.Create(_dmObject, _proc, lessThanOrEqual.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, lessThanOrEqual.B, _inferredPath);
            Result = new Expressions.LessThanOrEqual(lessThanOrEqual.Location, lhs, rhs);
        }

        public void VisitOr(DMASTOr or) {
            var lhs = DMExpression.Create(_dmObject, _proc, or.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, or.B, _inferredPath);
            Result = new Expressions.Or(or.Location, lhs, rhs);
        }

        public void VisitAnd(DMASTAnd and) {
            var lhs = DMExpression.Create(_dmObject, _proc, and.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, and.B, _inferredPath);
            Result = new Expressions.And(and.Location, lhs, rhs);
        }

        public void VisitTernary(DMASTTernary ternary) {
            var a = DMExpression.Create(_dmObject, _proc, ternary.A, _inferredPath);
            var b = DMExpression.Create(_dmObject, _proc, ternary.B, _inferredPath);
            var c = DMExpression.Create(_dmObject, _proc, ternary.C ?? new DMASTConstantNull(ternary.Location), _inferredPath);
            Result = new Expressions.Ternary(ternary.Location, a, b, c);
        }

        public void VisitDereference(DMASTDereference deref) {
            var astOperations = deref.Operations;

            // The base expression and list of operations to perform on it
            // These may be redefined if we encounter a global access mid-operation
            var expr = DMExpression.Create(_dmObject, _proc, deref.Expression, _inferredPath);
            var operations = new Dereference.Operation[deref.Operations.Length];
            int astOperationOffset = 0;

            static bool IsFuzzy(DMExpression expr) {
                switch (expr) {
                    case Dereference when expr.Path == null:
                    case ProcCall when expr.Path == null:
                    case New when expr.Path == null:
                    case List:
                    case Ternary:
                    case BinaryAnd:
                    case IsNull:
                    case Length:
                    case GetStep:
                    case GetDir:
                        return true;
                    default: return false;
                }
            }

            // Path of the previous operation that was iterated over (starting as the base expression)
            DreamPath? prevPath = expr.Path;
            bool pathIsFuzzy = IsFuzzy(expr);

            // Special behaviour for `global.x`, `global.vars`, and `global.f()`
            if (expr is Expressions.Global) {
                ref DMASTDereference.Operation firstOperation = ref astOperations[0];

                if (firstOperation.Kind == DMASTDereference.OperationKind.Field && firstOperation.Identifier.Identifier == "vars") {
                    // `global.vars`
                    expr = new GlobalVars(expr.Location);

                    var newOperationCount = operations.Length - 1;
                    if (newOperationCount == 0) {
                        Result = expr;
                        return;
                    }

                    operations = new Dereference.Operation[newOperationCount];
                    astOperationOffset = 1;

                    prevPath = null;
                    pathIsFuzzy = true;
                } else if (firstOperation.Kind == DMASTDereference.OperationKind.Field) {
                    // `global.x`

                    var globalId = _dmObject.GetGlobalVariableId(firstOperation.Identifier.Identifier);
                    if (globalId == null) {
                        throw new UnknownIdentifierException(deref.Location, $"global.{firstOperation.Identifier.Identifier}");
                    }

                    var property = DMObjectTree.Globals[globalId.Value];
                    expr = new GlobalField(expr.Location, property.Type, globalId.Value);

                    var newOperationCount = operations.Length - 1;
                    if (newOperationCount == 0) {
                        Result = expr;
                        return;
                    }

                    operations = new Dereference.Operation[newOperationCount];
                    astOperationOffset = 1;

                    prevPath = property.Type;
                    pathIsFuzzy = false;
                } else if (firstOperation.Kind == DMASTDereference.OperationKind.Call) {
                    // `global.f()`
                    ArgumentList argumentList = new(deref.Expression.Location, _dmObject, _proc, firstOperation.Parameters, null);

                    var proc = new Expressions.GlobalProc(expr.Location, firstOperation.Identifier.Identifier);
                    expr = new Expressions.ProcCall(expr.Location, proc, argumentList);

                    var newOperationCount = operations.Length - 1;
                    if (newOperationCount == 0) {
                        Result = expr;
                        return;
                    }

                    operations = new Dereference.Operation[newOperationCount];
                    astOperationOffset = 1;

                    prevPath = null;
                    pathIsFuzzy = true;
                } else {
                    throw new CompileErrorException(deref.Location, $"Invalid dereference operation performed on `global`");
                }
            }

            for (int i = 0; i < operations.Length; i++) {
                ref DMASTDereference.Operation astOperation = ref astOperations[i + astOperationOffset];
                ref Dereference.Operation operation = ref operations[i];

                operation.Kind = astOperation.Kind;

                // If the last operation evaluated as an ambiguous type, we force the next operation to be a search
                if (pathIsFuzzy) {
                    operation.Kind = operation.Kind switch {
                        DMASTDereference.OperationKind.Invalid => throw new InvalidOperationException(),

                        DMASTDereference.OperationKind.Field => DMASTDereference.OperationKind.FieldSearch,
                        DMASTDereference.OperationKind.FieldSafe => DMASTDereference.OperationKind.FieldSafeSearch,
                        DMASTDereference.OperationKind.FieldSearch => DMASTDereference.OperationKind.FieldSearch,
                        DMASTDereference.OperationKind.FieldSafeSearch => DMASTDereference.OperationKind.FieldSafeSearch,
                        DMASTDereference.OperationKind.Call => DMASTDereference.OperationKind.CallSearch,
                        DMASTDereference.OperationKind.CallSafe => DMASTDereference.OperationKind.CallSafeSearch,
                        DMASTDereference.OperationKind.CallSearch => DMASTDereference.OperationKind.CallSearch,
                        DMASTDereference.OperationKind.CallSafeSearch => DMASTDereference.OperationKind.CallSafeSearch,

                        // Indexes are always fuzzy anyway!
                        DMASTDereference.OperationKind.Index => DMASTDereference.OperationKind.Index,
                        DMASTDereference.OperationKind.IndexSafe => DMASTDereference.OperationKind.IndexSafe,

                        _ => throw new InvalidOperationException(),
                    };
                }
                switch (operation.Kind) {
                    case DMASTDereference.OperationKind.Field:
                    case DMASTDereference.OperationKind.FieldSafe: {
                            string field = astOperation.Identifier.Identifier;

                            if (prevPath == null) {
                                throw new UnknownIdentifierException(deref.Location, field);
                            }

                            DMObject dmObject = DMObjectTree.GetDMObject(prevPath.Value, false);
                            if (dmObject == null) {
                                throw new CompileErrorException(deref.Location, $"Type {prevPath.Value} does not exist");
                            }

                            DMVariable property = dmObject.GetVariable(field);
                            if (property != null) {
                                operation.Identifier = field;
                                operation.GlobalId = null;
                                operation.Path = property.Type;
                                if (operation.Kind == DMASTDereference.OperationKind.Field &&
                                    dmObject.IsSubtypeOf(DreamPath.Client)) {
                                    DMCompiler.Emit(WarningCode.UnsafeClientAccess, deref.Location,"Unsafe \"client\" access. Use the \"?.\" operator instead");
                                }
                            } else {
                                var globalId = dmObject.GetGlobalVariableId(field);
                                if (globalId != null) {
                                    property = DMObjectTree.Globals[globalId.Value];

                                    expr = new GlobalField(expr.Location, property.Type, globalId.Value);

                                    var newOperationCount = operations.Length - i - 1;
                                    if (newOperationCount == 0) {
                                        Result = expr;
                                        return;
                                    }

                                    operations = new Dereference.Operation[newOperationCount];
                                    astOperationOffset = i + 1;
                                    i = -1;
                                }
                            }

                            if (property == null) {
                                throw new UnknownIdentifierException(deref.Location, field);
                            }

                            if ((property.ValType & DMValueType.Unimplemented) == DMValueType.Unimplemented) {
                                DMCompiler.UnimplementedWarning(deref.Location, $"{prevPath}.{field} is not implemented and will have unexpected behavior");
                            }

                            prevPath = property.Type;
                            pathIsFuzzy = false;
                        }
                        break;

                    case DMASTDereference.OperationKind.FieldSearch:
                    case DMASTDereference.OperationKind.FieldSafeSearch:
                        // TODO: im pretty sure types should be inferred if a field with their name only exists in a single place, sounds cursed though
                        operation.Identifier = astOperation.Identifier.Identifier;
                        operation.GlobalId = null;
                        operation.Path = null;
                        prevPath = null;
                        pathIsFuzzy = true;
                        break;

                    case DMASTDereference.OperationKind.Index:
                    case DMASTDereference.OperationKind.IndexSafe:
                        // Passing the path here is cursed, but one of the tests seems to suggest we want that?
                        operation.Index = DMExpression.Create(_dmObject, _proc, astOperation.Index, prevPath);
                        operation.Path = prevPath;
                        prevPath = null;
                        pathIsFuzzy = true;
                        break;

                    case DMASTDereference.OperationKind.Call:
                    case DMASTDereference.OperationKind.CallSafe: {
                            string field = astOperation.Identifier.Identifier;
                            ArgumentList argumentList = new(deref.Expression.Location, _dmObject, _proc, astOperation.Parameters, null);

                            if (prevPath == null) {
                                throw new UnknownIdentifierException(deref.Location, field);
                            }

                            DMObject dmObject = DMObjectTree.GetDMObject(prevPath.Value, false);
                            if (dmObject == null) {
                                throw new CompileErrorException(deref.Location, $"Type {prevPath.Value} does not exist");
                            }

                            if (!dmObject.HasProc(field)) {
                                throw new CompileErrorException(deref.Location, $"Type {prevPath.Value} does not have a proc named \"{field}\"");
                            }

                            operation.Identifier = astOperation.Identifier.Identifier;
                            operation.Parameters = argumentList;
                            operation.Path = null;
                            prevPath = null;
                            pathIsFuzzy = true;
                        }
                        break;

                    case DMASTDereference.OperationKind.CallSearch:
                    case DMASTDereference.OperationKind.CallSafeSearch:
                        operation.Identifier = astOperation.Identifier.Identifier;
                        operation.Parameters = new ArgumentList(deref.Expression.Location, _dmObject, _proc, astOperation.Parameters, null);
                        operation.Path = null;
                        prevPath = null;
                        pathIsFuzzy = true;
                        break;

                    default:
                        throw new InvalidOperationException("unhandled deref operation kind");
                }
            }

            // The final value in prevPath is our expression's path!

            Result = new Expressions.Dereference(deref.Location, prevPath, expr, operations);
        }

        public void VisitNewPath(DMASTNewPath newPath) {
            var args = new ArgumentList(newPath.Location, _dmObject, _proc, newPath.Parameters, _inferredPath);
            Result = new Expressions.NewPath(newPath.Location, newPath.Path.Path, args);
        }

        public void VisitNewExpr(DMASTNewExpr newExpr) {
            var expr = DMExpression.Create(_dmObject, _proc, newExpr.Expression, _inferredPath);
            var args = new ArgumentList(newExpr.Location, _dmObject, _proc, newExpr.Parameters, _inferredPath);
            Result = new Expressions.New(newExpr.Location, expr, args);
        }

        public void VisitNewInferred(DMASTNewInferred newInferred) {
            if (_inferredPath is null) {
                throw new CompileErrorException(newInferred.Location, "An inferred new requires a type!");
            }

            var args = new ArgumentList(newInferred.Location, _dmObject, _proc, newInferred.Parameters, _inferredPath);
            Result = new Expressions.NewPath(newInferred.Location, _inferredPath.Value, args);
        }

        public void VisitPreIncrement(DMASTPreIncrement preIncrement) {
            var expr = DMExpression.Create(_dmObject, _proc, preIncrement.Expression, _inferredPath);
            Result = new Expressions.PreIncrement(preIncrement.Location, expr);
        }

        public void VisitPostIncrement(DMASTPostIncrement postIncrement) {
            var expr = DMExpression.Create(_dmObject, _proc, postIncrement.Expression, _inferredPath);
            Result = new Expressions.PostIncrement(postIncrement.Location, expr);
        }

        public void VisitPreDecrement(DMASTPreDecrement preDecrement) {
            var expr = DMExpression.Create(_dmObject, _proc, preDecrement.Expression, _inferredPath);
            Result = new Expressions.PreDecrement(preDecrement.Location, expr);
        }

        public void VisitPostDecrement(DMASTPostDecrement postDecrement) {
            var expr = DMExpression.Create(_dmObject, _proc, postDecrement.Expression, _inferredPath);
            Result = new Expressions.PostDecrement(postDecrement.Location, expr);
        }

        public void VisitLocate(DMASTLocate locate) {
            var container = locate.Container != null ? DMExpression.Create(_dmObject, _proc, locate.Container, _inferredPath) : null;

            if (locate.Expression == null) {
                if (_inferredPath == null) {
                    throw new CompileErrorException(locate.Location, "inferred locate requires a type");
                }

                Result = new Expressions.LocateInferred(locate.Location, _inferredPath.Value, container);
                return;
            }

            var pathExpr = DMExpression.Create(_dmObject, _proc, locate.Expression, _inferredPath);
            Result = new Expressions.Locate(locate.Location, pathExpr, container);
        }

        public void VisitGradient(DMASTGradient gradient) {
            var args = new ArgumentList(gradient.Location, _dmObject, _proc, gradient.Parameters);

            Result = new Gradient(gradient.Location, args);
        }

        public void VisitLocateCoordinates(DMASTLocateCoordinates locateCoordinates) {
            var x = DMExpression.Create(_dmObject, _proc, locateCoordinates.X, _inferredPath);
            var y = DMExpression.Create(_dmObject, _proc, locateCoordinates.Y, _inferredPath);
            var z = DMExpression.Create(_dmObject, _proc, locateCoordinates.Z, _inferredPath);
            Result = new Expressions.LocateCoordinates(locateCoordinates.Location, x, y, z);
        }

        public void VisitIsSaved(DMASTIsSaved isSaved) {
            var expr = DMExpression.Create(_dmObject, _proc, isSaved.Expression, _inferredPath);
            Result = new Expressions.IsSaved(isSaved.Location, expr);
        }

        public void VisitIsType(DMASTIsType isType) {
            var expr = DMExpression.Create(_dmObject, _proc, isType.Value, _inferredPath);
            var path = DMExpression.Create(_dmObject, _proc, isType.Type, _inferredPath);
            Result = new Expressions.IsType(isType.Location, expr, path);
        }

        public void VisitImplicitIsType(DMASTImplicitIsType isType) {
            var expr = DMExpression.Create(_dmObject, _proc, isType.Value, _inferredPath);

            if (expr.Path is null) {
                throw new CompileErrorException(isType.Location,"An inferred istype requires a type!");
            }

            Result = new Expressions.IsTypeInferred(isType.Location, expr, expr.Path.Value);
        }

        public void VisitIsNull(DMASTIsNull isNull) {
            var value = DMExpression.Create(_dmObject, _proc, isNull.Value, _inferredPath);

            Result = new IsNull(isNull.Location, value);
        }

        public void VisitLength(DMASTLength length) {
            var value = DMExpression.Create(_dmObject, _proc, length.Value, _inferredPath);

            Result = new Length(length.Location, value);
        }

        public void VisitGetStep(DMASTGetStep getStep) {
            var refExpression = DMExpression.Create(_dmObject, _proc, getStep.Ref, _inferredPath);
            var dirExpression = DMExpression.Create(_dmObject, _proc, getStep.Dir, _inferredPath);

            Result = new GetStep(getStep.Location, refExpression, dirExpression);
        }

        public void VisitGetDir(DMASTGetDir getDir) {
            var loc1 = DMExpression.Create(_dmObject, _proc, getDir.Loc1, _inferredPath);
            var loc2 = DMExpression.Create(_dmObject, _proc, getDir.Loc2, _inferredPath);

            Result = new GetDir(getDir.Location, loc1, loc2);
        }

        public void VisitList(DMASTList list) {
            (DMExpression? Key, DMExpression Value)[] values = Array.Empty<(DMExpression?, DMExpression)>();

            if (list.Values != null) {
                values = new (DMExpression?, DMExpression)[list.Values.Length];

                for (int i = 0; i < list.Values.Length; i++) {
                    DMASTCallParameter value = list.Values[i];
                    DMExpression? key = (value.Key != null) ? DMExpression.Create(_dmObject, _proc, value.Key) : null;
                    DMExpression listValue = DMExpression.Create(_dmObject, _proc, value.Value);

                    values[i] = (key, listValue);
                }
            }

            Result = new Expressions.List(list.Location, values);
        }

        public void VisitDimensionalList(DMASTDimensionalList list) {
            var sizes = new DMExpression[list.Sizes.Count];
            for (int i = 0; i < sizes.Length; i++) {
                var sizeExpr = DMExpression.Create(_dmObject, _proc, list.Sizes[i], _inferredPath);

                sizes[i] = sizeExpr;
            }

            Result = new DimensionalList(list.Location, sizes);
        }

        public void VisitNewList(DMASTNewList newList) {
            DMExpression[] expressions = new DMExpression[newList.Parameters.Length];

            for (int i = 0; i < newList.Parameters.Length; i++) {
                DMASTCallParameter parameter = newList.Parameters[i];
                if (parameter.Key != null) throw new CompileErrorException(newList.Location,"newlist() does not take named arguments");

                expressions[i] = DMExpression.Create(_dmObject, _proc, parameter.Value, _inferredPath);
            }

            Result = new Expressions.NewList(newList.Location, expressions);
        }

        public void VisitAddText(DMASTAddText addText) {
            if (addText.Parameters.Length < 2) throw new CompileErrorException(addText.Location, "Invalid addtext() parameter count; expected 2 or more arguments");
            DMExpression[] exp_arr = new DMExpression[addText.Parameters.Length];
            for (int i = 0; i < exp_arr.Length; i++)
            {
                DMASTCallParameter parameter = addText.Parameters[i];
                if(parameter.Key != null)
                    DMCompiler.Emit(WarningCode.TooManyArguments, parameter.Location, "addtext() does not take named arguments");
                exp_arr[i] = DMExpression.Create(_dmObject,_proc, parameter.Value, _inferredPath);
            }
            Result = new Expressions.AddText(addText.Location, exp_arr);
        }

        public void VisitProb(DMASTProb prob) {
            DMExpression p = DMExpression.Create(_dmObject, _proc, prob.P);

            Result = new Expressions.Prob(prob.Location, p);
        }

        public void VisitInput(DMASTInput input) {
            DMExpression[] arguments = new DMExpression[input.Parameters.Length];
            for (int i = 0; i < input.Parameters.Length; i++) {
                DMASTCallParameter parameter = input.Parameters[i];

                if (parameter.Key != null) {
                    DMCompiler.Emit(WarningCode.BadArgument, parameter.Location, "input() does not take named arguments");
                }

                arguments[i] = DMExpression.Create(_dmObject, _proc, parameter.Value);
            }

            DMExpression? list = null;
            if (input.List != null) {
                list = DMExpression.Create(_dmObject, _proc, input.List);

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

            Result = new Expressions.Input(input.Location, arguments, input.Types.Value, list);
        }

        public void VisitInitial(DMASTInitial initial) {
            var expr = DMExpression.Create(_dmObject, _proc, initial.Expression, _inferredPath);
            Result = new Expressions.Initial(initial.Location, expr);
        }

        public void VisitNameof(DMASTNameof nameof) {
            var expr = DMExpression.Create(_dmObject, _proc, nameof.Expression, _inferredPath);
            Result = new Expressions.Nameof(nameof.Location, expr);
        }

        public void VisitIn(DMASTExpressionIn expressionIn) {
            var expr = DMExpression.Create(_dmObject, _proc, expressionIn.Value, _inferredPath);
            var container = DMExpression.Create(_dmObject, _proc, expressionIn.List, _inferredPath);
            Result = new Expressions.In(expressionIn.Location, expr, container);
        }

        public void VisitInRange(DMASTExpressionInRange expressionInRange) {
            var value = DMExpression.Create(_dmObject, _proc, expressionInRange.Value, _inferredPath);
            var startRange = DMExpression.Create(_dmObject, _proc, expressionInRange.StartRange, _inferredPath);
            var endRange = DMExpression.Create(_dmObject, _proc, expressionInRange.EndRange, _inferredPath);
            Result = new Expressions.InRange(expressionInRange.Location, value, startRange, endRange);
        }

        public void VisitPick(DMASTPick pick) {
            Expressions.Pick.PickValue[] pickValues = new Expressions.Pick.PickValue[pick.Values.Length];
            for (int i = 0; i < pickValues.Length; i++) {
                DMASTPick.PickValue pickValue = pick.Values[i];
                DMExpression? weight = (pickValue.Weight != null) ? DMExpression.Create(_dmObject, _proc, pickValue.Weight) : null;
                DMExpression value = DMExpression.Create(_dmObject, _proc, pickValue.Value);

                if (weight is Expressions.Prob prob) // pick(prob(50);x, prob(200);y) format
                    weight = prob.P;

                pickValues[i] = new Expressions.Pick.PickValue(weight, value);
            }

            Result = new Expressions.Pick(pick.Location, pickValues);
        }
        
        public void VisitSin(DMASTSin sin) {
            var expr = DMExpression.Create(_dmObject, _proc, sin.Expression, _inferredPath);
            Result = new Expressions.Sin(sin.Location, expr);
        }

        public void VisitCos(DMASTCos cos) {
            var expr = DMExpression.Create(_dmObject, _proc, cos.Expression, _inferredPath);
            Result = new Expressions.Cos(cos.Location, expr);
        }

        public void VisitTan(DMASTTan tan) {
            var expr = DMExpression.Create(_dmObject, _proc, tan.Expression, _inferredPath);
            Result = new Expressions.Tan(tan.Location, expr);
        }

        public void VisitArcsin(DMASTArcsin arcsin) {
            var expr = DMExpression.Create(_dmObject, _proc, arcsin.Expression, _inferredPath);
            Result = new Expressions.ArcSin(arcsin.Location, expr);
        }

        public void VisitArccos(DMASTArccos arccos) {
            var expr = DMExpression.Create(_dmObject, _proc, arccos.Expression, _inferredPath);
            Result = new Expressions.ArcCos(arccos.Location, expr);
        }

        public void VisitArctan(DMASTArctan arctan) {
            var expr = DMExpression.Create(_dmObject, _proc, arctan.Expression, _inferredPath);
            Result = new Expressions.ArcTan(arctan.Location, expr);
        }

        public void VisitArctan2(DMASTArctan2 arctan2) {
            var xexpr = DMExpression.Create(_dmObject, _proc, arctan2.XExpression, _inferredPath);
            var yexpr = DMExpression.Create(_dmObject, _proc, arctan2.YExpression, _inferredPath);
            Result = new Expressions.ArcTan2(arctan2.Location, xexpr, yexpr);
        }

        public void VisitSqrt(DMASTSqrt sqrt) {
            var expr = DMExpression.Create(_dmObject, _proc, sqrt.Expression, _inferredPath);
            Result = new Expressions.Sqrt(sqrt.Location, expr);
        }

        public void VisitLog(DMASTLog log) {
            var expr = DMExpression.Create(_dmObject, _proc, log.Expression, _inferredPath);
            DMExpression? baseExpr = null;
            if (log.BaseExpression != null) {
                baseExpr = DMExpression.Create(_dmObject, _proc, log.BaseExpression, _inferredPath);
            }
            Result = new Expressions.Log(log.Location, expr, baseExpr);
        }

        public void VisitAbs(DMASTAbs abs) {
            var expr = DMExpression.Create(_dmObject, _proc, abs.Expression, _inferredPath);
            Result = new Expressions.Abs(abs.Location, expr);
        }

        public void VisitCall(DMASTCall call) {
            var procArgs = new ArgumentList(call.Location, _dmObject, _proc, call.ProcParameters, _inferredPath);

            switch (call.CallParameters.Length) {
                default:
                    DMCompiler.Emit(WarningCode.TooManyArguments, call.Location, "Too many arguments for call()");
                    DebugTools.Assert(call.CallParameters.Length > 2); // This feels paranoid but, whatever
                    goto case 2; // Fallthrough!
                case 2: {
                    var a = DMExpression.Create(_dmObject, _proc, call.CallParameters[0].Value, _inferredPath);
                    var b = DMExpression.Create(_dmObject, _proc, call.CallParameters[1].Value, _inferredPath);
                    Result = new Expressions.CallStatement(call.Location, a, b, procArgs);
                    break;
                }
                case 1: {
                    var a = DMExpression.Create(_dmObject, _proc, call.CallParameters[0].Value, _inferredPath);
                    Result = new Expressions.CallStatement(call.Location, a, procArgs);
                    break;
                }
                case 0:
                    DMCompiler.Emit(WarningCode.BadArgument, call.Location, "Not enough arguments for call()");
                    break;
            }
        }
    }
}
