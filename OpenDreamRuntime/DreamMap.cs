using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;

namespace OpenDreamRuntime {
    public class DreamMap {
        public DreamMap(DreamRuntime runtime) {
            Runtime = runtime;
        }

        public DreamRuntime Runtime { get; }

        public class MapCell {
            public DreamObject Area;
            public DreamObject Turf;
        }

        public class MapLevel {
            public MapCell[,] Cells;

            public MapLevel(int width, int height) {
                Cells = new MapCell[width, height];

                for (int x = 1; x <= width; x++) {
                    for (int y = 1; y <= height; y++) {
                        Cells[x - 1, y - 1] = new MapCell();
                    }
                }
            }
        }

        public List<MapLevel> Levels { get; private set; }
        public int Width, Height;

        private Dictionary<DreamPath, DreamObject> _mapLoaderAreas = new();

        public void LoadMap(DreamMapJson map) {
            Width = map.MaxX;
            Height = map.MaxY;
            Levels = new List<MapLevel>(map.MaxZ);
            for (int z = 1; z <= map.MaxZ; z++) {
                Levels.Add(new MapLevel(Width, Height));
            }

            DreamPath defaultTurf = Runtime.WorldInstance.GetVariable("turf").GetValueAsPath();
            DreamPath defaultArea = Runtime.WorldInstance.GetVariable("area").GetValueAsPath();

            foreach (MapBlockJson mapBlock in map.Blocks) {
                int blockX = 1;
                int blockY = 1;

                foreach (string cell in mapBlock.Cells) {
                    CellDefinitionJson cellDefinition = map.CellDefinitions[cell];
                    DreamObject turf = CreateMapObject(cellDefinition.Turf);
                    DreamObject area = GetMapLoaderArea(cellDefinition.Area?.Type ?? defaultArea);

                    if (turf == null) {
                        turf = Runtime.ObjectTree.CreateObject(defaultTurf);
                        turf.InitSpawn(new DreamProcArguments(null));
                    }

                    int x = mapBlock.X + blockX - 1;
                    int y = mapBlock.Y + mapBlock.Height - blockY;

                    SetTurf(x, y, mapBlock.Z, turf, false);
                    SetArea(x, y, mapBlock.Z, area);
                    foreach (MapObjectJson mapObject in cellDefinition.Objects) {
                        CreateMapObject(mapObject, turf);
                    }

                    blockX++;
                    if (blockX > mapBlock.Width) {
                        blockX = 1;
                        blockY++;
                    }
                }
            }
        }

        public void AddLevel() {
            DreamPath defaultTurf = Runtime.WorldInstance.GetVariable("turf").GetValueAsPath();
            DreamPath defaultArea = Runtime.WorldInstance.GetVariable("area").GetValueAsPath();
            DreamObject area = GetMapLoaderArea(defaultArea);

            Levels.Add(new MapLevel(Width, Height));

            int z = Levels.Count;
            for (int x = 1; x <= Width; x++) {
                for (int y = 1; y <= Height; y++) {
                    var turf = Runtime.ObjectTree.CreateObject(defaultTurf);
                    turf.InitSpawn(new DreamProcArguments(null));

                    SetTurf(x, y, z, turf, false);
                    SetArea(x, y, z, area);
                }
            }
        }

        public void RemoveLevel() {
            MapLevel toRemove = Levels[^1];
            foreach (MapCell cell in toRemove.Cells) {
                cell.Turf.Delete();
            }
            Levels.Remove(toRemove);
        }

        public void SetTurf(int x, int y, int z, DreamObject turf, bool replace = true) {
            if (!turf.IsSubtypeOf(DreamPath.Turf)) {
                throw new Exception("Turf was not a sub-type of " + DreamPath.Turf);
            }

            turf.SetVariable("x", new DreamValue(x));
            turf.SetVariable("y", new DreamValue(y));
            turf.SetVariable("z", new DreamValue(z));

            if (replace) {
                DreamObject existingTurf = GetTurfAt(x, y, z);

                //Every reference to the old turf becomes the new turf
                //Do this by turning the old turf object into the new one
                existingTurf.CopyFrom(turf);
            }


            Levels[z - 1].Cells[x - 1, y - 1].Turf = turf;
            Runtime.StateManager.AddTurfDelta(x - 1, y - 1, z - 1, turf);
        }

        public void SetArea(int x, int y, int z, DreamObject area) {
            if (!area.IsSubtypeOf(DreamPath.Area)) {
                throw new Exception("Invalid area " + area);
            }

            if (area.GetVariable("x").GetValueAsInteger() > x) area.SetVariable("x", new DreamValue(x));
            if (area.GetVariable("y").GetValueAsInteger() > y) area.SetVariable("y", new DreamValue(y));

            Levels[z - 1].Cells[x - 1, y - 1].Area = area;
        }

        public DreamObject GetTurfAt(int x, int y, int z) {
            if (x >= 1 && x <= Runtime.Map.Width && y >= 1 && y <= Runtime.Map.Height && z >= 1 && z <= Runtime.Map.Levels.Count) {
                return Levels[z - 1].Cells[x - 1, y - 1].Turf;
            } else {
                return null;
            }
        }

        public DreamObject GetAreaAt(int x, int y, int z) {
            return Levels[z - 1].Cells[x - 1, y - 1].Area;
        }

        private DreamObject GetMapLoaderArea(DreamPath areaPath) {
            if (_mapLoaderAreas.TryGetValue(areaPath, out DreamObject area)) {
                return area;
            } else {
                area = Runtime.ObjectTree.CreateObject(areaPath);
                area.InitSpawn(new DreamProcArguments(null));

                _mapLoaderAreas.Add(areaPath, area);
                return area;
            }
        }

        private DreamObject CreateMapObject(MapObjectJson mapObject, DreamObject loc = null) {
            if (!Runtime.ObjectTree.HasTreeEntry(mapObject.Type)) {
                Console.WriteLine("MAP LOAD: Skipping " + mapObject.Type);

                return null;
            }

            DreamObjectDefinition definition = Runtime.ObjectTree.GetObjectDefinitionFromPath(mapObject.Type);
            if (mapObject.VarOverrides?.Count > 0) {
                definition = new DreamObjectDefinition(definition);

                foreach (KeyValuePair<string, object> varOverride in mapObject.VarOverrides) {
                    if (definition.HasVariable(varOverride.Key)) {
                        definition.Variables[varOverride.Key] = Runtime.ObjectTree.GetDreamValueFromJsonElement(varOverride.Value);
                    }
                }
            }

            var obj = new DreamObject(Runtime, definition);
            obj.InitSpawn(new DreamProcArguments(new() { new DreamValue(loc) }));
            return obj;
        }
    }
}
