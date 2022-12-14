using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Json;
using TreeEntry = OpenDreamRuntime.Objects.IDreamObjectTree.TreeEntry;

namespace OpenDreamRuntime.Objects {
    public sealed class DreamObjectTree : IDreamObjectTree {
        public TreeEntry[] Types { get; private set; }
        public List<DreamProc> Procs { get; private set; }
        public List<string> Strings { get; private set; } //TODO: Store this somewhere else

        public TreeEntry Root { get; private set; }
        public TreeEntry World { get; private set; }
        public TreeEntry Client { get; private set; }
        public TreeEntry Datum { get; private set; }
        public TreeEntry Sound { get; private set; }
        public TreeEntry Matrix { get; private set; }
        public TreeEntry Exception { get; private set; }
        public TreeEntry Savefile { get; private set; }
        public TreeEntry Regex { get; private set; }
        public TreeEntry Filter { get; private set; }
        public TreeEntry Icon { get; private set; }
        public TreeEntry Image { get; private set; }
        public TreeEntry MutableAppearance { get; private set; }
        public TreeEntry Atom { get; private set; }
        public TreeEntry Area { get; private set; }
        public TreeEntry Turf { get; private set; }
        public TreeEntry Movable { get; private set; }
        public TreeEntry Obj { get; private set; }
        public TreeEntry Mob { get; private set; }

        private Dictionary<DreamPath, TreeEntry> _pathToType = new();
        private Dictionary<string, int> _globalProcIds;

        public void LoadJson(DreamCompiledJson json) {
            Strings = json.Strings;

            // Load procs first so types can set their init proc's super proc
            LoadProcsFromJson(json.Types, json.Procs, json.GlobalProcs);
            LoadTypesFromJson(json.Types);
        }

        public bool HasTreeEntry(DreamPath path) {
            return _pathToType.ContainsKey(path);
        }

