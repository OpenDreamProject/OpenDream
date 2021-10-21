using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;

namespace OpenDreamClient.Resources.ResourceTypes {
    class ResourceDMI : Resource {
        public Bitmap ImageBitmap;
        public DMIParser.ParsedDMIDescription Description;

        private readonly byte[] _pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0xD, 0xA, 0x1A, 0xA };

        public ResourceDMI(string resourcePath, byte[] data) : base(resourcePath, data) {
            if (!IsValidPNG()) throw new Exception("Attempted to create a DMI using an invalid PNG");

            ImageBitmap = (Bitmap)Image.FromStream(new MemoryStream(data));

            if (DMIParser.TryReadDMIDescription(Data, out string description)) {
                Description = DMIParser.ParseDMIDescription(description, ImageBitmap.Width);
            } else {
                //No DMI metadata, default to a single unnamed icon_state
                Description = DMIParser.ParsedDMIDescription.CreateEmpty(ImageBitmap.Width, ImageBitmap.Height);
            }
        }

        ~ResourceDMI() {
            ImageBitmap.Dispose();
        }

        public Rectangle GetTextureRect(string stateName, AtomDirection direction = AtomDirection.South, int animationFrame = 0) {
            DMIParser.ParsedDMIState state = Description.GetState(stateName);
            if (state == null) return Rectangle.Empty;
            DMIParser.ParsedDMIFrame frame = state.GetFrames(direction)[animationFrame];

            return new Rectangle(frame.X, frame.Y, Description.Width, Description.Height);
        }

        public BitmapSource CreateWPFImageSource() {
            MemoryStream ms = new MemoryStream();
            BitmapImage image = new BitmapImage();

            ImageBitmap.Save(ms, ImageFormat.Png);
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();

            return image;
        }

        private bool IsValidPNG() {
            if (Data.Length < _pngHeader.Length) return false;

            for (int i=0; i<_pngHeader.Length; i++) {
                if (Data[i] != _pngHeader[i]) return false;
            }

            return true;
        }
    }
}
