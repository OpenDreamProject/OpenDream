using OpenDreamShared.Compiler;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;

namespace DMCompiler.DM.Visitors {
    class DMObjectBuilder {
        private DMObject _currentObject = null;

        public void BuildObjectTree(DMASTFile astFile) {
            DMObjectTree.Reset();
            ProcessFile(astFile);

            foreach (DMObject dmObject in DMObjectTree.AllObjects.Values) {
                dmObject.CompileProcs();
            }

            DMObjectTree.CreateGlobalInitProc();
        }

        public void ProcessFile(DMASTFile file) {
            _currentObject = DMObjectTree.GetDMObject(DreamPath.Root);

            ProcessBlockInner(file.BlockInner);
        }

        public void ProcessBlockInner(DMASTBlockInner blockInner) {
            foreach (DMASTStatement statement in blockInner.Statements) {
                try {
                    ProcessStatement(statement);
                } catch (CompileErrorException e) {
                    Program.Error(e.Error);
                }
            }
        }

        public void ProcessStatement(DMASTStatement statement) {
            switch (statement) {
                case DMASTObjectDefinition objectDefinition: ProcessObjectDefinition(objectDefinition); break;
                case DMASTObjectVarDefinition varDefinition: ProcessVarDefinition(varDefinition); break;
                case DMASTObjectVarOverride varOverride: ProcessVarOverride(varOverride); break;
                case DMASTProcDefinition procDefinition: ProcessProcDefinition(procDefinition); break;
                case DMASTMultipleObjectVarDefinitions multipleVarDefinitions: {
                    foreach (DMASTObjectVarDefinition varDefinition in multipleVarDefinitions.VarDefinitions) {
                        ProcessVarDefinition(varDefinition);
                    }

                    break;
                }
                default: throw new ArgumentException("Invalid object statement");
            }
        }

        public void ProcessObjectDefinition(DMASTObjectDefinition objectDefinition) {
            DMObject oldObject = _currentObject;

            _currentObject = DMObjectTree.GetDMObject(objectDefinition.Path);
            if (objectDefinition.InnerBlock != null) ProcessBlockInner(objectDefinition.InnerBlock);
            _currentObject = oldObject;
        }

        public void ProcessVarDefinition(DMASTObjectVarDefinition varDefinition) {
            DMObject oldObject = _currentObject;
            DMVariable variable = new DMVariable(varDefinition.Type, varDefinition.Name, varDefinition.IsToplevel || varDefinition.IsGlobal, varDefinition.IsConst);

            _currentObject = DMObjectTree.GetDMObject(varDefinition.ObjectPath);

            if (variable.IsGlobal && !varDefinition.IsToplevel) {
                variable.InternalName = "$$$$" + _currentObject.Path + "." + varDefinition.Name;
                DMObjectTree.GetDMObject(DreamPath.Root).GlobalVariables[variable.InternalName] = variable;
                _currentObject.GlobalVariables[variable.Name] = variable;
            }
            else if (variable.IsGlobal) {
                _currentObject.GlobalVariables[variable.Name] = variable;
            }
            else {
                _currentObject.Variables[variable.Name] = variable;
            }

            try {
                SetVariableValue(variable, varDefinition.Value, varDefinition.Type);
            } catch (CompileErrorException e) {
                Program.Error(e.Error);
            }

            _currentObject = oldObject;
        }

        public void ProcessVarOverride(DMASTObjectVarOverride varOverride) {
            DMObject oldObject = _currentObject;

            _currentObject = DMObjectTree.GetDMObject(varOverride.ObjectPath);

            try {
                if (varOverride.VarName == "parent_type") {
                    DMASTConstantPath parentType = varOverride.Value as DMASTConstantPath;

                    if (parentType == null) throw new CompileErrorException("Expected a constant path");
                    _currentObject.Parent = DMObjectTree.GetDMObject(parentType.Value.Path);
                } else {
                    DMVariable variable = new DMVariable(null, varOverride.VarName, false);

                    SetVariableValue(variable, varOverride.Value, null);
                    _currentObject.VariableOverrides[variable.Name] = variable;
                }
            } catch (CompileErrorException e) {
                Program.Error(e.Error);
            }
            
            _currentObject = oldObject;
        }

