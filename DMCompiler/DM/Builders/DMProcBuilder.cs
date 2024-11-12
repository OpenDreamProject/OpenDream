using System.Diagnostics;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;
using DMCompiler.Compiler.DM;
using DMCompiler.Compiler.DM.AST;
using DMCompiler.DM.Expressions;

namespace DMCompiler.DM.Builders {
    internal sealed class DMProcBuilder(DMObject dmObject, DMProc proc) {
        /// <summary>
        /// BYOND currently has a ridiculous behaviour, where, <br/>
        /// sometimes when a set statement has a right-hand side that is non-constant, <br/>
        /// no error is emitted and instead its value is just, whatever the last well-evaluated set statement's value was. <br/>
        /// This behaviour is nonsense but for harsh parity we sometimes may need to carry it out to hold up a codebase; <br/>
        /// Yogstation (at time of writing) actually errors on OD if we don't implement this.
        /// </summary>
        // Starts null; marks that we've never seen one before and should just error like normal people.
        private Constant? _previousSetStatementValue;

        public void ProcessProcDefinition(DMASTProcDefinition procDefinition) {
            if (procDefinition.Body == null) return;

            foreach (DMASTDefinitionParameter parameter in procDefinition.Parameters) {
                string parameterName = parameter.Name;

                if (parameter.Value != null) { //Parameter has a default value
                    string afterDefaultValueCheck = proc.NewLabelName();
                    DMReference parameterRef = proc.GetLocalVariableReference(parameterName);

                    //Don't set parameter to default if not null
                    proc.PushReferenceValue(parameterRef);
                    proc.IsNull();
                    proc.JumpIfFalse(afterDefaultValueCheck);

                    //Set default
                    DMExpression.Emit(dmObject, proc, parameter.Value, parameter.ObjectType);
                    proc.Assign(parameterRef);
                    proc.Pop();

                    proc.AddLabel(afterDefaultValueCheck);
                }
            }

            ProcessBlockInner(procDefinition.Body, silenceEmptyBlockWarning : true);
            proc.ResolveLabels();
        }

        /// <param name="silenceEmptyBlockWarning">Used to avoid emitting noisy warnings about procs with nothing in them. <br/>
        /// FIXME: Eventually we should try to be smart enough to emit the error anyways for procs that <br/>
        /// A.) are not marked opendream_unimplemented and <br/>
        /// B.) have no descendant proc which actually has code in it (implying that this proc is just some abstract virtual for it)
        /// </param>
        private void ProcessBlockInner(DMASTProcBlockInner block, bool silenceEmptyBlockWarning = false) {
            // Done first because all set statements are "hoisted" -- evaluated before any code in the block is run
            foreach (var stmt in block.SetStatements) {
                Debug.Assert(stmt.IsAggregateOr<DMASTProcStatementSet>(), "Non-set statements were located in the block's SetStatements array! This is a bug.");

                ProcessStatement(stmt);
            }

            if(!silenceEmptyBlockWarning && block.Statements.Length == 0) { // If this block has no real statements
                // Not an error in BYOND, but we do have an emission for this!
                if (block.SetStatements.Length != 0) {
                    // Give a more articulate message about this, since it's kinda weird
                    DMCompiler.Emit(WarningCode.EmptyBlock,block.Location,"Empty block detected - set statements are executed outside of, before, and unconditional to, this block");
                } else {
                    DMCompiler.Emit(WarningCode.EmptyBlock,block.Location,"Empty block detected");
                }

                return;
            }

            foreach (DMASTProcStatement statement in block.Statements) {
                proc.DebugSource(statement.Location);
                ProcessStatement(statement);
            }
        }

