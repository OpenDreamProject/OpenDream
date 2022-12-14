using System.IO;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = Robust.Shared.Maths.Color;
using ParsedDMIDescription = OpenDreamShared.Resources.DMIParser.ParsedDMIDescription;
using ParsedDMIState = OpenDreamShared.Resources.DMIParser.ParsedDMIState;
using ParsedDMIFrame = OpenDreamShared.Resources.DMIParser.ParsedDMIFrame;

namespace OpenDreamRuntime.Objects;

public sealed class DreamIcon {
    public int Width, Height;
    public readonly Dictionary<string, IconState> States = new();

    private readonly DreamResourceManager _resourceManager;

    private int _frameCount;
    private (DreamResource, ParsedDMIDescription)? _cachedDMI;

    /// <summary>
    /// A list of operations to be applied when generating the DMI, along with what frames to apply them on
    /// </summary>
    private readonly List<(int AppliedFrames, IDreamIconOperation Operation)> _operations = new();

    /// <summary>
    /// Represents one of the icon states an icon is made of.
    /// </summary>
    public sealed class IconState {
        public int Frames;
        public readonly Dictionary<AtomDirection, List<IconFrame>> Directions = new();

        /// <summary>
        /// The total directions present in an exported DMI.<br/>
        /// An icon state in a DMI must contain either 1, 4, or 8 directions.
        /// </summary>
        public int ExportedDirectionCount {
            get {
                // If we have any of these directions then we export 8 directions
                foreach (var direction in Directions.Keys) {
                    switch (direction) {
                        case AtomDirection.Northeast:
                        case AtomDirection.Southeast:
                        case AtomDirection.Southwest:
                        case AtomDirection.Northwest:
                            return 8;
                    }
                }

                // Any of these means 4 directions
                foreach (var direction in Directions.Keys) {
                    switch (direction) {
                        case AtomDirection.North:
                        case AtomDirection.East:
                        case AtomDirection.West:
                            return 4;
                    }
                }

                // Otherwise, 1 direction
                return 1;
            }
        }
    }

    /// <summary>
    /// Represents one of the icon frames an icon is made of.<br/>
    /// Contains everything needed to create a new DMI in <see cref="DreamIcon.GenerateDMI()"/>
    /// </summary>
    public sealed class IconFrame {
        /// <summary>
        /// The image this icon frame originally comes from
        /// </summary>
        public Image<Rgba32> Image;

        /// <summary>
        /// The DMI information about this icon frame
        /// </summary>
        public ParsedDMIFrame DMIFrame;

        /// <summary>
        /// The size of the original icon frame
        /// </summary>
        public int Width, Height;

        public IconFrame(Image<Rgba32> image, ParsedDMIFrame dmiFrame, int width, int height) {
            Image = image;
            DMIFrame = dmiFrame;
            Width = width;
            Height = height;
        }
    }

    public DreamIcon(DreamResourceManager resourceManager) {
        _resourceManager = resourceManager;
    }

