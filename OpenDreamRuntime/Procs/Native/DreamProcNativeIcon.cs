using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Resources;

namespace OpenDreamRuntime.Procs.Native {
    static class DreamProcNativeIcon {
        [DreamProc("Width")]
        public static DreamValue NativeProc_Width(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamMetaObjectIcon.DreamIconObject dreamIconObject = DreamMetaObjectIcon.ObjectToDreamIcon[instance];

            return new DreamValue(dreamIconObject.Width);
        }

        [DreamProc("Height")]
        public static DreamValue NativeProc_Height(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamMetaObjectIcon.DreamIconObject dreamIconObject = DreamMetaObjectIcon.ObjectToDreamIcon[instance];

            return new DreamValue(dreamIconObject.Height);
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

            DreamValue newIcon = arguments.GetArgument(0, "new_icon");
            DreamValue iconState = arguments.GetArgument(1, "icon_state");
            DreamValue dir = arguments.GetArgument(2, "dir");
            DreamValue frame = arguments.GetArgument(3, "frame");
            DreamValue moving = arguments.GetArgument(4, "moving");
            DreamValue delay = arguments.GetArgument(5, "delay");

            // TODO: moving & delay

            var resourceManager = IoCManager.Resolve<DreamResourceManager>();
            var (iconRsc, iconDescription) = DreamMetaObjectIcon.GetIconResourceAndDescription(resourceManager, newIcon);

            DreamMetaObjectIcon.DreamIconObject iconObj = DreamMetaObjectIcon.ObjectToDreamIcon[instance];
            iconObj.InsertStates(iconRsc, iconDescription, iconState, dir, frame); // TODO: moving & delay
            return DreamValue.Null;
        }
    }
}
