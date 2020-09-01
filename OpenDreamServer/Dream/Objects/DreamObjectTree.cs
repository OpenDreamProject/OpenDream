using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamServer.Dream.Procs;
using OpenDreamServer.Resources;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenDreamServer.Dream.Objects {
    class DreamObjectTree {
        public class DreamObjectTreeEntry {
            public DreamObjectDefinition ObjectDefinition;
            public Dictionary<string, DreamObjectTreeEntry> Children = new Dictionary<string, DreamObjectTreeEntry>();
            public DreamObjectTreeEntry ParentEntry = null;

            //Children that exist on another branch of the tree
            //Ex: /movable is a parent of /obj, but /obj's path isn't /movable/obj
            public Dictionary<string, DreamObjectTreeEntry> BranchBreakingChildren = new Dictionary<string, DreamObjectTreeEntry>();

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

        private enum DreamObjectTreeVariableType {
            Resource = 0,
            Object = 1,
            Path = 2
        }

        private class DreamObjectTreeJsonObject {
            public string Name { get; set; }
            public string Parent { get; set; }
            public Dictionary<string, JsonElement> Variables { get; set; }
            public Dictionary<string, JsonElement> GlobalVariables { get; set; }
            public List<DreamObjectTreeJsonObject> Children { get; set; }
            public Dictionary<string, List<DreamObjectTreeJsonObjectProcDefinition>> Procs { get; set; }
        }

        private class DreamObjectTreeJsonObjectProcDefinition {
            public List<string> ArgumentNames { get; set; }
            public Dictionary<string, JsonElement> DefaultArgumentValues { get; set; }
            public byte[] Bytecode { get; set; }
            public string NativeProcName { get; set; }
        }

        public DreamObjectTreeEntry RootObject = new DreamObjectTreeEntry(DreamPath.Root);

        private Dictionary<string, DreamProc> _nativeProcs = new Dictionary<string, DreamProc>();
        private List<(DreamGlobalVariable, DreamPath, DreamProcArguments)> _runtimeInstantiatedGlobalVariables = new List<(DreamGlobalVariable, DreamPath, DreamProcArguments)>();

        public void RegisterNativeProc(string nativeProcName, DreamProc nativeProc) {
            _nativeProcs.Add(nativeProcName, nativeProc);
        }

        public bool HasTreeEntry(DreamPath path) {
            if (path.Type != DreamPath.PathType.Absolute) {
                throw new Exception("Path must be an absolute path");
            }

            if (path.Equals(DreamPath.Root) && RootObject != null) return true;

            DreamObjectTreeEntry treeEntry = RootObject;
            for (int i = 0; i < path.Elements.Length; i++) {
                string element = path.Elements[i];

                if (treeEntry.Children.ContainsKey(element)) {
                    treeEntry = treeEntry.Children[element];
                } else {
                    return false;
                }
            }

            return true;
        }

        public DreamObjectTreeEntry GetTreeEntryFromPath(DreamPath path) {
            if (path.Type != DreamPath.PathType.Absolute) {
                throw new Exception("Path must be an absolute path");
            }

            if (path.Equals(DreamPath.Root)) return RootObject;

            DreamObjectTreeEntry treeEntry = RootObject;
            for (int i = 0; i < path.Elements.Length; i++) {
                string element = path.Elements[i];

                if (treeEntry.Children.ContainsKey(element)) {
                    treeEntry = treeEntry.Children[element];
                } else {
                    throw new Exception("Object '" + path + "' does not exist");
                }
            }

            return treeEntry;
        }

        public DreamObjectDefinition GetObjectDefinitionFromPath(DreamPath path) {
            return GetTreeEntryFromPath(path).ObjectDefinition;
        }

        public DreamObject CreateObject(DreamPath path, DreamProcArguments creationArguments) {
            return new DreamObject(GetObjectDefinitionFromPath(path), creationArguments);
        }

        public DreamObject CreateObject(DreamPath path) {
            return CreateObject(path, new DreamProcArguments(null));
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

        public void LoadFromJson(string json) {
            DreamObjectTreeJsonObject rootJsonObject = JsonSerializer.Deserialize<DreamObjectTreeJsonObject>(json);

            if (rootJsonObject.Name != "") {
                throw new Exception("Root object in json should have an empty name");
            }

            RootObject = new DreamObjectTreeEntry(DreamPath.Root);
            LoadTreeEntryFromJson(RootObject, rootJsonObject);
        }

        private void LoadTreeEntryFromJson(DreamObjectTreeEntry treeEntry, DreamObjectTreeJsonObject jsonObject) {
            LoadVariablesFromJson(treeEntry.ObjectDefinition, jsonObject);

            if (jsonObject.Procs != null) {
                LoadProcsFromJson(treeEntry.ObjectDefinition, jsonObject.Procs);
            }
            
            if (jsonObject.Children != null) {
                foreach (DreamObjectTreeJsonObject childJsonObject in jsonObject.Children) {
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
                    return new DreamValue(jsonElement.GetDouble());
                } else {
                    Int32 value = 0x7FFFFFFF;

                    jsonElement.TryGetInt32(out value);
                    return new DreamValue(value);
                }
            } else if (jsonElement.ValueKind == JsonValueKind.Object) {
                DreamObjectTreeVariableType variableType = (DreamObjectTreeVariableType)jsonElement.GetProperty("type").GetByte();

                if (variableType == DreamObjectTreeVariableType.Resource) {
                    JsonElement resourcePathElement = jsonElement.GetProperty("resourcePath");

                    if (resourcePathElement.ValueKind == JsonValueKind.String) {
                        DreamResource resource = Program.DreamResourceManager.LoadResource(resourcePathElement.GetString());

                        return new DreamValue(resource);
                    } else if (resourcePathElement.ValueKind == JsonValueKind.Null) {
                        return new DreamValue((DreamObject)null);
                    } else {
                        throw new Exception("Property 'resourcePath' must be a string or null");
                    }
                } else if (variableType == DreamObjectTreeVariableType.Object) {
                    return new DreamValue((DreamObject)null);
                } else if (variableType == DreamObjectTreeVariableType.Path) {
                    return new DreamValue(new DreamPath(jsonElement.GetProperty("value").GetString()));
                } else {
                    throw new Exception("Invalid variable type (" + variableType + ")");
                }
            } else {
                throw new Exception("Invalid value kind for dream value (" + jsonElement.ValueKind + ")");
            }
        }

        private void LoadVariablesFromJson(DreamObjectDefinition objectDefinition, DreamObjectTreeJsonObject jsonObject) {
            if (jsonObject.Variables != null) {
                foreach (KeyValuePair<string, JsonElement> jsonVariable in jsonObject.Variables) {
                    JsonElement jsonElement = jsonVariable.Value;
                    DreamValue value = GetDreamValueFromJsonElement(jsonElement);

                    objectDefinition.SetVariableDefinition(jsonVariable.Key, value);

                    if (jsonElement.ValueKind == JsonValueKind.Object) {
                        DreamObjectTreeVariableType variableType = (DreamObjectTreeVariableType)jsonElement.GetProperty("type").GetByte();

                        if (variableType == DreamObjectTreeVariableType.Object) {
                            JsonElement objectPath;

                            if (jsonElement.TryGetProperty("path", out objectPath)) {
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
                        }
                    }
                }
            }

            if (jsonObject.GlobalVariables != null) {
                foreach (KeyValuePair<string, JsonElement> jsonGlobalVariable in jsonObject.GlobalVariables) {
                    JsonElement jsonElement = jsonGlobalVariable.Value;
                    DreamValue value = GetDreamValueFromJsonElement(jsonElement);
                    DreamGlobalVariable globalVariable = new DreamGlobalVariable(value);

                    objectDefinition.GlobalVariables.Add(jsonGlobalVariable.Key, globalVariable);

                    if (jsonElement.ValueKind == JsonValueKind.Object) {
                        DreamObjectTreeVariableType variableType = (DreamObjectTreeVariableType)jsonElement.GetProperty("type").GetByte();

                        if (variableType == DreamObjectTreeVariableType.Object) {
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

        private void LoadProcsFromJson(DreamObjectDefinition objectDefinition, Dictionary<string, List<DreamObjectTreeJsonObjectProcDefinition>> jsonProcs) {
            foreach (KeyValuePair<string, List<DreamObjectTreeJsonObjectProcDefinition>> jsonProc in jsonProcs) {
                string procName = jsonProc.Key;

                foreach (DreamObjectTreeJsonObjectProcDefinition procDefinition in jsonProc.Value) {
                    if (procDefinition.NativeProcName != null) {
                        objectDefinition.SetProcDefinition(jsonProc.Key, _nativeProcs[procDefinition.NativeProcName]);
                    } else {
                        List<string> argumentNames = (procDefinition.ArgumentNames != null) ? procDefinition.ArgumentNames : new List<string>();
                        Dictionary<string, DreamValue> defaultArgumentValues = null;
                        byte[] bytecode = procDefinition.Bytecode != null ? procDefinition.Bytecode : new byte[0];

                        if (procDefinition.DefaultArgumentValues != null) {
                            defaultArgumentValues = new Dictionary<string, DreamValue>();

                            foreach (KeyValuePair<string, JsonElement> defaultArgumentValue in procDefinition.DefaultArgumentValues) {
                                defaultArgumentValues[defaultArgumentValue.Key] = GetDreamValueFromJsonElement(defaultArgumentValue.Value);
                            }
                        }

                        objectDefinition.SetProcDefinition(jsonProc.Key, new DreamProc(bytecode, argumentNames, defaultArgumentValues));
                    }
                }
            }
        }
    }
}
