﻿using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
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
        [Dependency] private readonly DreamResourceManager _dreamResourceManager = default!;

        private DreamCompiledJson _compiledJson;

        public DreamObjectTree ObjectTree { get; private set; }
        public DreamObject WorldInstance { get; private set; }
        public int DMExceptionCount { get; set; }

        // Global state that may not really (really really) belong here
        public List<DreamValue> Globals { get; set; } = new();
        public Dictionary<string, DreamProc> GlobalProcs { get; set; } = new();
        public DreamList WorldContentsList { get; set; }
        public Dictionary<DreamObject, DreamList> AreaContents { get; set; } = new();
        public Dictionary<DreamObject, int> ReferenceIDs { get; set; } = new();
        public List<DreamObject> Mobs { get; set; } = new();
        public Random Random { get; set; } = new();

        public void Initialize() {
            InitializeConnectionManager();

            DreamCompiledJson json = LoadJson();
            if (json == null)
                return;

            _compiledJson = json;

            _dreamResourceManager.Initialize();

            ObjectTree = new DreamObjectTree(json);
            SetMetaObjects();

            if (_compiledJson.GlobalProcs != null) {
                foreach (var procJson in _compiledJson.GlobalProcs) {
                    GlobalProcs.Add(procJson.Key, ObjectTree.LoadProcJson(procJson.Key, procJson.Value));
                }
            }

            DreamProcNative.SetupNativeProcs(ObjectTree);

            _dreamMapManager.Initialize();
            WorldInstance = ObjectTree.CreateObject(DreamPath.World);
            WorldInstance.InitSpawn(new DreamProcArguments(null));

            if (_compiledJson.Globals != null) {
                var jsonGlobals = _compiledJson.Globals;
                Globals.EnsureCapacity(jsonGlobals.GlobalCount);

                for (int i = 0; i < jsonGlobals.GlobalCount; i++) {
                    object globalValue = jsonGlobals.Globals.GetValueOrDefault(i, null);
                    Globals.Add(ObjectTree.GetDreamValueFromJsonElement(globalValue));
                }
            }

            //The first global is always `world`
            Globals[0] = new DreamValue(WorldInstance);

            if (json.GlobalInitProc != null) {
                var globalInitProc = new DMProc("(global init)", null, null, null, json.GlobalInitProc.Bytecode, json.GlobalInitProc.MaxStackSize, true);
                globalInitProc.Spawn(WorldInstance, new DreamProcArguments(new(), new()));
            }

            _dreamMapManager.LoadMaps(json.Maps);
            WorldInstance.SpawnProc("New");
        }

        public void Shutdown() {

        }

        public void Update()
        {
            UpdateStat();
        }

        private DreamCompiledJson LoadJson() {
            string jsonPath = _configManager.GetCVar<string>(OpenDreamCVars.JsonPath);
            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath)) {
                Logger.Fatal("Error while loading the compiled json. The opendream.json_path CVar may be empty, or points to a file that doesn't exist");
                IoCManager.Resolve<ITaskManager>().RunOnMainThread(() => { IoCManager.Resolve<IBaseServer>().Shutdown("Error while loading the compiled json. The opendream.json_path CVar may be empty, or points to a file that doesn't exist"); });
                return null;
            }

            string jsonSource = File.ReadAllText(jsonPath);
            return JsonSerializer.Deserialize<DreamCompiledJson>(jsonSource);
        }

        private void SetMetaObjects() {
            ObjectTree.SetMetaObject(DreamPath.Root, new DreamMetaObjectRoot());
            ObjectTree.SetMetaObject(DreamPath.List, new DreamMetaObjectList());
            ObjectTree.SetMetaObject(DreamPath.Client, new DreamMetaObjectClient());
            ObjectTree.SetMetaObject(DreamPath.World, new DreamMetaObjectWorld());
            ObjectTree.SetMetaObject(DreamPath.Datum, new DreamMetaObjectDatum());
            ObjectTree.SetMetaObject(DreamPath.Matrix, new DreamMetaObjectMatrix());
            ObjectTree.SetMetaObject(DreamPath.Regex, new DreamMetaObjectRegex());
            ObjectTree.SetMetaObject(DreamPath.Atom, new DreamMetaObjectAtom());
            ObjectTree.SetMetaObject(DreamPath.Area, new DreamMetaObjectArea());
            ObjectTree.SetMetaObject(DreamPath.Turf, new DreamMetaObjectTurf());
            ObjectTree.SetMetaObject(DreamPath.Movable, new DreamMetaObjectMovable());
            ObjectTree.SetMetaObject(DreamPath.Mob, new DreamMetaObjectMob());
        }

        public void SetGlobalNativeProc(NativeProc.HandlerFn func) {
            var (name, defaultArgumentValues, argumentNames) = NativeProc.GetNativeInfo(func);
            var proc = new NativeProc(name, null, argumentNames, null, defaultArgumentValues, func);

            GlobalProcs[name] = proc;
        }

        public void SetGlobalNativeProc(Func<AsyncNativeProc.State, Task<DreamValue>> func) {
            var (name, defaultArgumentValues, argumentNames) = NativeProc.GetNativeInfo(func);
            var proc = new AsyncNativeProc(name, null, argumentNames, null, defaultArgumentValues, func);

            GlobalProcs[name] = proc;
        }

        public void WriteWorldLog(string message, LogLevel level = LogLevel.Info)
        {
            if (!WorldInstance.GetVariable("log").TryGetValueAsDreamResource(out var logRsc))
            {
                logRsc = new ConsoleOutputResource();
                WorldInstance.SetVariableValue("log", new DreamValue(logRsc));
                Logger.Log(LogLevel.Error, $"Failed to write to the world log, falling back to console output. Original log message follows: [{LogMessage.LogLevelToName(level)}] world.log: {message}");
            }

            if (logRsc is ConsoleOutputResource) // Output() on ConsoleOutputResource uses LogLevel.Info
            {
                Logger.LogS(level, "world.log", message);
            }
            else
            {
                logRsc.Output(new DreamValue($"[{LogMessage.LogLevelToName(level)}] world.log: {message}"));
            }
        }
    }
}
