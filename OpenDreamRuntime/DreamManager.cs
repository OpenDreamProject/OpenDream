using System.IO;
using System.Text.Json;
using DMCompiler.Bytecode;
using DMCompiler.Json;
using DMCompiler.Compiler;
using OpenDreamRuntime.Map;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Resources;
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
    public HashSet<DreamObject> Clients { get; } = new();

    public Random Random { get; set; } = new();
    public DreamProc ImageConstructor, ImageFactoryProc;
    public int ListPoolThreshold, ListPoolSize;
    public Dictionary<WarningCode, ErrorLevel> OptionalErrors { get; private set; } = new();
    public bool Initialized { get; private set; }
    public GameTick InitializedTick { get; private set; }
    public bool IsShutDown { get; private set; }

    /// <summary>
    /// A millisecond count of when the current tick started.
    /// Set to Environment.TickCount64 at the beginning of every tick.
    /// </summary>
    public long CurrentTickStart { get; private set; }

    private ISawmill _sawmill = default!;

    [Dependency] private AtomManager _atomManager = default!;
    [Dependency] private DreamRefManager _refManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IDreamMapManager _dreamMapManager = default!;
    [Dependency] private ProcScheduler _procScheduler = default!;
    [Dependency] private DreamResourceManager _dreamResourceManager = default!;
    [Dependency] private ITaskManager _taskManager = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private DreamObjectTree _objectTree = default!;
    [Dependency] private IEntityManager _entityManager = default!;

    //TODO This arg is awful and temporary until RT supports cvar overrides in unit tests
    public void PreInitialize(string? jsonPath) {
        _sawmill = Logger.GetSawmill("opendream");
        ListPoolThreshold = _config.GetCVar(OpenDreamCVars.ListPoolThreshold);
        ListPoolSize = _config.GetCVar(OpenDreamCVars.ListPoolSize);

        try {
            ByondApi.ByondApi.Initialize(this, _refManager, _atomManager, _dreamMapManager, _objectTree);
        } catch (DllNotFoundException e) {
            _sawmill.Warning($"ByondApi could not initialize due to missing dll: {e.Message}");
        }

        InitializeConnectionManager();
        _dreamResourceManager.PreInitialize();

        if (!LoadJson(jsonPath)) {
            _taskManager.RunOnMainThread(() => { IoCManager.Resolve<IBaseServer>().Shutdown("Error while loading the compiled json. The opendream.json_path CVar may be empty, or points to a file that doesn't exist"); });
        }
    }

    public void StartWorld() {
        using (Profiler.BeginZone("StartWorld", color: (uint)Color.OrangeRed.ToArgb())) {
            // It is now OK to call user code, like /New procs.
            Initialized = true;
            InitializedTick = _gameTiming.CurTick;
            CurrentTickStart = Environment.TickCount64;

            // Call global <init> with waitfor=FALSE
            _objectTree.GlobalInitProc?.Spawn(WorldInstance, new()).Dispose();

            // Call New() on all /area and /turf that exist, each with waitfor=FALSE separately. If <global init> created any /area, call New a SECOND TIME
            // new() up /objs and /mobs from compiled-in maps [order: (1,1) then (2,1) then (1,2) then (2,2)]
            _dreamMapManager.InitializeAtoms();

            // Call world.New()
            WorldInstance.SpawnProc("New").Dispose();
        }
    }

    public void Shutdown() {
        WorldInstance.DecRef();
        WorldInstance.Delete();
        WorldInstance = null!;

        ShutdownConnectionManager();
        Initialized = false;
        IsShutDown = true;
    }

    public void Update() {
        if (!Initialized)
            return;

        using (Profiler.BeginZone("Tick", color: (uint)Color.OrangeRed.ToArgb())) {
            CurrentTickStart = Environment.TickCount64;

            using (Profiler.BeginZone("DM Execution", color: (uint)Color.LightPink.ToArgb()))
                _procScheduler.Process();

            using (Profiler.BeginZone("Map Update", color: (uint)Color.LightPink.ToArgb())) {
                UpdateStat();
                _dreamMapManager.UpdateTiles();
            }

            using (Profiler.BeginZone("ByondApi Thread Syncs & Temp Refs"))
                ByondApi.ByondApi.Update();

            using (Profiler.BeginZone("Disk IO", color: (uint)Color.LightPink.ToArgb()))
                DreamObjectSavefile.FlushAllUpdates();

            WorldInstance.Cpu = WorldInstance.TickUsage;
        }

        Profiler.EmitFrameMark();
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

        OptionalErrors = json.OptionalErrors;

        var rootPath = Path.GetFullPath(Path.GetDirectoryName(jsonPath)!);
        var resources = json.Resources ?? Array.Empty<string>();
        _dreamResourceManager.Initialize(rootPath, resources, json.Interface);

        _refManager.Initialize();
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
                var globalJson = jsonGlobals.Globals.GetValueOrDefault(i, null);
                using var globalValue = _objectTree.GetDreamValueFromJsonElement(globalJson);

                SetGlobal(i, globalValue);
            }
        }

        _dreamMapManager.LoadMaps(json.Maps);
        return true;
    }

    public void WriteWorldLog(string message, LogLevel level = LogLevel.Info, string sawmill = "world.log") {
        using var worldLog = WorldInstance.GetVariable("log");
        if (!worldLog.TryGetValueAsDreamResource(out var logRsc)) {
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

    public void HandleException(Exception e, string msg = "", string file = "", int line = 0, bool inWorldError = false) {
        if (string.IsNullOrEmpty(msg)) { // Just print the C# exception if we don't override the message
            msg = e.Message;
        }

        LastDMException = e;
        OnException?.Invoke(this, e);

        // Invoke world.Error()
        var obj = _objectTree.CreateObject<DreamObjectException>(_objectTree.Exception);
        if (e is DMThrowException throwException)
            obj.Name = throwException.Value;
        else
            obj.Name = new DreamValue(e.Message);
        obj.Desc = new DreamValue(msg);
        obj.Line = new DreamValue(line);
        obj.File = new DreamValue(file);
        if (!inWorldError) // if an error occurs in /world/Error(), don't call it again
            WorldInstance.SpawnProc("Error", usr: null, new DreamValue(obj)).Dispose();
        else {
            _sawmill.Error("CRITICAL: An error occurred in /world/Error()");
            WriteWorldLog(msg);
        }

        obj.DecRef();
    }

    public void OptionalException<T>(WarningCode code, string exceptionText) where T : Exception {
        if (OptionalErrors.TryGetValue(code, out var level) && level == ErrorLevel.Error) {
            T exception = (T)Activator.CreateInstance(typeof(T), exceptionText)!;
            throw exception;
        }
    }

    public void SetGlobal(int id, DreamValue value) {
        value.IncRef();
        Globals[id].Dispose();
        Globals[id] = value;
    }
}
