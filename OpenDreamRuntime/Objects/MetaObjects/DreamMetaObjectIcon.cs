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
            public AtomDirection Direction = AtomDirection.South;
            public int Frame = 0;
            public int? Moving;
            public DMIParser.ParsedDMIDescription Description;
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
                switch (ext)
                {
                    case ".dmi":
                        dreamIcon = new DreamIcon() {Resource = _rscMan.LoadResource(fileString)};
                        Stream dmiStream = new MemoryStream(dreamIcon.Resource.ResourceData);
                        dreamIcon.Description = DMIParser.ParseDMI(dmiStream);
                        dreamObject.SetVariableValue("icon", new DreamValue(dreamIcon.Resource.ResourcePath));
                        break;
                    case ".png":
                    case ".jpg":
                    case ".rsi":
                    case ".gif":
                    case ".bmp":
                        throw new NotImplementedException($"Unimplemented icon type '{ext}'");
                    default:
                        throw new Exception($"Invalid icon file {fileString}");
                }

            } else if (icon.TryGetValueAsDreamResource(out var rsc))
            {
                dreamIcon = new DreamIcon() {Resource = rsc};
                Stream dmiStream = new MemoryStream(dreamIcon.Resource.ResourceData);
                dreamIcon.Description = DMIParser.ParseDMI(dmiStream);
                dreamObject.SetVariableValue("icon", new DreamValue(dreamIcon.Resource.ResourcePath));
            } else {
                throw new Exception("Invalid icon file " + icon);
            }

            if (state.TryGetValueAsString(out var iconState))
            {
                dreamIcon.State = iconState;
            }

            if (dir.TryGetValueAsInteger(out var dirVal) && (AtomDirection)dirVal != AtomDirection.None)
            {
                dreamIcon.Direction = (AtomDirection)dirVal;
            }

            if (frame.TryGetValueAsInteger(out var frameVal))
            {
                dreamIcon.Frame = frameVal;
            }

            if (moving != DreamValue.Null)
            {
                if (!moving.TryGetValueAsInteger(out var movingVal) || movingVal != 0)
                {
                    dreamIcon.Moving = 1;
                }
                else
                {
                    dreamIcon.Moving = 0;
                }
            }

            //dreamObject.ObjectDefinition.

            ObjectToDreamIcon.Add(dreamObject, dreamIcon);

        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            ObjectToDreamIcon.Remove(dreamObject);

            base.OnObjectDeleted(dreamObject);
        }
    }
}
