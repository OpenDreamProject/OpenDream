using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using DMCompiler.Bytecode;
using DMCompiler.Json;
using OpenDreamRuntime.Map;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using OpenDreamRuntime.Util;
using OpenDreamShared;
using OpenDreamShared.Dream;
using Robust.Server;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace OpenDreamRuntime;

public sealed partial class DreamManager {
    public DreamObjectWorld WorldInstance { get; set; }
    public Exception? LastDMException { get; set; }

    public event EventHandler<Exception>? OnException;

    // Global state that may not really (really really) belong here
    public DreamValue[] Globals { get; set; } = Array.Empty<DreamValue>();
    public List<string> GlobalNames { get; private set; } = new();
    public ConcurrentDictionary<int, WeakDreamRef> ReferenceIDsToDreamObject { get; } = new();
    public HashSet<DreamObject> Clients { get; } = new();

    // I solemnly swear this benefits from being a linked list (constant remove times without relying on object hash) --kaylie
    public LinkedList<WeakDreamRef> Datums { get; } = new();
    public readonly ConcurrentBag<DreamObject> DelQueue = new();
    public Random Random { get; set; } = new();
    public Dictionary<string, List<DreamObject>> Tags { get; } = new();
    public DreamProc ImageConstructor, ImageFactoryProc;

    public bool Initialized { get; private set; }
    public GameTick InitializedTick { get; private set; }
    public bool IsShutDown { get; private set; }

    /// <summary>
    /// A millisecond count of when the current tick started.
    /// Set to Environment.TickCount64 at the beginning of every tick.
    /// </summary>
    public long CurrentTickStart { get; private set; }

    private ISawmill _sawmill = default!;
    private int _dreamObjectRefIdCounter;

    [Dependency] private readonly AtomManager _atomManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
    [Dependency] private readonly ProcScheduler _procScheduler = default!;
    [Dependency] private readonly DreamResourceManager _dreamResourceManager = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DreamObjectTree _objectTree = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    private ServerAppearanceSystem? _appearanceSystem;

    //TODO This arg is awful and temporary until RT supports cvar overrides in unit tests
    public void PreInitialize(string? jsonPath) {
        _sawmill = Logger.GetSawmill("opendream");
        ByondApi.ByondApi.Initialize(this, _atomManager, _dreamMapManager, _objectTree);

        InitializeConnectionManager();
        _dreamResourceManager.PreInitialize();

        if (!LoadJson(jsonPath)) {
            _taskManager.RunOnMainThread(() => { IoCManager.Resolve<IBaseServer>().Shutdown("Error while loading the compiled json. The opendream.json_path CVar may be empty, or points to a file that doesn't exist"); });
        }
    }

    public void StartWorld() {
        using (Profiler.BeginZone("StartWorld", color:(uint)Color.OrangeRed.ToArgb())) {
            // It is now OK to call user code, like /New procs.
            Initialized = true;
            InitializedTick = _gameTiming.CurTick;
            CurrentTickStart = Environment.TickCount64;

            // Call global <init> with waitfor=FALSE
            _objectTree.GlobalInitProc?.Spawn(WorldInstance, new());

            // Call New() on all /area and /turf that exist, each with waitfor=FALSE separately. If <global init> created any /area, call New a SECOND TIME
            // new() up /objs and /mobs from compiled-in maps [order: (1,1) then (2,1) then (1,2) then (2,2)]
            _dreamMapManager.InitializeAtoms();

            // Call world.New()
            WorldInstance.SpawnProc("New");
        }
    }

    public void Shutdown() {
        // TODO: Respect not calling parent and aborting shutdown
        WorldInstance.Delete();
        ShutdownConnectionManager();
        Initialized = false;
        IsShutDown = true;
    }

    public void Update() {
        if (!Initialized)
                return;

        using (Profiler.BeginZone("Tick", color:(uint)Color.OrangeRed.ToArgb())) {
            CurrentTickStart = Environment.TickCount64;

            using (Profiler.BeginZone("DM Execution", color:(uint)Color.LightPink.ToArgb()))
                _procScheduler.Process();

            using (Profiler.BeginZone("Map Update", color:(uint)Color.LightPink.ToArgb())){
                UpdateStat();
                _dreamMapManager.UpdateTiles();
            }

            using (Profiler.BeginZone("ByondApi Thread Syncs"))
                ByondApi.ByondApi.ExecuteThreadSyncs();

            using (Profiler.BeginZone("Disk IO", color:(uint)Color.LightPink.ToArgb()))
                DreamObjectSavefile.FlushAllUpdates();

            WorldInstance.Cpu = WorldInstance.TickUsage;

            using (Profiler.BeginZone("Deletion Queue", color:(uint)Color.LightPink.ToArgb()))
                ProcessDelQueue();
        }

        Profiler.EmitFrameMark();
    }

