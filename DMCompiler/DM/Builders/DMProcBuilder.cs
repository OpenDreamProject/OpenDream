using System.Diagnostics;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;
using DMCompiler.Compiler.DM;
using DMCompiler.Compiler.DM.AST;
using DMCompiler.DM.Expressions;

namespace DMCompiler.DM.Builders {
    internal sealed class DMProcBuilder(DMCompiler compiler, DMObject dmObject, DMProc proc) {
        private readonly DMExpressionBuilder _exprBuilder = new(new(compiler, dmObject, proc));

        /// <summary>
        /// BYOND currently has a ridiculous behaviour, where, <br/>
        /// sometimes when a set statement has a right-hand side that is non-constant, <br/>
        /// no error is emitted and instead its value is just, whatever the last well-evaluated set statement's value was. <br/>
        /// This behaviour is nonsense but for harsh parity we sometimes may need to carry it out to hold up a codebase; <br/>
        /// Yogstation (at time of writing) actually errors on OD if we don't implement this.
        /// </summary>
        // Starts null; marks that we've never seen one before and should just error like normal people.
        private Constant? _previousSetStatementValue;

        private ExpressionContext ExprContext => new(compiler, dmObject, proc);

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
                    _exprBuilder.Emit(parameter.Value, parameter.ObjectType);
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
                    compiler.Emit(WarningCode.EmptyBlock,block.Location,"Empty block detected - set statements are executed outside of, before, and unconditional to, this block");
                } else {
                    compiler.Emit(WarningCode.EmptyBlock,block.Location,"Empty block detected");
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
                case DMASTProcStatementLink statementLink: ProcessStatementLink(statementLink); break;
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
                    compiler.ForcedError(statement.Location, $"Invalid proc statement {statement.GetType()}");
                    break;
            }
        }

        public void ProcessStatementExpression(DMASTProcStatementExpression statement) {
            _exprBuilder.Emit(statement.Expression);
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
                            compiler.UnimplementedWarning(statementSet.Location,
                                "'set src = usr.loc' is unimplemented");
                        } else if (deref == "group") {
                            proc.VerbSrc = VerbSrc.UsrGroup;
                            compiler.UnimplementedWarning(statementSet.Location,
                                "'set src = usr.group' is unimplemented");
                        } else {
                            goto default;
                        }

                        break;
                    case DMASTIdentifier {Identifier: "world"}:
                        proc.VerbSrc = statementSet.WasInKeyword ? VerbSrc.InWorld : VerbSrc.World;
                        if (statementSet.WasInKeyword)
                            compiler.UnimplementedWarning(statementSet.Location,
                                "'set src = world.contents' is unimplemented");
                        else
                            compiler.UnimplementedWarning(statementSet.Location,
                                "'set src = world' is unimplemented");
                        break;
                    case DMASTDereference {Expression: DMASTIdentifier{Identifier: "world"}, Operations: var operations}:
                        if (operations is not [DMASTDereference.FieldOperation {Identifier: "contents"}])
                            goto default;

                        proc.VerbSrc = VerbSrc.InWorld;
                        compiler.UnimplementedWarning(statementSet.Location,
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
                        compiler.Emit(WarningCode.BadExpression, statementSet.Value.Location, "Invalid verb src");
                        break;
                }

                return;
            }

            if (!_exprBuilder.TryConstant(statementSet.Value, out var constant)) { // If this set statement's rhs is not constant
                bool didError = compiler.Emit(WarningCode.InvalidSetStatement, statementSet.Location, $"'{attribute}' attribute should be a constant");
                if (didError) // if this is an error
                    return; // don't do the cursed thing

                constant = _previousSetStatementValue;
            } else {
                _previousSetStatementValue = constant;
            }

            // oh no.
            if (constant is null) {
                compiler.Emit(WarningCode.BadExpression, statementSet.Location, $"'{attribute}' attribute must be a constant");
                return;
            }

            // Check if it was 'set x in y' or whatever
            // (which is illegal for everything except setting src to something)
            if (statementSet.WasInKeyword) {
                compiler.Emit(WarningCode.BadToken, statementSet.Location, "Use of 'in' keyword is illegal here. Did you mean '='?");
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

                    compiler.UnimplementedWarning(statementSet.Location, "set instant is not implemented");
                    break;
                case "background":
                    if (constant.IsTruthy())
                        proc.Attributes |= ProcAttributes.Background;
                    else
                        proc.Attributes &= ~ProcAttributes.Background;
                    break;
                case "name":
                    if (constant is not Expressions.String nameStr) {
                        compiler.Emit(WarningCode.BadExpression, constant.Location, "name attribute must be a string");
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
                        compiler.Emit(WarningCode.BadExpression, constant.Location,
                            "category attribute must be a string or null");
                    }

                    break;
                case "desc":
                    // TODO: verb.desc is supposed to be printed when you type the verb name and press F1. Check the ref for details.
                    if (constant is not Expressions.String descStr) {
                        compiler.Emit(WarningCode.BadExpression, constant.Location, "desc attribute must be a string");
                        break;
                    }

                    proc.VerbDesc = descStr.Value;
                    break;
                case "invisibility":
                    // The ref says 0-101 for atoms and 0-100 for verbs
                    // BYOND doesn't clamp the actual var value but it does seem to treat out-of-range values as their extreme
                    if (constant is not Number invisNum) {
                        compiler.Emit(WarningCode.BadExpression, constant.Location, "invisibility attribute must be an int");
                        break;
                    }

                    proc.Invisibility = Convert.ToSByte(Math.Clamp(MathF.Floor(invisNum.Value), 0f, 100f));
                    break;
                case "src":
                    compiler.UnimplementedWarning(statementSet.Location, "set src is not implemented");
                    break;
            }
        }

        public void ProcessStatementDel(DMASTProcStatementDel statementDel) {
            _exprBuilder.Emit(statementDel.Value);
            proc.DeleteObject();
        }

        public void ProcessStatementSpawn(DMASTProcStatementSpawn statementSpawn) {
            _exprBuilder.Emit(statementSpawn.Delay);

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

        /// <remarks>
        /// Global/static var declarations are handled by <see cref="DMCodeTree.ProcGlobalVarNode" />
        /// </remarks>
        public void ProcessStatementVarDeclaration(DMASTProcStatementVarDeclaration varDeclaration) {
            if (varDeclaration.IsGlobal) { return; }

            DMExpression value;
            if (varDeclaration.Value != null) {
                value = _exprBuilder.Create(varDeclaration.Value, varDeclaration.Type);

                if (!varDeclaration.ValType.MatchesType(compiler, value.ValType)) {
                    compiler.Emit(WarningCode.InvalidVarType, varDeclaration.Location,
                        $"{varDeclaration.Name}: Invalid var value {value.ValType}, expected {varDeclaration.ValType}");
                }
            } else {
                value = new Null(varDeclaration.Location);
            }

            bool successful;
            if (varDeclaration.IsConst) {
                if (!value.TryAsConstant(compiler, out var constValue)) {
                    compiler.Emit(WarningCode.HardConstContext, varDeclaration.Location, "Const var must be set to a constant");
                    return;
                }

                successful = proc.TryAddLocalConstVariable(varDeclaration.Name, varDeclaration.Type, constValue);
            } else {
                successful = proc.TryAddLocalVariable(varDeclaration.Name, varDeclaration.Type, varDeclaration.ValType);
            }

            if (!successful) {
                compiler.Emit(WarningCode.DuplicateVariable, varDeclaration.Location, $"Duplicate var {varDeclaration.Name}");
                return;
            }

            value.EmitPushValue(ExprContext);
            proc.Assign(proc.GetLocalVariableReference(varDeclaration.Name));
            proc.Pop();
        }

        public void ProcessStatementReturn(DMASTProcStatementReturn statement) {
            if (statement.Value != null) {
                var expr = _exprBuilder.Create(statement.Value);

                // Don't type-check unimplemented procs
                if (proc.TypeChecked && (proc.Attributes & ProcAttributes.Unimplemented) == 0) {
                    if (expr.TryAsConstant(compiler, out var exprConst)) {
                        proc.ValidateReturnType(exprConst);
                    } else {
                        proc.ValidateReturnType(expr);
                    }
                }

                expr.EmitPushValue(ExprContext);
            } else {
                proc.PushReferenceValue(DMReference.Self); //Default return value
            }

            proc.Return();
        }

        public void ProcessStatementIf(DMASTProcStatementIf statement) {
            _exprBuilder.Emit(statement.Condition);

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

                if (statementFor.Expression2 is DMASTExpressionIn dmastIn && statementFor.Expression3 is null) {
                    var expr2 = statementFor.Expression2 != null ? _exprBuilder.CreateIgnoreUnknownReference(statementFor.Expression2) : null;

                    // TODO: Wow this sucks
                    if (expr2 is UnknownReference unknownRef) {
                        if(statementFor.Expression1 is not DMASTVarDeclExpression || dmastIn.LHS is not DMASTIdentifier ident)
                            unknownRef.EmitCompilerError(compiler);
                        else {
                            ProcessStatementVarDeclaration(new DMASTProcStatementVarDeclaration(statementFor.Location, new DMASTPath(statementFor.Location, new DreamPath(ident.Identifier)), null, DMValueType.Anything));
                            var meep = dmastIn.LHS;
                            expr2 = _exprBuilder.Create(meep);
                        }
                    } else {
                        expr2 = _exprBuilder.Create(dmastIn.LHS);
                    }

                    DMASTExpression outputExpr;
                    if (statementFor.Expression1 is DMASTVarDeclExpression decl) {
                        outputExpr = new DMASTIdentifier(decl.Location, decl.DeclPath.Path.LastElement!);
                    } else {
                        outputExpr = statementFor.Expression1;
                    }

                    var outputVar = _exprBuilder.Create(outputExpr);
                    var list = _exprBuilder.Create(dmastIn.RHS);

                    switch (outputVar) {
                        case Local outputLocal: {
                            outputLocal.LocalVar.ExplicitValueType = statementFor.DMTypes;
                            if(outputLocal.LocalVar is DMProc.LocalConstVariable)
                                compiler.Emit(WarningCode.WriteToConstant, outputExpr.Location, "Cannot change constant value");
                            break;
                        }
                        case Field { IsConst: true }: {
                            compiler.Emit(WarningCode.WriteToConstant, outputExpr.Location, "Cannot change constant value");
                            break;
                        }
                    }

                    switch (expr2) {
                        case Local assocLocal: {
                            assocLocal.LocalVar.ExplicitValueType = statementFor.DMTypes;
                            if(assocLocal.LocalVar is DMProc.LocalConstVariable)
                                compiler.Emit(WarningCode.WriteToConstant, outputExpr.Location, "Cannot change constant value");
                            break;
                        }
                        case Field { IsConst: true }:
                        {
                            compiler.Emit(WarningCode.WriteToConstant, outputExpr.Location, "Cannot change constant value");
                            break;
                        }
                    }

                    ProcessStatementForList(list, outputVar, expr2, statementFor.DMTypes, statementFor.Body);
                }
                else if (statementFor.Expression2 != null || statementFor.Expression3 != null) {
                    var initializer = statementFor.Expression1 != null ? _exprBuilder.Create(statementFor.Expression1) : null;
                    var comparator = statementFor.Expression2 != null ? _exprBuilder.Create(statementFor.Expression2) : null;
                    var incrementor = statementFor.Expression3 != null ? _exprBuilder.Create(statementFor.Expression3) : null;

                    ProcessStatementForStandard(initializer, comparator, incrementor, statementFor.Body);
                } else {
                    switch (statementFor.Expression1) {
                        case DMASTAssign {LHS: DMASTVarDeclExpression decl, RHS: DMASTExpressionInRange range}: {
                            var initializer = statementFor.Expression1 != null ? _exprBuilder.Create(statementFor.Expression1) : null;
                            var identifier = new DMASTIdentifier(decl.Location, decl.DeclPath.Path.LastElement);
                            var outputVar = _exprBuilder.Create(identifier);

                            var start = _exprBuilder.Create(range.StartRange);
                            var end = _exprBuilder.Create(range.EndRange);
                            var step = range.Step != null
                                ? _exprBuilder.Create(range.StartRange)
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

                            var outputVar = _exprBuilder.Create(outputExpr);

                            if (outputVar is Local { LocalVar: DMProc.LocalConstVariable } or Field { IsConst: true }) {
                                compiler.Emit(WarningCode.WriteToConstant, outputExpr.Location, "Cannot change constant value");
                            }

                            var start = _exprBuilder.Create(exprRange.StartRange);
                            var end = _exprBuilder.Create(exprRange.EndRange);
                            var step = exprRange.Step != null
                                ? _exprBuilder.Create(exprRange.Step)
                                : new Number(exprRange.Location, 1);

                            ProcessStatementForRange(null, outputVar, start, end, step, statementFor.Body);
                            break;
                        }
                        case DMASTVarDeclExpression vd: {
                            var initializer = statementFor.Expression1 != null ? _exprBuilder.Create(statementFor.Expression1) : null;
                            var declInfo = new ProcVarDeclInfo(vd.DeclPath.Path);
                            var identifier = new DMASTIdentifier(vd.Location, declInfo.VarName);
                            var outputVar = _exprBuilder.Create(identifier);

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

                            var outputVar = _exprBuilder.Create(outputExpr);
                            var list = _exprBuilder.Create(exprIn.RHS);

                            if (outputVar is Local outputLocal) {
                                outputLocal.LocalVar.ExplicitValueType = statementFor.DMTypes;
                            if(outputLocal.LocalVar is DMProc.LocalConstVariable)
                                compiler.Emit(WarningCode.WriteToConstant, outputExpr.Location, "Cannot change constant value");
                            } else if (outputVar is Field { IsConst: true })
                                compiler.Emit(WarningCode.WriteToConstant, outputExpr.Location, "Cannot change constant value");

                            ProcessStatementForList(list, outputVar, null, statementFor.DMTypes, statementFor.Body);
                            break;
                        }
                        default:
                            compiler.Emit(WarningCode.BadExpression, statementFor.Location, "Invalid expression in for");
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
                    initializer.EmitPushValue(ExprContext);
                    proc.Pop();
                }

                string loopLabel = proc.NewLabelName();
                proc.LoopStart(loopLabel);
                {
                    if (comparator != null) {
                        comparator.EmitPushValue(ExprContext);
                        proc.BreakIfFalse();
                    }

                    ProcessBlockInner(body);

                    proc.MarkLoopContinue(loopLabel);
                    if (incrementor != null) {
                        incrementor.EmitPushValue(ExprContext);
                        proc.Pop();
                    }

                    proc.LoopJumpToStart(loopLabel);
                }
                proc.LoopEnd();
            }
            proc.EndScope();
        }

        public void ProcessLoopAssignment(LValue lValue, LValue? assocValue = null, DMExpression? list = null) {
            if (lValue.CanReferenceShortCircuit()) {
                string endLabel = proc.NewLabelName();
                string endLabel2 = proc.NewLabelName();

                DMReference outputRef = lValue.EmitReference(ExprContext, endLabel, DMExpression.ShortCircuitMode.PopNull);
                proc.Enumerate(outputRef);
                proc.Jump(endLabel2);

                proc.AddLabel(endLabel);
                proc.EnumerateNoAssign();
                proc.AddLabel(endLabel2);
            } else {
                DMReference outputRef = lValue.EmitReference(ExprContext, null);
                if (assocValue != null && list != null) {
                    DMReference assocRef = assocValue.EmitReference(ExprContext, null);
                    DMReference listRef = list.EmitReference(ExprContext, null);
                    proc.EnumerateAssoc(outputRef, assocRef, listRef);
                } else {
                    proc.Enumerate(outputRef);
                }
            }
        }

        public void ProcessStatementForList(DMExpression list, DMExpression outputVar, DMExpression? outputAssocVar, DMComplexValueType? typeCheck, DMASTProcBlockInner body) {
            if (outputVar is not LValue lValue) {
                compiler.Emit(WarningCode.BadExpression, outputVar.Location, "Invalid output var");
                lValue = new BadLValue(outputVar.Location);
            }

            if (outputAssocVar is not LValue && outputAssocVar is not null) {
                compiler.Emit(WarningCode.BadExpression, outputAssocVar.Location, "Invalid output var");
                lValue = new BadLValue(outputAssocVar.Location);
            }

            LValue? outputValue = (LValue)outputAssocVar!;

            // Having no "as [types]" will use the var's type for the type filter
            if (typeCheck == null && lValue.Path != null) {
                typeCheck = lValue.Path;
            }

            bool performingImplicitIsType = false;
            list.EmitPushValue(ExprContext);
            if (typeCheck?.TypePath is { } typeCheckPath) { // We have a specific type to filter for
                if (compiler.DMObjectTree.TryGetTypeId(typeCheckPath, out var filterTypeId)) {
                    // Create an enumerator that will do the implicit istype() for us
                    proc.CreateFilteredListEnumerator(filterTypeId, typeCheckPath);
                } else {
                    compiler.Emit(WarningCode.ItemDoesntExist, outputVar.Location,
                        $"Cannot filter enumeration by type {typeCheckPath}, it does not exist");
                    proc.CreateListEnumerator();
                }
            } else { // Either no type filter or we're using the slower "as [types]"
                performingImplicitIsType = !(typeCheck is null || typeCheck.Value.IsAnything);
                proc.CreateListEnumerator();
            }

            proc.StartScope();
            {
                string loopLabel = proc.NewLabelName();
                proc.LoopStart(loopLabel);
                {
                    proc.MarkLoopContinue(loopLabel);

                    ProcessLoopAssignment(lValue, outputValue, list);

                    // "as mob|etc" will insert code equivalent to "if(!(istype(X, mob) || istype(X, etc))) continue;"
                    // It would be ideal if the type filtering could be done by the interpreter, like it does when the var has a type
                    // But the code currently isn't structured in a way that it could be done nicely
                    if (performingImplicitIsType) {
                        var afterTypeCheckIf = proc.NewLabelName();
                        var afterTypeCheckExpr = proc.NewLabelName();

                        void CheckType(DMValueType type, DreamPath path, ref bool doOr) {
                            if (!typeCheck!.Value.Type.HasFlag(type))
                                return;
                            if (!compiler.DMObjectTree.TryGetTypeId(path, out var typeId))
                                return;

                            if (doOr)
                                proc.BooleanOr(afterTypeCheckExpr);
                            doOr = true;

                            lValue.EmitPushValue(ExprContext);
                            proc.PushType(typeId);
                            proc.IsType();
                        }

                        bool doOr = false; // Only insert BooleanOr after the first type
                        CheckType(DMValueType.Area, DreamPath.Area, ref doOr);
                        CheckType(DMValueType.Turf, DreamPath.Turf, ref doOr);
                        CheckType(DMValueType.Obj, DreamPath.Obj, ref doOr);
                        CheckType(DMValueType.Mob, DreamPath.Mob, ref doOr);
                        proc.AddLabel(afterTypeCheckExpr);
                        if (doOr) {
                            proc.Not();
                            proc.JumpIfFalse(afterTypeCheckIf);
                            proc.Continue();
                        }

                        proc.AddLabel(afterTypeCheckIf);
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
                compiler.ForcedError(initializer.Location,
                    "Attempted to create a type enumerator with a null type");
                return;
            }

            if (compiler.DMObjectTree.TryGetTypeId(type.Value, out var typeId)) {
                proc.PushType(typeId);
                proc.CreateTypeEnumerator();
            } else {
                compiler.Emit(WarningCode.ItemDoesntExist, initializer.Location, $"Type {type.Value} does not exist");
            }

            proc.StartScope();
            {
                if (initializer != null) {
                    initializer.EmitPushValue(ExprContext);
                    proc.Pop();
                }

                string loopLabel = proc.NewLabelName();
                proc.LoopStart(loopLabel);
                {
                    proc.MarkLoopContinue(loopLabel);

                    if (outputVar is Expressions.LValue lValue) {
                        ProcessLoopAssignment(lValue);
                    } else {
                        compiler.Emit(WarningCode.BadExpression, outputVar.Location, "Invalid output var");
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
            start.EmitPushValue(ExprContext);
            end.EmitPushValue(ExprContext);
            if (step != null) {
                step.EmitPushValue(ExprContext);
            } else {
                proc.PushFloat(1.0f);
            }

            proc.CreateRangeEnumerator();
            proc.StartScope();
            {
                if (initializer != null) {
                    initializer.EmitPushValue(ExprContext);
                    proc.Pop();
                }

                string loopLabel = proc.NewLabelName();
                proc.LoopStart(loopLabel);
                {
                    proc.MarkLoopContinue(loopLabel);

                    if (outputVar is Expressions.LValue lValue) {
                        ProcessLoopAssignment(lValue);
                    } else {
                        compiler.Emit(WarningCode.BadExpression, outputVar.Location, "Invalid output var");
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
                _exprBuilder.Emit(statementWhile.Conditional);
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
                _exprBuilder.Emit(statementDoWhile.Conditional);
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

            _exprBuilder.Emit(statementSwitch.Value);
            foreach (DMASTProcStatementSwitch.SwitchCase switchCase in statementSwitch.Cases) {
                if (switchCase is DMASTProcStatementSwitch.SwitchCaseValues switchCaseValues) {
                    string caseLabel = proc.NewLabelName();

                    foreach (DMASTExpression value in switchCaseValues.Values) {
                        Constant GetCaseValue(DMASTExpression expression) {
                            if (!_exprBuilder.TryConstant(expression, out var constant))
                                compiler.Emit(WarningCode.HardConstContext, expression.Location, "Expected a constant");

                            // Return 0 if unsuccessful so that we can continue compiling
                            return constant ?? new Number(expression.Location, 0.0f);
                        }

                        if (value is DMASTSwitchCaseRange range) { // if(1 to 5) or something
                            Constant lower = GetCaseValue(range.RangeStart);
                            Constant upper = GetCaseValue(range.RangeEnd);

                            Constant CoerceBound(Constant bound) {
                                if (bound is Null) { // We do a little null coercion, as a treat
                                    compiler.Emit(WarningCode.MalformedRange, range.RangeStart.Location,
                                        "Malformed range, lower bound is coerced from null to 0");
                                    return new Number(lower.Location, 0.0f);
                                }

                                //DM 514.1580 does NOT care if the constants within a range are strings, and does a strange conversion to 0 or something, without warning or notice.
                                //We are (hopefully) deviating from parity here and just calling that a Compiler error.
                                if (bound is not Number) {
                                    compiler.Emit(WarningCode.InvalidRange, range.RangeStart.Location,
                                        "Invalid range, lower bound is not a number");
                                    bound = new Number(bound.Location, 0.0f);
                                }

                                return bound;
                            }

                            lower = CoerceBound(lower);
                            upper = CoerceBound(upper);

                            lower.EmitPushValue(ExprContext);
                            upper.EmitPushValue(ExprContext);
                            proc.SwitchCaseRange(caseLabel);
                        } else {
                            Constant constant = GetCaseValue(value);

                            constant.EmitPushValue(ExprContext);
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
            _exprBuilder.Emit(statementBrowse.Receiver);
            _exprBuilder.Emit(statementBrowse.Body);
            _exprBuilder.Emit(statementBrowse.Options);
            proc.Browse();
        }

        public void ProcessStatementBrowseResource(DMASTProcStatementBrowseResource statementBrowseResource) {
            _exprBuilder.Emit(statementBrowseResource.Receiver);
            _exprBuilder.Emit(statementBrowseResource.File);
            _exprBuilder.Emit(statementBrowseResource.Filename);
            proc.BrowseResource();
        }

        public void ProcessStatementOutputControl(DMASTProcStatementOutputControl statementOutputControl) {
            _exprBuilder.Emit(statementOutputControl.Receiver);
            _exprBuilder.Emit(statementOutputControl.Message);
            _exprBuilder.Emit(statementOutputControl.Control);
            proc.OutputControl();
        }

        public void ProcessStatementLink(DMASTProcStatementLink statementLink) {
            _exprBuilder.Emit(statementLink.Receiver);
            _exprBuilder.Emit(statementLink.Url);
            proc.Link();
        }

        public void ProcessStatementFtp(DMASTProcStatementFtp statementFtp) {
            _exprBuilder.Emit(statementFtp.Receiver);
            _exprBuilder.Emit(statementFtp.File);
            _exprBuilder.Emit(statementFtp.Name);
            proc.Ftp();
        }

        public void ProcessStatementOutput(DMASTProcStatementOutput statementOutput) {
            DMExpression left = _exprBuilder.Create(statementOutput.A);
            DMExpression right = _exprBuilder.Create(statementOutput.B);

            if (left is LValue) {
                // An LValue on the left needs a special opcode so that its reference can be used
                // This allows for special operations like "savefile[...] << ..."

                string endLabel = proc.NewLabelName();
                DMReference leftRef = left.EmitReference(ExprContext, endLabel, DMExpression.ShortCircuitMode.PopNull);
                right.EmitPushValue(ExprContext);
                proc.OutputReference(leftRef);
                proc.AddLabel(endLabel);
            } else {
                left.EmitPushValue(ExprContext);
                right.EmitPushValue(ExprContext);
                proc.Output();
            }
        }

        public void ProcessStatementInput(DMASTProcStatementInput statementInput) {
            DMExpression left = _exprBuilder.Create(statementInput.A);
            DMExpression right = _exprBuilder.Create(statementInput.B);

            // The left-side value of an input operation must be an LValue
            // (I think? I haven't found an exception but there could be one)
            if (left is not LValue) {
                compiler.Emit(WarningCode.BadExpression, left.Location, "Left side must be an l-value");
                return;
            }

            // The right side must also be an LValue. Because where else would the value go?
            if (right is not LValue) {
                compiler.Emit(WarningCode.BadExpression, right.Location, "Right side must be an l-value");
                return;
            }

            string rightEndLabel = proc.NewLabelName();
            string leftEndLabel = proc.NewLabelName();
            DMReference rightRef = right.EmitReference(ExprContext, rightEndLabel, DMExpression.ShortCircuitMode.PopNull);
            DMReference leftRef = left.EmitReference(ExprContext, leftEndLabel, DMExpression.ShortCircuitMode.PopNull);

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
                    compiler.Emit(WarningCode.DuplicateVariable, param.Location, $"Duplicate var {param.Name}");
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
            _exprBuilder.Emit(statement.Value);
            proc.Throw();
        }
    }
}