    /// <summary>
    /// Generate a DMI using all the inserted icon states
    /// </summary>
    /// <remarks>The resulting DMI will consist of one long flat row of frames</remarks>
    /// <returns>The DreamResource containing the DMI and the ParsedDMIDescription used to construct it</returns>
    /// <exception cref="NotImplementedException">Using icon states of various sizes is unimplemented</exception>
    public (DreamResource Resource, ParsedDMIDescription Description) GenerateDMI() {
        if (_cachedDMI != null)
            return _cachedDMI.Value;

        int frameWidth = Width, frameHeight = Height;
        if (_frameCount == 0) { // No frames creates a blank 32x32 image (TODO: should be world.icon_size)
            frameWidth = 32;
            frameHeight = 32;
        }

        Dictionary<string, ParsedDMIState> dmiStates = new(States.Count);
        int span = frameWidth * Math.Max(_frameCount, 1);
        Rgba32[] pixels = new Rgba32[span * frameHeight];

        int currentFrame = 0;
        foreach (var iconStatePair in States) {
            var iconState = iconStatePair.Value;
            ParsedDMIState newState = new() { Name = iconStatePair.Key, Loop = false, Rewind = false };

            dmiStates.Add(newState.Name, newState);

            int exportedDirectionCount = iconState.ExportedDirectionCount;
            for (int directionIndex = 0; directionIndex < exportedDirectionCount; directionIndex++) {
                AtomDirection direction = DMIParser.DMIFrameDirections[directionIndex];
                int firstFrame = currentFrame;

                currentFrame += iconState.Frames;
                if (!iconState.Directions.TryGetValue(direction, out var frames))
                    continue; // Blank frames

                var newFrames = DrawFrames(pixels, firstFrame, frames);
                newState.Directions.Add(direction, newFrames);
            }
        }

        Image<Rgba32> dmiImage = Image.LoadPixelData(pixels, span, frameHeight);
        ParsedDMIDescription newDescription = new() {Width = frameWidth, Height = frameHeight, States = dmiStates};

        using (MemoryStream dmiImageStream = new MemoryStream()) {
            var pngTextData = new PngTextData("Description", newDescription.ExportAsText(), null, null);
            var pngMetadata = dmiImage.Metadata.GetPngMetadata();
            pngMetadata.TextData.Add(pngTextData);

            dmiImage.SaveAsPng(dmiImageStream);

            DreamResource newResource = _resourceManager.CreateResource(dmiImageStream.GetBuffer());
            _cachedDMI = (newResource, newDescription);
            return _cachedDMI.Value;
        }
    }

    public void ApplyOperation(IDreamIconOperation operation) {
        // The operation gets applied to every current frame, but not any inserted after this
        _operations.Add( (_frameCount, operation) );
        _cachedDMI = null;
    }

    public void InsertStates(DreamResource resource, ParsedDMIDescription fromDescription, DreamValue state,
        DreamValue dir, DreamValue frame, bool useStateName = true) {
        bool copyingAllDirs = !dir.TryGetValueAsInteger(out var dirVal);
        bool copyingAllStates = !state.TryGetValueAsString(out var copyingState);
        bool copyingAllFrames = !frame.TryGetValueAsInteger(out var copyingFrame);
        // TODO: Copy movement states?

        AtomDirection copyingDirection = (AtomDirection) dirVal;
        if (!Enum.IsDefined(copyingDirection) || copyingDirection == AtomDirection.None) {
            copyingAllDirs = true;
        }

        // The size of every state will be resized to match the largest state
        Width = Math.Max(Width, fromDescription.Width);
        Height = Math.Max(Height, fromDescription.Height);

        Image<Rgba32> image = _resourceManager.LoadImage(resource);

        if (copyingAllStates) {
            foreach (var copyStateName in fromDescription.States.Keys) {
                InsertState(image, fromDescription, copyStateName, copyStateName,
                    copyingAllDirs ? null : copyingDirection, copyingAllFrames ? null : copyingFrame);
            }
        } else {
            InsertState(image, fromDescription, useStateName ? copyingState! : String.Empty, copyingState!,
                copyingAllDirs ? null : copyingDirection, copyingAllFrames ? null : copyingFrame);
        }
    }

    private void InsertState(Image<Rgba32> image, ParsedDMIDescription description, string stateName,
        string copyingState, AtomDirection? dir = null, int? frame = null) {
        ParsedDMIState? inserting = description.GetStateOrDefault(copyingState);
        if (inserting == null)
            return;

        Dictionary<AtomDirection, ParsedDMIFrame[]> insertingDirections = inserting.GetFrames(dir, frame - 1);

        if (!States.TryGetValue(stateName, out var iconState)) {
            iconState = new IconState();
            States.Add(stateName, iconState);
        }

        _frameCount -= iconState.Frames * iconState.ExportedDirectionCount;

        foreach (var insertingPair in insertingDirections) {
            List<IconFrame> iconFrames = new(insertingPair.Value.Length);

            foreach (var dmiFrame in insertingPair.Value) {
                iconFrames.Add(new IconFrame(image, dmiFrame, description.Width, description.Height));
            }

            iconState.Directions[insertingPair.Key] = iconFrames;
            iconState.Frames = Math.Max(iconState.Frames, iconFrames.Count);
        }

        _frameCount += iconState.Frames * iconState.ExportedDirectionCount;
        _cachedDMI = null;
    }

