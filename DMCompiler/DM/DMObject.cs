using DMCompiler.DM.Visitors;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.DM {
    class DMObject {
        public int Id;
        public DreamPath Path;
        public DMObject Parent;
        public Dictionary<string, List<int>> Procs = new();
        public Dictionary<string, DMVariable> Variables = new();
        public Dictionary<string, DMVariable> VariableOverrides = new(); //NOTE: The type of all these variables are null
        public Dictionary<string, int> GlobalVariables = new();
        public List<DMExpression> InitializationProcExpressions = new();
        public int? InitializationProc;

        public DMObject(int id, DreamPath path, DMObject parent) {
            Id = id;
            Path = path;
            Parent = parent;
        }
        public void AddProc(string name, DMProc proc) {
            if (!Procs.ContainsKey(name)) Procs.Add(name, new List<int>(1));

            Procs[name].Add(proc.Id);
        }

        public DMVariable GetVariable(string name) {
            if (Variables.TryGetValue(name, out DMVariable variable)) {
                return variable;
            }
            return Parent?.GetVariable(name);
        }

        public bool HasProc(string name) {
            if (Procs.ContainsKey(name)) return true;

            return Parent?.HasProc(name) ?? false;
        }

        public List<int> GetProcs(string name) {
            return Procs.GetValueOrDefault(name, Parent?.GetProcs(name) ?? null);
        }

        public bool IsProcUnimplemented(string name) {
            List<int> procs = GetProcs(name);

            if (procs != null) {
                foreach (int procId in procs)
                {
                    DMProc proc = DMObjectTree.AllProcs[procId];
                    if ((proc.Attributes & ProcAttributes.Unimplemented) == ProcAttributes.Unimplemented) return true;
                }
            }

            return false;
        }

        public DMVariable CreateGlobalVariable(DreamPath? type, string name, bool isConst) {
            int id = DMObjectTree.CreateGlobal(out DMVariable global, type, name, isConst);

            GlobalVariables[name] = id;
            return global;
        }

        public int? GetGlobalVariableId(string name) {
            if (GlobalVariables.TryGetValue(name, out int id)) {
                return id;
            }

            return Parent?.GetGlobalVariableId(name);
        }

        public DMVariable GetGlobalVariable(string name) {
            int? id = GetGlobalVariableId(name);

            return (id == null) ? null : DMObjectTree.Globals[id.Value];
        }

        public void CreateInitializationProc() {
            if (InitializationProcExpressions.Count > 0 && InitializationProc == null)
            {
                var init = DMObjectTree.CreateDMProc(this, null);
                InitializationProc = init.Id;
                init.PushArguments(0);
                init.Call(DMReference.SuperProc);

                foreach (DMExpression expression in InitializationProcExpressions) {
                    try {
                        expression.EmitPushValue(this, init);
                    } catch (CompileErrorException e) {
                        DMCompiler.Error(e.Error);
                    }
                }
            }
        }

        public DreamTypeJson CreateJsonRepresentation() {
            DreamTypeJson typeJson = new DreamTypeJson();

            typeJson.Path = Path.PathString;
            typeJson.Parent = Parent?.Id;

            if (Variables.Count > 0 || VariableOverrides.Count > 0) {
                typeJson.Variables = new Dictionary<string, object>();

                foreach (KeyValuePair<string, DMVariable> variable in Variables) {
                    if (!variable.Value.TryAsJsonRepresentation(out var valueJson))
                        throw new Exception($"Failed to serialize {Path}.{variable.Key}");

                    typeJson.Variables.Add(variable.Key, valueJson);
                }

                foreach (KeyValuePair<string, DMVariable> variable in VariableOverrides) {
                    if (!variable.Value.TryAsJsonRepresentation(out var valueJson))
                        throw new Exception($"Failed to serialize {Path}.{variable.Key}");

                    typeJson.Variables[variable.Key] = valueJson;
                }
            }

            if (GlobalVariables.Count > 0) {
                typeJson.GlobalVariables = GlobalVariables;
            }

            if (InitializationProc != null)
            {
                typeJson.InitProc = InitializationProc;
            }

            if (Procs.Count > 0)
            {
                typeJson.Procs = new List<List<int>>(Procs.Values);
            }
            return typeJson;
        }

        public bool IsSubtypeOf(DreamPath path) {
            if (Path.IsDescendantOf(path)) return true;
            if (Parent != null) return Parent.IsSubtypeOf(path);
            return false;
        }
    }
}
