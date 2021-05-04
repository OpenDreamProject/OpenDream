using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using System;
using System.Collections.Generic;

namespace DMCompiler.DM.Visitors {
    class DMVisitorProcBuilder : DMASTVisitor {
        private DMObject _dmObject;
        private DMProc _proc;
        private Stack<object> _valueStack = new();
        private int _labelIdCounter = 0;
        private DMVariable _currentVariable = null;

        public DMVisitorProcBuilder(DMObject dmObject, DMProc proc) {
            _dmObject = dmObject;
            _proc = proc;
        }

        public void VisitProcDefinition(DMASTProcDefinition procDefinition) {
            if (procDefinition.Body != null) {
                foreach (DMASTDefinitionParameter parameter in procDefinition.Parameters) {
                    string parameterName = parameter.Name;

                    _proc.AddLocalVariable(parameterName, parameter.ObjectType);
                    if (parameter.Value != null) {
                        string afterDefaultValueCheck = NewLabelName();

                        _proc.PushLocalVariable(parameterName);
                        _proc.IsNull();
                        _proc.JumpIfFalse(afterDefaultValueCheck);

                        _proc.PushLocalVariable(parameterName);
                        _currentVariable = new DMVariable(parameter.ObjectType, parameterName, false);
                        parameter.Value.Visit(this);
                        _proc.Assign();

                        _proc.AddLabel(afterDefaultValueCheck);
                    }
                }

                _valueStack.Clear();
                procDefinition.Body.Visit(this);
                _proc.ResolveLabels();
            }
        }

        public void VisitProcBlockInner(DMASTProcBlockInner block) {
            foreach (DMASTProcStatement statement in block.Statements) {
                statement.Visit(this);
            }
        }

        public void VisitProcStatementExpression(DMASTProcStatementExpression statement) {
            statement.Expression.Visit(this);
        }

        public void VisitProcStatementContinue(DMASTProcStatementContinue statementContinue) {
            _proc.Continue();
        }

        public void VisitProcStatementBreak(DMASTProcStatementBreak statementBreak) {
            _proc.Break();
        }

        public void VisitProcStatementSet(DMASTProcStatementSet statementSet) {
            //TODO: Proc attributes
        }

        public void VisitProcStatementDel(DMASTProcStatementDel statementDel) {
            statementDel.Value.Visit(this);
            _proc.DeleteObject();
        }

        public void VisitProcStatementVarDeclaration(DMASTProcStatementVarDeclaration varDeclaration) {
            _currentVariable = new DMVariable(varDeclaration.Type, varDeclaration.Name, false);
            _proc.AddLocalVariable(varDeclaration.Name, varDeclaration.Type);

            if (varDeclaration.Value != null) {
                varDeclaration.Value.Visit(this);
            } else {
                _proc.PushNull();
            }

            _proc.SetLocalVariable(varDeclaration.Name);
        }

        public void VisitProcStatementReturn(DMASTProcStatementReturn statement) {
            if (statement.Value != null) {
                statement.Value.Visit(this);
            } else {
                _proc.PushSelf(); //Default return value
            }

            _proc.Return();
        }

        public void VisitProcStatementIf(DMASTProcStatementIf statement) {
            statement.Condition.Visit(this);
            
            if (statement.ElseBody == null) {
                string endLabel = NewLabelName();

                _proc.JumpIfFalse(endLabel);
                _proc.StartScope();
                statement.Body.Visit(this);
                _proc.EndScope();
                _proc.AddLabel(endLabel);
            } else {
                string elseLabel = NewLabelName();
                string endLabel = NewLabelName();

                _proc.JumpIfFalse(elseLabel);

                _proc.StartScope();
                statement.Body.Visit(this);
                _proc.EndScope();
                _proc.Jump(endLabel);

                _proc.AddLabel(elseLabel);
                statement.ElseBody.Visit(this);
                _proc.AddLabel(endLabel);
            }
        }

        public void VisitProcStatementForStandard(DMASTProcStatementForStandard statementForStandard) {
            _proc.StartScope();
            {
                if (statementForStandard.Initializer != null) statementForStandard.Initializer.Visit(this);

                string loopLabel = NewLabelName();
                _proc.LoopStart(loopLabel);
                {
                    statementForStandard.Comparator.Visit(this);
                    _proc.BreakIfFalse();

                    statementForStandard.Body.Visit(this);

                    _proc.LoopContinue(loopLabel);
                    statementForStandard.Incrementor.Visit(this);
                    _proc.LoopJumpToStart(loopLabel);
                }
                _proc.LoopEnd();
            }
            _proc.EndScope();
        }