        public TreeEntry GetTreeEntry(DreamPath path) {
            if (!_pathToType.TryGetValue(path, out TreeEntry? type)) {
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

        public bool TryGetGlobalProc(string name, [NotNullWhen(true)] out DreamProc? globalProc) {
            globalProc = _globalProcIds.TryGetValue(name, out int procId) ? Procs[procId] : null;

            return (globalProc != null);
        }

        public IEnumerable<TreeEntry> GetAllDescendants(DreamPath path) {
            TreeEntry treeEntry = GetTreeEntry(path);

            yield return treeEntry;

            foreach (int typeId in treeEntry.InheritingTypes) {
                TreeEntry type = Types[typeId];
                IEnumerator<TreeEntry> typeChildren = GetAllDescendants(type.Path).GetEnumerator();

                while (typeChildren.MoveNext()) yield return typeChildren.Current;
            }
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
            // TODO: Setting meta objects outside of their order of inheritance can break things.
            metaObject.ParentType = GetTreeEntry(path).ParentEntry.ObjectDefinition.MetaObject;

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
                var type = new TreeEntry(path, i);

                Types[i] = type;
                _pathToType[path] = type;
                pathToTypeId[path] = i;
            }

            Root = GetTreeEntry(DreamPath.Root);
            World = GetTreeEntry(DreamPath.World);
            Client = GetTreeEntry(DreamPath.Client);
            Datum = GetTreeEntry(DreamPath.Datum);
            Sound = GetTreeEntry(DreamPath.Sound);
            Matrix = GetTreeEntry(DreamPath.Matrix);
            Exception = GetTreeEntry(DreamPath.Exception);
            Savefile = GetTreeEntry(DreamPath.Savefile);
            Regex = GetTreeEntry(DreamPath.Regex);
            Filter = GetTreeEntry(DreamPath.Filter);
            Icon = GetTreeEntry(DreamPath.Icon);
            Image = GetTreeEntry(DreamPath.Image);
            MutableAppearance = GetTreeEntry(DreamPath.MutableAppearance);
            Atom = GetTreeEntry(DreamPath.Atom);
            Area = GetTreeEntry(DreamPath.Area);
            Turf = GetTreeEntry(DreamPath.Turf);
            Movable = GetTreeEntry(DreamPath.Movable);
            Obj = GetTreeEntry(DreamPath.Obj);
            Mob = GetTreeEntry(DreamPath.Mob);

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

            //Third pass: Load each type's vars and procs
            //This must happen top-down from the root of the object tree for inheritance to work
            //Thus, the enumeration of GetAllDescendants()
            uint treeIndex = 0;
            foreach (TreeEntry type in GetAllDescendants(DreamPath.Root)) {
                int typeId = pathToTypeId[type.Path];
                DreamTypeJson jsonType = types[typeId];
                var definition = new DreamObjectDefinition(this, type);

                type.ObjectDefinition = definition;
                type.TreeIndex = treeIndex++;

                LoadVariablesFromJson(definition, jsonType);

                if (jsonType.Procs != null) {
                    foreach (var procList in jsonType.Procs) {
                        foreach (var procId in procList) {
                            var proc = Procs[procId];
                            type.ObjectDefinition.SetProcDefinition(proc.Name, procId);
                        }
                    }
                }

                if (jsonType.InitProc != null) {
                    var initProc = Procs[jsonType.InitProc.Value];
                    if (definition.InitializationProc != null)
                        initProc.SuperProc = Procs[definition.InitializationProc.Value];
                    definition.InitializationProc = jsonType.InitProc.Value;
                }
            }

            // Fourth pass: Set every TreeEntry's ChildrenCount
            foreach (TreeEntry type in TraversePostOrder(Root)) {
                if (type.ParentEntry != null)
                    type.ParentEntry.ChildCount += type.ChildCount + 1;
            }

            //Fifth pass: Set atom's text
            foreach (TreeEntry type in GetAllDescendants(DreamPath.Atom)) {
                if (type.ObjectDefinition.Variables["text"].Equals(DreamValue.Null) && type.ObjectDefinition.Variables["name"].TryGetValueAsString(out var name)) {
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

        public DreamProc LoadProcJson(DreamTypeJson[] types, ProcDefinitionJson procDefinition) {
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

            DreamPath owningType = new DreamPath(types[procDefinition.OwningTypeId].Path);
            var proc = new DMProc(owningType, procDefinition.Name, null, argumentNames, argumentTypes, bytecode, procDefinition.MaxStackSize, procDefinition.Attributes, procDefinition.VerbName, procDefinition.VerbCategory, procDefinition.VerbDesc, procDefinition.Invisibility, this);
            proc.Source = procDefinition.Source;
            proc.Line = procDefinition.Line;
            return proc;
        }

        private void LoadProcsFromJson(DreamTypeJson[] types, ProcDefinitionJson[] jsonProcs, List<int> jsonGlobalProcs) {
            Procs = new(jsonProcs.Length);
            foreach (var proc in jsonProcs)
            {
                Procs.Add(LoadProcJson(types, proc));
            }

            if (jsonGlobalProcs != null) {
                _globalProcIds = new(jsonGlobalProcs.Count);

                foreach (var procId in jsonGlobalProcs) {
                    var proc = Procs[procId];

                    _globalProcIds.Add(proc.Name, procId);
                }
            }
        }

        public NativeProc CreateNativeProc(DreamPath owningType, NativeProc.HandlerFn func, out int procId) {
            var (name, defaultArgumentValues, argumentNames) = NativeProc.GetNativeInfo(func);
            var proc = new NativeProc(owningType, name, null, argumentNames, null, defaultArgumentValues, func, null, null, null, null);
            procId = Procs.Count;
            Procs.Add(proc);
            return proc;
        }

        public AsyncNativeProc CreateAsyncNativeProc(DreamPath owningType, Func<AsyncNativeProc.State, Task<DreamValue>> func, out int procId) {
            var (name, defaultArgumentValues, argumentNames) = NativeProc.GetNativeInfo(func);
            var proc = new AsyncNativeProc(owningType, name, null, argumentNames, null, defaultArgumentValues, func,null, null, null, null);
            procId = Procs.Count;
            Procs.Add(proc);
            return proc;
        }

        public void SetGlobalNativeProc(NativeProc.HandlerFn func) {
            var (name, defaultArgumentValues, argumentNames) = NativeProc.GetNativeInfo(func);
            var proc = new NativeProc(DreamPath.Root, name, null, argumentNames, null, defaultArgumentValues, func, null, null, null, null);

            Procs[_globalProcIds[name]] = proc;
        }

        public void SetGlobalNativeProc(Func<AsyncNativeProc.State, Task<DreamValue>> func) {
            var (name, defaultArgumentValues, argumentNames) = NativeProc.GetNativeInfo(func);
            var proc = new AsyncNativeProc(DreamPath.Root, name, null, argumentNames, null, defaultArgumentValues, func, null, null, null, null);

            Procs[_globalProcIds[name]] = proc;
        }

        public void SetNativeProc(DreamObjectDefinition definition, NativeProc.HandlerFn func) {
            var proc = CreateNativeProc(definition.Type, func, out var procId);

            definition.SetProcDefinition(proc.Name, procId);
        }

        public void SetNativeProc(DreamObjectDefinition definition, Func<AsyncNativeProc.State, Task<DreamValue>> func) {
            var proc = CreateAsyncNativeProc(definition.Type, func, out var procId);

            definition.SetProcDefinition(proc.Name, procId);
        }

        /// <summary>
        /// Enumerate the inheritance tree in post-order
        /// </summary>
        private IEnumerable<TreeEntry> TraversePostOrder(TreeEntry from) {
            foreach (int typeId in from.InheritingTypes) {
                TreeEntry type = Types[typeId];
                IEnumerator<TreeEntry> typeChildren = TraversePostOrder(type).GetEnumerator();

                while (typeChildren.MoveNext()) yield return typeChildren.Current;
            }

            yield return from;
        }
    }

    public interface IDreamObjectTree {
        public sealed class TreeEntry {
            public DreamPath Path;
            public readonly int Id;
            public DreamObjectDefinition ObjectDefinition;
            public TreeEntry ParentEntry;
            public List<int> InheritingTypes = new();

            /// <summary>
            /// This node's index in the inheritance tree based on a depth-first search<br/>
            /// Useful for quickly determining inheritance
            /// </summary>
            public uint TreeIndex;

            /// <summary>
            /// The total amount of children this node has
            /// </summary>
            public uint ChildCount;

            public TreeEntry(DreamPath path, int id) {
                Path = path;
                Id = id;
            }
        }

        public TreeEntry[] Types { get; }
        public List<DreamProc> Procs { get; }
        public List<string> Strings { get; }

        // All the built-in types
        public TreeEntry World { get; }
        public TreeEntry Client { get; }
        public TreeEntry Datum { get; }
        public TreeEntry Sound { get; }
        public TreeEntry Matrix { get; }
        public TreeEntry Exception { get; }
        public TreeEntry Savefile { get; }
        public TreeEntry Regex { get; }
        public TreeEntry Filter { get; }
        public TreeEntry Icon { get; }
        public TreeEntry Image { get; }
        public TreeEntry MutableAppearance { get; }
        public TreeEntry Atom { get; }
        public TreeEntry Area { get; }
        public TreeEntry Turf { get; }
        public TreeEntry Movable { get; }
        public TreeEntry Obj { get; }
        public TreeEntry Mob { get; }

        public void LoadJson(DreamCompiledJson json);
        public void SetMetaObject(DreamPath path, IDreamMetaObject metaObject);
        public void SetGlobalNativeProc(NativeProc.HandlerFn func);
        public void SetGlobalNativeProc(Func<AsyncNativeProc.State, Task<DreamValue>> func);
        public void SetNativeProc(DreamObjectDefinition definition, NativeProc.HandlerFn func);
        public void SetNativeProc(DreamObjectDefinition definition, Func<AsyncNativeProc.State, Task<DreamValue>> func);

        public DreamObject CreateObject(DreamPath path);
        public bool TryGetGlobalProc(string name, [NotNullWhen(true)] out DreamProc? globalProc);
        public bool HasTreeEntry(DreamPath path);
        public TreeEntry GetTreeEntry(DreamPath path);
        public TreeEntry GetTreeEntry(int typeId);
        public DreamObjectDefinition GetObjectDefinition(DreamPath path);
        public DreamObjectDefinition GetObjectDefinition(int typeId);
        public IEnumerable<TreeEntry> GetAllDescendants(DreamPath path);
        public DreamValue GetDreamValueFromJsonElement(object value);
    }
}
