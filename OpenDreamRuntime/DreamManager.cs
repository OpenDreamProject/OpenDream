using System.IO;
using System.Linq;
using System.Text.Json;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Resources;
using OpenDreamShared;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using Robust.Server;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;

namespace OpenDreamRuntime {
    partial class DreamManager : IDreamManager {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
        [Dependency] private readonly IProcScheduler _procScheduler = default!;
        [Dependency] private readonly DreamResourceManager _dreamResourceManager = default!;


        public DreamObjectTree ObjectTree { get; private set; } = new();
        public DreamObject WorldInstance { get; private set; }
        public Exception? LastDMException { get; set; }

        // Global state that may not really (really really) belong here
        public List<DreamValue> Globals { get; set; } = new();
        public DreamList WorldContentsList { get; set; }
        public Dictionary<DreamObject, DreamList> AreaContents { get; set; } = new();
        public Dictionary<DreamObject, int> ReferenceIDs { get; set; } = new();
        public List<DreamObject> Mobs { get; set; } = new();
        public List<DreamObject> Clients { get; set; } = new();
        public List<DreamObject> Datums { get; set; } = new();
        public Random Random { get; set; } = new();
        public Dictionary<string, List<DreamObject>> Tags { get; set; } = new();

        private DreamCompiledJson _compiledJson;

        //TODO This arg is awful and temporary until RT supports cvar overrides in unit tests
        public void Initialize(string jsonPath) {
            InitializeConnectionManager();
            _dreamResourceManager.Initialize(jsonPath);

            if (!LoadJson(jsonPath)) {
                IoCManager.Resolve<ITaskManager>().RunOnMainThread(() => { IoCManager.Resolve<IBaseServer>().Shutdown("Error while loading the compiled json. The opendream.json_path CVar may be empty, or points to a file that doesn't exist"); });
                return;
            }

            //TODO: Move to LoadJson()
            _dreamMapManager.LoadMaps(_compiledJson.Maps);
            WorldInstance.SpawnProc("New");
        }

        public void Shutdown() {

        }

        public void Update()
        {
            _procScheduler.Process();
            UpdateStat();
            _dreamMapManager.UpdateTiles();

            WorldInstance.SetVariableValue("cpu", WorldInstance.GetVariable("tick_usage"));
        }

        public bool LoadJson(string? jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
                return false;

            string jsonSource = File.ReadAllText(jsonPath);
            DreamCompiledJson? json = JsonSerializer.Deserialize<DreamCompiledJson>(jsonSource);
            if (json == null)
                return false;

            _compiledJson = json;
            _dreamResourceManager.SetDirectory(Path.GetDirectoryName(jsonPath));
            if(!string.IsNullOrEmpty(_compiledJson.Interface) && !_dreamResourceManager.DoesFileExist(_compiledJson.Interface))
                throw new FileNotFoundException("Interface DMF not found at "+Path.Join(Path.GetDirectoryName(jsonPath),_compiledJson.Interface));
            //TODO: Empty or invalid _compiledJson.Interface should return default interface - see issue #851
            ObjectTree.LoadJson(json);

            SetMetaObjects();

            DreamProcNative.SetupNativeProcs(ObjectTree);

            _dreamMapManager.Initialize();
            WorldInstance = ObjectTree.CreateObject(DreamPath.World);
            WorldInstance.InitSpawn(new DreamProcArguments(null));

            if (_compiledJson.Globals != null) {
                var jsonGlobals = _compiledJson.Globals;
                Globals.Clear();
                Globals.EnsureCapacity(jsonGlobals.GlobalCount);

                for (int i = 0; i < jsonGlobals.GlobalCount; i++) {
                    object globalValue = jsonGlobals.Globals.GetValueOrDefault(i, null);
                    Globals.Add(ObjectTree.GetDreamValueFromJsonElement(globalValue));
                }
            }

            //The first global is always `world`
            Globals[0] = new DreamValue(WorldInstance);

            if (json.GlobalInitProc != null) {
                var globalInitProc = new DMProc(DreamPath.Root, "(global init)", null, null, null, json.GlobalInitProc.Bytecode, json.GlobalInitProc.MaxStackSize, json.GlobalInitProc.Attributes, json.GlobalInitProc.VerbName, json.GlobalInitProc.VerbCategory, json.GlobalInitProc.VerbDesc, json.GlobalInitProc.Invisibility);
                globalInitProc.Spawn(WorldInstance, new DreamProcArguments(new(), new()));
            }

            return true;
        }

