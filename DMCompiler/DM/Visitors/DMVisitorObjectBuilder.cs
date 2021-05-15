using DMCompiler.Compiler.DM;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;

namespace DMCompiler.DM.Visitors {
    class DMVisitorObjectBuilder : DMASTVisitor {
        private Stack<object> _valueStack = new();
        private DMObject _currentObject = null;
        private DMVariable _currentVariable = null;

        public void BuildObjectTree(DMASTFile astFile) {
            DMObjectTree.Reset();
            _valueStack.Clear();
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
                statement.Visit(this);
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

            _currentObject = DMObjectTree.GetDMObject(varDefinition.ObjectPath);
            _currentVariable = new DMVariable(varDefinition.Type, varDefinition.Name, varDefinition.IsGlobal);

            if (_currentVariable.IsGlobal) {
                _currentObject.GlobalVariables[_currentVariable.Name] = _currentVariable;
            } else {
                _currentObject.Variables[_currentVariable.Name] = _currentVariable;
            }

            varDefinition.Value.Visit(this);
            _currentVariable.Value = _valueStack.Pop();
            _currentObject = oldObject;
        }

        public void VisitObjectVarOverride(DMASTObjectVarOverride varOverride) {
            DMObject oldObject = _currentObject;

            _currentObject = DMObjectTree.GetDMObject(varOverride.ObjectPath);

            if (varOverride.VarName == "parent_type") {
                DMASTConstantPath parentType = varOverride.Value as DMASTConstantPath;

                if (parentType == null) throw new Exception("Expected a constant path");
                _currentObject.Parent = DMObjectTree.GetDMObject(parentType.Value.Path);
            } else {
                _currentVariable = new DMVariable(null, varOverride.VarName, false);
                varOverride.Value.Visit(this);
                _currentVariable.Value = _valueStack.Pop();

                _currentObject.VariableOverrides[_currentVariable.Name] = _currentVariable;
            }

            _currentObject = oldObject;
        }

        public void VisitProcDefinition(DMASTProcDefinition procDefinition) {
            string procName = procDefinition.Name;
            DMObject dmObject = _currentObject;

            if (procDefinition.ObjectPath.HasValue) {
                dmObject = DMObjectTree.GetDMObject(_currentObject.Path.Combine(procDefinition.ObjectPath.Value));
            }

            if (!procDefinition.IsOverride && dmObject.HasProc(procName)) {
                throw new Exception("Type " + dmObject.Path + " already has a proc named \"" + procName + "\"");
            }

            DMProc proc = new DMProc(procDefinition);
            
            dmObject.AddProc(procName, proc);
            if (procDefinition.IsVerb) {
                DMASTPath procPath = new DMASTPath(new DreamPath(".proc/" + procName));
                DMASTAppend verbAppend = new DMASTAppend(new DMASTIdentifier("verbs"), new DMASTConstantPath(procPath));

                dmObject.InitializationProcStatements.Add(new DMASTProcStatementExpression(verbAppend));
            }
        }

        #region Values
        public void VisitNewPath(DMASTNewPath newPath) {
            DMASTAssign assign = new DMASTAssign(new DMASTIdentifier(_currentVariable.Name), newPath);
            DMASTProcStatementExpression statement = new DMASTProcStatementExpression(assign);

            if (_currentVariable.IsGlobal) {
                DMObjectTree.AddGlobalInitProcStatement(statement);
            } else {
                _currentObject.InitializationProcStatements.Add(statement);
            }

            _valueStack.Push(null);
        }

        public void VisitNewInferred(DMASTNewInferred newInferred) {
            DMASTAssign assign = new DMASTAssign(new DMASTIdentifier(_currentVariable.Name), newInferred);
            DMASTProcStatementExpression statement = new DMASTProcStatementExpression(assign);

            if (_currentVariable.IsGlobal) {
                DMObjectTree.AddGlobalInitProcStatement(statement);
            } else {
                _currentObject.InitializationProcStatements.Add(statement);
            }

            _valueStack.Push(null);
        }

        public void VisitList(DMASTList list) {
            DMASTAssign assign = new DMASTAssign(new DMASTIdentifier(_currentVariable.Name), list);
            DMASTProcStatementExpression statement = new DMASTProcStatementExpression(assign);

            if (_currentVariable.IsGlobal) {
                DMObjectTree.AddGlobalInitProcStatement(statement);
            } else {
                _currentObject.InitializationProcStatements.Add(statement);
            }

            _valueStack.Push(null);
        }

        public void VisitStringFormat(DMASTStringFormat stringFormat) {
            if (!_currentVariable.IsGlobal) throw new Exception("Initial value of '" + _currentVariable.Name + "' cannot be a formatted string.");

            DMASTAssign assign = new DMASTAssign(new DMASTIdentifier(_currentVariable.Name), stringFormat);
            DMASTProcStatementExpression statement = new DMASTProcStatementExpression(assign);
            DMObjectTree.AddGlobalInitProcStatement(statement);

            _valueStack.Push(null);
        }

        public void VisitProcCall(DMASTProcCall procCall) {
            if (!_currentVariable.IsGlobal) throw new Exception("Initial value of '" + _currentVariable.Name + "' cannot be a proc call.");

            DMASTAssign assign = new DMASTAssign(new DMASTIdentifier(_currentVariable.Name), procCall);
            DMASTProcStatementExpression statement = new DMASTProcStatementExpression(assign);
            DMObjectTree.AddGlobalInitProcStatement(statement);

            _valueStack.Push(null);
        }

        public void VisitConstantNull(DMASTConstantNull constantNull) {
            _valueStack.Push(null);
        }

        public void VisitConstantPath(DMASTConstantPath constantPath) {
            _valueStack.Push(constantPath.Value.Path);
        }

        public void VisitConstantString(DMASTConstantString constantPath) {
            _valueStack.Push(constantPath.Value);
        }

        public void VisitConstantResource(DMASTConstantResource constantResource) {
            _valueStack.Push(new DMResource(constantResource.Path));
        }

        public void VisitConstantInteger(DMASTConstantInteger constantInteger) {
            _valueStack.Push(constantInteger.Value);
        }

        public void VisitConstantFloat(DMASTConstantFloat constantFloat) {
            _valueStack.Push(constantFloat.Value);
        }
        #endregion Values
    }
}