    public void ProcessDelQueue() {
        while (DelQueue.TryTake(out var obj)) {
            obj.Delete();
        }
    }

    public bool TryGetGlobalProc(string name, [NotNullWhen(true)] out DreamProc? proc) {
        return _objectTree.TryGetGlobalProc(name, out proc);
    }

    public bool LoadJson(string? jsonPath) {
        if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
            return false;

        string jsonSource = File.ReadAllText(jsonPath);
        DreamCompiledJson? json = JsonSerializer.Deserialize<DreamCompiledJson>(jsonSource);
        if (json == null)
            return false;

        if (!json.Metadata.Version.Equals(OpcodeVerifier.GetOpcodesHash())) {
            _sawmill.Error("Compiler opcode version does not match the runtime version!");
        }

        var rootPath = Path.GetFullPath(Path.GetDirectoryName(jsonPath)!);
        var resources = json.Resources ?? Array.Empty<string>();
        _dreamResourceManager.Initialize(rootPath, resources, json.Interface);

        _objectTree.LoadJson(json);
        DreamProcNative.SetupNativeProcs(_objectTree);
        ImageConstructor = _objectTree.Image.ObjectDefinition.GetProc("New");
        _objectTree.TryGetGlobalProc("image", out ImageFactoryProc!);

        _dreamMapManager.Initialize();
        WorldInstance = new DreamObjectWorld(_objectTree.World.ObjectDefinition);

        // Call /world/<init>. This is an IMPLEMENTATION DETAIL and non-DMStandard should NOT be run here.
        WorldInstance.InitSpawn(new());

        if (json.Globals is { } jsonGlobals) {
            Globals = new DreamValue[jsonGlobals.GlobalCount];
            GlobalNames = jsonGlobals.Names;

            for (int i = 0; i < jsonGlobals.GlobalCount; i++) {
                object globalValue = jsonGlobals.Globals.GetValueOrDefault(i, null);
                Globals[i] = _objectTree.GetDreamValueFromJsonElement(globalValue);
            }
        }

        _dreamMapManager.LoadMaps(json.Maps);
        return true;
    }

    public void WriteWorldLog(string message, LogLevel level = LogLevel.Info, string sawmill = "world.log") {
        if (!WorldInstance.GetVariable("log").TryGetValueAsDreamResource(out var logRsc)) {
            logRsc = new ConsoleOutputResource();
            WorldInstance.SetVariableValue("log", new DreamValue(logRsc));
            _sawmill.Log(LogLevel.Error, $"Failed to write to the world log, falling back to console output. Original log message follows: [{LogMessage.LogLevelToName(level)}] world.log: {message}");
        }

        if (logRsc is ConsoleOutputResource consoleOut) { // Output() on ConsoleOutputResource uses LogLevel.Info
            consoleOut.WriteConsole(level, sawmill, message);
        } else {
            logRsc.Output(new DreamValue($"[{LogMessage.LogLevelToName(level)}] {sawmill}: {message}"));

            if (_config.GetCVar(OpenDreamCVars.AlwaysShowExceptions)) {
                Logger.GetSawmill(sawmill).Log(level, message);
            }
        }
    }

    public uint FindOrAddString(string str) {
        var idx = FindString(str);
        if (idx == null) {
            _objectTree.Strings.Add(str);
            idx = (uint)(_objectTree.Strings.Count - 1);
        }

        return (uint)idx;
    }

    public uint? FindString(string str) {
        int idx = _objectTree.Strings.IndexOf(str);

        if (idx < 0) {
            return null;
        }

        return (uint)idx;
    }

    public string CreateRef(DreamValue value) {
        return $"[0x{CreateRefInt(value, out _):x}]";
    }

