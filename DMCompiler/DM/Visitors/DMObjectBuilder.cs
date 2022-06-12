using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using System;
using OpenDreamShared.Dream.Procs;
using System.Collections.Generic;

namespace DMCompiler.DM.Visitors {
    class DMObjectBuilder {
        private DMObject _currentObject = null;

        public void BuildObjectTree(DMASTFile astFile) {
            DMObjectTree.Reset();
            ProcessFile(astFile);

            // TODO Nuke this pass
            foreach (DMObject dmObject in DMObjectTree.AllObjects) {
                dmObject.CreateInitializationProc();
            }

            foreach (DMProc proc in DMObjectTree.AllProcs)
                proc.Compile();

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
                    DMCompiler.Error(e.Error);
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

            DMCompiler.VerbosePrint($"Generating {objectDefinition.Path}");
            _currentObject = DMObjectTree.GetDMObject(objectDefinition.Path);
            if (objectDefinition.InnerBlock != null) ProcessBlockInner(objectDefinition.InnerBlock);
            _currentObject = oldObject;
        }

        public void ProcessVarDefinition(DMASTObjectVarDefinition varDefinition) {
            DMObject oldObject = _currentObject;
            DMVariable variable;

            _currentObject = DMObjectTree.GetDMObject(varDefinition.ObjectPath);

            if (varDefinition.IsGlobal) {
                variable = _currentObject.CreateGlobalVariable(varDefinition.Type, varDefinition.Name, varDefinition.IsConst);
            } else {
                variable = new DMVariable(varDefinition.Type, varDefinition.Name, false, varDefinition.IsConst);
                _currentObject.Variables[variable.Name] = variable;
            }

            try {
                SetVariableValue(variable, varDefinition.Value, varDefinition.ValType);
            } catch (CompileErrorException e) {
                DMCompiler.Error(e.Error);
            }

            _currentObject = oldObject;
        }