        public void ProcessProcDefinition(DMASTProcDefinition procDefinition) {
            string procName = procDefinition.Name;
            DMObject dmObject = _currentObject;

            try {
                if (procDefinition.ObjectPath.HasValue) {
                    dmObject = DMObjectTree.GetDMObject(_currentObject.Path.Combine(procDefinition.ObjectPath.Value));
                }

                if (!procDefinition.IsOverride && dmObject.HasProc(procName)) {
                    throw new CompileErrorException("Type " + dmObject.Path + " already has a proc named \"" + procName + "\"");
                }

                DMProc proc = new DMProc(procDefinition);

                dmObject.AddProc(procName, proc);

                if (procDefinition.Body != null) {
                    foreach (var stmt in GetStatements(procDefinition.Body)) {
                        // TODO multiple var definitions.
                        if (stmt is DMASTProcStatementVarDeclaration varDeclaration && varDeclaration.IsGlobal) {
                            DMVariable variable = new DMVariable(varDeclaration.Type, varDeclaration.Name, true, varDeclaration.IsConst);
                            variable.InternalName = "PROC$$$$" + _currentObject.Path + "$" + procName + "$" + varDeclaration.Name;
                            variable.Value = new Expressions.Null();
                            variable.Type = varDeclaration.Type;

                            DMObjectTree.GetDMObject(DreamPath.Root).GlobalVariables[variable.InternalName] = variable;
                            Expressions.Field field = new Expressions.Field(variable.Type, variable.InternalName);
                            if (varDeclaration.Value != null) {
                                DMExpression expression = DMExpression.Create(_currentObject, DMObjectTree.GlobalInitProc, varDeclaration.Value, varDeclaration.Type);
                                Expressions.Assignment assign = new Expressions.Assignment(field, expression);
                                DMObjectTree.AddGlobalInitProcAssign(assign);
                            }
                            proc.AddGlobalVariable(varDeclaration.Name, variable);
                        }
                    }
                }

                if (procDefinition.IsVerb) {
                    Expressions.Field field = new Expressions.Field(DreamPath.List, "verbs");
                    DreamPath procPath = new DreamPath(".proc/" + procName);
                    Expressions.Append append = new Expressions.Append(field, new Expressions.Path(procPath));

                    dmObject.InitializationProcExpressions.Add(append);
                }
            } catch (CompileErrorException e) {
                Program.Error(e.Error);
            }
        }

        // TODO Move this to an appropriate location
        static public IEnumerable<DMASTProcStatement> GetStatements(DMASTProcBlockInner block) {
            foreach (var stmt in block.Statements) {
                yield return stmt;
                List<DMASTProcBlockInner> recurse;
                switch (stmt) {
                    case DMASTProcStatementSpawn ps: recurse = new() { ps.Body }; break;
                    case DMASTProcStatementIf ps: recurse = new() { ps.Body, ps.ElseBody }; break;
                    case DMASTProcStatementFor ps: recurse = new() { ps.Body }; break;
                    case DMASTProcStatementForLoop ps: recurse = new() { ps.Body }; break;
                    case DMASTProcStatementWhile ps: recurse = new() { ps.Body }; break;
                    case DMASTProcStatementDoWhile ps: recurse = new() { ps.Body }; break;
                    // TODO Good luck if you declare a static var inside a switch
                    case DMASTProcStatementSwitch ps: {
                            recurse = new();
                            foreach (var swcase in ps.Cases) {
                                recurse.Add(swcase.Body);
                            }
                            break;
                        }
                    case DMASTProcStatementTryCatch ps: recurse = new() { ps.TryBody, ps.CatchBody }; break;
                    default: recurse = new(); break;
                }
                foreach (var subblock in recurse) {
                    if (subblock == null) { continue; }
                    foreach (var substmt in GetStatements(subblock)) {
                        yield return substmt;
                    }
                }
            }
        }

        public HashSet<string> const_procs = new() { "rgb", "matrix" };
        private bool ConstProc(string s) {
            return const_procs.Contains(s);
        }

        private void SetVariableValue(DMVariable variable, DMASTExpression value, DreamPath? type) {
            DMExpression expression = DMExpression.Create(_currentObject, variable.IsGlobal ? DMObjectTree.GlobalInitProc : null, value, type);

            if (variable.IsConst) {
                variable.Value = expression.ToConstant();
                return;
            }
            if (variable.IsGlobal && variable.Name != "world") {
                variable.Value = new Expressions.Null();
                EmitInitializationAssign(variable, expression);
                return;
            }
            if (value is DMASTProcCall ast_proc && ast_proc.Callable is DMASTCallableProcIdentifier ast_callable && ConstProc(ast_callable.Identifier)) {
                EmitInitializationAssign(variable, expression);
                variable.Value = new Expressions.Null();
                return;
            }
            if (value is DMASTNewList) {
                variable.Value = new Expressions.Null();
                EmitInitializationAssign(variable, expression);
                return;
            }

            switch (expression) {
                case Expressions.List:
                case Expressions.NewPath:
                    variable.Value = new Expressions.Null();
                    EmitInitializationAssign(variable, expression);
                    break;
                case Expressions.StringFormat:
                case Expressions.ProcCall:
                    if (!variable.IsGlobal) throw new CompileErrorException($"Invalid initial value for \"{variable.Name}\"");

                    variable.Value = new Expressions.Null();
                    EmitInitializationAssign(variable, expression);
                    break;
                default:
                    variable.Value = expression.ToConstant();
                    break;
            }
        }

        private void EmitInitializationAssign(DMVariable variable, DMExpression expression) {
            Expressions.Field field = new Expressions.Field(variable.Type, variable.InternalName);
            Expressions.Assignment assign = new Expressions.Assignment(field, expression);

            if (variable.IsGlobal) {
                DMObjectTree.AddGlobalInitProcAssign(assign);
            } else {
                _currentObject.InitializationProcExpressions.Add(assign);
            }
        }
    }
}
