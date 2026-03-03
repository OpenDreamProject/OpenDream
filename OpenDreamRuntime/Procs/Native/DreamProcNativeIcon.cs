using System.Diagnostics;
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

        [DreamProc("GetPixel")]
        [DreamProcParameter("x", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("y", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("icon_state", Type = DreamValueTypeFlag.String)]
        [DreamProcParameter("dir", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
        [DreamProcParameter("frame", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
        [DreamProcParameter("moving", Type = DreamValueTypeFlag.Float, DefaultValue = -1)]
        public static DreamValue NativeProc_GetPixel(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            bundle.GetArgument(0, "x").TryGetValueAsInteger(out var xPos);
            bundle.GetArgument(1, "y").TryGetValueAsInteger(out var yPos);
            bundle.GetArgument(2, "icon_state").TryGetValueAsString(out var iconState);
            bundle.GetArgument(3, "dir").TryGetValueAsInteger(out var dir);
            bundle.GetArgument(4, "frame").TryGetValueAsInteger(out var frame);
            //TODO: Implement moving var
            bundle.GetArgument(5, "moving").TryGetValueAsInteger(out var moving);

            DreamIcon iconObj = ((DreamObjectIcon)src!).Icon;
            if(!iconObj.States.TryGetValue(iconState ?? string.Empty, out var state))
                //Bad icon_state returns null
                return DreamValue.Null;

            //Values less than 1 are out of bounds.
            if (xPos < 1 || yPos < 1) return DreamValue.Null;

            if (frame < 1) {
                frame = 0; //BYONDISM: Frames less than 1 count as 0,
            } else if (frame > state.Frames) {
                return DreamValue.Null;
            } else {
                frame -= 1; // Convert from 1-index to 0-index
            }

            AtomDirection atomDir = dir switch {
                0 or 2 => AtomDirection.South,
                1 => AtomDirection.North,
                4 => AtomDirection.East,
                5 => AtomDirection.Northeast,
                6 => AtomDirection.Southeast,
                8 => AtomDirection.West,
                9 => AtomDirection.Northwest,
                10 => AtomDirection.Southwest,
                _ => AtomDirection.None
            };
            //BYONDISM: Bad dir values just crash instantly :)
            if (atomDir == AtomDirection.None) return DreamValue.Null;

            if (!state.Directions.TryGetValue(atomDir, out var frameList))
                //Empty dir's return null
                return DreamValue.Null;

            var finalFrame = frameList[frame].Image;
            if (finalFrame is null) return DreamValue.Null;

            //Out-of-bounds xy values return null.
            if (xPos > finalFrame.Width || yPos > finalFrame.Height) return DreamValue.Null;
            //SixLabors.Image<Rgba32> is 0-indexed from the top-left, BYOND is 1-indexed from the bottom left.
            xPos -= 1;
            yPos = finalFrame.Height - yPos;

            var pix = finalFrame[xPos, yPos];
            //If A is fully transparent return null
            //if A is partially transparent return "#RRGGBBAA"
            //if A is not transparent return "#RRGGBB"
            return pix.A switch {
                0 => DreamValue.Null,
                255 => new DreamValue($"#{pix.R:X2}{pix.G:X2}{pix.B:X2}"),
                _ => new DreamValue($"#{pix.R:X2}{pix.G:X2}{pix.B:X2}{pix.A:X2}")
            };
        }
    }
}
