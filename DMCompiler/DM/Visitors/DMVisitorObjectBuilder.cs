using DMCompiler.Compiler.DM;
using DMCompiler.Compiler.DM.Visitors;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;

namespace DMCompiler.DM.Visitors {
    class DMVisitorObjectBuilder : DMASTVisitor {
        private Stack<object> _valueStack = new();
        private Dictionary<DreamPath, DMObject> _dmObjects = new();
        private UInt32 _dmObjectIdCounter = 0;
        private DMObject _currentObject = null;
        private DMVisitorProcBuilder _procBuilder = new DMVisitorProcBuilder();
        private DMVariable _currentVariable = null;

        public Dictionary<DreamPath, DMObject> BuildObjects(DMASTFile astFile) {
            _valueStack.Clear();
            _dmObjects.Clear();
            _dmObjectIdCounter = 0;
            astFile.Visit(this);

            return _dmObjects;
        }

        public void VisitFile(DMASTFile file) {
            _currentObject = GetDMObject(DreamPath.Root);

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

            _currentObject = GetDMObject(newObjectPath);
            if (objectDefinition.InnerBlock != null) objectDefinition.InnerBlock.Visit(this);
            _currentObject = oldObject;
        }

        public void VisitObjectVarDefinition(DMASTObjectVarDefinition varDefinition) {
            DMObject dmObject = _currentObject;

            if (varDefinition.ObjectPath != null) {
                dmObject = GetDMObject(_currentObject.Path.Combine(varDefinition.ObjectPath.Path));
            }

            _currentVariable = new DMVariable((varDefinition.Type != null) ? varDefinition.Type.Path : null, varDefinition.Name, varDefinition.IsGlobal);
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

            if (varOverride.ObjectPath != null) {
                dmObject = GetDMObject(_currentObject.Path.Combine(varOverride.ObjectPath.Path));
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
            string procName = procDefinition.Path.Path.LastElement;
            DreamPath objectPath = _currentObject.Path.Combine(procDefinition.Path.Path.FromElements(0, -2));
            int procElementIndex = objectPath.FindElement("proc");
            if (procElementIndex == -1) procElementIndex = objectPath.FindElement("verb");

            if (procElementIndex != -1) {
                objectPath = objectPath.RemoveElement(procElementIndex);
            }

            DMObject dmObject = GetDMObject(objectPath);
            DMProc proc = _procBuilder.BuildProc(procDefinition);
            if (!dmObject.Procs.ContainsKey(procName)) dmObject.Procs.Add(procName, new List<DMProc>());

            foreach (DMASTDefinitionParameter parameter in procDefinition.Parameters) {
                proc.Parameters.Add(parameter.Path.Path.LastElement);
            }

            dmObject.Procs[procName].Add(proc);
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

        private DMObject GetDMObject(DreamPath path) {
            DMObject dmObject;
            if (!_dmObjects.TryGetValue(path, out dmObject)) {
                DreamPath? parentType = null;

                if (path.Elements.Length >= 2) {
                    parentType = path.FromElements(0, -2);
                    GetDMObject(parentType.Value); //Make sure the parent exists
                }

                dmObject = new DMObject(_dmObjectIdCounter++, path, parentType);
                _dmObjects.Add(path, dmObject);
            }

            return dmObject;
        }

        private DMVariable GetVariable(DMObject dmObject, string variableName) {
            DMVariable variable = null;

            while (variable == null) {
                if (!dmObject.Variables.TryGetValue(variableName, out variable)) {
                    if (dmObject.Parent == null) break;

                    dmObject = GetDMObject(dmObject.Parent.Value);
                }
            }

            return variable;
        }
    }
}
