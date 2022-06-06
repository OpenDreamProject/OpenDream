using System.IO;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectIcon : DreamMetaObjectDatum
    {
        [Dependency] private readonly DreamResourceManager _rscMan = default!;

        public enum DreamIconMovingMode : byte
        {
            Both = 0,
            Movement = 1,
            NonMovement = 2,

        }

        public static Dictionary<DreamObject, DreamIconObject> ObjectToDreamIcon = new();

        public struct DreamIconObject {
            // Actual DMI data
            public DMIParser.ParsedDMIDescription Description; // TODO Eventually this should probably be removed in favor of just directly storing the data for the subset of the DMI that we actually care about

            // These vars correspond to the args in icon/new() and the resulting /icon obj, not the actual DMI data
            public string Icon;
            public string? State; // Specific icon_state. Null is all states.
            public AtomDirection? Direction; // Specific dir. Null is all dirs.
            public byte? Frame; //1-indexed. Specific frame. Null is all frames.
            public DreamIconMovingMode Moving;

            public DreamIconObject(DreamResource rsc, DreamValue state, DreamValue dir, DreamValue frame, DreamValue moving)
            {
                if (Path.GetExtension(rsc.ResourcePath) != ".dmi")
                {
                    throw new Exception("Invalid icon file");
                }

                Description = DMIParser.ParseDMI(new MemoryStream(rsc.ResourceData));
                Icon = rsc.ResourcePath;

                // TODO confirm BYOND behavior of invalid args for icon, dir, and frame

                if (state.TryGetValueAsString(out var iconState))
                {
                    State = iconState;
                }
                else
                {
                    State = null;
                }

                if (dir.TryGetValueAsInteger(out var dirVal) && (AtomDirection)dirVal != AtomDirection.None)
                {
                    Direction = (AtomDirection)dirVal;
                }
                else
                {
                    Direction = null;
                }

                if (frame.TryGetValueAsInteger(out var frameVal))
                {
                    //TODO: Figure out how many frames an icon can have and see if this needs to be bigger than a byte
                    Frame = Convert.ToByte(frameVal - 1); //1-indexed
                }
                else
                {
                    Frame = null;
                }

                if (moving != DreamValue.Null)
                {
                    if (moving.TryGetValueAsInteger(out var movingVal) && movingVal == 0)
                    {
                        Moving = DreamIconMovingMode.NonMovement;
                    }
                    else
                    {
                        Moving = DreamIconMovingMode.Movement;
                    }
                }
                else
                {
                    Moving = DreamIconMovingMode.Both;
                }
            }
        }

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            DreamValue icon = creationArguments.GetArgument(0, "icon");
            DreamValue state = creationArguments.GetArgument(1, "icon_state");
            DreamValue dir = creationArguments.GetArgument(2, "dir");
            DreamValue frame = creationArguments.GetArgument(3, "frame");
            DreamValue moving = creationArguments.GetArgument(4, "moving");

            DreamIconObject dreamIconObject;

            if (icon.TryGetValueAsDreamObjectOfType(DreamPath.Icon, out DreamObject copyFrom)) {
                dreamIconObject = ObjectToDreamIcon[copyFrom];
            } else if (icon.TryGetValueAsString(out string fileString))
            {
                var ext = Path.GetExtension(fileString);
                switch (ext) // TODO implement other icon file types
                {
                    case ".dmi":
                        dreamIconObject = new DreamIconObject(_rscMan.LoadResource(fileString), state, dir, frame, moving);
                        break;
                    case ".png":
                    case ".jpg":
                    case ".rsi": // RT-specific, not in BYOND
                    case ".gif":
                    case ".bmp":
                        throw new NotImplementedException($"Unimplemented icon type '{ext}'");
                    default:
                        throw new Exception($"Invalid icon file {fileString}");
                }

            } else if (icon.TryGetValueAsDreamResource(out var rsc))
            {
                dreamIconObject = new DreamIconObject(rsc, state, dir, frame, moving);
            } else {
                throw new Exception("Invalid icon file " + icon);
            }

            ObjectToDreamIcon.Add(dreamObject, dreamIconObject);

        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            ObjectToDreamIcon.Remove(dreamObject);

            base.OnObjectDeleted(dreamObject);
        }
    }
}
