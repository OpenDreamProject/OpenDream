using Content.Shared.Compiler;
using Content.Shared.Compiler.DM;
using Content.Shared.Dream;

namespace DMCompiler.DM.Visitors {
    class DMVisitorObjectBuilder : DMASTVisitor {
        private DMObject _currentObject = null;

        public void BuildObjectTree(DMASTFile astFile) {
            DMObjectTree.Reset();
            astFile.Visit(this);

            foreach (DMObject dmObject in DMObjectTree.AllObjects.Values) {
                dmObject.CompileProcs();
            }

            DMObjectTree.CreateGlobalInitProc();
        }

        public void VisitFile(DMASTFile file) {
            _currentObject = DMObjectTree.GetDMObject(DreamPath.Root);

            file.BlockInner.Visit(this);
        }

        public void VisitBlockInner(DMASTBlockInner blockInner) {
            foreach (DMASTStatement statement in blockInner.Statements) {
                try {
                    statement.Visit(this);
                } catch (CompileErrorException e) {
                    Program.Error(e.Error);
                }
            }
        }

        public void VisitCallParameter(DMASTCallParameter callParameter) {
            callParameter.Value.Visit(this);
        }

        public void VisitObjectDefinition(DMASTObjectDefinition objectDefinition) {
            DMObject oldObject = _currentObject;

            _currentObject = DMObjectTree.GetDMObject(objectDefinition.Path);
            objectDefinition.InnerBlock?.Visit(this);
            _currentObject = oldObject;
        }

        public void VisitObjectVarDefinition(DMASTObjectVarDefinition varDefinition) {
            DMObject oldObject = _currentObject;
            DMVariable variable = new DMVariable(varDefinition.Type, varDefinition.Name, varDefinition.IsGlobal);

            _currentObject = DMObjectTree.GetDMObject(varDefinition.ObjectPath);

            if (variable.IsGlobal) {
                _currentObject.GlobalVariables[variable.Name] = variable;
            } else {
                _currentObject.Variables[variable.Name] = variable;
            }

            try {
                SetVariableValue(variable, varDefinition.Value, varDefinition.Type);
            } catch (CompileErrorException e) {
                Program.Error(e.Error);
            }

            _currentObject = oldObject;
        }

        public void VisitMultipleObjectVarDefinitions(DMASTMultipleObjectVarDefinitions multipleObjectVarDefinitions) {
            foreach (DMASTObjectVarDefinition varDefinition in multipleObjectVarDefinitions.VarDefinitions) {
                varDefinition.Visit(this);
            }
        }

        public void VisitObjectVarOverride(DMASTObjectVarOverride varOverride) {
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

        public void VisitProcDefinition(DMASTProcDefinition procDefinition) {
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

        private void SetVariableValue(DMVariable variable, DMASTExpression value, DreamPath? type) {
            DMExpression expression = DMExpression.Create(_currentObject, variable.IsGlobal ? DMObjectTree.GlobalInitProc : null, value, type);

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
            Expressions.Field field = new Expressions.Field(variable.Type, variable.Name);
            Expressions.Assignment assign = new Expressions.Assignment(field, expression);

            if (variable.IsGlobal) {
                DMObjectTree.AddGlobalInitProcAssign(assign);
            } else {
                _currentObject.InitializationProcExpressions.Add(assign);
            }
        }
    }
}