        public void ProcessStatement(DMASTProcStatement statement) {
            switch (statement) {
                case DMASTInvalidProcStatement: break;
                case DMASTNullProcStatement: break;
                case DMASTProcStatementExpression statementExpression: ProcessStatementExpression(statementExpression); break;
                case DMASTProcStatementContinue statementContinue: ProcessStatementContinue(statementContinue); break;
                case DMASTProcStatementGoto statementGoto: ProcessStatementGoto(statementGoto); break;
                case DMASTProcStatementLabel statementLabel: ProcessStatementLabel(statementLabel); break;
                case DMASTProcStatementBreak statementBreak: ProcessStatementBreak(statementBreak); break;
                case DMASTProcStatementDel statementDel: ProcessStatementDel(statementDel); break;
                case DMASTProcStatementSpawn statementSpawn: ProcessStatementSpawn(statementSpawn); break;
                case DMASTProcStatementReturn statementReturn: ProcessStatementReturn(statementReturn); break;
                case DMASTProcStatementIf statementIf: ProcessStatementIf(statementIf); break;
                case DMASTProcStatementFor statementFor: ProcessStatementFor(statementFor); break;
                case DMASTProcStatementInfLoop statementInfLoop: ProcessStatementInfLoop(statementInfLoop); break;
                case DMASTProcStatementWhile statementWhile: ProcessStatementWhile(statementWhile); break;
                case DMASTProcStatementDoWhile statementDoWhile: ProcessStatementDoWhile(statementDoWhile); break;
                case DMASTProcStatementSwitch statementSwitch: ProcessStatementSwitch(statementSwitch); break;
                case DMASTProcStatementBrowse statementBrowse: ProcessStatementBrowse(statementBrowse); break;
                case DMASTProcStatementBrowseResource statementBrowseResource: ProcessStatementBrowseResource(statementBrowseResource); break;
                case DMASTProcStatementOutputControl statementOutputControl: ProcessStatementOutputControl(statementOutputControl); break;
                case DMASTProcStatementFtp statementFtp: ProcessStatementFtp(statementFtp); break;
                case DMASTProcStatementOutput statementOutput: ProcessStatementOutput(statementOutput); break;
                case DMASTProcStatementInput statementInput: ProcessStatementInput(statementInput); break;
                case DMASTProcStatementVarDeclaration varDeclaration: ProcessStatementVarDeclaration(varDeclaration); break;
                case DMASTProcStatementTryCatch tryCatch: ProcessStatementTryCatch(tryCatch); break;
                case DMASTProcStatementThrow dmThrow: ProcessStatementThrow(dmThrow); break;
                case DMASTProcStatementSet statementSet: ProcessStatementSet(statementSet); break;
                //NOTE: Is there a more generic way of doing this, where Aggregate doesn't need every possible type state specified here?
                //      please write such generic thing if more than three aggregates show up in this switch.
                case DMASTAggregate<DMASTProcStatementSet> gregSet: // Hi Greg
                    foreach (var setStatement in gregSet.Statements)
                        ProcessStatementSet(setStatement);
                    break;
                case DMASTAggregate<DMASTProcStatementVarDeclaration> gregVar:
                    foreach (var declare in gregVar.Statements)
                        ProcessStatementVarDeclaration(declare);
                    break;
                default:
                    DMCompiler.ForcedError(statement.Location, $"Invalid proc statement {statement.GetType()}");
                    break;
            }
        }

        public void ProcessStatementExpression(DMASTProcStatementExpression statement) {
            DMExpression.Emit(dmObject, proc, statement.Expression);
            proc.Pop();
        }

        public void ProcessStatementContinue(DMASTProcStatementContinue statementContinue) {
            proc.Continue(statementContinue.Label);
        }

        public void ProcessStatementGoto(DMASTProcStatementGoto statementGoto) {
            proc.Goto(statementGoto.Label);
        }

        public void ProcessStatementLabel(DMASTProcStatementLabel statementLabel) {
            var codeLabel = proc.TryAddCodeLabel(statementLabel.Name);
            var labelName = codeLabel?.LabelName ?? statementLabel.Name;

            proc.AddLabel(labelName);

            if (statementLabel.Body is not null) {
                proc.StartScope();
                {
                    ProcessBlockInner(statementLabel.Body);
                }
                proc.EndScope();
                proc.AddLabel(labelName + "_end");
            }
        }

        public void ProcessStatementBreak(DMASTProcStatementBreak statementBreak) {
            proc.Break(statementBreak.Label);
        }

