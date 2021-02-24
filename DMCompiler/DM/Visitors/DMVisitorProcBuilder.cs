using DMCompiler.DM;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream.Procs;
using System;
using System.Collections.Generic;

namespace DMCompiler.Compiler.DM.Visitors {
    class DMVisitorProcBuilder : DMASTVisitor {
        private DMProc _proc;
        private Stack<object> _valueStack = new();
        private int _labelIdCounter = 0;

        public DMVisitorProcBuilder() { }

        public DMVisitorProcBuilder(DMProc proc) {
            _proc = proc;
        }

        public DMProc BuildProc(DMASTProcDefinition procDefinition) {
            _proc = new DMProc();

            if (procDefinition.Body != null) {
                foreach (DMASTDefinitionParameter parameter in procDefinition.Parameters) {
                    string parameterName = parameter.Path.Path.LastElement;

                    _proc.AddLocalVariable(parameterName);
                    if (parameter.Value != null) {
                        string afterDefaultValueCheck = NewLabelName();
                        
                        _proc.GetIdentifier(parameterName);
                        _proc.PushNull();
                        _proc.Equal();
                        _proc.JumpIfFalse(afterDefaultValueCheck);

                        _proc.GetIdentifier(parameterName);
                        parameter.Value.Visit(this);
                        _proc.Assign();

                        _proc.AddLabel(afterDefaultValueCheck);
                    }
                }

                _valueStack.Clear();
                procDefinition.Body.Visit(this);
                _proc.ResolveLabels();
            }

            return _proc;
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
            if (varDeclaration.Value is DMASTNewInferred) {
                if (varDeclaration.Type == null) throw new Exception("An inferred new requires a type!");
                DMASTCallParameter[] parameters = ((DMASTNewInferred)varDeclaration.Value).Parameters;

                new DMASTNewPath(varDeclaration.Type, parameters).Visit(this);
            } else if (varDeclaration.Value != null) {
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
                string loopBodyLabel = NewLabelName();
                _proc.LoopStart(loopLabel);
                {
                    statementForStandard.Comparator.Visit(this);
                    _proc.JumpIfTrue(loopBodyLabel);
                    _proc.Break();

                    _proc.AddLabel(loopBodyLabel);
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
                string typeCheckLabel = NewLabelName();
                string loopBodyLabel = NewLabelName();
                _proc.LoopStart(loopLabel);
                {
                    _proc.Enumerate(statementForList.Variable.Identifier);
                    _proc.JumpIfTrue(typeCheckLabel);
                    _proc.Break();

                    _proc.AddLabel(typeCheckLabel);
                    DMASTProcStatementVarDeclaration varDeclaration = statementForList.Initializer as DMASTProcStatementVarDeclaration;
                    if (varDeclaration != null && varDeclaration.Type != null) {
                        statementForList.Variable.Visit(this);
                        _proc.PushPath(varDeclaration.Type.Path);
                        _proc.IsType();

                        _proc.JumpIfTrue(loopBodyLabel);
                        _proc.Continue();
                    }

                    _proc.AddLabel(loopBodyLabel);
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
                string loopBodyLabel = NewLabelName();
                _proc.LoopStart(loopLabel);
                {
                    _proc.Enumerate(statementForRange.Variable.Identifier);
                    _proc.JumpIfTrue(loopBodyLabel);
                    _proc.Break();

                    _proc.AddLabel(loopBodyLabel);
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
            string bodyLabel = NewLabelName();

            _proc.LoopStart(loopLabel);
            {
                statementWhile.Conditional.Visit(this);
                _proc.JumpIfTrue(bodyLabel);
                _proc.Break();

                _proc.AddLabel(bodyLabel);
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
            List<(string CaseLabel, DMASTProcBlockInner CaseBody)> valueCases = new List<(string, DMASTProcBlockInner)>();
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
                defaultCaseBody.Visit(this);
            }
            _proc.Jump(endLabel);

            foreach ((string CaseLabel, DMASTProcBlockInner CaseBody) valueCase in valueCases) {
                _proc.AddLabel(valueCase.CaseLabel);
                valueCase.CaseBody.Visit(this);
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
                _proc.PushSrc();
            } else {
                _proc.GetIdentifier(identifier.Identifier);
            }
        }

        public void VisitCallableProcIdentifier(DMASTCallableProcIdentifier procIdentifier) {
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
            dereference.Expression.Visit(this);

            foreach (DMASTDereference.Dereference deref in dereference.Dereferences) {
                if (deref.Type == DMASTDereference.DereferenceType.Direct) {
                    _proc.Dereference(deref.Property);
                } else if (deref.Type == DMASTDereference.DereferenceType.Search) {
                    throw new NotImplementedException();
                }
            }
        }

        public void VisitDereferenceProc(DMASTDereferenceProc dereferenceProc) {
            dereferenceProc.Expression.Visit(this);

            for (int i = 0; i < dereferenceProc.Dereferences.Length; i++) {
                DMASTDereference.Dereference deref = dereferenceProc.Dereferences[i];

                if (deref.Type == DMASTDereference.DereferenceType.Direct) {
                    if (i < dereferenceProc.Dereferences.Length - 1) { //Last deref is dereferencing a proc
                        _proc.Dereference(deref.Property);
                    } else {
                        _proc.DereferenceProc(deref.Property);
                    }
                } else if (deref.Type == DMASTDereference.DereferenceType.Search) {
                    throw new NotImplementedException();
                }
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
            initial.Expression.Visit(this);
            _proc.Initial();
        }

        public void VisitIsType(DMASTIsType isType) {
            isType.Value.Visit(this);
            isType.Type.Visit(this);
            _proc.IsType();
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
