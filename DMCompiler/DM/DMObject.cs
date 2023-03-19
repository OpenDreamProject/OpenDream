using DMCompiler.DM.Visitors;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.DM {
    /// <remarks>
    /// This doesn't represent a particular, specific instance of an object, <br/>
    /// but rather stores the compile-time information necessary to describe a certain object definition, <br/>
    /// including its procs, vars, path, parent, etc.
    /// </remarks>
    class DMObject {
        public int Id;
        public DreamPath Path;
        public DMObject Parent;
        public Dictionary<string, List<int>> Procs = new();
        public Dictionary<string, DMVariable> Variables = new();
        /// <summary> It's OK if the override var is not literally the exact same object as what it overrides. </summary>
        public Dictionary<string, DMVariable> VariableOverrides = new();
        public Dictionary<string, int> GlobalVariables = new();
        /// <summary>A list of var and verb initializations implicitly done before the user's New() is called.</summary>
        public List<DMExpression> InitializationProcExpressions = new();
        public int? InitializationProc;

        public bool IsRoot => Path == DreamPath.Root;
        
        public List<DMASTObjectVarOverride>? danglingOverrides = null; // Overrides waiting for the LateVarDef event to happen

        private bool _isSubscribedToVarDef = false;
        [CanBeNull] private List<DMProc> _verbs;

        public DMObject(int id, DreamPath path, DMObject parent) {
            Id = id;
            Path = path;
            Parent = parent;
        }

        public void AddProc(string name, DMProc proc) {
            if (!Procs.ContainsKey(name)) Procs.Add(name, new List<int>(1));

            Procs[name].Add(proc.Id);
        }

        private void HandleLateVarDef(object sender, DMVariable varDefined)
        {
            DMObject maybeAncestor = (DMObject)sender;
            for(int i = 0; i < danglingOverrides.Count; ++i)
            {
                var varOverride = danglingOverrides[i];
                if (varOverride.VarName == varDefined.Name) // FINALLY we can do this
                {
                    if (IsSubtypeOf(maybeAncestor.Path)) // Resolves the ambiguous var override
                    {
                        // Thank god DMObjectBuilder is static, amirite?
                        DMObjectBuilder.OverrideVariableValue(this, ref varDefined, varOverride.Value); // I'd like to mark DMObjectBuilder as a friend class but that's not a thing in C# so
                        VariableOverrides[varDefined.Name] = varDefined;
                        danglingOverrides.RemoveAt(i);
                        break;
                    }
                }
            }
            if (danglingOverrides.Count == 0) // Unsubscribe if we're done doing this
            {
                Robust.Shared.Utility.DebugTools.Assert(danglingOverrides.Count == 0);
                DMObjectBuilder.VarDefined -= this.HandleLateVarDef;
                _isSubscribedToVarDef = false;
            }
        }

        public void WaitForLateVarDefinition(DMASTObjectVarOverride varOverride)
        {
            if (danglingOverrides is null)
                danglingOverrides = new List<DMASTObjectVarOverride>();
            for(int i = 0; i < danglingOverrides.Count; ++i)
            {
                var otherOverride = danglingOverrides[i];
                if(otherOverride.VarName == varOverride.VarName) // This looks like an override for ANOTHER override.
                {   // Meaning we're probably already subscribed or... something?
                    // Whatever. I guess we're the real override, now.
                    danglingOverrides[i] = varOverride;
                    // NOTE: This doesn't work quite right if DMObjectBuilder ever starts evaluating object definitions in a different order than how they appear in the source code.
                    return;
                }
            }
            danglingOverrides.Add(varOverride);
            if (_isSubscribedToVarDef == false)
            {
                DMObjectBuilder.VarDefined += this.HandleLateVarDef; // GOD I hope this works
                _isSubscribedToVarDef = true;
            }
        }

        ///<remarks>
        /// Note that this DOES NOT query our <see cref= "GlobalVariables" />. <br/>
        /// <see langword="TODO:"/> Make this (and other things) match the nomenclature of <see cref="HasLocalVariable"/>
        /// </remarks>
        public DMVariable GetVariable(string name) {
            if (Variables.TryGetValue(name, out DMVariable variable)) {
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

        public bool HasProcNoInheritence(string name) {
            return Procs.ContainsKey(name);
        }

        /// <summary>
        /// Slightly more nuanced than HasProc, makes sure that one of the DMProcs we have in this hierarchy is the original definition.
        /// </summary>
        /// <returns>True if we could find a definition, false if not.</returns>
        public bool HasProcDefined(string name) {
            if(Procs.TryGetValue(name, out var IDList)) {
                // You'd expect us to be able to just index into the first entry,
                // but, no, it can seriously be in {override, override, definition, override} order
                foreach (int ID in IDList) {
                    DMProc proc = DMObjectTree.AllProcs[ID];
                    if((proc.Attributes & ProcAttributes.IsOverride) != ProcAttributes.IsOverride) {
                        return true;
                    }
                }
            }
            return Parent?.HasProcDefined(name) ?? false;
        }

        [CanBeNull]
        public List<int> GetProcs(string name) {
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
                init.PushArguments(0);
                init.Call(DMReference.SuperProc);

                string lastSource = null;
                foreach (DMExpression expression in InitializationProcExpressions) {
                    try {
                        if (expression.Location.Line is int line) {
                            // Only emit DebugSource when source changes
                            if (expression.Location.SourceFile != lastSource) {
                                init.DebugSource(expression.Location.SourceFile);
                                lastSource = expression.Location.SourceFile;
                            }

                            init.DebugLine(line);
                        }

                        expression.EmitPushValue(this, init);
                    } catch (CompileErrorException e) {
                        DMCompiler.Emit(e.Error);
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