        public void ProcessStatementSet(DMASTProcStatementSet statementSet) {
            var attribute = statementSet.Attribute.ToLower();

            if(attribute == "src") {
                // TODO: Would be much better if the parser was just more strict with the expression
                switch (statementSet.Value) {
                    case DMASTIdentifier {Identifier: "usr"}:
                        proc.VerbSrc = statementSet.WasInKeyword ? VerbSrc.InUsr : VerbSrc.Usr;
                        break;
                    case DMASTDereference {Expression: DMASTIdentifier{Identifier: "usr"}, Operations: var operations}:
                        if (operations is not [DMASTDereference.FieldOperation {Identifier: var deref}])
                            goto default;

                        if (deref == "contents") {
                            proc.VerbSrc = VerbSrc.InUsr;
                        }  else if (deref == "loc") {
                            proc.VerbSrc = VerbSrc.UsrLoc;
                            DMCompiler.UnimplementedWarning(statementSet.Location,
                                "'set src = usr.loc' is unimplemented");
                        } else if (deref == "group") {
                            proc.VerbSrc = VerbSrc.UsrGroup;
                            DMCompiler.UnimplementedWarning(statementSet.Location,
                                "'set src = usr.group' is unimplemented");
                        } else {
                            goto default;
                        }

                        break;
                    case DMASTIdentifier {Identifier: "world"}:
                        proc.VerbSrc = statementSet.WasInKeyword ? VerbSrc.InWorld : VerbSrc.World;
                        if (statementSet.WasInKeyword)
                            DMCompiler.UnimplementedWarning(statementSet.Location,
                                "'set src = world.contents' is unimplemented");
                        else
                            DMCompiler.UnimplementedWarning(statementSet.Location,
                                "'set src = world' is unimplemented");
                        break;
                    case DMASTDereference {Expression: DMASTIdentifier{Identifier: "world"}, Operations: var operations}:
                        if (operations is not [DMASTDereference.FieldOperation {Identifier: "contents"}])
                            goto default;

                        proc.VerbSrc = VerbSrc.InWorld;
                        DMCompiler.UnimplementedWarning(statementSet.Location,
                            "'set src = world.contents' is unimplemented");
                        break;
                    case DMASTProcCall {Callable: DMASTCallableProcIdentifier {Identifier: { } viewType and ("view" or "oview")}}:
                        // TODO: Ranges
                        if (statementSet.WasInKeyword)
                            proc.VerbSrc = viewType == "view" ? VerbSrc.InView : VerbSrc.InOView;
                        else
                            proc.VerbSrc = viewType == "view" ? VerbSrc.View : VerbSrc.OView;
                        break;
                    // range() and orange() are undocumented, but they work
                    case DMASTProcCall {Callable: DMASTCallableProcIdentifier {Identifier: { } viewType and ("range" or "orange")}}:
                        // TODO: Ranges
                        if (statementSet.WasInKeyword)
                            proc.VerbSrc = viewType == "range" ? VerbSrc.InRange : VerbSrc.InORange;
                        else
                            proc.VerbSrc = viewType == "range" ? VerbSrc.Range : VerbSrc.ORange;
                        break;
                    default:
                        DMCompiler.Emit(WarningCode.BadExpression, statementSet.Value.Location, "Invalid verb src");
                        break;
                }

                return;
            }

            if (!DMExpression.TryConstant(dmObject, proc, statementSet.Value, out var constant)) { // If this set statement's rhs is not constant
                bool didError = DMCompiler.Emit(WarningCode.InvalidSetStatement, statementSet.Location, $"'{attribute}' attribute should be a constant");
                if (didError) // if this is an error
                    return; // don't do the cursed thing

                constant = _previousSetStatementValue;
            } else {
                _previousSetStatementValue = constant;
            }

            // oh no.
            if (constant is null) {
                DMCompiler.Emit(WarningCode.BadExpression, statementSet.Location, $"'{attribute}' attribute must be a constant");
                return;
            }

            // Check if it was 'set x in y' or whatever
            // (which is illegal for everything except setting src to something)
            if (statementSet.WasInKeyword) {
                DMCompiler.Emit(WarningCode.BadToken, statementSet.Location, "Use of 'in' keyword is illegal here. Did you mean '='?");
                //fallthrough into normal behaviour because this error is kinda pedantic
            }

            switch (statementSet.Attribute.ToLower()) {
                case "waitfor": {
                    proc.WaitFor(constant.IsTruthy());
                    break;
                }
                case "opendream_unimplemented": {
                    if (constant.IsTruthy())
                        proc.Attributes |= ProcAttributes.Unimplemented;
                    else
                        proc.Attributes &= ~ProcAttributes.Unimplemented;
                    break;
                }
                case "hidden":
                    if (constant.IsTruthy())
                        proc.Attributes |= ProcAttributes.Hidden;
                    else
                        proc.Attributes &= ~ProcAttributes.Hidden;
                    break;
                case "popup_menu":
                    if (constant.IsTruthy()) // The default is to show it so we flag it if it's hidden
                        proc.Attributes &= ~ProcAttributes.HidePopupMenu;
                    else
                        proc.Attributes |= ProcAttributes.HidePopupMenu;
                    break;
                case "instant":
                    if (constant.IsTruthy())
                        proc.Attributes |= ProcAttributes.Instant;
                    else
                        proc.Attributes &= ~ProcAttributes.Instant;

                    DMCompiler.UnimplementedWarning(statementSet.Location, "set instant is not implemented");
                    break;
                case "background":
                    if (constant.IsTruthy())
                        proc.Attributes |= ProcAttributes.Background;
                    else
                        proc.Attributes &= ~ProcAttributes.Background;
                    break;
                case "name":
                    if (constant is not Expressions.String nameStr) {
                        DMCompiler.Emit(WarningCode.BadExpression, constant.Location, "name attribute must be a string");
                        break;
                    }

                    proc.VerbName = nameStr.Value;
                    break;
                case "category":
                    if (constant is Expressions.String str) {
                        proc.VerbCategory = str.Value;
                    } else if (constant is Null) {
                        proc.VerbCategory = null;
                    } else {
                        DMCompiler.Emit(WarningCode.BadExpression, constant.Location,
                            "category attribute must be a string or null");
                    }

                    break;
                case "desc":
                    // TODO: verb.desc is supposed to be printed when you type the verb name and press F1. Check the ref for details.
                    if (constant is not Expressions.String descStr) {
                        DMCompiler.Emit(WarningCode.BadExpression, constant.Location, "desc attribute must be a string");
                        break;
                    }

                    proc.VerbDesc = descStr.Value;
                    break;
                case "invisibility":
                    // The ref says 0-101 for atoms and 0-100 for verbs
                    // BYOND doesn't clamp the actual var value but it does seem to treat out-of-range values as their extreme
                    if (constant is not Number invisNum) {
                        DMCompiler.Emit(WarningCode.BadExpression, constant.Location, "invisibility attribute must be an int");
                        break;
                    }

                    proc.Invisibility = Convert.ToSByte(Math.Clamp(MathF.Floor(invisNum.Value), 0f, 100f));
                    break;
                case "src":
                    DMCompiler.UnimplementedWarning(statementSet.Location, "set src is not implemented");
                    break;
            }
        }

