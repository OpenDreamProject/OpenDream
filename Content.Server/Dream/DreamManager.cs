using Content.Server.DM;
using Content.Server.Dream.MetaObjects;
using Content.Server.Dream.NativeProcs;
using Content.Shared.Dream;
using Content.Shared.Json;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Content.Server.Dream {
    class DreamManager : IDreamManager {
        [Dependency] IConfigurationManager _configManager = null;
        [Dependency] IPlayerManager _playerManager = null;
        [Dependency] IDreamMapManager _dreamMapManager = null;

        public DreamObjectTree ObjectTree { get; private set; }
        public int DMExceptionCount { get; set; }

        public DreamList WorldContentsList { get; set; }

        private Dictionary<DreamObject, NetUserId> _clientToUserId = new();

        public void Initialize() {
            string jsonFile = _configManager.GetCVar<string>("opendream.json");
            string jsonSource = File.ReadAllText(jsonFile);
            DreamCompiledJson json = JsonSerializer.Deserialize<DreamCompiledJson>(jsonSource);
            ObjectTree = new DreamObjectTree(json);
            SetMetaObjects();
            DreamProcNative.SetupNativeProcs(ObjectTree);

            DreamObject world = ObjectTree.CreateObject(DreamPath.World);
            ObjectTree.Root.ObjectDefinition.GlobalVariables["world"].Value = new DreamValue(world);
            world.InitSpawn(new DreamProcArguments(null));

            _dreamMapManager.LoadMap(null);
            world.SpawnProc("New");

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        public void Shutdown() {

        }

        public IPlayerSession GetSessionFromClient(DreamObject client) {
            return _playerManager.GetSessionByUserId(_clientToUserId[client]);
        }

        private void SetMetaObjects() {
            ObjectTree.SetMetaObject(DreamPath.Root, new DreamMetaObjectRoot());
            ObjectTree.SetMetaObject(DreamPath.List, new DreamMetaObjectList());
            ObjectTree.SetMetaObject(DreamPath.Client, new DreamMetaObjectClient());
            ObjectTree.SetMetaObject(DreamPath.World, new DreamMetaObjectWorld());
            ObjectTree.SetMetaObject(DreamPath.Datum, new DreamMetaObjectDatum());
            ObjectTree.SetMetaObject(DreamPath.Atom, new DreamMetaObjectAtom());
            ObjectTree.SetMetaObject(DreamPath.Movable, new DreamMetaObjectMovable());
        }

        private void OnPlayerStatusChanged(object sender, SessionStatusEventArgs e) {
            switch (e.NewStatus) {
                case SessionStatus.Connected:
                    e.Session.JoinGame();
                    break;
                case SessionStatus.InGame: {
                    DreamObject client = ObjectTree.CreateObject(DreamPath.Client);
                    _clientToUserId[client] = e.Session.UserId;
                    client.InitSpawn(new DreamProcArguments(new() { DreamValue.Null }));

                    break;
                }
            }
        }
    }
}
