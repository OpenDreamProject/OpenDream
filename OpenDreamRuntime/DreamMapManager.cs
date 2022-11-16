﻿using System.Diagnostics.CodeAnalysis;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace OpenDreamRuntime {
    public sealed class DreamMapManager : IDreamMapManager {
        public sealed class Level {
            public readonly int Z;
            public readonly IMapGrid Grid;
            public readonly Cell[,] Cells;
            public readonly Dictionary<Vector2i, Tile> QueuedTileUpdates = new();

            public Level(int z, IMapGrid grid, DreamObject area, Vector2i size) {
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
        [Dependency] private readonly IDreamManager _dreamManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        private ServerAppearanceSystem? _appearanceSystem;

        public Vector2i Size { get; private set; }
        public int Levels => _levels.Count;

        private readonly List<Level> _levels = new();
        private readonly Dictionary<DreamObject, (Vector2i Pos, Level Level)> _turfToTilePos = new();
        private readonly Dictionary<DreamPath, DreamObject> _areas = new();
        private DreamPath _defaultArea, _defaultTurf;

        public void Initialize() {
            _appearanceSystem = _entitySystemManager.GetEntitySystem<ServerAppearanceSystem>();

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

        public void LoadMaps(List<DreamMapJson> maps) {
            if (maps.Count == 0) throw new ArgumentException("No maps were given");
            if (maps.Count > 1)
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

        private void SetTurf(Vector2i pos, Level level, DreamObjectDefinition type, DreamProcArguments creationArguments) {
            if (IsInvalidCoordinate(pos, level.Z)) throw new ArgumentException("Invalid coordinates");

            Cell cell = level.Cells[pos.X - 1, pos.Y - 1];
            if (cell.Turf != null) {
                cell.Turf.SetObjectDefinition(type);
            } else {
                cell.Turf = new DreamObject(type);
                _turfToTilePos.Add(cell.Turf, (pos, level));
            }

            cell.Turf.InitSpawn(creationArguments);
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
        private DreamObject GetArea(DreamPath type) {
            if (!_areas.TryGetValue(type, out DreamObject? area)) {
                area = _dreamManager.ObjectTree.CreateObject(type);
                area.InitSpawn(new(null));
                _areas.Add(type, area);
            }

            return area;
        }

        public DreamObject GetAreaAt(DreamObject turf) {
            (Vector2i pos, Level level) = GetTurfPosition(turf);

            return level.Cells[pos.X - 1, pos.Y - 1].Area;
        }

        public void SetZLevels(int levels) {
            if (levels > Levels) {
                DreamObjectDefinition defaultTurfDef = _dreamManager.ObjectTree.GetObjectDefinition(_defaultTurf);
                DreamObject defaultArea = GetArea(_defaultArea);

                for (int z = Levels + 1; z <= levels; z++) {
                    MapId mapId = new(z);
                    _mapManager.CreateMap(mapId);

                    IMapGrid grid = _mapManager.CreateGrid(mapId);
                    Level level = new Level(z, grid, defaultArea, Size);
                    _levels.Add(level);

                    for (int x = 1; x <= Size.X; x++) {
                        for (int y = 1; y <= Size.Y; y++) {
                            Vector2i pos = (x, y);

                            SetTurf(pos, level, defaultTurfDef, new(null));
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

        private void LoadMapBlock(MapBlockJson block, Dictionary<string, CellDefinitionJson> cellDefinitions) {
            int blockX = 1;
            int blockY = 1;

            foreach (string cell in block.Cells) {
                CellDefinitionJson cellDefinition = cellDefinitions[cell];
                DreamPath areaType = cellDefinition.Area != null ? _dreamManager.ObjectTree.Types[cellDefinition.Area.Type].Path : _defaultArea;
                DreamObject area = GetArea(areaType);

                Vector2i pos = (block.X + blockX - 1, block.Y + block.Height - blockY);
                Level level = _levels[block.Z - 1];

                SetTurf(pos, level, CreateMapObjectDefinition(cellDefinition.Turf), new(null));
                level.SetArea(pos, area);

                if (TryGetTurfAt(pos, level.Z, out var turf)) {
                    foreach (MapObjectJson mapObject in cellDefinition.Objects) {
                        var objDef = CreateMapObjectDefinition(mapObject);
                        var obj = new DreamObject(objDef);

                        obj.InitSpawn(new DreamProcArguments(new() { new DreamValue(turf) }));
                    }
                }

                blockX++;
                if (blockX > block.Width) {
                    blockX = 1;
                    blockY++;
                }
            }
        }

        private DreamObjectDefinition CreateMapObjectDefinition(MapObjectJson mapObject) {
            DreamObjectDefinition definition = _dreamManager.ObjectTree.GetObjectDefinition(mapObject.Type);
            if (mapObject.VarOverrides?.Count > 0) {
                definition = new DreamObjectDefinition(definition);

                foreach (KeyValuePair<string, object> varOverride in mapObject.VarOverrides) {
                    if (definition.HasVariable(varOverride.Key)) {
                        definition.Variables[varOverride.Key] = _dreamManager.ObjectTree.GetDreamValueFromJsonElement(varOverride.Value);
                    }
                }
            }

            return definition;
        }
    }

    public interface IDreamMapManager {
        public Vector2i Size { get; }
        public int Levels { get; }

        public void Initialize();
        public void UpdateTiles();
        public void LoadMaps(List<DreamMapJson> maps);
        public void SetTurf(DreamObject turf, DreamObjectDefinition type, DreamProcArguments creationArguments);
        public void SetTurfAppearance(DreamObject turf, IconAppearance appearance);
        public IconAppearance GetTurfAppearance(DreamObject turf);
        public bool TryGetTurfAt(Vector2i pos, int z, [NotNullWhen(true)] out DreamObject? turf);
        public (Vector2i Pos, DreamMapManager.Level Level) GetTurfPosition(DreamObject turf);
        public DreamObject GetAreaAt(DreamObject turf);
        public void SetZLevels(int levels);
    }
}
