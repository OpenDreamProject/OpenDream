using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System;
using System.Text;
using System.Collections.Generic;
using DMCompiler.Bytecode;
using OpenDreamShared.Compiler;

namespace DMCompiler.DM {
    /// <remarks>
    /// This doesn't represent a particular, specific instance of an object, <br/>
    /// but rather stores the compile-time information necessary to describe a certain object definition, <br/>
    /// including its procs, vars, path, parent, etc.
    /// </remarks>
    internal sealed class DMObject {
        public int Id;
        public DreamPath Path;
        public DMObject? Parent;
        public Dictionary<string, List<int>> Procs = new();
        public Dictionary<string, DMVariable> Variables = new();
        /// <summary> It's OK if the override var is not literally the exact same object as what it overrides. </summary>
        public Dictionary<string, DMVariable> VariableOverrides = new();
        public Dictionary<string, int> GlobalVariables = new();
        /// <summary>A list of var and verb initializations implicitly done before the user's New() is called.</summary>
        public HashSet<string> ConstVariables = new();
        public HashSet<string> TmpVariables = new();
        public List<DMExpression> InitializationProcExpressions = new();
        public int? InitializationProc;

        public bool IsRoot => Path == DreamPath.Root;

        private List<DMProc>? _verbs;

        public DMObject(int id, DreamPath path, DMObject? parent) {
            Id = id;
            Path = path;
            Parent = parent;
        }

        public void AddProc(string name, DMProc proc) {
            if (!Procs.ContainsKey(name)) Procs.Add(name, new List<int>(1));

            Procs[name].Add(proc.Id);
        }

        ///<remarks>
        /// Note that this DOES NOT query our <see cref= "GlobalVariables" />. <br/>
        /// <see langword="TODO:"/> Make this (and other things) match the nomenclature of <see cref="HasLocalVariable"/>
        /// </remarks>
        public DMVariable? GetVariable(string name) {
            if (Variables.TryGetValue(name, out var variable)) {
                return variable;
            }

            return Parent?.GetVariable(name);
        }

        /// <summary>
        /// Does a recursive search through self and parents to check if we already contain this variable, as a NON-STATIC VALUE!
        /// </summary>
        public bool HasLocalVariable(string name) {
            if (Variables.ContainsKey(name))
                return true;
            if (Parent == null)
                return false;
            return Parent.HasLocalVariable(name);
        }

        /// <summary> Similar to <see cref="HasLocalVariable"/>, just checks our globals/statics instead. </summary>
        /// <remarks> Does NOT return true if the global variable is in the root namespace, unless called on the Root object itself.</remarks>
        public bool HasGlobalVariable(string name) {
            if (IsRoot)
                return GlobalVariables.ContainsKey(name);
            return HasGlobalVariableNotInRoot(name);
        }

        private bool HasGlobalVariableNotInRoot(string name) {
            if (GlobalVariables.ContainsKey(name))
                return true;
            if (Parent == null || Parent.IsRoot)
                return false;
            return Parent.HasGlobalVariable(name);
        }

        public bool HasProc(string name) {
            if (Procs.ContainsKey(name)) return true;

            return Parent?.HasProc(name) ?? false;
        }

        public bool HasProcNoInheritance(string name) {
            return Procs.ContainsKey(name);
        }

        public List<int>? GetProcs(string name) {
            return Procs.GetValueOrDefault(name, Parent?.GetProcs(name) ?? null);
        }

        public void AddVerb(DMProc verb) {
            _verbs ??= new();
            _verbs.Add(verb);
        }

        public DMVariable CreateGlobalVariable(DreamPath? type, string name, bool isConst, DMValueType valType = DMValueType.Anything) {
            int id = DMObjectTree.CreateGlobal(out DMVariable global, type, name, isConst, valType);

            GlobalVariables[name] = id;
            return global;
        }

        /// <summary>
        /// Recursively searches for a global/static with the given name.
        /// </summary>
        /// <returns>Either the ID or null if no such global exists.</returns>
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
            if (InitializationProcExpressions.Count > 0 && InitializationProc == null) {
                var init = DMObjectTree.CreateDMProc(this, null);
                InitializationProc = init.Id;
                init.Call(DMReference.SuperProc, DMCallArgumentsType.None, 0);

                foreach (DMExpression expression in InitializationProcExpressions) {
                    try {
                        init.DebugSource(expression.Location);

                        expression.EmitPushValue(this, init);
                    } catch (CompileErrorException e) {
                        DMCompiler.Emit(e.Error);
                    }
                }
            }
        }

        public DreamTypeJson CreateJsonRepresentation(StringBuilder? stringBuilder) {
            DreamTypeJson typeJson = new DreamTypeJson();

            if (stringBuilder != null) {
                stringBuilder.AppendLine($"Type: {Path.PathString}");
                stringBuilder.AppendLine("\tParent: " + (Parent?.Path.PathString ?? "null"));
                stringBuilder.AppendLine("\tProcs:");
                foreach (var proc in Procs) stringBuilder.AppendLine($"\t\t{proc.Key}()");

                if (Variables.Count > 0) {
                    stringBuilder.AppendLine("\tVariables:");
                    foreach (var variable in Variables) {
                        var res = variable.Value.Type;
                        if (res == null) {
                            stringBuilder.AppendLine($"\t\t{variable.Key}: untyped");
                            continue;
                        }

                        stringBuilder.AppendLine($"\t\t{variable.Key}: {res.Value.PathString ?? "null"}");
                    }
                }

                if (VariableOverrides.Count > 0) {
                    stringBuilder.AppendLine("\tVariable Overrides:");
                    foreach (var variable in VariableOverrides) {
                        var res = variable.Value.Type;
                        if (res == null) {
                            stringBuilder.AppendLine($"\t\t{variable.Key}: untyped");
                            continue;
                        }

                        stringBuilder.AppendLine($"\t\t{variable.Key}: {res.Value.PathString ?? "null"}");
                    }
                }

                if (GlobalVariables.Count > 0) {
                    stringBuilder.AppendLine("\tGlobal Variables:");
                    foreach (var variable in GlobalVariables)
                        stringBuilder.AppendLine($"\t\t{variable.Key}: {variable.Value}");
                }

                if (ConstVariables.Count > 0) {
                    stringBuilder.AppendLine("\tConst Variables:");
                    foreach (var variable in ConstVariables) stringBuilder.AppendLine($"\t\t{variable}");
                }

                if (TmpVariables.Count > 0) {
                    stringBuilder.AppendLine("\tTmp Variables:");
                    foreach (var variable in TmpVariables) stringBuilder.AppendLine($"\t\t{variable}");
                }

                if (_verbs != null) {
                    stringBuilder.AppendLine("\tVerbs:");
                    foreach (var verb in _verbs) stringBuilder.AppendLine($"\t\t{verb.Name}");
                }

                stringBuilder.AppendLine();
            }

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

            if (ConstVariables.Count > 0) {
                typeJson.ConstVariables = ConstVariables;
            }

            if (TmpVariables.Count > 0) {
                typeJson.TmpVariables = TmpVariables;
            }

            if (InitializationProc != null) {
                typeJson.InitProc = InitializationProc;
            }

            if (Procs.Count > 0) {
                typeJson.Procs = new List<List<int>>(Procs.Values);
            }

            if (_verbs != null) {
                typeJson.Verbs = new List<int>(_verbs.Count);

                foreach (var verb in _verbs) {
                    typeJson.Verbs.Add(verb.Id);
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
