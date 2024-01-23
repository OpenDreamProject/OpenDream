using System.IO;
using System.Linq;
using System.Text.Json;
using DMCompiler.Bytecode;
using DMCompiler.Json;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;
using OpenDreamShared;
using OpenDreamShared.Dream;
using Robust.Server;
using Robust.Server.Player;
using Robust.Server.ServerStatus;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace OpenDreamRuntime {
    public sealed partial class DreamManager {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
        [Dependency] private readonly ProcScheduler _procScheduler = default!;
        [Dependency] private readonly DreamResourceManager _dreamResourceManager = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly DreamObjectTree _objectTree = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IStatusHost _statusHost = default!;
        [Dependency] private readonly IDependencyCollection _dependencyCollection = default!;

        private ServerAppearanceSystem? _appearanceSystem;

        public DreamObjectWorld WorldInstance { get; private set; }
        public Exception? LastDMException { get; set; }

        public event EventHandler<Exception>? OnException;

        // Global state that may not really (really really) belong here
        public DreamValue[] Globals { get; set; } = Array.Empty<DreamValue>();
        public List<string> GlobalNames { get; private set; } = new();
        public Dictionary<DreamObject, int> ReferenceIDs { get; } = new();
        public Dictionary<int, DreamObject> ReferenceIDsToDreamObject { get; } = new();
        public HashSet<DreamObject> Clients { get; set; } = new();
        public HashSet<DreamObject> Datums { get; set; } = new();
        public Random Random { get; set; } = new();
        public Dictionary<string, List<DreamObject>> Tags { get; set; } = new();
        public DreamProc ImageConstructor, ImageFactoryProc;
        private int _dreamObjectRefIdCounter;

        private DreamCompiledJson _compiledJson;
        public bool Initialized { get; private set; }
        public GameTick InitializedTick { get; private set; }

        private ISawmill _sawmill = default!;

        //TODO This arg is awful and temporary until RT supports cvar overrides in unit tests
        public void PreInitialize(string? jsonPath) {
            _sawmill = Logger.GetSawmill("opendream");

            InitializeConnectionManager();
            _dreamResourceManager.PreInitialize();

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
            _dreamMapManager.InitializeAtoms(_compiledJson.Maps);

            // Call world.New()
            WorldInstance.SpawnProc("New");
        }

        public void Shutdown() {
            // TODO: Respect not calling parent and aborting shutdown
            WorldInstance.Delete();
            ShutdownConnectionManager();
            Initialized = false;
        }

        public void Update() {
            if (!Initialized)
                return;

            _procScheduler.Process();
            UpdateStat();
            _dreamMapManager.UpdateTiles();
            DreamObjectSavefile.FlushAllUpdates();
            WorldInstance.SetVariableValue("cpu", WorldInstance.GetVariable("tick_usage"));
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

            _compiledJson = json;
            var rootPath = Path.GetFullPath(Path.GetDirectoryName(jsonPath)!);
            var resources = _compiledJson.Resources ?? Array.Empty<string>();
            _dreamResourceManager.Initialize(rootPath, resources);
            if(!string.IsNullOrEmpty(_compiledJson.Interface) && !_dreamResourceManager.DoesFileExist(_compiledJson.Interface))
                throw new FileNotFoundException("Interface DMF not found at "+Path.Join(rootPath,_compiledJson.Interface));

            _objectTree.LoadJson(json);

            DreamProcNative.SetupNativeProcs(_objectTree);
            ImageConstructor = _objectTree.Image.ObjectDefinition.GetProc("New");
            _objectTree.TryGetGlobalProc("image", out ImageFactoryProc!);

            _dreamMapManager.Initialize();
            WorldInstance = new DreamObjectWorld(_objectTree.World.ObjectDefinition);

            // Call /world/<init>. This is an IMPLEMENTATION DETAIL and non-DMStandard should NOT be run here.
            WorldInstance.InitSpawn(new());

            if (_compiledJson.Globals is GlobalListJson jsonGlobals) {
                Globals = new DreamValue[jsonGlobals.GlobalCount];
                GlobalNames = jsonGlobals.Names;

                for (int i = 0; i < jsonGlobals.GlobalCount; i++) {
                    object globalValue = jsonGlobals.Globals.GetValueOrDefault(i, null);
                    Globals[i] = _objectTree.GetDreamValueFromJsonElement(globalValue);
                }
            }

            Globals[GlobalNames.IndexOf("world")] = new DreamValue(WorldInstance);

            _dreamMapManager.LoadMaps(_compiledJson.Maps);

            var aczProvider = new DreamAczProvider(_dependencyCollection, rootPath, resources);
            _statusHost.SetMagicAczProvider(aczProvider);
            _statusHost.SetFullHybridAczProvider(aczProvider);

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

                if (_configManager.GetCVar(OpenDreamCVars.AlwaysShowExceptions)) {
                    Logger.GetSawmill(sawmill).Log(level, message);
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
                    switch(refObject){
                        case DreamObjectTurf: refType = RefType.DreamObjectTurf; break;
                        case DreamObjectMob: refType = RefType.DreamObjectMob; break;
                        case DreamObjectArea: refType = RefType.DreamObjectArea; break;
                        case DreamObjectClient: refType = RefType.DreamObjectArea; break;
                        case DreamObjectImage: refType = RefType.DreamObjectImage; break;
                        default: {
                            refType = RefType.DreamObjectDatum;
                            if(refObject.IsSubtypeOf(_objectTree.Obj))
                                refType = RefType.DreamObject;
                            else if (refObject.GetType() == typeof(DreamList))
                                refType = RefType.DreamObjectList;
                            break;
                        }
                    }
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

            // The first digit is the type
            return $"[0x{((int) refType+idx):x}]";
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
                // The first one/two digits give the type, the last 6 give the index
                var typeId = (RefType) (refId & 0xFF000000);
                refId = (refId & 0x00FFFFFF); // The ref minus its ref type prefix

                switch (typeId) {
                    case RefType.Null:
                        return DreamValue.Null;
                    case RefType.DreamObjectArea:
                    case RefType.DreamObjectClient:
                    case RefType.DreamObjectDatum:
                    case RefType.DreamObjectImage:
                    case RefType.DreamObjectList:
                    case RefType.DreamObjectMob:
                    case RefType.DreamObjectTurf:
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
                        return _appearanceSystem.TryGetAppearance(refId, out IconAppearance? appearance)
                            ? new DreamValue(appearance)
                            : DreamValue.Null;
                    case RefType.Proc:
                        return new(_objectTree.Procs[refId]);
                    default:
                        throw new Exception($"Invalid reference type for ref {refString}");
                }
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

        public void HandleException(Exception e, string msg = "", string file = "", int line = 0) {
            if (string.IsNullOrEmpty(msg)) { // Just print the C# exception if we don't override the message
                msg = e.Message;
            }

            LastDMException = e;
            OnException?.Invoke(this, e);

            // Invoke world.Error()
            var obj =_objectTree.CreateObject<DreamObjectException>(_objectTree.Exception);
            obj.Name = e.Message;
            obj.Description = msg;
            obj.Line = line;
            obj.File = file;

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
        DreamObjectImage = 0xD000000,
        DreamObjectList = 0xF000000,
        DreamObjectDatum = 0x21000000,
        String = 0x6000000,
        DreamType = 0x9000000, //in byond type is from 0x8 to 0xb, but fuck that
        DreamResource = 0x27000000, //Equivalent to file
        DreamAppearance = 0x3A000000,
        Proc = 0x26000000
    }
}
