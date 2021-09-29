using System;
using System.Collections.Generic;
using System.IO;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;
using Robust.Client.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace OpenDreamClient.Resources.ResourceTypes {
    class DMIResource : DreamResource {
        private readonly byte[] _pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0xD, 0xA, 0x1A, 0xA };

        public Texture Texture;
        public Vector2i IconSize;
        public Dictionary<string, State> States;

        public DMIResource(string resourcePath, byte[] data) : base(resourcePath, data)
        {
            if (!IsValidPNG()) throw new Exception("Attempted to create a DMI using an invalid PNG");

            Stream dmiStream = new MemoryStream(data);
            DMIParser.ParsedDMIDescription description = DMIParser.ParseDMI(dmiStream);

            dmiStream.Seek(0, SeekOrigin.Begin);

            Texture = IoCManager.Resolve<IClyde>().LoadTextureFromPNGStream(dmiStream);
            IconSize = new Vector2i(description.Width, description.Height);
            States = new Dictionary<string, State>();
            foreach (DMIParser.ParsedDMIState parsedState in description.States.Values) {
                States.Add(parsedState.Name, new State(Texture, parsedState, description.Width, description.Height));
            }
        }

        private bool IsValidPNG() {
            if (Data.Length < _pngHeader.Length) return false;

            for (int i=0; i<_pngHeader.Length; i++) {
                if (Data[i] != _pngHeader[i]) return false;
            }

            return true;
        }

        public struct State {
            private Dictionary<AtomDirection, AtlasTexture[]> _frames;

            public State(Texture texture, DMIParser.ParsedDMIState parsedState, int width, int height) {
                _frames = new Dictionary<AtomDirection, AtlasTexture[]>();

                foreach (KeyValuePair<AtomDirection, DMIParser.ParsedDMIFrame[]> pair in parsedState.Directions) {
                    AtomDirection dir = pair.Key;
                    DMIParser.ParsedDMIFrame[] parsedFrames = pair.Value;
                    AtlasTexture[] frames = new AtlasTexture[parsedFrames.Length];

                    for (int i = 0; i < parsedFrames.Length; i++) {
                        DMIParser.ParsedDMIFrame parsedFrame = parsedFrames[i];

                        frames[i] = new AtlasTexture(texture, new UIBox2(parsedFrame.X, parsedFrame.Y, parsedFrame.X + width, parsedFrame.Y + height));
                    }

                    _frames.Add(dir, frames);
                }
            }

            public AtlasTexture[] GetFrames(AtomDirection direction) {
                if (!_frames.TryGetValue(direction, out AtlasTexture[] frames))
                    frames = _frames[AtomDirection.South];

                return frames;
            }
        }

    }
}
