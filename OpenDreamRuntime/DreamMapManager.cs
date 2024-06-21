using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DMCompiler.Json;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;
using Level = OpenDreamRuntime.IDreamMapManager.Level;
using Cell = OpenDreamRuntime.IDreamMapManager.Cell;

namespace OpenDreamRuntime;

public sealed class DreamMapManager : IDreamMapManager {
    [Dependency] private readonly DreamManager _dreamManager = default!;
    [Dependency] private readonly AtomManager _atomManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly DreamObjectTree _objectTree = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    // Set in Initialize
    private ServerAppearanceSystem _appearanceSystem = default!;
    private SharedMapSystem _mapSystem = default!;

    public Vector2i Size { get; private set; }
    public int Levels => _levels.Count;

    private readonly List<Level> _levels = new();
    private readonly Dictionary<MapObjectJson, DreamObjectArea> _areas = new();

    private readonly ISawmill _sawmill = Logger.GetSawmill("opendream.map");

    // Set in Initialize
    private MapObjectJson _defaultArea = default!;
    private TreeEntry _defaultTurf = default!;

    public void Initialize() {
        _appearanceSystem = _entitySystemManager.GetEntitySystem<ServerAppearanceSystem>();
        _mapSystem = _entitySystemManager.GetEntitySystem<MapSystem>();

        DreamObjectDefinition worldDefinition = _objectTree.World.ObjectDefinition;

        // Default area
        if (worldDefinition.Variables["area"].TryGetValueAsType(out var area)) {
            if(!area.ObjectDefinition.IsSubtypeOf(_objectTree.Area)) throw new Exception("bad area");

            _defaultArea = new MapObjectJson(area.Id);
        } else if (worldDefinition.Variables["area"].IsNull ||
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
        } else if (worldDefinition.Variables["turf"].IsNull ||
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

            _mapSystem.SetTiles(level.Grid, tiles);
            level.QueuedTileUpdates.Clear();
        }
    }

    public void LoadMaps(List<DreamMapJson>? maps) {
        var world = _dreamManager.WorldInstance;
        var maxX = (int)world.ObjectDefinition.Variables["maxx"].UnsafeGetValueAsFloat();
        var maxY = (int)world.ObjectDefinition.Variables["maxy"].UnsafeGetValueAsFloat();
        var maxZ = (int)world.ObjectDefinition.Variables["maxz"].UnsafeGetValueAsFloat();

        if (maps != null) {
            foreach (var map in maps) {
                maxX = Math.Max(maxX, map.MaxX);
                maxY = Math.Max(maxY, map.MaxY);
                maxZ = Math.Max(maxZ, map.MaxZ);
            }
        }

        Size = new Vector2i(maxX, maxY);
        SetZLevels(maxZ);

        if (maps != null) {
            // Load turfs and areas of compiled-in maps, recursively calling <init>, but suppressing all New
            foreach (var map in maps) {
                foreach (MapBlockJson block in map.Blocks) {
                    LoadMapAreasAndTurfs(block, map.CellDefinitions);
                }
            }
        }
    }

