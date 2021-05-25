using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using System;
using System.Collections.Generic;

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

        public void VisitStringFormat(DMASTStringFormat stringFormat) {
            var expressions = new DMExpression[stringFormat.InterpolatedValues.Length];

            for (int i = 0; i < stringFormat.InterpolatedValues.Length; i++) {
                expressions[i] = DMExpression.Create(_dmObject, _proc, stringFormat.InterpolatedValues[i]);
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
                    throw new Exception($"unknown identifier {name}");
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
                    if (procCall.Parameters.Length != 1) throw new Exception("arglist must have 1 argument");

                    var expr = DMExpression.Create(_dmObject, _proc, procCall.Parameters[0].Value);
                    Result = new Expressions.Arglist(expr);
                    return;
                }
            }

            var target = DMExpression.Create(_dmObject, _proc, procCall.Callable);
            var args = new ArgumentList(_dmObject, _proc, procCall.Parameters);
            Result = new Expressions.ProcCall(target, args);
        }

        public void VisitAssign(DMASTAssign assign) {
            var lhs = DMExpression.Create(_dmObject, _proc, assign.Expression);
            var rhs = DMExpression.Create(_dmObject, _proc, assign.Value, lhs.Path);
            Result = new Expressions.Assignment(lhs, rhs);
        }

        public void VisitNegate(DMASTNegate negate) {
            var expr = DMExpression.Create(_dmObject, _proc, negate.Expression);
            Result = new Expressions.Negate(expr);
        }

        public void VisitNot(DMASTNot not) {
            var expr = DMExpression.Create(_dmObject, _proc, not.Expression);
            Result = new Expressions.Not(expr);
        }

        public void VisitBinaryNot(DMASTBinaryNot binaryNot) {
            var expr = DMExpression.Create(_dmObject, _proc, binaryNot.Value);
            Result = new Expressions.BinaryNot(expr);
        }

        public void VisitAdd(DMASTAdd add) {
            var lhs = DMExpression.Create(_dmObject, _proc, add.A);
            var rhs = DMExpression.Create(_dmObject, _proc, add.B);
            Result = new Expressions.Add(lhs, rhs);
        }

        public void VisitSubtract(DMASTSubtract subtract) {
            var lhs = DMExpression.Create(_dmObject, _proc, subtract.A);
            var rhs = DMExpression.Create(_dmObject, _proc, subtract.B);
            Result = new Expressions.Add(lhs, rhs);
        }
        
        public void VisitMultiply(DMASTMultiply multiply) {
            var lhs = DMExpression.Create(_dmObject, _proc, multiply.A);
            var rhs = DMExpression.Create(_dmObject, _proc, multiply.B);
            Result = new Expressions.Multiply(lhs, rhs);
        }

        public void VisitDivide(DMASTDivide divide) {
            var lhs = DMExpression.Create(_dmObject, _proc, divide.A);
            var rhs = DMExpression.Create(_dmObject, _proc, divide.B);
            Result = new Expressions.Divide(lhs, rhs);
        }

        public void VisitModulus(DMASTModulus modulus) {
            var lhs = DMExpression.Create(_dmObject, _proc, modulus.A);
            var rhs = DMExpression.Create(_dmObject, _proc, modulus.B);
            Result = new Expressions.Modulo(lhs, rhs);
        }

        public void VisitPower(DMASTPower power) {
            var lhs = DMExpression.Create(_dmObject, _proc, power.A);
            var rhs = DMExpression.Create(_dmObject, _proc, power.B);
            Result = new Expressions.Power(lhs, rhs);
        }

        public void VisitAppend(DMASTAppend append) {
            var lhs = DMExpression.Create(_dmObject, _proc, append.A);
            var rhs = DMExpression.Create(_dmObject, _proc, append.B);
            Result = new Expressions.Append(lhs, rhs);
        }

        public void VisitCombine(DMASTCombine combine) {
            var lhs = DMExpression.Create(_dmObject, _proc, combine.A);
            var rhs = DMExpression.Create(_dmObject, _proc, combine.B);
            Result = new Expressions.Combine(lhs, rhs);
        }

        public void VisitRemove(DMASTRemove remove) {
            var lhs = DMExpression.Create(_dmObject, _proc, remove.A);
            var rhs = DMExpression.Create(_dmObject, _proc, remove.B);
            Result = new Expressions.Remove(lhs, rhs);
        }

        public void VisitMask(DMASTMask mask) {
            var lhs = DMExpression.Create(_dmObject, _proc, mask.A);
            var rhs = DMExpression.Create(_dmObject, _proc, mask.B);
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

        public void VisitLeftShift(DMASTLeftShift leftShift) {
            var lhs = DMExpression.Create(_dmObject, _proc, leftShift.A);
            var rhs = DMExpression.Create(_dmObject, _proc, leftShift.B);
            Result = new Expressions.LeftShift(lhs, rhs);
        }

        public void VisitRightShift(DMASTRightShift rightShift) {
            var lhs = DMExpression.Create(_dmObject, _proc, rightShift.A);
            var rhs = DMExpression.Create(_dmObject, _proc, rightShift.B);
            Result = new Expressions.RightShift(lhs, rhs);
        }

        public void VisitBinaryAnd(DMASTBinaryAnd binaryAnd) {
            var lhs = DMExpression.Create(_dmObject, _proc, binaryAnd.A);
            var rhs = DMExpression.Create(_dmObject, _proc, binaryAnd.B);
            Result = new Expressions.BinaryAnd(lhs, rhs);
        }

        public void VisitBinaryXor(DMASTBinaryXor binaryXor) {
            var lhs = DMExpression.Create(_dmObject, _proc, binaryXor.A);
            var rhs = DMExpression.Create(_dmObject, _proc, binaryXor.B);
            Result = new Expressions.BinaryXor(lhs, rhs);
        }

        public void VisitBinaryOr(DMASTBinaryOr binaryOr) {
            var lhs = DMExpression.Create(_dmObject, _proc, binaryOr.A);
            var rhs = DMExpression.Create(_dmObject, _proc, binaryOr.B);
            Result = new Expressions.BinaryOr(lhs, rhs);
        }

        public void VisitEqual(DMASTEqual equal) {
            var lhs = DMExpression.Create(_dmObject, _proc, equal.A);
            var rhs = DMExpression.Create(_dmObject, _proc, equal.B);
            Result = new Expressions.Equal(lhs, rhs);
        }

        public void VisitNotEqual(DMASTNotEqual notEqual) {
            var lhs = DMExpression.Create(_dmObject, _proc, notEqual.A);
            var rhs = DMExpression.Create(_dmObject, _proc, notEqual.B);
            Result = new Expressions.NotEqual(lhs, rhs);
        }
        
        public void VisitGreaterThan(DMASTGreaterThan greaterThan) {
            var lhs = DMExpression.Create(_dmObject, _proc, greaterThan.A);
            var rhs = DMExpression.Create(_dmObject, _proc, greaterThan.B);
            Result = new Expressions.GreaterThan(lhs, rhs);
        }

        public void VisitGreaterThanOrEqual(DMASTGreaterThanOrEqual greaterThanOrEqual) {
            var lhs = DMExpression.Create(_dmObject, _proc, greaterThanOrEqual.A);
            var rhs = DMExpression.Create(_dmObject, _proc, greaterThanOrEqual.B);
            Result = new Expressions.GreaterThanOrEqual(lhs, rhs);
        }

        public void VisitLessThan(DMASTLessThan lessThan) {
            var lhs = DMExpression.Create(_dmObject, _proc, lessThan.A);
            var rhs = DMExpression.Create(_dmObject, _proc, lessThan.B);
            Result = new Expressions.LessThan(lhs, rhs);
        }

        public void VisitLessThanOrEqual(DMASTLessThanOrEqual lessThanOrEqual) {
            var lhs = DMExpression.Create(_dmObject, _proc, lessThanOrEqual.A);
            var rhs = DMExpression.Create(_dmObject, _proc, lessThanOrEqual.B);
            Result = new Expressions.LessThanOrEqual(lhs, rhs);
        }

        public void VisitOr(DMASTOr or) {
            var lhs = DMExpression.Create(_dmObject, _proc, or.A);
            var rhs = DMExpression.Create(_dmObject, _proc, or.B);
            Result = new Expressions.Or(lhs, rhs);
        }

        public void VisitAnd(DMASTAnd and) {
            var lhs = DMExpression.Create(_dmObject, _proc, and.A);
            var rhs = DMExpression.Create(_dmObject, _proc, and.B);
            Result = new Expressions.And(lhs, rhs);
        }

        public void VisitTernary(DMASTTernary ternary) {
            var a = DMExpression.Create(_dmObject, _proc, ternary.A);
            var b = DMExpression.Create(_dmObject, _proc, ternary.B);
            var c = DMExpression.Create(_dmObject, _proc, ternary.C);
            Result = new Expressions.Ternary(a, b, c);
        }

        public void VisitListIndex(DMASTListIndex listIndex) {
            var expr = DMExpression.Create(_dmObject, _proc, listIndex.Expression);
            var index = DMExpression.Create(_dmObject, _proc, listIndex.Index);
            Result = new Expressions.ListIndex(expr, index);
        }

        public void VisitDereference(DMASTDereference dereference) {
            var expr = DMExpression.Create(_dmObject, _proc, dereference.Expression);
            Result = new Expressions.Dereference(expr, dereference, true);
        }

        public void VisitDereferenceProc(DMASTDereferenceProc dereferenceProc) {
            var expr = DMExpression.Create(_dmObject, _proc, dereferenceProc.Expression);
            Result = new Expressions.DereferenceProc(expr, dereferenceProc);
        }

        public void VisitNewPath(DMASTNewPath newPath) {
            var args = new ArgumentList(_dmObject, _proc, newPath.Parameters);
            Result = new Expressions.NewPath(newPath.Path.Path, args);
        }

        public void VisitNewInferred(DMASTNewInferred newInferred) {
            if (_inferredPath is null) {
                throw new Exception("An inferred new requires a type!");
            }

            var args = new ArgumentList(_dmObject, _proc, newInferred.Parameters);
            Result = new Expressions.NewPath(_inferredPath.Value, args);
        }

        public void VisitNewIdentifier(DMASTNewIdentifier newIdentifier) {
            var expr = DMExpression.Create(_dmObject, _proc, newIdentifier.Identifier);
            var args = new ArgumentList(_dmObject, _proc, newIdentifier.Parameters);
            Result = new Expressions.New(expr, args);
        }

        public void VisitNewDereference(DMASTNewDereference newDereference) {
            var expr = DMExpression.Create(_dmObject, _proc, newDereference.Dereference);
            var args = new ArgumentList(_dmObject, _proc, newDereference.Parameters);
            Result = new Expressions.New(expr, args);
        }

        public void VisitPreIncrement(DMASTPreIncrement preIncrement) {
            var expr = DMExpression.Create(_dmObject, _proc, preIncrement.Expression);
            Result = new Expressions.PreIncrement(expr);
        }

        public void VisitPostIncrement(DMASTPostIncrement postIncrement) {
            var expr = DMExpression.Create(_dmObject, _proc, postIncrement.Expression);
            Result = new Expressions.PostIncrement(expr);
        }

        public void VisitPreDecrement(DMASTPreDecrement preDecrement) {
            var expr = DMExpression.Create(_dmObject, _proc, preDecrement.Expression);
            Result = new Expressions.PreDecrement(expr);
        }

        public void VisitPostDecrement(DMASTPostDecrement postDecrement) {
            var expr = DMExpression.Create(_dmObject, _proc, postDecrement.Expression);
            Result = new Expressions.PostDecrement(expr);
        }

        public void VisitLocate(DMASTLocate locate) {
            var container = locate.Container != null ? DMExpression.Create(_dmObject, _proc, locate.Container) : null;

            if (locate.Expression == null) {
                if (_inferredPath == null) {
                    throw new Exception("inferred lcoate requires a type");
                }
                Result = new Expressions.LocateInferred(_inferredPath.Value, container);
                return;
            }

            var pathExpr = DMExpression.Create(_dmObject, _proc, locate.Expression);
            Result = new Expressions.Locate(pathExpr, container);
        }

        public void VisitLocateCoordinates(DMASTLocateCoordinates locateCoordinates) {
            var _x = DMExpression.Create(_dmObject, _proc, locateCoordinates.X);
            var _y = DMExpression.Create(_dmObject, _proc, locateCoordinates.Y);
            var _z = DMExpression.Create(_dmObject, _proc, locateCoordinates.Z);
            Result = new Expressions.LocateCoordinates(_x, _y, _z);
        }

        public void VisitIsType(DMASTIsType isType) {
            var expr = DMExpression.Create(_dmObject, _proc, isType.Value);
            var path = DMExpression.Create(_dmObject, _proc, isType.Type);
            Result = new Expressions.IsType(expr, path);
        }

        public void VisitImplicitIsType(DMASTImplicitIsType isType) {
            var expr = DMExpression.Create(_dmObject, _proc, isType.Value);

            if (expr.Path is null) {
                throw new Exception("An inferred istype requires a type!");
            }

            Result = new Expressions.IsTypeInferred(expr, expr.Path.Value);
        }
        
        public void VisitList(DMASTList list) {
            Result = new Expressions.List(list);
        }

        public void VisitInput(DMASTInput input) {
            Result = new Expressions.Input(input);
        }

        public void VisitInitial(DMASTInitial initial) {
            var expr = DMExpression.Create(_dmObject, _proc, initial.Expression);
            Result = new Expressions.Initial(expr);
        }

        public void VisitIn(DMASTExpressionIn expressionIn) {
            var expr = DMExpression.Create(_dmObject, _proc, expressionIn.Value);
            var container = DMExpression.Create(_dmObject, _proc, expressionIn.List);   
            Result = new Expressions.In(expr, container);
        }

        public void VisitCall(DMASTCall call) {
            var procArgs = new ArgumentList(_dmObject, _proc, call.CallParameters);

            switch (call.CallParameters.Length) {
                case 1:
                    {
                        var a = DMExpression.Create(_dmObject, _proc, call.CallParameters[0].Value);
                        Result = new Expressions.CallStatement(a, procArgs);
                    }
                    break;

                case 2:
                    {
                        var a = DMExpression.Create(_dmObject, _proc, call.CallParameters[0].Value);
                        var b = DMExpression.Create(_dmObject, _proc, call.CallParameters[1].Value);
                        Result = new Expressions.CallStatement(a, b, procArgs);
                    }
                    break;

                default:
                    throw new Exception("invalid argument count for call()");
            }            
        }
    }
}
