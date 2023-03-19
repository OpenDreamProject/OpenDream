using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace OpenDreamRuntime {
    public sealed class DreamMapManager : IDreamMapManager {
        public sealed class Level {
            public readonly int Z;
            public readonly MapGridComponent Grid;
            public readonly Cell[,] Cells;
            public readonly Dictionary<Vector2i, Tile> QueuedTileUpdates = new();

            public Level(int z, MapGridComponent grid, DreamObject area, Vector2i size) {
                Z = z;
                Grid = grid;

                Cells = new Cell[size.X, size.Y];
                for (int x = 0; x < size.X; x++) {
                    for (int y = 0; y < size.Y; y++) {
                        Cells[x, y] = new Cell(area);
                    }
                }
            }

            public void SetArea(Vector2i pos, DreamObject area) {
                if (!area.GetVariable("x").TryGetValueAsInteger(out int x) || x == 0 || x > pos.X)
                    area.SetVariable("x", new DreamValue(pos.X));
                if (!area.GetVariable("y").TryGetValueAsInteger(out int y) || y == 0 || y > pos.Y)
                    area.SetVariable("y", new DreamValue(pos.Y));
                if (!area.GetVariable("z").TryGetValueAsInteger(out int z) || z == 0 || z > Z)
                    area.SetVariable("z", new DreamValue(Z));

                Cells[pos.X - 1, pos.Y - 1].Area = area;
            }
        }

        public sealed class Cell {
            public DreamObject? Turf;
            public DreamObject Area;

            public Cell(DreamObject area) {
                Area = area;
            }
        };

        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IDreamObjectTree _objectTree = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        private ServerAppearanceSystem _appearanceSystem = default!;  // set in Initialize

        public Vector2i Size { get; private set; }
        public int Levels => _levels.Count;
        public List<DreamObject> AllAtoms { get; } = new();
        public IEnumerable<DreamObject> AllAreas => _areas.Values;
        public IEnumerable<DreamObject> AllTurfs => _turfToTilePos.Keys; // Hijack this dictionary

        private readonly List<Level> _levels = new();
        private readonly Dictionary<DreamObject, (Vector2i Pos, Level Level)> _turfToTilePos = new();
        private readonly Dictionary<MapObjectJson, DreamObject> _areas = new();
        private MapObjectJson _defaultArea = default!;  // set in Initialize
        private IDreamObjectTree.TreeEntry _defaultTurf;

        public void Initialize() {
            AllAtoms.Clear();

            _appearanceSystem = _entitySystemManager.GetEntitySystem<ServerAppearanceSystem>();

            DreamObjectDefinition worldDefinition = _objectTree.World.ObjectDefinition;

            // Default area
            if (worldDefinition.Variables["area"].TryGetValueAsType(out var area)) {
                if(!area.ObjectDefinition.IsSubtypeOf(_objectTree.Area)) throw new Exception("bad area");

                _defaultArea = new MapObjectJson(area.Id);
            } else if (worldDefinition.Variables["area"] == DreamValue.Null ||
                     worldDefinition.Variables["area"].TryGetValueAsInteger(out var areaInt) && areaInt == 0) {
                //TODO: Properly handle disabling default area
                _defaultArea = new MapObjectJson(_objectTree.Area.Id);
            } else {
                throw new Exception("bad area");
            }

            //Default turf
            if (worldDefinition.Variables["turf"].TryGetValueAsType(out var turf)) {
                if (!turf.ObjectDefinition.IsSubtypeOf(_objectTree.Turf))
                    throw new Exception("bad turf");
                _defaultTurf = turf;
            } else if (worldDefinition.Variables["turf"] == DreamValue.Null ||
                       worldDefinition.Variables["turf"].TryGetValueAsInteger(out var turfInt) && turfInt == 0) {
                //TODO: Properly handle disabling default turf
                _defaultTurf = _objectTree.Turf;
            } else {
                throw new Exception("bad turf");
            }
        }

        public void UpdateTiles() {
            foreach (Level level in _levels) {
                if (level.QueuedTileUpdates.Count == 0)
                    continue;

                List<(Vector2i, Tile)> tiles = new(level.QueuedTileUpdates.Count);
                foreach (var tileUpdate in level.QueuedTileUpdates) {
                    tiles.Add( (tileUpdate.Key, tileUpdate.Value) );
                }

                level.Grid.SetTiles(tiles);
                level.QueuedTileUpdates.Clear();
            }
        }

        public void LoadAreasAndTurfs(List<DreamMapJson> maps) {
            if (maps.Count == 0) throw new ArgumentException("No maps were given");
            if (maps.Count > 1) {
                Logger.Warning("Loading more than one map is not implemented, skipping additional maps");
            }

            DreamMapJson map = maps[0];

            Size = new Vector2i(map.MaxX, map.MaxY);
            SetZLevels(map.MaxZ);

            foreach (MapBlockJson block in map.Blocks) {
                LoadMapAreasAndTurfs(block, map.CellDefinitions);
            }
        }

        public void InitializeAtoms(List<DreamMapJson> maps) {
            // Call New() on all /area in this particular order, each with waitfor=FALSE
            var seenAreas = new HashSet<DreamObject>();
            for (var z = 1; z <= Levels; ++z) {
                for (var y = 1; y <= Size.Y; ++y) {
                    for (var x = 1; x <= Size.X; ++x) {
                        var area = _levels[z - 1].Cells[x - 1, y - 1].Area;
                        if (seenAreas.Add(area)) {
                            area.SpawnProc("New");
                        }
                    }
                }
            }

            // Also call New() on all /area not in the grid.
            // This may call New() a SECOND TIME. This is intentional.
            foreach (var thing in AllAtoms) {
                if (thing.IsSubtypeOf(_objectTree.Area)) {
                    if (seenAreas.Add(thing)) {
                        thing.SpawnProc("New");
                    }
                }
            }

            // Call New() on all /turf in the grid, each with waitfor=FALSE
            for (var z = 1; z <= Levels; ++z) {
                for (var y = Size.Y; y >= 1; --y) {
                    for (var x = Size.X; x >= 1; --x) {
                        _levels[z - 1].Cells[x - 1, y - 1].Turf?.SpawnProc("New");
                    }
                }
            }

            // new() up /objs and /mobs from compiled-in maps
            DreamMapJson map = maps[0];
            foreach (MapBlockJson block in map.Blocks) {
                LoadMapObjectsAndMobs(block, map.CellDefinitions);
            }
        }

        private DreamObject SetTurf(Vector2i pos, Level level, DreamObjectDefinition type, DreamProcArguments creationArguments) {
            if (IsInvalidCoordinate(pos, level.Z)) throw new ArgumentException("Invalid coordinates");

            Cell cell = level.Cells[pos.X - 1, pos.Y - 1];
            if (cell.Turf != null) {
                cell.Turf.SetObjectDefinition(type);
            } else {
                cell.Turf = new DreamObject(type);
                _turfToTilePos.Add(cell.Turf, (pos, level));
                // Only add the /turf to .contents when it's created.
                cell.Area.GetVariable("contents").GetValueAsDreamList().AddValue(new(cell.Turf));
                AllAtoms.Add(cell.Turf);
            }

            cell.Turf.InitSpawn(creationArguments);
            return cell.Turf;
        }

        public void SetTurf(DreamObject turf, DreamObjectDefinition type, DreamProcArguments creationArguments) {
            (Vector2i pos, Level level) = _turfToTilePos[turf];

            SetTurf(pos, level, type, creationArguments);
        }

        public void SetTurfAppearance(DreamObject turf, IconAppearance appearance) {
            (Vector2i pos, Level level) = _turfToTilePos[turf];

            uint appearanceId = _appearanceSystem.AddAppearance(appearance);
            if (appearanceId > ushort.MaxValue - 1) {
                // TODO: Maybe separate appearance IDs and turf IDs to prevent this possibility
                Logger.Error($"Failed to set turf's appearance at {pos} because its appearance ID was greater than {ushort.MaxValue - 1}");
                return;
            }

            ushort turfId = (ushort) (appearanceId + 1); // +1 because 0 is used for empty turfs
            level.QueuedTileUpdates[pos] = new Tile(turfId);
        }

        public IconAppearance GetTurfAppearance(DreamObject turf) {
            (Vector2i pos, Level level) = _turfToTilePos[turf];

            if (!level.QueuedTileUpdates.TryGetValue(pos, out var tile)) {
                tile = level.Grid.GetTileRef(pos).Tile;
            }

            uint appearanceId = (uint)tile.TypeId - 1;
            return _appearanceSystem.GetAppearance(appearanceId);
        }

        public bool TryGetTurfAt(Vector2i pos, int z, [NotNullWhen(true)] out DreamObject? turf) {
            if (IsInvalidCoordinate(pos, z) || !_levels.TryGetValue(z - 1, out var level)) {
                turf = null;
                return false;
            }

            turf = level.Cells[pos.X - 1, pos.Y - 1].Turf;
            return (turf != null);
        }

        public (Vector2i Pos, Level Level) GetTurfPosition(DreamObject turf) {
            return _turfToTilePos[turf];
        }

        //Returns an area loaded by a DMM
        //Does not include areas created by DM code
        private DreamObject GetOrCreateArea(MapObjectJson prototype) {
            if (!_areas.TryGetValue(prototype, out DreamObject? area)) {
                var definition = CreateMapObjectDefinition(prototype);
                area = new DreamObject(definition);
                area.InitSpawn(new());
                _areas.Add(prototype, area);
            }

            return area;
        }

        public DreamObject GetAreaAt(DreamObject turf) {
            (Vector2i pos, Level level) = GetTurfPosition(turf);

            return level.Cells[pos.X - 1, pos.Y - 1].Area;
        }

        public void SetZLevels(int levels) {
            if (levels > Levels) {
                DreamObject defaultArea = GetOrCreateArea(_defaultArea);

                for (int z = Levels + 1; z <= levels; z++) {
                    MapId mapId = new(z);
                    _mapManager.CreateMap(mapId);

                    MapGridComponent grid = _mapManager.CreateGrid(mapId);
                    Level level = new Level(z, grid, defaultArea, Size);
                    _levels.Add(level);

                    for (int x = 1; x <= Size.X; x++) {
                        for (int y = 1; y <= Size.Y; y++) {
                            Vector2i pos = (x, y);

                            SetTurf(pos, level, _defaultTurf.ObjectDefinition, new(null));
                        }
                    }
                }

                UpdateTiles();
            } else if (levels < Levels) {
                _levels.RemoveRange(levels, Levels - levels);
                for (int z = Levels; z > levels; z--) {
                    _mapManager.DeleteMap(new MapId(z));
                }
            }
        }

        private bool IsInvalidCoordinate(Vector2i pos, int z) {
            return pos.X < 1 || pos.X > Size.X ||
                   pos.Y < 1 || pos.Y > Size.Y ||
                   z < 1 || z > Levels;
        }

        private void LoadMapAreasAndTurfs(MapBlockJson block, Dictionary<string, CellDefinitionJson> cellDefinitions) {
            int blockX = 1;
            int blockY = 1;

            // Order here doesn't really matter because it's not observable.
            foreach (string cell in block.Cells) {
                CellDefinitionJson cellDefinition = cellDefinitions[cell];
                DreamObject area = GetOrCreateArea(cellDefinition.Area ?? _defaultArea);

                Vector2i pos = (block.X + blockX - 1, block.Y + block.Height - blockY);
                Level level = _levels[block.Z - 1];

                var turf = SetTurf(pos, level, CreateMapObjectDefinition(cellDefinition.Turf), new());
                // The following calls level.SetArea via an event on the area's `contents` var.
                if (level.Cells[pos.X - 1, pos.Y - 1].Area != area) {
                    area.GetVariable("contents").MustGetValueAsDreamList().AddValue(new(turf));
                }

                blockX++;
                if (blockX > block.Width) {
                    blockX = 1;
                    blockY++;
                }
            }
        }

        private void LoadMapObjectsAndMobs(MapBlockJson block, Dictionary<string, CellDefinitionJson> cellDefinitions) {
            // The order we call New() here should be (1,1), (2,1), (1,2), (2,2)
            int blockY = block.Y;
            foreach (var row in block.Cells.Chunk(block.Width).Reverse()) {
                int blockX = block.X;
                foreach (var cell in row) {
                    CellDefinitionJson cellDefinition = cellDefinitions[cell];

                    if (TryGetTurfAt((blockX, blockY), block.Z, out var turf)) {
                        foreach (MapObjectJson mapObject in cellDefinition.Objects) {
                            var objDef = CreateMapObjectDefinition(mapObject);
                            var obj = new DreamObject(objDef);

                            obj.InitSpawn(new DreamProcArguments(new() { new DreamValue(turf) }));
                        }
                    }

                    ++blockX;
                }
                ++blockY;
            }
        }

        private DreamObjectDefinition CreateMapObjectDefinition(MapObjectJson mapObject) {
            DreamObjectDefinition definition = _objectTree.GetObjectDefinition(mapObject.Type);
            if (mapObject.VarOverrides?.Count > 0) {
                definition = new DreamObjectDefinition(definition);

                foreach (KeyValuePair<string, object> varOverride in mapObject.VarOverrides) {
                    if (definition.HasVariable(varOverride.Key)) {
                        definition.Variables[varOverride.Key] = _objectTree.GetDreamValueFromJsonElement(varOverride.Value);
                    }
                }
            }

            return definition;
        }
    }

    public interface IDreamMapManager {
        public Vector2i Size { get; }
        public int Levels { get; }
        public List<DreamObject> AllAtoms { get; }
        public IEnumerable<DreamObject> AllAreas { get; }
        public IEnumerable<DreamObject> AllTurfs { get; }

        public void Initialize();
        public void LoadAreasAndTurfs(List<DreamMapJson> maps);
        public void InitializeAtoms(List<DreamMapJson> maps);
        public void UpdateTiles();

        public void SetTurf(DreamObject turf, DreamObjectDefinition type, DreamProcArguments creationArguments);
        public void SetTurfAppearance(DreamObject turf, IconAppearance appearance);
        public IconAppearance GetTurfAppearance(DreamObject turf);
        public bool TryGetTurfAt(Vector2i pos, int z, [NotNullWhen(true)] out DreamObject? turf);
        public (Vector2i Pos, DreamMapManager.Level Level) GetTurfPosition(DreamObject turf);
        public DreamObject GetAreaAt(DreamObject turf);
        public void SetZLevels(int levels);
    }
}
