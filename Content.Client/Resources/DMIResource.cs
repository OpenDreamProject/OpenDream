using Content.Shared.Dream;
using Content.Shared.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using System.Collections.Generic;
using System.IO;

namespace Content.Client.Resources {
    class DMIResource : BaseResource {
        public struct State {
            public Dictionary<AtomDirection, AtlasTexture[]> Frames;

            public State(Texture texture, DMIParser.ParsedDMIState parsedState, int width, int height) {
                Frames = new Dictionary<AtomDirection, AtlasTexture[]>();

                foreach (KeyValuePair<AtomDirection, DMIParser.ParsedDMIFrame[]> pair in parsedState.Directions) {
                    AtomDirection dir = pair.Key;
                    DMIParser.ParsedDMIFrame[] parsedFrames = pair.Value;
                    AtlasTexture[] frames = new AtlasTexture[parsedFrames.Length];

                    for (int i = 0; i < parsedFrames.Length; i++) {
                        DMIParser.ParsedDMIFrame parsedFrame = parsedFrames[i];

                        frames[i] = new AtlasTexture(texture, new UIBox2(parsedFrame.X, parsedFrame.Y, parsedFrame.X + width, parsedFrame.Y + height));
                    }

                    Frames.Add(dir, frames);
                }
            }
        }

        public Texture Texture;
        public Dictionary<string, State> States;

        public override void Load(IResourceCache cache, ResourcePath path) {
            Stream dmiStream = cache.ContentFileRead(path);
            DMIParser.ParsedDMIDescription description = DMIParser.ParseDMI(dmiStream);

            dmiStream.Seek(0, SeekOrigin.Begin);

            Texture = IoCManager.Resolve<IClyde>().LoadTextureFromPNGStream(dmiStream);
            States = new Dictionary<string, State>();
            foreach (DMIParser.ParsedDMIState parsedState in description.States.Values) {
                States.Add(parsedState.Name, new State(Texture, parsedState, description.Width, description.Height));
            }
        }
    }
}
