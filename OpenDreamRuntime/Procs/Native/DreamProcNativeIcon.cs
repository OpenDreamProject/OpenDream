using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Procs.Native {
    static class DreamProcNativeIcon {
        [DreamProc("Width")]
        public static DreamValue NativeProc_Width(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamMetaObjectIcon.DreamIconObject dreamIconObject = DreamMetaObjectIcon.ObjectToDreamIcon[instance];

            return new DreamValue(dreamIconObject.Description.Width);
        }

        [DreamProc("Height")]
        public static DreamValue NativeProc_Height(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamMetaObjectIcon.DreamIconObject dreamIconObject = DreamMetaObjectIcon.ObjectToDreamIcon[instance];

            return new DreamValue(dreamIconObject.Description.Height);
        }

        [DreamProc("Insert")]
        [DreamProcParameter("new_icon", Type = DreamValue.DreamValueType.DreamObject)]
        [DreamProcParameter("icon_state", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("dir", Type = DreamValue.DreamValueType.Float)]
        [DreamProcParameter("frame", Type = DreamValue.DreamValueType.Float)]
        [DreamProcParameter("moving", Type = DreamValue.DreamValueType.Float)]
        [DreamProcParameter("delay", Type = DreamValue.DreamValueType.Float)]
        public static DreamValue NativeProc_Insert(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            //TODO Figure out what happens when you pass the wrong types as args

            if (!arguments.GetArgument(0, "new_icon").TryGetValueAsDreamObject(out var new_icon))
            {
                // TODO: Implement this
                if (arguments.GetArgument(0, "new_icon").TryGetValueAsDreamResource(out _))
                {
                    throw new NotImplementedException("icon.Insert() doesn't support DreamResources yet");
                }
            }
            arguments.GetArgument(1, "icon_state").TryGetValueAsString(out var icon_state);
            AtomDirection? dir = null;
            if (arguments.GetArgument(2, "dir").TryGetValueAsInteger(out var dirNum))
            {
                dir = (AtomDirection)dirNum;
            }
            int? frame = null;
            if (arguments.GetArgument(3, "frame").TryGetValueAsInteger(out var frameNum))
            {
                frame = frameNum;
            }
            bool moving = !(arguments.GetArgument(4, "moving").TryGetValueAsInteger(out var movingNum) && movingNum == 0);
            int? delay = null;
            if (arguments.GetArgument(5, "delay").TryGetValueAsInteger(out var delayNum))
            {
                delay = delayNum;
            }

            DreamMetaObjectIcon.DreamIconObject instanceIconObject = DreamMetaObjectIcon.ObjectToDreamIcon[instance];
            DreamMetaObjectIcon.DreamIconObject newIconObject = DreamMetaObjectIcon.ObjectToDreamIcon[new_icon];
            instanceIconObject.Description.InsertIcon(newIconObject.Description, icon_state, dir, frame, delay);

            instanceIconObject.Moving = moving ? DreamMetaObjectIcon.DreamIconMovingMode.Movement : DreamMetaObjectIcon.DreamIconMovingMode.NonMovement;
            return DreamValue.Null;
        }
    }
}
