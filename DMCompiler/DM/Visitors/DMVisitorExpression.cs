using OpenDreamShared.Compiler;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;

namespace DMCompiler.DM.Visitors {
    class DMVisitorExpression : DMASTVisitor {
        DMObject _dmObject { get; }
        DMProc _proc { get; }
        DreamPath? _inferredPath { get; }
        internal DMExpression Result { get; private set; }

        internal DMVisitorExpression(DMObject dmObject, DMProc proc, DreamPath? inferredPath) {
            _dmObject = dmObject;
            _proc = proc;
            _inferredPath = inferredPath;
        }

        public void VisitProcStatementExpression(DMASTProcStatementExpression statement) {
            statement.Expression.Visit(this);
        }


        public void VisitConstantNull(DMASTConstantNull constant) {
            Result = new Expressions.Null();
        }

        public void VisitConstantInteger(DMASTConstantInteger constant) {
            Result = new Expressions.Number(constant.Value);
        }

        public void VisitConstantFloat(DMASTConstantFloat constant) {
            Result = new Expressions.Number(constant.Value);
        }

        public void VisitConstantString(DMASTConstantString constant) {
            Result = new Expressions.String(constant.Value);
        }

        public void VisitConstantResource(DMASTConstantResource constant) {
            Result = new Expressions.Resource(constant.Path);
        }

        public void VisitConstantPath(DMASTConstantPath constant) {
            Result = new Expressions.Path(constant.Value.Path);
        }

        public void VisitUpwardPathSearch(DMASTUpwardPathSearch constant) {
            var pathExpr = DMExpression.Constant(_dmObject, _proc, constant.Path);
            if (pathExpr is not Expressions.Path) throw new CompileErrorException("Cannot do an upward path search on " + pathExpr);

            DreamPath path = ((Expressions.Path)pathExpr).Value;
            DreamPath? foundPath = DMObjectTree.UpwardSearch(path, constant.Search.Path);

            if (foundPath == null) {
                throw new CompileErrorException($"Invalid path {path}.{constant.Search.Path}");
            }

            Result = new Expressions.Path(foundPath.Value);
        }

        public void VisitStringFormat(DMASTStringFormat stringFormat) {
            var expressions = new DMExpression[stringFormat.InterpolatedValues.Length];

            for (int i = 0; i < stringFormat.InterpolatedValues.Length; i++) {
                if (stringFormat.InterpolatedValues[i] is null) {
                    expressions[i] = new Expressions.Null();
                } else {
                    expressions[i] = DMExpression.Create(_dmObject, _proc, stringFormat.InterpolatedValues[i], _inferredPath);
                }
            }

            Result = new Expressions.StringFormat(stringFormat.Value, expressions);
        }


        public void VisitIdentifier(DMASTIdentifier identifier) {
            var name = identifier.Identifier;

            if (name == "src") {
                Result = new Expressions.Src(_dmObject.Path);
            } else if (name == "usr") {
                Result = new Expressions.Usr();
            } else if (name == "args") {
                Result = new Expressions.Args();
            } else {
                if (_proc == null)
                    throw new CompileErrorException($"Cannot resolve identifier in const context: {name}");

                DMProc.DMLocalVariable localVar = _proc.GetLocalVariable(name);

                if (localVar != null) {
                    Result = new Expressions.Local(localVar.Type, name);
                    return;
                }

                var field = _dmObject.GetVariable(name);

                if (field == null) {
                    field = _dmObject.GetGlobalVariable(name);
                }

                if (field == null) {
                    throw new CompileErrorException($"unknown identifier {name}");
                }

                Result = new Expressions.Field(field.Type, name);
            }
        }


        public void VisitCallableSelf(DMASTCallableSelf self) {
            Result = new Expressions.ProcSelf();
        }

        public void VisitCallableSuper(DMASTCallableSuper super) {
            Result = new Expressions.ProcSuper();
        }

        public void VisitCallableProcIdentifier(DMASTCallableProcIdentifier procIdentifier) {
            Result = new Expressions.Proc(procIdentifier.Identifier);
        }

        public void VisitProcCall(DMASTProcCall procCall) {
            // arglist hack
            if (procCall.Callable is DMASTCallableProcIdentifier ident) {
                if (ident.Identifier == "arglist") {
                    if (procCall.Parameters.Length != 1) throw new CompileErrorException("arglist must have 1 argument");

                    var expr = DMExpression.Create(_dmObject, _proc, procCall.Parameters[0].Value, _inferredPath);
                    Result = new Expressions.Arglist(expr);
                    return;
                }
            }

            var target = DMExpression.Create(_dmObject, _proc, procCall.Callable, _inferredPath);
            var args = new ArgumentList(_dmObject, _proc, procCall.Parameters);
            Result = new Expressions.ProcCall(target, args);
        }

