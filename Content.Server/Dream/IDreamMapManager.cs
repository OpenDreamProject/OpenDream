using Content.Shared.Json;
using Robust.Shared.Maths;
using System.Collections.Generic;

namespace Content.Server.Dream {
    interface IDreamMapManager {
        public Vector2i Size { get; }
        public int Levels { get; }

        public void LoadMaps(List<DreamMapJson> maps);
        public void SetTurf(int x, int y, int z, DreamObject turf);
        public DreamObject GetTurf(int x, int y, int z);
    }
}