        public void ProcessStatementDel(DMASTProcStatementDel statementDel) {
            DMExpression.Emit(dmObject, proc, statementDel.Value);
            proc.DeleteObject();
        }

        public void ProcessStatementSpawn(DMASTProcStatementSpawn statementSpawn) {
            DMExpression.Emit(dmObject, proc, statementSpawn.Delay);

            string afterSpawnLabel = proc.NewLabelName();
            proc.Spawn(afterSpawnLabel);

            proc.StartScope();
            {
                ProcessBlockInner(statementSpawn.Body);

                //Prevent the new thread from executing outside its own code
                proc.PushNull();
                proc.Return();
            }
            proc.EndScope();

            proc.AddLabel(afterSpawnLabel);
        }

        public void ProcessStatementVarDeclaration(DMASTProcStatementVarDeclaration varDeclaration) {
            if (varDeclaration.IsGlobal) { return; } //Currently handled by DMObjectBuilder

            DMExpression value;
            if (varDeclaration.Value != null) {
                value = DMExpression.Create(dmObject, proc, varDeclaration.Value, varDeclaration.Type);

                if (!varDeclaration.ValType.MatchesType(value.ValType)) {
                    DMCompiler.Emit(WarningCode.InvalidVarType, varDeclaration.Location,
                        $"{varDeclaration.Name}: Invalid var value {value.ValType}, expected {varDeclaration.ValType}");
                }
            } else {
                value = new Null(varDeclaration.Location);
            }

            bool successful;
            if (varDeclaration.IsConst) {
                if (!value.TryAsConstant(out var constValue)) {
                    DMCompiler.Emit(WarningCode.HardConstContext, varDeclaration.Location, "Const var must be set to a constant");
                    return;
                }

                successful = proc.TryAddLocalConstVariable(varDeclaration.Name, varDeclaration.Type, constValue);
            } else {
                successful = proc.TryAddLocalVariable(varDeclaration.Name, varDeclaration.Type, varDeclaration.ValType);
            }

            if (!successful) {
                DMCompiler.Emit(WarningCode.DuplicateVariable, varDeclaration.Location, $"Duplicate var {varDeclaration.Name}");
                return;
            }

            value.EmitPushValue(dmObject, proc);
            proc.Assign(proc.GetLocalVariableReference(varDeclaration.Name));
            proc.Pop();
        }

        public void ProcessStatementReturn(DMASTProcStatementReturn statement) {
            if (statement.Value != null) {
                var expr = DMExpression.Create(dmObject, proc, statement.Value);

                // Don't type-check unimplemented procs
                if (proc.TypeChecked && (proc.Attributes & ProcAttributes.Unimplemented) == 0) {
                    if (expr.TryAsConstant(out var exprConst)) {
                        proc.ValidateReturnType(exprConst);
                    } else {
                        proc.ValidateReturnType(expr);
                    }
                }

                expr.EmitPushValue(dmObject, proc);
            } else {
                proc.PushReferenceValue(DMReference.Self); //Default return value
            }

            proc.Return();
        }

        public void ProcessStatementIf(DMASTProcStatementIf statement) {
            DMExpression.Emit(dmObject, proc, statement.Condition);

            if (statement.ElseBody == null) {
                string endLabel = proc.NewLabelName();

                proc.JumpIfFalse(endLabel);
                proc.StartScope();
                ProcessBlockInner(statement.Body);
                proc.EndScope();
                proc.AddLabel(endLabel);
            } else {
                string elseLabel = proc.NewLabelName();
                string endLabel = proc.NewLabelName();

                proc.JumpIfFalse(elseLabel);

                proc.StartScope();
                ProcessBlockInner(statement.Body);
                proc.EndScope();
                proc.Jump(endLabel);

                proc.AddLabel(elseLabel);
                proc.StartScope();
                ProcessBlockInner(statement.ElseBody);
                proc.EndScope();
                proc.AddLabel(endLabel);
            }
        }

