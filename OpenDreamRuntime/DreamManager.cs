using System.IO;
using System.Linq;
using System.Text.Json;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using OpenDreamShared;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using Robust.Server;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace OpenDreamRuntime {
    internal partial class DreamManager : IDreamManager {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
        [Dependency] private readonly IProcScheduler _procScheduler = default!;
        [Dependency] private readonly DreamResourceManager _dreamResourceManager = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        private ServerAppearanceSystem? _appearanceSystem;

        public DreamObjectWorld WorldInstance { get; private set; }
        public Exception? LastDMException { get; set; }

        public event EventHandler<Exception>? OnException;

        // Global state that may not really (really really) belong here
        public List<DreamValue> Globals { get; set; } = new();
        public IReadOnlyList<string> GlobalNames { get; private set; } = new List<string>();
        public Dictionary<DreamObject, int> ReferenceIDs { get; } = new();
        public Dictionary<int, DreamObject> ReferenceIDsToDreamObject { get; } = new();
        public HashSet<DreamObject> Clients { get; set; } = new();
        public HashSet<DreamObject> Datums { get; set; } = new();
        public Random Random { get; set; } = new();
        public Dictionary<string, List<DreamObject>> Tags { get; set; } = new();
        private int _dreamObjectRefIdCounter;

        private DreamCompiledJson _compiledJson;
        public bool Initialized { get; private set; }
        public GameTick InitializedTick { get; private set; }

        //TODO This arg is awful and temporary until RT supports cvar overrides in unit tests
        public void PreInitialize(string? jsonPath) {
            InitializeConnectionManager();
            _dreamResourceManager.Initialize();

            if (!LoadJson(jsonPath)) {
                _taskManager.RunOnMainThread(() => { IoCManager.Resolve<IBaseServer>().Shutdown("Error while loading the compiled json. The opendream.json_path CVar may be empty, or points to a file that doesn't exist"); });
            }
        }

        public void StartWorld() {
            // It is now OK to call user code, like /New procs.
            Initialized = true;
            InitializedTick = _gameTiming.CurTick;

            // Call global <init> with waitfor=FALSE
            _objectTree.GlobalInitProc?.Spawn(WorldInstance, new());

            // Call New() on all /area and /turf that exist, each with waitfor=FALSE separately. If <global init> created any /area, call New a SECOND TIME
            // new() up /objs and /mobs from compiled-in maps [order: (1,1) then (2,1) then (1,2) then (2,2)]
            _dreamMapManager.InitializeAtoms(_compiledJson.Maps![0]);

            // Call world.New()
            WorldInstance.SpawnProc("New");
        }

        public void Shutdown() {
            Initialized = false;
        }

        public void Update() {
            if (!Initialized)
                return;

            _procScheduler.Process();
            UpdateStat();
            _dreamMapManager.UpdateTiles();

            WorldInstance.SetVariableValue("cpu", WorldInstance.GetVariable("tick_usage"));
        }

        public bool LoadJson(string? jsonPath) {
            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
                return false;

            string jsonSource = File.ReadAllText(jsonPath);
            DreamCompiledJson? json = JsonSerializer.Deserialize<DreamCompiledJson>(jsonSource);
            if (json == null)
                return false;

            if (json.Maps == null || json.Maps.Count == 0) throw new ArgumentException("No maps were given");
            if (json.Maps.Count > 1) {
                Logger.Warning("Loading more than one map is not implemented, skipping additional maps");
            }

            _compiledJson = json;
            _dreamResourceManager.SetDirectory(Path.GetDirectoryName(jsonPath));
            if(!string.IsNullOrEmpty(_compiledJson.Interface) && !_dreamResourceManager.DoesFileExist(_compiledJson.Interface))
                throw new FileNotFoundException("Interface DMF not found at "+Path.Join(Path.GetDirectoryName(jsonPath),_compiledJson.Interface));

            _objectTree.LoadJson(json);

            DreamProcNative.SetupNativeProcs(_objectTree);

            _dreamMapManager.Initialize();
            WorldInstance = new DreamObjectWorld(_objectTree.World.ObjectDefinition);

            // Call /world/<init>. This is an IMPLEMENTATION DETAIL and non-DMStandard should NOT be run here.
            WorldInstance.InitSpawn(new());

            if (_compiledJson.Globals is GlobalListJson jsonGlobals) {
                Globals.Clear();
                Globals.EnsureCapacity(jsonGlobals.GlobalCount);
                GlobalNames = jsonGlobals.Names;

                for (int i = 0; i < jsonGlobals.GlobalCount; i++) {
                    object globalValue = jsonGlobals.Globals.GetValueOrDefault(i, null);
                    Globals.Add(_objectTree.GetDreamValueFromJsonElement(globalValue));
                }
            }

            // The first global is always `world`.
            Globals[0] = new DreamValue(WorldInstance);

            // Load turfs and areas of compiled-in maps, recursively calling <init>, but suppressing all New
            _dreamMapManager.LoadAreasAndTurfs(_compiledJson.Maps[0]);

            return true;
        }

        public void WriteWorldLog(string message, LogLevel level = LogLevel.Info, string sawmill = "world.log") {
            if (!WorldInstance.GetVariable("log").TryGetValueAsDreamResource(out var logRsc)) {
                logRsc = new ConsoleOutputResource();
                WorldInstance.SetVariableValue("log", new DreamValue(logRsc));
                Logger.Log(LogLevel.Error, $"Failed to write to the world log, falling back to console output. Original log message follows: [{LogMessage.LogLevelToName(level)}] world.log: {message}");
            }

            if (logRsc is ConsoleOutputResource consoleOut) { // Output() on ConsoleOutputResource uses LogLevel.Info
                consoleOut.WriteConsole(level, sawmill, message);
            } else {
                logRsc.Output(new DreamValue($"[{LogMessage.LogLevelToName(level)}] {sawmill}: {message}"));

                if (_configManager.GetCVar(OpenDreamCVars.AlwaysShowExceptions)) {
                    Logger.LogS(level, sawmill, message);
                }
            }
        }

        public string CreateRef(DreamValue value) {
            RefType refType;
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

                    refType = RefType.DreamObject;
                    if (!ReferenceIDs.TryGetValue(refObject, out idx)) {
                        idx = _dreamObjectRefIdCounter++;
                        ReferenceIDs.Add(refObject, idx);
                        ReferenceIDsToDreamObject.Add(idx, refObject);
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
                idx = (int)_appearanceSystem.AddAppearance(appearance);
            } else if (value.TryGetValueAsDreamResource(out var refRsc)) {
                refType = RefType.DreamResource;
                idx = refRsc.Id;
            }  else if (value.TryGetValueAsProc(out var proc)) {
                refType = RefType.Proc;
                idx = proc.Id;
            } else {
                throw new NotImplementedException($"Ref for {value} is unimplemented");
            }

            // The first digit is the type, i.e. 1 for objects and 2 for strings
            return $"{(int) refType}{idx}";
        }

        public DreamValue LocateRef(string refString) {
            if (!int.TryParse(refString, out var refId)) {
                // If the ref is not an integer, it may be a tag
                if (Tags.TryGetValue(refString, out var tagList)) {
                    return new DreamValue(tagList.First());
                }

                return DreamValue.Null;
            }

            // The first digit is the type
            var typeId = (RefType) int.Parse(refString.Substring(0, 1));
            var untypedRefString = refString.Substring(1); // The ref minus its ref type prefix

            refId = int.Parse(untypedRefString);

            switch (typeId) {
                case RefType.Null:
                    return DreamValue.Null;
                case RefType.DreamObject:
                    if (ReferenceIDsToDreamObject.TryGetValue(refId, out var dreamObject))
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
                case RefType.DreamResource:
                    if (!_dreamResourceManager.TryLoadResource(refId, out var resource))
                        return DreamValue.Null;

                    return new DreamValue(resource);
                case RefType.DreamAppearance:
                    _appearanceSystem ??= _entitySystemManager.GetEntitySystem<ServerAppearanceSystem>();
                    return _appearanceSystem.TryGetAppearance((uint)refId, out IconAppearance? appearance)
                        ? new DreamValue(appearance)
                        : DreamValue.Null;
                case RefType.Proc:
                    return new(_objectTree.Procs[refId]);
                default:
                    throw new Exception($"Invalid reference type for ref {refString}");
            }
        }

        public void HandleException(Exception e) {
            LastDMException = e;
            OnException?.Invoke(this, e);
        }
    }
}
