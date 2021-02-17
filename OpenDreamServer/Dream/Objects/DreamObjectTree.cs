using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace OpenDreamServer.Dream.Objects {
    class DreamObjectTree {
        public class DreamObjectTreeEntry {
            public DreamObjectDefinition ObjectDefinition;
            public Dictionary<string, DreamObjectTreeEntry> Children = new();
            public DreamObjectTreeEntry ParentEntry = null;

            //Children that exist on another branch of the tree
            //Ex: /obj is a child of /atom/movable, but /obj's path isn't /atom/movable/obj
            public Dictionary<string, DreamObjectTreeEntry> BranchBreakingChildren = new();

            public DreamObjectTreeEntry(DreamPath path) {
                ObjectDefinition = new DreamObjectDefinition(path);
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

        public DreamObjectTreeEntry RootObject = new DreamObjectTreeEntry(DreamPath.Root);

        private List<(DreamGlobalVariable, DreamPath, DreamProcArguments)> _runtimeInstantiatedGlobalVariables = new List<(DreamGlobalVariable, DreamPath, DreamProcArguments)>();

        public bool HasTreeEntry(DreamPath path) {
            if (path.Type != DreamPath.PathType.Absolute) {
                throw new Exception("Path must be an absolute path");
            }

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

        public DreamObject CreateObject(DreamPath path, DreamProcArguments creationArguments) {
            if (path.Equals(DreamPath.List)) {
                return new DreamList(creationArguments);
            } else {
                return new DreamObject(GetObjectDefinitionFromPath(path), creationArguments);
            }
        }

        public DreamObject CreateObject(DreamPath path) {
            return CreateObject(path, new DreamProcArguments(null));
        }

        public DreamList CreateList() {
            return (DreamList)CreateObject(DreamPath.List);
        }
        
        public void SetMetaObject(DreamPath path, IDreamMetaObject metaObject) {
            List<DreamObjectTreeEntry> treeEntries = GetTreeEntryFromPath(path).GetAllDescendants(true, true);

            foreach (DreamObjectTreeEntry treeEntry in treeEntries) {
                treeEntry.ObjectDefinition.MetaObject = metaObject;
            }
        }

        public void InstantiateGlobalVariables() {
            foreach ((DreamGlobalVariable, DreamPath, DreamProcArguments) runtimeInstantiatedGlobalVariable in _runtimeInstantiatedGlobalVariables) {
                DreamObject instantiatedObject = CreateObject(runtimeInstantiatedGlobalVariable.Item2, runtimeInstantiatedGlobalVariable.Item3);

                runtimeInstantiatedGlobalVariable.Item1.Value = new DreamValue(instantiatedObject);
            }
        }

        public void LoadFromJson(DreamObjectJson rootJsonObject) {
            if (rootJsonObject.Name != "") {
                throw new Exception("Root object in json should have an empty name");
            }

            RootObject = new DreamObjectTreeEntry(DreamPath.Root);
            LoadTreeEntryFromJson(RootObject, rootJsonObject);
        }

        private void LoadTreeEntryFromJson(DreamObjectTreeEntry treeEntry, DreamObjectJson jsonObject) {
            LoadVariablesFromJson(treeEntry.ObjectDefinition, jsonObject);

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

        private DreamValue GetDreamValueFromJsonElement(JsonElement jsonElement) {
            if (jsonElement.ValueKind == JsonValueKind.String) {
                return new DreamValue(jsonElement.GetString());
            } else if (jsonElement.ValueKind == JsonValueKind.Number) {
                if (jsonElement.GetRawText().Contains(".")) {
                    return new DreamValue(jsonElement.GetSingle());
                } else {
                    int value;
                    if (!jsonElement.TryGetInt32(out value)) value = Int32.MaxValue;

                    return new DreamValue(value);
                }
            } else if (jsonElement.ValueKind == JsonValueKind.Object) {
                JsonVariableType variableType = (JsonVariableType)jsonElement.GetProperty("type").GetByte();

                if (variableType == JsonVariableType.Resource) {
                    JsonElement resourcePathElement = jsonElement.GetProperty("resourcePath");

                    if (resourcePathElement.ValueKind == JsonValueKind.String) {
                        DreamResource resource = Program.DreamResourceManager.LoadResource(resourcePathElement.GetString());

                        return new DreamValue(resource);
                    } else if (resourcePathElement.ValueKind == JsonValueKind.Null) {
                        return new DreamValue((DreamObject)null);
                    } else {
                        throw new Exception("Property 'resourcePath' must be a string or null");
                    }
                } else if (variableType == JsonVariableType.Object) {
                    return new DreamValue((DreamObject)null);
                } else if (variableType == JsonVariableType.Path) {
                    return new DreamValue(new DreamPath(jsonElement.GetProperty("value").GetString()));
                } else if (variableType == JsonVariableType.List) {
                    return new DreamValue((DreamObject)null);
                } else {
                    throw new Exception("Invalid variable type (" + variableType + ")");
                }
            } else {
                throw new Exception("Invalid value kind for dream value (" + jsonElement.ValueKind + ")");
            }
        }

        private void LoadVariablesFromJson(DreamObjectDefinition objectDefinition, DreamObjectJson jsonObject) {
            if (jsonObject.Variables != null) {
                foreach (KeyValuePair<string, object> jsonVariable in jsonObject.Variables) {
                    JsonElement jsonElement = (JsonElement)jsonVariable.Value;
                    DreamValue value = GetDreamValueFromJsonElement(jsonElement);

                    objectDefinition.SetVariableDefinition(jsonVariable.Key, value);

                    if (jsonElement.ValueKind == JsonValueKind.Object) {
                        JsonVariableType variableType = (JsonVariableType)jsonElement.GetProperty("type").GetByte();

                        if (variableType == JsonVariableType.Object) {
                            if (jsonElement.TryGetProperty("path", out JsonElement objectPath)) {
                                DreamPath path = new DreamPath(objectPath.GetString());
                                DreamProcArguments creationArguments = new DreamProcArguments(new List<DreamValue>(), new Dictionary<string, DreamValue>());
                                JsonElement arguments;

                                if (jsonElement.TryGetProperty("arguments", out arguments)) {
                                    foreach (JsonElement jsonCreationArgument in arguments.EnumerateArray()) {
                                        creationArguments.OrderedArguments.Add(GetDreamValueFromJsonElement(jsonCreationArgument));
                                    }
                                }

                                if (jsonElement.TryGetProperty("namedArguments", out arguments)) {
                                    foreach (JsonProperty jsonCreationArgument in arguments.EnumerateObject()) {
                                        creationArguments.NamedArguments.Add(jsonCreationArgument.Name, GetDreamValueFromJsonElement(jsonCreationArgument.Value));
                                    }
                                }

                                objectDefinition.RuntimeInstantiatedVariables[jsonVariable.Key] = (path, creationArguments);
                            }
                        } else if (variableType == JsonVariableType.List) {
                            List<(DreamValue Index, DreamValue Value)> listValues = new List<(DreamValue, DreamValue)>();

                            if (jsonElement.TryGetProperty("values", out JsonElement values)) {
                                for (int i = 0; i < values.GetArrayLength(); i++) {
                                    JsonElement jsonListValue = values[i];
                                    JsonElement jsonValue = jsonListValue.GetProperty("value");
                                    DreamValue listValue = GetDreamValueFromJsonElement(jsonValue);

                                    if (jsonListValue.TryGetProperty("key", out JsonElement jsonKey)) {
                                        listValues.Add((GetDreamValueFromJsonElement(jsonKey), listValue));
                                    } else {
                                        listValues.Add((new DreamValue((DreamObject)null), listValue));
                                    }
                                }
                            }

                            objectDefinition.RuntimeInstantiatedLists.Add((jsonVariable.Key, listValues));
                        }
                    }
                }
            }

            if (jsonObject.GlobalVariables != null) {
                foreach (KeyValuePair<string, object> jsonGlobalVariable in jsonObject.GlobalVariables) {
                    JsonElement jsonElement = (JsonElement)jsonGlobalVariable.Value;
                    DreamValue value = GetDreamValueFromJsonElement(jsonElement);
                    DreamGlobalVariable globalVariable = new DreamGlobalVariable(value);

                    objectDefinition.GlobalVariables.Add(jsonGlobalVariable.Key, globalVariable);

                    if (jsonElement.ValueKind == JsonValueKind.Object) {
                        JsonVariableType variableType = (JsonVariableType)jsonElement.GetProperty("type").GetByte();

                        if (variableType == JsonVariableType.Object) {
                            JsonElement objectPath;

                            if (jsonElement.TryGetProperty("path", out objectPath)) {
                                DreamPath path = new DreamPath(objectPath.GetString());
                                DreamProcArguments creationArguments = new DreamProcArguments(new List<DreamValue>());

                                _runtimeInstantiatedGlobalVariables.Add((globalVariable, path, creationArguments));
                            }
                        }
                    }
                }
            }
        }

        private void LoadProcsFromJson(DreamObjectDefinition objectDefinition, Dictionary<string, List<ProcDefinitionJson>> jsonProcs) {
            foreach (KeyValuePair<string, List<ProcDefinitionJson>> jsonProc in jsonProcs) {
                string procName = jsonProc.Key;

                foreach (ProcDefinitionJson procDefinition in jsonProc.Value) {
                    List<string> argumentNames = (procDefinition.ArgumentNames != null) ? procDefinition.ArgumentNames : new List<string>();
                    byte[] bytecode = procDefinition.Bytecode != null ? procDefinition.Bytecode : new byte[0];

                    objectDefinition.SetProcDefinition(jsonProc.Key, new DreamProc(bytecode, argumentNames));
                }
            }
        }
    }
}
