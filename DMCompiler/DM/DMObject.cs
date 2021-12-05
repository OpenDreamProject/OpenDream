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
        public Dictionary<string, List<DMProc>> Procs = new();
        public Dictionary<string, DMVariable> Variables = new();
        public Dictionary<string, DMVariable> VariableOverrides = new(); //NOTE: The type of all these variables are null
        public Dictionary<string, int> GlobalVariables = new();
        public List<DMExpression> InitializationProcExpressions = new();
        public DMProc InitializationProc = null;

        public DMObject(int id, DreamPath path, DMObject parent) {
            Id = id;
            Path = path;
            Parent = parent;
        }

        public void CompileProcs() {
            if (InitializationProcExpressions.Count > 0) {
                CreateInitializationProc();

                foreach (DMExpression expression in InitializationProcExpressions) {
                    expression.EmitPushValue(this, InitializationProc);
                }
            }

            foreach (List<DMProc> procs in Procs.Values) {
                foreach (DMProc proc in procs) {
                    DMCompiler.VerbosePrint($"Compiling proc {Path}.{proc.Name}()");
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
                if ((variable.Value?.ValType & DMValueType.Unimplemented) == DMValueType.Unimplemented && !DMCompiler.Settings.SuppressUnimplementedWarnings)
                {
                    DMCompiler.Warning(new CompilerWarning(Location.Unknown, $"{Path}.{name} is not implemented and will have unexpected behavior"));
                }
                return variable;
            }
            return Parent?.GetVariable(name);
        }

        public bool HasProc(string name) {
            if (Procs.ContainsKey(name)) return true;

            return Parent?.HasProc(name) ?? false;
        }

        public List<DMProc> GetProcs(string name) {
            return Procs.GetValueOrDefault(name, Parent?.GetProcs(name) ?? null);
        }

        public bool IsProcUnimplemented(string name) {
            List<DMProc> procs = GetProcs(name);

            if (procs != null) {
                foreach (DMProc proc in procs) {
                    if (proc.Unimplemented) return true;
                }
            }

            return false;
        }

        public DMVariable CreateGlobalVariable(DreamPath? type, string name) {
            int id = DMObjectTree.CreateGlobal(out DMVariable global, type, name);

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
        }

        public DreamTypeJson CreateJsonRepresentation() {
            DreamTypeJson typeJson = new DreamTypeJson();

            typeJson.Path = Path.PathString;
            typeJson.Parent = Parent?.Id;

            if (Variables.Count > 0 || VariableOverrides.Count > 0) {
                typeJson.Variables = new Dictionary<string, object>();

                foreach (KeyValuePair<string, DMVariable> variable in Variables) {
                    typeJson.Variables.Add(variable.Key, variable.Value.ToJsonRepresentation());
                }

                foreach (KeyValuePair<string, DMVariable> variable in VariableOverrides) {
                    typeJson.Variables[variable.Key] = variable.Value.ToJsonRepresentation();
                }
            }

            if (GlobalVariables.Count > 0) {
                typeJson.GlobalVariables = GlobalVariables;
            }

            if (InitializationProc != null) {
                typeJson.InitProc = InitializationProc.GetJsonRepresentation();
            }

            if (Procs.Count > 0) {
                typeJson.Procs = new Dictionary<string, List<ProcDefinitionJson>>();

                foreach (KeyValuePair<string, List<DMProc>> procs in Procs) {
                    List<ProcDefinitionJson> procJson = new();

                    foreach (DMProc proc in procs.Value) {
                        procJson.Add(proc.GetJsonRepresentation());
                    }

                    typeJson.Procs.Add(procs.Key, procJson);
                }
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
