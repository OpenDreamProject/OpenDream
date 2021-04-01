using DMCompiler.Compiler.DM;
using DMCompiler.Compiler.DM.Visitors;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;

namespace DMCompiler.DM.Visitors {
    class DMVisitorObjectBuilder : DMASTVisitor {
        private Stack<object> _valueStack = new();
        private DMObjectTree _objectTree = null;
        private DMObject _currentObject = null;
        private DMVisitorProcBuilder _procBuilder = new DMVisitorProcBuilder();
        private DMVariable _currentVariable = null;

        public DMObjectTree BuildObjectTree(DMASTFile astFile) {
            _objectTree = new DMObjectTree();
            _valueStack.Clear();
            astFile.Visit(this);

            return _objectTree;
        }

        public void VisitFile(DMASTFile file) {
            _currentObject = _objectTree.GetDMObject(DreamPath.Root);

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
            DreamPath newObjectPath = objectDefinition.Path.Path;

            if (newObjectPath.Type == DreamPath.PathType.Relative) newObjectPath = _currentObject.Path.AddToPath(newObjectPath.PathString);

            _currentObject = _objectTree.GetDMObject(newObjectPath);
            if (objectDefinition.InnerBlock != null) objectDefinition.InnerBlock.Visit(this);
            _currentObject = oldObject;
        }

        public void VisitObjectVarDefinition(DMASTObjectVarDefinition varDefinition) {
            DMObject dmObject = _currentObject;

            if (varDefinition.ObjectPath.HasValue) {
                dmObject = _objectTree.GetDMObject(_currentObject.Path.Combine(varDefinition.ObjectPath.Value));
            }

            _currentVariable = new DMVariable(varDefinition.Type, varDefinition.Name, varDefinition.IsGlobal);
            varDefinition.Value.Visit(this);
            _currentVariable.Value = _valueStack.Pop();

            if (_currentVariable.IsGlobal) {
                dmObject.GlobalVariables[_currentVariable.Name] = _currentVariable;
            } else {
                dmObject.Variables[_currentVariable.Name] = _currentVariable;
            }
        }

        public void VisitObjectVarOverride(DMASTObjectVarOverride varOverride) {
            DMObject dmObject = _currentObject;

            if (varOverride.ObjectPath.HasValue) {
                dmObject = _objectTree.GetDMObject(_currentObject.Path.Combine(varOverride.ObjectPath.Value));
            }

            if (varOverride.VarName == "parent_type") {
                DMASTConstantPath parentType = varOverride.Value as DMASTConstantPath;

                if (parentType == null) throw new Exception("Expected a constant path");
                dmObject.Parent = parentType.Value.Path;
            } else {
                _currentVariable = new DMVariable(null, varOverride.VarName, false); //TODO: Find the type
                varOverride.Value.Visit(this);
                _currentVariable.Value = _valueStack.Pop();

                dmObject.Variables[_currentVariable.Name] = _currentVariable;
            }
        }

        public void VisitProcDefinition(DMASTProcDefinition procDefinition) {
            DMObject dmObject = _currentObject;

            if (procDefinition.ObjectPath.HasValue) {
                dmObject = _objectTree.GetDMObject(_currentObject.Path.Combine(procDefinition.ObjectPath.Value));
            }

            DMProc proc = _procBuilder.BuildProc(procDefinition);

            foreach (DMASTDefinitionParameter parameter in procDefinition.Parameters) {
                proc.AddParameter(parameter.Path.Path.LastElement, parameter.Type);
            }

            if (!dmObject.Procs.ContainsKey(procDefinition.Name)) {
                dmObject.Procs.Add(procDefinition.Name, new List<DMProc>() { proc });
            } else {
                dmObject.Procs[procDefinition.Name].Add(proc);
            }
            
            if (procDefinition.IsVerb) {
                DMVisitorProcBuilder initProcBuilder = new DMVisitorProcBuilder(dmObject.CreateInitializationProc());

                DMASTPath procPath = new DMASTPath(new DreamPath(".proc/" + procDefinition.Name));
                DMASTAppend verbAppend = new DMASTAppend(new DMASTIdentifier("verbs"), new DMASTConstantPath(procPath));
                verbAppend.Visit(initProcBuilder);
            }
        }

        public void VisitNewPath(DMASTNewPath newPath) {
            DMProc initProc = _currentVariable.IsGlobal ? Program.GlobalInitProc : _currentObject.CreateInitializationProc();
            DMVisitorProcBuilder initProcBuilder = new DMVisitorProcBuilder(initProc);

            new DMASTAssign(new DMASTIdentifier(_currentVariable.Name), newPath).Visit(initProcBuilder);

            _valueStack.Push(null);
        }

        public void VisitNewInferred(DMASTNewInferred newInferred) {
            if (_currentVariable.Type == null) throw new Exception("Implicit new() requires a type");

            DMProc initProc = _currentVariable.IsGlobal ? Program.GlobalInitProc : _currentObject.CreateInitializationProc();
            DMVisitorProcBuilder initProcBuilder = new DMVisitorProcBuilder(initProc);

            DMASTPath path = new DMASTPath(_currentVariable.Type.Value);
            DMASTNewPath newPath = new DMASTNewPath(path, newInferred.Parameters);
            new DMASTAssign(new DMASTIdentifier(_currentVariable.Name), newPath).Visit(initProcBuilder);

            _valueStack.Push(null);
        }

        public void VisitList(DMASTList list) {
            DMProc initProc = _currentVariable.IsGlobal ? Program.GlobalInitProc : _currentObject.CreateInitializationProc();
            DMVisitorProcBuilder initProcBuilder = new DMVisitorProcBuilder(initProc);

            new DMASTAssign(new DMASTIdentifier(_currentVariable.Name), list).Visit(initProcBuilder);

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
    }
}
