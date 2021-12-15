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

            foreach (DMObject dmObject in DMObjectTree.AllObjects) {
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

            try {
                if (varOverride.VarName == "parent_type") {
                    DMASTConstantPath parentType = varOverride.Value as DMASTConstantPath;

                    if (parentType == null) throw new CompileErrorException(varOverride.Location, "Expected a constant path");
                    _currentObject.Parent = DMObjectTree.GetDMObject(parentType.Value.Path);
                } else if (varOverride.VarName == "name" && _currentObject.IsSubtypeOf(DreamPath.Atom) && !_currentObject.VariableOverrides.ContainsKey("text"))
                {
                    SetVarOverride();

                    var text = new DMVariable(null, "text", false, false);
                    var name = varOverride.Value as DMASTConstantString;

                    if (name is null || name.Value.Length < 1)
                    {
                        return;
                    }

                    SetVariableValue(text, new DMASTConstantString(varOverride.Location, name.Value[0].ToString()));
                    _currentObject.VariableOverrides[text.Name] = text;
                }
                else {
                    SetVarOverride();
                }
            } catch (CompileErrorException e) {
                DMCompiler.Error(e.Error);
            }

            _currentObject = oldObject;

            void SetVarOverride()
            {
                DMVariable variable = new DMVariable(null, varOverride.VarName, false, false);

                SetVariableValue(variable, varOverride.Value);
                _currentObject.VariableOverrides[variable.Name] = variable;
            }
        }

        public void ProcessProcDefinition(DMASTProcDefinition procDefinition) {
            string procName = procDefinition.Name;
            DMObject dmObject = _currentObject;

            try {
                if (procDefinition.ObjectPath.HasValue) {
                    dmObject = DMObjectTree.GetDMObject(_currentObject.Path.Combine(procDefinition.ObjectPath.Value));
                }

                if (!procDefinition.IsOverride && dmObject.HasProc(procName)) {
                    throw new CompileErrorException(procDefinition.Location, "Type " + dmObject.Path + " already has a proc named \"" + procName + "\"");
                }

                DMProc proc = new DMProc(procDefinition);

                dmObject.AddProc(procName, proc);

                if (procDefinition.Body != null)
                {
                    foreach (var stmt in GetStatements(procDefinition.Body))
                    {
                        // TODO multiple var definitions.
                        if (stmt is DMASTProcStatementVarDeclaration varDeclaration && varDeclaration.IsGlobal)
                        {
                            DMVariable variable = proc.CreateGlobalVariable(varDeclaration.Type, varDeclaration.Name, varDeclaration.IsConst);
                            variable.Value = new Expressions.Null(varDeclaration.Location);

                            Expressions.GlobalField field = new Expressions.GlobalField(varDeclaration.Location, variable.Type, proc.GetGlobalVariableId(varDeclaration.Name).Value);
                            if (varDeclaration.Value != null)
                            {
                                DMVisitorExpression._scopeMode = "static";
                                DMExpression expression = DMExpression.Create(_currentObject, DMObjectTree.GlobalInitProc, varDeclaration.Value, varDeclaration.Type);
                                DMVisitorExpression._scopeMode = "normal";
                                Expressions.Assignment assign = new Expressions.Assignment(varDeclaration.Location, field, expression);
                                DMObjectTree.AddGlobalInitProcAssign(assign);
                            }
                        }
                    }
                }

                if (procDefinition.IsVerb) {
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
        static public IEnumerable<DMASTProcStatement> GetStatements(DMASTProcBlockInner block)
        {
            foreach (var stmt in block.Statements)
            {
                yield return stmt;
                List<DMASTProcBlockInner> recurse;
                switch (stmt)
                {
                    case DMASTProcStatementSpawn ps: recurse = new() { ps.Body }; break;
                    case DMASTProcStatementIf ps: recurse = new() { ps.Body, ps.ElseBody }; break;
                    case DMASTProcStatementFor ps: recurse = new() { ps.Body }; break;
                    case DMASTProcStatementForLoop ps: recurse = new() { ps.Body }; break;
                    case DMASTProcStatementWhile ps: recurse = new() { ps.Body }; break;
                    case DMASTProcStatementDoWhile ps: recurse = new() { ps.Body }; break;
                    case DMASTProcStatementInfLoop ps: recurse = new() { ps.Body }; break;
                    // TODO Good luck if you declare a static var inside a switch
                    case DMASTProcStatementSwitch ps:
                        {
                            recurse = new();
                            foreach (var swcase in ps.Cases)
                            {
                                recurse.Add(swcase.Body);
                            }
                            break;
                        }
                    case DMASTProcStatementTryCatch ps: recurse = new() { ps.TryBody, ps.CatchBody }; break;
                    default: recurse = new(); break;
                }
                foreach (var subblock in recurse)
                {
                    if (subblock == null) { continue; }
                    foreach (var substmt in GetStatements(subblock))
                    {
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

            switch (expression) {
                case Expressions.List:
                case Expressions.NewList:
                case Expressions.NewPath:
                //Not the best way to check the rgb() proc, but temporary
                case Expressions.ProcCall procCall when procCall.GetTargetProc(_currentObject).Proc?.Name == "rgb": 
                    //TODO: A more proper compile-time evaluation of rgb()
                    variable.Value = new Expressions.Null(Location.Unknown);
                    EmitInitializationAssign(variable, expression);
                    break;
                case Expressions.StringFormat:
                case Expressions.ProcCall:
                    if (!variable.IsGlobal) throw new CompileErrorException(value.Location,$"Invalid initial value for \"{variable.Name}\"");

                    variable.Value = new Expressions.Null(Location.Unknown);
                    EmitInitializationAssign(variable, expression);
                    break;
                default:
                    throw new CompileErrorException(value.Location, $"Invalid initial value for \"{variable.Name}\"");
            }
        }

        private void EmitInitializationAssign(DMVariable variable, DMExpression expression) {
            if (variable.IsGlobal) {
                int? globalId = _currentObject.GetGlobalVariableId(variable.Name);
                if (globalId == null) throw new Exception($"Invalid global {_currentObject.Path}.{variable.Name}");

                Expressions.GlobalField field = new Expressions.GlobalField(Location.Unknown, variable.Type, globalId.Value);
                Expressions.Assignment assign = new Expressions.Assignment(Location.Unknown, field, expression);

                DMObjectTree.AddGlobalInitProcAssign(assign);
            } else {
                Expressions.Field field = new Expressions.Field(Location.Unknown, variable);
                Expressions.Assignment assign = new Expressions.Assignment(Location.Unknown, field, expression);

                _currentObject.InitializationProcExpressions.Add(assign);
            }
        }
    }
}
