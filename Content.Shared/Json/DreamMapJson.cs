using Content.Shared.Dream;
using System.Collections.Generic;

namespace Content.Shared.Json {
    public class DreamMapJson {
        public int MaxX { get; set; }
        public int MaxY { get; set; }
        public int MaxZ { get; set; }
        public Dictionary<string, CellDefinitionJson> CellDefinitions { get; set; } = new();
        public List<MapBlockJson> Blocks { get; set; } = new();
    }

    public class CellDefinitionJson {
        public string Name { get; set; }
        public MapObjectJson Turf { get; set; }
        public MapObjectJson Area { get; set; }
        public List<MapObjectJson> Objects { get; set; } = new();

        public CellDefinitionJson(string name) {
            Name = name;
        }
    }

    public class MapObjectJson {
        public DreamPath Type { get; set; }
        public Dictionary<string, object> VarOverrides { get; set; }

        public MapObjectJson(DreamPath type) {
            Type = type;
        }

        public void AddVarOverride(string varName, object varValue) {
            if (VarOverrides == null) VarOverrides = new Dictionary<string, object>();

            VarOverrides.Add(varName, varValue);
        }
    }

    public class MapBlockJson {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<string> Cells { get; set; } = new();

        public MapBlockJson(int x, int y, int z) {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