        public void ProcessStatementFor(DMASTProcStatementFor statementFor) {
            proc.StartScope();
            {
                foreach (var decl in FindVarDecls(statementFor.Expression1)) {
                    ProcessStatementVarDeclaration(new DMASTProcStatementVarDeclaration(statementFor.Location, decl.DeclPath, null, DMValueType.Anything));
                }

                if (statementFor.Expression2 != null || statementFor.Expression3 != null) {
                    var initializer = statementFor.Expression1 != null ? DMExpression.Create(dmObject, proc, statementFor.Expression1) : null;
                    var comparator = statementFor.Expression2 != null ? DMExpression.Create(dmObject, proc, statementFor.Expression2) : null;
                    var incrementor = statementFor.Expression3 != null ? DMExpression.Create(dmObject, proc, statementFor.Expression3) : null;

                    ProcessStatementForStandard(initializer, comparator, incrementor, statementFor.Body);
                } else {
                    switch (statementFor.Expression1) {
                        case DMASTAssign {LHS: DMASTVarDeclExpression decl, RHS: DMASTExpressionInRange range}: {
                            var initializer = statementFor.Expression1 != null ? DMExpression.Create(dmObject, proc, statementFor.Expression1) : null;
                            var identifier = new DMASTIdentifier(decl.Location, decl.DeclPath.Path.LastElement);
                            var outputVar = DMExpression.Create(dmObject, proc, identifier);

                            var start = DMExpression.Create(dmObject, proc, range.StartRange);
                            var end = DMExpression.Create(dmObject, proc, range.EndRange);
                            var step = range.Step != null
                                ? DMExpression.Create(dmObject, proc, range.Step)
                                : new Number(range.Location, 1);

                            ProcessStatementForRange(initializer, outputVar, start, end, step, statementFor.Body);
                            break;
                        }
                        case DMASTExpressionInRange exprRange: {
                            DMASTVarDeclExpression? decl = exprRange.Value as DMASTVarDeclExpression;
                            decl ??= exprRange.Value is DMASTAssign assign
                                ? assign.LHS as DMASTVarDeclExpression
                                : null;

                            DMASTExpression outputExpr;
                            if (decl != null) {
                                outputExpr = new DMASTIdentifier(exprRange.Value.Location, decl.DeclPath.Path.LastElement);
                            } else {
                                outputExpr = exprRange.Value;
                            }

                            var outputVar = DMExpression.Create(dmObject, proc, outputExpr);

                            var start = DMExpression.Create(dmObject, proc, exprRange.StartRange);
                            var end = DMExpression.Create(dmObject, proc, exprRange.EndRange);
                            var step = exprRange.Step != null
                                ? DMExpression.Create(dmObject, proc, exprRange.Step)
                                : new Number(exprRange.Location, 1);

                            ProcessStatementForRange(null, outputVar, start, end, step, statementFor.Body);
                            break;
                        }
                        case DMASTVarDeclExpression vd: {
                            var initializer = statementFor.Expression1 != null ? DMExpression.Create(dmObject, proc, statementFor.Expression1) : null;
                            var declInfo = new ProcVarDeclInfo(vd.DeclPath.Path);
                            var identifier = new DMASTIdentifier(vd.Location, declInfo.VarName);
                            var outputVar = DMExpression.Create(dmObject, proc, identifier);

                            ProcessStatementForType(initializer, outputVar, declInfo.TypePath, statementFor.Body);
                            break;
                        }
                        case DMASTExpressionIn exprIn: {
                            DMASTExpression outputExpr;
                            if (exprIn.LHS is DMASTVarDeclExpression decl) {
                                outputExpr = new DMASTIdentifier(decl.Location, decl.DeclPath.Path.LastElement);
                            } else {
                                outputExpr = exprIn.LHS;
                            }

                            var outputVar = DMExpression.Create(dmObject, proc, outputExpr);
                            var list = DMExpression.Create(dmObject, proc, exprIn.RHS);

                            if (outputVar is Local outputLocal) {
                                outputLocal.LocalVar.ExplicitValueType = statementFor.DMTypes;
                            }

                            ProcessStatementForList(list, outputVar, statementFor.DMTypes, statementFor.Body);
                            break;
                        }
                        default:
                            DMCompiler.Emit(WarningCode.BadExpression, statementFor.Location, "Invalid expression in for");
                            break;
                    }
                }
            }
            proc.EndScope();

            IEnumerable<DMASTVarDeclExpression> FindVarDecls(DMASTExpression expr) {
                if (expr is DMASTVarDeclExpression p) {
                    yield return p;
                }
                foreach (var leaf in expr.Leaves()) {
                    foreach(var decl in FindVarDecls(leaf)) {
                        yield return decl;
                    }
                }
            }
        }

        public void ProcessStatementForStandard(DMExpression? initializer, DMExpression? comparator, DMExpression? incrementor, DMASTProcBlockInner body) {
            proc.StartScope();
            {
                if (initializer != null) {
                    initializer.EmitPushValue(dmObject, proc);
                    proc.Pop();
                }

                string loopLabel = proc.NewLabelName();
                proc.LoopStart(loopLabel);
                {
                    if (comparator != null) {
                        comparator.EmitPushValue(dmObject, proc);
                        proc.BreakIfFalse();
                    }

                    ProcessBlockInner(body);

                    proc.MarkLoopContinue(loopLabel);
                    if (incrementor != null) {
                        incrementor.EmitPushValue(dmObject, proc);
                        proc.Pop();
                    }
                    proc.LoopJumpToStart(loopLabel);
                }
                proc.LoopEnd();
            }
            proc.EndScope();
        }

