using System.Diagnostics.Contracts;
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
            if(!DreamProcNativeHelpers.TryParseColor(rgbValue, out var rgb))
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

        [DreamProc("Flip")]
        [DreamProcParameter("dir", Type = DreamValueTypeFlag.Float, DefaultValue = null)]
        public static DreamValue NativeProc_Flip(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            var srcDreamIcon = ((DreamObjectIcon)src!).Icon;

            AtomDirection dir = (AtomDirection)bundle.GetArgument(0, "dir").MustGetValueAsInteger();
            if(!dir.IsValid() || dir.Cardinals() != dir)
                return DreamValue.Null;
            if((dir & (AtomDirection.Northwest)) == AtomDirection.Northwest || (dir & (AtomDirection.Southeast)) == AtomDirection.Southeast) // ???
                return DreamValue.Null;

            bool flipVertical = (dir & (AtomDirection.North | AtomDirection.South)) != 0;
            bool flipHorizontal = (dir & (AtomDirection.East | AtomDirection.West)) != 0;

            if(flipVertical && flipHorizontal && srcDreamIcon.Width != srcDreamIcon.Height) {
                return DreamValue.Null;
            }

            srcDreamIcon.ApplyOperation(new DreamIconOperationFlip(flipVertical, flipHorizontal));
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

        [DreamProc("MapColors")]
        public static DreamValue NativeProc_MapColors(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            var srcDreamIcon = ((DreamObjectIcon)src!).Icon;

            return bundle.GetArgument(0, "") switch {
                var rValue when rValue.TryGetValueAsString(out var _) => HandleMapColors_Rgba(bundle, srcDreamIcon),
                var rRValue when rRValue.TryGetValueAsFloatCoerceNull(out var _) => HandleMapColors_Component(bundle, srcDreamIcon),
                _ => throw new ArgumentException("Could not determine MapColors form from first argument"),
            };
        }

        private static DreamValue HandleMapColors_Rgba(NativeProc.Bundle bundle, DreamIcon icon) {
            bool calculateTransparency;
            Matrix4x4 colorMatrix;
            Vector4 row0;

            [Pure]
            static bool TryParseRGB(DreamValue value, out Color color) {
                if(!value.TryGetValueAsString(out var str)) {
                    color = default;
                    return false;
                }

                if(!ColorHelpers.TryParseColor(str, out color)) {
                    color = default;
                    return false;
                }

                if(!ColorHelpers.IncludesTransparency(str))
                    color.A = 1; // yeah idk why it does this either

                return true;
            }

            switch(bundle.Arguments.TrimEnd(DreamValue.Null).Length) {
                case 3 or 4: { // RGB form
                    if(!DreamProcNativeHelpers.TryParseColor(bundle.GetArgument(0, ""), out var colorR))
                        goto default;

                    if(!DreamProcNativeHelpers.TryParseColor(bundle.GetArgument(1, ""), out var colorG))
                        goto default;

                    if(!DreamProcNativeHelpers.TryParseColor(bundle.GetArgument(2, ""), out var colorB))
                        goto default;

                    if(!DreamProcNativeHelpers.TryParseColor(bundle.GetArgument(3, ""), out var color0))
                        color0 = default;

                    colorMatrix = new() {
                        X = colorR.RGBA,
                        Y = colorG.RGBA,
                        Z = colorB.RGBA,
                        W = new Vector4(0, 0, 0, 1),
                    };
                    row0 = color0.RGBA;
                    calculateTransparency = false;

                    break;
                }

                case 5: { // RGBA form
                    const int expectedColors = 5;
                    var colors = new Color[expectedColors];

                    for(int i = 0; i < expectedColors; i++) {
                        if(!TryParseRGB(bundle.GetArgument(i, ""), out var c))
                            goto default;

                        colors[i] = c;
                    }

                    colorMatrix = new() {
                        X = colors[0].RGBA,
                        Y = colors[1].RGBA,
                        Z = colors[2].RGBA,
                        W = colors[3].RGBA,
                    };
                    row0 = colors[4].RGBA;
                    calculateTransparency = true;

                    break;
                }

                default: {
                    throw new ArgumentException("Malformed arguments for RGB or RGBA MapColors");
                }
            }

            icon.ApplyOperation(new DreamIconOperationMapColors(colorMatrix, row0, calculateTransparency));
            return DreamValue.Null;
        }

        private static DreamValue HandleMapColors_Component(NativeProc.Bundle bundle, DreamIcon icon) {
            Matrix4x4 colorMatrix; // the RGBA set
            Vector4 row0;

            [Pure]
            static bool TryBuildColorMatrix(int expectedRows, int rowLength, NativeProc.Bundle bundle, out Matrix4x4 matrix) {
                Matrix4x4 newMatrix = new();

                for(int row = 0; row < expectedRows; row++) {
                    Vector4 values = new(0);

                    for(int i = 0; i < rowLength; i++) {
                        if(!bundle.GetArgument(rowLength * row + i, "").TryGetValueAsFloatCoerceNull(out var value)) {
                            matrix = default;
                            return false;
                        }

                        values[i] = value;
                    }

                    newMatrix[row] = values;
                }

                matrix = newMatrix;
                return true;
            }

            switch(bundle.Arguments.TrimEnd(DreamValue.Null).Length) {
                case 9 or 12: { // Alpha omitted
                    const int expectedRows = 3;
                    const int rowLength = 3;

                    if(!TryBuildColorMatrix(expectedRows, rowLength, bundle, out colorMatrix))
                        goto default;

                    colorMatrix[3] = new(0, 0, 0, 1); // alpha components

                    row0 = new(0);
                    for(int i = 0; i < rowLength; i++) {
                        row0[i] = bundle.GetArgument(rowLength * (expectedRows) + i, "").UnsafeGetValueAsFloat();
                    }

                    break;
                }

                case 16 or 20: { // Alpha included
                    const int expectedRows = 4;
                    const int rowLength = 4;

                    if(!TryBuildColorMatrix(expectedRows, rowLength, bundle, out colorMatrix))
                        goto default;

                    row0 = new(0);
                    for(int i = 0; i < rowLength; i++) {
                        row0[i] = bundle.GetArgument(rowLength * (expectedRows) + i, "").UnsafeGetValueAsFloat();
                    }

                    break;
                }

                default: throw new ArgumentException("Malformed arguments for component MapColors");
            }

            icon.ApplyOperation(new DreamIconOperationMapColors(colorMatrix, row0, true));
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

        [DreamProc("SetIntensity")]
        [DreamProcParameter("r", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("g", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("b", Type = DreamValueTypeFlag.Float)]
        public static DreamValue NativeProc_SetIntensity(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            var srcDreamIcon = ((DreamObjectIcon)src!).Icon;

            var rValue = bundle.GetArgument(0, "r");
            var gValue = bundle.GetArgument(1, "g");
            var bValue = bundle.GetArgument(2, "b");

            var r = rValue.UnsafeGetValueAsFloat();
            var g = !gValue.IsNull ? gValue.UnsafeGetValueAsFloat() : r;
            var b = !bValue.IsNull ? bValue.UnsafeGetValueAsFloat() : r;

            if(r < 0 || g < 0 || b < 0)
                return DreamValue.Null;

            srcDreamIcon.ApplyOperation(new DreamIconOperationSetIntensity(r, g, b));
            return DreamValue.Null;
        }

        [DreamProc("Shift")]
        [DreamProcParameter("dir", Type = DreamValueTypeFlag.Float, DefaultValue = null)]
        [DreamProcParameter("offset", Type = DreamValueTypeFlag.Float, DefaultValue = null)]
        [DreamProcParameter("wrap", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_Shift(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            var srcDreamIcon = ((DreamObjectIcon)src!).Icon;

            AtomDirection dir = (AtomDirection)bundle.GetArgument(0, "dir").UnsafeGetValueAsFloat();
            if(!dir.IsValid() || dir.Cardinals() != dir)
                return DreamValue.Null;

            int offset = (int)bundle.GetArgument(1, "offset").UnsafeGetValueAsFloat();
            if(offset == 0)
                return DreamValue.Null;

            // This is genuinely how BYOND checks this parameter
            bool wrap = (int)bundle.GetArgument(2, "wrap").UnsafeGetValueAsFloat() != 0;

            Vector2i shiftVector = new();
            if((dir & AtomDirection.North) != 0)
                shiftVector.Y += offset;
            if((dir & AtomDirection.South) != 0)
                shiftVector.Y -= offset;
            if((dir & AtomDirection.East) != 0)
                shiftVector.X += offset;
            if((dir & AtomDirection.West) != 0)
                shiftVector.X -= offset;

            srcDreamIcon.ApplyOperation(new DreamIconOperationShift(shiftVector, wrap));
            return DreamValue.Null;
        }

        [DreamProc("SwapColor")]
        public static DreamValue NativeProc_SwapColor(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            var srcDreamIcon = ((DreamObjectIcon)src!).Icon;

            var oldRgbValue = bundle.GetArgument(0, "");
            if(!DreamProcNativeHelpers.TryParseColor(oldRgbValue, out var oldColor))
                throw new ArgumentException($"invalid search color {oldRgbValue}");

            var newRgbValue = bundle.GetArgument(1, "");
            if(!DreamProcNativeHelpers.TryParseColor(newRgbValue, out var newColor))
                throw new ArgumentException($"invalid replace color {newRgbValue}");

            // We need to parse the color string to make sure the alpha component isn't there
            oldRgbValue.TryGetValueAsString(out var oldRgbString);
            bool considerAlpha = string.IsNullOrWhiteSpace(oldRgbString) || ColorHelpers.IncludesTransparency(oldRgbString);

            srcDreamIcon.ApplyOperation(new DreamIconOperationSwapColor(oldColor, newColor, considerAlpha));
            return DreamValue.Null;
        }

        [DreamProc("Turn")]
        [DreamProcParameter("angle", Type = DreamValueTypeFlag.Float)]
        public static DreamValue NativeProc_Turn(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            DreamValue angleArg = bundle.GetArgument(0, "angle");
            ((DreamObjectIcon)src!).Turn(angleArg);
            return DreamValue.Null;
        }
    }
}
