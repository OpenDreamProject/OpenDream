using OpenDreamShared.Compiler;
using OpenDreamShared.Compiler.DM;
using System.Collections.Generic;

namespace DMCompiler.DM.Visitors {
    class DMVisitorProcBuilder : DMASTVisitor {
        private DMObject _dmObject;
        private DMProc _proc;

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
                        string afterDefaultValueCheck = _proc.NewLabelName();

                        _proc.PushLocalVariable(parameterName);
                        _proc.IsNull();
                        _proc.JumpIfFalse(afterDefaultValueCheck);

                        _proc.PushLocalVariable(parameterName);
                        DMExpression.Emit(_dmObject, _proc, parameter.Value, parameter.ObjectType);
                        _proc.Assign();

                        _proc.AddLabel(afterDefaultValueCheck);
                    }
                }

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
            DMExpression.Emit(_dmObject, _proc, statement.Expression);
            // TODO: does this need pop?
        }

        public void VisitProcStatementContinue(DMASTProcStatementContinue statementContinue) {
            _proc.Continue();
        }

        public void VisitProcStatementGoto(DMASTProcStatementGoto statementGoto) {
            _proc.Goto(statementGoto.Label.Identifier);
        }

        public void VisitProcStatementLabel(DMASTProcStatementLabel statementLabel) {
            _proc.AddLabel(statementLabel.Name + "_codelabel");
        }

        public void VisitProcStatementBreak(DMASTProcStatementBreak statementBreak) {
            _proc.Break();
        }

        public void VisitProcStatementSet(DMASTProcStatementSet statementSet) {
            //TODO: Proc attributes
            if (statementSet.Attribute.ToLower() == "waitfor") {
                var constant = DMExpression.Constant(_dmObject, _proc, statementSet.Value);

                if (constant is not Expressions.Number) {
                    throw new CompileErrorException($"waitfor attribute should be a number (got {constant})");
                }

                _proc.WaitFor(constant.IsTruthy());
            }
        }

        public void VisitProcStatementDel(DMASTProcStatementDel statementDel) {
            DMExpression.Emit(_dmObject, _proc, statementDel.Value);
            _proc.DeleteObject();
        }

        public void VisitProcStatementSpawn(DMASTProcStatementSpawn statementSpawn) {
            DMExpression.Emit(_dmObject, _proc, statementSpawn.Delay);

            string afterSpawnLabel = _proc.NewLabelName();
            _proc.Spawn(afterSpawnLabel);

            statementSpawn.Body.Visit(this);
            _proc.Return(); //Prevent the new thread from executing outside its own code

            _proc.AddLabel(afterSpawnLabel);
        }

        public void VisitProcStatementVarDeclaration(DMASTProcStatementVarDeclaration varDeclaration) {
            _proc.AddLocalVariable(varDeclaration.Name, varDeclaration.Type);

            if (varDeclaration.Value != null) {
                DMExpression.Emit(_dmObject, _proc, varDeclaration.Value, varDeclaration.Type);
            } else {
                _proc.PushNull();
            }

            _proc.SetLocalVariable(varDeclaration.Name);
        }

        public void VisitProcStatementReturn(DMASTProcStatementReturn statement) {
            if (statement.Value != null) {
                DMExpression.Emit(_dmObject, _proc, statement.Value);
            } else {
                _proc.PushSelf(); //Default return value
            }

            _proc.Return();
        }

        public void VisitProcStatementIf(DMASTProcStatementIf statement) {
            DMExpression.Emit(_dmObject, _proc, statement.Condition);

            if (statement.ElseBody == null) {
                string endLabel = _proc.NewLabelName();

                _proc.JumpIfFalse(endLabel);
                _proc.StartScope();
                statement.Body.Visit(this);
                _proc.EndScope();
                _proc.AddLabel(endLabel);
            } else {
                string elseLabel = _proc.NewLabelName();
                string endLabel = _proc.NewLabelName();

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
                statementForStandard.Initializer?.Visit(this);

                string loopLabel = _proc.NewLabelName();
                _proc.LoopStart(loopLabel);
                {
                    DMExpression.Emit(_dmObject, _proc, statementForStandard.Comparator);
                    _proc.BreakIfFalse();

                    statementForStandard.Body.Visit(this);

                    _proc.LoopContinue(loopLabel);
                    DMExpression.Emit(_dmObject, _proc, statementForStandard.Incrementor);
                    _proc.LoopJumpToStart(loopLabel);
                }
                _proc.LoopEnd();
            }
            _proc.EndScope();
        }

        public void VisitProcStatementForList(DMASTProcStatementForList statementForList) {
            DMExpression.Emit(_dmObject, _proc, statementForList.List);
            _proc.CreateListEnumerator();
            _proc.StartScope();
            {
                statementForList.Initializer?.Visit(this);

                string loopLabel = _proc.NewLabelName();
                _proc.LoopStart(loopLabel);
                {
                    _proc.Enumerate(statementForList.Variable.Identifier);
                    _proc.BreakIfFalse();

                    DMASTProcStatementVarDeclaration varDeclaration = statementForList.Initializer as DMASTProcStatementVarDeclaration;
                    if (varDeclaration != null && varDeclaration.Type != null) {
                        DMExpression.Emit(_dmObject, _proc, statementForList.Variable);
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
            DMExpression.Emit(_dmObject, _proc, statementForRange.RangeStart);
            DMExpression.Emit(_dmObject, _proc, statementForRange.RangeEnd);
            DMExpression.Emit(_dmObject, _proc, statementForRange.Step);
            _proc.CreateRangeEnumerator();
            _proc.StartScope();
            {
                statementForRange.Initializer?.Visit(this);

                string loopLabel = _proc.NewLabelName();
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
            string loopLabel = _proc.NewLabelName();

            _proc.LoopStart(loopLabel);
            {
                DMExpression.Emit(_dmObject, _proc, statementWhile.Conditional);
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
            string loopLabel = _proc.NewLabelName();
            string loopEndLabel = _proc.NewLabelName();

            _proc.LoopStart(loopLabel);
            {
                statementDoWhile.Body.Visit(this);

                _proc.LoopContinue(loopLabel);
                DMExpression.Emit(_dmObject, _proc, statementDoWhile.Conditional);
                _proc.JumpIfFalse(loopEndLabel);
                _proc.LoopJumpToStart(loopLabel);

                _proc.AddLabel(loopEndLabel);
                _proc.Break();
            }
            _proc.LoopEnd();
        }

        public void VisitProcStatementSwitch(DMASTProcStatementSwitch statementSwitch) {
            string endLabel = _proc.NewLabelName();
            List<(string CaseLabel, DMASTProcBlockInner CaseBody)> valueCases = new();
            DMASTProcBlockInner defaultCaseBody = null;

            DMExpression.Emit(_dmObject, _proc, statementSwitch.Value);
            foreach (DMASTProcStatementSwitch.SwitchCase switchCase in statementSwitch.Cases) {
                if (switchCase is DMASTProcStatementSwitch.SwitchCaseValues switchCaseValues) {
                    string caseLabel = _proc.NewLabelName();

                    foreach (DMASTExpression value in switchCaseValues.Values) {
                        if (value is DMASTSwitchCaseRange range) {
                            var lower = DMExpression.Constant(_dmObject, _proc, range.RangeStart);
                            var upper = DMExpression.Constant(_dmObject, _proc, range.RangeEnd);

                            lower.EmitPushValue(_dmObject, _proc);
                            upper.EmitPushValue(_dmObject, _proc);
                            _proc.SwitchCaseRange(caseLabel);
                        } else {
                            var constant = DMExpression.Constant(_dmObject, _proc, value);
                            constant.EmitPushValue(_dmObject, _proc);
                            _proc.SwitchCase(caseLabel);
                        }
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
            DMExpression.Emit(_dmObject, _proc, statementBrowse.Receiver);
            DMExpression.Emit(_dmObject, _proc, statementBrowse.Body);
            DMExpression.Emit(_dmObject, _proc, statementBrowse.Options);
            _proc.Browse();
        }

        public void VisitProcStatementBrowseResource(DMASTProcStatementBrowseResource statementBrowseResource) {
            DMExpression.Emit(_dmObject, _proc, statementBrowseResource.Receiver);
            DMExpression.Emit(_dmObject, _proc, statementBrowseResource.File);
            DMExpression.Emit(_dmObject, _proc, statementBrowseResource.Filename);
            _proc.BrowseResource();
        }

        public void VisitProcStatementOutputControl(DMASTProcStatementOutputControl statementOutputControl) {
            DMExpression.Emit(_dmObject, _proc, statementOutputControl.Receiver);
            DMExpression.Emit(_dmObject, _proc, statementOutputControl.Message);
            DMExpression.Emit(_dmObject, _proc, statementOutputControl.Control);
            _proc.OutputControl();
        }

        public void HandleCompileErrorException(CompileErrorException exception) {
            Program.VisitorErrors.Add(exception.Error);
        }
    }
}
