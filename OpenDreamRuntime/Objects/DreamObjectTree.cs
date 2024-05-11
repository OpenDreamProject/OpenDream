using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using DMCompiler.Json;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.DebugAdapter;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Exceptions;
using MethodImplAttribute = System.Runtime.CompilerServices.MethodImplAttribute;
using MethodImplOptions = System.Runtime.CompilerServices.MethodImplOptions;

namespace OpenDreamRuntime.Objects;

public sealed class DreamObjectTree {
    public TreeEntry[] Types { get; private set; }
    public List<DreamProc> Procs { get; private set; } = new();
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

    private readonly Dictionary<string, TreeEntry> _pathToType = new();
    private Dictionary<string, int> _globalProcIds;

    [Dependency] private readonly AtomManager _atomManager = default!;
    [Dependency] private readonly DreamManager _dreamManager = default!;
    [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IDreamDebugManager _dreamDebugManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly DreamResourceManager _dreamResourceManager = default!;
    [Dependency] private readonly WalkManager _walkManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly ProcScheduler _procScheduler = default!;
    private ServerAppearanceSystem? _appearanceSystem;
    private TransformSystem? _transformSystem;
    private PvsOverrideSystem? _pvsOverrideSystem;
    private MetaDataSystem? _metaDataSystem;
    private ServerVerbSystem? _verbSystem;

    public void LoadJson(DreamCompiledJson json) {
        var types = json.Types ?? Array.Empty<DreamTypeJson>();
        if (types.Length == 0 || types[0].Path != "/")
            throw new ArgumentException("The first type must be root!", nameof(json));

        Root = new("/", 0);

        _entitySystemManager.TryGetEntitySystem(out _appearanceSystem);
        _entitySystemManager.TryGetEntitySystem(out _transformSystem);
        _entitySystemManager.TryGetEntitySystem(out _pvsOverrideSystem);
        _entitySystemManager.TryGetEntitySystem(out _metaDataSystem);
        _entitySystemManager.TryGetEntitySystem(out _verbSystem);

        Strings = json.Strings ?? new();

        if (json.GlobalInitProc is { } initProcDef) {
            GlobalInitProc = new DMProc(0, Root, initProcDef, "<global init>", _dreamManager, _atomManager, _dreamMapManager, _dreamDebugManager, _dreamResourceManager, this, _procScheduler, _verbSystem);
        } else {
            GlobalInitProc = null;
        }

        var procs = json.Procs;
        var globalProcs = json.GlobalProcs;

        LoadTypesFromJson(types, procs, globalProcs);
    }

    public TreeEntry GetTreeEntry(string path) {
        if (!_pathToType.TryGetValue(path, out TreeEntry? type)) {
            throw new Exception($"Object '{path}' does not exist");
        }

        return type;
    }

    public TreeEntry GetTreeEntry(int typeId) {
        return Types[typeId];
    }

    public bool TryGetTreeEntry(string path, [NotNullWhen(true)] out TreeEntry? treeEntry) {
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

            typeChildren.Dispose();
        }
    }

    /// <remarks>
    /// It is the job of whatever calls this function to then initialize the object! <br/>
    /// (by calling the result of <see cref="DreamObject.InitProc(DreamThread, DreamObject?, DreamProcArguments)"/> or <see cref="DreamObject.InitSpawn(DreamProcArguments)"/>)
    /// </remarks>
    public DreamObject CreateObject(TreeEntry type) {
        if (type == List)
            return CreateList();
        if (type == Savefile)
            return new DreamObjectSavefile(Savefile.ObjectDefinition);
        if (type.ObjectDefinition.IsSubtypeOf(Matrix))
            return new DreamObjectMatrix(type.ObjectDefinition);
        if (type.ObjectDefinition.IsSubtypeOf(Sound))
            return new DreamObjectSound(type.ObjectDefinition);
        if (type.ObjectDefinition.IsSubtypeOf(Regex))
            return new DreamObjectRegex(type.ObjectDefinition);
        if (type.ObjectDefinition.IsSubtypeOf(Image))
            return new DreamObjectImage(type.ObjectDefinition);
        if (type.ObjectDefinition.IsSubtypeOf(Icon))
            return new DreamObjectIcon(type.ObjectDefinition);
        if (type.ObjectDefinition.IsSubtypeOf(Filter))
            return new DreamObjectFilter(type.ObjectDefinition);
        if (type.ObjectDefinition.IsSubtypeOf(Mob))
            return new DreamObjectMob(type.ObjectDefinition);
        if (type.ObjectDefinition.IsSubtypeOf(Movable))
            return new DreamObjectMovable(type.ObjectDefinition);
        if (type.ObjectDefinition.IsSubtypeOf(Area))
            return new DreamObjectArea(type.ObjectDefinition);
        if (type.ObjectDefinition.IsSubtypeOf(Atom))
            return new DreamObjectAtom(type.ObjectDefinition);
        if (type.ObjectDefinition.IsSubtypeOf(Client))
            throw new Exception("Cannot create objects of type /client");
        if (type.ObjectDefinition.IsSubtypeOf(Turf))
            throw new Exception("New turfs must be created by the map manager");
        if (type.ObjectDefinition.IsSubtypeOf(Exception))
            return new DreamObjectException(type.ObjectDefinition);

        return new DreamObject(type.ObjectDefinition);
    }