    private ParsedDMIFrame[] DrawFrames(Rgba32[] pixels, int firstFrameIndex, List<IconFrame> frames) {
        ParsedDMIFrame[] newFrames = new ParsedDMIFrame[frames.Count];
        int x = firstFrameIndex * Width;
        int imageSpan = _frameCount * Width;

        for (var frameIndex = 0; frameIndex < frames.Count; frameIndex++) {
            var frame = frames[frameIndex];

            newFrames[frameIndex] = new ParsedDMIFrame {X = x, Y = 0, Delay = frame.DMIFrame.Delay};
            if (frameIndex > frames.Count)
                continue; // Empty frame

            ParsedDMIFrame dmiFrame = frame.DMIFrame;
            Image<Rgba32> image = frame.Image;
            int srcFrameX = dmiFrame.X, srcFrameY = dmiFrame.Y;
            if (frame.Width != Width || frame.Height != Height) { // Resize the frame to match our size
                // There is no way this is performant with a large number of frames...
                // TODO: Try to reduce the amount of cloned images here somehow
                image = image.Clone();
                image.Mutate(mutator => {
                    mutator.Crop(new Rectangle(srcFrameX, srcFrameY, frame.Width, frame.Height));
                    mutator.Resize(Width, Height);
                    srcFrameX = 0;
                    srcFrameY = 0;
                });
            }

            // Copy the frame from the original image to the new one
            image.ProcessPixelRows(accessor => {
                for (int y = 0; y < Height; y++) {
                    var rowSpan = accessor.GetRowSpan(srcFrameY + y);

                    for (int frameX = 0; frameX < Width; frameX++) {
                        int pixelLocation = (y * imageSpan) + x + frameX;

                        pixels[pixelLocation] = rowSpan[srcFrameX + frameX];
                    }
                }

                foreach (var operation in _operations) {
                    if (operation.AppliedFrames <= firstFrameIndex + frameIndex)
                        break; // operation.AppliedFrames should be in ascending order; we can quit now

                    var bounds = UIBox2i.FromDimensions(x, 0, Width, Height);
                    operation.Operation.ApplyToFrame(pixels, imageSpan, firstFrameIndex + frameIndex, bounds);
                }
            });

            x += Width;
        }

        return newFrames;
    }
}

public interface IDreamIconOperation {
    public void ApplyToFrame(Rgba32[] pixels, int imageSpan, int frame, UIBox2i bounds);
}

[Virtual]
public class DreamIconOperationBlend : IDreamIconOperation {
    // With the same values as the ICON_* defines in DMStandard
    public enum BlendType {
        Add = 0,
        Subtract = 1,
        Multiply = 2,
        Overlay = 3,
        And = 4,
        Or = 5,
        Underlay = 6
    }

    private readonly BlendType _type;
    private readonly int _xOffset, _yOffset;

    protected DreamIconOperationBlend(BlendType type, int xOffset, int yOffset) {
        _type = type;
        _xOffset = xOffset;
        _yOffset = yOffset;

        if (_type is not BlendType.Overlay and not BlendType.Underlay and not BlendType.Add and not BlendType.Subtract)
            throw new NotImplementedException($"\"{_type}\" blending is not implemented");
    }

    public virtual void ApplyToFrame(Rgba32[] pixels, int imageSpan, int frame, UIBox2i bounds) {
        throw new NotImplementedException();
    }