        public void VisitProcStatementForList(DMASTProcStatementForList statementForList) {
            statementForList.List.Visit(this);
            _proc.CreateListEnumerator();
            _proc.StartScope();
            {
                if (statementForList.Initializer != null) statementForList.Initializer.Visit(this);

                string loopLabel = NewLabelName();
                _proc.LoopStart(loopLabel);
                {
                    _proc.Enumerate(statementForList.Variable.Identifier);
                    _proc.BreakIfFalse();

                    DMASTProcStatementVarDeclaration varDeclaration = statementForList.Initializer as DMASTProcStatementVarDeclaration;
                    if (varDeclaration != null && varDeclaration.Type != null) {
                        statementForList.Variable.Visit(this);
                        _proc.PushPath(varDeclaration.Type.Value);
                        _proc.IsType();

                        _proc.ContinueIfFalse();
                    }

                    statementForList.Body.Visit(this);

                    _proc.LoopContinue(loopLabel);
                    _proc.LoopJumpToStart(loopLabel);
                }
                _proc.LoopEnd();
            }
            _proc.EndScope();
            _proc.DestroyEnumerator();
        }

        public void VisitProcStatementForRange(DMASTProcStatementForRange statementForRange) {
            statementForRange.RangeStart.Visit(this);
            statementForRange.RangeEnd.Visit(this);
            statementForRange.Step.Visit(this);
            _proc.CreateRangeEnumerator();
            _proc.StartScope();
            {
                if (statementForRange.Initializer != null) statementForRange.Initializer.Visit(this);

                string loopLabel = NewLabelName();
                _proc.LoopStart(loopLabel);
                {
                    _proc.Enumerate(statementForRange.Variable.Identifier);
                    _proc.BreakIfFalse();

                    statementForRange.Body.Visit(this);

                    _proc.LoopContinue(loopLabel);
                    _proc.LoopJumpToStart(loopLabel);
                }
                _proc.LoopEnd();
            }
            _proc.EndScope();
            _proc.DestroyEnumerator();
        }

        public void VisitProcStatementWhile(DMASTProcStatementWhile statementWhile) {
            string loopLabel = NewLabelName();

            _proc.LoopStart(loopLabel);
            {
                statementWhile.Conditional.Visit(this);
                _proc.BreakIfFalse();

                _proc.StartScope();
                {
                    statementWhile.Body.Visit(this);

                    _proc.LoopContinue(loopLabel);
                    _proc.LoopJumpToStart(loopLabel);
                }
                _proc.EndScope();
            }
            _proc.LoopEnd();
        }

        public void VisitProcStatementDoWhile(DMASTProcStatementDoWhile statementDoWhile) {
            string loopLabel = NewLabelName();
            string loopEndLabel = NewLabelName();

            _proc.LoopStart(loopLabel);
            {
                statementDoWhile.Body.Visit(this);

                _proc.LoopContinue(loopLabel);
                statementDoWhile.Conditional.Visit(this);
                _proc.JumpIfFalse(loopEndLabel);
                _proc.LoopJumpToStart(loopLabel);

                _proc.AddLabel(loopEndLabel);
                _proc.Break();
            }
            _proc.LoopEnd();
        }