        public void VisitAssign(DMASTAssign assign) {
            var lhs = DMExpression.Create(_dmObject, _proc, assign.Expression, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, assign.Value, lhs.Path);
            Result = new Expressions.Assignment(lhs, rhs);
        }

        public void VisitNegate(DMASTNegate negate) {
            var expr = DMExpression.Create(_dmObject, _proc, negate.Expression, _inferredPath);
            Result = new Expressions.Negate(expr);
        }

        public void VisitNot(DMASTNot not) {
            var expr = DMExpression.Create(_dmObject, _proc, not.Expression, _inferredPath);
            Result = new Expressions.Not(expr);
        }

        public void VisitBinaryNot(DMASTBinaryNot binaryNot) {
            var expr = DMExpression.Create(_dmObject, _proc, binaryNot.Value, _inferredPath);
            Result = new Expressions.BinaryNot(expr);
        }

        public void VisitAdd(DMASTAdd add) {
            var lhs = DMExpression.Create(_dmObject, _proc, add.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, add.B, _inferredPath);
            Result = new Expressions.Add(lhs, rhs);
        }

        public void VisitSubtract(DMASTSubtract subtract) {
            var lhs = DMExpression.Create(_dmObject, _proc, subtract.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, subtract.B, _inferredPath);
            Result = new Expressions.Subtract(lhs, rhs);
        }

        public void VisitMultiply(DMASTMultiply multiply) {
            var lhs = DMExpression.Create(_dmObject, _proc, multiply.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, multiply.B, _inferredPath);
            Result = new Expressions.Multiply(lhs, rhs);
        }

        public void VisitDivide(DMASTDivide divide) {
            var lhs = DMExpression.Create(_dmObject, _proc, divide.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, divide.B, _inferredPath);
            Result = new Expressions.Divide(lhs, rhs);
        }

        public void VisitModulus(DMASTModulus modulus) {
            var lhs = DMExpression.Create(_dmObject, _proc, modulus.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, modulus.B, _inferredPath);
            Result = new Expressions.Modulo(lhs, rhs);
        }

        public void VisitPower(DMASTPower power) {
            var lhs = DMExpression.Create(_dmObject, _proc, power.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, power.B, _inferredPath);
            Result = new Expressions.Power(lhs, rhs);
        }

        public void VisitAppend(DMASTAppend append) {
            var lhs = DMExpression.Create(_dmObject, _proc, append.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, append.B, _inferredPath);
            Result = new Expressions.Append(lhs, rhs);
        }

        public void VisitCombine(DMASTCombine combine) {
            var lhs = DMExpression.Create(_dmObject, _proc, combine.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, combine.B, _inferredPath);
            Result = new Expressions.Combine(lhs, rhs);
        }

        public void VisitRemove(DMASTRemove remove) {
            var lhs = DMExpression.Create(_dmObject, _proc, remove.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, remove.B, _inferredPath);
            Result = new Expressions.Remove(lhs, rhs);
        }

        public void VisitMask(DMASTMask mask) {
            var lhs = DMExpression.Create(_dmObject, _proc, mask.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, mask.B, _inferredPath);
            Result = new Expressions.Mask(lhs, rhs);
        }

        public void VisitMultiplyAssign(DMASTMultiplyAssign multiplyAssign) {
            var lhs = DMExpression.Create(_dmObject, _proc, multiplyAssign.A);
            var rhs = DMExpression.Create(_dmObject, _proc, multiplyAssign.B);
            Result = new Expressions.MultiplyAssign(lhs, rhs);
        }

        public void VisitDivideAssign(DMASTDivideAssign divideAssign) {
            var lhs = DMExpression.Create(_dmObject, _proc, divideAssign.A);
            var rhs = DMExpression.Create(_dmObject, _proc, divideAssign.B);
            Result = new Expressions.DivideAssign(lhs, rhs);
        }

        public void VisitLeftShiftAssign(DMASTLeftShiftAssign leftShiftAssign) {
            var lhs = DMExpression.Create(_dmObject, _proc, leftShiftAssign.A);
            var rhs = DMExpression.Create(_dmObject, _proc, leftShiftAssign.B);
            Result = new Expressions.LeftShiftAssign(lhs, rhs);
        }