    protected void BlendPixel(Rgba32[] pixels, int dstPixelPosition, Rgba32 src) {
        Rgba32 dst = pixels[dstPixelPosition];

        switch (_type) {
            case BlendType.Add: {
                pixels[dstPixelPosition].R = (byte)Math.Min(dst.R + src.R, byte.MaxValue);
                pixels[dstPixelPosition].G = (byte)Math.Min(dst.G + src.G, byte.MaxValue);
                pixels[dstPixelPosition].B = (byte)Math.Min(dst.B + src.B, byte.MaxValue);

                // BYOND uses the smaller of the two alphas
                pixels[dstPixelPosition].A = Math.Min(dst.A, src.A);
                break;
            }
            case BlendType.Subtract: {
                pixels[dstPixelPosition].R = (byte)Math.Max(dst.R - src.R, byte.MinValue);
                pixels[dstPixelPosition].G = (byte)Math.Max(dst.G - src.G, byte.MinValue);
                pixels[dstPixelPosition].B = (byte)Math.Max(dst.B - src.B, byte.MinValue);

                // BYOND uses the smaller of the two alphas
                pixels[dstPixelPosition].A = Math.Min(dst.A, src.A);
                break;
            }

            case BlendType.Overlay: {
                pixels[dstPixelPosition].R = (byte) (dst.R + (src.R - dst.R) * src.A / 255);
                pixels[dstPixelPosition].G = (byte) (dst.G + (src.G - dst.G) * src.A / 255);
                pixels[dstPixelPosition].B = (byte) (dst.B + (src.B - dst.B) * src.A / 255);

                byte highAlpha = Math.Max(dst.A, src.A);
                byte lowAlpha = Math.Min(dst.A, src.A);
                pixels[dstPixelPosition].A = (byte) (highAlpha + (highAlpha * lowAlpha / 255));
                break;
            }
            case BlendType.Underlay: {
                // Opposite of overlay
                (dst, src) = (src, dst);
                goto case BlendType.Overlay;
            }
        }
    }
}

public sealed class DreamIconOperationBlendImage : DreamIconOperationBlend {
    private readonly Image<Rgba32> _blending;
    private readonly ParsedDMIDescription _blendingDescription;

    public DreamIconOperationBlendImage(BlendType type, int xOffset, int yOffset, DreamValue blending) : base(type, xOffset, yOffset) {
        var objectTree = IoCManager.Resolve<IDreamObjectTree>();
        var resourceManager = IoCManager.Resolve<DreamResourceManager>();
        (var blendingResource, _blendingDescription) = DreamMetaObjectIcon.GetIconResourceAndDescription(objectTree, resourceManager, blending);
        _blending = resourceManager.LoadImage(blendingResource);
    }

    public override void ApplyToFrame(Rgba32[] pixels, int imageSpan, int frame, UIBox2i bounds) {
        _blending.ProcessPixelRows(accessor => {
            // The first frame of the source image blends with the first frame of the destination image
            // The second frame blends with the second, and so on
            // TODO: What happens if each icon state has a different number of frames?
            (int X, int Y)? srcFramePos = CalculateFramePosition(frame);
            if (srcFramePos == null)
                return;

            // TODO: x & y offsets

            for (int y = bounds.Top; y < bounds.Bottom; y++) {
                var row = accessor.GetRowSpan(srcFramePos.Value.Y + y - bounds.Top);

                for (int x = bounds.Left; x < bounds.Right; x++) {
                    int dstPixelPosition = (y * imageSpan) + x;
                    Rgba32 dst = pixels[dstPixelPosition];
                    Rgba32 src = row[srcFramePos.Value.X + x - bounds.Left];

                    BlendPixel(pixels, dstPixelPosition, src);
                }
            }
        });
    }

    /// <summary>
    /// Calculate the position of a frame in the image used to blend
    /// </summary>
    /// <param name="frame">The frame's index</param>
    /// <returns>The frame's position, or null if there is no such frame</returns>
    private (int X, int Y)? CalculateFramePosition(int frame) {
        int totalRows = _blending.Height / _blendingDescription.Height;
        int framesPerRow = _blending.Width / _blendingDescription.Width;
        int row = frame / framesPerRow;
        int column = frame - (row * framesPerRow);

        if (row >= totalRows)
            return null;

        return (column * _blendingDescription.Width, row * _blendingDescription.Height);
    }
}

public sealed class DreamIconOperationBlendColor : DreamIconOperationBlend {
    private readonly Rgba32 _color;

    public DreamIconOperationBlendColor(BlendType type, int xOffset, int yOffset, Color color) : base(type, xOffset, yOffset) {
        _color = new Rgba32(color.RByte, color.GByte, color.BByte, color.AByte);
    }

    public override void ApplyToFrame(Rgba32[] pixels, int imageSpan, int frame, UIBox2i bounds) {
        // TODO: x & y offsets

        for (int y = bounds.Top; y < bounds.Bottom; y++) {
            for (int x = bounds.Left; x < bounds.Right; x++) {
                int dstPixelPosition = (y * imageSpan) + x;

                BlendPixel(pixels, dstPixelPosition, _color);
            }
        }
    }
}
