using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using BlendType = OpenDreamRuntime.Objects.DreamIconOperationBlend.BlendType;

namespace OpenDreamRuntime.Procs.Native {
    static class DreamProcNativeIcon {
        [DreamProc("Width")]
        public static DreamValue NativeProc_Width(NativeProc.State state) {
            DreamIcon dreamIconObject = DreamMetaObjectIcon.ObjectToDreamIcon[state.Src];

            return new DreamValue(dreamIconObject.Width);
        }

        [DreamProc("Height")]
        public static DreamValue NativeProc_Height(NativeProc.State state) {
            DreamIcon dreamIconObject = DreamMetaObjectIcon.ObjectToDreamIcon[state.Src];

            return new DreamValue(dreamIconObject.Height);
        }

        [DreamProc("Insert")]
        [DreamProcParameter("new_icon", Type = DreamValue.DreamValueType.DreamObject)]
        [DreamProcParameter("icon_state", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("dir", Type = DreamValue.DreamValueType.Float)]
        [DreamProcParameter("frame", Type = DreamValue.DreamValueType.Float)]
        [DreamProcParameter("moving", Type = DreamValue.DreamValueType.Float)]
        [DreamProcParameter("delay", Type = DreamValue.DreamValueType.Float)]
        public static DreamValue NativeProc_Insert(NativeProc.State state) {
            //TODO Figure out what happens when you pass the wrong types as args

            DreamValue newIcon = state.Arguments.GetArgument(0, "new_icon");
            DreamValue iconState = state.Arguments.GetArgument(1, "icon_state");
            DreamValue dir = state.Arguments.GetArgument(2, "dir");
            DreamValue frame = state.Arguments.GetArgument(3, "frame");
            DreamValue moving = state.Arguments.GetArgument(4, "moving");
            DreamValue delay = state.Arguments.GetArgument(5, "delay");

            // TODO: moving & delay

            var resourceManager = IoCManager.Resolve<DreamResourceManager>();
            if (!resourceManager.TryLoadIcon(newIcon, out var iconRsc))
                throw new Exception($"Cannot insert {newIcon}");

            DreamIcon iconObj = DreamMetaObjectIcon.ObjectToDreamIcon[state.Src];
            iconObj.InsertStates(iconRsc, iconState, dir, frame); // TODO: moving & delay
            return DreamValue.Null;
        }

        public static void Blend(DreamIcon icon, DreamValue blend, BlendType function, int x, int y) {
            if (blend.TryGetValueAsString(out var colorStr)) {
                if (!ColorHelpers.TryParseColor(colorStr, out var color))
                    throw new Exception($"Invalid color {colorStr}");

                icon.ApplyOperation(new DreamIconOperationBlendColor(function, x, y, color));
            } else {
                icon.ApplyOperation(new DreamIconOperationBlendImage(function, x, y, blend));
            }
        }

        [DreamProc("Blend")]
        [DreamProcParameter("icon", Type = DreamValue.DreamValueType.DreamObject)]
        [DreamProcParameter("function", Type = DreamValue.DreamValueType.Float, DefaultValue = (int)BlendType.Add)] // ICON_ADD
        [DreamProcParameter("x", Type = DreamValue.DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("y", Type = DreamValue.DreamValueType.Float, DefaultValue = 1)]
        public static DreamValue NativeProc_Blend(NativeProc.State state) {
            //TODO Figure out what happens when you pass the wrong types as args

            DreamValue icon = state.Arguments.GetArgument(0, "icon");
            DreamValue function = state.Arguments.GetArgument(1, "function");

            state.Arguments.GetArgument(2, "x").TryGetValueAsInteger(out var x);
            state.Arguments.GetArgument(3, "y").TryGetValueAsInteger(out var y);

            if (!function.TryGetValueAsInteger(out var functionValue))
                throw new Exception($"Invalid 'function' argument {function}");

            Blend(DreamMetaObjectIcon.ObjectToDreamIcon[state.Src], icon, (BlendType)functionValue, x, y);
            return DreamValue.Null;
        }

        [DreamProc("Scale")]
        [DreamProcParameter("width", Type = DreamValue.DreamValueType.Float)]
        [DreamProcParameter("height", Type = DreamValue.DreamValueType.Float)]
        public static DreamValue NativeProc_Scale(NativeProc.State state) {
            //TODO Figure out what happens when you pass the wrong types as args

            state.Arguments.GetArgument(0, "width").TryGetValueAsInteger(out var width);
            state.Arguments.GetArgument(1, "height").TryGetValueAsInteger(out var height);

            DreamIcon iconObj = DreamMetaObjectIcon.ObjectToDreamIcon[state.Src];
            iconObj.Width = width;
            iconObj.Height = height;
            return DreamValue.Null;
        }
    }
}
