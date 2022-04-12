using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using JetBrains.Annotations;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Json;
using Robust.Shared.IoC;

namespace OpenDreamRuntime.Objects {
    public sealed class DreamObjectTree {
        public sealed class TreeEntry {
            public DreamPath Path;
            public DreamObjectDefinition ObjectDefinition;
            public TreeEntry? ParentEntry;
            public List<int> InheritingTypes = new();

            public TreeEntry(DreamPath path) {
                Path = path;
            }
        }

        public TreeEntry[] Types;
        public List<string> Strings; //TODO: Store this somewhere else

        private Dictionary<DreamPath, TreeEntry> _pathToType = new();

        public DreamObjectTree(DreamCompiledJson json) {
            Strings = json.Strings;

            LoadTypesFromJson(json.Types);
        }

        public bool HasTreeEntry(DreamPath path) {
            return _pathToType.ContainsKey(path);
        }

        public TreeEntry GetTreeEntry(DreamPath path) {
            if (!_pathToType.TryGetValue(path, out TreeEntry type)) {
                throw new Exception($"Object '{path}' does not exist");
            }

            return type;
        }

        public TreeEntry GetTreeEntry(int typeId) {
            return Types[typeId];
        }

        public DreamObjectDefinition GetObjectDefinition(DreamPath path) {
            return GetTreeEntry(path).ObjectDefinition;
        }

        public DreamObjectDefinition GetObjectDefinition(int typeId) {
            return GetTreeEntry(typeId).ObjectDefinition;
        }

        /// <summary>
        /// BFS traversal of all a given DreamPath's descendants
        /// </summary>
        /// <param name="path">Given path</param>
        /// <returns>IEnumerable of tree entries</returns>
        public IEnumerable<TreeEntry> GetAllDescendants(DreamPath path) {
            TreeEntry treeEntry = GetTreeEntry(path);

            yield return treeEntry;

            foreach (int typeId in treeEntry.InheritingTypes) {
                TreeEntry type = Types[typeId];
                IEnumerator<TreeEntry> typeChildren = GetAllDescendants(type.Path).GetEnumerator();

                while (typeChildren.MoveNext()) yield return typeChildren.Current;
            }
        }

        /// <summary>
        /// Returns a list of the TreeEntry's Types sorted in DFS traversal order
        /// </summary>
        /// <returns>IEnumerable of sorted TreeEntries</returns>
        /// <seealso cref="DreamObjectDefinition.IsSubtypeOf">IsDescendantOf</seealso>
        public IEnumerable<TreeEntry> GetDFSSortedTypeList() {
            List<TreeEntry> dfs_sorted_types = new();
            Stack<TreeEntry> dfs_sort_stack = new();

            dfs_sort_stack.Push(Types[0]); // Always the root ('/')
            while (dfs_sort_stack.Count != 0) {
                TreeEntry current = dfs_sort_stack.Pop();
                dfs_sorted_types.Add(current);

                // Because stacks pop the elements out LIFO, push in reverse
                // to traverse the returned list in the same order as was returned to us
                List<int> rev_children = new(current.InheritingTypes);
                rev_children.Reverse();

                foreach (int childTypeId in current.InheritingTypes) {
                    dfs_sort_stack.Push(Types[childTypeId]);
                }
            }
            return dfs_sorted_types;
        }

        // It is the job of whatever calls this function to then initialize the object
        // by calling the result of DreamObject.InitProc or DreamObject.InitSpawn
        public DreamObject CreateObject(DreamPath path) {
            if (path.Equals(DreamPath.List)) {
                return DreamList.CreateUninitialized();
            } else {
                return new DreamObject(GetObjectDefinition(path));
            }
        }

        public void SetMetaObject(DreamPath path, IDreamMetaObject metaObject) {
            foreach (TreeEntry treeEntry in GetAllDescendants(path)) {
                treeEntry.ObjectDefinition.MetaObject = metaObject;
            }
        }

