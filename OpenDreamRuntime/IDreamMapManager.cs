using OpenDreamRuntime.Objects;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;

namespace OpenDreamRuntime {
    interface IDreamMapManager {
        public Vector2i Size { get; }
        public int Levels { get; }

        public void Initialize();
        public void LoadMaps(List<DreamMapJson> maps);
        public void SetTurf(int x, int y, int z, DreamObject turf, bool replace = true);
        public void SetArea(int x, int y, int z, DreamObject area);
        public DreamObject GetTurf(int x, int y, int z);
        public DreamObject GetArea(DreamPath type);
        public DreamObject GetAreaAt(int x, int y, int z);
        public void SetZLevels(int levels);
    }
}
