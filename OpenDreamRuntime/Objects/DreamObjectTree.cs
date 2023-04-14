using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.DebugAdapter;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using Robust.Shared.Serialization.Manager.Exceptions;
using TreeEntry = OpenDreamRuntime.Objects.IDreamObjectTree.TreeEntry;

namespace OpenDreamRuntime.Objects {
    public sealed class DreamObjectTree : IDreamObjectTree {
        public TreeEntry[] Types { get; private set; }
        public List<DreamProc> Procs { get; private set; }
        public List<string> Strings { get; private set; } //TODO: Store this somewhere else
        public DreamProc? GlobalInitProc { get; private set; }

        public TreeEntry Root { get; private set; }
        public TreeEntry List { get; private set; }
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

        [Dependency] private readonly IDreamManager _dreamManager = default!;
        [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
        [Dependency] private readonly IDreamDebugManager _dreamDebugManager = default!;
        [Dependency] private readonly DreamResourceManager _dreamResourceManager = default!;

        public void LoadJson(DreamCompiledJson json) {
            Strings = json.Strings;

            if (json.GlobalInitProc is ProcDefinitionJson initProcDef) {
                GlobalInitProc = new DMProc(DreamPath.Root, initProcDef, "<global init>", _dreamManager, _dreamMapManager, _dreamDebugManager, _dreamResourceManager, this);
            } else {
                GlobalInitProc = null;
            }

            // Load procs first so types can set their init proc's super proc
            LoadProcsFromJson(json.Types, json.Procs, json.GlobalProcs);
            LoadTypesFromJson(json.Types);
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

        public bool TryGetTreeEntry(DreamPath path, [NotNullWhen(true)] out TreeEntry? treeEntry) {
            return _pathToType.TryGetValue(path, out treeEntry);
        }

        public DreamObjectDefinition GetObjectDefinition(int typeId) {
            return GetTreeEntry(typeId).ObjectDefinition;
        }

        public bool TryGetGlobalProc(string name, [NotNullWhen(true)] out DreamProc? globalProc) {
            globalProc = _globalProcIds.TryGetValue(name, out int procId) ? Procs[procId] : null;

            return (globalProc != null);
        }

        public IEnumerable<TreeEntry> GetAllDescendants(TreeEntry treeEntry) {
            yield return treeEntry;

            foreach (int typeId in treeEntry.InheritingTypes) {
                TreeEntry type = Types[typeId];
                IEnumerator<TreeEntry> typeChildren = GetAllDescendants(type).GetEnumerator();

                while (typeChildren.MoveNext()) yield return typeChildren.Current;
            }
        }

        /// <remarks>
        /// It is the job of whatever calls this function to then initialize the object! <br/>
        /// (by calling the result of <see cref="DreamObject.InitProc(DreamThread, DreamObject?, DreamProcArguments)"/> or <see cref="DreamObject.InitSpawn(DreamProcArguments)"/>)
        /// </remarks>
        public DreamObject CreateObject(TreeEntry type) {
            if (type == List) {
                return CreateList();
            } else {
                return new DreamObject(type.ObjectDefinition);
            }
        }

        // TODO: Maybe in the future, DreamList could be made not a DreamObject so this doesn't have to be done through the object tree?
        public DreamList CreateList(int size = 0) {
            return new DreamList(List.ObjectDefinition, size);
        }

        public DreamList CreateList(string[] elements) {
            DreamList list = CreateList(elements.Length);

            foreach (String value in elements) {
                list.AddValue(new DreamValue(value));
            }

            return list;
        }

        public void SetMetaObject(TreeEntry type, IDreamMetaObject metaObject) {
            // TODO: Setting meta objects outside of their order of inheritance can break things.
            metaObject.ParentType = type.ParentEntry.ObjectDefinition.MetaObject;

            foreach (TreeEntry treeEntry in GetAllDescendants(type)) {
                treeEntry.ObjectDefinition.MetaObject = metaObject;
            }
        }

        public DreamValue GetDreamValueFromJsonElement(object? value) {
            if (value == null) return DreamValue.Null;

            JsonElement jsonElement = (JsonElement)value;
            switch (jsonElement.ValueKind) {
                case JsonValueKind.String:
                    var str = jsonElement.GetString();
                    if (str == null)
                        throw new NullNotAllowedException();

                    return new DreamValue(str);
                case JsonValueKind.Number:
                    return new DreamValue(jsonElement.GetSingle());
                case JsonValueKind.Object: {
                    JsonVariableType variableType = (JsonVariableType)jsonElement.GetProperty("type").GetByte();

                    switch (variableType) {
                        case JsonVariableType.Resource: {
                            var resourcePath = jsonElement.GetProperty("resourcePath").GetString();
                            if (resourcePath == null)
                                throw new NullNotAllowedException();

                            var resM = IoCManager.Resolve<DreamResourceManager>();
                            DreamResource resource = resM.LoadResource(resourcePath);

                            return new DreamValue(resource);
                        }
                        case JsonVariableType.Type:
                            JsonElement typeValue = jsonElement.GetProperty("value");

                            return new DreamValue(Types[typeValue.GetInt32()]);
                        case JsonVariableType.Proc:
                            return new DreamValue(Procs[jsonElement.GetProperty("value").GetInt32()]);
                        case JsonVariableType.ProcStub: {
                            TreeEntry type = Types[jsonElement.GetProperty("value").GetInt32()];

                            return DreamValue.CreateProcStub(type);
                        }
                        case JsonVariableType.VerbStub: {
                            TreeEntry type = Types[jsonElement.GetProperty("value").GetInt32()];

                            return DreamValue.CreateVerbStub(type);
                        }
                        case JsonVariableType.List:
                            DreamList list = CreateList();

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
                        case JsonVariableType.PositiveInfinity:
                            return new DreamValue(float.PositiveInfinity);
                        case JsonVariableType.NegativeInfinity:
                            return new DreamValue(float.NegativeInfinity);
                        default:
                            throw new Exception($"Invalid variable type ({variableType})");
                    }
                }
                default:
                    throw new Exception($"Invalid value kind for dream value ({jsonElement.ValueKind})");
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
            List = GetTreeEntry(DreamPath.List);
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
            foreach (TreeEntry type in GetAllDescendants(Root)) {
                int typeId = pathToTypeId[type.Path];
                DreamTypeJson jsonType = types[typeId];
                var definition = new DreamObjectDefinition(_dreamManager, this, type);

                type.ObjectDefinition = definition;
                type.TreeIndex = treeIndex++;

                LoadVariablesFromJson(definition, jsonType);

                if (jsonType.Procs != null) {
                    foreach (var procList in jsonType.Procs) {
                        foreach (var procId in procList) {
                            var proc = Procs[procId];

                            definition.SetProcDefinition(proc.Name, procId);
                        }
                    }
                }

                if (jsonType.Verbs != null) {
                    definition.Verbs ??= new(jsonType.Verbs.Count);
                    definition.Verbs.AddRange(jsonType.Verbs);
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
            foreach (TreeEntry type in GetAllDescendants(Atom)) {
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
            DreamPath owningType = new DreamPath(types[procDefinition.OwningTypeId].Path);
            return new DMProc(owningType, procDefinition, null, _dreamManager,
                _dreamMapManager, _dreamDebugManager, _dreamResourceManager, this);
        }

        private void LoadProcsFromJson(DreamTypeJson[] types, ProcDefinitionJson[] jsonProcs, List<int> jsonGlobalProcs) {
            Procs = new(jsonProcs.Length);
            foreach (var proc in jsonProcs) {
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

        public void SetNativeProc(TreeEntry type, NativeProc.HandlerFn func) {
            var proc = CreateNativeProc(type.Path, func, out var procId);

            type.ObjectDefinition.SetProcDefinition(proc.Name, procId);
        }

        public void SetNativeProc(TreeEntry type, Func<AsyncNativeProc.State, Task<DreamValue>> func) {
            var proc = CreateAsyncNativeProc(type.Path, func, out var procId);

            type.ObjectDefinition.SetProcDefinition(proc.Name, procId);
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
        // TODO: Could probably be merged with DreamObjectDefinition
        public sealed class TreeEntry {
            public DreamPath Path;
            public readonly int Id;
            public DreamObjectDefinition ObjectDefinition;
            public TreeEntry ParentEntry;
            public readonly List<int> InheritingTypes = new();

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

            public override string ToString() {
                return Path.PathString;
            }
        }

        public TreeEntry[] Types { get; }
        public List<DreamProc> Procs { get; }
        public List<string> Strings { get; }
        public DreamProc? GlobalInitProc { get; }

        // All the built-in types
        public TreeEntry Root { get; }
        public TreeEntry List { get; }
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
        public void SetMetaObject(TreeEntry type, IDreamMetaObject metaObject);
        public void SetGlobalNativeProc(NativeProc.HandlerFn func);
        public void SetGlobalNativeProc(Func<AsyncNativeProc.State, Task<DreamValue>> func);
        public void SetNativeProc(TreeEntry type, NativeProc.HandlerFn func);
        public void SetNativeProc(TreeEntry type, Func<AsyncNativeProc.State, Task<DreamValue>> func);

        public DreamObject CreateObject(TreeEntry type);
        public DreamList CreateList(int size = 0);
        public DreamList CreateList(string[] elements);
        public bool TryGetGlobalProc(string name, [NotNullWhen(true)] out DreamProc? globalProc);
        public TreeEntry GetTreeEntry(DreamPath path);
        public TreeEntry GetTreeEntry(int typeId);
        public bool TryGetTreeEntry(DreamPath path, out TreeEntry? treeEntry);
        public DreamObjectDefinition GetObjectDefinition(int typeId);
        public IEnumerable<TreeEntry> GetAllDescendants(TreeEntry treeEntry);
        public DreamValue GetDreamValueFromJsonElement(object value);
    }
}