        public DreamValue GetDreamValueFromJsonElement(object value) {
            if (value == null) return DreamValue.Null;

            JsonElement jsonElement = (JsonElement)value;
            switch (jsonElement.ValueKind) {
                case JsonValueKind.String:
                    return new DreamValue(jsonElement.GetString());
                case JsonValueKind.Number:
                    return new DreamValue(jsonElement.GetSingle());
                case JsonValueKind.Object: {
                    JsonVariableType variableType = (JsonVariableType)jsonElement.GetProperty("type").GetByte();

                    switch (variableType) {
                        case JsonVariableType.Resource: {
                            JsonElement resourcePathElement = jsonElement.GetProperty("resourcePath");

                            switch (resourcePathElement.ValueKind) {
                                case JsonValueKind.String: {
                                    var resM = IoCManager.Resolve<DreamResourceManager>();
                                    DreamResource resource = resM.LoadResource(resourcePathElement.GetString());

                                    return new DreamValue(resource);
                                }
                                case JsonValueKind.Null:
                                    return DreamValue.Null;
                                default:
                                    throw new Exception("Property 'resourcePath' must be a string or null");
                            }
                        }
                        case JsonVariableType.Path:
                            JsonElement pathValue = jsonElement.GetProperty("value");

                            switch (pathValue.ValueKind) {
                                case JsonValueKind.Number: return new DreamValue(Types[pathValue.GetInt32()].Path);
                                case JsonValueKind.String: return new DreamValue(new DreamPath(pathValue.GetString()));
                                default: throw new Exception("Invalid path value");
                            }
                        case JsonVariableType.List:
                            DreamList list = DreamList.Create();

                            if (jsonElement.TryGetProperty("values", out JsonElement values)) {
                                foreach (JsonElement listValue in values.EnumerateArray()) {
                                    list.AddValue(GetDreamValueFromJsonElement(listValue));
                                }
                            }

                            if (jsonElement.TryGetProperty("associatedValues", out JsonElement associatedValues)) {
                                foreach (JsonProperty associatedValue in associatedValues.EnumerateObject()) {
                                    DreamValue key = new DreamValue(associatedValue.Name);

                                    list.SetValue(key, GetDreamValueFromJsonElement(associatedValue.Value));
                                }
                            }

                            return new DreamValue(list);
                        default:
                            throw new Exception("Invalid variable type (" + variableType + ")");
                    }
                }
                default:
                    throw new Exception("Invalid value kind for dream value (" + jsonElement.ValueKind + ")");
            }
        }

