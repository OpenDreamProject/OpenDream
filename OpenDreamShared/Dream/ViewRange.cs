using System;

namespace OpenDreamShared.Dream {
    public struct ViewRange {
        public int Width, Height;
        public bool IsSquare => (Width == Height);

        //View can be centered in both directions?
        public bool IsCenterable => (Width % 2 == 1) && (Height % 2 == 1);

        public ViewRange(int range) {
            Width = range;
            Height = range;
        }

        public ViewRange(string range) {
            string[] split = range.Split("x");

            if (split.Length != 2) throw new Exception($"Invalid view range string \"{range}\"");
            Width = int.Parse(split[0]);
            Height = int.Parse(split[1]);
        }

        public override string ToString() {
            return $"{Width}x{Height}";
        }
    }
}
