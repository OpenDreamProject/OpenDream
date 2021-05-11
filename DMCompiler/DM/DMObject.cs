using DMCompiler.Compiler.DM;
using DMCompiler.DM.Visitors;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;

namespace DMCompiler.DM {
    class DMObject {
        public UInt32 Id;
        public DreamPath Path;
        public DMObject Parent;
        public Dictionary<string, List<DMProc>> Procs = new();
        public Dictionary<string, DMVariable> Variables = new();
        public Dictionary<string, DMVariable> VariableOverrides = new(); //NOTE: The type of all these variables are null
        public Dictionary<string, DMVariable> GlobalVariables = new();
        public List<DMASTProcStatement> InitializationProcStatements = new();
        public DMProc InitializationProc = null;

        public DMObject(UInt32 id, DreamPath path, DMObject parent) {
            Id = id;
            Path = path;
            Parent = parent;
        }

        public void CompileProcs() {
            if (InitializationProcStatements.Count > 0) {
                DMVisitorProcBuilder initProcBuilder = new DMVisitorProcBuilder(this, CreateInitializationProc());

                foreach (DMASTProcStatement statement in InitializationProcStatements) {
                    statement.Visit(initProcBuilder);
                }
            }

            foreach (List<DMProc> procs in Procs.Values) {
                foreach (DMProc proc in procs) {
                    proc.Compile(this);
                }
            }
        }

        public void AddProc(string name, DMProc proc) {
            if (!Procs.ContainsKey(name)) Procs.Add(name, new List<DMProc>());

            Procs[name].Add(proc);
        }

        public DMVariable GetVariable(string name) {
            if (Variables.TryGetValue(name, out DMVariable variable)) {
                return variable;
            }
            return Parent?.GetVariable(name);
        }

        public bool HasProc(string name) {
            if (Procs.ContainsKey(name)) return true;
            else if (Parent != null) return Parent.HasProc(name);
            else return false;
        }
        
        public DMVariable GetGlobalVariable(string name) {
            if (GlobalVariables.TryGetValue(name, out DMVariable variable)) {
                return variable;
            }
            return Parent?.GetGlobalVariable(name);
        }

        public DMProc CreateInitializationProc() {
            if (InitializationProc == null) {
                InitializationProc = new DMProc(null);

                InitializationProc.PushSuperProc();
                InitializationProc.JumpIfFalse("no_super");

                InitializationProc.PushSuperProc();
                InitializationProc.PushArguments(0);
                InitializationProc.Call();

                InitializationProc.AddLabel("no_super");
                InitializationProc.ResolveLabels();
            }

            return InitializationProc;
        }

        public DreamObjectJson CreateJsonRepresentation() {
            DreamObjectJson objectJson = new DreamObjectJson();

            objectJson.Name = (!Path.Equals(DreamPath.Root)) ? Path.LastElement : "";
            objectJson.Parent = Parent?.Path.PathString;

            if (Variables.Count > 0 || VariableOverrides.Count > 0) {
                objectJson.Variables = new Dictionary<string, object>();

                foreach (KeyValuePair<string, DMVariable> variable in Variables) {
                    objectJson.Variables.Add(variable.Key, CreateDreamObjectJsonVariable(variable.Value.Value));
                }

                foreach (KeyValuePair<string, DMVariable> variable in VariableOverrides) {
                    objectJson.Variables[variable.Key] = CreateDreamObjectJsonVariable(variable.Value.Value);
                }
            }

            if (GlobalVariables.Count > 0) {
                objectJson.GlobalVariables = new Dictionary<string, object>();

                foreach (KeyValuePair<string, DMVariable> variable in GlobalVariables) {
                    objectJson.GlobalVariables.Add(variable.Key, CreateDreamObjectJsonVariable(variable.Value.Value));
                }
            }

            if (InitializationProc != null) {
                objectJson.InitProc = new ProcDefinitionJson() {
                    Bytecode = InitializationProc.Bytecode.ToArray()
                };
            }

            if (Procs.Count > 0) {
                objectJson.Procs = new Dictionary<string, List<ProcDefinitionJson>>();

                foreach (KeyValuePair<string, List<DMProc>> procs in Procs) {
                    List<ProcDefinitionJson> procJson = new List<ProcDefinitionJson>();

                    foreach (DMProc proc in procs.Value) {
                        procJson.Add(proc.GetJsonRepresentation());
                    }

                    objectJson.Procs.Add(procs.Key, procJson);
                }
            }

            return objectJson;
        }

        private static object CreateDreamObjectJsonVariable(object value) {
            if (value is int || value is float || value is string) {
                return value;
            } else if (value is null) {
                return new Dictionary<string, object>() {
                    { "type", JsonVariableType.Null }
                };
            } else if (value is DMResource resource) {
                return new Dictionary<string, object>() {
                    { "type", JsonVariableType.Resource },
                    { "resourcePath", resource.ResourcePath }
                };
            } else if (value is DreamPath path) {
                return new Dictionary<string, object>() {
                    { "type", JsonVariableType.Path },
                    { "value", path.PathString }
                };
            } else {
                throw new Exception("Invalid variable value");
            }
        }
    }
}
