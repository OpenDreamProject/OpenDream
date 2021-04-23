using System;
using System.Collections.Generic;

namespace OpenDreamClient.Dream {
    class Map {
        public struct Level {
            public ATOM[,] Turfs;
        }

        public List<Level> Levels = new();
        public int Width { get => Levels[0].Turfs.GetLength(0); }
        public int Height { get => Levels[0].Turfs.GetLength(1); }

        public bool IsValidCoordinate(int x, int y) {
            return  x >= 0 && x < Width &&
                    y >= 0 && y < Height;
        }

        public List<ATOM> GetTurfs(int x, int y, int z, int width, int height) {
            int startX = Math.Max(x, 0);
            int startY = Math.Max(y, 0);
            int endX = Math.Min(x + width, Width);
            int endY = Math.Min(y + height, Height);
            List<ATOM> turfs = new();

            for (x = startX; x < endX; x++) {
                for (y = startY; y < endY; y++) {
                    turfs.Add(Levels[z].Turfs[x, y]);
                }
            }

            return turfs;
        }
    }
}