        public void VisitRightShiftAssign(DMASTRightShiftAssign rightShiftAssign) {
            var lhs = DMExpression.Create(_dmObject, _proc, rightShiftAssign.A);
            var rhs = DMExpression.Create(_dmObject, _proc, rightShiftAssign.B);
            Result = new Expressions.RightShiftAssign(lhs, rhs);
        }

        public void VisitXorAssign(DMASTXorAssign xorAssign) {
            var lhs = DMExpression.Create(_dmObject, _proc, xorAssign.A);
            var rhs = DMExpression.Create(_dmObject, _proc, xorAssign.B);
            Result = new Expressions.XorAssign(lhs, rhs);
        }

        public void VisitModulusAssign(DMASTModulusAssign modulusAssign) {
            var lhs = DMExpression.Create(_dmObject, _proc, modulusAssign.A);
            var rhs = DMExpression.Create(_dmObject, _proc, modulusAssign.B);
            Result = new Expressions.ModulusAssign(lhs, rhs);
        }

        public void VisitLeftShift(DMASTLeftShift leftShift) {
            var lhs = DMExpression.Create(_dmObject, _proc, leftShift.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, leftShift.B, _inferredPath);
            Result = new Expressions.LeftShift(lhs, rhs);
        }

        public void VisitRightShift(DMASTRightShift rightShift) {
            var lhs = DMExpression.Create(_dmObject, _proc, rightShift.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, rightShift.B, _inferredPath);
            Result = new Expressions.RightShift(lhs, rhs);
        }

        public void VisitBinaryAnd(DMASTBinaryAnd binaryAnd) {
            var lhs = DMExpression.Create(_dmObject, _proc, binaryAnd.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, binaryAnd.B, _inferredPath);
            Result = new Expressions.BinaryAnd(lhs, rhs);
        }

        public void VisitBinaryXor(DMASTBinaryXor binaryXor) {
            var lhs = DMExpression.Create(_dmObject, _proc, binaryXor.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, binaryXor.B, _inferredPath);
            Result = new Expressions.BinaryXor(lhs, rhs);
        }

        public void VisitBinaryOr(DMASTBinaryOr binaryOr) {
            var lhs = DMExpression.Create(_dmObject, _proc, binaryOr.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, binaryOr.B, _inferredPath);
            Result = new Expressions.BinaryOr(lhs, rhs);
        }

        public void VisitEqual(DMASTEqual equal) {
            var lhs = DMExpression.Create(_dmObject, _proc, equal.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, equal.B, _inferredPath);
            Result = new Expressions.Equal(lhs, rhs);
        }

        public void VisitNotEqual(DMASTNotEqual notEqual) {
            var lhs = DMExpression.Create(_dmObject, _proc, notEqual.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, notEqual.B, _inferredPath);
            Result = new Expressions.NotEqual(lhs, rhs);
        }

        public void VisitEquivalent(DMASTEquivalent equivalent) {
            var lhs = DMExpression.Create(_dmObject, _proc, equivalent.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, equivalent.B, _inferredPath);
            Result = new Expressions.Equivalent(lhs, rhs);
        }

        public void VisitNotEquivalent(DMASTNotEquivalent notEquivalent) {
            var lhs = DMExpression.Create(_dmObject, _proc, notEquivalent.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, notEquivalent.B, _inferredPath);
            Result = new Expressions.NotEquivalent(lhs, rhs);
        }

        public void VisitGreaterThan(DMASTGreaterThan greaterThan) {
            var lhs = DMExpression.Create(_dmObject, _proc, greaterThan.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, greaterThan.B, _inferredPath);
            Result = new Expressions.GreaterThan(lhs, rhs);
        }

        public void VisitGreaterThanOrEqual(DMASTGreaterThanOrEqual greaterThanOrEqual) {
            var lhs = DMExpression.Create(_dmObject, _proc, greaterThanOrEqual.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, greaterThanOrEqual.B, _inferredPath);
            Result = new Expressions.GreaterThanOrEqual(lhs, rhs);
        }

        public void VisitLessThan(DMASTLessThan lessThan) {
            var lhs = DMExpression.Create(_dmObject, _proc, lessThan.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, lessThan.B, _inferredPath);
            Result = new Expressions.LessThan(lhs, rhs);
        }

