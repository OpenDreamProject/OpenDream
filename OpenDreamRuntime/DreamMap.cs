﻿using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Compiler;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Compiler.DMPreprocessor;
using OpenDreamShared.Dream;
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
            public UInt32 Turf;
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

        public void LoadMap(string filePath) {
            DMPreprocessor dmmPreprocessor = new DMPreprocessor(false);
            dmmPreprocessor.IncludeFile(Runtime.ResourceManager.RootPath, filePath);

            DMMParser dmmParser = new DMMParser(Runtime, new DMLexer(filePath, dmmPreprocessor.GetResult()));
            DMMParser.Map map = dmmParser.ParseMap();

            if (dmmParser.Errors.Count > 0) {
                foreach (CompilerError error in dmmParser.Errors) {
                    Console.WriteLine(error);
                }

                throw new Exception("Errors while parsing map");
            }

            Width = map.MaxX - 1;
            Height = map.MaxY - 1;
            Levels = new List<MapLevel>(map.MaxZ);
            for (int z = 1; z <= map.MaxZ; z++) {
                Levels.Add(new MapLevel(Width, Height));
            }

            DreamPath defaultTurf = Runtime.WorldInstance.GetVariable("turf").GetValueAsPath();
            DreamPath defaultArea = Runtime.WorldInstance.GetVariable("area").GetValueAsPath();

            foreach (DMMParser.MapBlock mapBlock in map.Blocks) {
                foreach (KeyValuePair<(int X, int Y), string> cell in mapBlock.Cells) {
                    DMMParser.CellDefinition cellDefinition = map.CellDefinitions[cell.Value];
                    DreamObject turf = CreateMapObject(cellDefinition.Turf);
                    DreamObject area = GetMapLoaderArea(cellDefinition.Area?.Type ?? defaultArea);

                    if (turf == null) turf = Runtime.ObjectTree.CreateObject(defaultTurf);
                    
                    int x = mapBlock.X + cell.Key.X - 1;
                    int y = mapBlock.Y + cell.Key.Y - 1;

                    SetTurf(x, y, mapBlock.Z, turf, false);
                    SetArea(x, y, mapBlock.Z, area);
                    foreach (DMMParser.MapObject mapObject in cellDefinition.Objects) {
                        CreateMapObject(mapObject, turf);
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
                    SetTurf(x, y, z, Runtime.ObjectTree.CreateObject(defaultTurf), false);
                    SetArea(x, y, z, area);
                }
            }
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
            

            Levels[z - 1].Cells[x - 1, y - 1].Turf = Runtime.AtomIDs[turf];
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
                return Runtime.AtomIDToAtom[Levels[z - 1].Cells[x - 1, y - 1].Turf];
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

                _mapLoaderAreas.Add(areaPath, area);
                return area;
            }
        }

        private DreamObject CreateMapObject(DMMParser.MapObject mapObject, DreamObject loc = null) {
            if (!Runtime.ObjectTree.HasTreeEntry(mapObject.Type)) {
                Console.WriteLine("MAP LOAD: Skipping " + mapObject.Type);

                return null;
            }

            DreamObjectDefinition definition = Runtime.ObjectTree.GetObjectDefinitionFromPath(mapObject.Type);
            if (mapObject.VarOverrides.Count > 0) {
                definition = new DreamObjectDefinition(definition);

                foreach (KeyValuePair<string, DreamValue> varOverride in mapObject.VarOverrides) {
                    if (definition.HasVariable(varOverride.Key)) {
                        definition.Variables[varOverride.Key] = varOverride.Value;
                    }
                }
            }

            return new DreamObject(Runtime, definition, new DreamProcArguments(new() { new DreamValue(loc) }));
        }
    }
}
