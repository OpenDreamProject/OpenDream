using System.Buffers;
using System.IO;
using System.Linq;
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
    private static readonly ArrayPool<Rgba32> PixelArrayPool = ArrayPool<Rgba32>.Shared;

    public int Width, Height;
    public readonly Dictionary<string, IconState> States = new();

    private readonly DreamResourceManager _resourceManager;

    private IconResource? _cachedDMI;

    private int FrameCount => States.Values.Sum(state => state.Frames * DMIParser.GetExportedDirectionCount(state.Directions));

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
    }

    /// <summary>
    /// Represents one of the icon frames an icon is made of.<br/>
    /// Contains everything needed to create a new DMI in <see cref="DreamIcon.GenerateDMI()"/>
    /// </summary>
    public sealed class IconFrame {
        /// <summary>
        /// The image this icon frame originally comes from<br/>
        /// Null if empty
        /// </summary>
        public readonly Image<Rgba32>? Image;

        /// <summary>
        /// The DMI information about this icon frame
        /// </summary>
        public readonly ParsedDMIFrame DMIFrame;

        /// <summary>
        /// The size of the original icon frame
        /// </summary>
        public readonly int Width, Height;

        public IconFrame(Image<Rgba32>? image, ParsedDMIFrame dmiFrame, int width, int height) {
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
    public IconResource GenerateDMI() {
        if (_cachedDMI != null)
            return _cachedDMI;

        int frameCount = FrameCount;

        int frameWidth = Width, frameHeight = Height;
        if (frameCount == 0) { // No frames creates a blank 32x32 image (TODO: should be world.icon_size)
            frameWidth = 32;
            frameHeight = 32;
        }

        Dictionary<string, ParsedDMIState> dmiStates = new(States.Count);
        int span = frameWidth * Math.Max(frameCount, 1);
        Rgba32[] pixels = PixelArrayPool.Rent(span * frameHeight);

        int currentFrame = 0;
        foreach (var iconStatePair in States) {
            var iconState = iconStatePair.Value;
            ParsedDMIState newState = new() { Name = iconStatePair.Key, Loop = false, Rewind = false };

            dmiStates.Add(newState.Name, newState);

            int exportedDirectionCount = DMIParser.GetExportedDirectionCount(iconState.Directions);
            for (int directionIndex = 0; directionIndex < exportedDirectionCount; directionIndex++) {
                AtomDirection direction = DMIParser.DMIFrameDirections[directionIndex];
                int firstFrame = currentFrame;

                currentFrame += iconState.Frames;
                if (!iconState.Directions.TryGetValue(direction, out var frames))
                    continue; // Blank frames

                var newFrames = DrawFrames(pixels, firstFrame, frames, direction);
                newState.Directions.Add(direction, newFrames);
            }
        }

        Image<Rgba32> dmiImage = Image.LoadPixelData(pixels, span, frameHeight);
        ParsedDMIDescription newDescription = new() {Width = frameWidth, Height = frameHeight, States = dmiStates};

        PixelArrayPool.Return(pixels, clearArray: true);

        using (MemoryStream dmiImageStream = new MemoryStream()) {
            var pngTextData = new PngTextData("Description", newDescription.ExportAsText(), null, null);
            var pngMetadata = dmiImage.Metadata.GetPngMetadata();
            pngMetadata.TextData.Add(pngTextData);

            dmiImage.SaveAsPng(dmiImageStream);

            IconResource newResource = _resourceManager.CreateIconResource(dmiImageStream.GetBuffer(), dmiImage, newDescription);
            _cachedDMI = newResource;
            return _cachedDMI;
        }
    }

    public void ApplyOperation(IDreamIconOperation operation) {
        operation.OnApply(this);

        // The operation gets applied to every current frame, but not any inserted after this
        _operations.Add( (FrameCount, operation) );
        _cachedDMI = null;
    }

    public void InsertStates(IconResource icon, DreamValue state, DreamValue dir, DreamValue frame,
        bool isConstructor = false) {
        bool copyingAllDirs = !dir.TryGetValueAsInteger(out var dirVal);
        bool copyingAllStates = !state.TryGetValueAsString(out var copyingState);
        bool copyingAllFrames = !frame.TryGetValueAsInteger(out var copyingFrame);
        // TODO: Copy movement states?

        AtomDirection copyingDirection = (AtomDirection) dirVal;
        if (!Enum.IsDefined(copyingDirection) || copyingDirection == AtomDirection.None) {
            copyingAllDirs = true;
        }

        // The size of every state will be resized to match the largest state
        Width = Math.Max(Width, icon.DMI.Width);
        Height = Math.Max(Height, icon.DMI.Height);

        if (copyingAllStates) {
            foreach (var copyStateName in icon.DMI.States.Keys) {
                InsertState(icon, copyStateName, copyStateName,
                    copyingAllDirs ? null : copyingDirection, copyingAllFrames ? null : copyingFrame,
                    isConstructor: isConstructor);
            }
        } else {
            InsertState(icon, isConstructor ? string.Empty : copyingState!, copyingState!,
                copyingAllDirs ? null : copyingDirection, copyingAllFrames ? null : copyingFrame,
                isConstructor: isConstructor);
        }
    }

    private void InsertState(IconResource icon, string stateName, string copyingState, AtomDirection? dir = null,
        int? frame = null, bool isConstructor = false) {
        ParsedDMIState? inserting = icon.DMI.GetStateOrDefault(copyingState);
        if (inserting == null)
            return;

        // TODO: Passing "asSouth: isConstructor" here would be the correct behavior
        // But that currently breaks /icon.Insert(other_icon, dir=...) in some important cases
        var insertingDirections = inserting.GetFrames(dir, frame - 1, asSouth: false);

        if (!States.TryGetValue(stateName, out var iconState)) {
            iconState = new IconState();
        }

        foreach (var insertingPair in insertingDirections) {
            if (insertingPair.Value.Length == 0)
                continue;

            List<IconFrame> iconFrames = new(insertingPair.Value.Length);

            foreach (var dmiFrame in insertingPair.Value) {
                iconFrames.Add(new IconFrame(icon.Texture, dmiFrame, icon.DMI.Width, icon.DMI.Height));
            }

            iconState.Directions[insertingPair.Key] = iconFrames;
            iconState.Frames = Math.Max(iconState.Frames, iconFrames.Count);
        }

        // Only add the state if it contains any frames
        if (!States.ContainsKey(stateName) && iconState.Frames > 0) {
            States.Add(stateName, iconState);
        }

        _cachedDMI = null;
    }

    private ParsedDMIFrame[] DrawFrames(Rgba32[] pixels, int firstFrameIndex, List<IconFrame> frames, AtomDirection dir) {
        ParsedDMIFrame[] newFrames = new ParsedDMIFrame[frames.Count];
        int x = firstFrameIndex * Width;
        int imageSpan = FrameCount * Width;

        for (var frameIndex = 0; frameIndex < frames.Count; frameIndex++) {
            var frame = frames[frameIndex];

            newFrames[frameIndex] = new ParsedDMIFrame {X = x, Y = 0, Delay = frame.DMIFrame.Delay};
            if (frameIndex > frames.Count)
                continue; // Empty frame

            ParsedDMIFrame dmiFrame = frame.DMIFrame;
            Image<Rgba32>? image = frame.Image;
            int srcFrameX = dmiFrame.X, srcFrameY = dmiFrame.Y;
            if (image != null) {
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
                image?.ProcessPixelRows(accessor => {
                    for (int y = 0; y < Height; y++) {
                        var rowSpan = accessor.GetRowSpan(srcFrameY + y);

                        for (int frameX = 0; frameX < Width; frameX++) {
                            int pixelLocation = (y * imageSpan) + x + frameX;

                            pixels[pixelLocation] = rowSpan[srcFrameX + frameX];
                        }
                    }
                });
            }

            foreach (var operation in _operations) {
                if (operation.AppliedFrames <= firstFrameIndex + frameIndex)
                    break; // operation.AppliedFrames should be in ascending order; we can quit now

                var bounds = UIBox2i.FromDimensions(x, 0, Width, Height);
                operation.Operation.ApplyToFrame(pixels, imageSpan, frameIndex, dir, bounds);
            }

            x += Width;
        }

        return newFrames;
    }
}

public interface IDreamIconOperation {
    public void OnApply(DreamIcon icon);
    public void ApplyToFrame(Rgba32[] pixels, int imageSpan, int frame, AtomDirection dir, UIBox2i bounds);
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

        if (_type is not BlendType.Overlay and not BlendType.Underlay and not BlendType.Multiply and not BlendType.Add and not BlendType.Subtract)
            throw new NotImplementedException($"\"{_type}\" blending is not implemented");
    }

    public virtual void OnApply(DreamIcon icon) { }

    public virtual void ApplyToFrame(Rgba32[] pixels, int imageSpan, int frame, AtomDirection dir, UIBox2i bounds) {
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

            case BlendType.Multiply: {
                pixels[dstPixelPosition].R = (byte)Math.Round((double)dst.R * src.R / 0xFF); // It rounds!
                pixels[dstPixelPosition].G = (byte)Math.Round((double)dst.G * src.G / 0xFF);
                pixels[dstPixelPosition].B = (byte)Math.Round((double)dst.B * src.B / 0xFF);

                pixels[dstPixelPosition].A = (byte)Math.Round((double)dst.A * src.A / 0xFF);
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
    private readonly ParsedDMIState? _blendingState;

    public DreamIconOperationBlendImage(BlendType type, int xOffset, int yOffset, DreamValue blending) : base(type, xOffset, yOffset) {
        //TODO: Find a way to get rid of this!
        var resourceManager = IoCManager.Resolve<DreamResourceManager>();

        if (!resourceManager.TryLoadIcon(blending, out var blendingIcon)) {
            throw new Exception($"Value {blending} is not a valid icon to blend");
        }

        _blending = blendingIcon.Texture;
        _blendingState = blendingIcon.DMI.States.Values.FirstOrDefault();
    }

    public override void OnApply(DreamIcon icon) {
        if (_blendingState == null)
            return;

        // If any states in the icon have less directions than the one we're blending onto it,
        // We give them the new directions.
        foreach (var state in icon.States.Values) {
            foreach (var dir in _blendingState.Directions.Keys) {
                if (!state.Directions.ContainsKey(dir)) { // Direction doesn't exist, add it
                    var newFrames = new List<DreamIcon.IconFrame>(state.Frames);

                    // Create the empty frames
                    for (int i = 0; i < state.Frames; i++) {
                        newFrames.Add(new(null, new ParsedDMIFrame(), icon.Width, icon.Height));
                    }

                    state.Directions.Add(dir, newFrames);
                }
            }
        }

        // TODO: We add frames too
    }

    public override void ApplyToFrame(Rgba32[] pixels, int imageSpan, int frame, AtomDirection dir, UIBox2i bounds) {
        if (_blendingState?.Directions.TryGetValue(dir, out var blendingDirFrames) is not true)
            return;
        if (blendingDirFrames.Length <= frame)
            return;

        var blendingFrame = blendingDirFrames[frame];

        _blending.ProcessPixelRows(accessor => {
            // TODO: x & y offsets

            for (int y = bounds.Top; y < bounds.Bottom; y++) {
                var row = accessor.GetRowSpan(blendingFrame.Y + y - bounds.Top);

                for (int x = bounds.Left; x < bounds.Right; x++) {
                    int dstPixelPosition = (y * imageSpan) + x;
                    Rgba32 src = row[blendingFrame.X + x - bounds.Left];

                    BlendPixel(pixels, dstPixelPosition, src);
                }
            }
        });
    }
}

public sealed class DreamIconOperationBlendColor : DreamIconOperationBlend {
    private readonly Rgba32 _color;

    public DreamIconOperationBlendColor(BlendType type, int xOffset, int yOffset, Color color) : base(type, xOffset, yOffset) {
        _color = new Rgba32(color.RByte, color.GByte, color.BByte, color.AByte);
    }

    public override void ApplyToFrame(Rgba32[] pixels, int imageSpan, int frame, AtomDirection dir, UIBox2i bounds) {
        // TODO: x & y offsets

        for (int y = bounds.Top; y < bounds.Bottom; y++) {
            for (int x = bounds.Left; x < bounds.Right; x++) {
                int dstPixelPosition = (y * imageSpan) + x;

                BlendPixel(pixels, dstPixelPosition, _color);
            }
        }
    }
}