        public void ProcessLoopAssignment(LValue lValue) {
            if (lValue.CanReferenceShortCircuit()) {
                string endLabel = proc.NewLabelName();
                string endLabel2 = proc.NewLabelName();

                DMReference outputRef = lValue.EmitReference(dmObject, proc, endLabel, DMExpression.ShortCircuitMode.PopNull);
                proc.Enumerate(outputRef);
                proc.Jump(endLabel2);

                proc.AddLabel(endLabel);
                proc.EnumerateNoAssign();
                proc.AddLabel(endLabel2);
            } else {
                DMReference outputRef = lValue.EmitReference(dmObject, proc, null);
                proc.Enumerate(outputRef);
            }
        }

        public void ProcessStatementForList(DMExpression list, DMExpression outputVar, DMComplexValueType? dmTypes, DMASTProcBlockInner body) {
            if (outputVar is not LValue lValue) {
                DMCompiler.Emit(WarningCode.BadExpression, outputVar.Location, "Invalid output var");
                lValue = null;
            }

            // Depending on the var's type and possibly a given "as [types]", an implicit istype() check is performed
            DreamPath? implicitTypeCheck = null;
            if (dmTypes == null) {
                // No "as" means the var's type will be used
                implicitTypeCheck = lValue?.Path;
            } else if (dmTypes.Value.TypePath != null) {
                // "as /datum" will perform a check for /datum
                implicitTypeCheck = dmTypes.Value.TypePath;
            } else if (!dmTypes.Value.IsAnything) {
                // "as anything" performs no check. Other values are unimplemented.
                DMCompiler.UnimplementedWarning(outputVar.Location,
                    $"As type {dmTypes} in for loops is unimplemented. No type check will be performed.");
            }

            list.EmitPushValue(dmObject, proc);
            if (implicitTypeCheck != null) {
                if (DMObjectTree.TryGetTypeId(implicitTypeCheck.Value, out var filterTypeId)) {
                    // Create an enumerator that will do the implicit istype() for us
                    proc.CreateFilteredListEnumerator(filterTypeId, implicitTypeCheck.Value);
                } else {
                    DMCompiler.Emit(WarningCode.ItemDoesntExist, outputVar.Location,
                        $"Cannot filter enumeration by type {implicitTypeCheck.Value}, it does not exist");
                    proc.CreateListEnumerator();
                }
            } else {
                proc.CreateListEnumerator();
            }

            proc.StartScope();
            {
                string loopLabel = proc.NewLabelName();
                proc.LoopStart(loopLabel);
                {
                    proc.MarkLoopContinue(loopLabel);

                    if (lValue != null) {
                        ProcessLoopAssignment(lValue);
                    }

                    ProcessBlockInner(body);
                    proc.LoopJumpToStart(loopLabel);
                }
                proc.LoopEnd();
            }
            proc.EndScope();
            proc.DestroyEnumerator();
        }

        public void ProcessStatementForType(DMExpression? initializer, DMExpression outputVar, DreamPath? type, DMASTProcBlockInner body) {
            if (type == null) {
                // This shouldn't happen, just to be safe
                DMCompiler.ForcedError(initializer.Location,
                    "Attempted to create a type enumerator with a null type");
                return;
            }

            if (DMObjectTree.TryGetTypeId(type.Value, out var typeId)) {
                proc.PushType(typeId);
                proc.CreateTypeEnumerator();
            } else {
                DMCompiler.Emit(WarningCode.ItemDoesntExist, initializer.Location, $"Type {type.Value} does not exist");
            }

            proc.StartScope();
            {
                if (initializer != null) {
                    initializer.EmitPushValue(dmObject, proc);
                    proc.Pop();
                }

                string loopLabel = proc.NewLabelName();
                proc.LoopStart(loopLabel);
                {
                    proc.MarkLoopContinue(loopLabel);

                    if (outputVar is Expressions.LValue lValue) {
                        ProcessLoopAssignment(lValue);
                    } else {
                        DMCompiler.Emit(WarningCode.BadExpression, outputVar.Location, "Invalid output var");
                    }

                    ProcessBlockInner(body);
                    proc.LoopJumpToStart(loopLabel);
                }
                proc.LoopEnd();
            }
            proc.EndScope();
            proc.DestroyEnumerator();
        }

        public void ProcessStatementForRange(DMExpression? initializer, DMExpression outputVar, DMExpression start, DMExpression end, DMExpression? step, DMASTProcBlockInner body) {
            start.EmitPushValue(dmObject, proc);
            end.EmitPushValue(dmObject, proc);
            if (step != null) {
                step.EmitPushValue(dmObject, proc);
            } else {
                proc.PushFloat(1.0f);
            }

            proc.CreateRangeEnumerator();
            proc.StartScope();
            {
                if (initializer != null) {
                    initializer.EmitPushValue(dmObject, proc);
                    proc.Pop();
                }

                string loopLabel = proc.NewLabelName();
                proc.LoopStart(loopLabel);
                {
                    proc.MarkLoopContinue(loopLabel);

                    if (outputVar is Expressions.LValue lValue) {
                        ProcessLoopAssignment(lValue);
                    } else {
                        DMCompiler.Emit(WarningCode.BadExpression, outputVar.Location, "Invalid output var");
                    }

                    ProcessBlockInner(body);
                    proc.LoopJumpToStart(loopLabel);
                }
                proc.LoopEnd();
            }
            proc.EndScope();
            proc.DestroyEnumerator();
        }