    public T CreateObject<T>(TreeEntry type) where T : DreamObject {
        return (T)CreateObject(type);
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
                    case JsonVariableType.List:
                        DreamList list = CreateList();

                        if (jsonElement.TryGetProperty("values", out JsonElement values)) {
                            foreach (JsonElement listValue in values.EnumerateArray()) {
                                if (listValue.ValueKind == JsonValueKind.Object &&
                                    !listValue.TryGetProperty("type", out _)) {
                                    if (!listValue.TryGetProperty("key", out var jsonKey) ||
                                        !listValue.TryGetProperty("value", out var jsonValue))
                                        throw new Exception("List value was missing a key or value property");

                                    list.SetValue(GetDreamValueFromJsonElement(jsonKey),
                                        GetDreamValueFromJsonElement(jsonValue), allowGrowth: true);
                                } else {
                                    list.AddValue(GetDreamValueFromJsonElement(listValue));
                                }
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

    private void LoadTypesFromJson(DreamTypeJson[] types, ProcDefinitionJson[]? procs, int[]? globalProcs) {
        Types = new TreeEntry[types.Length];

        //First pass: Create types and set them up for initialization
        Types[0] = Root;
        for (int i = 1; i < Types.Length; i++) {
            var path = types[i].Path;
            var type = new TreeEntry(path, i);

            Types[i] = type;
            _pathToType[path] = type;
        }

        World = GetTreeEntry("/world");
        List = GetTreeEntry("/list");
        Client = GetTreeEntry("/client");
        Datum = GetTreeEntry("/datum");
        Sound = GetTreeEntry("/sound");
        Matrix = GetTreeEntry("/matrix");
        Exception = GetTreeEntry("/exception");
        Savefile = GetTreeEntry("/savefile");
        Regex = GetTreeEntry("/regex");
        Filter = GetTreeEntry("/dm_filter");
        Icon = GetTreeEntry("/icon");
        Image = GetTreeEntry("/image");
        MutableAppearance = GetTreeEntry("/mutable_appearance");
        Atom = GetTreeEntry("/atom");
        Area = GetTreeEntry("/area");
        Turf = GetTreeEntry("/turf");
        Movable = GetTreeEntry("/atom/movable");
        Obj = GetTreeEntry("/obj");
        Mob = GetTreeEntry("/mob");

        // Load procs first so types can set their init proc's super proc
        LoadProcsFromJson(procs, globalProcs);

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
            int typeId = type.Id;
            DreamTypeJson jsonType = types[typeId];
            var definition = new DreamObjectDefinition(_dreamManager, this, _atomManager, _dreamMapManager, _mapManager, _dreamResourceManager, _walkManager, _entityManager, _playerManager, _serializationManager, _appearanceSystem, _transformSystem, _pvsOverrideSystem, _metaDataSystem, _verbSystem, type);

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

        // Fifth pass: Set atom's name and text
        foreach (TreeEntry type in GetAllDescendants(Atom)) {
            if (type.ObjectDefinition.Variables["name"].IsNull)
                type.ObjectDefinition.Variables["name"] = new(type.Name.Replace("_", " "));

            if (type.ObjectDefinition.Variables["text"].IsNull && type.ObjectDefinition.Variables["name"].TryGetValueAsString(out var name)) {
                type.ObjectDefinition.Variables["text"] = new DreamValue(string.IsNullOrEmpty(name) ? string.Empty : name[..1]);
            }
        }

        // Register verbs
        if (_verbSystem != null) {
            foreach (DreamProc proc in Procs) {
                if (!proc.IsVerb)
                    continue;

                _verbSystem.RegisterVerb(proc);
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

        if (jsonObject.ConstVariables != null) {
            objectDefinition.ConstVariables ??= new();
            foreach (string jsonConstVariable in jsonObject.ConstVariables) {
                objectDefinition.ConstVariables.Add(jsonConstVariable);
            }
        }

        if(jsonObject.TmpVariables != null) {
            objectDefinition.TmpVariables ??= new();
            foreach (string jsonTmpVariable in jsonObject.TmpVariables) {
                objectDefinition.TmpVariables.Add(jsonTmpVariable);
            }
        }
    }

    public DreamProc LoadProcJson(int id, ProcDefinitionJson procDefinition) {
        TreeEntry owningType = Types[procDefinition.OwningTypeId];
        return new DMProc(id, owningType, procDefinition, null, _dreamManager,
            _atomManager, _dreamMapManager, _dreamDebugManager, _dreamResourceManager, this, _procScheduler, _verbSystem);
    }

    private void LoadProcsFromJson(ProcDefinitionJson[]? jsonProcs, int[]? jsonGlobalProcs) {
        Procs.Clear();
        if (jsonProcs != null) {
            Procs.EnsureCapacity(jsonProcs.Length);

            foreach (var procJson in jsonProcs) {
                var proc = LoadProcJson(Procs.Count, procJson);

                Procs.Add(proc);
            }
        }

        if (jsonGlobalProcs != null) {
            _globalProcIds = new(jsonGlobalProcs.Length);

            foreach (var procId in jsonGlobalProcs) {
                var proc = Procs[procId];

                _globalProcIds.Add(proc.Name, procId);
            }
        }
    }

    internal NativeProc CreateNativeProc(TreeEntry owningType, NativeProc.HandlerFn func) {
        var (name, defaultArgumentValues, argumentNames) = NativeProc.GetNativeInfo(func);
        var proc = new NativeProc(Procs.Count, owningType, name, argumentNames, defaultArgumentValues, func, _dreamManager, _atomManager, _dreamMapManager, _dreamResourceManager, _walkManager, this);

        Procs.Add(proc);
        return proc;
    }

    private AsyncNativeProc CreateAsyncNativeProc(TreeEntry owningType, Func<AsyncNativeProc.State, Task<DreamValue>> func) {
        var (name, defaultArgumentValues, argumentNames) = NativeProc.GetNativeInfo(func);
        var proc = new AsyncNativeProc(Procs.Count, owningType, name, argumentNames, defaultArgumentValues, func);

        Procs.Add(proc);
        return proc;
    }

    internal void SetGlobalNativeProc(NativeProc.HandlerFn func) {
        var (name, defaultArgumentValues, argumentNames) = NativeProc.GetNativeInfo(func);
        var proc = new NativeProc(_globalProcIds[name], Root, name, argumentNames, defaultArgumentValues, func, _dreamManager, _atomManager, _dreamMapManager, _dreamResourceManager, _walkManager, this);

        Procs[proc.Id] = proc;
    }

    public void SetGlobalNativeProc(Func<AsyncNativeProc.State, Task<DreamValue>> func) {
        var (name, defaultArgumentValues, argumentNames) = NativeProc.GetNativeInfo(func);
        var proc = new AsyncNativeProc(_globalProcIds[name], Root, name, argumentNames, defaultArgumentValues, func);

        Procs[proc.Id] = proc;
    }

    internal void SetNativeProc(TreeEntry type, NativeProc.HandlerFn func) {
        var proc = CreateNativeProc(type, func);

        type.ObjectDefinition.SetProcDefinition(proc.Name, proc.Id);
    }

    public void SetNativeProc(TreeEntry type, Func<AsyncNativeProc.State, Task<DreamValue>> func) {
        var proc = CreateAsyncNativeProc(type, func);

        type.ObjectDefinition.SetProcDefinition(proc.Name, proc.Id);
    }

    /// <summary>
    /// Enumerate the inheritance tree in post-order
    /// </summary>
    private IEnumerable<TreeEntry> TraversePostOrder(TreeEntry from) {
        foreach (int typeId in from.InheritingTypes) {
            TreeEntry type = Types[typeId];
            using IEnumerator<TreeEntry> typeChildren = TraversePostOrder(type).GetEnumerator();

            while (typeChildren.MoveNext()) yield return typeChildren.Current;
        }

        yield return from;
    }
}

public sealed class TreeEntry {
    public readonly string Name;
    public readonly string Path;
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

    public TreeEntry(string path, int id) {
        int lastSlash = path.LastIndexOf('/');

        Name = (lastSlash != -1) ? path.Substring(lastSlash + 1) : path;
        Path = path;
        Id = id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSubtypeOf(TreeEntry ancestor) {
        // Unsigned underflow is desirable here
        return (TreeIndex - ancestor.TreeIndex) <= ancestor.ChildCount;
    }

    public override string ToString() {
        return Path;
    }
}
