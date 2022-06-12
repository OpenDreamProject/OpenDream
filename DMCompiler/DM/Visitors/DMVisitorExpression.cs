using System;
using DMCompiler.DM.Expressions;
using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.DM.Visitors {
    sealed class DMVisitorExpression : DMASTVisitor {
        DMObject _dmObject { get; }
        DMProc _proc { get; }
        DreamPath? _inferredPath { get; }
        internal DMExpression Result { get; private set; }

        // NOTE This needs to be turned into a Stack of modes if more complicated scope changes are added in the future
        public static string _scopeMode;

        internal DMVisitorExpression(DMObject dmObject, DMProc proc, DreamPath? inferredPath)
        {
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
            Result = new Expressions.Path(constant.Location, constant.Value.Path);
        }

        public void VisitUpwardPathSearch(DMASTUpwardPathSearch constant) {
            DMExpression.TryConstant(_dmObject, _proc, constant.Path, out var pathExpr);
            if (pathExpr is not Expressions.Path expr) throw new CompileErrorException(constant.Location, "Cannot do an upward path search on " + pathExpr);

            DreamPath path = expr.Value;
            DreamPath? foundPath = DMObjectTree.UpwardSearch(path, constant.Search.Path);

            if (foundPath == null) {
                throw new CompileErrorException(constant.Location,$"Invalid path {path}.{constant.Search.Path}");
            }

            Result = new Expressions.Path(constant.Location, foundPath.Value);
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


        public void VisitIdentifier(DMASTIdentifier identifier)
        {
            var name = identifier.Identifier;

            switch (name)
            {
                case "src":
                    Result = new Expressions.Src(identifier.Location, _dmObject.Path);
                    break;
                case "usr":
                    Result = new Expressions.Usr(identifier.Location);
                    break;
                case "args":
                    Result = new Expressions.Args(identifier.Location);
                    break;
                default:
                {
                    DMProc.LocalVariable localVar = _proc?.GetLocalVariable(name);
                    if (localVar != null && _scopeMode == "normal") {
                        Result = new Expressions.Local(identifier.Location, localVar);
                        return;
                    }

                    int? procGlobalId = _proc?.GetGlobalVariableId(name);
                    if (procGlobalId != null)
                    {
                        Result = new Expressions.GlobalField(identifier.Location, DMObjectTree.Globals[procGlobalId.Value].Type, procGlobalId.Value);
                        return;
                    }

                    var field = _dmObject?.GetVariable(name);
                    if (field != null && _scopeMode == "normal") {
                        Result = new Expressions.Field(identifier.Location, field);
                        return;
                    }

                    int? globalId = _dmObject?.GetGlobalVariableId(name);
                    if (globalId != null) {
                        Result = new Expressions.GlobalField(identifier.Location, DMObjectTree.Globals[globalId.Value].Type, globalId.Value);
                        return;
                    }

                    throw new CompileErrorException(identifier.Location, $"Unknown identifier \"{name}\"");
                }
            }
        }

        public void VisitGlobalIdentifier(DMASTGlobalIdentifier globalIdentifier) {
            string name = globalIdentifier.Identifier;

            int? globalId = _dmObject?.GetGlobalVariableId(name);
            if (globalId != null) {
                Result = new Expressions.GlobalField(globalIdentifier.Location, DMObjectTree.Globals[globalId.Value].Type, globalId.Value);
                return;
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
            if (_scopeMode == "static") {
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
                    if (procCall.Parameters.Length != 1) throw new CompileErrorException(procCall.Location, "arglist must have 1 argument");

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
            var rhs = DMExpression.Create(_dmObject, _proc, assign.Value, lhs.Path);
            Result = new Expressions.Assignment(assign.Location, lhs, rhs);
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
            var rhs = DMExpression.Create(_dmObject, _proc, land.B, lhs.Path);
            Result = new Expressions.LogicalAndAssign(land.Location, lhs, rhs);
        }
        public void VisitLogicalOrAssign(DMASTLogicalOrAssign lor) {
            var lhs = DMExpression.Create(_dmObject, _proc, lor.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, lor.B, lhs.Path);
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
            Result = new Expressions.Equal(equal.Location, lhs, rhs);
        }

        public void VisitNotEqual(DMASTNotEqual notEqual) {
            var lhs = DMExpression.Create(_dmObject, _proc, notEqual.A, _inferredPath);
            var rhs = DMExpression.Create(_dmObject, _proc, notEqual.B, _inferredPath);
            Result = new Expressions.NotEqual(notEqual.Location, lhs, rhs);
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

        public void VisitListIndex(DMASTListIndex listIndex) {
            var expr = DMExpression.Create(_dmObject, _proc, listIndex.Expression, _inferredPath);
            var index = DMExpression.Create(_dmObject, _proc, listIndex.Index, expr.Path);
            Result = new Expressions.ListIndex(listIndex.Location, expr, index, expr.Path, listIndex.Conditional);
        }

        public void VisitDereference(DMASTDereference dereference) {
            var expr = DMExpression.Create(_dmObject, _proc, dereference.Expression, _inferredPath);

            if (dereference.Type == DMASTDereference.DereferenceType.Direct && !Dereference.DirectConvertable(expr, dereference)) {
                if (expr.Path == null) {
                    throw new CompileErrorException(dereference.Location, $"Invalid property \"{dereference.Property}\"");
                }

                DMObject dmObject = DMObjectTree.GetDMObject(expr.Path.Value, false);
                if (dmObject == null) throw new CompileErrorException(dereference.Location, $"Type {expr.Path.Value} does not exist");

                var property = dmObject.GetVariable(dereference.Property);
                if (property != null) {
                    Result = new Expressions.Dereference(dereference.Location, property.Type, expr, dereference.Conditional, dereference.Property);
                } else {
                    var globalId = dmObject.GetGlobalVariableId(dereference.Property);
                    if (globalId != null) {
                        property = DMObjectTree.Globals[globalId.Value];
                        Result = new Expressions.GlobalField(dereference.Location, property.Type, globalId.Value);
                    }
                }

                if (property == null) {
                    throw new CompileErrorException(dereference.Location, $"Invalid property \"{dereference.Property}\" on type {dmObject.Path}");
                }

                if ((property.Value?.ValType & DMValueType.Unimplemented) == DMValueType.Unimplemented) {
                    DMCompiler.UnimplementedWarning(dereference.Location, $"{dmObject.Path}.{dereference.Property} is not implemented and will have unexpected behavior");
                }
            } else {
                Result = new Expressions.Dereference(dereference.Location, null, expr, dereference.Conditional, dereference.Property);
            }
        }

        public void VisitDereferenceProc(DMASTDereferenceProc dereferenceProc) {
            var expr = DMExpression.Create(_dmObject, _proc, dereferenceProc.Expression, _inferredPath);
            Result = new Expressions.DereferenceProc(dereferenceProc.Location, expr, dereferenceProc);
        }

        public void VisitNewPath(DMASTNewPath newPath) {
            var args = new ArgumentList(newPath.Location, _dmObject, _proc, newPath.Parameters, _inferredPath);
            Result = new Expressions.NewPath(newPath.Location, newPath.Path.Path, args);
        }

        public void VisitNewMultidimensionalList(DMASTNewMultidimensionalList newList)
        {
            DMExpression[] expressions = new DMExpression[newList.Dimensions.Length];
            for (int i = 0; i < newList.Dimensions.Length; i++)
            {
                expressions[i] = DMExpression.Create(_dmObject, _proc, newList.Dimensions[i], _inferredPath);
            }

            Result = new Expressions.NewMultidimensionalList(newList.Location, expressions);
        }

        public void VisitNewInferred(DMASTNewInferred newInferred) {
            if (_inferredPath is null) {
                throw new CompileErrorException(newInferred.Location, "An inferred new requires a type!");
            }

            var args = new ArgumentList(newInferred.Location, _dmObject, _proc, newInferred.Parameters, _inferredPath);
            Result = new Expressions.NewPath(newInferred.Location, _inferredPath.Value, args);
        }

        public void VisitNewIdentifier(DMASTNewIdentifier newIdentifier) {
            var expr = DMExpression.Create(_dmObject, _proc, newIdentifier.Identifier, _inferredPath);
            var args = new ArgumentList(newIdentifier.Location, _dmObject, _proc, newIdentifier.Parameters, _inferredPath);
            Result = new Expressions.New(newIdentifier.Location, expr, args);
        }

        public void VisitNewDereference(DMASTNewDereference newDereference) {
            var expr = DMExpression.Create(_dmObject, _proc, newDereference.Dereference, _inferredPath);
            var args = new ArgumentList(newDereference.Location, _dmObject, _proc, newDereference.Parameters, _inferredPath);
            Result = new Expressions.New(newDereference.Location, expr, args);
        }

        public void VisitNewListIndex(DMASTNewListIndex newListIdx) {
            var expr = DMExpression.Create(_dmObject, _proc, newListIdx.ListIdx, _inferredPath);
            var args = new ArgumentList(newListIdx.Location, _dmObject, _proc, newListIdx.Parameters, _inferredPath);
            Result = new Expressions.New(newListIdx.Location, expr, args);
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

        public void VisitLocateCoordinates(DMASTLocateCoordinates locateCoordinates) {
            var _x = DMExpression.Create(_dmObject, _proc, locateCoordinates.X, _inferredPath);
            var _y = DMExpression.Create(_dmObject, _proc, locateCoordinates.Y, _inferredPath);
            var _z = DMExpression.Create(_dmObject, _proc, locateCoordinates.Z, _inferredPath);
            Result = new Expressions.LocateCoordinates(locateCoordinates.Location, _x, _y, _z);
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

        public void VisitList(DMASTList list) {
            Result = new Expressions.List(list.Location, list);
        }

        public void VisitNewList(DMASTNewList newList) {
            DMExpression[] expressions = new DMExpression[newList.Parameters.Length];

            for (int i = 0; i < newList.Parameters.Length; i++) {
                DMASTCallParameter parameter = newList.Parameters[i];
                if (parameter.Name != null) throw new CompileErrorException(newList.Location,"newlist() does not take named arguments");

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
                if(parameter.Name != null)
                    throw new CompileErrorException(parameter.Location, "addtext() does not take named arguments");
                exp_arr[i] = DMExpression.Create(_dmObject,_proc, parameter.Value, _inferredPath);
            }
            Result = new Expressions.AddText(addText.Location, exp_arr);
        }

        public void VisitInput(DMASTInput input) {
            Result = new Expressions.Input(input.Location, input);
        }

        public void VisitInitial(DMASTInitial initial) {
            var expr = DMExpression.Create(_dmObject, _proc, initial.Expression, _inferredPath);
            Result = new Expressions.Initial(initial.Location, expr);
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
                DMExpression weight = (pickValue.Weight != null) ? DMExpression.Create(_dmObject, _proc, pickValue.Weight) : null;
                DMExpression value = DMExpression.Create(_dmObject, _proc, pickValue.Value);

                pickValues[i] = new Expressions.Pick.PickValue(weight, value);
            }

            Result = new Expressions.Pick(pick.Location, pickValues);
        }

        public void VisitCall(DMASTCall call) {
            var procArgs = new ArgumentList(call.Location, _dmObject, _proc, call.ProcParameters, _inferredPath);

            switch (call.CallParameters.Length) {
                case 1:
                    {
                        var a = DMExpression.Create(_dmObject, _proc, call.CallParameters[0].Value, _inferredPath);
                        Result = new Expressions.CallStatement(call.Location, a, procArgs);
                    }
                    break;

                case 2:
                    {
                        var a = DMExpression.Create(_dmObject, _proc, call.CallParameters[0].Value, _inferredPath);
                        var b = DMExpression.Create(_dmObject, _proc, call.CallParameters[1].Value, _inferredPath);
                        Result = new Expressions.CallStatement(call.Location, a, b, procArgs);
                    }
                    break;

                default:
                    throw new CompileErrorException(call.Location,"invalid argument count for call()");
            }
        }
    }
}