        private void LoadTypesFromJson(DreamTypeJson[] types) {
            Dictionary<DreamPath, int> pathToTypeId = new();
            Types = new TreeEntry[types.Length];

            //First pass: Create types and set them up for initialization
            for (int i = 0; i < Types.Length; i++) {
                DreamPath path = new DreamPath(types[i].Path);

                Types[i] = new TreeEntry(path);
                _pathToType[path] = Types[i];
                pathToTypeId[path] = i;
            }

            //Second pass: Set each type's parent and children
            for (int i = 0; i < Types.Length; i++) {
                DreamTypeJson jsonType = types[i];
                TreeEntry type = Types[i];

                if (jsonType.Parent != null) {
                    TreeEntry parent = Types[jsonType.Parent.Value];

                    parent.InheritingTypes.Add(i);
                    type.ParentEntry = parent;
                }
            }

            //Third pass: Create a DFS list of types so that all children of a parent type have contiguous indices,
            //so we can type-check by just checking in a range. See: DreamPath.IsDescendantOf
            IEnumerable<TreeEntry> dfs_sorted_types = GetDFSSortedTypeList();
            uint class_num = 0;
            foreach (TreeEntry type in dfs_sorted_types) {
                if(type.ObjectDefinition == null) continue;
                type.ObjectDefinition.typeIndex = class_num++;
                if (type.ParentEntry != null)
                    type.ParentEntry.ObjectDefinition.numChildren++;
                type.ObjectDefinition.numChildren = (uint)type.InheritingTypes.Count;
            }

            //Fourth pass: Load each type's vars and procs
            //This must happen top-down from the root of the object tree for inheritance to work
            //Thus, the enumeration of GetAllDescendants()
            foreach (TreeEntry type in GetAllDescendants(DreamPath.Root)) {
                int typeId = pathToTypeId[type.Path];
                DreamTypeJson jsonType = types[typeId];

                DreamObjectDefinition definition;
                if (type.ParentEntry != null) {
                    definition = new DreamObjectDefinition(type.Path, type.ParentEntry.ObjectDefinition);
                } else {
                    definition = new DreamObjectDefinition(type.Path);
                }
                type.ObjectDefinition = definition;

                LoadVariablesFromJson(definition, jsonType);
                if (jsonType.Procs != null) {
                    LoadProcsFromJson(definition, jsonType.Procs);
                }

                if (jsonType.InitProc != null) {
                    var initProc = new DMProc($"{type.Path}/(init)", null, null, null, jsonType.InitProc.Bytecode, jsonType.InitProc.MaxStackSize, jsonType.InitProc.Attributes, jsonType.InitProc.VerbName, jsonType.InitProc.VerbCategory, jsonType.InitProc.VerbDesc, jsonType.InitProc.Invisibility);

                    initProc.SuperProc = definition.InitializionProc;
                    definition.InitializionProc = initProc;
                }
            }

            //Fifth pass: Set atom's text
            foreach (TreeEntry type in GetAllDescendants(DreamPath.Atom))
            {
                if (type.ObjectDefinition.Variables["text"].Equals(DreamValue.Null) && type.ObjectDefinition.Variables["name"].TryGetValueAsString(out var name))
                {
                    type.ObjectDefinition.SetVariableDefinition("text", new DreamValue(String.IsNullOrEmpty(name) ? String.Empty : name[..1]));
                }
            }
        }

        private void LoadVariablesFromJson(DreamObjectDefinition objectDefinition, DreamTypeJson jsonObject) {
            if (jsonObject.Variables != null) {
                foreach (KeyValuePair<string, object> jsonVariable in jsonObject.Variables) {
                    DreamValue value = GetDreamValueFromJsonElement(jsonVariable.Value);

                    objectDefinition.SetVariableDefinition(jsonVariable.Key, value);
                }
            }

            if (jsonObject.GlobalVariables != null) {
                foreach (KeyValuePair<string, int> jsonGlobalVariable in jsonObject.GlobalVariables) {
                    objectDefinition.GlobalVariables.Add(jsonGlobalVariable.Key, jsonGlobalVariable.Value);
                }
            }
        }

        public DreamProc LoadProcJson(string procName, ProcDefinitionJson procDefinition) {
            byte[] bytecode = procDefinition.Bytecode ?? Array.Empty<byte>();
            List<string> argumentNames = new();
            List<DMValueType> argumentTypes = new();

            if (procDefinition.Arguments != null) {
                argumentNames.EnsureCapacity(procDefinition.Arguments.Count);
                argumentTypes.EnsureCapacity(procDefinition.Arguments.Count);

                foreach (ProcArgumentJson argument in procDefinition.Arguments) {
                    argumentNames.Add(argument.Name);
                    argumentTypes.Add(argument.Type);
                }
            }

            return new DMProc(procName, null, argumentNames, argumentTypes, bytecode, procDefinition.MaxStackSize, procDefinition.Attributes, procDefinition.VerbName, procDefinition.VerbCategory, procDefinition.VerbDesc, procDefinition.Invisibility);
        }

        private void LoadProcsFromJson(DreamObjectDefinition objectDefinition, Dictionary<string, List<ProcDefinitionJson>> jsonProcs) {
            foreach (KeyValuePair<string, List<ProcDefinitionJson>> jsonProc in jsonProcs) {
                string procName = $"{objectDefinition.Type}/{jsonProc.Key}";

                foreach (ProcDefinitionJson procDefinition in jsonProc.Value) {
                    objectDefinition.SetProcDefinition(jsonProc.Key, LoadProcJson(procName, procDefinition));
                }
            }
        }
    }
}
