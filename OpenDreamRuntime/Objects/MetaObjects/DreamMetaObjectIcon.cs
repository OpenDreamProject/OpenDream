using System.IO;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using ParsedDMIDescription = OpenDreamShared.Resources.DMIParser.ParsedDMIDescription;
using ParsedDMIState = OpenDreamShared.Resources.DMIParser.ParsedDMIState;
using ParsedDMIFrame = OpenDreamShared.Resources.DMIParser.ParsedDMIFrame;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectIcon : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly DreamResourceManager _rscMan = default!;

        public DreamMetaObjectIcon() {
            IoCManager.InjectDependencies(this);
        }

        public static readonly Dictionary<DreamObject, DreamIconObject> ObjectToDreamIcon = new();

        public sealed class DreamIconObject {
            public int Width = 0, Height = 0;
            public readonly Dictionary<string, IconState> States = new();

            private readonly DreamResourceManager _resourceManager;

            private int _frameCount = 0;
            private (DreamResource, ParsedDMIDescription)? _cachedDMI;

            /// <summary>
            /// Represents one of the icon states an icon is made of.
            /// </summary>
            public class IconState {
                public int Frames = 0;
                public Dictionary<AtomDirection, List<IconFrame>> Directions = new();
            }

            /// <summary>
            /// Represents one of the icon frames an icon is made of.<br/>
            /// Contains everything needed to create a new DMI in <see cref="DreamIconObject.GenerateDMI()"/>
            /// </summary>
            public struct IconFrame {
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
            }

            public DreamIconObject(DreamResourceManager resourceManager) {
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

                Dictionary<string, ParsedDMIState> dmiStates = new(States.Count);
                int frameWidth = Width, frameHeight = Height;
                int span = frameWidth * _frameCount;
                Rgba32[] pixels = new Rgba32[span * frameHeight];

                int currentFrame = 0;
                foreach (var iconStatePair in States) {
                    var iconState = iconStatePair.Value;
                    ParsedDMIState newState = new() { Name = iconStatePair.Key, Loop = false, Rewind = false };

                    dmiStates.Add(newState.Name, newState);

                    foreach (var directionPair in iconState.Directions) {
                        var newFrames = DrawFrames(pixels, currentFrame * frameWidth, directionPair.Value);

                        currentFrame += newFrames.Length;
                        newState.Directions.Add(directionPair.Key, newFrames);
                    }
                }

                Image<Rgba32> dmiImage = Image.LoadPixelData(pixels, span, frameHeight);
                ParsedDMIDescription newDescription = new() {Width = Width, Height = Height, States = dmiStates};

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

            public void InsertStates(DreamResource resource, ParsedDMIDescription fromDescription, DreamValue state,
                DreamValue dir, DreamValue frame) {
                bool copyingAllDirs = !dir.TryGetValueAsInteger(out var dirVal);
                bool copyingAllStates = !(state.TryGetValueAsString(out var copyingState) && fromDescription.States.ContainsKey(copyingState));
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
                    InsertState(image, fromDescription, String.Empty, copyingState!,
                        copyingAllDirs ? null : copyingDirection, copyingAllFrames ? null : copyingFrame);
                }
            }

            private void InsertState(Image<Rgba32> image, ParsedDMIDescription description, string stateName, string copyingState, AtomDirection? dir = null, int? frame = null) {
                ParsedDMIState inserting = description.States[copyingState];
                Dictionary<AtomDirection, ParsedDMIFrame[]> insertingDirections = inserting.GetFrames(dir, frame - 1);

                if (!States.TryGetValue(stateName, out var iconState)) {
                    iconState = new IconState();
                    States.Add(stateName, iconState);
                }

                _frameCount -= iconState.Frames * iconState.Directions.Count;

                foreach (var insertingPair in insertingDirections) {
                    List<IconFrame> iconFrames = new(insertingPair.Value.Length);

                    foreach (var dmiFrame in insertingPair.Value) {
                        iconFrames.Add(new IconFrame {
                            Image = image,
                            DMIFrame = dmiFrame,
                            Width = description.Width, Height = description.Height
                        });
                    }

                    iconState.Directions[insertingPair.Key] = iconFrames;
                    iconState.Frames = Math.Max(iconState.Frames, iconFrames.Count);
                }

                _frameCount += iconState.Frames * iconState.Directions.Count;
                _cachedDMI = null;
            }

            private ParsedDMIFrame[] DrawFrames(Rgba32[] pixels, int x, List<IconFrame> frames) {
                ParsedDMIFrame[] newFrames = new ParsedDMIFrame[frames.Count];

                for (var frameIndex = 0; frameIndex < frames.Count; frameIndex++) {
                    var frame = frames[frameIndex];

                    newFrames[frameIndex] = new ParsedDMIFrame {X = x, Y = 0, Delay = frame.DMIFrame.Delay};
                    if (frameIndex > frames.Count)
                        continue; // Empty frame

                    if (frame.Width != Width || frame.Height != Height)
                        throw new NotImplementedException("Icon scaling is not implemented");

                    // Copy the frame from the original image to the new one
                    frame.Image.ProcessPixelRows(accessor => {
                        ParsedDMIFrame dmiFrame = frame.DMIFrame;

                        for (int y = 0; y < Height; y++) {
                            var rowSpan = accessor.GetRowSpan(dmiFrame.Y + y);

                            for (int frameX = 0; frameX < Width; frameX++) {
                                int pixelLocation = (y * Width * _frameCount) + x + frameX;

                                pixels[pixelLocation] = rowSpan[dmiFrame.X + frameX];
                            }
                        }
                    });

                    x += Width;
                }

                return newFrames;
            }
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            ParentType?.OnObjectCreated(dreamObject, creationArguments);

            // TODO confirm BYOND behavior of invalid args for icon, dir, and frame
            DreamValue icon = creationArguments.GetArgument(0, "icon");
            DreamValue state = creationArguments.GetArgument(1, "icon_state");
            DreamValue dir = creationArguments.GetArgument(2, "dir");
            DreamValue frame = creationArguments.GetArgument(3, "frame");
            DreamValue moving = creationArguments.GetArgument(4, "moving");

            var (iconRsc, iconDescription) = GetIconResourceAndDescription(_rscMan, icon);

            DreamIconObject dreamIconObject = new(_rscMan);
            dreamIconObject.InsertStates(iconRsc, iconDescription, state, dir, frame);
            ObjectToDreamIcon.Add(dreamObject, dreamIconObject);
        }

        public void OnObjectDeleted(DreamObject dreamObject) {
            ObjectToDreamIcon.Remove(dreamObject);

            ParentType?.OnObjectDeleted(dreamObject);
        }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

            switch (varName) {
                case "icon":
                    // Setting the icon to anything other than a DreamResource will actually set it to null
                    if (value.Type != DreamValue.DreamValueType.DreamResource) {
                        dreamObject.SetVariableValue("icon", DreamValue.Null);
                    }

                    break;
            }
        }

        public static (DreamResource Resource, ParsedDMIDescription Description) GetIconResourceAndDescription(
            DreamResourceManager resourceManager, DreamValue value) {
            if (value.TryGetValueAsDreamObjectOfType(DreamPath.Icon, out var iconObj)) {
                DreamIconObject dreamIconObject = ObjectToDreamIcon[iconObj];

                return dreamIconObject.GenerateDMI();
            }

            DreamResource iconRsc;

            if (value.TryGetValueAsString(out var fileString)) {
                var ext = Path.GetExtension(fileString);

                switch (ext) {
                    case ".dmi":
                        iconRsc = resourceManager.LoadResource(fileString);
                        break;

                    // TODO implement other icon file types
                    case ".png":
                    case ".jpg":
                    case ".rsi": // RT-specific, not in BYOND
                    case ".gif":
                    case ".bmp":
                        throw new NotImplementedException($"Unimplemented icon type '{ext}'");
                    default:
                        throw new Exception($"Invalid icon file {fileString}");
                }
            } else if (!value.TryGetValueAsDreamResource(out iconRsc)) {
                throw new Exception($"Invalid icon {value}");
            }

            byte[]? rscData = iconRsc.ResourceData;
            if (rscData == null)
                throw new Exception($"No data in file {iconRsc} to construct icon from");

            return (iconRsc, DMIParser.ParseDMI(new MemoryStream(rscData)));
        }
    }
}
