using System.Linq;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using BlendType = OpenDreamRuntime.Objects.DreamIconOperationBlend.BlendType;
using DreamValueTypeFlag = OpenDreamRuntime.DreamValue.DreamValueTypeFlag;

namespace OpenDreamRuntime.Procs.Native {
    internal static class DreamProcNativeIcon {
        [DreamProc("Width")]
        public static DreamValue NativeProc_Width(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            return new DreamValue(((DreamObjectIcon)src!).Icon.Width);
        }

        [DreamProc("Height")]
        public static DreamValue NativeProc_Height(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            return new DreamValue(((DreamObjectIcon)src!).Icon.Height);
        }

        [DreamProc("Insert")]
        [DreamProcParameter("new_icon", Type = DreamValueTypeFlag.DreamObject)]
        [DreamProcParameter("icon_state", Type = DreamValueTypeFlag.String)]
        [DreamProcParameter("dir", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("frame", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("moving", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("delay", Type = DreamValueTypeFlag.Float)]
        public static DreamValue NativeProc_Insert(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            //TODO Figure out what happens when you pass the wrong types as args

            DreamValue newIcon = bundle.GetArgument(0, "new_icon");
            DreamValue iconState = bundle.GetArgument(1, "icon_state");
            DreamValue dir = bundle.GetArgument(2, "dir");
            DreamValue frame = bundle.GetArgument(3, "frame");
            DreamValue moving = bundle.GetArgument(4, "moving");
            DreamValue delay = bundle.GetArgument(5, "delay");

            // TODO: moving & delay

            var resourceManager = IoCManager.Resolve<DreamResourceManager>();
            if (!resourceManager.TryLoadIcon(newIcon, out var iconRsc))
                throw new Exception($"Cannot insert {newIcon}");

            ((DreamObjectIcon)src!).Icon.InsertStates(iconRsc, iconState, dir, frame); // TODO: moving & delay
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
        [DreamProcParameter("icon", Type = DreamValueTypeFlag.DreamObject)]
        [DreamProcParameter("function", Type = DreamValueTypeFlag.Float, DefaultValue = (int)BlendType.Add)] // ICON_ADD
        [DreamProcParameter("x", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
        [DreamProcParameter("y", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
        public static DreamValue NativeProc_Blend(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            //TODO Figure out what happens when you pass the wrong types as args

            DreamValue icon = bundle.GetArgument(0, "icon");
            DreamValue function = bundle.GetArgument(1, "function");

            bundle.GetArgument(2, "x").TryGetValueAsInteger(out var x);
            bundle.GetArgument(3, "y").TryGetValueAsInteger(out var y);

            if (!function.TryGetValueAsInteger(out var functionValue))
                throw new Exception($"Invalid 'function' argument {function}");

            Blend(((DreamObjectIcon)src!).Icon, icon, (BlendType)functionValue, x, y);
            return DreamValue.Null;
        }

        [DreamProc("DrawBox")]
        [DreamProcParameter("rgb", Type = DreamValueTypeFlag.String)]
        [DreamProcParameter("x1", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("y1", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("x2", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("y2", Type = DreamValueTypeFlag.Float)]
        public static DreamValue NativeProc_DrawBox(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            var srcDreamIcon = ((DreamObjectIcon)src!).Icon;

            var rgbValue = bundle.GetArgument(0, "rgb");
            if(!rgbValue.TryGetValueAsString(out var rgbStr) || !ColorHelpers.TryParseColor(rgbStr, out var rgb))
                if(string.IsNullOrEmpty(rgbStr))
                    rgb = Color.Transparent;
                else
                    throw new ArgumentException($"invalid rgb value {rgbValue}");

            int x1 = (int)bundle.GetArgument(1, "x1").UnsafeGetValueAsFloat();
            int y1 = (int)bundle.GetArgument(2, "y1").UnsafeGetValueAsFloat();

            if(!bundle.GetArgument(3, "x2").TryGetValueAsInteger(out var x2))
                x2 = x1;
            if(!bundle.GetArgument(4, "y2").TryGetValueAsInteger(out var y2))
                y2 = y1;

            srcDreamIcon.ApplyOperation(new DreamIconOperationDrawBox(rgb, new(x1, y1), new(x2, y2)));

            return DreamValue.Null;
        }

        [DreamProc("GetPixel")]
        [DreamProcParameter("x", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("y", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("icon_state", Type = DreamValueTypeFlag.DreamObject | DreamValueTypeFlag.String, DefaultValue = null)]
        [DreamProcParameter("dir", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
        [DreamProcParameter("frame", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
        [DreamProcParameter("moving", Type = DreamValueTypeFlag.Float, DefaultValue = -1)]
        public static DreamValue NativeProc_GetPixel(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            var srcDreamIcon = (DreamObjectIcon)src!;

            //arg validation
            int x = (int)bundle.GetArgument(0, "x").UnsafeGetValueAsFloat();
            int y = (int)bundle.GetArgument(1, "y").UnsafeGetValueAsFloat();

            //outside valid bounds returns null
            if(x < 1 || x > srcDreamIcon.Icon.Width || y < 1 || y > srcDreamIcon.Icon.Height)
                return DreamValue.Null;

            if(!bundle.GetArgument(2, "icon_state").TryGetValueAsString(out string? iconState))
                iconState = "";

            var generatedDMI = srcDreamIcon.Icon.GenerateDMI();
            if(!generatedDMI.DMI.States.TryGetValue(iconState, out var iconStateObject)){
                if(iconState == string.Empty)
                    iconStateObject = generatedDMI.DMI.States.First().Value;
                else //invalid icon state causes BYOND to create error.log but it's empty
                    return DreamValue.Null; //throw new ArgumentException($"Invalid icon_state {iconState} passed to /icon.GetPixel()");
            }

            AtomDirection dir = (AtomDirection)bundle.GetArgument(3, "dir").UnsafeGetValueAsFloat();
            if(dir == AtomDirection.None)
                dir = iconStateObject.Directions.Keys.First();
            else if(!iconStateObject.Directions.ContainsKey(dir))
                return DreamValue.Null;

            int frame = Math.Max(1, bundle.GetArgument(4, "frame").MustGetValueAsInteger())-1;

            if (iconStateObject.FrameCount < frame)
                return DreamValue.Null;

            DreamValue moving = bundle.GetArgument(5, "moving"); // TODO: implement movement states

            var stateDirFrame = iconStateObject.Directions[dir][frame];

            var pixel = generatedDMI.Texture[stateDirFrame.X+x-1,stateDirFrame.Y+(srcDreamIcon.Icon.Height-y)];
            return pixel.A switch {
                0 => DreamValue.Null,
                255 => new DreamValue($"#{pixel.R:x2}{pixel.G:x2}{pixel.B:x2}"),
                _ => new DreamValue($"#{pixel.R:x2}{pixel.G:x2}{pixel.B:x2}{pixel.A:x2}")
            };
        }

        [DreamProc("Scale")]
        [DreamProcParameter("width", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("height", Type = DreamValueTypeFlag.Float)]
        public static DreamValue NativeProc_Scale(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            //TODO Figure out what happens when you pass the wrong types as args

            bundle.GetArgument(0, "width").TryGetValueAsInteger(out var width);
            bundle.GetArgument(1, "height").TryGetValueAsInteger(out var height);

            DreamIcon iconObj = ((DreamObjectIcon)src!).Icon;
            iconObj.Width = width;
            iconObj.Height = height;
            return DreamValue.Null;
        }

        [DreamProc("Turn")]
        [DreamProcParameter("angle", Type = DreamValueTypeFlag.Float)]
        public static DreamValue NativeProc_Turn(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            DreamValue angleArg = bundle.GetArgument(0, "angle");
            if (!angleArg.TryGetValueAsFloat(out float angle)) {
                return new DreamValue(src!); // Defaults to input on invalid angle
            }

            _NativeProc_TurnInternal((DreamObjectIcon)src!, angle);
            return DreamValue.Null;
        }

        /// <summary> Turns a given icon a given amount of degrees clockwise. </summary>
        public static void _NativeProc_TurnInternal(DreamObjectIcon src, float angle) {
            src.Turn(angle);
        }
    }
}