        private void SetMetaObjects() {
            // Datum needs to be set first
            ObjectTree.SetMetaObject(DreamPath.Datum, new DreamMetaObjectDatum());

            //TODO Investigate what types BYOND can reparent without exploding and only allow reparenting those
            ObjectTree.SetMetaObject(DreamPath.List, new DreamMetaObjectList());
            ObjectTree.SetMetaObject(DreamPath.Client, new DreamMetaObjectClient());
            ObjectTree.SetMetaObject(DreamPath.World, new DreamMetaObjectWorld());
            ObjectTree.SetMetaObject(DreamPath.Matrix, new DreamMetaObjectMatrix());
            ObjectTree.SetMetaObject(DreamPath.Regex, new DreamMetaObjectRegex());
            ObjectTree.SetMetaObject(DreamPath.Atom, new DreamMetaObjectAtom());
            ObjectTree.SetMetaObject(DreamPath.Area, new DreamMetaObjectArea());
            ObjectTree.SetMetaObject(DreamPath.Turf, new DreamMetaObjectTurf());
            ObjectTree.SetMetaObject(DreamPath.Movable, new DreamMetaObjectMovable());
            ObjectTree.SetMetaObject(DreamPath.Mob, new DreamMetaObjectMob());
            ObjectTree.SetMetaObject(DreamPath.Icon, new DreamMetaObjectIcon());
            ObjectTree.SetMetaObject(DreamPath.Savefile, new DreamMetaObjectSavefile());
        }

        public void WriteWorldLog(string message, LogLevel level = LogLevel.Info, string sawmill = "world.log") {
            if (!WorldInstance.GetVariable("log").TryGetValueAsDreamResource(out var logRsc)) {
                logRsc = new ConsoleOutputResource();
                WorldInstance.SetVariableValue("log", new DreamValue(logRsc));
                Logger.Log(LogLevel.Error, $"Failed to write to the world log, falling back to console output. Original log message follows: [{LogMessage.LogLevelToName(level)}] world.log: {message}");
            }

            if (logRsc is ConsoleOutputResource) // Output() on ConsoleOutputResource uses LogLevel.Info
            {
                Logger.LogS(level, sawmill, message);
            }
            else
            {
                logRsc.Output(new DreamValue($"[{LogMessage.LogLevelToName(level)}] {sawmill}: {message}"));
                if (_configManager.GetCVar(OpenDreamCVars.AlwaysShowExceptions))
                {
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
                        idx = ReferenceIDs.Count;
                        ReferenceIDs.Add(refObject, idx);
                    }
                }
            } else if (value.TryGetValueAsString(out var refStr)) {
                refType = RefType.String;
                idx = ObjectTree.Strings.IndexOf(refStr);

                if (idx == -1) {
                    ObjectTree.Strings.Add(refStr);
                    idx = ObjectTree.Strings.Count - 1;
                }
            } else if (value.TryGetValueAsPath(out var refPath)) {
                var treeEntry = ObjectTree.GetTreeEntry(refPath);

                refType = RefType.DreamPath;
                idx = treeEntry.Id;
            } else if (value.TryGetValueAsDreamResource(out var refRsc)) {
                // Bit of a hack. This should use a resource's ID once they are refactored to have them.
                return $"{(int) RefType.DreamResource}{refRsc.ResourcePath}";
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

            if (typeId == RefType.DreamResource) {
                // DreamResource refs are a little special and use their path instead of an id
                return new DreamValue(_dreamResourceManager.LoadResource(untypedRefString));
            } else {
                refId = int.Parse(untypedRefString);

                switch (typeId) {
                    case RefType.Null:
                        return DreamValue.Null;
                    case RefType.DreamObject:
                        foreach (KeyValuePair<DreamObject, int> referenceIdPair in ReferenceIDs) {
                            if (referenceIdPair.Value == refId) return new DreamValue(referenceIdPair.Key);
                        }

                        return DreamValue.Null;
                    case RefType.String:
                        return ObjectTree.Strings.Count > refId
                            ? new DreamValue(ObjectTree.Strings[refId])
                            : DreamValue.Null;
                    case RefType.DreamPath:
                        return ObjectTree.Types.Length > refId
                            ? new DreamValue(ObjectTree.Types[refId].Path)
                            : DreamValue.Null;
                    default:
                        throw new Exception($"Invalid reference type for ref {refString}");
                }
            }
        }
    }
}
