using System;
using System.Collections.Generic;

namespace OpenDreamClient.Dream {
    class Map {
        public ATOM[,] Turfs;
        public int Width { get => Turfs.GetLength(0); }
        public int Height { get => Turfs.GetLength(1); }

        public Map(ATOM[,] turfs) {
            Turfs = turfs;
        }

        public bool IsValidCoordinate(int x, int y) {
            return  x >= 0 && x < Turfs.GetLength(0) &&
                    y >= 0 && y < Turfs.GetLength(1);
        }

        public List<ATOM> GetTurfs(int x, int y, int width, int height) {
            int startX = Math.Max(x, 0);
            int startY = Math.Max(y, 0);
            int endX = Math.Min(x + width, Width);
            int endY = Math.Min(y + height, Height);
            List<ATOM> turfs = new();

            for (x = startX; x < endX; x++) {
                for (y = startY; y < endY; y++) {
                    turfs.Add(Turfs[x, y]);
                }
            }

            return turfs;
        }
    }
}