        public void VisitProcStatementSwitch(DMASTProcStatementSwitch statementSwitch) {
            string endLabel = NewLabelName();
            List<(string CaseLabel, DMASTProcBlockInner CaseBody)> valueCases = new();
            DMASTProcBlockInner defaultCaseBody = null;

            statementSwitch.Value.Visit(this);
            foreach (DMASTProcStatementSwitch.SwitchCase switchCase in statementSwitch.Cases) {
                if (switchCase is DMASTProcStatementSwitch.SwitchCaseValues) {
                    string caseLabel = NewLabelName();

                    foreach (DMASTExpressionConstant value in ((DMASTProcStatementSwitch.SwitchCaseValues)switchCase).Values) {
                        value.Visit(this);
                        _proc.SwitchCase(caseLabel);
                    }

                    valueCases.Add((caseLabel, switchCase.Body));
                } else {
                    defaultCaseBody = ((DMASTProcStatementSwitch.SwitchCaseDefault)switchCase).Body;
                }
            }

            if (defaultCaseBody != null) {
                _proc.StartScope();
                {
                    defaultCaseBody.Visit(this);
                }
                _proc.EndScope();
            }
            _proc.Jump(endLabel);

            foreach ((string CaseLabel, DMASTProcBlockInner CaseBody) valueCase in valueCases) {
                _proc.AddLabel(valueCase.CaseLabel);
                _proc.StartScope();
                {
                    valueCase.CaseBody.Visit(this);
                }
                _proc.EndScope();
                _proc.Jump(endLabel);
            }

            _proc.AddLabel(endLabel);
        }

        public void VisitProcStatementBrowse(DMASTProcStatementBrowse statementBrowse) {
            statementBrowse.Receiver.Visit(this);
            statementBrowse.Body.Visit(this);
            statementBrowse.Options.Visit(this);
            _proc.Browse();
        }

        public void VisitProcStatementBrowseResource(DMASTProcStatementBrowseResource statementBrowseResource) {
            statementBrowseResource.Receiver.Visit(this);
            statementBrowseResource.File.Visit(this);
            statementBrowseResource.Filename.Visit(this);
            _proc.BrowseResource();
        }

        public void VisitProcStatementOutputControl(DMASTProcStatementOutputControl statementOutputControl) {
            statementOutputControl.Receiver.Visit(this);
            statementOutputControl.Message.Visit(this);
            statementOutputControl.Control.Visit(this);
            _proc.OutputControl();
        }

        public void VisitProcCall(DMASTProcCall procCall) {
            if (procCall.Callable is DMASTCallableSuper) {
                _proc.PushSuperProc();

                if (procCall.Parameters.Length == 0) {
                    _proc.PushProcArguments();
                } else {
                    PushCallParameters(procCall.Parameters);
                }
            } else if (procCall.Callable is DMASTCallableSelf) {
                _proc.CallSelf();
                PushCallParameters(procCall.Parameters);
            } else {
                procCall.Callable.Visit(this);
                PushCallParameters(procCall.Parameters);
            }
            
            _proc.Call();
        }

        public void VisitCallParameter(DMASTCallParameter parameter) {
            parameter.Value.Visit(this);
        }

        public void VisitIdentifier(DMASTIdentifier identifier) {
            if (identifier.Identifier == "src") {
                _currentVariable = new DMVariable(_dmObject.Path, "src", false);
                _proc.PushSrc();
            } else if (identifier.Identifier == "usr") {
                _currentVariable = new DMVariable(DreamPath.Mob, "usr", false);
                _proc.PushUsr();
            } else if (identifier.Identifier == "args") {
                _currentVariable = new DMVariable(DreamPath.List, "args", false);
                _proc.GetIdentifier("args");
            } else {
                DMProc.DMLocalVariable localVar = _proc.GetLocalVariable(identifier.Identifier);

                if (localVar != null) {
                    _currentVariable = new DMVariable(localVar.Type, identifier.Identifier, false);
                    _proc.PushLocalVariable(identifier.Identifier);
                } else {
                    _currentVariable =  _dmObject.GetVariable(identifier.Identifier);
                    if (_currentVariable == null) _currentVariable =  _dmObject.GetGlobalVariable(identifier.Identifier);
                    if (_currentVariable == null) throw new Exception("Invalid identifier \"" + identifier.Identifier + "\"");

                    _proc.GetIdentifier(identifier.Identifier);
                }
            }
        }

        public void VisitCallableProcIdentifier(DMASTCallableProcIdentifier procIdentifier) {
            if (!_dmObject.HasProc(procIdentifier.Identifier)) {
                throw new Exception("Type + " + _dmObject.Path + " does not have a proc named \"" + procIdentifier.Identifier + "\"");
            }

            _proc.GetProc(procIdentifier.Identifier);
        }

        public void VisitCallableSelf(DMASTCallableSelf self) {
            _proc.PushSelf();
        }

