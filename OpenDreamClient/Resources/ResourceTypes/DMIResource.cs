using System.IO;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;
using Robust.Client.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OpenDreamClient.Resources.ResourceTypes;

public sealed class DMIResource : DreamResource {
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0xD, 0xA, 0x1A, 0xA];

    public Texture Texture;
    public Vector2i IconSize;
    public DMIParser.ParsedDMIDescription Description;

    private readonly Dictionary<string, State> _states;

    public DMIResource(int id, byte[] data) : base(id, data) {
        _states = new Dictionary<string, State>();
        ProcessDMIData();
    }

    public override void UpdateData(byte[] data) {
        base.UpdateData(data);
        ProcessDMIData();
    }

    private void ProcessDMIData() {
        if (!IsValidPNG()) throw new Exception("Attempted to create a DMI using an invalid PNG");

        using Stream dmiStream = new MemoryStream(Data);
        DMIParser.ParsedDMIDescription description = DMIParser.ParseDMI(dmiStream);

        dmiStream.Seek(0, SeekOrigin.Begin);

        Image<Rgba32> image = Image.Load<Rgba32>(dmiStream);
        Texture = IoCManager.Resolve<IClyde>().LoadTextureFromImage(image);
        IconSize = new Vector2i(description.Width, description.Height);
        Description = description;

        _states.Clear();
        foreach (DMIParser.ParsedDMIState parsedState in description.States.Values) {
            State state = new State(Texture, parsedState, description.Width, description.Height);

            _states.Add(parsedState.Name, state);
        }
    }

    public State? GetState(string? stateName) {
        if (stateName == null || !_states.ContainsKey(stateName))
            return _states.TryGetValue(string.Empty, out var state) ? state : null; // Default state, if one exists

        return _states[stateName];
    }

    private bool IsValidPNG() {
        if (Data.Length < PngHeader.Length) return false;

        for (int i=0; i<PngHeader.Length; i++) {
            if (Data[i] != PngHeader[i]) return false;
        }

        return true;
    }

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

        public AtlasTexture[] GetFrames(AtomDirection direction) {
            // Find another direction to use if this one doesn't exist
            if (!Frames.ContainsKey(direction)) {
                // The diagonal directions attempt to use east/west
                if (direction is AtomDirection.Northeast or AtomDirection.Southeast)
                    direction = AtomDirection.East;
                else if (direction is AtomDirection.Northwest or AtomDirection.Southwest)
                    direction = AtomDirection.West;

                // Use the south direction if the above still isn't valid
                if (!Frames.ContainsKey(direction))
                    direction = AtomDirection.South;
            }

            return Frames[direction];
        }
    }
}