        //Generic infinite loop, while loops with static expression as their conditional with positive truthfullness get turned into this as well as empty for() calls
        public void ProcessStatementInfLoop(DMASTProcStatementInfLoop statementInfLoop){
            proc.StartScope();
            {
                string loopLabel = proc.NewLabelName();
                proc.LoopStart(loopLabel);
                {
                    proc.MarkLoopContinue(loopLabel);
                    ProcessBlockInner(statementInfLoop.Body);
                    proc.LoopJumpToStart(loopLabel);
                }
                proc.LoopEnd();
            }
            proc.EndScope();
        }

        public void ProcessStatementWhile(DMASTProcStatementWhile statementWhile) {
            string loopLabel = proc.NewLabelName();

            proc.LoopStart(loopLabel);
            {
                proc.MarkLoopContinue(loopLabel);
                DMExpression.Emit(dmObject, proc, statementWhile.Conditional);
                proc.BreakIfFalse();

                proc.StartScope();
                {
                    ProcessBlockInner(statementWhile.Body);
                    proc.LoopJumpToStart(loopLabel);
                }
                proc.EndScope();
            }
            proc.LoopEnd();
        }

        public void ProcessStatementDoWhile(DMASTProcStatementDoWhile statementDoWhile) {
            string loopLabel = proc.NewLabelName();
            string loopEndLabel = proc.NewLabelName();

            proc.LoopStart(loopLabel);
            {
                ProcessBlockInner(statementDoWhile.Body);

                proc.MarkLoopContinue(loopLabel);
                DMExpression.Emit(dmObject, proc, statementDoWhile.Conditional);
                proc.JumpIfFalse(loopEndLabel);
                proc.LoopJumpToStart(loopLabel);

                proc.AddLabel(loopEndLabel);
                proc.Break();
            }
            proc.LoopEnd();
        }

        public void ProcessStatementSwitch(DMASTProcStatementSwitch statementSwitch) {
            string endLabel = proc.NewLabelName();
            List<(string CaseLabel, DMASTProcBlockInner CaseBody)> valueCases = new();
            DMASTProcBlockInner? defaultCaseBody = null;

            DMExpression.Emit(dmObject, proc, statementSwitch.Value);
            foreach (DMASTProcStatementSwitch.SwitchCase switchCase in statementSwitch.Cases) {
                if (switchCase is DMASTProcStatementSwitch.SwitchCaseValues switchCaseValues) {
                    string caseLabel = proc.NewLabelName();

                    foreach (DMASTExpression value in switchCaseValues.Values) {
                        Constant GetCaseValue(DMASTExpression expression) {
                            if (!DMExpression.TryConstant(dmObject, proc, expression, out var constant))
                                DMCompiler.Emit(WarningCode.HardConstContext, expression.Location, "Expected a constant");

                            // Return 0 if unsuccessful so that we can continue compiling
                            return constant ?? new Number(expression.Location, 0.0f);
                        }

                        if (value is DMASTSwitchCaseRange range) { // if(1 to 5) or something
                            Constant lower = GetCaseValue(range.RangeStart);
                            Constant upper = GetCaseValue(range.RangeEnd);

                            Constant CoerceBound(Constant bound) {
                                if (bound is Null) { // We do a little null coercion, as a treat
                                    DMCompiler.Emit(WarningCode.MalformedRange, range.RangeStart.Location,
                                        "Malformed range, lower bound is coerced from null to 0");
                                    return new Number(lower.Location, 0.0f);
                                }

                                //DM 514.1580 does NOT care if the constants within a range are strings, and does a strange conversion to 0 or something, without warning or notice.
                                //We are (hopefully) deviating from parity here and just calling that a Compiler error.
                                if (bound is not Number) {
                                    DMCompiler.Emit(WarningCode.InvalidRange, range.RangeStart.Location,
                                        "Invalid range, lower bound is not a number");
                                    bound = new Number(bound.Location, 0.0f);
                                }

                                return bound;
                            }

                            lower = CoerceBound(lower);
                            upper = CoerceBound(upper);

                            lower.EmitPushValue(dmObject, proc);
                            upper.EmitPushValue(dmObject, proc);
                            proc.SwitchCaseRange(caseLabel);
                        } else {
                            Constant constant = GetCaseValue(value);

                            constant.EmitPushValue(dmObject, proc);
                            proc.SwitchCase(caseLabel);
                        }
                    }

                    valueCases.Add((caseLabel, switchCase.Body));
                } else {
                    defaultCaseBody = ((DMASTProcStatementSwitch.SwitchCaseDefault)switchCase).Body;
                }
            }
            proc.Pop();

            if (defaultCaseBody != null) {
                proc.StartScope();
                {
                    ProcessBlockInner(defaultCaseBody);
                }
                proc.EndScope();
            }
            proc.Jump(endLabel);

            foreach ((string CaseLabel, DMASTProcBlockInner CaseBody) valueCase in valueCases) {
                proc.AddLabel(valueCase.CaseLabel);
                proc.StartScope();
                {
                    ProcessBlockInner(valueCase.CaseBody);
                }
                proc.EndScope();
                proc.Jump(endLabel);
            }

            proc.AddLabel(endLabel);
        }