        public void VisitCall(DMASTCall call) {
            if (call.CallParameters.Length > 1) call.CallParameters[1].Visit(this);
            call.CallParameters[0].Visit(this);
            PushCallParameters(call.ProcParameters);
            _proc.CallStatement();
        }

        public void VisitAssign(DMASTAssign assign) {
            assign.Expression.Visit(this);
            assign.Value.Visit(this);
            _proc.Assign();
        }

        public void VisitNewInferred(DMASTNewInferred newInferred) {
            if (_currentVariable.Type == null) throw new Exception("An inferred new requires a type!");

            _proc.PushPath(_currentVariable.Type.Value);
            PushCallParameters(newInferred.Parameters);
            _proc.CreateObject();
        }

        public void VisitNewPath(DMASTNewPath newPath) {
            _proc.PushPath(newPath.Path.Path);
            PushCallParameters(newPath.Parameters);
            _proc.CreateObject();
        }

        public void VisitNewIdentifier(DMASTNewIdentifier newIdentifier) {
            newIdentifier.Identifier.Visit(this);
            PushCallParameters(newIdentifier.Parameters);
            _proc.CreateObject();
        }

        public void VisitNewDereference(DMASTNewDereference newDereference) {
            newDereference.Dereference.Visit(this);
            PushCallParameters(newDereference.Parameters);
            _proc.CreateObject();
        }

        public void VisitDereference(DMASTDereference dereference) {
            Dereference(dereference, true);
        }

        public void VisitDereferenceProc(DMASTDereferenceProc dereferenceProc) {
            Dereference(dereferenceProc, false);

            DMASTDereference.Dereference deref = dereferenceProc.Dereferences[dereferenceProc.Dereferences.Length - 1];
            if (deref.Type == DMASTDereference.DereferenceType.Direct) {
                if (_currentVariable.Type == null) {
                    throw new Exception("Cannot dereference property \"" + deref.Property + "\" because \"" + _currentVariable.Name + "\" does not have a type");
                }

                DreamPath type = _currentVariable.Type.Value;
                DMObject dmObject = DMObjectTree.GetDMObject(type, false);

                if (!dmObject.HasProc(deref.Property)) throw new Exception("Type + " + type + " does not have a proc named \"" + deref.Property + "\"");
                _proc.DereferenceProc(deref.Property);
            } else if (deref.Type == DMASTDereference.DereferenceType.Search) { //No compile-time checks
                _proc.DereferenceProc(deref.Property);
            }
        }

        public void VisitNot(DMASTNot not) {
            not.Expression.Visit(this);
            _proc.Not();
        }

        public void VisitNegate(DMASTNegate negate) {
            negate.Expression.Visit(this);
            _proc.Negate();
        }

        public void VisitTernary(DMASTTernary ternary) {
            string cLabel = NewLabelName();
            string endLabel = NewLabelName();

            ternary.A.Visit(this);
            _proc.JumpIfFalse(cLabel);
            ternary.B.Visit(this);
            _proc.Jump(endLabel);
            _proc.AddLabel(cLabel);
            ternary.C.Visit(this);
            _proc.AddLabel(endLabel);
        }

        public void VisitAdd(DMASTAdd add) {
            add.A.Visit(this);
            add.B.Visit(this);
            _proc.Add();
        }

        public void VisitSubtract(DMASTSubtract subtract) {
            subtract.A.Visit(this);
            subtract.B.Visit(this);
            _proc.Subtract();
        }

        public void VisitPreIncrement(DMASTPreIncrement preIncrement) {
            preIncrement.Expression.Visit(this);
            _proc.PushInt(1);
            _proc.Append();

            preIncrement.Expression.Visit(this);
        }

        public void VisitPreDecrement(DMASTPreDecrement preDecrement) {
            preDecrement.Expression.Visit(this);
            _proc.PushInt(1);
            _proc.Remove();

            preDecrement.Expression.Visit(this);
        }

        public void VisitPostIncrement(DMASTPostIncrement postIncrement) {
            postIncrement.Expression.Visit(this);

            postIncrement.Expression.Visit(this);
            _proc.PushInt(1);
            _proc.Append();
        }

        public void VisitPostDecrement(DMASTPostDecrement postDecrement) {
            postDecrement.Expression.Visit(this);

            postDecrement.Expression.Visit(this);
            _proc.PushInt(1);
            _proc.Remove();
        }

