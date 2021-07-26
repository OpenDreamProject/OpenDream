using Content.Server.DM;
using Content.Shared.Dream;
using Content.Shared.Json;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;

namespace Content.Server.Dream {
    class DreamMapManager : IDreamMapManager {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IDreamManager _dreamManager = default!;
        [Dependency] private readonly IAtomManager _atomManager = default!;

        public Vector2i Size { get; private set; }
        public int Levels { get; private set; }

        private Dictionary<MapCoordinates, DreamObject> _turfs = new();

        public void LoadMaps(List<DreamMapJson> maps) {
            if (maps.Count == 0) throw new ArgumentException("No maps were given");
            else if (maps.Count > 1) throw new NotImplementedException("Loading more than one map is not implemented");
            DreamMapJson map = maps[0];

            _mapManager.CreateNewMapEntity(MapId.Nullspace);
            Size = new Vector2i(map.MaxX, map.MaxY);
            Levels = map.MaxZ;

            for (int z = 1; z <= Levels; z++) {
                _mapManager.CreateMap(new MapId(z));
            }

            DreamPath defaultTurf = _dreamManager.WorldInstance.GetVariable("turf").GetValueAsPath();
            //DreamPath defaultArea = _dreamManager.WorldInstance.GetVariable("area").GetValueAsPath();

            foreach (MapBlockJson block in map.Blocks) {
                LoadMapBlock(block, map.CellDefinitions, defaultTurf);
            }
        }

        public void SetTurf(int x, int y, int z, DreamObject turf) {
            _turfs[new MapCoordinates(x, y, new MapId(z))] = turf;

            IEntity entity = _atomManager.GetAtomEntity(turf);

            entity.Transform.AttachParent(_mapManager.GetMapEntity(new MapId(z)));
            entity.Transform.LocalPosition = new Vector2(x, y);

            //TODO: Every reference to the old turf should point to the new one
        }

        public DreamObject GetTurf(int x, int y, int z) {
            _turfs.TryGetValue(new MapCoordinates(x, y, new MapId(z)), out DreamObject turf);

            return turf;
        }

        private void LoadMapBlock(MapBlockJson block, Dictionary<string, CellDefinitionJson> cellDefinitions, DreamPath defaultTurf) {
            int blockX = 1;
            int blockY = 1;

            foreach (string cell in block.Cells) {
                CellDefinitionJson cellDefinition = cellDefinitions[cell];
                DreamObject turf = CreateMapObject(cellDefinition.Turf);
                //DreamObject area = GetMapLoaderArea(cellDefinition.Area?.Type ?? defaultArea);

                if (turf == null) {
                    turf = _dreamManager.ObjectTree.CreateObject(defaultTurf);
                    turf.InitSpawn(new DreamProcArguments(null));
                }

                int x = block.X + blockX - 1;
                int y = block.Y + block.Height - blockY;

                SetTurf(x, y, block.Z, turf);
                //SetArea(x, y, block.Z, area);
                foreach (MapObjectJson mapObject in cellDefinition.Objects) {
                    CreateMapObject(mapObject, turf);
                }

                blockX++;
                if (blockX > block.Width) {
                    blockX = 1;
                    blockY++;
                }
            }
        }

        private DreamObject CreateMapObject(MapObjectJson mapObject, DreamObject loc = null) {
            if (!_dreamManager.ObjectTree.HasTreeEntry(mapObject.Type)) {
                Logger.Warning("MAP LOAD: Skipping " + mapObject.Type);

                return null;
            }

            DreamObjectTree.DreamObjectTreeEntry objectTreeEntry = _dreamManager.ObjectTree.GetTreeEntryFromPath(mapObject.Type);
            DreamObjectDefinition definition = objectTreeEntry.ObjectDefinition;
            if (mapObject.VarOverrides?.Count > 0) {
                definition = new DreamObjectDefinition(definition);

                foreach (KeyValuePair<string, object> varOverride in mapObject.VarOverrides) {
                    if (definition.HasVariable(varOverride.Key)) {
                        definition.Variables[varOverride.Key] = _dreamManager.ObjectTree.GetDreamValueFromJsonElement(varOverride.Value);
                    }
                }
            }

            var obj = new DreamObject(definition);
            obj.InitSpawn(new DreamProcArguments(new() { new DreamValue(loc) }));
            return obj;
        }
    }
}