    public uint CreateRefInt(DreamValue value, out RefType refType) {
        int idx;

        if (value.TryGetValueAsDreamObject(out var refObject)) {
            if (refObject == null) {
                refType = RefType.Null;
                idx = 0;
            } else {
                if(refObject.Deleted) {
                    // i dont believe this will **ever** be called, but just to be sure, funky errors /might/ appear in the future if someone does a fucky wucky and calls this on a deleted object.
                    throw new Exception("Cannot create reference ID for an object that is deleted");
                }

                switch(refObject){
                    case DreamObjectTurf: refType = RefType.DreamObjectTurf; break;
                    case DreamObjectMob: refType = RefType.DreamObjectMob; break;
                    case DreamObjectArea: refType = RefType.DreamObjectArea; break;
                    case DreamObjectClient: refType = RefType.DreamObjectArea; break;
                    case DreamObjectImage: refType = RefType.DreamObjectImage; break;
                    case DreamObjectFilter: refType = RefType.DreamObjectFilter; break;
                    default: {
                        refType = RefType.DreamObjectDatum;
                        if(refObject.IsSubtypeOf(_objectTree.Obj))
                            refType = RefType.DreamObject;
                        else if (refObject.GetType() == typeof(DreamList))
                            refType = RefType.DreamObjectList;
                        break;
                    }
                }

                if (refObject.RefId is not {} id) {
                    idx = Interlocked.Increment(ref _dreamObjectRefIdCounter);
                    refObject.RefId = idx;

                    // SAFETY: Infallible! idx is always unique and add can only fail if this is not the case.
                    ReferenceIDsToDreamObject.TryAdd(idx, new WeakDreamRef(refObject));
                } else {
                    idx = id;
                }
            }
        } else if (value.TryGetValueAsString(out var refStr)) {
            refType = RefType.String;
            idx = _objectTree.Strings.IndexOf(refStr);

            if (idx == -1) {
                _objectTree.Strings.Add(refStr);
                idx = _objectTree.Strings.Count - 1;
            }
        } else if (value.TryGetValueAsType(out var type)) {
            refType = RefType.DreamType;
            idx = type.Id;
        } else if (value.TryGetValueAsAppearance(out var appearance)) {
            refType = RefType.DreamAppearance;
            _appearanceSystem ??= _entitySystemManager.GetEntitySystem<ServerAppearanceSystem>();
            idx = (int)_appearanceSystem.AddAppearance(appearance).MustGetId();
        } else if (value.TryGetValueAsDreamResource(out var refRsc)) {
            refType = RefType.DreamResource;
            idx = refRsc.Id;
        }  else if (value.TryGetValueAsProc(out var proc)) {
            refType = RefType.Proc;
            idx = proc.Id;
        } else if (value.TryGetValueAsFloat(out var floatValue)) {
            refType = RefType.Number;

            // Yes, this combines with the refType and produces an invalid ref.
            // This is BYOND behavior (as of writing at least, on 516.1661).
            idx = BitConverter.SingleToInt32Bits(floatValue);
        } else {
            throw new NotImplementedException($"Ref for {value} is unimplemented");
        }

        // The highest byte is the type
        return (uint)refType | (uint)idx;
    }

    /// <summary>
    /// Iterates the list of datums
    /// </summary>
    /// <returns>Datum enumerator</returns>
    /// <remarks>As it's a convenient time, this will collect any dead datum refs as it finds them.</remarks>
    public IEnumerable<DreamObject> IterateDatums() {
        // This isn't a common operation so we'll use this time to also do some pruning.
        var node = Datums.First;

        while (node is not null) {
            var next = node.Next;
            var val = node.Value.Target;
            if (val is null)
                Datums.Remove(node);
            else
                yield return val;
            node = next;
        }
    }

    public DreamValue RefIdToValue(int rawRefId) {
        // The first one/two digits give the type, the last 6 give the index
        var typeId = (RefType)(rawRefId & 0xFF000000);
        var refId = (rawRefId & 0x00FFFFFF); // The ref minus its ref type prefix

        switch (typeId) {
            case RefType.Null:
                return DreamValue.Null;
            case RefType.DreamObjectArea:
            case RefType.DreamObjectClient:
            case RefType.DreamObjectDatum:
            case RefType.DreamObjectImage:
            case RefType.DreamObjectFilter:
            case RefType.DreamObjectList:
            case RefType.DreamObjectMob:
            case RefType.DreamObjectTurf:
            case RefType.DreamObject:
                if (ReferenceIDsToDreamObject.TryGetValue(refId, out var weakRef) && weakRef.Target is { } dreamObject)
                    return new(dreamObject);

                return DreamValue.Null;
            case RefType.String:
                return _objectTree.Strings.Count > refId
                    ? new DreamValue(_objectTree.Strings[refId])
                    : DreamValue.Null;
            case RefType.DreamType:
                return _objectTree.Types.Length > refId
                    ? new DreamValue(_objectTree.Types[refId])
                    : DreamValue.Null;
            case RefType.DreamResourceIcon: // Alias of DreamResource for now. TODO: Does this *only* contain icon resources?
            case RefType.DreamResource:
                if (!_dreamResourceManager.TryLoadResource(refId, out var resource))
                    return DreamValue.Null;

                return new DreamValue(resource);
            case RefType.DreamAppearance:
                _appearanceSystem ??= _entitySystemManager.GetEntitySystem<ServerAppearanceSystem>();
                return _appearanceSystem.TryGetAppearanceById((uint)refId, out ImmutableAppearance? appearance)
                    ? new DreamValue(appearance.ToMutable())
                    : DreamValue.Null;
            case RefType.Proc:
                return new(_objectTree.Procs[refId]);
            case RefType.Number: // For the oh so few numbers this works with (most numbers clobber the ref type)
                return new(BitConverter.Int32BitsToSingle(refId));
            default:
                throw new Exception($"Invalid reference type for ref [0x{rawRefId:x}]");
        }
    }

