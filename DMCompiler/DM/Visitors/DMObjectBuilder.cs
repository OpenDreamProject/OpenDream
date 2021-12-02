using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using System;
using OpenDreamShared.Dream.Procs;

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

            _currentObject = DMObjectTree.GetDMObject(objectDefinition.Path);
            if (objectDefinition.InnerBlock != null) ProcessBlockInner(objectDefinition.InnerBlock);
            _currentObject = oldObject;
        }

        public void ProcessVarDefinition(DMASTObjectVarDefinition varDefinition) {
            DMObject oldObject = _currentObject;
            DMVariable variable;

            _currentObject = DMObjectTree.GetDMObject(varDefinition.ObjectPath);

            if (varDefinition.IsGlobal) {
                variable = _currentObject.CreateGlobalVariable(varDefinition.Type, varDefinition.Name);
            } else {
                variable = new DMVariable(varDefinition.Type, varDefinition.Name, varDefinition.IsGlobal);
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
                } else {
                    DMVariable variable = new DMVariable(null, varOverride.VarName, false);

                    SetVariableValue(variable, varOverride.Value);
                    _currentObject.VariableOverrides[variable.Name] = variable;
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
                    throw new CompileErrorException(procDefinition.Location, "Type " + dmObject.Path + " already has a proc named \"" + procName + "\"");
                }

                DMProc proc = new DMProc(procDefinition);

                dmObject.AddProc(procName, proc);
                if (procDefinition.IsVerb) {
                    Expressions.Field field = new Expressions.Field(DreamPath.List, "verbs");
                    DreamPath procPath = new DreamPath(".proc/" + procName);
                    Expressions.Append append = new Expressions.Append(field, new Expressions.Path(procPath));

                    dmObject.InitializationProcExpressions.Add(append);
                }
            } catch (CompileErrorException e) {
                DMCompiler.Error(e.Error);
            }
        }

        private void SetVariableValue(DMVariable variable, DMASTExpression value, DMValueType valType = DMValueType.Anything) {
            DMExpression expression = DMExpression.Create(_currentObject, variable.IsGlobal ? DMObjectTree.GlobalInitProc : null, value, variable.Type);
            expression.ValType = valType;

            switch (expression) {
                case Expressions.List:
                case Expressions.NewPath:
                    variable.Value = new Expressions.Null();
                    EmitInitializationAssign(variable, expression);
                    break;
                case Expressions.StringFormat:
                case Expressions.ProcCall:
                    if (!variable.IsGlobal) throw new CompileErrorException(value.Location,$"Invalid initial value for \"{variable.Name}\"");

                    variable.Value = new Expressions.Null();
                    EmitInitializationAssign(variable, expression);
                    break;
                default:
                    variable.Value = expression.ToConstant();
                    break;
            }
        }

        private void EmitInitializationAssign(DMVariable variable, DMExpression expression) {
            if (variable.IsGlobal) {
                int? globalId = _currentObject.GetGlobalVariableId(variable.Name);
                if (globalId == null) throw new Exception($"Invalid global {_currentObject.Path}.{variable.Name}");

                Expressions.GlobalField field = new Expressions.GlobalField(variable.Type, globalId.Value);
                Expressions.Assignment assign = new Expressions.Assignment(field, expression);

                DMObjectTree.AddGlobalInitProcAssign(assign);
            } else {
                Expressions.Field field = new Expressions.Field(variable.Type, variable.Name);
                Expressions.Assignment assign = new Expressions.Assignment(field, expression);

                _currentObject.InitializationProcExpressions.Add(assign);
            }
        }
    }
}
