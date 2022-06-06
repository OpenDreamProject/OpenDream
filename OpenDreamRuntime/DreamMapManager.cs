using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using Robust.Shared.Map;

namespace OpenDreamRuntime {
    sealed class DreamMapManager : IDreamMapManager {
        public struct Cell {
            public DreamObject Turf;
            public DreamObject Area;
        };

        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IDreamManager _dreamManager = default!;
        [Dependency] private readonly IAtomManager _atomManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public Vector2i Size { get; private set; }
        public int Levels { get => _levels.Count; }

        private List<Cell[,]> _levels = new();
        private Dictionary<DreamPath, DreamObject> _areas = new();
        private DreamPath _defaultArea, _defaultTurf;

        public void Initialize() {
            _mapManager.CreateNewMapEntity(MapId.Nullspace);

            DreamObjectDefinition worldDefinition = _dreamManager.ObjectTree.GetObjectDefinition(DreamPath.World);

            // Default area
            if (worldDefinition.Variables["area"].TryGetValueAsPath(out var area))
            {

                if(!_dreamManager.ObjectTree.GetObjectDefinition(area).IsSubtypeOf(DreamPath.Area)) throw new Exception("bad area");
                _defaultArea = area;

            }
            else if (worldDefinition.Variables["area"] == DreamValue.Null ||
                     worldDefinition.Variables["area"].TryGetValueAsInteger(out var areaInt) && areaInt == 0)
            {
                //TODO: Properly handle disabling default area
                _defaultArea = DreamPath.Area;
            }
            else
            {
                throw new Exception("bad area");
            }

            //Default turf
            if (worldDefinition.Variables["turf"].TryGetValueAsPath(out var turf))
            {
                if(!_dreamManager.ObjectTree.GetObjectDefinition(turf).IsSubtypeOf(DreamPath.Turf)) throw new Exception("bad turf");
                _defaultTurf = turf;
            }
            else if (worldDefinition.Variables["turf"] == DreamValue.Null ||
                     worldDefinition.Variables["turf"].TryGetValueAsInteger(out var turfInt) && turfInt == 0)
            {
                //TODO: Properly handle disabling default turf
                _defaultTurf = DreamPath.Turf;
            }
            else
            {
                throw new Exception("bad turf");
            }
        }

        public void LoadMaps(List<DreamMapJson> maps) {
            if (maps.Count == 0) throw new ArgumentException("No maps were given");
            else if (maps.Count > 1)
            {
                Logger.Warning("Loading more than one map is not implemented, skipping additional maps");
            }
            DreamMapJson map = maps[0];

            Size = new Vector2i(map.MaxX, map.MaxY);
            SetZLevels(map.MaxZ);

            foreach (MapBlockJson block in map.Blocks) {
                LoadMapBlock(block, map.CellDefinitions);
            }
        }

        public void SetTurf(int x, int y, int z, DreamObject turf, bool replace = true) {
            if (!IsValidCoordinate(x, y, z)) throw new ArgumentException("Invalid coordinates");

            _levels[z - 1][x - 1, y - 1].Turf = turf;

            EntityUid entity = _atomManager.GetAtomEntity(turf);
            if (!_entityManager.TryGetComponent<TransformComponent>(entity, out var transform))
                return;

            transform.AttachParent(_mapManager.GetMapEntityId(new MapId(z)));
            transform.LocalPosition = new Vector2(x, y);

            if (replace) {
                //Every reference to the old turf becomes the new turf
                //Do this by turning the old turf object into the new one
                DreamObject existingTurf = GetTurf(x, y, z);
                existingTurf.CopyFrom(turf);
            }
        }

        public void SetArea(int x, int y, int z, DreamObject area) {
            if (!IsValidCoordinate(x, y, z)) throw new ArgumentException("Invalid coordinates");
            if (area.GetVariable("x").GetValueAsInteger() > x) area.SetVariable("x", new DreamValue(x));
            if (area.GetVariable("y").GetValueAsInteger() > y) area.SetVariable("y", new DreamValue(y));

            _levels[z - 1][x - 1, y - 1].Area = area;
        }

