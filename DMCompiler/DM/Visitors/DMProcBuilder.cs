using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DM;
using System.Collections.Generic;
using OpenDreamShared.Dream;
using System;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.DM.Visitors {
    class DMProcBuilder {
        private DMObject _dmObject;
        private DMProc _proc;

        public DMProcBuilder(DMObject dmObject, DMProc proc) {
            _dmObject = dmObject;
            _proc = proc;
        }

        public void ProcessProcDefinition(DMASTProcDefinition procDefinition) {
            if (procDefinition.Body == null) return;

            foreach (DMASTDefinitionParameter parameter in procDefinition.Parameters) {
                string parameterName = parameter.Name;

                if (parameter.Value != null) { //Parameter has a default value
                    string afterDefaultValueCheck = _proc.NewLabelName();
                    DMReference parameterRef = _proc.GetLocalVariableReference(parameterName);

                    //Don't set parameter to default if not null
                    _proc.PushReferenceValue(parameterRef);
                    _proc.IsNull();
                    _proc.JumpIfFalse(afterDefaultValueCheck);

                    //Set default
                    try {
                        DMExpression.Emit(_dmObject, _proc, parameter.Value, parameter.ObjectType);
                    } catch (CompileErrorException e) {
                        DMCompiler.Error(e.Error);
                    }
                    _proc.Assign(parameterRef);
                    _proc.Pop();

                    _proc.AddLabel(afterDefaultValueCheck);
                }
            }

            ProcessBlockInner(procDefinition.Body);
            _proc.ResolveLabels();
        }

        public void ProcessBlockInner(DMASTProcBlockInner block) {
            // TODO ProcessStatementSet() needs to be before any loops but this is nasty
            foreach (var stmt in block.Statements)
            {
                if (stmt is DMASTProcStatementSet set)
                {
                    try
                    {
                        ProcessStatementSet(set);
                    }
                    catch (CompileAbortException e)
                    {
                        // The statement's location info isn't passed all the way down so change the error to make it more accurate
                        e.Error.Location = set.Location;
                        DMCompiler.Error(e.Error);
                        return; // Don't spam the error that will continue to exist
                    }
                    catch (CompileErrorException e) { //Retreat from the statement when there's an error
                        DMCompiler.Error(e.Error);
                    }
                }
            }
            foreach (DMASTProcStatement statement in block.Statements) {
                try
                {
                    // see above
                    if (statement is DMASTProcStatementSet)
                    {
                        continue;
                    }
                    ProcessStatement(statement);
                }
                catch (CompileAbortException e)
                {
                    // The statement's location info isn't passed all the way down so change the error to make it more accurate
                    e.Error.Location = statement.Location;
                    DMCompiler.Error(e.Error);
                    return; // Don't spam the error that will continue to exist
                }
                catch (CompileErrorException e) { //Retreat from the statement when there's an error
                    DMCompiler.Error(e.Error);
                }
            }
        }

        public void ProcessStatement(DMASTProcStatement statement) {
            switch (statement) {
                case DMASTProcStatementExpression statementExpression: ProcessStatementExpression(statementExpression); break;
                case DMASTProcStatementContinue statementContinue: ProcessStatementContinue(statementContinue); break;
                case DMASTProcStatementGoto statementGoto: ProcessStatementGoto(statementGoto); break;
                case DMASTProcStatementLabel statementLabel: ProcessStatementLabel(statementLabel); break;
                case DMASTProcStatementBreak statementBreak: ProcessStatementBreak(statementBreak); break;
                case DMASTProcStatementDel statementDel: ProcessStatementDel(statementDel); break;
                case DMASTProcStatementSpawn statementSpawn: ProcessStatementSpawn(statementSpawn); break;
                case DMASTProcStatementReturn statementReturn: ProcessStatementReturn(statementReturn); break;
                case DMASTProcStatementIf statementIf: ProcessStatementIf(statementIf); break;
                case DMASTProcStatementForStandard statementForStandard: ProcessStatementForStandard(statementForStandard); break;
                case DMASTProcStatementForList statementForList: ProcessStatementForList(statementForList); break;
                case DMASTProcStatementForRange statementForRange: ProcessStatementForRange(statementForRange); break;
                case DMASTProcStatementForType statementForType: ProcessStatementForType(statementForType); break;
                case DMASTProcStatementInfLoop statementInfLoop: ProcessStatementInfLoop(statementInfLoop); break;
                case DMASTProcStatementWhile statementWhile: ProcessStatementWhile(statementWhile); break;
                case DMASTProcStatementDoWhile statementDoWhile: ProcessStatementDoWhile(statementDoWhile); break;
                case DMASTProcStatementSwitch statementSwitch: ProcessStatementSwitch(statementSwitch); break;
                case DMASTProcStatementBrowse statementBrowse: ProcessStatementBrowse(statementBrowse); break;
                case DMASTProcStatementBrowseResource statementBrowseResource: ProcessStatementBrowseResource(statementBrowseResource); break;
                case DMASTProcStatementOutputControl statementOutputControl: ProcessStatementOutputControl(statementOutputControl); break;
                case DMASTProcStatementVarDeclaration varDeclaration: ProcessStatementVarDeclaration(varDeclaration); break;
                case DMASTProcStatementTryCatch tryCatch: ProcessStatementTryCatch(tryCatch); break;
                case DMASTProcStatementThrow dmThrow: ProcessStatementThrow(dmThrow); break;
                case DMASTProcStatementMultipleVarDeclarations multipleVarDeclarations: {
                    foreach (DMASTProcStatementVarDeclaration varDeclaration in multipleVarDeclarations.VarDeclarations) {
                        ProcessStatementVarDeclaration(varDeclaration);
                    }

                    break;
                }
                default: throw new ArgumentException("Invalid proc statement");
            }
        }

        public void ProcessStatementExpression(DMASTProcStatementExpression statement) {
            DMExpression.Emit(_dmObject, _proc, statement.Expression);
            _proc.Pop();
        }

        public void ProcessStatementContinue(DMASTProcStatementContinue statementContinue) {
            _proc.Continue(statementContinue.Label);
        }

        public void ProcessStatementGoto(DMASTProcStatementGoto statementGoto) {
            _proc.Goto(statementGoto.Label.Identifier);
        }

        public void ProcessStatementLabel(DMASTProcStatementLabel statementLabel) {
            _proc.AddLabel(statementLabel.Name + "_codelabel");
            if (statementLabel.Body is not null)
            {
                _proc.StartScope();
                {
                    ProcessBlockInner(statementLabel.Body);
                }
                _proc.EndScope();
                _proc.AddLabel(statementLabel.Name + "_end");
            }
        }

        public void ProcessStatementBreak(DMASTProcStatementBreak statementBreak) {
            _proc.Break(statementBreak.Label);
        }

        public void ProcessStatementSet(DMASTProcStatementSet statementSet)
        {
            var attribute = statementSet.Attribute.ToLower();
            // TODO deal with "src"
            if (!DMExpression.TryConstant(_dmObject, _proc, statementSet.Value, out var constant) && attribute != "src") {
                throw new CompileErrorException(statementSet.Location, $"{attribute} attribute should be a constant");
            }

            switch (statementSet.Attribute.ToLower()) {
                case "waitfor": {
                    _proc.WaitFor(constant.IsTruthy());
                    break;
                }
                case "opendream_unimplemented": {
                    if (constant.IsTruthy())
                    {
                        _proc.Attributes |= ProcAttributes.Unimplemented;
                    }
                    else
                    {
                        _proc.Attributes &= ~ProcAttributes.Unimplemented;
                    }
                    break;
                }
                case "hidden":
                    if (constant.IsTruthy())
                    {
                        _proc.Attributes |= ProcAttributes.Hidden;
                    }
                    else
                    {
                        _proc.Attributes &= ~ProcAttributes.Hidden;
                    }
                    break;
                case "popup_menu":
                    if (constant.IsTruthy()) // The default is to show it so we flag it if it's hidden
                    {
                        _proc.Attributes &= ~ProcAttributes.HidePopupMenu;
                    }
                    else
                    {
                        _proc.Attributes |= ProcAttributes.HidePopupMenu;
                    }

                    DMCompiler.UnimplementedWarning(statementSet.Location, "set popup_menu is not implemented");
                    break;
                case "instant":
                    if (constant.IsTruthy())
                    {
                        _proc.Attributes |= ProcAttributes.Instant;
                    }
                    else
                    {
                        _proc.Attributes &= ~ProcAttributes.Instant;
                    }

                    DMCompiler.UnimplementedWarning(statementSet.Location, "set instant is not implemented");
                    break;
                case "background":
                    if (constant.IsTruthy())
                    {
                        _proc.Attributes |= ProcAttributes.Background;
                    }
                    else
                    {
                        _proc.Attributes &= ~ProcAttributes.Background;
                    }
                    break;
                case "name":
                    DMASTConstantString name = statementSet.Value as DMASTConstantString;
                    if (name is null) throw new CompileErrorException(statementSet.Location, "bad text");
                    _proc.VerbName = name.Value;
                    break;
                case "category":
                    switch (statementSet.Value)
                    {
                        case DMASTConstantString category:
                        {
                            _proc.VerbCategory = category.Value;
                            break;
                        }
                        case DMASTConstantNull:
                            _proc.VerbCategory = null;
                            break;
                        default:
                            throw new CompileErrorException(statementSet.Location, "bad text");
                    }

                    break;
                case "desc":
                    DMASTConstantString desc = statementSet.Value as DMASTConstantString;
                    if (desc is null) throw new CompileErrorException(statementSet.Location, "bad text");
                    _proc.VerbDesc = desc.Value;

                    if (!DMCompiler.Settings.SuppressUnimplementedWarnings) {
                        DMCompiler.Warning(new CompilerWarning(statementSet.Location, "set desc is not implemented"));
                    }
                    break;
                case "invisibility":
                    // The ref says 0-101 for atoms and 0-100 for verbs
                    // BYOND doesn't clamp the actual var value but it does seem to treat out-of-range values as their extreme
                    DMASTConstantFloat invisFloat = statementSet.Value as DMASTConstantFloat;
                    if (invisFloat is null)
                    {
                        DMASTConstantInteger invisInt = statementSet.Value as DMASTConstantInteger;
                        if(invisInt is null) throw new CompileErrorException(statementSet.Location, "bad num");
                        _proc.Invisibility = Convert.ToSByte(Math.Clamp(invisInt.Value, 0, 100));
                    }
                    else
                    {
                        _proc.Invisibility = Convert.ToSByte(Math.Clamp(Math.Floor(invisFloat.Value), 0, 100));
                    }

                    DMCompiler.UnimplementedWarning(statementSet.Location, "set invisibility is not implemented");
                    break;
                case "src":
                    DMCompiler.UnimplementedWarning(statementSet.Location, "set src is not implemented");
                    break;
            }
        }

        public void ProcessStatementDel(DMASTProcStatementDel statementDel) {
            DMExpression.Emit(_dmObject, _proc, statementDel.Value);
            _proc.DeleteObject();
        }

        public void ProcessStatementSpawn(DMASTProcStatementSpawn statementSpawn) {
            DMExpression.Emit(_dmObject, _proc, statementSpawn.Delay);

            string afterSpawnLabel = _proc.NewLabelName();
            _proc.Spawn(afterSpawnLabel);

            ProcessBlockInner(statementSpawn.Body);

            //Prevent the new thread from executing outside its own code
            _proc.PushNull();
            _proc.Return();

            _proc.AddLabel(afterSpawnLabel);
        }

        public void ProcessStatementVarDeclaration(DMASTProcStatementVarDeclaration varDeclaration) {
            if (varDeclaration.IsGlobal) { return; } //Currently handled by DMObjectBuilder

            DMExpression value;
            if (varDeclaration.Value != null) {
                try {
                    value = DMExpression.Create(_dmObject, _proc, varDeclaration.Value, varDeclaration.Type);
                } catch (CompileErrorException e) {
                    DMCompiler.Error(e.Error);
                    value = new Expressions.Null(varDeclaration.Location);
                }
            } else {
                value = new Expressions.Null(varDeclaration.Location);
            }

            bool successful;
            if (varDeclaration.IsConst) {
                if (!value.TryAsConstant(out var constValue)) {
                    DMCompiler.Error(new CompilerError(varDeclaration.Location, "Const var must be set to a constant"));
                    return;
                }

                successful = _proc.TryAddLocalConstVariable(varDeclaration.Name, varDeclaration.Type, constValue);
            } else {
                successful = _proc.TryAddLocalVariable(varDeclaration.Name, varDeclaration.Type);
            }

            if (!successful) {
                DMCompiler.Error(new CompilerError(varDeclaration.Location, $"Duplicate var {varDeclaration.Name}"));
                return;
            }

            value.EmitPushValue(_dmObject, _proc);
            _proc.Assign(_proc.GetLocalVariableReference(varDeclaration.Name));
            _proc.Pop();
        }

        public void ProcessStatementReturn(DMASTProcStatementReturn statement) {
            if (statement.Value != null) {
                DMExpression.Emit(_dmObject, _proc, statement.Value);
            } else {
                _proc.PushReferenceValue(DMReference.Self); //Default return value
            }

            _proc.Return();
        }

        public void ProcessStatementIf(DMASTProcStatementIf statement) {
            DMExpression.Emit(_dmObject, _proc, statement.Condition);

            if (statement.ElseBody == null) {
                string endLabel = _proc.NewLabelName();

                _proc.JumpIfFalse(endLabel);
                _proc.StartScope();
                ProcessBlockInner(statement.Body);
                _proc.EndScope();
                _proc.AddLabel(endLabel);
            } else {
                string elseLabel = _proc.NewLabelName();
                string endLabel = _proc.NewLabelName();

                _proc.JumpIfFalse(elseLabel);

                _proc.StartScope();
                ProcessBlockInner(statement.Body);
                _proc.EndScope();
                _proc.Jump(endLabel);

                _proc.AddLabel(elseLabel);
                ProcessBlockInner(statement.ElseBody);
                _proc.AddLabel(endLabel);
            }
        }

        public void ProcessStatementForStandard(DMASTProcStatementForStandard statementForStandard) {
            _proc.StartScope();
            {
                if (statementForStandard.Initializer != null) {
                    ProcessStatement(statementForStandard.Initializer);
                }

                string loopLabel = _proc.NewLabelName();
                _proc.LoopStart(loopLabel);
                {
                    DMExpression.Emit(_dmObject, _proc, statementForStandard.Comparator);
                    _proc.BreakIfFalse();

                    ProcessBlockInner(statementForStandard.Body);

                    _proc.LoopContinue(loopLabel);
                    if (statementForStandard.Incrementor != null)
                    {
                        DMExpression.Emit(_dmObject, _proc, statementForStandard.Incrementor);
                        _proc.Pop();
                    }
                    _proc.LoopJumpToStart(loopLabel);
                }
                _proc.LoopEnd();
            }
            _proc.EndScope();
        }

        public void ProcessStatementForList(DMASTProcStatementForList statementForList) {
            DMASTProcStatementVarDeclaration varDeclaration = statementForList.Initializer as DMASTProcStatementVarDeclaration;

            DMExpression.Emit(_dmObject, _proc, statementForList.List);
            _proc.CreateListEnumerator();
            _proc.StartScope();
            {
                if (statementForList.Initializer != null) {
                    ProcessStatement(statementForList.Initializer);
                }

                string loopLabel = _proc.NewLabelName();
                _proc.LoopStart(loopLabel);
                {
                    DMExpression outputVariable = DMExpression.Create(_dmObject, _proc, statementForList.Variable);
                    (DMReference outputRef, _) = outputVariable.EmitReference(_dmObject, _proc);
                    _proc.Enumerate(outputRef);
                    _proc.BreakIfFalse();

                    if (varDeclaration != null && varDeclaration.Type != null)
                    {
                        DMExpression.Emit(_dmObject, _proc, statementForList.Variable);
                        _proc.PushPath(varDeclaration.Type.Value);
                        _proc.IsType();

                        _proc.ContinueIfFalse();
                    }

                    ProcessBlockInner(statementForList.Body);

                    _proc.LoopContinue(loopLabel);
                    _proc.LoopJumpToStart(loopLabel);
                }
                _proc.LoopEnd();
            }
            _proc.EndScope();
            _proc.DestroyEnumerator();
        }

        public void ProcessStatementForType(DMASTProcStatementForType statementForType)
        {
            DMASTProcStatementVarDeclaration varDeclaration = statementForType.Initializer as DMASTProcStatementVarDeclaration;

            if (varDeclaration?.Type != null)
            {
                _proc.PushPath(varDeclaration.Type.Value);
                _proc.CreateTypeEnumerator();
            }
            else // This shouldn't happen, just to be safe
            {
                DMCompiler.Error(new CompilerError(varDeclaration.Location, "Attempted to create a type enumerator with a null type"));
                return;
            }

            _proc.StartScope();
            {
                if (statementForType.Initializer != null) {
                    ProcessStatement(statementForType.Initializer);
                }

                string loopLabel = _proc.NewLabelName();
                _proc.LoopStart(loopLabel);
                {
                    DMExpression outputVariable = DMExpression.Create(_dmObject, _proc, statementForType.Variable);
                    (DMReference outputRef, _) = outputVariable.EmitReference(_dmObject, _proc);
                    _proc.Enumerate(outputRef);
                    _proc.BreakIfFalse();

                    ProcessBlockInner(statementForType.Body);

                    _proc.LoopContinue(loopLabel);
                    _proc.LoopJumpToStart(loopLabel);
                }
                _proc.LoopEnd();
            }
            _proc.EndScope();
            _proc.DestroyEnumerator();
        }

        public void ProcessStatementForRange(DMASTProcStatementForRange statementForRange) {
            DMExpression.Emit(_dmObject, _proc, statementForRange.RangeStart);
            DMExpression.Emit(_dmObject, _proc, statementForRange.RangeEnd);
            DMExpression.Emit(_dmObject, _proc, statementForRange.Step);
            _proc.CreateRangeEnumerator();
            _proc.StartScope();
            {
                if (statementForRange.Initializer != null) {
                    ProcessStatement(statementForRange.Initializer);
                }

                string loopLabel = _proc.NewLabelName();
                _proc.LoopStart(loopLabel);
                {
                    DMExpression outputVariable = DMExpression.Create(_dmObject, _proc, statementForRange.Variable);
                    (DMReference outputRef, _) = outputVariable.EmitReference(_dmObject, _proc);
                    _proc.Enumerate(outputRef);
                    _proc.BreakIfFalse();

                    ProcessBlockInner(statementForRange.Body);

                    _proc.LoopContinue(loopLabel);
                    _proc.LoopJumpToStart(loopLabel);
                }
                _proc.LoopEnd();
            }
            _proc.EndScope();
            _proc.DestroyEnumerator();
        }

        //Generic infinite loop, while loops with static expression as their conditional with positive truthfullness get turned into this as well as empty for() calls
        public void ProcessStatementInfLoop(DMASTProcStatementInfLoop statementInfLoop){
            _proc.StartScope();
            {
                string loopLabel = _proc.NewLabelName();
                _proc.LoopStart(loopLabel);
                {
                    ProcessBlockInner(statementInfLoop.Body);
                    _proc.LoopContinue(loopLabel);
                    _proc.LoopJumpToStart(loopLabel);
                }
                _proc.LoopEnd();
            }
            _proc.EndScope();
        }

        public void ProcessStatementWhile(DMASTProcStatementWhile statementWhile) {
            string loopLabel = _proc.NewLabelName();

            _proc.LoopStart(loopLabel);
            {
                DMExpression.Emit(_dmObject, _proc, statementWhile.Conditional);
                _proc.BreakIfFalse();

                _proc.StartScope();
                {
                    ProcessBlockInner(statementWhile.Body);

                    _proc.LoopContinue(loopLabel);
                    _proc.LoopJumpToStart(loopLabel);
                }
                _proc.EndScope();
            }
            _proc.LoopEnd();
        }

        public void ProcessStatementDoWhile(DMASTProcStatementDoWhile statementDoWhile) {
            string loopLabel = _proc.NewLabelName();
            string loopEndLabel = _proc.NewLabelName();

            _proc.LoopStart(loopLabel);
            {
                ProcessBlockInner(statementDoWhile.Body);

                _proc.LoopContinue(loopLabel);
                DMExpression.Emit(_dmObject, _proc, statementDoWhile.Conditional);
                _proc.JumpIfFalse(loopEndLabel);
                _proc.LoopJumpToStart(loopLabel);

                _proc.AddLabel(loopEndLabel);
                _proc.Break();
            }
            _proc.LoopEnd();
        }

        public void ProcessStatementSwitch(DMASTProcStatementSwitch statementSwitch) {
            string endLabel = _proc.NewLabelName();
            List<(string CaseLabel, DMASTProcBlockInner CaseBody)> valueCases = new();
            DMASTProcBlockInner defaultCaseBody = null;

            DMExpression.Emit(_dmObject, _proc, statementSwitch.Value);
            foreach (DMASTProcStatementSwitch.SwitchCase switchCase in statementSwitch.Cases) {
                if (switchCase is DMASTProcStatementSwitch.SwitchCaseValues switchCaseValues) {
                    string caseLabel = _proc.NewLabelName();

                    foreach (DMASTExpression value in switchCaseValues.Values) {
                        if (value is DMASTSwitchCaseRange range) {
                            if (!DMExpression.TryConstant(_dmObject, _proc, range.RangeStart, out var lower))
                                throw new CompileErrorException(new CompilerError(range.RangeStart.Location, "Expected a constant"));
                            if (!DMExpression.TryConstant(_dmObject, _proc, range.RangeEnd, out var upper))
                                throw new CompileErrorException(new CompilerError(range.RangeEnd.Location, "Expected a constant"));

                            lower.EmitPushValue(_dmObject, _proc);
                            upper.EmitPushValue(_dmObject, _proc);
                            _proc.SwitchCaseRange(caseLabel);
                        } else {
                            if (!DMExpression.TryConstant(_dmObject, _proc, value, out var constant))
                                throw new CompileErrorException(new CompilerError(value.Location, "Expected a constant"));

                            constant.EmitPushValue(_dmObject, _proc);
                            _proc.SwitchCase(caseLabel);
                        }
                    }

                    valueCases.Add((caseLabel, switchCase.Body));
                } else {
                    defaultCaseBody = ((DMASTProcStatementSwitch.SwitchCaseDefault)switchCase).Body;
                }
            }
            _proc.Pop();

            if (defaultCaseBody != null) {
                _proc.StartScope();
                {
                    ProcessBlockInner(defaultCaseBody);
                }
                _proc.EndScope();
            }
            _proc.Jump(endLabel);

            foreach ((string CaseLabel, DMASTProcBlockInner CaseBody) valueCase in valueCases) {
                _proc.AddLabel(valueCase.CaseLabel);
                _proc.StartScope();
                {
                    ProcessBlockInner(valueCase.CaseBody);
                }
                _proc.EndScope();
                _proc.Jump(endLabel);
            }

            _proc.AddLabel(endLabel);
        }

        public void ProcessStatementBrowse(DMASTProcStatementBrowse statementBrowse) {
            DMExpression.Emit(_dmObject, _proc, statementBrowse.Receiver);
            DMExpression.Emit(_dmObject, _proc, statementBrowse.Body);
            DMExpression.Emit(_dmObject, _proc, statementBrowse.Options);
            _proc.Browse();
        }

        public void ProcessStatementBrowseResource(DMASTProcStatementBrowseResource statementBrowseResource) {
            DMExpression.Emit(_dmObject, _proc, statementBrowseResource.Receiver);
            DMExpression.Emit(_dmObject, _proc, statementBrowseResource.File);
            DMExpression.Emit(_dmObject, _proc, statementBrowseResource.Filename);
            _proc.BrowseResource();
        }

        public void ProcessStatementOutputControl(DMASTProcStatementOutputControl statementOutputControl) {
            DMExpression.Emit(_dmObject, _proc, statementOutputControl.Receiver);
            DMExpression.Emit(_dmObject, _proc, statementOutputControl.Message);
            DMExpression.Emit(_dmObject, _proc, statementOutputControl.Control);
            _proc.OutputControl();
        }

        public void ProcessStatementTryCatch(DMASTProcStatementTryCatch tryCatch) {
            string catchLabel = _proc.NewLabelName();
            string endLabel = _proc.NewLabelName();

            _proc.StartScope();
            ProcessBlockInner(tryCatch.TryBody);
            _proc.EndScope();
            _proc.Jump(endLabel);

            if (tryCatch.CatchParameter != null)
            {
                //TODO set the value to what is thrown in try
                var param = tryCatch.CatchParameter as DMASTProcStatementVarDeclaration;
                if (!_proc.TryAddLocalVariable(param.Name, param.Type)) {
                    DMCompiler.Error(new CompilerError(param.Location, $"Duplicate var {param.Name}"));
                }
            }

            //TODO make catching actually work
            _proc.AddLabel(catchLabel);
            if (tryCatch.CatchBody != null) {
                _proc.StartScope();
                ProcessBlockInner(tryCatch.CatchBody);
                _proc.EndScope();
            }
            _proc.AddLabel(endLabel);

        }

        public void ProcessStatementThrow(DMASTProcStatementThrow statement) {
            //TODO proper value handling and catching

            DMExpression.Emit(_dmObject, _proc, statement.Value);
            _proc.Throw();
        }
    }
}