    public DreamValue LocateRef(string refString) {
        bool canBePointer = false;

        if (refString.StartsWith('[') && refString.EndsWith(']')) {
            // Strip the surrounding []
            refString = refString.Substring(1, refString.Length - 2);

            // This ref could possibly be a "pointer" (the hex number made up of an id and an index)
            canBePointer = refString.StartsWith("0x");
        }

        if (canBePointer && int.TryParse(refString.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out var refId)) {
            return RefIdToValue(refId);
        }

        // Search for an object with this ref as its tag
        // Note that surrounding [] are stripped out at this point, this is intentional
        // Doing locate("[abc]") is the same as locate("abc")
        if (Tags.TryGetValue(refString, out var tagList)) {
            return new DreamValue(tagList.First());
        }

        // Nothing found
        return DreamValue.Null;
    }

    public DreamObject? GetFromClientReference(DreamConnection connection, ClientObjectReference reference) {
        switch (reference.Type) {
            case ClientObjectReference.RefType.Client:
                return connection.Client;
            case ClientObjectReference.RefType.Entity:
                _atomManager.TryGetMovableFromEntity(_entityManager.GetEntity(reference.Entity), out var atom);
                return atom;
            case ClientObjectReference.RefType.Turf:
                _dreamMapManager.TryGetTurfAt((reference.TurfX, reference.TurfY), reference.TurfZ, out var turf);
                return turf;
        }

        return null;
    }

    public ClientObjectReference GetClientReference(DreamObjectAtom atom) {
        if (atom is DreamObjectMovable movable) {
            return new(_entityManager.GetNetEntity(movable.Entity));
        } else if (atom is DreamObjectTurf turf) {
            return new((turf.X, turf.Y), turf.Z);
        } else {
            throw new NotImplementedException($"Cannot create a client reference for {atom}");
        }
    }

    public void HandleException(Exception e, string msg = "", string file = "", int line = 0) {
        if (string.IsNullOrEmpty(msg)) { // Just print the C# exception if we don't override the message
            msg = e.Message;
        }

        LastDMException = e;
        OnException?.Invoke(this, e);

        // Invoke world.Error()
        var obj =_objectTree.CreateObject<DreamObjectException>(_objectTree.Exception);
        if(e is DMThrowException throwException)
            obj.Name = throwException.Value;
        else
            obj.Name = new DreamValue(e.Message);
        obj.Desc =  new DreamValue(msg);
        obj.Line = new DreamValue(line);
        obj.File = new DreamValue(file);

        WorldInstance.SpawnProc("Error", usr: null, new DreamValue(obj));
    }
}

public enum RefType : uint {
    Null = 0x0,
    DreamObjectTurf = 0x1000000,
    DreamObject = 0x2000000,
    DreamObjectMob = 0x3000000,
    DreamObjectArea = 0x4000000,
    DreamObjectClient = 0x5000000,
    DreamObjectFilter = 0x5300000,
    DreamResourceIcon = 0xC000000,
    DreamObjectImage = 0xD000000,
    DreamObjectList = 0xF000000,
    DreamObjectDatum = 0x21000000,
    String = 0x6000000,
    DreamType = 0x9000000, //in byond type is from 0x8 to 0xb, but fuck that
    DreamResource = 0x27000000, //Equivalent to file
    DreamAppearance = 0x3A000000,
    Proc = 0x26000000,
    Number = 0x2A000000
}
