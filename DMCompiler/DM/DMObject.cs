﻿using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.DM {
    class DMObject {
        public UInt32 Id;
        public DreamPath Path;
        public DMObject Parent;
        public Dictionary<string, List<DMProc>> Procs = new();
        public Dictionary<string, DMVariable> Variables = new();
        public Dictionary<string, DMVariable> VariableOverrides = new(); //NOTE: The type of all these variables are null
        public Dictionary<string, int> GlobalVariables = new();
        public List<DMExpression> InitializationProcExpressions = new();
        public DMProc InitializationProc = null;

        public DMObject(UInt32 id, DreamPath path, DMObject parent) {
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
                if ((variable.Value?.ValType & DMValueType.Unimplemented) == DMValueType.Unimplemented && Program.WarnForUnimplemented)
                {
                    Program.Warning(new CompilerWarning(null, $"{Path}.{name} is not implemented and will have unexpected behavior"));
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

        public DreamObjectJson CreateJsonRepresentation() {
            DreamObjectJson objectJson = new DreamObjectJson();

            objectJson.Name = (!Path.Equals(DreamPath.Root)) ? Path.LastElement : "";
            objectJson.Parent = Parent?.Path.PathString;

            if (Variables.Count > 0 || VariableOverrides.Count > 0) {
                objectJson.Variables = new Dictionary<string, object>();

                foreach (KeyValuePair<string, DMVariable> variable in Variables) {
                    objectJson.Variables.Add(variable.Key, variable.Value.ToJsonRepresentation());
                }

                foreach (KeyValuePair<string, DMVariable> variable in VariableOverrides) {
                    objectJson.Variables[variable.Key] = variable.Value.ToJsonRepresentation();
                }
            }

            if (GlobalVariables.Count > 0) {
                objectJson.GlobalVariables = GlobalVariables;
            }

            if (InitializationProc != null) {
                objectJson.InitProc = new ProcDefinitionJson() {
                    Bytecode = InitializationProc.Bytecode.ToArray()
                };
            }

            if (Procs.Count > 0) {
                objectJson.Procs = new Dictionary<string, List<ProcDefinitionJson>>();

                foreach (KeyValuePair<string, List<DMProc>> procs in Procs) {
                    List<ProcDefinitionJson> procJson = new();

                    foreach (DMProc proc in procs.Value) {
                        procJson.Add(proc.GetJsonRepresentation());
                    }

                    objectJson.Procs.Add(procs.Key, procJson);
                }
            }

            return objectJson;
        }

        public bool IsSubtypeOf(DreamPath path) {
            if (Path.IsDescendantOf(path)) return true;
            if (Parent != null) return Parent.IsSubtypeOf(path);
            return false;
        }
    }
}
