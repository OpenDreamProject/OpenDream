using OpenDreamClient.Resources.ResourceTypes;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OpenDreamClient.Input {
    //Lifted from SS14 for the most part
    //Used for atoms with an opaque mouse_opacity
    internal sealed class ClickMapManager : IClickMapManager {
        [ViewVariables]
        private readonly Dictionary<AtlasTexture, ClickMap> _clickMaps = new();

        public void CreateClickMap(DMIResource.State state, Image<Rgba32> image) {
            foreach (AtlasTexture[] textures in state.Frames.Values) {
                foreach (AtlasTexture texture in textures) {
                    _clickMaps[texture] = ClickMap.FromImage(image, texture);
                }
            }
        }

        public bool IsOccluding(AtlasTexture texture, Vector2i pos) {
            if (!_clickMaps.TryGetValue(texture, out var clickMap)) {
                return false;
            }

            return SampleClickMap(clickMap, pos, clickMap.Size, Vector2i.Zero);
        }

        private static bool SampleClickMap(ClickMap map, Vector2i pos, Vector2i bounds, Vector2i offset) {
            var (width, height) = bounds;
            var (px, py) = pos;

            if (px < 0 || px >= width || py < 0 || py >= height)
                return false;

            return map.IsOccluded(px, py);
        }

        internal sealed class ClickMap {
            [ViewVariables] private readonly byte[] _data;

            public int Width { get; }
            public int Height { get; }
            [ViewVariables] public Vector2i Size => (Width, Height);

            public bool IsOccluded(int x, int y) {
                var i = y * Width + x;
                return (_data[i / 8] & (1 << (i % 8))) != 0;
            }

            public bool IsOccluded(Vector2i vector) {
                var (x, y) = vector;
                return IsOccluded(x, y);
            }

            private ClickMap(byte[] data, int width, int height) {
                Width = width;
                Height = height;
                _data = data;
            }

            public static ClickMap FromImage<T>(Image<T> image, AtlasTexture atlas) where T : unmanaged, IPixel<T> {
                var width = (int)atlas.SubRegion.Width;
                var height = (int)atlas.SubRegion.Height;

                var dataSize = (int)Math.Ceiling(width * height / 8f);
                var data = new byte[dataSize];

                var pixelSpan = image.GetPixelSpan();

                for (var i = 0; i < width*height; i++) {
                    var y = (int)atlas.SubRegion.Top + (i / width);
                    var x = (int)atlas.SubRegion.Left + (i % width);
                    Rgba32 rgba = default;
                    pixelSpan[y * image.Width + x].ToRgba32(ref rgba);
                    if (rgba.A > 0) {
                        data[i / 8] |= (byte)(1 << (i % 8));
                    }
                }

                return new ClickMap(data, width, height);
            }
        }
    }

    public interface IClickMapManager {
        public void CreateClickMap(DMIResource.State state, Image<Rgba32> image);
        public bool IsOccluding(AtlasTexture texture, Vector2i pos);
    }
}
