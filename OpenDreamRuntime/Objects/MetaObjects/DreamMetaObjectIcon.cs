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
    sealed class DreamMetaObjectIcon : IDreamMetaObject
    {
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

            /// <summary>
            /// Represents one of the icon states this icon is made of.
            /// Contains everything needed to create a new DMI in <see cref="DreamIconObject.GenerateDMI()"/>
            /// </summary>
            public struct IconState {
                /// <summary>
                /// The image this icon state originally comes from
                /// </summary>
                public Image<Rgba32> Image;

                /// <summary>
                /// The DMI information about this icon state
                /// </summary>
                public ParsedDMIState DMIState;

                /// <summary>
                /// The size of the original icon state
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
            public (DreamResource, ParsedDMIDescription) GenerateDMI() {
                Dictionary<string, ParsedDMIState> dmiStates = new(States.Count);
                int frameWidth = Width, frameHeight = Height;
                int span = frameWidth * _frameCount;
                Rgba32[] pixels = new Rgba32[span * frameHeight];

                int currentFrame = 0;
                foreach (var iconState in States.Values) {
                    ParsedDMIState state = iconState.DMIState;
                    ParsedDMIState newState = new() { Name = state.Name, Loop = state.Loop, Rewind = state.Rewind };

                    dmiStates.Add(newState.Name, newState);

                    if (iconState.Width != frameWidth || iconState.Height != frameHeight)
                        throw new NotImplementedException("Icon scaling is not implemented");

                    iconState.Image.ProcessPixelRows(accessor => {
                        foreach (var stateDir in state.Directions) {
                            ParsedDMIFrame[] newFrames = new ParsedDMIFrame[stateDir.Value.Length];

                            for (int frameIndex = 0; frameIndex < stateDir.Value.Length; frameIndex++) {
                                ParsedDMIFrame frame = stateDir.Value[frameIndex];
                                int x = currentFrame * frameWidth;

                                for (int y = 0; y < frameHeight; y++) {
                                    var rowSpan = accessor.GetRowSpan(frame.Y + y);

                                    for (int frameX = 0; frameX < frameWidth; frameX++) {
                                        int pixelLocation = (y * span) + x + frameX;

                                        pixels[pixelLocation] = rowSpan[frame.X + frameX];
                                    }
                                }

                                newFrames[frameIndex] = new ParsedDMIFrame { X = x, Y = 0, Delay = frame.Delay};
                                currentFrame++;
                            }

                            newState.Directions.Add(stateDir.Key, newFrames);
                        }
                    });
                }

                Image<Rgba32> dmiImage = Image.LoadPixelData(pixels, span, frameHeight);
                ParsedDMIDescription newDescription = new() {Width = Width, Height = Height, States = dmiStates};

                using (MemoryStream dmiImageStream = new MemoryStream()) {
                    var pngTextData = new PngTextData("Description", newDescription.ExportAsText(), null, null);
                    var pngMetadata = dmiImage.Metadata.GetPngMetadata();
                    pngMetadata.TextData.Add(pngTextData);

                    dmiImage.SaveAsPng(dmiImageStream);
                }

                // TODO: Overhaul the resource system so all the above can actually be used
                return (null, new ParsedDMIDescription());
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
                        InsertState(image, fromDescription, copyStateName,
                            copyingAllDirs ? null : copyingDirection, copyingAllFrames ? null : copyingFrame);
                    }
                } else {
                    InsertState(image, fromDescription, copyingState!,
                        copyingAllDirs ? null : copyingDirection, copyingAllFrames ? null : copyingFrame);
                }
            }

            private void InsertState(Image<Rgba32> image, ParsedDMIDescription description, string stateName, AtomDirection? dir = null, int? frame = null) {
                ParsedDMIState state = description.States[stateName];

                // Only create a copy if we're using a specific dir or frame
                if (dir != null || frame != null)
                    state = state.Copy(dir, frame - 1);

                _frameCount += state.FrameCount;
                States.Add(stateName, new IconState {
                    Image = image,
                    DMIState = state,
                    Width = description.Width,
                    Height = description.Height
                });
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