    public void InitializeAtoms(List<DreamMapJson>? maps) {
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
        foreach (var thing in _atomManager.EnumerateAtoms(_objectTree.Area)) {
            if (seenAreas.Add(thing)) {
                thing.SpawnProc("New");
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

        if (maps != null) {
            // new() up /objs and /mobs from compiled-in maps
            foreach (var map in maps) {
                foreach (MapBlockJson block in map.Blocks) {
                    LoadMapObjectsAndMobs(block, map.CellDefinitions);
                }
            }
        }
    }

    private DreamObject SetTurf(Vector2i pos, int z, DreamObjectDefinition type, DreamProcArguments creationArguments) {
        if (IsInvalidCoordinate(pos, z))
            throw new ArgumentException("Invalid coordinates");

        Cell cell = _levels[z - 1].Cells[pos.X - 1, pos.Y - 1];
        if (cell.Turf != null) {
            cell.Turf.SetTurfType(type);
        } else {
            cell.Turf = new DreamObjectTurf(type, pos.X, pos.Y, z, cell);
            // Only add the /turf to .contents when it's created.
            cell.Area.Contents.AddValue(new(cell.Turf));
        }

        IconAppearance turfAppearance = _atomManager.GetAppearanceFromDefinition(cell.Turf.ObjectDefinition);
        SetTurfAppearance(cell.Turf, turfAppearance);

        cell.Turf.InitSpawn(creationArguments);
        return cell.Turf;
    }

    public void SetTurf(DreamObjectTurf turf, DreamObjectDefinition type, DreamProcArguments creationArguments) {
        SetTurf((turf.X, turf.Y), turf.Z, type, creationArguments);
    }

    public void SetTurfAppearance(DreamObjectTurf turf, IconAppearance appearance) {
        int appearanceId = _appearanceSystem.AddAppearance(appearance);

        var level = _levels[turf.Z - 1];
        int turfId = (appearanceId + 1); // +1 because 0 is used for empty turfs
        level.QueuedTileUpdates[(turf.X, turf.Y)] = new Tile(turfId);
        turf.AppearanceId = appearanceId;
    }

    public bool TryGetCellAt(Vector2i pos, int z, [NotNullWhen(true)] out Cell? cell) {
        if (IsInvalidCoordinate(pos, z) || !_levels.TryGetValue(z - 1, out var level)) {
            cell = null;
            return false;
        }

        cell = level.Cells[pos.X - 1, pos.Y - 1];
        return true;
    }

    public bool TryGetTurfAt(Vector2i pos, int z, [NotNullWhen(true)] out DreamObjectTurf? turf) {
        if (TryGetCellAt(pos, z, out var cell)) {
            turf = cell.Turf;
            return (turf != null);
        }

        turf = null;
        return false;
    }

    //Returns an area loaded by a DMM
    //Does not include areas created by DM code
    private DreamObjectArea GetOrCreateArea(MapObjectJson prototype) {
        if (!_areas.TryGetValue(prototype, out DreamObjectArea? area)) {
            var definition = CreateMapObjectDefinition(prototype);
            area = new DreamObjectArea(definition);
            area.InitSpawn(new());
            _areas.Add(prototype, area);
        }

        return area;
    }

    public void SetWorldSize(Vector2i size) {
        Vector2i oldSize = Size;

        var newX = Math.Max(oldSize.X, size.X);
        var newY = Math.Max(oldSize.Y, size.Y);

        DreamObjectArea defaultArea = GetOrCreateArea(_defaultArea);

        Size = (newX, newY);

        if(Size.X > oldSize.X || Size.Y > oldSize.Y) {
            foreach (Level existingLevel in _levels) {
                var oldCells = existingLevel.Cells;

                existingLevel.Cells = new Cell[Size.X, Size.Y];
                for (var x = 1; x <= Size.X; x++) {
                    for (var y = 1; y <= Size.Y; y++) {
                        if (x <= oldSize.X && y <= oldSize.Y) {
                            existingLevel.Cells[x - 1, y - 1] = oldCells[x - 1, y - 1];
                            continue;
                        }

                        existingLevel.Cells[x - 1, y - 1] = new Cell(defaultArea);
                        SetTurf(new Vector2i(x, y), existingLevel.Z, _defaultTurf.ObjectDefinition, new());
                    }
                }
            }
        }

        if (Size.X > size.X || Size.Y > size.Y) {
            Size = size;

            foreach (Level existingLevel in _levels) {
                var oldCells = existingLevel.Cells;

                existingLevel.Cells = new Cell[size.X, size.Y];
                for (var x = 1; x <= oldSize.X; x++) {
                    for (var y = 1; y <= oldSize.Y; y++) {
                        if (x > size.X || y > size.Y) {
                            var deleteCell = oldCells[x - 1, y - 1];
                            deleteCell.Turf?.Delete();
                            _mapSystem.SetTile(existingLevel.Grid, new Vector2i(x, y), Tile.Empty);
                            foreach (var movableToDelete in deleteCell.Movables) {
                                movableToDelete.Delete();
                            }
                        } else {
                            existingLevel.Cells[x - 1, y - 1] = oldCells[x - 1, y - 1];
                        }
                    }
                }
            }
        }

    }

    public void SetZLevels(int levels) {
        if (levels > Levels) {
            DreamObjectArea defaultArea = GetOrCreateArea(_defaultArea);

            for (int z = Levels + 1; z <= levels; z++) {
                MapId mapId = new(z);
                _mapSystem.CreateMap(mapId);

                var grid = _mapManager.CreateGridEntity(mapId);
                Level level = new Level(z, grid, defaultArea, Size);
                _levels.Add(level);

                for (int x = 1; x <= Size.X; x++) {
                    for (int y = 1; y <= Size.Y; y++) {
                        Vector2i pos = (x, y);

                        SetTurf(pos, z, _defaultTurf.ObjectDefinition, new());
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
            DreamObjectArea area = GetOrCreateArea(cellDefinition.Area ?? _defaultArea);

            Vector2i pos = (block.X + blockX - 1, block.Y + block.Height - blockY);
            Level level = _levels[block.Z - 1];

            var turf = SetTurf(pos, block.Z, CreateMapObjectDefinition(cellDefinition.Turf), new());
            // The following calls level.SetArea via an event on the area's `contents` var.
            if (level.Cells[pos.X - 1, pos.Y - 1].Area != area) {
                area.Contents.AddValue(new(turf));
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

                        // TODO: Use modified types during compile so this hack isn't necessary
                        DreamObject obj;
                        if (objDef.IsSubtypeOf(_objectTree.Mob)) {
                            obj = new DreamObjectMob(objDef);
                        } else if (objDef.IsSubtypeOf(_objectTree.Movable)) {
                            obj = new DreamObjectMovable(objDef);
                        } else if (objDef.IsSubtypeOf(_objectTree.Atom)) {
                            obj = new DreamObjectAtom(objDef);
                        } else {
                            obj = new DreamObject(objDef);
                        }

                        obj.InitSpawn(new(new DreamValue(turf)));
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

    public EntityUid GetZLevelEntity(int z) {
        return _levels[z - 1].Grid.Owner;
    }
}

public interface IDreamMapManager {
    public sealed class Level {
        public readonly int Z;
        public readonly Entity<MapGridComponent> Grid;
        public Cell[,] Cells;
        public readonly Dictionary<Vector2i, Tile> QueuedTileUpdates = new();

        public Level(int z, Entity<MapGridComponent> grid, DreamObjectArea area, Vector2i size) {
            Z = z;
            Grid = grid;

            Cells = new Cell[size.X, size.Y];
            for (int x = 0; x < size.X; x++) {
                for (int y = 0; y < size.Y; y++) {
                    Cells[x, y] = new Cell(area);
                }
            }
        }
    }

    public sealed class Cell {
        public DreamObjectTurf? Turf;
        public DreamObjectArea Area;
        public readonly List<DreamObjectMovable> Movables = new();

        public Cell(DreamObjectArea area) {
            Area = area;
        }
    }

    public Vector2i Size { get; }
    public int Levels { get; }

    public void Initialize();
    public void LoadMaps(List<DreamMapJson>? maps);
    public void InitializeAtoms(List<DreamMapJson>? maps);
    public void UpdateTiles();

    public void SetTurf(DreamObjectTurf turf, DreamObjectDefinition type, DreamProcArguments creationArguments);
    public void SetTurfAppearance(DreamObjectTurf turf, IconAppearance appearance);
    public bool TryGetCellAt(Vector2i pos, int z, [NotNullWhen(true)] out Cell? cell);
    public bool TryGetTurfAt(Vector2i pos, int z, [NotNullWhen(true)] out DreamObjectTurf? turf);
    public void SetZLevels(int levels);
    public void SetWorldSize(Vector2i size);
    public EntityUid GetZLevelEntity(int z);
}
