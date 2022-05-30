using System;
using DMCompiler.DM.Expressions;
using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.DM.Visitors {
    sealed class DMExpressionBuilder {
        private DMObject _dmObject { get; }
        private DMProc _proc { get; }
        private DreamPath? _inferredPath { get; }

        // NOTE This needs to be turned into a Stack of modes if more complicated scope changes are added in the future
        private string _scopeMode;

        internal DMExpressionBuilder(DMObject dmObject, DMProc proc, DreamPath? inferredPath)
        {
            _dmObject = dmObject;
            _proc = proc;
            _inferredPath = inferredPath;
        }

        public DMExpression BuildExpression(DMASTExpression expression, string scopeMode = "normal") {
            _scopeMode = scopeMode;

            switch (expression) {
                case DMASTExpressionConstant constant: return BuildExpressionConstant(constant);
                case DMASTCallable: return BuildCallable(expression);
                case DMASTStringFormat stringFormat: return BuildStringFormat(stringFormat);
                case DMASTIdentifier identifier: return BuildIdentifier(identifier);
                case DMASTGlobalIdentifier globalIdentifier: return BuildGlobalIdentifier(globalIdentifier);
                case DMASTProcCall procCall: return BuildProcCall(procCall);
                case DMASTUnary: return BuildUnaryExpression(expression);
                case DMASTBinary: return BuildBinaryExpression(expression);
                case DMASTTernary ternary: return BuildTernary(ternary);
                case DMASTListIndex listIndex: return BuildListIndex(listIndex);
                case DMASTNewPath newPath: return BuildNewPath(newPath);
                case DMASTNewInferred newInferred: return BuildNewInferred(newInferred);
                case DMASTNewIdentifier newIdentifier: return BuildNewIdentifier(newIdentifier);
                case DMASTNewDereference newDereference: return BuildNewDereference(newDereference);
                case DMASTNewListIndex newListIdx: return BuildNewListIndex(newListIdx);
                case DMASTLocate locate: return BuildLocate(locate);
                case DMASTLocateCoordinates locateCoordinates: return BuildLocateCoordinates(locateCoordinates);
                case DMASTList list: return BuildList(list);
                case DMASTNewList newList: return BuildNewList(newList);
                case DMASTAddText addText: return BuildAddText(addText);
                case DMASTInput input: return BuildInput(input);
                case DMASTExpressionInRange inRange: return BuildInRange(inRange);
                case DMASTPick pick: return BuildPick(pick);
                case DMASTCall call: return BuildCall(call);
                default:
                    DMCompiler.Error(expression.Location, $"Failed to build expression from AST node of type {expression.GetType()}");
                    return new Expressions.Null(expression.Location);
            }
        }

        private DMExpression BuildExpressionConstant(DMASTExpressionConstant constant) {
            switch (constant) {
                case DMASTUpwardPathSearch upwardPathSearch: return BuildUpwardPathSearch(upwardPathSearch);
                case DMASTConstantNull: return new Expressions.Null(constant.Location);
                case DMASTConstantInteger constInt: return new Expressions.Number(constant.Location, constInt.Value);
                case DMASTConstantFloat constFloat: return new Expressions.Number(constant.Location, constFloat.Value);
                case DMASTConstantString constString: return new Expressions.String(constant.Location, constString.Value);
                case DMASTConstantResource constResource: return new Expressions.Resource(constant.Location, constResource.Path);
                case DMASTConstantPath constPath: return new Expressions.Path(constant.Location, constPath.Value.Path);
                default:
                    DMCompiler.Error(constant.Location, $"Failed to build constant expression from AST node of type {constant.GetType()}");
                    return new Expressions.Null(constant.Location);
            }
        }

        private DMExpression BuildUpwardPathSearch(DMASTUpwardPathSearch constant) {
            DMExpression.TryConstant(_dmObject, _proc, constant.Path, out var pathExpr);
            if (pathExpr is not Expressions.Path expr) throw new CompileErrorException(constant.Location, "Cannot do an upward path search on " + pathExpr);

            DreamPath path = expr.Value;
            DreamPath? foundPath = DMObjectTree.UpwardSearch(path, constant.Search.Path);

            if (foundPath == null) {
                throw new CompileErrorException(constant.Location, $"Invalid path {path}.{constant.Search.Path}");
            }

            return new Expressions.Path(constant.Location, foundPath.Value);
        }

        private DMExpression BuildCallable(DMASTExpression callable) {
            switch (callable) {
                case DMASTCallableSelf callableSelf: return new Expressions.ProcSelf(callableSelf.Location);
                case DMASTCallableSuper callableSuper: return new Expressions.ProcSuper(callableSuper.Location);
                case DMASTCallableGlobalProc callableGlobal: return new Expressions.GlobalProc(callableGlobal.Location, callableGlobal.Identifier);
                case DMASTDereferenceProc derefProc: return BuildDereferenceProc(derefProc);
                case DMASTDereference deref: return BuildDereference(deref);
                case DMASTCallableProcIdentifier procIdentifier:
                    if (_scopeMode == "static") {
                        return new Expressions.GlobalProc(procIdentifier.Location, procIdentifier.Identifier);
                    } else {
                        if (_dmObject.HasProc(procIdentifier.Identifier)) {
                            return new Expressions.Proc(procIdentifier.Location, procIdentifier.Identifier);
                        } else if (DMObjectTree.TryGetGlobalProc(procIdentifier.Identifier, out _)) {
                            return new Expressions.GlobalProc(procIdentifier.Location, procIdentifier.Identifier);
                        } else {
                            throw new CompileErrorException(procIdentifier.Location, $"Type {_dmObject.Path} does not have a proc named \"{procIdentifier.Identifier}\"");
                        }
                    }
                default:
                    throw new CompileErrorException(callable.Location, $"Failed to build proc expression from AST node of type {callable.GetType()}");
            }
        }

        private DMExpression BuildStringFormat(DMASTStringFormat stringFormat) {
            var expressions = new DMExpression[stringFormat.InterpolatedValues.Length];

            for (int i = 0; i < stringFormat.InterpolatedValues.Length; i++) {
                if (stringFormat.InterpolatedValues[i] is null) {
                    expressions[i] = new Expressions.Null(stringFormat.Location);
                } else {
                    expressions[i] = DMExpression.Create(_dmObject, _proc, stringFormat.InterpolatedValues[i], _inferredPath);
                }
            }

            return new Expressions.StringFormat(stringFormat.Location, stringFormat.Value, expressions);
        }

        private DMExpression BuildIdentifier(DMASTIdentifier identifier)
        {
            var name = identifier.Identifier;

            switch (name)
            {
                case "src": return new Expressions.Src(identifier.Location, _dmObject.Path);
                case "usr": return new Expressions.Usr(identifier.Location);
                case "args": return new Expressions.Args(identifier.Location);
                default:
                    DMProc.LocalVariable localVar = _proc?.GetLocalVariable(name);
                    if (localVar != null && _scopeMode == "normal") {
                        return new Expressions.Local(identifier.Location, localVar);
                    }

                    int? procGlobalId = _proc?.GetGlobalVariableId(name);
                    if (procGlobalId != null)
                    {
                        return new Expressions.GlobalField(identifier.Location, DMObjectTree.Globals[procGlobalId.Value].Type, procGlobalId.Value);
                    }

                    var field = _dmObject?.GetVariable(name);
                    if (field != null && _scopeMode == "normal") {
                        return new Expressions.Field(identifier.Location, field);
                    }

                    int? globalId = _dmObject?.GetGlobalVariableId(name);
                    if (globalId != null) {
                        return new Expressions.GlobalField(identifier.Location, DMObjectTree.Globals[globalId.Value].Type, globalId.Value);
                    }

                    DMCompiler.Error(identifier.Location, $"Unknown identifier \"{name}\"");
                    return new Expressions.Null(identifier.Location);
            }
        }

        private DMExpression BuildGlobalIdentifier(DMASTGlobalIdentifier globalIdentifier) {
            string name = globalIdentifier.Identifier;

            int? globalId = _dmObject?.GetGlobalVariableId(name);
            if (globalId != null) {
                return new Expressions.GlobalField(globalIdentifier.Location, DMObjectTree.Globals[globalId.Value].Type, globalId.Value);
            }

            DMCompiler.Error(globalIdentifier.Location, $"Unknown global \"{name}\"");
            return new Expressions.Null(globalIdentifier.Location);
        }

        private DMExpression BuildProcCall(DMASTProcCall procCall) {
            // arglist hack
            if (procCall.Callable is DMASTCallableProcIdentifier ident) {
                if (ident.Identifier == "arglist") {
                    if (procCall.Parameters.Length != 1) throw new CompileErrorException(procCall.Location, "arglist must have 1 argument");

                    var expr = DMExpression.Create(_dmObject, _proc, procCall.Parameters[0].Value, _inferredPath);
                    return new Expressions.Arglist(procCall.Location, expr);
                }
            }

            var target = DMExpression.Create(_dmObject, _proc, (DMASTExpression)procCall.Callable, _inferredPath);
            var args = new ArgumentList(procCall.Location, _dmObject, _proc, procCall.Parameters);
            return new Expressions.ProcCall(procCall.Location, target, args);
        }

        private DMExpression BuildUnaryExpression(DMASTExpression expression) {
            DMASTUnary unary = (DMASTUnary)expression;
            DMExpression expr = DMExpression.Create(_dmObject, _proc, unary.Expression, _inferredPath);

            switch (unary) {
                case DMASTNot: return new Expressions.Not(expression.Location, expr);
                case DMASTNegate: return new Expressions.Negate(expression.Location, expr);
                case DMASTBinaryNot: return new Expressions.BinaryNot(expression.Location, expr);
                case DMASTPreIncrement: return new Expressions.PreIncrement(expression.Location, expr);
                case DMASTPostIncrement: return new Expressions.PostIncrement(expression.Location, expr);
                case DMASTPreDecrement: return new Expressions.PreDecrement(expression.Location, expr);
                case DMASTPostDecrement: return new Expressions.PostDecrement(expression.Location, expr);
                case DMASTIsSaved: return new Expressions.IsSaved(expression.Location, expr);
                case DMASTInitial: return new Expressions.Initial(expression.Location, expr);
                case DMASTImplicitIsType:
                    if (expr.Path is null) {
                        DMCompiler.Error(expression.Location, "An inferred istype requires a type!");
                        return new Expressions.Null(expression.Location);
                    } else {
                        return new Expressions.IsTypeInferred(expression.Location, expr, expr.Path.Value);
                    }
            }

            DMCompiler.Error(expression.Location, $"Failed to build an expression from unary AST node of type {expression.GetType()}");
            return new Expressions.Null(expression.Location);
        }

        private DMExpression BuildBinaryExpression(DMASTExpression expression) {
            DMASTBinary binary = (DMASTBinary)expression;
            DMExpression lhs = DMExpression.Create(_dmObject, _proc, binary.LHS, _inferredPath);
            DMExpression rhs = DMExpression.Create(_dmObject, _proc, binary.RHS, lhs.Path);

            switch (binary) {
                case DMASTAssign: return new Expressions.Assignment(expression.Location, lhs, rhs);
                case DMASTAdd: return new Expressions.Add(expression.Location, lhs, rhs);
                case DMASTSubtract: return new Expressions.Subtract(expression.Location, lhs, rhs);
                case DMASTMultiply: return new Expressions.Multiply(expression.Location, lhs, rhs);
                case DMASTDivide: return new Expressions.Divide(expression.Location, lhs, rhs);
                case DMASTModulus: return new Expressions.Modulo(expression.Location, lhs, rhs);
                case DMASTPower: return new Expressions.Power(expression.Location, lhs, rhs);
                case DMASTAppend: return new Expressions.Append(expression.Location, lhs, rhs);
                case DMASTCombine: return new Expressions.Combine(expression.Location, lhs, rhs);
                case DMASTRemove: return new Expressions.Remove(expression.Location, lhs, rhs);
                case DMASTMask: return new Expressions.Mask(expression.Location, lhs, rhs);
                case DMASTLogicalAndAssign: return new Expressions.LogicalAndAssign(expression.Location, lhs, rhs);
                case DMASTLogicalOrAssign: return new Expressions.LogicalOrAssign(expression.Location, lhs, rhs);
                case DMASTMultiplyAssign: return new Expressions.MultiplyAssign(expression.Location, lhs, rhs);
                case DMASTDivideAssign: return new Expressions.DivideAssign(expression.Location, lhs, rhs);
                case DMASTLeftShiftAssign: return new Expressions.LeftShiftAssign(expression.Location, lhs, rhs);
                case DMASTRightShiftAssign: return new Expressions.RightShiftAssign(expression.Location, lhs, rhs);
                case DMASTXorAssign: return new Expressions.XorAssign(expression.Location, lhs, rhs);
                case DMASTModulusAssign: return new Expressions.ModulusAssign(expression.Location, lhs, rhs);
                case DMASTLeftShift: return new Expressions.LeftShift(expression.Location, lhs, rhs);
                case DMASTRightShift: return new Expressions.RightShift(expression.Location, lhs, rhs);
                case DMASTBinaryAnd: return new Expressions.BinaryAnd(expression.Location, lhs, rhs);
                case DMASTBinaryXor: return new Expressions.BinaryXor(expression.Location, lhs, rhs);
                case DMASTBinaryOr: return new Expressions.BinaryOr(expression.Location, lhs, rhs);
                case DMASTEqual: return new Expressions.Equal(expression.Location, lhs, rhs);
                case DMASTNotEqual: return new Expressions.NotEqual(expression.Location, lhs, rhs);
                case DMASTEquivalent: return new Expressions.Equivalent(expression.Location, lhs, rhs);
                case DMASTNotEquivalent: return new Expressions.NotEquivalent(expression.Location, lhs, rhs);
                case DMASTGreaterThan: return new Expressions.GreaterThan(expression.Location, lhs, rhs);
                case DMASTGreaterThanOrEqual: return new Expressions.GreaterThanOrEqual(expression.Location, lhs, rhs);
                case DMASTLessThan: return new Expressions.LessThan(expression.Location, lhs, rhs);
                case DMASTLessThanOrEqual: return new Expressions.LessThanOrEqual(expression.Location, lhs, rhs);
                case DMASTOr: return new Expressions.Or(expression.Location, lhs, rhs);
                case DMASTAnd: return new Expressions.And(expression.Location, lhs, rhs);
                case DMASTIsType: return new Expressions.IsType(expression.Location, lhs, rhs);
                case DMASTExpressionIn: return new Expressions.In(expression.Location, lhs, rhs);
            }

            DMCompiler.Error(expression.Location, $"Failed to build an expression from binary AST node of type {expression.GetType()}");
            return new Expressions.Null(expression.Location);
        }

        private DMExpression BuildTernary(DMASTTernary ternary) {
            var a = DMExpression.Create(_dmObject, _proc, ternary.A, _inferredPath);
            var b = DMExpression.Create(_dmObject, _proc, ternary.B, _inferredPath);
            var c = DMExpression.Create(_dmObject, _proc, ternary.C ?? new DMASTConstantNull(ternary.Location), _inferredPath);
            return new Expressions.Ternary(ternary.Location, a, b, c);
        }

        private DMExpression BuildListIndex(DMASTListIndex listIndex) {
            var expr = DMExpression.Create(_dmObject, _proc, listIndex.Expression, _inferredPath);
            var index = DMExpression.Create(_dmObject, _proc, listIndex.Index, expr.Path);
            return new Expressions.ListIndex(listIndex.Location, expr, index, expr.Path, listIndex.Conditional);
        }

        private DMExpression BuildDereference(DMASTDereference dereference) {
            var expr = DMExpression.Create(_dmObject, _proc, dereference.Expression, _inferredPath);

            if (dereference.Type == DMASTDereference.DereferenceType.Direct && !Dereference.DirectConvertable(expr, dereference)) {
                if (expr.Path == null) {
                    throw new CompileErrorException(dereference.Location, $"Invalid property \"{dereference.Property}\"");
                }

                DMObject dmObject = DMObjectTree.GetDMObject(expr.Path.Value, false);
                if (dmObject == null) throw new CompileErrorException(dereference.Location, $"Type {expr.Path.Value} does not exist");

                DMExpression value;
                var property = dmObject.GetVariable(dereference.Property);
                if (property != null) {
                    value = new Expressions.Dereference(dereference.Location, property.Type, expr, dereference.Conditional, dereference.Property);
                } else {
                    var globalId = dmObject.GetGlobalVariableId(dereference.Property);
                    if (globalId != null) {
                        property = DMObjectTree.Globals[globalId.Value];
                        value = new Expressions.GlobalField(dereference.Location, property.Type, globalId.Value);
                    } else {
                        throw new CompileErrorException(dereference.Location, $"Invalid property \"{dereference.Property}\" on type {dmObject.Path}");
                    }
                }

                if ((property.Value?.ValType & DMValueType.Unimplemented) == DMValueType.Unimplemented) {
                    DMCompiler.UnimplementedWarning(dereference.Location, $"{dmObject.Path}.{dereference.Property} is not implemented and will have unexpected behavior");
                }

                return value;
            } else {
                return new Expressions.Dereference(dereference.Location, null, expr, dereference.Conditional, dereference.Property);
            }
        }

        private DMExpression BuildDereferenceProc(DMASTDereferenceProc dereferenceProc) {
            var expr = DMExpression.Create(_dmObject, _proc, dereferenceProc.Expression, _inferredPath);
            return new Expressions.DereferenceProc(dereferenceProc.Location, expr, dereferenceProc);
        }

        private DMExpression BuildNewPath(DMASTNewPath newPath) {
            var args = new ArgumentList(newPath.Location, _dmObject, _proc, newPath.Parameters, _inferredPath);
            return new Expressions.NewPath(newPath.Location, newPath.Path.Path, args);
        }

        private DMExpression BuildNewInferred(DMASTNewInferred newInferred) {
            if (_inferredPath is null) {
                DMCompiler.Error(newInferred.Location, "An inferred new requires a type!");
                return new Expressions.Null(newInferred.Location);
            }

            var args = new ArgumentList(newInferred.Location, _dmObject, _proc, newInferred.Parameters, _inferredPath);
            return new Expressions.NewPath(newInferred.Location, _inferredPath.Value, args);
        }

        private DMExpression BuildNewIdentifier(DMASTNewIdentifier newIdentifier) {
            var expr = DMExpression.Create(_dmObject, _proc, newIdentifier.Identifier, _inferredPath);
            var args = new ArgumentList(newIdentifier.Location, _dmObject, _proc, newIdentifier.Parameters, _inferredPath);
            return new Expressions.New(newIdentifier.Location, expr, args);
        }

        private DMExpression BuildNewDereference(DMASTNewDereference newDereference) {
            var expr = DMExpression.Create(_dmObject, _proc, newDereference.Dereference, _inferredPath);
            var args = new ArgumentList(newDereference.Location, _dmObject, _proc, newDereference.Parameters, _inferredPath);
            return new Expressions.New(newDereference.Location, expr, args);
        }

        private DMExpression BuildNewListIndex(DMASTNewListIndex newListIdx) {
            var expr = DMExpression.Create(_dmObject, _proc, newListIdx.ListIdx, _inferredPath);
            var args = new ArgumentList(newListIdx.Location, _dmObject, _proc, newListIdx.Parameters, _inferredPath);
            return new Expressions.New(newListIdx.Location, expr, args);
        }

        private DMExpression BuildLocate(DMASTLocate locate) {
            var container = locate.Container != null ? DMExpression.Create(_dmObject, _proc, locate.Container, _inferredPath) : null;

            if (locate.Expression == null) {
                if (_inferredPath == null) {
                    DMCompiler.Error(locate.Location, "An inferred locate requires a type!");
                    return new Expressions.Null(locate.Location);
                }

                return new Expressions.LocateInferred(locate.Location, _inferredPath.Value, container);
            }

            var pathExpr = DMExpression.Create(_dmObject, _proc, locate.Expression, _inferredPath);
            return new Expressions.Locate(locate.Location, pathExpr, container);
        }

        private DMExpression BuildLocateCoordinates(DMASTLocateCoordinates locateCoordinates) {
            var _x = DMExpression.Create(_dmObject, _proc, locateCoordinates.X, _inferredPath);
            var _y = DMExpression.Create(_dmObject, _proc, locateCoordinates.Y, _inferredPath);
            var _z = DMExpression.Create(_dmObject, _proc, locateCoordinates.Z, _inferredPath);
            return new Expressions.LocateCoordinates(locateCoordinates.Location, _x, _y, _z);
        }

        private DMExpression BuildList(DMASTList list) {
            return new Expressions.List(list.Location, list);
        }

        private DMExpression BuildNewList(DMASTNewList newList) {
            DMExpression[] expressions = new DMExpression[newList.Parameters.Length];

            for (int i = 0; i < newList.Parameters.Length; i++) {
                DMASTCallParameter parameter = newList.Parameters[i];

                if (parameter.Name == null) {
                    expressions[i] = DMExpression.Create(_dmObject, _proc, parameter.Value, _inferredPath);
                } else {
                    DMCompiler.Error(parameter.Location, "newlist() does not take named arguments");
                    expressions[i] = new Expressions.Null(parameter.Location);
                }
            }

            return new Expressions.NewList(newList.Location, expressions);
        }

        private DMExpression BuildAddText(DMASTAddText addText) {
            if (addText.Parameters.Length < 2) {
                DMCompiler.Error(addText.Location, "Invalid addtext() parameter count; expected 2 or more arguments");
                return new Expressions.Null(addText.Location);
            }

            DMExpression[] expArr = new DMExpression[addText.Parameters.Length];
            for (int i = 0; i < expArr.Length; i++)
            {
                DMASTCallParameter parameter = addText.Parameters[i];

                if(parameter.Name == null) {
                    expArr[i] = DMExpression.Create(_dmObject, _proc, parameter.Value, _inferredPath);
                } else {
                    DMCompiler.Error(parameter.Location, "addtext() does not take named arguments");
                    expArr[i] = new Expressions.Null(parameter.Location);
                }
            }

            return new Expressions.AddText(addText.Location, expArr);
        }

        private DMExpression BuildInput(DMASTInput input) {
            return new Expressions.Input(input.Location, input);
        }

        private DMExpression BuildInRange(DMASTExpressionInRange inRange) {
            var value = DMExpression.Create(_dmObject, _proc, inRange.Value, _inferredPath);
            var startRange = DMExpression.Create(_dmObject, _proc, inRange.StartRange, _inferredPath);
            var endRange = DMExpression.Create(_dmObject, _proc, inRange.EndRange, _inferredPath);
            return new Expressions.InRange(inRange.Location, value, startRange, endRange);
        }

        private DMExpression BuildPick(DMASTPick pick) {
            Expressions.Pick.PickValue[] pickValues = new Expressions.Pick.PickValue[pick.Values.Length];
            for (int i = 0; i < pickValues.Length; i++) {
                DMASTPick.PickValue pickValue = pick.Values[i];
                DMExpression weight = (pickValue.Weight != null) ? DMExpression.Create(_dmObject, _proc, pickValue.Weight) : null;
                DMExpression value = DMExpression.Create(_dmObject, _proc, pickValue.Value);

                pickValues[i] = new Expressions.Pick.PickValue(weight, value);
            }

            return new Expressions.Pick(pick.Location, pickValues);
        }

        private DMExpression BuildCall(DMASTCall call) {
            var procArgs = new ArgumentList(call.Location, _dmObject, _proc, call.ProcParameters, _inferredPath);

            switch (call.CallParameters.Length) {
                case 1:
                {
                    var a = DMExpression.Create(_dmObject, _proc, call.CallParameters[0].Value, _inferredPath);
                    return new Expressions.CallStatement(call.Location, a, procArgs);
                }
                case 2:
                {
                    var a = DMExpression.Create(_dmObject, _proc, call.CallParameters[0].Value, _inferredPath);
                    var b = DMExpression.Create(_dmObject, _proc, call.CallParameters[1].Value, _inferredPath);
                    return new Expressions.CallStatement(call.Location, a, b, procArgs);
                }
                default:
                    DMCompiler.Error(call.Location, "Invalid call() parameter count; expected 1 or 2 arguments");
                    return new Expressions.Null(call.Location);
            }
        }
    }
}