        public void VisitMultiply(DMASTMultiply multiply) {
            multiply.A.Visit(this);
            multiply.B.Visit(this);
            _proc.Multiply();
        }

        public void VisitDivide(DMASTDivide divide) {
            divide.A.Visit(this);
            divide.B.Visit(this);
            _proc.Divide();
        }

        public void VisitModulus(DMASTModulus modulus) {
            modulus.A.Visit(this);
            modulus.B.Visit(this);
            _proc.Modulus();
        }

        public void VisitPower(DMASTPower power) {
            power.A.Visit(this);
            power.B.Visit(this);
            _proc.Power();
        }

        public void VisitAppend(DMASTAppend append) {
            append.A.Visit(this);
            append.B.Visit(this);
            _proc.Append();
        }

        public void VisitRemove(DMASTRemove remove) {
            remove.A.Visit(this);
            remove.B.Visit(this);
            _proc.Remove();
        }

        public void VisitCombine(DMASTCombine combine) {
            combine.A.Visit(this);
            combine.B.Visit(this);
            _proc.Combine();
        }

        public void VisitMask(DMASTMask mask) {
            mask.A.Visit(this);
            mask.B.Visit(this);
            _proc.Mask();
        }

        public void VisitMultiplyAssign(DMASTMultiplyAssign multiplyAssign) {
            multiplyAssign.A.Visit(this);

            multiplyAssign.A.Visit(this);
            multiplyAssign.B.Visit(this);
            _proc.Multiply();

            _proc.Assign();
        }

        public void VisitDivideAssign(DMASTDivideAssign divideAssign) {
            divideAssign.A.Visit(this);

            divideAssign.A.Visit(this);
            divideAssign.B.Visit(this);
            _proc.Divide();

            _proc.Assign();
        }

        public void VisitLeftShiftAssign(DMASTLeftShiftAssign leftShiftAssign) {
            leftShiftAssign.A.Visit(this);

            leftShiftAssign.A.Visit(this);
            leftShiftAssign.B.Visit(this);
            _proc.BitShiftLeft();

            _proc.Assign();
        }

        public void VisitRightShiftAssign(DMASTRightShiftAssign rightShiftAssign) {
            rightShiftAssign.A.Visit(this);

            rightShiftAssign.A.Visit(this);
            rightShiftAssign.B.Visit(this);
            _proc.BitShiftRight();

            _proc.Assign();
        }

        public void VisitXorAssign(DMASTXorAssign xorAssign) {
            xorAssign.A.Visit(this);

            xorAssign.A.Visit(this);
            xorAssign.B.Visit(this);
            _proc.BinaryXor();

            _proc.Assign();
        }

        public void VisitLeftShift(DMASTLeftShift leftShift) {
            leftShift.A.Visit(this);
            leftShift.B.Visit(this);
            _proc.BitShiftLeft();
        }

        public void VisitRightShift(DMASTRightShift rightShift) {
            rightShift.A.Visit(this);
            rightShift.B.Visit(this);
            _proc.BitShiftRight();
        }

        public void VisitBinaryNot(DMASTBinaryNot binaryNot) {
            binaryNot.Value.Visit(this);
            _proc.BinaryNot();
        }

        public void VisitBinaryAnd(DMASTBinaryAnd binaryAnd) {
            binaryAnd.A.Visit(this);
            binaryAnd.B.Visit(this);
            _proc.BinaryAnd();
        }

        public void VisitBinaryXor(DMASTBinaryXor binaryXor) {
            binaryXor.A.Visit(this);
            binaryXor.B.Visit(this);
            _proc.BinaryXor();
        }

        public void VisitBinaryOr(DMASTBinaryOr binaryOr) {
            binaryOr.A.Visit(this);
            binaryOr.B.Visit(this);
            _proc.BinaryOr();
        }

        public void VisitEqual(DMASTEqual equal) {
            equal.A.Visit(this);
            equal.B.Visit(this);
            _proc.Equal();
        }

        public void VisitNotEqual(DMASTNotEqual notEqual) {
            notEqual.A.Visit(this);
            notEqual.B.Visit(this);
            _proc.NotEqual();
        }

        public void VisitGreaterThan(DMASTGreaterThan greaterThan) {
            greaterThan.A.Visit(this);
            greaterThan.B.Visit(this);
            _proc.GreaterThan();
        }

