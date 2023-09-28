using System;
using System.Collections.Generic;

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
        public Dictionary<string, object>? VarOverrides { get; set; }

        public MapObjectJson(int type) {
            Type = type;
        }

        public bool AddVarOverride(string varName, object varValue) {
            VarOverrides ??= new();
            bool contained = VarOverrides.ContainsKey(varName);
            VarOverrides[varName] = varValue;
            return !contained;
        }

        public override bool Equals(object? obj) {
            return obj is MapObjectJson json &&
                Type == json.Type &&
                EqualityComparer<Dictionary<string, object>>.Default.Equals(VarOverrides, json.VarOverrides);
        }

        public override int GetHashCode() {
            return HashCode.Combine(Type, VarOverrides);
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
