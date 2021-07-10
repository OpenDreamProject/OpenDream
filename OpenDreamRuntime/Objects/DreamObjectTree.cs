using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;
using System.Text.Json;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamRuntime.Objects {
    public class DreamObjectTree {
        public DreamRuntime Runtime { get; }

        public DreamObjectTree(DreamRuntime runtime) {
            Runtime = runtime;
            RootObject = new DreamObjectTreeEntry(Runtime, DreamPath.Root);
        }

        public class DreamObjectTreeEntry {
            public DreamObjectDefinition ObjectDefinition;
            public Dictionary<string, DreamObjectTreeEntry> Children = new();
            public DreamObjectTreeEntry ParentEntry = null;

            //Children that exist on another branch of the tree
            //Ex: /obj is a child of /atom/movable, but /obj's path isn't /atom/movable/obj
            public Dictionary<string, DreamObjectTreeEntry> BranchBreakingChildren = new();

            public DreamObjectTreeEntry(DreamRuntime runtime, DreamPath path) {
                ObjectDefinition = new DreamObjectDefinition(runtime, path);
            }

            public DreamObjectTreeEntry(DreamPath path, DreamObjectTreeEntry parentTreeEntry) {
                ObjectDefinition = new DreamObjectDefinition(path, parentTreeEntry.ObjectDefinition);
                ParentEntry = parentTreeEntry;
            }

            public List<DreamObjectTreeEntry> GetAllDescendants(bool includeBranchBreakingDescendants = false, bool inclusive = false) {
                List<DreamObjectTreeEntry> descendants = new List<DreamObjectTreeEntry>();

                if (inclusive) {
                    descendants.Add(this);
                }

                foreach (KeyValuePair<string, DreamObjectTreeEntry> child in Children) {
                    descendants.AddRange(child.Value.GetAllDescendants(includeBranchBreakingDescendants, true));
                }

                if (includeBranchBreakingDescendants) {
                    foreach (KeyValuePair<string, DreamObjectTreeEntry> child in BranchBreakingChildren) {
                        descendants.AddRange(child.Value.GetAllDescendants(includeBranchBreakingDescendants, true));
                    }
                }


                return descendants;
            }
        }

        public DreamObjectTreeEntry RootObject;

        public bool HasTreeEntry(DreamPath path) {
            if (path.Type != DreamPath.PathType.Absolute) return false;

            if (path.Equals(DreamPath.Root) && RootObject != null) return true;

            DreamObjectTreeEntry treeEntry = RootObject;
            foreach (string element in path.Elements) {
                if (!treeEntry.Children.TryGetValue(element, out treeEntry)) return false;
            }

            return true;
        }

        public DreamObjectTreeEntry GetTreeEntryFromPath(DreamPath path) {
            if (path.Type != DreamPath.PathType.Absolute) {
                throw new Exception("Path must be an absolute path");
            }

            if (path.Equals(DreamPath.Root)) return RootObject;

            DreamObjectTreeEntry treeEntry = RootObject;
            foreach (string element in path.Elements) {
                if (!treeEntry.Children.TryGetValue(element, out treeEntry)) {
                    throw new Exception("Object '" + path + "' does not exist");
                }
            }

            return treeEntry;
        }

        public DreamObjectDefinition GetObjectDefinitionFromPath(DreamPath path) {
            return GetTreeEntryFromPath(path).ObjectDefinition;
        }

        // It is the job of whatever calls this function to then initialize the object
        // by calling the result of DreamObject.InitProc or DreamObject.InitSpawn
        public DreamObject CreateObject(DreamPath path) {
            if (path.Equals(DreamPath.List)) {
                return DreamList.CreateUninitialized(Runtime);
            } else {
                return new DreamObject(Runtime, GetObjectDefinitionFromPath(path));
            }
        }

        public void SetMetaObject(DreamPath path, IDreamMetaObject metaObject) {
            List<DreamObjectTreeEntry> treeEntries = GetTreeEntryFromPath(path).GetAllDescendants(true, true);

            foreach (DreamObjectTreeEntry treeEntry in treeEntries) {
                treeEntry.ObjectDefinition.MetaObject = metaObject;
            }
        }

        public void LoadFromJson(DreamObjectJson rootJsonObject) {
            if (rootJsonObject.Name != "") {
                throw new Exception("Root object in json should have an empty name");
            }

            RootObject = new DreamObjectTreeEntry(Runtime, DreamPath.Root);
            LoadTreeEntryFromJson(RootObject, rootJsonObject);
        }

        private void LoadTreeEntryFromJson(DreamObjectTreeEntry treeEntry, DreamObjectJson jsonObject) {
            LoadVariablesFromJson(treeEntry.ObjectDefinition, jsonObject);

            if (jsonObject.InitProc != null) {
                var initProc = new DMProc($"{treeEntry.ObjectDefinition.Type}/(init)", Runtime, null, null, null, jsonObject.InitProc.Bytecode, true);

                initProc.SuperProc = treeEntry.ObjectDefinition.InitializionProc;
                treeEntry.ObjectDefinition.InitializionProc = initProc;
            }

            if (jsonObject.Procs != null) {
                LoadProcsFromJson(treeEntry.ObjectDefinition, jsonObject.Procs);
            }

            if (jsonObject.Children != null) {
                foreach (DreamObjectJson childJsonObject in jsonObject.Children) {
                    DreamObjectTreeEntry childObjectTreeEntry;
                    DreamPath childObjectPath = treeEntry.ObjectDefinition.Type.AddToPath(childJsonObject.Name);

                    if (childJsonObject.Parent != null) {
                        DreamObjectTreeEntry parentTreeEntry = GetTreeEntryFromPath(new DreamPath(childJsonObject.Parent));

                        childObjectTreeEntry = new DreamObjectTreeEntry(childObjectPath, parentTreeEntry);
                        parentTreeEntry.BranchBreakingChildren.Add(childJsonObject.Name, childObjectTreeEntry);
                    } else {
                        childObjectTreeEntry = new DreamObjectTreeEntry(childObjectPath, treeEntry);
                    }

                    LoadTreeEntryFromJson(childObjectTreeEntry, childJsonObject);
                    treeEntry.Children.Add(childJsonObject.Name, childObjectTreeEntry);
                }
            }
        }

        private DreamValue GetDreamValueFromJsonElement(object value) {
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
                                    DreamResource resource = Runtime.ResourceManager.LoadResource(resourcePathElement.GetString());

                                    return new DreamValue(resource);
                                }
                                case JsonValueKind.Null:
                                    return DreamValue.Null;
                                default:
                                    throw new Exception("Property 'resourcePath' must be a string or null");
                            }
                        }
                        case JsonVariableType.Path:
                            return new DreamValue(new DreamPath(jsonElement.GetProperty("value").GetString()));
                        default:
                            throw new Exception("Invalid variable type (" + variableType + ")");
                    }
                }
                default:
                    throw new Exception("Invalid value kind for dream value (" + jsonElement.ValueKind + ")");
            }
        }

        private void LoadVariablesFromJson(DreamObjectDefinition objectDefinition, DreamObjectJson jsonObject) {
            if (jsonObject.Variables != null) {
                foreach (KeyValuePair<string, object> jsonVariable in jsonObject.Variables) {
                    DreamValue value = GetDreamValueFromJsonElement(jsonVariable.Value);

                    objectDefinition.SetVariableDefinition(jsonVariable.Key, value);
                }
            }

            if (jsonObject.GlobalVariables != null) {
                foreach (KeyValuePair<string, object> jsonGlobalVariable in jsonObject.GlobalVariables) {
                    DreamValue value = GetDreamValueFromJsonElement(jsonGlobalVariable.Value);
                    DreamGlobalVariable globalVariable = new DreamGlobalVariable(value);

                    objectDefinition.GlobalVariables.Add(jsonGlobalVariable.Key, globalVariable);
                }
            }
        }

        private void LoadProcsFromJson(DreamObjectDefinition objectDefinition, Dictionary<string, List<ProcDefinitionJson>> jsonProcs) {
            foreach (KeyValuePair<string, List<ProcDefinitionJson>> jsonProc in jsonProcs) {
                string procName = jsonProc.Key;

                foreach (ProcDefinitionJson procDefinition in jsonProc.Value) {
                    byte[] bytecode = procDefinition.Bytecode ?? Array.Empty<byte>();
                    List<string> argumentNames = new();
                    List<DMValueType> argumentTypes = new();

                    if (procDefinition.Arguments != null) {
                        foreach (ProcArgumentJson argument in procDefinition.Arguments) {
                            argumentNames.Add(argument.Name);
                            argumentTypes.Add(argument.Type);
                        }
                    }

                    var proc = new DMProc($"{objectDefinition.Type}/{jsonProc.Key}", Runtime, null, argumentNames, argumentTypes, bytecode, procDefinition.WaitFor);
                    objectDefinition.SetProcDefinition(jsonProc.Key, proc);
                }
            }
        }
    }
}