        public void VisitLessThanOrEqual(DMASTLessThanOrEqual lessThanOrEqual) {
            var lhs = DMExpression.Create(_dmObject, _proc, lessThanOrEqual.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, lessThanOrEqual.B, _inferredPath);
            Result = new Expressions.LessThanOrEqual(lhs, rhs);
        }

        public void VisitOr(DMASTOr or) {
            var lhs = DMExpression.Create(_dmObject, _proc, or.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, or.B, _inferredPath);
            Result = new Expressions.Or(lhs, rhs);
        }

        public void VisitAnd(DMASTAnd and) {
            var lhs = DMExpression.Create(_dmObject, _proc, and.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, and.B, _inferredPath);
            Result = new Expressions.And(lhs, rhs);
        }

        public void VisitTernary(DMASTTernary ternary) {
            var a = DMExpression.Create(_dmObject, _proc, ternary.A, _inferredPath);
            var b = DMExpression.Create(_dmObject, _proc, ternary.B, _inferredPath);
            var c = DMExpression.Create(_dmObject, _proc, ternary.C, _inferredPath);
            Result = new Expressions.Ternary(a, b, c);
        }

        public void VisitListIndex(DMASTListIndex listIndex) {
            var expr = DMExpression.Create(_dmObject, _proc, listIndex.Expression, _inferredPath);
            var index = DMExpression.Create(_dmObject, _proc, listIndex.Index, expr.Path);
            Result = new Expressions.ListIndex(expr, index, expr.Path);
        }

        public void VisitDereference(DMASTDereference dereference) {
            var expr = DMExpression.Create(_dmObject, _proc, dereference.Expression, _inferredPath);
            Result = new Expressions.Dereference(expr, dereference);
        }

        public void VisitDereferenceProc(DMASTDereferenceProc dereferenceProc) {
            var expr = DMExpression.Create(_dmObject, _proc, dereferenceProc.Expression, _inferredPath);
            Result = new Expressions.DereferenceProc(expr, dereferenceProc);
        }

        public void VisitNewPath(DMASTNewPath newPath) {
            var args = new ArgumentList(_dmObject, _proc, newPath.Parameters, _inferredPath);
            Result = new Expressions.NewPath(newPath.Path.Path, args);
        }

        public void VisitNewInferred(DMASTNewInferred newInferred) {
            if (_inferredPath is null) {
                throw new CompileErrorException("An inferred new requires a type!");
            }

            var args = new ArgumentList(_dmObject, _proc, newInferred.Parameters, _inferredPath);
            Result = new Expressions.NewPath(_inferredPath.Value, args);
        }

        public void VisitNewIdentifier(DMASTNewIdentifier newIdentifier) {
            var expr = DMExpression.Create(_dmObject, _proc, newIdentifier.Identifier, _inferredPath);
            var args = new ArgumentList(_dmObject, _proc, newIdentifier.Parameters, _inferredPath);
            Result = new Expressions.New(expr, args);
        }

        public void VisitNewDereference(DMASTNewDereference newDereference) {
            var expr = DMExpression.Create(_dmObject, _proc, newDereference.Dereference, _inferredPath);
            var args = new ArgumentList(_dmObject, _proc, newDereference.Parameters, _inferredPath);
            Result = new Expressions.New(expr, args);
        }

        public void VisitPreIncrement(DMASTPreIncrement preIncrement) {
            var expr = DMExpression.Create(_dmObject, _proc, preIncrement.Expression, _inferredPath);
            Result = new Expressions.PreIncrement(expr);
        }

        public void VisitPostIncrement(DMASTPostIncrement postIncrement) {
            var expr = DMExpression.Create(_dmObject, _proc, postIncrement.Expression, _inferredPath);
            Result = new Expressions.PostIncrement(expr);
        }

        public void VisitPreDecrement(DMASTPreDecrement preDecrement) {
            var expr = DMExpression.Create(_dmObject, _proc, preDecrement.Expression, _inferredPath);
            Result = new Expressions.PreDecrement(expr);
        }

        public void VisitPostDecrement(DMASTPostDecrement postDecrement) {
            var expr = DMExpression.Create(_dmObject, _proc, postDecrement.Expression, _inferredPath);
            Result = new Expressions.PostDecrement(expr);
        }

