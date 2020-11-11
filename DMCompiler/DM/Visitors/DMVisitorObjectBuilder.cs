using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;

namespace DMCompiler.DM.Visitors {
    class DMVisitorObjectBuilder : DMASTVisitor {
        private Stack<object> _valueStack = new Stack<object>();
        private Dictionary<DreamPath, DMObject> _dmObjects = new Dictionary<DreamPath, DMObject>();
        private UInt32 _dmObjectIdCounter = 0;
        private DMObject _currentObject = null;
        private DMVisitorProcBuilder _procBuilder = new DMVisitorProcBuilder();

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

        public void VisitObjectDefinition(DMASTObjectDefinition objectDefinition) {
            DMObject oldObject = _currentObject;
            DreamPath newObjectPath = objectDefinition.Path.Path;

            if (newObjectPath.Type == DreamPath.PathType.Relative) newObjectPath = _currentObject.Path.AddToPath(newObjectPath.PathString);

            _currentObject = GetDMObject(newObjectPath);
            if (objectDefinition.InnerBlock != null) objectDefinition.InnerBlock.Visit(this);
            _currentObject = oldObject;
        }

        public void VisitObjectVarDefinition(DMASTObjectVarDefinition varDefinition) {
            if (varDefinition.Name == "parent_type") {
                DMASTConstantPath parentType = varDefinition.Value as DMASTConstantPath;

                if (parentType == null) throw new Exception("Expected a constant path");
                _currentObject.Parent = parentType.Value.Path;
            } else {
                object value;

                if (varDefinition.Value is DMASTNewInferred) {
                    value = new DMNewInstance(varDefinition.Type.Path);
                } else {
                    varDefinition.Value.Visit(this);
                    value = _valueStack.Pop();
                }

                _currentObject.Variables.Add(varDefinition.Name, value);
            }
        }

        public void VisitProcDefinition(DMASTProcDefinition procDefinition) {
            string procName = procDefinition.Path.Path.LastElement;
            DreamPath objectPath = _currentObject.Path.Combine(procDefinition.Path.Path.FromElements(0, -2));
            int procElementIndex = objectPath.FindElement("proc");

            if (procElementIndex != -1) {
                objectPath = objectPath.RemoveElement(procElementIndex);
            }

            DMObject dmObject = GetDMObject(objectPath);
            DMProc proc = _procBuilder.BuildProc(procDefinition);
            if (!dmObject.Procs.ContainsKey(procName)) dmObject.Procs.Add(procName, new List<DMProc>());

            foreach (DMASTDefinitionParameter parameter in procDefinition.Parameters) {
                object defaultValue;

                if (parameter.Value != null) {
                    parameter.Value.Visit(this);

                    defaultValue = _valueStack.Pop();
                } else {
                    defaultValue = null;
                }
                
                proc.Parameters.Add(new DMProc.Parameter(parameter.Path.Path.LastElement, defaultValue));
            }

            dmObject.Procs[procName].Add(proc);
        }

        public void VisitProcCall(DMASTProcCall procCall) {
            DMASTCallableIdentifier identifier = procCall.Callable as DMASTCallableIdentifier;

            if (identifier != null && identifier.Identifier == "list") {
                List<object> values = new List<object>();

                foreach (DMASTCallParameter parameter in procCall.Parameters) {
                    if (parameter.Name != null) throw new Exception("Associated list values are not yet supported in this context");

                    parameter.Visit(this);
                    values.Add(_valueStack.Pop());
                }

                _valueStack.Push(new DMList(values.ToArray()));
            } else {
                throw new Exception("Invalid value");
            }
        }

        public void VisitExpressionNegate(DMASTExpressionNegate negate) {
            negate.Expression.Visit(this);
            object value = _valueStack.Pop();

            if (value is int) {
                _valueStack.Push(-(int)value);
            } else {
                throw new Exception("Invalid value");
            }
        }

        public void VisitBinaryOr(DMASTBinaryOr binaryOr) {
            binaryOr.B.Visit(this);
            binaryOr.A.Visit(this);
            object a = _valueStack.Pop();
            object b = _valueStack.Pop();

            if (a is int && b is int) {
                _valueStack.Push((int)a | (int)b);
            } else {
                throw new Exception("Invalid value");
            }
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
                DreamPath parentType = path.FromElements(0, -2);

                if (parentType.Elements.Length >= 1) {
                    GetDMObject(parentType); //Make sure the parent exists
                }

                dmObject = new DMObject(_dmObjectIdCounter++, path, parentType);
                _dmObjects.Add(path, dmObject);
            }

            return dmObject;
        }
    }
}
