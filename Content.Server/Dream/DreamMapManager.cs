using Content.Server.DM;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using System.Collections.Generic;

namespace Content.Server.Dream {
    class DreamMapManager : IDreamMapManager {
        [Dependency] IMapManager _mapManager = null;
        [Dependency] IDreamManager _dreamManager = null;
        [Dependency] IAtomManager _atomManager = null;

        private Dictionary<MapCoordinates, DreamObject> _turfs = new();

        public void LoadMap(string dmmFilePath) {
            _mapManager.CreateNewMapEntity(MapId.Nullspace);
            _mapManager.CreateMap(new MapId(1));

            for (int x = 1; x <= 10; x++) {
                for (int y = 1; y <= 10; y++) {
                    DreamObject turf = _dreamManager.ObjectTree.CreateObject(DreamPath.Turf);
                    turf.InitSpawn(new DreamProcArguments(new() { DreamValue.Null }));
                    turf.SetVariable("icon", new DreamValue("turf.png"));
                    SetTurf(x, y, 1, turf);
                }
            }
        }

        public void SetTurf(int x, int y, int z, DreamObject turf) {
            _turfs[new MapCoordinates(x, y, new MapId(z))] = turf;

            IEntity entity = _atomManager.GetAtomEntity(turf);

            entity.Transform.AttachParent(_mapManager.GetMapEntity(new MapId(z)));
            entity.Transform.LocalPosition = new Vector2(x, y);
        }

        public DreamObject GetTurf(int x, int y, int z) {
            _turfs.TryGetValue(new MapCoordinates(x, y, new MapId(z)), out DreamObject turf);

            return turf;
        }
    }
}