        public void VisitLocate(DMASTLocate locate) {
            var container = locate.Container != null ? DMExpression.Create(_dmObject, _proc, locate.Container, _inferredPath) : null;

            if (locate.Expression == null) {
                if (_inferredPath == null) {
                    throw new CompileErrorException("inferred lcoate requires a type");
                }
                Result = new Expressions.LocateInferred(_inferredPath.Value, container);
                return;
            }

            var pathExpr = DMExpression.Create(_dmObject, _proc, locate.Expression, _inferredPath);
            Result = new Expressions.Locate(pathExpr, container);
        }

        public void VisitLocateCoordinates(DMASTLocateCoordinates locateCoordinates) {
            var _x = DMExpression.Create(_dmObject, _proc, locateCoordinates.X, _inferredPath);
            var _y = DMExpression.Create(_dmObject, _proc, locateCoordinates.Y, _inferredPath);
            var _z = DMExpression.Create(_dmObject, _proc, locateCoordinates.Z, _inferredPath);
            Result = new Expressions.LocateCoordinates(_x, _y, _z);
        }

        public void VisitIsSaved(DMASTIsSaved isSaved) {
            var expr = DMExpression.Create(_dmObject, _proc, isSaved.Expression, _inferredPath);
            Result = new Expressions.IsSaved(expr);
        }

        public void VisitIsType(DMASTIsType isType) {
            var expr = DMExpression.Create(_dmObject, _proc, isType.Value, _inferredPath);
            var path = DMExpression.Create(_dmObject, _proc, isType.Type, _inferredPath);
            Result = new Expressions.IsType(expr, path);
        }

        public void VisitImplicitIsType(DMASTImplicitIsType isType) {
            var expr = DMExpression.Create(_dmObject, _proc, isType.Value, _inferredPath);

            if (expr.Path is null) {
                throw new CompileErrorException("An inferred istype requires a type!");
            }

            Result = new Expressions.IsTypeInferred(expr, expr.Path.Value);
        }

        public void VisitList(DMASTList list) {
            Result = new Expressions.List(list);
        }

        public void VisitNewList(DMASTNewList newList) {
            DMExpression[] expressions = new DMExpression[newList.Parameters.Length];

            for (int i = 0; i < newList.Parameters.Length; i++) {
                DMASTCallParameter parameter = newList.Parameters[i];
                if (parameter.Name != null) throw new CompileErrorException("newlist() does not take named arguments");

                expressions[i] = DMExpression.Create(_dmObject, _proc, parameter.Value, _inferredPath);
            }

            Result = new Expressions.NewList(expressions);
        }

        public void VisitInput(DMASTInput input) {
            Result = new Expressions.Input(input);
        }

        public void VisitInitial(DMASTInitial initial) {
            var expr = DMExpression.Create(_dmObject, _proc, initial.Expression, _inferredPath);
            Result = new Expressions.Initial(expr);
        }

        public void VisitIn(DMASTExpressionIn expressionIn) {
            var expr = DMExpression.Create(_dmObject, _proc, expressionIn.Value, _inferredPath);
            var container = DMExpression.Create(_dmObject, _proc, expressionIn.List, _inferredPath);
            Result = new Expressions.In(expr, container);
        }

        public void VisitPick(DMASTPick pick) {
            Expressions.Pick.PickValue[] pickValues = new Expressions.Pick.PickValue[pick.Values.Length];
            for (int i = 0; i < pickValues.Length; i++) {
                DMASTPick.PickValue pickValue = pick.Values[i];
                DMExpression weight = (pickValue.Weight != null) ? DMExpression.Create(_dmObject, _proc, pickValue.Weight) : null;
                DMExpression value = DMExpression.Create(_dmObject, _proc, pickValue.Value);

                pickValues[i] = new Expressions.Pick.PickValue(weight, value);
            }

            Result = new Expressions.Pick(pickValues);
        }

        public void VisitCall(DMASTCall call) {
            var procArgs = new ArgumentList(_dmObject, _proc, call.ProcParameters, _inferredPath);

            switch (call.CallParameters.Length) {
                case 1:
                    {
                        var a = DMExpression.Create(_dmObject, _proc, call.CallParameters[0].Value, _inferredPath);
                        Result = new Expressions.CallStatement(a, procArgs);
                    }
                    break;

                case 2:
                    {
                        var a = DMExpression.Create(_dmObject, _proc, call.CallParameters[0].Value, _inferredPath);
                        var b = DMExpression.Create(_dmObject, _proc, call.CallParameters[1].Value, _inferredPath);
                        Result = new Expressions.CallStatement(a, b, procArgs);
                    }
                    break;

                default:
                    throw new CompileErrorException("invalid argument count for call()");
            }
        }
    }
}
