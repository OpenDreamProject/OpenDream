namespace Content.Server.Dream {
    interface IDreamMapManager {
        public void LoadMap(string dmmFilePath);
        public void SetTurf(int x, int y, int z, DreamObject turf);
        public DreamObject GetTurf(int x, int y, int z);
    }
}
