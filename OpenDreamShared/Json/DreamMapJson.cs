using System.Collections.Generic;
using OpenDreamShared.Dream;

namespace OpenDreamShared.Json {
    public sealed class DreamMapJson {
        public int MaxX { get; set; }
        public int MaxY { get; set; }
        public int MaxZ { get; set; }
        public Dictionary<string, CellDefinitionJson> CellDefinitions { get; set; } = new();
        public List<MapBlockJson> Blocks { get; set; } = new();
    }

    public sealed class CellDefinitionJson {
        public string Name { get; set; }
        public MapObjectJson Turf { get; set; }
        public MapObjectJson Area { get; set; }
        public List<MapObjectJson> Objects { get; set; } = new();

        public CellDefinitionJson(string name) {
            Name = name;
        }
    }

    public sealed class MapObjectJson {
        public int Type { get; set; }
        public Dictionary<string, object> VarOverrides { get; set; }

        public MapObjectJson(int type) {
            Type = type;
        }

        public bool AddVarOverride(string varName, object varValue) {
            if (VarOverrides == null) VarOverrides = new Dictionary<string, object>();

            if (VarOverrides.ContainsKey(varName))
            {
                VarOverrides[varName] = varValue;
                return false;
            }
            VarOverrides.Add(varName, varValue);
            return true;
        }
    }

    public sealed class MapBlockJson {
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