        public void ProcessVarOverride(DMASTObjectVarOverride varOverride) {
            DMObject oldObject = _currentObject;

            _currentObject = DMObjectTree.GetDMObject(varOverride.ObjectPath);

            try
            {
                switch (varOverride.VarName)
                {
                    case "parent_type":
                    {
                        DMASTConstantPath parentType = varOverride.Value as DMASTConstantPath;

                        if (parentType == null) throw new CompileErrorException(varOverride.Location, "Expected a constant path");
                        _currentObject.Parent = DMObjectTree.GetDMObject(parentType.Value.Path);
                        break;
                    }
                    case "tag":
                        DMCompiler.Error(new CompilerError(varOverride.Location, "tag: may not be set at compile-time"));
                        break;
                    default:
                    {
                        DMVariable variable = new DMVariable(null, varOverride.VarName, false, false);

                        SetVariableValue(variable, varOverride.Value);
                        _currentObject.VariableOverrides[variable.Name] = variable;
                        break;
                    }
                }
            } catch (CompileErrorException e) {
                DMCompiler.Error(e.Error);
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
                    throw new CompileErrorException(procDefinition.Location, $"Type {dmObject.Path} already has a proc named \"{procName}\"");
                }

                DMProc proc;

                if (procDefinition.ObjectPath == null) {
                    if (DMObjectTree.TryGetGlobalProc(procDefinition.Name, out _)) {
                        throw new CompileErrorException(new CompilerError(procDefinition.Location, $"proc {procDefinition.Name} is already defined in global scope"));
                    }

                    proc = DMObjectTree.CreateDMProc(dmObject, procDefinition);
                    DMObjectTree.AddGlobalProc(proc.Name, proc.Id);
                } else {
                    proc = DMObjectTree.CreateDMProc(dmObject, procDefinition);
                    dmObject.AddProc(procName, proc);
                }

                if (procDefinition.Body != null) {
                    foreach (var stmt in GetStatements(procDefinition.Body)) {
                        // TODO multiple var definitions.
                        if (stmt is DMASTProcStatementVarDeclaration varDeclaration && varDeclaration.IsGlobal) {
                            DMVariable variable = proc.CreateGlobalVariable(varDeclaration.Type, varDeclaration.Name, varDeclaration.IsConst);
                            variable.Value = new Expressions.Null(varDeclaration.Location);

                            if (varDeclaration.Value != null) {
                                DMVisitorExpression._scopeMode = "static";
                                DMExpression expression = DMExpression.Create(dmObject, proc, varDeclaration.Value, varDeclaration.Type);
                                DMVisitorExpression._scopeMode = "normal";
                                DMObjectTree.AddGlobalInitAssign(dmObject, proc.GetGlobalVariableId(varDeclaration.Name).Value, expression);
                            }
                        }
                    }
                }

                if (procDefinition.IsVerb && (dmObject.IsSubtypeOf(DreamPath.Atom) || dmObject.IsSubtypeOf(DreamPath.Client)) && !DMCompiler.Settings.NoStandard) {
                    Expressions.Field field = new Expressions.Field(Location.Unknown, dmObject.GetVariable("verbs"));
                    DreamPath procPath = new DreamPath(".proc/" + procName);
                    Expressions.Append append = new Expressions.Append(Location.Unknown, field, new Expressions.Path(Location.Unknown, procPath));

                    dmObject.InitializationProcExpressions.Add(append);
                }
            } catch (CompileErrorException e) {
                DMCompiler.Error(e.Error);
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
                    case DMASTProcStatementInfLoop ps: recurse = new() { ps.Body }; break;
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

        private void SetVariableValue(DMVariable variable, DMASTExpression value, DMValueType valType = DMValueType.Anything) {
            DMVisitorExpression._scopeMode = variable.IsGlobal ? "static" : "normal";
            DMExpression expression = DMExpression.Create(_currentObject, variable.IsGlobal ? DMObjectTree.GlobalInitProc : null, value, variable.Type);
            DMVisitorExpression._scopeMode = "normal";
            expression.ValType = valType;

            if (expression.TryAsConstant(out var constant)) {
                variable.Value = constant;
                return;
            }

            if (variable.IsConst) {
                throw new CompileErrorException(value.Location, "Value of const var must be a constant");
            }

            //Whether this should be initialized at runtime
            bool isValid = expression switch {
                //TODO: A better way of handling procs evaluated at compile time
                Expressions.ProcCall procCall => procCall.GetTargetProc(_currentObject).Proc?.Name switch {
                    "rgb" => true,
                    "generator" => true,
                    "matrix" => true,
                    "icon" => true,
                    "file" => true,
                    "sound" => true,
                    _ => variable.IsGlobal
                },

                Expressions.List => true,
                Expressions.NewList => true,
                Expressions.NewPath => true,
                Expressions.NewMultidimensionalList => true,
                Expressions.GlobalField => variable.IsGlobal, // Global set to another global
                Expressions.StringFormat => variable.IsGlobal,
                _ => false
            };

            if (isValid) {
                variable.Value = new Expressions.Null(Location.Internal);
                EmitInitializationAssign(variable, expression);
            } else {
                throw new CompileErrorException(value.Location, $"Invalid initial value for \"{variable.Name}\"");
            }
        }

        private void EmitInitializationAssign(DMVariable variable, DMExpression expression) {
            if (variable.IsGlobal) {
                int? globalId = _currentObject.GetGlobalVariableId(variable.Name);
                if (globalId == null) throw new Exception($"Invalid global {_currentObject.Path}.{variable.Name}");

                DMObjectTree.AddGlobalInitAssign(_currentObject, globalId.Value, expression);
            } else {
                Expressions.Field field = new Expressions.Field(Location.Unknown, variable);
                Expressions.Assignment assign = new Expressions.Assignment(Location.Unknown, field, expression);

                _currentObject.InitializationProcExpressions.Add(assign);
            }
        }
    }
}