        public void VisitGreaterThanOrEqual(DMASTGreaterThanOrEqual greaterThanOrEqual) {
            greaterThanOrEqual.A.Visit(this);
            greaterThanOrEqual.B.Visit(this);
            _proc.GreaterThanOrEqual();
        }

        public void VisitLessThan(DMASTLessThan lessThan) {
            lessThan.A.Visit(this);
            lessThan.B.Visit(this);
            _proc.LessThan();
        }

        public void VisitLessThanOrEqual(DMASTLessThanOrEqual lessThanOrEqual) {
            lessThanOrEqual.A.Visit(this);
            lessThanOrEqual.B.Visit(this);
            _proc.LessThanOrEqual();
        }

        public void VisitOr(DMASTOr or) {
            string endLabel = NewLabelName();

            or.A.Visit(this);
            _proc.BooleanOr(endLabel);
            or.B.Visit(this);
            _proc.AddLabel(endLabel);
        }

        public void VisitAnd(DMASTAnd and) {
            string endLabel = NewLabelName();

            and.A.Visit(this);
            _proc.BooleanAnd(endLabel);
            and.B.Visit(this);
            _proc.AddLabel(endLabel);
        }

        public void VisitListIndex(DMASTListIndex listIndex) {
            listIndex.Expression.Visit(this);
            listIndex.Index.Visit(this);

            _proc.IndexList();
        }

        public void VisitIn(DMASTExpressionIn expressionIn) {
            expressionIn.Value.Visit(this);
            expressionIn.List.Visit(this);

            _proc.IsInList();
        }

        public void VisitStringFormat(DMASTStringFormat stringFormat) {
            for (int i = stringFormat.InterpolatedValues.Length - 1; i >= 0; i--) {
                stringFormat.InterpolatedValues[i].Visit(this);
            }

            _proc.FormatString(stringFormat.Value);
        }

        public void VisitInput(DMASTInput input) {
            PushCallParameters(input.Parameters);
            _proc.Prompt(input.Types);
        }

        public void VisitInitial(DMASTInitial initial) {
            if (initial.Expression is DMASTIdentifier identifier) {
                DMProc.DMLocalVariable localVariable = _proc.GetLocalVariable(identifier.Identifier);

                if (localVariable != null) {
                    throw new NotImplementedException("Using initial() on local variables is not implemented");
                } else {
                    _proc.PushSrc();
                    _proc.Initial(identifier.Identifier);
                }
            } else if (initial.Expression is DMASTDereference dereference) {
                Dereference(dereference, false);

                DMASTDereference.Dereference lastDeref = dereference.Dereferences[dereference.Dereferences.Length - 1];
                _proc.Initial(lastDeref.Property);
            } else {
                throw new Exception("Expected an identifier");
            }
        }

        public void VisitIsType(DMASTIsType isType) {
            isType.Value.Visit(this);
            isType.Type.Visit(this);
            _proc.IsType();
        }
        
        public void VisitImplicitIsType(DMASTImplicitIsType isType) {
            isType.Value.Visit(this);

            if (_currentVariable.Type == null) throw new Exception("Value does not have a type");

            _proc.PushPath(_currentVariable.Type.Value);
            _proc.IsType();
        }

        public void VisitLocateCoordinates(DMASTLocateCoordinates locateCoordinates) {
            locateCoordinates.X.Visit(this);
            locateCoordinates.Y.Visit(this);
            locateCoordinates.Z.Visit(this);

            _proc.LocateCoordinates();
        }

        public void VisitLocate(DMASTLocate locate) {
            if (locate.Expression != null) {
                locate.Expression.Visit(this);
            } else {
                if (_currentVariable.Type == null) throw new Exception("locate() requires a type");

                _proc.PushPath(_currentVariable.Type.Value);
            }

            if (locate.Container != null) {
                locate.Container.Visit(this);
            } else {
                _proc.GetIdentifier("world");
            }

            _proc.Locate();
        }

