using System.Collections.Generic;
using System.IO;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectIcon : DreamMetaObjectDatum
    {
        [Dependency] private readonly DreamResourceManager _rscMan = default;
        public struct DreamIcon {
            public DreamResource Resource;
            public string? State;
            public AtomDirection Direction;
            public int Frame;
            public byte? Moving;
            public DMIParser.ParsedDMIDescription Description;

            public DreamIcon(DreamResource rsc, DreamValue? state, DreamValue? dir, DreamValue? frame, DreamValue? moving)
            {
                Resource = rsc;
                Description = DMIParser.ParseDMI(new MemoryStream(rsc.ResourceData));

                // TODO confirm BYOND behavior of invalid types

                if (state is not null && state.Value.TryGetValueAsString(out var iconState))
                {
                    State = iconState;
                }
                else
                {
                    State = null;
                }

                if (dir is not null && dir.Value.TryGetValueAsInteger(out var dirVal) && (AtomDirection)dirVal != AtomDirection.None)
                {
                    Direction = (AtomDirection)dirVal;
                }
                else
                {
                    Direction = AtomDirection.South;
                }

                if (frame is not null && frame.Value.TryGetValueAsInteger(out var frameVal))
                {
                    Frame = frameVal;
                }
                else
                {
                    Frame = 0;
                }

                if (moving != DreamValue.Null)
                {
                    if (moving is not null && (!moving.Value.TryGetValueAsInteger(out var movingVal) || movingVal != 0))
                    {
                        Moving = 1;
                    }
                    else
                    {
                        Moving = 0;
                    }
                }
                else
                {
                    Moving = null;
                }
            }
        }

        public static Dictionary<DreamObject, DreamIcon> ObjectToDreamIcon = new();

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            DreamValue icon = creationArguments.GetArgument(0, "icon");
            DreamValue state = creationArguments.GetArgument(1, "icon_state");
            DreamValue dir = creationArguments.GetArgument(2, "dir");
            DreamValue frame = creationArguments.GetArgument(3, "frame");
            DreamValue moving = creationArguments.GetArgument(4, "moving");

            DreamIcon dreamIcon;

            if (icon.TryGetValueAsDreamObjectOfType(DreamPath.Icon, out DreamObject copyFrom)) {
                dreamIcon = ObjectToDreamIcon[copyFrom];
            } else if (icon.TryGetValueAsString(out string fileString))
            {
                var ext = Path.GetExtension(fileString);
                switch (ext) // TODO implement other icon file types
                {
                    case ".dmi":
                        dreamIcon = new DreamIcon(_rscMan.LoadResource(fileString), state, dir, frame, moving);
                        Stream dmiStream = new MemoryStream(dreamIcon.Resource.ResourceData);
                        dreamIcon.Description = DMIParser.ParseDMI(dmiStream);
                        dreamObject.SetVariableValue("icon", new DreamValue(dreamIcon.Resource.ResourcePath));
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
                dreamIcon = new DreamIcon(rsc, state, dir, frame, moving);
                dreamObject.SetVariableValue("icon", new DreamValue(dreamIcon.Resource.ResourcePath));
            } else {
                throw new Exception("Invalid icon file " + icon);
            }

            ObjectToDreamIcon.Add(dreamObject, dreamIcon);

        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            ObjectToDreamIcon.Remove(dreamObject);

            base.OnObjectDeleted(dreamObject);
        }
    }
}