        public DreamObject GetTurf(int x, int y, int z) {
            if (!IsValidCoordinate(x, y, z)) return null;

            return _levels[z - 1][x - 1, y - 1].Turf;
        }

        //Returns an area loaded by a DMM
        //Does not include areas created by DM code
        public DreamObject GetArea(DreamPath type) {
            if (!_areas.TryGetValue(type, out DreamObject area)) {
                area = _dreamManager.ObjectTree.CreateObject(type);
                area.InitSpawn(new(null));
                _areas.Add(type, area);
            }

            return area;
        }

        public DreamObject GetAreaAt(int x, int y, int z) {
            if (!IsValidCoordinate(x, y, z)) throw new ArgumentException("Invalid coordinates");

            return _levels[z - 1][x - 1, y - 1].Area;
        }

        public void SetZLevels(int levels) {
            if (levels > Levels) {
                for (int z = Levels + 1; z <= levels; z++) {
                    _levels.Add(new Cell[Size.X, Size.Y]);
                    _mapManager.CreateMap(new MapId(z));

                    for (int x = 1; x <= Size.X; x++) {
                        for (int y = 1; y <= Size.Y; y++) {
                            DreamObject turf = _dreamManager.ObjectTree.CreateObject(_defaultTurf);

                            turf.InitSpawn(new(null));
                            SetTurf(x, y, z, turf, replace: false);
                            SetArea(x, y, z, GetArea(_defaultArea));
                        }
                    }
                }
            } else if (levels < Levels) {
                _levels.RemoveRange(levels, Levels - levels);
                for (int z = Levels; z > levels; z--) {
                    _mapManager.DeleteMap(new MapId(z));
                }
            }
        }

        private bool IsValidCoordinate(int x, int y, int z) {
            return (x <= Size.X && y <= Size.Y && z <= Levels) && (x >= 1 && y >= 1 && z >= 1);
        }

        private void LoadMapBlock(MapBlockJson block, Dictionary<string, CellDefinitionJson> cellDefinitions) {
            int blockX = 1;
            int blockY = 1;

            foreach (string cell in block.Cells) {
                CellDefinitionJson cellDefinition = cellDefinitions[cell];
                DreamPath areaType = cellDefinition.Area != null ? _dreamManager.ObjectTree.Types[cellDefinition.Area.Type].Path : _defaultArea;
                DreamObject area = GetArea(areaType);

                int x = block.X + blockX - 1;
                int y = block.Y + block.Height - blockY;

                DreamObject turf;
                if (cellDefinition.Turf != null) {
                    turf = CreateMapObject(cellDefinition.Turf);
                } else {
                    turf = _dreamManager.ObjectTree.CreateObject(_defaultTurf);
                }

                SetTurf(x, y, block.Z, turf);
                SetArea(x, y, block.Z, area);
                turf.InitSpawn(new DreamProcArguments(null));


                foreach (MapObjectJson mapObject in cellDefinition.Objects) {
                    var obj = CreateMapObject(mapObject);
                    obj.InitSpawn(new DreamProcArguments(new() { new DreamValue(turf) }));
                }

                blockX++;
                if (blockX > block.Width) {
                    blockX = 1;
                    blockY++;
                }
            }
        }

        private DreamObject CreateMapObject(MapObjectJson mapObject) {
            DreamObjectDefinition definition = _dreamManager.ObjectTree.GetObjectDefinition(mapObject.Type);
            if (mapObject.VarOverrides?.Count > 0) {
                definition = new DreamObjectDefinition(definition);

                foreach (KeyValuePair<string, object> varOverride in mapObject.VarOverrides) {
                    if (definition.HasVariable(varOverride.Key)) {
                        definition.Variables[varOverride.Key] = _dreamManager.ObjectTree.GetDreamValueFromJsonElement(varOverride.Value);
                    }
                }
            }

            return new DreamObject(definition);
        }
    }
}