        public void VisitList(DMASTList list) {
            _proc.CreateList();

            if (list.Values != null) {
                foreach (DMASTCallParameter value in list.Values) {
                    DMASTAssign associatedAssign = value.Value as DMASTAssign;

                    if (associatedAssign != null) {
                        associatedAssign.Value.Visit(this);

                        if (associatedAssign.Expression is DMASTIdentifier) {
                            _proc.PushString(value.Name);
                            _proc.ListAppendAssociated();
                        } else {
                            associatedAssign.Expression.Visit(this);
                            _proc.ListAppendAssociated();
                        }
                    } else {
                        value.Visit(this);

                        if (value.Name != null) {
                            _proc.PushString(value.Name);
                            _proc.ListAppendAssociated();
                        } else {
                            _proc.ListAppend();
                        }
                    }
                }
            }
        }

        public void VisitConstantInteger(DMASTConstantInteger constant) {
            _proc.PushInt(constant.Value);
        }

        public void VisitConstantFloat(DMASTConstantFloat constant) {
            _proc.PushFloat(constant.Value);
        }

        public void VisitConstantString(DMASTConstantString constant) {
            _proc.PushString(constant.Value);
        }

        public void VisitConstantResource(DMASTConstantResource constant) {
            _proc.PushResource(constant.Path);
        }

        public void VisitConstantPath(DMASTConstantPath constant) {
            _proc.PushPath(constant.Value.Path);
        }

        public void VisitConstantNull(DMASTConstantNull constant) {
            _proc.PushNull();
        }

        private string NewLabelName() {
            return "label" + _labelIdCounter++;
        }

        private void Dereference(DMASTDereference dereference, bool includingLast) {
            dereference.Expression.Visit(this);

            DMASTDereference.Dereference[] dereferences = dereference.Dereferences;
            for (int i = 0; i < (includingLast ? dereferences.Length : dereferences.Length - 1); i++) {
                DMASTDereference.Dereference deref = dereferences[i];

                if (deref.Type == DMASTDereference.DereferenceType.Direct) {
                    if (_currentVariable.Type == null) {
                        throw new Exception("Cannot dereference property \"" + deref.Property + "\" because \"" + _currentVariable.Name + "\" does not have a type");
                    }

                    DreamPath type = _currentVariable.Type.Value;
                    DMObject dmObject = DMObjectTree.GetDMObject(type, false);

                    _currentVariable = dmObject.GetVariable(deref.Property);
                    if (_currentVariable == null) _currentVariable = dmObject.GetGlobalVariable(deref.Property);
                    if (_currentVariable == null) throw new Exception("Invalid property \"" + deref.Property + "\" on type " + dmObject.Path);

                    _proc.Dereference(deref.Property);
                } else if (deref.Type == DMASTDereference.DereferenceType.Search) { //No compile-time checks
                    _currentVariable = new DMVariable(null, deref.Property, false);

                    _proc.Dereference(deref.Property);
                }
            }
        }

        private void PushCallParameters(DMASTCallParameter[] parameters) {
            if (parameters != null) {
                if (parameters.Length > 0 && parameters[0].Value is DMASTProcCall) {
                    DMASTProcCall procCallParameter = (DMASTProcCall)parameters[0].Value;

                    if (procCallParameter.Callable is DMASTCallableProcIdentifier) {
                        DMASTCallableProcIdentifier procCallIdentifier = (DMASTCallableProcIdentifier)procCallParameter.Callable;

                        if (procCallIdentifier.Identifier == "arglist") {
                            if (parameters.Length != 1) throw new Exception("arglist must be the only argument");
                            if (procCallParameter.Parameters.Length != 1) throw new Exception("arglist must have 1 argument");

                            procCallParameter.Parameters[0].Visit(this);
                            _proc.PushArgumentList();
                            return;
                        }
                    }
                }

                List<DreamProcOpcodeParameterType> parameterTypes = new List<DreamProcOpcodeParameterType>();
                List<string> parameterNames = new List<string>();

                foreach (DMASTCallParameter parameter in parameters) {
                    parameter.Visit(this);

                    if (parameter.Name != null) {
                        parameterTypes.Add(DreamProcOpcodeParameterType.Named);
                        parameterNames.Add(parameter.Name);
                    } else {
                        parameterTypes.Add(DreamProcOpcodeParameterType.Unnamed);
                    }
                }

                _proc.PushArguments(parameters.Length, parameterTypes.ToArray(), parameterNames.ToArray());
            } else {
                _proc.PushArguments(0);
            }
        }
    }
}