        public void ProcessStatementBrowse(DMASTProcStatementBrowse statementBrowse) {
            DMExpression.Emit(dmObject, proc, statementBrowse.Receiver);
            DMExpression.Emit(dmObject, proc, statementBrowse.Body);
            DMExpression.Emit(dmObject, proc, statementBrowse.Options);
            proc.Browse();
        }

        public void ProcessStatementBrowseResource(DMASTProcStatementBrowseResource statementBrowseResource) {
            DMExpression.Emit(dmObject, proc, statementBrowseResource.Receiver);
            DMExpression.Emit(dmObject, proc, statementBrowseResource.File);
            DMExpression.Emit(dmObject, proc, statementBrowseResource.Filename);
            proc.BrowseResource();
        }

        public void ProcessStatementOutputControl(DMASTProcStatementOutputControl statementOutputControl) {
            DMExpression.Emit(dmObject, proc, statementOutputControl.Receiver);
            DMExpression.Emit(dmObject, proc, statementOutputControl.Message);
            DMExpression.Emit(dmObject, proc, statementOutputControl.Control);
            proc.OutputControl();
        }

        public void ProcessStatementFtp(DMASTProcStatementFtp statementFtp) {
            DMExpression.Emit(dmObject, proc, statementFtp.Receiver);
            DMExpression.Emit(dmObject, proc, statementFtp.File);
            DMExpression.Emit(dmObject, proc, statementFtp.Name);
            proc.Ftp();
        }

        public void ProcessStatementOutput(DMASTProcStatementOutput statementOutput) {
            DMExpression left = DMExpression.Create(dmObject, proc, statementOutput.A);
            DMExpression right = DMExpression.Create(dmObject, proc, statementOutput.B);

            if (left is LValue) {
                // An LValue on the left needs a special opcode so that its reference can be used
                // This allows for special operations like "savefile[...] << ..."

                string endLabel = proc.NewLabelName();
                DMReference leftRef = left.EmitReference(dmObject, proc, endLabel, DMExpression.ShortCircuitMode.PopNull);
                right.EmitPushValue(dmObject, proc);
                proc.OutputReference(leftRef);
                proc.AddLabel(endLabel);
            } else {
                left.EmitPushValue(dmObject, proc);
                right.EmitPushValue(dmObject, proc);
                proc.Output();
            }
        }

        public void ProcessStatementInput(DMASTProcStatementInput statementInput) {
            DMExpression left = DMExpression.Create(dmObject, proc, statementInput.A);
            DMExpression right = DMExpression.Create(dmObject, proc, statementInput.B);

            // The left-side value of an input operation must be an LValue
            // (I think? I haven't found an exception but there could be one)
            if (left is not LValue) {
                DMCompiler.Emit(WarningCode.BadExpression, left.Location, "Left side must be an l-value");
                return;
            }

            // The right side must also be an LValue. Because where else would the value go?
            if (right is not LValue) {
                DMCompiler.Emit(WarningCode.BadExpression, right.Location, "Right side must be an l-value");
                return;
            }

            string rightEndLabel = proc.NewLabelName();
            string leftEndLabel = proc.NewLabelName();
            DMReference rightRef = right.EmitReference(dmObject, proc, rightEndLabel, DMExpression.ShortCircuitMode.PopNull);
            DMReference leftRef = left.EmitReference(dmObject, proc, leftEndLabel, DMExpression.ShortCircuitMode.PopNull);

            proc.Input(leftRef, rightRef);

            proc.AddLabel(leftEndLabel);
            proc.AddLabel(rightEndLabel);
        }

        public void ProcessStatementTryCatch(DMASTProcStatementTryCatch tryCatch) {
            string catchLabel = proc.NewLabelName();
            string endLabel = proc.NewLabelName();

            if (tryCatch.CatchParameter != null) {
                var param = tryCatch.CatchParameter as DMASTProcStatementVarDeclaration;

                if (!proc.TryAddLocalVariable(param.Name, param.Type, param.ValType)) {
                    DMCompiler.Emit(WarningCode.DuplicateVariable, param.Location, $"Duplicate var {param.Name}");
                }

                proc.StartTry(catchLabel, proc.GetLocalVariableReference(param.Name));
            } else {
                proc.StartTryNoValue(catchLabel);
            }

            proc.StartScope();
            ProcessBlockInner(tryCatch.TryBody);
            proc.EndScope();
            proc.EndTry();
            proc.Jump(endLabel);

            proc.AddLabel(catchLabel);
            if (tryCatch.CatchBody != null) {
                proc.StartScope();
                ProcessBlockInner(tryCatch.CatchBody);
                proc.EndScope();
            }
            proc.AddLabel(endLabel);

        }

        public void ProcessStatementThrow(DMASTProcStatementThrow statement) {
            DMExpression.Emit(dmObject, proc, statement.Value);
            proc.Throw();
        }
    }
}
