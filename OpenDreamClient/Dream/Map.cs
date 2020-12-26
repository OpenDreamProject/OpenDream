using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Xaml;

namespace OpenDreamClient.Dream {
    class Map {
        public ATOM[,] Turfs;
        public int Width { get => Turfs.GetLength(0); }
        public int Height { get => Turfs.GetLength(1); }

        public Map(ATOM[,] turfs) {
            Turfs = turfs;
        }
    }
}
