using System.Buffers;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using Robust.Shared.Utility;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using DMCompiler.DM;
using OpenDreamRuntime.Map;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Rendering;
using DreamValueType = OpenDreamRuntime.DreamValue.DreamValueType;
using DreamValueTypeFlag = OpenDreamRuntime.DreamValue.DreamValueTypeFlag;
using Robust.Server;
using Robust.Shared.Asynchronous;
using OpenDreamShared.Rendering;
using System.ComponentModel;

namespace OpenDreamRuntime.Procs.Native;

/// <remarks>
/// Note that this proc container also includes global procs which are used to create some DM objects,
/// like filter(), matrix(), etc.
/// </remarks>
internal static class DreamProcNativeRoot {
    [DreamProc("alert")]
    [DreamProcParameter("Usr", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Message", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Title", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Button1", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Button2", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Button3", Type = DreamValueTypeFlag.String)]
    public static async Task<DreamValue> NativeProc_alert(AsyncNativeProc.State state) {
        string message, title, button1, button2, button3;

        DreamValue usrArgument = state.GetArgument(0, "Usr");
        usrArgument.TryGetValueAsDreamObject(out var usr);

        if (usr is DreamObjectMob or DreamObjectClient) {
            message = state.GetArgument(1, "Message").Stringify();
            title = state.GetArgument(2, "Title").Stringify();
            button1 = state.GetArgument(3, "Button1").Stringify();
            button2 = state.GetArgument(4, "Button2").Stringify();
            button3 = state.GetArgument(5, "Button3").Stringify();
        } else { // Implicitly use usr, shift args over 1
            usr = state.Usr;
            message = usrArgument.Stringify();
            title = state.GetArgument(1, "Message").Stringify();
            button1 = state.GetArgument(2, "Title").Stringify();
            button2 = state.GetArgument(3, "Button1").Stringify();
            button3 = state.GetArgument(4, "Button2").Stringify();
        }

        DreamConnection? connection = null;
        if (usr is DreamObjectMob usrMob)
            connection = usrMob.Connection;
        else if (usr is DreamObjectClient usrClient)
            connection = usrClient.Connection;

        if (connection == null)
            return new("OK"); // Returns "OK" if Usr is invalid

        if (String.IsNullOrEmpty(button1)) button1 = "Ok";

        return await connection.Alert(title, message, button1, button2, button3);
    }

    /* vars:

    animate smoothly:

    alpha
    color
    glide_size
    infra_luminosity
    layer
    maptext_width, maptext_height, maptext_x, maptext_y
    luminosity
    pixel_x, pixel_y, pixel_w, pixel_z
    transform

    do not animate smoothly:

    dir
    icon
    icon_state
    invisibility
    maptext
    suffix

    */
    [DreamProc("animate")]
    [DreamProcParameter("Object", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("time", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("loop", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("easing", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("flags", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("delay", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("pixel_x", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("pixel_y", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("pixel_z", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("pixel_w", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("maptext", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("maptext_width", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("maptext_height", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("maptext_x", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("maptext_y", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("dir", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("alpha", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("transform", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("color", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("luminosity", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("infra_luminosity", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("layer", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("glide_size", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("icon", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("icon_state", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("invisibility", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("suffix", Type = DreamValueTypeFlag.String)]
    //filter args -dups commented out
    [DreamProcParameter("size", Type = DreamValueTypeFlag.Float)]
    //[DreamProcParameter("color", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("x", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("y", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("offset", Type = DreamValueTypeFlag.Float)]
    //[DreamProcParameter("flags", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("border", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("render_source", Type = DreamValueTypeFlag.String)]
    //[DreamProcParameter("icon", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("space", Type = DreamValueTypeFlag.Float)]
    //[DreamProcParameter("transform", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("blend_mode", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("density", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("threshold", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("factor", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("repeat", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("radius", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("falloff", Type = DreamValueTypeFlag.Float)]
    //[DreamProcParameter("alpha", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_animate(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        bool chainAnim = false;

        if (!bundle.GetArgument(0, "Object").TryGetValueAsDreamObject<DreamObject>(out var obj)){
            if(bundle.LastAnimatedObject is null || bundle.LastAnimatedObject.Value.IsNull)
                throw new Exception($"animate() called without an object and no previous object to animate");
            else if(!bundle.LastAnimatedObject.Value.TryGetValueAsDreamObject<DreamObject>(out obj))
                return DreamValue.Null;
            chainAnim = true;
        }

        bundle.LastAnimatedObject = new DreamValue(obj);
        if(obj.IsSubtypeOf(bundle.ObjectTree.Filter)) {//TODO animate filters
            return DreamValue.Null;
        }

        // TODO: Is this the correct behavior for invalid time?
        if (!bundle.GetArgument(1, "time").TryGetValueAsFloat(out float time))
            return DreamValue.Null;

        bundle.GetArgument(2, "loop").TryGetValueAsInteger(out int loop);
        bundle.GetArgument(3, "easing").TryGetValueAsInteger(out int easing);
        if(!Enum.IsDefined(typeof(AnimationEasing), easing & ~((int)AnimationEasing.EaseIn | (int)AnimationEasing.EaseOut)))
            throw new ArgumentOutOfRangeException("easing", easing, $"Invalid easing value in animate(): {easing}");
        bundle.GetArgument(4, "flags").TryGetValueAsInteger(out int flagsInt);
        var flags = (AnimationFlags)flagsInt;
        if((flags & (AnimationFlags.AnimationParallel | AnimationFlags.AnimationContinue)) != 0)
            chainAnim = true;
        if((flags & AnimationFlags.AnimationEndNow) != 0)
            chainAnim = false;
        bundle.GetArgument(5, "delay").TryGetValueAsInteger(out int delay);

        var pixelX = bundle.GetArgument(6, "pixel_x");
        var pixelY = bundle.GetArgument(7, "pixel_y");
        var pixelZ = bundle.GetArgument(8, "pixel_z");
        var pixelW = bundle.GetArgument(9, "pixel_w");
        var maptext = bundle.GetArgument(10, "maptext");
        var maptextWidth = bundle.GetArgument(11, "maptext_width");
        var maptextHeight = bundle.GetArgument(12, "maptext_height");
        var maptextX = bundle.GetArgument(13, "maptext_x");
        var maptextY = bundle.GetArgument(14, "maptext_y");
        var dir = bundle.GetArgument(15, "dir");
        var alpha = bundle.GetArgument(16, "alpha");
        var transform = bundle.GetArgument(17, "transform");
        var color = bundle.GetArgument(18, "color");
        var luminosity = bundle.GetArgument(19, "luminosity");
        var infraLuminosity = bundle.GetArgument(20, "infra_luminosity");
        var layer = bundle.GetArgument(21, "layer");
        var glideSize = bundle.GetArgument(22, "glide_size");
        var icon = bundle.GetArgument(23, "icon");
        var iconState = bundle.GetArgument(24, "icon_state");
        var invisibility = bundle.GetArgument(25, "invisibility");
        var suffix = bundle.GetArgument(26, "suffix");

        if((flags & AnimationFlags.AnimationRelative) != 0){
            if(!bundle.AtomManager.TryGetAppearance(obj, out var appearance))
                return DreamValue.Null; //can't do anything animating an object with no appearance
            // This works for maptext_x/y/width/height, pixel_x/y/w/z, luminosity, layer, alpha, transform, and color. For transform and color, the current value is multiplied by the new one. Vars not in this list are simply changed as if this flag is not present.
            if(!pixelX.IsNull)
                pixelX = new(pixelX.UnsafeGetValueAsFloat() + appearance.PixelOffset.X);
            if(!pixelY.IsNull)
                pixelY = new(pixelY.UnsafeGetValueAsFloat() + appearance.PixelOffset.Y);
            /* TODO these are not yet implemented
            if(!pixelZ.IsNull)
                pixelZ = new(pixelZ.UnsafeGetValueAsFloat() + obj.GetVariable("pixel_z").UnsafeGetValueAsFloat()); //TODO change to appearance when pixel_z is implemented
            */
            if(!maptextWidth.IsNull)
                maptextWidth = new(maptextWidth.UnsafeGetValueAsFloat() + appearance.MaptextSize.X);
            if(!maptextHeight.IsNull)
                maptextHeight = new(maptextHeight.UnsafeGetValueAsFloat() + appearance.MaptextSize.Y);
            if(!maptextX.IsNull)
                maptextX = new(maptextX.UnsafeGetValueAsFloat() + appearance.MaptextOffset.X);
            if(!maptextY.IsNull)
                maptextY = new(maptextY.UnsafeGetValueAsFloat() + appearance.MaptextOffset.Y);
            /*
            if(!luminosity.IsNull)
                luminosity = new(luminosity.UnsafeGetValueAsFloat() + obj.GetVariable("luminosity").UnsafeGetValueAsFloat()); //TODO change to appearance when luminosity is implemented
            */
            if(!layer.IsNull)
                layer = new(layer.UnsafeGetValueAsFloat() + appearance.Layer);
            if(!alpha.IsNull)
                alpha = new(alpha.UnsafeGetValueAsFloat() + appearance.Alpha);
            if(!transform.IsNull) {
                if(transform.TryGetValueAsDreamObject<DreamObjectMatrix>(out var multTransform)){
                    DreamObjectMatrix objTransformClone = DreamObjectMatrix.MakeMatrix(bundle.ObjectTree, appearance.Transform);
                    DreamObjectMatrix.MultiplyMatrix(objTransformClone, multTransform);
                    transform = new(objTransformClone);
                }
            }

            if(!color.IsNull) {
                ColorMatrix cMatrix;
                if(color.TryGetValueAsString(out var colorStr) && Color.TryParse(colorStr, out var colorObj)){
                    cMatrix = new ColorMatrix(colorObj);
                } else if (!color.TryGetValueAsDreamList(out var colorList) || !DreamProcNativeHelpers.TryParseColorMatrix(colorList, out cMatrix)){
                    cMatrix = ColorMatrix.Identity; //fallback to identity if invalid
                }

                ColorMatrix objCMatrix;
                DreamValue objColor = obj.GetVariable("color");
                if(objColor.TryGetValueAsString(out var objColorStr) && Color.TryParse(objColorStr, out var objColorObj)){
                    objCMatrix = new ColorMatrix(objColorObj);
                } else if (!objColor.TryGetValueAsDreamList(out var objColorList) || !DreamProcNativeHelpers.TryParseColorMatrix(objColorList, out objCMatrix)){
                    objCMatrix = ColorMatrix.Identity; //fallback to identity if invalid
                }

                ColorMatrix.Multiply(ref objCMatrix, ref cMatrix, out var resultMatrix);
                color = new DreamValue(new DreamList(bundle.ObjectTree.List.ObjectDefinition, resultMatrix.GetValues().Select(x => new DreamValue(x)).ToList(), null));
            }
        }

        var resourceManager = bundle.ResourceManager;
        bundle.AtomManager.AnimateAppearance(obj, TimeSpan.FromMilliseconds(time * 100), (AnimationEasing)easing, loop, flags, delay, chainAnim,
        appearance => {
            if (!pixelX.IsNull) {
                obj.SetVariableValue("pixel_x", pixelX);
                pixelX.TryGetValueAsInteger(out appearance.PixelOffset.X);
            }

            if (!pixelY.IsNull) {
                obj.SetVariableValue("pixel_y", pixelY);
                pixelY.TryGetValueAsInteger(out appearance.PixelOffset.Y);
            }

            /* TODO world.map_format
            if (!pixelZ.IsNull) {
                obj.SetVariableValue("pixel_z", pixelZ);
                pixelZ.TryGetValueAsInteger(out appearance.PixelOffset.Z);
            }
            */

            if (!maptextX.IsNull) {
                obj.SetVariableValue("maptext_x", maptextX);
                maptextX.TryGetValueAsInteger(out appearance.MaptextOffset.X);
            }

            if (!maptextY.IsNull) {
                obj.SetVariableValue("maptext_y", maptextY);
                maptextY.TryGetValueAsInteger(out appearance.MaptextOffset.Y);
            }

            if (!maptextWidth.IsNull) {
                obj.SetVariableValue("maptext_width", maptextWidth);
                maptextX.TryGetValueAsInteger(out appearance.MaptextSize.X);
            }

            if (!maptextHeight.IsNull) {
                obj.SetVariableValue("maptext_y", maptextHeight);
                maptextY.TryGetValueAsInteger(out appearance.MaptextSize.Y);
            }

            if(!maptext.IsNull){
                obj.SetVariableValue("maptext", maptext);
                maptext.TryGetValueAsString(out appearance.Maptext);
            }

            if (!dir.IsNull) {
                obj.SetVariableValue("dir", dir);
                if(dir.TryGetValueAsInteger(out int dirValue))
                    appearance.Direction = (AtomDirection)dirValue;
            }

            if (!alpha.IsNull) {
                obj.SetVariableValue("alpha", alpha);
                if(alpha.TryGetValueAsInteger(out var alphaInt))
                    appearance.Alpha = (byte) Math.Clamp(alphaInt,0,255);
            }

            if (!transform.IsNull) {
                obj.SetVariableValue("transform", transform);
                if(transform.TryGetValueAsDreamObject<DreamObjectMatrix>(out var transformObj))
                    appearance.Transform = DreamObjectMatrix.MatrixToTransformFloatArray(transformObj);
            }

            if (!color.IsNull) {
                obj.SetVariableValue("color", color);
                if(color.TryGetValueAsString(out var colorStr))
                    Color.TryParse(colorStr, out appearance.Color);
                else if (color.TryGetValueAsDreamList(out var colorList)) {
                    if(DreamProcNativeHelpers.TryParseColorMatrix(colorList, out var colorMatrix))
                        appearance.ColorMatrix = colorMatrix;
                }
            }

            /* TODO luminosity
            if (!luminosity.IsNull) {
                obj.SetVariableValue("luminosity", luminosity);
                luminosity.TryGetValueAsInteger(out appearance.Luminosity);
            }
            */

            /* TODO infra_luminosity
            if (!infraLuminosity.IsNull) {
                obj.SetVariableValue("infra_luminosity", infraLuminosity);
                infraLuminosity.TryGetValueAsInteger(out appearance.InfraLuminosity);
            }
            */

            if (!layer.IsNull) {
                obj.SetVariableValue("layer", layer);
                layer.TryGetValueAsFloat(out appearance.Layer);
            }

            if (!glideSize.IsNull) {
                obj.SetVariableValue("glide_size", glideSize);
                glideSize.TryGetValueAsFloat(out appearance.GlideSize);
            }

            if (!icon.IsNull) {
                obj.SetVariableValue("icon", icon);
                if(resourceManager.TryLoadIcon(icon, out var iconResource))
                    appearance.Icon = iconResource.Id;
            }

            if (!iconState.IsNull) {
                obj.SetVariableValue("icon_state", iconState);
                iconState.TryGetValueAsString(out appearance.IconState);
            }

            if (!invisibility.IsNull) {
                obj.SetVariableValue("invisibility", invisibility);
                invisibility.TryGetValueAsInteger(out var invisibilityValue);
                appearance.Invisibility = (sbyte)Math.Clamp(invisibilityValue, -127, 127);
            }

            /* TODO suffix
            if (!suffix.IsNull) {
                obj.SetVariableValue("suffix", suffix);
                suffix.TryGetValueAsString(out appearance.Suffix);
            }
            */
        });

        return DreamValue.Null;
    }

    [DreamProc("ascii2text")]
    [DreamProcParameter("N", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_ascii2text(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue ascii = bundle.GetArgument(0, "N");
        if (!ascii.TryGetValueAsInteger(out int asciiValue))
            throw new Exception($"{ascii} is not a number");

        return new DreamValue(char.ConvertFromUtf32(asciiValue));
    }

    public static DreamList Block(DreamObjectTree objectTree, IDreamMapManager mapManager,
        DreamObjectTurf corner1, DreamObjectTurf corner2) {
        return Block(objectTree, mapManager, corner1.X, corner1.Y, corner1.Z, corner2.X, corner2.Y, corner2.Z);
    }

    public static DreamList Block(DreamObjectTree objectTree, IDreamMapManager mapManager,
        int x1, int y1, int z1, int x2, int y2, int z2) {
        int startX = Math.Min(x1, x2);
        int startY = Math.Min(y1, y2);
        int startZ = Math.Min(z1, z2);
        int endX = Math.Max(x1, x2);
        int endY = Math.Max(y1, y2);
        int endZ = Math.Max(z1, z2);

        DreamList turfs = objectTree.CreateList((endX - startX + 1) * (endY - startY + 1) * (endZ - startZ + 1));

        // Collected in z-y-x order
        for (int z = startZ; z <= endZ; z++) {
            for (int y = startY; y <= endY; y++) {
                for (int x = startX; x <= endX; x++) {
                    if (mapManager.TryGetTurfAt((x, y), z, out var turf)) {
                        turfs.AddValue(new DreamValue(turf));
                    }
                }
            }
        }

        return turfs;
    }

    [DreamProc("block")]
    [DreamProcParameter("Start", Type = DreamValueTypeFlag.DreamObject | DreamValueTypeFlag.Float)]
    [DreamProcParameter("End", Type = DreamValueTypeFlag.DreamObject | DreamValueTypeFlag.Float)]
    [DreamProcParameter("StartZ", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("EndX", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("EndY", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("EndZ", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_block(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var arg1 = bundle.GetArgument(0, "Start");
        var arg2 = bundle.GetArgument(1, "End");

        if (arg1.TryGetValueAsDreamObject<DreamObjectTurf>(out var startT)) {
            if (!arg2.TryGetValueAsDreamObject<DreamObjectTurf>(out var endT))
                return new DreamValue(bundle.ObjectTree.CreateList());

            return new(Block(bundle.ObjectTree, bundle.MapManager, startT, endT));
        } else {
            // Need to check that we weren't passed something like block("cat", turf) which should return an empty list
            if (arg2.TryGetValueAsDreamObject<DreamObjectTurf>(out _))
                return new DreamValue(bundle.ObjectTree.CreateList());

            // coordinate-style
            if (!arg1.TryGetValueAsInteger(out var x1))
                x1 = 1; // First three default to 1 when passed null or invalid
            if (!arg2.TryGetValueAsInteger(out var y1))
                y1 = 1;
            if (!bundle.GetArgument(2, "StartZ").TryGetValueAsInteger(out var z1))
                z1 = 1;
            if (!bundle.GetArgument(3, "EndX").TryGetValueAsInteger(out var x2))
                x2 = x1; // Last three default to the start coords if null or invalid
            if (!bundle.GetArgument(4, "EndY").TryGetValueAsInteger(out var y2))
                y2 = y1;
            if (!bundle.GetArgument(5, "EndZ").TryGetValueAsInteger(out var z2))
                z2 = z1;

            return new(Block(bundle.ObjectTree, bundle.MapManager, x1, y1, z1, x2, y2, z2));
        }
    }

    [DreamProc("bounds_dist")]
    [DreamProcParameter("Reference", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Target", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_bounds_dist(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "Reference").TryGetValueAsDreamObject<DreamObjectAtom>(out var origin) ||
            !bundle.GetArgument(1, "Target").TryGetValueAsDreamObject<DreamObjectAtom>(out var target)) {
            return new DreamValue(float.PositiveInfinity);
        }

        var position1 = bundle.AtomManager.GetAtomPosition(origin);
        var position2 = bundle.AtomManager.GetAtomPosition(target);
        if (position1.Z != position2.Z) {
            return new DreamValue(float.PositiveInfinity);
        }

        //todo, support step for pixel movement
        if (!origin.TryGetVariable("bound_width", out var originWidth)) {
            originWidth = new(bundle.DreamManager.WorldInstance.IconSize);
        }

        if (!origin.TryGetVariable("bound_height", out var originHeight)) {
            originHeight = new(bundle.DreamManager.WorldInstance.IconSize);
        }

        if (!target.TryGetVariable("bound_width", out var targetWidth)) {
            targetWidth = new(bundle.DreamManager.WorldInstance.IconSize);
        }

        if (!origin.TryGetVariable("bound_height", out var targetHeight)) {
            targetHeight = new(bundle.DreamManager.WorldInstance.IconSize);
        }

        return new DreamValue(MathF.Max(MathF.Abs(position2.X - position1.X) * bundle.DreamManager.WorldInstance.IconSize -
                                    MathF.Abs(originWidth.MustGetValueAsFloat() + targetWidth.MustGetValueAsFloat()) / 2,
                                    MathF.Abs(position2.Y - position1.Y) * bundle.DreamManager.WorldInstance.IconSize -
                                    MathF.Abs(originHeight.MustGetValueAsFloat() + targetHeight.MustGetValueAsFloat()) / 2));
    }

    [DreamProc("ceil")]
    [DreamProcParameter("A", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_ceil(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        bundle.GetArgument(0, "A").TryGetValueAsFloat(out float floatNum);

        return new DreamValue(MathF.Ceiling(floatNum));
    }

    [DreamProc("ckey")]
    [DreamProcParameter("Key", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_ckey(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "Key").TryGetValueAsString(out var key)) {
            return DreamValue.Null;
        }

        key = DreamProcNativeHelpers.Ckey(key);
        return new DreamValue(key);
    }

    [DreamProc("ckeyEx")]
    [DreamProcParameter("Text", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_ckeyEx(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "Text").TryGetValueAsString(out var text)) {
            return DreamValue.Null;
        }

        text = Regex.Replace(text, "[\\^]|[^A-z0-9@_-]", ""); //Remove all punctuation except - and _
        return new DreamValue(text);
    }

    [DreamProc("clamp")]
    [DreamProcParameter("Value", Type = DreamValueTypeFlag.Float | DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Low", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("High", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_clamp(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue value = bundle.GetArgument(0, "Value");

        if (!bundle.GetArgument(1, "Low").TryGetValueAsFloat(out float lVal))
            ClampLowerBoundNotANumber();
        if (!bundle.GetArgument(2, "High").TryGetValueAsFloat(out float hVal))
            ClampUpperBoundNotANumber();

        // BYOND supports switching low/high args around
        if (lVal > hVal) {
            (hVal, lVal) = (lVal, hVal);
        }

        if (value.TryGetValueAsDreamList(out var list)) {
            DreamList tmp = bundle.ObjectTree.CreateList();
            foreach (DreamValue val in list.EnumerateValues()) {
                if (!val.TryGetValueAsFloat(out float floatVal))
                    continue;

                tmp.AddValue(new DreamValue(Math.Clamp(floatVal, lVal, hVal)));
            }

            return new DreamValue(tmp);
        } else if (value.TryGetValueAsFloat(out float floatVal) || value.IsNull) {
            return new DreamValue(Math.Clamp(floatVal, lVal, hVal));
        } else {
            ClampUnexpectedType();
            return DreamValue.Null;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClampLowerBoundNotANumber() {
        throw new Exception("Lower bound is not a number");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClampUpperBoundNotANumber() {
        throw new Exception("Upper bound is not a number");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClampUnexpectedType() {
        throw new Exception("Clamp expects a number or list");
    }

    [DreamProc("cmptext")]
    [DreamProcParameter("T1", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_cmptext(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "T1").TryGetValueAsString(out var t1))
            return DreamValue.False;

        for (int i = 1; i < bundle.Arguments.Length; i++) {
            var arg = bundle.Arguments[i];

            if (!arg.TryGetValueAsString(out var t2))
                return DreamValue.False;

            if (!t2.Equals(t1, StringComparison.InvariantCultureIgnoreCase))
                return DreamValue.False;
        }

        return DreamValue.True;
    }

    [DreamProc("cmptextEx")]
    [DreamProcParameter("T1", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_cmptextEx(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "T1").TryGetValueAsString(out var t1))
            return DreamValue.False;

        for (int i = 1; i < bundle.Arguments.Length; i++) {
            var arg = bundle.Arguments[i];

            if (!arg.TryGetValueAsString(out var t2))
                return DreamValue.False;

            if (!t2.Equals(t1, StringComparison.InvariantCulture))
                return DreamValue.False;
        }

        return DreamValue.True;
    }

    [DreamProc("copytext")]
    [DreamProcParameter("T", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
    [DreamProcParameter("End", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_copytext(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        bundle.GetArgument(2, "End").TryGetValueAsInteger(out var end); //1-indexed

        if (!bundle.GetArgument(0, "T").TryGetValueAsString(out string? text))
            return (end == 0) ? DreamValue.Null : new DreamValue("");
        if (!bundle.GetArgument(1, "Start").TryGetValueAsInteger(out int start)) //1-indexed
            return new DreamValue("");

        if (end <= 0) end += text.Length + 1;
        else if (end > text.Length + 1) end = text.Length + 1;

        if (start == 0) return new DreamValue("");
        else if (start < 0) start += text.Length + 1;

        return new DreamValue(text.Substring(start - 1, end - start));
    }

    [DreamProc("copytext_char")]
    [DreamProcParameter("T", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
    [DreamProcParameter("End", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_copytext_char(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        bundle.GetArgument(2, "End").TryGetValueAsInteger(out var end); //1-indexed

        if (!bundle.GetArgument(0, "T").TryGetValueAsString(out string? text))
            return (end == 0) ? DreamValue.Null : new DreamValue("");
        if (!bundle.GetArgument(1, "Start").TryGetValueAsInteger(out int start)) //1-indexed
            return new DreamValue("");

        StringInfo textElements = new StringInfo(text);

        if (end <= 0) end += textElements.LengthInTextElements + 1;
        else if (end > textElements.LengthInTextElements + 1) end = textElements.LengthInTextElements + 1;

        if (start == 0) return new DreamValue("");
        else if (start < 0) start += textElements.LengthInTextElements + 1;

        if (start > textElements.LengthInTextElements)
            return new(string.Empty);

        return new DreamValue(textElements.SubstringByTextElements(start - 1, end - start));
    }

    [DreamProc("CRASH")]
    [DreamProcParameter("msg", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_CRASH(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        bundle.GetArgument(0, "msg").TryGetValueAsString(out var message);

        // BYOND doesn't give a message if the value is anything other than a string
        throw new DMCrashRuntime(message ?? string.Empty);
    }

    [DreamProc("fcopy")]
    [DreamProcParameter("Src", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamResource)]
    [DreamProcParameter("Dst", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_fcopy(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var arg1 = bundle.GetArgument(0, "Src");

        DreamResource? srcFile = null;
        if (bundle.ResourceManager.TryLoadIcon(arg1, out var icon)) {
            srcFile = icon;
        } else if (arg1.TryGetValueAsDreamResource(out DreamResource? arg1Rsc)) {
            srcFile = arg1Rsc;
        } else if (arg1.TryGetValueAsDreamObject<DreamObjectSavefile>(out var savefile)) {
            srcFile = savefile.Resource;
        } else if (arg1.TryGetValueAsString(out var srcPath)) {
            srcFile = bundle.ResourceManager.LoadResource(srcPath);
        }

        if (srcFile?.ResourceData == null) {
            throw new Exception($"Bad src file {arg1}");
        }

        var arg2 = bundle.GetArgument(1, "Dst");
        if (!arg2.TryGetValueAsString(out var dst)) {
            throw new Exception($"Bad dst file {arg2}");
        }

        return new DreamValue(bundle.ResourceManager.CopyFile(srcFile, dst) ? 1 : 0);
    }

    [DreamProc("fcopy_rsc")]
    [DreamProcParameter("File", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamResource)]
    public static DreamValue NativeProc_fcopy_rsc(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var arg1 = bundle.GetArgument(0, "File");

        if (bundle.ResourceManager.TryLoadIcon(arg1, out var icon))
            return new(icon);

        string? filePath;
        if (arg1.TryGetValueAsDreamResource(out var arg1Rsc)) {
            filePath = arg1Rsc.ResourcePath;
        } else {
            arg1.TryGetValueAsString(out filePath);
        }

        if (filePath == null)
            return DreamValue.Null;

        return new DreamValue(bundle.ResourceManager.LoadResource(filePath));
    }

    [DreamProc("fdel")]
    [DreamProcParameter("File", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_fdel(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue file = bundle.GetArgument(0, "File");

        string? filePath;
        if (file.TryGetValueAsDreamResource(out var resource)) {
            filePath = resource.ResourcePath;
        } else if(!file.TryGetValueAsString(out filePath)) {
            throw new Exception($"{file} is not a valid file");
        }

        bool successful = filePath.EndsWith("/") ? bundle.ResourceManager.DeleteDirectory(filePath) : bundle.ResourceManager.DeleteFile(filePath);
        return new DreamValue(successful ? 1 : 0);
    }

    [DreamProc("fexists")]
    [DreamProcParameter("File", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamResource)]
    public static DreamValue NativeProc_fexists(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue file = bundle.GetArgument(0, "File");

        string? filePath;
        if (file.TryGetValueAsDreamResource(out var rsc)) {
            filePath = rsc.ResourcePath;
        } else if (!file.TryGetValueAsString(out filePath)) {
            return DreamValue.Null;
        }

        return new DreamValue(bundle.ResourceManager.DoesFileExist(filePath) ? 1 : 0);
    }

    [DreamProc("file")]
    [DreamProcParameter("Path", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamResource)]
    public static DreamValue NativeProc_file(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue path = bundle.GetArgument(0, "Path");

        if (path.TryGetValueAsString(out var rscPath)) {
            var resource = bundle.ResourceManager.LoadResource(rscPath);

            return new DreamValue(resource);
        }

        if (path.Type == DreamValueType.DreamResource) {
            return path;
        }

        throw new Exception("Invalid path argument");
    }

    [DreamProc("file2text")]
    [DreamProcParameter("File", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamResource)]
    public static DreamValue NativeProc_file2text(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue file = bundle.GetArgument(0, "File");
        DreamResource? resource;

        if (file.TryGetValueAsString(out var rscPath)) {
            resource = bundle.ResourceManager.LoadResource(rscPath);
        } else if (!file.TryGetValueAsDreamResource(out resource)) {
            return DreamValue.Null;
        }

        string? text = resource.ReadAsString();
        return (text != null) ? new DreamValue(text) : DreamValue.Null;
    }

    [DreamProc("filter")]
    [DreamProcParameter("type", Type = DreamValueTypeFlag.String)] // Must be from a valid list
    [DreamProcParameter("size", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("color", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("x", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("y", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("offset", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("flags", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("border", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("render_source", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("icon", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("space", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("transform", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("blend_mode", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("density", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("threshold", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("factor", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("repeat", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("radius", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("falloff", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("alpha", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_filter(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var propertyValues = ArrayPool<DreamValue>.Shared.Rent(bundle.Arguments.Length);

        // ReadOnlySpan shenanigans making things difficult..
        bundle.Arguments.CopyTo(propertyValues);

        static IEnumerable<(string, DreamValue)> EnumerateProperties(List<string> argumentNames, DreamValue[] values) {
            for (int i = 0; i < argumentNames.Count; i++) { // Every argument is a filter property
                var propertyName = argumentNames[i];
                var property = values[i];
                if (property.IsNull)
                    continue;

                yield return (propertyName, property);
            }
        }

        var propertyEnumerator = EnumerateProperties(bundle.Proc.ArgumentNames!, propertyValues);
        var filter = DreamObjectFilter.TryCreateFilter(bundle.ObjectTree, propertyEnumerator);

        ArrayPool<DreamValue>.Shared.Return(propertyValues, clearArray: true);
        return new(filter);
    }

    [DreamProc("findtext")]
    [DreamProcParameter("Haystack", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Needle", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
    [DreamProcParameter("End", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_findtext(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        // TODO This is for handling nulls, check if it works right for other bad types
        int failCount = 0;
        if (!bundle.GetArgument(0, "Haystack").TryGetValueAsString(out var text)) {
            failCount++;
        }

        DreamValue needleArg = bundle.GetArgument(1, "Needle");
        DreamObjectRegex? regex = null;
        if (!needleArg.TryGetValueAsString(out var needle)) {
            if(!needleArg.TryGetValueAsDreamObject(out regex)) {
                failCount++;
            }
        }

        if (failCount > 0 || string.IsNullOrEmpty(text) || (string.IsNullOrEmpty(needle) && regex == null)) {
            return new DreamValue(failCount == 2 ? 1 : 0);
        }

        int start = bundle.GetArgument(2, "Start").MustGetValueAsInteger(); //1-indexed
        int end = bundle.GetArgument(3, "End").MustGetValueAsInteger(); //1-indexed

        if (start > text.Length || start == 0) return new DreamValue(0);

        if (start < 0) {
            start = text.Length + start + 1; //1-indexed
        }

        if (end < 0) {
            end = text.Length + end + 1; //1-indexed
        }

        if (end == 0 || end > text.Length + 1) {
            end = text.Length + 1;
        }

        if (regex is not null) {
            return regex.FindHelper(text, start - 1, end - start);
        }

        int needleIndex = text.IndexOf(needle, start - 1, end - start, StringComparison.OrdinalIgnoreCase);
        return new DreamValue(needleIndex + 1); //1-indexed
    }

    [DreamProc("findtextEx")]
    [DreamProcParameter("Haystack", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Needle", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
    [DreamProcParameter("End", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_findtextEx(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        // TODO This is for handling nulls, check if it works right for other bad types
        int failCount = 0;
        if (!bundle.GetArgument(0, "Haystack").TryGetValueAsString(out var text)) {
            failCount++;
        }

        DreamValue needleArg = bundle.GetArgument(1, "Needle");
        DreamObjectRegex? regex = null;
        if (!needleArg.TryGetValueAsString(out var needle)) {
            if (!needleArg.TryGetValueAsDreamObject(out regex)) {
                failCount++;
            }
        }

        if (failCount > 0 || string.IsNullOrEmpty(text) || (string.IsNullOrEmpty(needle) && regex == null)) {
            return new DreamValue(failCount == 2 ? 1 : 0);
        }

        int start = bundle.GetArgument(2, "Start").MustGetValueAsInteger(); //1-indexed
        int end = bundle.GetArgument(3, "End").MustGetValueAsInteger(); //1-indexed

        if (start <= 0 || start > text.Length || end < 0) return new DreamValue(0);

        if (end == 0 || end > text.Length + 1) {
            end = text.Length + 1;
        }

        if (regex is not null) {
            return regex.FindHelper(text, start - 1, end - start);
        }

        int needleIndex = text.IndexOf(needle, start - 1, end - start, StringComparison.InvariantCulture);
        if (needleIndex != -1) {
            return new DreamValue(needleIndex + 1); //1-indexed
        } else {
            return new DreamValue(0);
        }
    }

    [DreamProc("findlasttext")]
    [DreamProcParameter("Haystack", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Needle", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    [DreamProcParameter("End", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
    public static DreamValue NativeProc_findlasttext(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        // TODO This is for handling nulls, check if it works right for other bad types
        int failCount = 0;
        if (!bundle.GetArgument(0, "Haystack").TryGetValueAsString(out var text))
            failCount++;
        if (!bundle.GetArgument(1, "Needle").TryGetValueAsString(out var needle))
            failCount++;

        if (failCount > 0 || string.IsNullOrEmpty(text) || string.IsNullOrEmpty(needle)) {
            return new DreamValue(failCount == 2 ? 1 : 0);
        }

        int start = bundle.GetArgument(2, "Start").MustGetValueAsInteger(); //chars from the end
        int end = bundle.GetArgument(3, "End").MustGetValueAsInteger(); //1-indexed from the beginning
        int actualstart;
        int actualcount;

        if(start > 0)
            actualstart = start-1;
        else
            actualstart = (text.Length-1) + start;
        actualstart += needle.Length-1;
        actualstart = Math.Max(Math.Min(text.Length, actualstart),0);

        if (end > 0)
            actualcount = actualstart - (end - 1) + needle.Length;
        else
            actualcount = actualstart - ((text.Length - 1) + (end));
        actualcount = Math.Max(Math.Min(actualstart+1, actualcount),0);
        int needleIndex = text.LastIndexOf(needle, actualstart, actualcount, StringComparison.OrdinalIgnoreCase);
        return new DreamValue(needleIndex + 1); //1-indexed, or 0 if not found (LastIndexOf returns -1 if not found)
    }

    [DreamProc("findlasttextEx")]
    [DreamProcParameter("Haystack", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Needle", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    [DreamProcParameter("End", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
    public static DreamValue NativeProc_findlasttextEx(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        // TODO This is for handling nulls, check if it works right for other bad types
        int failCount = 0;
        if (!bundle.GetArgument(0, "Haystack").TryGetValueAsString(out var text))
            failCount++;
        if (!bundle.GetArgument(1, "Needle").TryGetValueAsString(out var needle))
            failCount++;

        if (failCount > 0 || string.IsNullOrEmpty(text) || string.IsNullOrEmpty(needle)) {
            return new DreamValue(failCount == 2 ? 1 : 0);
        }

        int start = bundle.GetArgument(2, "Start").GetValueAsInteger(); //1-indexed
        int end = bundle.GetArgument(3, "End").GetValueAsInteger(); //1-indexed
        int actualstart;
        int actualcount;

        if(start > 0)
            actualstart = start-1;
        else
            actualstart = (text.Length-1) + start;
        actualstart += needle.Length-1;
        actualstart = Math.Max(Math.Min(text.Length, actualstart),0);

        if(end > 0)
            actualcount = actualstart - (end-1) + needle.Length;
        else
            actualcount  = actualstart - ((text.Length-1) + (end));
        actualcount += needle.Length-1;
        actualcount = Math.Max(Math.Min(actualstart+1, actualcount),0);
        int needleIndex = text.LastIndexOf(needle, actualstart, actualcount, StringComparison.InvariantCulture);
        return new DreamValue(needleIndex + 1); //1-indexed, or 0 if not found (LastIndexOf returns -1 if not found)
    }

    [DreamProc("flick")]
    [DreamProcParameter("Icon", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamResource)]
    [DreamProcParameter("Object", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamResource)]
    public static DreamValue NativeProc_flick(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var iconArg = bundle.GetArgument(0, "Icon");
        var objectArg = bundle.GetArgument(1, "Object");

        if (!objectArg.TryGetValueAsDreamObject<DreamObjectAtom>(out var atom))
            return DreamValue.Null;

        var appearance = bundle.AtomManager.MustGetAppearance(atom);
        int iconId;
        if (iconArg.TryGetValueAsString(out var iconState)) {
            if (appearance.Icon == null)
                return DreamValue.Null;

            iconId = appearance.Icon.Value;
        } else if (iconArg.TryGetValueAsDreamResource(out var resource)) {
            iconId = resource.Id;
            iconState = appearance.IconState;
        } else {
            return DreamValue.Null;
        }

        var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
        if (!entitySystemManager.TryGetEntitySystem(out ServerAppearanceSystem? appearanceSystem))
            return DreamValue.Null;

        appearanceSystem.Flick(atom, iconId, iconState);
        return DreamValue.Null;
    }

    [DreamProc("flist")]
    [DreamProcParameter("Path", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_flist(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "Path").TryGetValueAsString(out var path)) {
            path = "./";
        }

        try {
            var listing = bundle.ResourceManager.EnumerateListing(path);
            DreamList list = bundle.ObjectTree.CreateList(listing);
            return new DreamValue(list);
        } catch (DirectoryNotFoundException) {
            return new DreamValue(bundle.ObjectTree.CreateList()); // empty list
        }
    }

    [DreamProc("floor")]
    [DreamProcParameter("A", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_floor(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        bundle.GetArgument(0, "A").TryGetValueAsFloat(out float floatNum);

        return new DreamValue(MathF.Floor(floatNum));
    }

    [DreamProc("fract")]
    [DreamProcParameter("n", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_fract(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        bundle.GetArgument(0, "n").TryGetValueAsFloat(out float floatNum);

        if(float.IsInfinity(floatNum)) {
            return new DreamValue(0);
        }

        return new DreamValue(floatNum - MathF.Truncate(floatNum));
    }

    [DreamProc("ftime")]
    [DreamProcParameter("File", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("IsCreationTime", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_ftime(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue file = bundle.GetArgument(0, "File");
        DreamValue isCreationTime = bundle.GetArgument(1, "IsCreationTime");

        if (file.TryGetValueAsString(out var rscPath)) {
            var fi = new FileInfo(rscPath);
            if (isCreationTime.IsTruthy()) {
                return new DreamValue((fi.CreationTime - new DateTime(2000, 1, 1)).TotalMilliseconds / 100);
            }

            return new DreamValue((fi.LastWriteTime - new DateTime(2000, 1, 1)).TotalMilliseconds / 100);
        }

        throw new Exception("Invalid path argument");
    }

    [DreamProc("get_step_to")]
    [DreamProcParameter("Ref", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Trg", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Min", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_get_step_to(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var refArg = bundle.GetArgument(0, "Ref");
        var trgArg = bundle.GetArgument(1, "Trg");
        var minArg = (int)bundle.GetArgument(2, "Min").UnsafeGetValueAsFloat();

        if (!refArg.TryGetValueAsDreamObject<DreamObjectAtom>(out var refAtom))
            return DreamValue.Null;
        if (!trgArg.TryGetValueAsDreamObject<DreamObjectAtom>(out var trgAtom))
            return DreamValue.Null;

        var loc = bundle.AtomManager.GetAtomPosition(refAtom);
        var dest = bundle.AtomManager.GetAtomPosition(trgAtom);
        var steps = bundle.MapManager.CalculateSteps(loc, dest, minArg);

        // We perform a whole path-find then return only the first step
        // Truly, DM's most optimized proc
        var direction = steps.FirstOrDefault();
        var stepLoc = direction switch {
            // The ref says get_step_to() returns 0 if there's no change, but it also says it returns null.
            // I wasn't able to get it to return 0 so null it is.
            0 => null,
            _ => DreamProcNativeHelpers.GetStep(bundle.AtomManager, bundle.MapManager, refAtom, direction)
        };

        return new(stepLoc);
    }

    [DreamProc("get_steps_to")]
    [DreamProcParameter("Ref", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Trg", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Min", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_get_steps_to(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var refArg = bundle.GetArgument(0, "Ref");
        var trgArg = bundle.GetArgument(1, "Trg");
        var minArg = (int)bundle.GetArgument(2, "Min").UnsafeGetValueAsFloat();

        if (!refArg.TryGetValueAsDreamObject<DreamObjectAtom>(out var refAtom))
            return DreamValue.Null;
        if (!trgArg.TryGetValueAsDreamObject<DreamObjectAtom>(out var trgAtom))
            return DreamValue.Null;

        var loc = bundle.AtomManager.GetAtomPosition(refAtom);
        var dest = bundle.AtomManager.GetAtomPosition(trgAtom);
        var steps = bundle.MapManager.CalculateSteps(loc, dest, minArg);
        var result = bundle.ObjectTree.CreateList();

        foreach (var step in steps) {
            result.AddValue(new((int)step));
        }

        // Null if there are no steps
        return new(result.GetLength() > 0 ? result : null);
    }

    [DreamProc("generator")]
    [DreamProcParameter("type", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("A", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("B", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("rand", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_generator(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        // TODO: Invalid value gives an invalid /generator instance
        var outputTypeString = bundle.GetArgument(0, "type").MustGetValueAsString();

        var a = bundle.GetArgument(1, "A");
        var b = bundle.GetArgument(2, "B");
        var distNum = bundle.GetArgument(3, "rand").MustGetValueAsInteger();

        GeneratorOutputType outputType;
        GeneratorDistribution distribution;
        switch(outputTypeString) {
            case "num":
                outputType = GeneratorOutputType.Num;
                break;
            case "vector":
                outputType = GeneratorOutputType.Vector;
                break;
            case "box":
                outputType = GeneratorOutputType.Box;
                break;
            case "color":
                outputType = GeneratorOutputType.Color;
                break;
            case "circle":
                outputType = GeneratorOutputType.Circle;
                break;
            case "sphere":
                outputType = GeneratorOutputType.Sphere;
                break;
            case "square":
                outputType = GeneratorOutputType.Square;
                break;
            case "cube":
                outputType = GeneratorOutputType.Cube;
                break;
            default:
                throw new InvalidEnumArgumentException("Invalid output type specified in generator()");
        }

        switch(distNum) {
            case 0:
                distribution = GeneratorDistribution.Uniform;
                break;
            case 1:
                distribution = GeneratorDistribution.Normal;
                break;
            case 2:
                distribution = GeneratorDistribution.Linear;
                break;
            case 3:
                distribution = GeneratorDistribution.Square;
                break;
            default:
                throw new InvalidEnumArgumentException("Invalid distribution type specified in generator()");
        }

        return new(new DreamObjectGenerator(bundle.ObjectTree.Generator.ObjectDefinition, a, b, outputType, distribution));
    }

    [DreamProc("hascall")]
    [DreamProcParameter("Object", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("ProcName", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_hascall(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var arg = bundle.GetArgument(0, "Object");
        if (!arg.TryGetValueAsDreamObject<DreamObject>(out var obj))
            return new DreamValue(0);
        if(!bundle.GetArgument(1, "ProcName").TryGetValueAsString(out var procName))
            return new DreamValue(0);

        return new DreamValue(obj.ObjectDefinition.HasProc(procName) ? 1 : 0);
    }

    [DreamProc("hearers")]
    [DreamProcParameter("Depth", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("Center", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_hearers(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) { //TODO: Change depending on center
        return DreamProcNativeHelpers.HandleViewersHearers(bundle, usr, true);
    }

    [DreamProc("html_decode")]
    [DreamProcParameter("HtmlText", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_html_decode(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        string htmlText = bundle.GetArgument(0, "HtmlText").Stringify();

        return new DreamValue(HttpUtility.HtmlDecode(htmlText));
    }

    [DreamProc("html_encode")]
    [DreamProcParameter("PlainText", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_html_encode(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        string plainText = bundle.GetArgument(0, "PlainText").Stringify();

        return new DreamValue(HttpUtility.HtmlEncode(plainText));
    }

    [DreamProc("icon_states")]
    [DreamProcParameter("Icon", Type = DreamValueTypeFlag.DreamResource)]
    [DreamProcParameter("mode", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_icon_states(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var mode = bundle.GetArgument(1, "mode").MustGetValueAsInteger();
        if (mode != 0) {
            throw new NotImplementedException("Only mode 0 is implemented");
        }

        var arg = bundle.GetArgument(0, "Icon");

        if (arg.TryGetValueAsDreamObject<DreamObjectIcon>(out var iconObj)) {
            // Fast path for /icon, we don't need to generate the entire DMI
            return new DreamValue(bundle.ObjectTree.CreateList(iconObj.Icon.States.Keys.ToArray()));
        } else if (bundle.ResourceManager.TryLoadIcon(arg, out var iconRsc)) {
            return new DreamValue(bundle.ObjectTree.CreateList(iconRsc.DMI.States.Keys.ToArray()));
        } else if (arg.IsNull) {
            return DreamValue.Null;
        } else {
            throw new Exception($"Bad icon {arg}");
        }
    }

    [DreamProc("image")]
    [DreamProcParameter("icon", Type = DreamValueTypeFlag.DreamResource)]
    [DreamProcParameter("loc", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("icon_state", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("layer", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("dir", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("pixel_x", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("pixel_y", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_image(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamObject imageObject = bundle.ObjectTree.CreateObject(bundle.ObjectTree.Image);
        imageObject.InitSpawn(new DreamProcArguments(bundle.Arguments)); // TODO: Don't create another thread
        return new DreamValue(imageObject);
    }

    [DreamProc("isarea")]
    [DreamProcParameter("Loc1", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_isarea(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        foreach (var arg in bundle.Arguments) {
            if (!arg.IsDreamObject<DreamObjectArea>())
                return DreamValue.False;
        }

        return DreamValue.True;
    }

    [DreamProc("isfile")]
    [DreamProcParameter("File")]
    public static DreamValue NativeProc_isfile(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue file = bundle.GetArgument(0, "File");

        return new DreamValue((file.Type == DreamValueType.DreamResource) ? 1 : 0);
    }

    [DreamProc("isicon")]
    [DreamProcParameter("Icon")]
    public static DreamValue NativeProc_isicon(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue icon = bundle.GetArgument(0, "Icon");
        if (icon.IsDreamObject<DreamObjectIcon>())
            return new DreamValue(1);
        else if (icon.TryGetValueAsDreamResource(out var resource)) {
            switch (Path.GetExtension(resource.ResourcePath)) {
                case ".dmi":
                case ".bmp":
                case ".png":
                case ".jpg":
                case ".gif":
                    return new DreamValue(1);
                default:
                    return new DreamValue(0);
            }
        } else {
            return new DreamValue(0);
        }
    }

    [DreamProc("isinf")]
    [DreamProcParameter("n", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_isinf(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (bundle.GetArgument(0, "n").TryGetValueAsFloat(out float floatnum)) {
            return new DreamValue(float.IsInfinity(floatnum) ? 1f : 0f);
        }

        return DreamValue.False;
    }

    [DreamProc("islist")]
    [DreamProcParameter("Object")]
    public static DreamValue NativeProc_islist(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        bool isList = bundle.GetArgument(0, "Object").IsDreamObject<DreamList>();
        return new DreamValue(isList ? 1 : 0);
    }

    [DreamProc("isloc")]
    [DreamProcParameter("Loc1", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_isloc(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        foreach (var arg in bundle.Arguments) {
            // The DM reference says "mobs, objs, turfs, or areas"
            // You might think this excludes /atom/movable, but it does not
            // So test for any DreamObjectAtom type
            if (!arg.TryGetValueAsDreamObject<DreamObjectAtom>(out _))
                return DreamValue.False;
        }

        return DreamValue.True;
    }

    [DreamProc("ismob")]
    [DreamProcParameter("Loc1", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_ismob(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        foreach (var arg in bundle.Arguments) {
            if (!arg.IsDreamObject<DreamObjectMob>())
                return DreamValue.False;
        }

        return DreamValue.True;
    }

    [DreamProc("isobj")]
    [DreamProcParameter("Loc1", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_isobj(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        foreach (var arg in bundle.Arguments) {
            if (!arg.TryGetValueAsDreamObject(out var dreamObject) || dreamObject == null || !dreamObject.IsSubtypeOf(bundle.ObjectTree.Obj))
                return DreamValue.False;
        }

        return DreamValue.True;
    }

    [DreamProc("ismovable")]
    [DreamProcParameter("Loc1", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_ismovable(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        foreach (var arg in bundle.Arguments) {
            if (!arg.IsDreamObject<DreamObjectMovable>())
                return DreamValue.False;
        }

        return DreamValue.True;
    }

    [DreamProc("isnan")]
    [DreamProcParameter("n", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_isnan(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (bundle.GetArgument(0, "n").TryGetValueAsFloat(out float floatNum))
            return new DreamValue(float.IsNaN(floatNum) ? 1 : 0);

        return DreamValue.False;
    }

    [DreamProc("isnull")]
    [DreamProcParameter("Val")]
    public static DreamValue NativeProc_isnull(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue value = bundle.GetArgument(0, "Val");

        return new DreamValue((value.IsNull) ? 1 : 0);
    }

    [DreamProc("isnum")]
    [DreamProcParameter("Val")]
    public static DreamValue NativeProc_isnum(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue value = bundle.GetArgument(0, "Val");

        return new DreamValue((value.Type == DreamValueType.Float) ? 1 : 0);
    }

    [DreamProc("ispath")]
    [DreamProcParameter("Val")]
    [DreamProcParameter("Type", Type = DreamValueTypeFlag.DreamType)]
    public static DreamValue NativeProc_ispath(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue value = bundle.GetArgument(0, "Val");
        DreamValue type = bundle.GetArgument(1, "Type");

        if (value.TryGetValueAsType(out var valueType)) {
            if (type.TryGetValueAsType(out var ancestor)) {
                return new DreamValue(valueType.ObjectDefinition.IsSubtypeOf(ancestor) ? 1 : 0);
            } else {
                return new DreamValue(1);
            }
        }

        return new DreamValue(0);
    }

    [DreamProc("istext")]
    [DreamProcParameter("Val")]
    public static DreamValue NativeProc_istext(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue value = bundle.GetArgument(0, "Val");

        return new DreamValue((value.Type == DreamValueType.String) ? 1 : 0);
    }

    [DreamProc("isturf")]
    [DreamProcParameter("Loc1", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_isturf(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        foreach (var arg in bundle.Arguments) {
            if (!arg.IsDreamObject<DreamObjectTurf>())
                return DreamValue.False;
        }

        return DreamValue.True;
    }

    private static DreamValue CreateValueFromJsonElement(DreamObjectTree objectTree, JsonElement jsonElement) {
        switch (jsonElement.ValueKind) {
            case JsonValueKind.Array: {
                DreamList list = objectTree.CreateList(jsonElement.GetArrayLength());

                foreach (JsonElement childElement in jsonElement.EnumerateArray()) {
                    DreamValue value = CreateValueFromJsonElement(objectTree, childElement);

                    list.AddValue(value);
                }

                return new DreamValue(list);
            }
            case JsonValueKind.Object: {
                // Stick to using an enumerator because getting an array, specially of an
                // object in a plaintext notation of unconstrained size, causes lot of
                // heap allocation, so avoid that.
                var enumerator = jsonElement.EnumerateObject();
                DreamList list = objectTree.CreateList();

                // The object contained nothing.
                if(!enumerator.MoveNext())
                    return new DreamValue(list);

                // For handling special values expressed as single-property objects
                // Such as float-point values Infinity and NaN
                var first = enumerator.Current;
                var hasSecond = enumerator.MoveNext();
                if (!hasSecond) {
                    switch(first.Name) {
                        case "__number__": {
                            var raw = first.Value.GetString();
                            var val = raw != null ? float.Parse(raw) : float.NaN;
                            return new DreamValue(val);
                        }
                    }
                }

                // It was not a single-property? Or the property was not special?
                // FANTASTIC. STOP PRETENDING BEING A PARSER AND INSERT THEM IN A LIST
                DreamValue v1 = CreateValueFromJsonElement(objectTree, first.Value);
                list.SetValue(new DreamValue(first.Name), v1);

                if(!hasSecond)
                    return new DreamValue(list);

                var second = enumerator.Current;
                DreamValue v2 = CreateValueFromJsonElement(objectTree, second.Value);
                list.SetValue(new DreamValue(second.Name), v2);

                // Enumerate the damn rest of the godawful fucking shitty JSON
                foreach (JsonProperty childProperty in jsonElement.EnumerateObject()) {
                    DreamValue value = CreateValueFromJsonElement(objectTree, childProperty.Value);

                    list.SetValue(new DreamValue(childProperty.Name), value);
                }

                return new DreamValue(list);
            }
            case JsonValueKind.String:
                return new DreamValue(jsonElement.GetString() ?? ""); // it shouldn't be null but it was throwing a warning
            case JsonValueKind.Number:
                if (!jsonElement.TryGetSingle(out float floatValue)) {
                    throw new Exception("Invalid number " + jsonElement);
                }

                return new DreamValue(floatValue);
            case JsonValueKind.True:
                return new DreamValue(1);
            case JsonValueKind.False:
                return new DreamValue(0);
            case JsonValueKind.Null:
                return DreamValue.Null;
            default:
                throw new Exception("Invalid ValueKind " + jsonElement.ValueKind);
        }
    }

    /// <summary>
    /// A helper function for /proc/json_encode(). Encodes a DreamValue into a json writer.
    /// </summary>
    /// <param name="writer">The json writer to encode into</param>
    /// <param name="value">The DreamValue to encode</param>
    private static void JsonEncode(Utf8JsonWriter writer, DreamValue value) {
        // In parity with DM, we give up and just print a 'null' at the maximum recursion.
        if (writer.CurrentDepth >= 20) {
            writer.WriteNullValue();
            return;
        }

        if (value.TryGetValueAsFloat(out float floatValue)) {
            // For parity with Byond where it gets around the JSON standard not supporting
            // the floating point specials INFINITY and NAN by writing it as an object
            if (float.IsFinite(floatValue))
                writer.WriteNumberValue(floatValue);
            else {
                writer.WriteStartObject();
                writer.WriteString("__number__", floatValue.ToString());
                writer.WriteEndObject();
            }
        } else if (value.TryGetValueAsString(out var text))
            writer.WriteStringValue(text);
        else if (value.TryGetValueAsType(out var type))
            writer.WriteStringValue(type.Path);
        else if (value.TryGetValueAsProc(out var proc))
            writer.WriteStringValue(proc.ToString());
        else if (value.TryGetValueAsIDreamList(out var list)) {
            if (list.IsAssociative) {
                writer.WriteStartObject();

                foreach (DreamValue listValue in list.EnumerateValues()) {
                    var key = listValue.Stringify();

                    if (list.ContainsKey(listValue)) {
                        var subValue = list.GetValue(listValue);
                        if(subValue.TryGetValueAsDreamList(out var subList) && subList is DreamListVars) //BYOND parity, do not print vars["vars"] - note that this is *not* a generic infinite loop protection on purpose
                            continue;
                        writer.WritePropertyName(key);
                        JsonEncode(writer, subValue);
                    } else {
                        writer.WriteNull(key);
                    }
                }

                writer.WriteEndObject();
            } else {
                writer.WriteStartArray();

                foreach (DreamValue listValue in list.EnumerateValues()) {
                    JsonEncode(writer, listValue);
                }

                writer.WriteEndArray();
            }
        } else if (value.TryGetValueAsDreamObject(out var dreamObject)) {
            switch (dreamObject) {
                case null:
                    writer.WriteNullValue();
                    break;
                case DreamObjectMatrix matrix: {  // Special behaviour for /matrix values
                    writer.WriteStartArray();

                    foreach (var f in DreamObjectMatrix.EnumerateMatrix(matrix)) {
                        writer.WriteNumberValue(f);
                    }

                    writer.WriteEndArray();
                    // This doesn't have any corresponding snowflaking in CreateValueFromJsonElement()
                    // because BYOND actually just forgets that this was a matrix after doing json encoding.
                    break;
                }
                case DreamObjectVector vector: { // Special behaviour for /vector values
                    if (vector.Is3D)
                        writer.WriteStringValue($"vector({vector.X},{vector.Y},{vector.Z})");
                    else
                        writer.WriteStringValue($"vector({vector.X},{vector.Y})");
                    break;
                }
                default:
                    writer.WriteStringValue(value.Stringify());
                    break;
            }
        } else if (value.TryGetValueAsDreamResource(out var dreamResource)) {
            writer.WriteStringValue(dreamResource.ResourcePath);
        } else {
            throw new Exception($"Cannot json_encode {value}");
        }
    }

    [DreamProc("json_decode")]
    [DreamProcParameter("JSON", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_json_decode(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "JSON").TryGetValueAsString(out var jsonString)) {
            throw new Exception("Unknown value");
        }

        JsonElement jsonRoot = JsonSerializer.Deserialize<JsonElement>(jsonString);

        return CreateValueFromJsonElement(bundle.ObjectTree, jsonRoot);
    }

    [DreamProc("json_encode")]
    [DreamProcParameter("Value")]
    [DreamProcParameter("flags")]
    public static DreamValue NativeProc_json_encode(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        using MemoryStream stream = new MemoryStream();
        // 515 JSON_PRETTY_PRINT flag
        bundle.GetArgument(1, "flags").TryGetValueAsInteger(out var prettyPrint);

        JsonWriterOptions options = new JsonWriterOptions {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // "\"" instead of "\u0022"
            Indented = (prettyPrint == 1)
        };

        using Utf8JsonWriter jsonWriter = new(stream, options);

        JsonEncode(jsonWriter, bundle.GetArgument(0, "Value"));
        jsonWriter.Flush();

        return new DreamValue(Encoding.UTF8.GetString(stream.AsSpan()));
    }

    public static DreamValue _length(DreamValue value, bool countBytes) {
        if (value.TryGetValueAsString(out var str)) {
            return new DreamValue(countBytes ? str.Length : str.EnumerateRunes().Count());
        } else if (value.TryGetValueAsIDreamList(out var list)) {
            return new DreamValue(list.GetLength());
        } else if (value.Type is DreamValueType.Float or DreamValueType.DreamObject or DreamValueType.DreamType) {
            return new DreamValue(0);
        }

        throw new Exception($"Cannot check length of {value}");
    }

    [DreamProc("length_char")]
    [DreamProcParameter("E")]
    public static DreamValue NativeProc_length_char(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue value = bundle.GetArgument(0, "E");
        return _length(value, false);
    }

    [DreamProc("lerp")]
    [DreamProcParameter("A")]
    [DreamProcParameter("B")]
    [DreamProcParameter("factor", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_lerp(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue valA = bundle.GetArgument(0, "A");
        DreamValue valB = bundle.GetArgument(1, "B");
        DreamValue valFactor = bundle.GetArgument(2, "factor");

        if (!valFactor.TryGetValueAsFloatCoerceNull(out var factor))
            throw new Exception($"lerp factor {valFactor} is not a num");

        // TODO: Support non-num arguments like vectors
        if (valA.TryGetValueAsFloatCoerceNull(out var floatA) && valB.TryGetValueAsFloatCoerceNull(out var floatB)) {
            return new DreamValue(floatA + (floatB - floatA) * factor);
        }

        // TODO: Change this to a type mismatch runtime once the other valid arg types are supported
        throw new NotImplementedException($"lerp() currently only supports nums and null; got {valA} and {valB}");
    }

    [DreamProc("list2params")]
    [DreamProcParameter("List")]
    public static DreamValue NativeProc_list2params(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "List").TryGetValueAsIDreamList(out var list))
            return new DreamValue(string.Empty);

        return new DreamValue(List2Params(list));
    }

    [DreamProc("lowertext")]
    [DreamProcParameter("T", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_lowertext(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var arg = bundle.GetArgument(0, "T");
        if (!arg.TryGetValueAsString(out var text)) {
            return arg;
        }

        return new DreamValue(text.ToLower());
    }

    // TODO: Out-line all of the exception throws into no-inline functions.
    [DreamProc("matrix")]
    [DreamProcParameter("a")]
    [DreamProcParameter("b")]
    [DreamProcParameter("c")]
    [DreamProcParameter("d")]
    [DreamProcParameter("e")]
    [DreamProcParameter("f")]
    public static DreamValue NativeProc_matrix(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamObjectMatrix matrix;
        // normal, documented uses of matrix().
        switch(bundle.Arguments.Length) {
            case 6: // Take the arguments and construct a matrix.
            case 0: // Since arguments are empty, this just creates an identity matrix.
                matrix = bundle.ObjectTree.CreateObject<DreamObjectMatrix>(bundle.ObjectTree.Matrix);
                matrix.InitSpawn(new DreamProcArguments(bundle.Arguments));
                return new DreamValue(matrix);
            case 1: // Clone the matrix.
                var firstArg = bundle.GetArgument(0, "a");
                if (!firstArg.TryGetValueAsDreamObject<DreamObjectMatrix>(out var argObject)) // Expecting a matrix here
                    throw new ArgumentException($"/matrix() called with invalid argument '{firstArg}'");
                matrix = DreamObjectMatrix.MatrixClone(bundle.ObjectTree, argObject);
                return new DreamValue(matrix);
            case 5:
            case 4:
            case 3:
            case 2:
                break;
            default:
                throw new ArgumentException($"/matrix() called with {bundle.Arguments.Length}, expected 6 or less");
        }

        /* Byond here be dragons.
            * In 2015, Lummox posted onto the BYOND forums this little blog post: http://www.byond.com/forum/post/1881375
            * in it, he describes an otherwise-completely-undocumented use of the matrix() proc
            * in which it takes, some sort of "opcode" and some system of arguments and does stuff with them,
            * all of which are just aliases for already-existing behaviour in DM through the /matrix methods
            * (m.Clone() or m.Interpolate() and so on)
            *
            * Normally I'd never stoop to developing any such ridiculous behaviour, but for some reason,
            * Paradise and a few other targets actually make use of these alternative signatures.
            * So, here's that.
        */
        //First lets extract the opcode.
        var opcodeArgument = bundle.GetArgument(bundle.Arguments.Length - 1, "opcode");
        if (!opcodeArgument.TryGetValueAsInteger(out int opcodeArgumentValue))
            throw new ArgumentException($"/matrix() override called with '{opcodeArgument}', expecting opcode");

        bool doModify = false; // A bool to represent the MATRIX_MODIFY flag
        if ((opcodeArgumentValue & (int)MatrixOpcode.Modify) == (int)MatrixOpcode.Modify) {
            doModify = true;
            opcodeArgumentValue &= ~(int)MatrixOpcode.Modify;
        }

        MatrixOpcode opcode = (MatrixOpcode)opcodeArgumentValue;
        if (!Enum.IsDefined(opcode))
            throw new ArgumentException($"/matrix() override called with invalid opcode '{opcodeArgumentValue}'");

        //Now do the transformation or whatever that's implied by the opcode.
        var firstArgument = bundle.GetArgument(0, "a");
        var secondArgument = bundle.GetArgument(1, "b");
        switch (opcode) {
            case MatrixOpcode.Copy: // Clone the matrix. Basically a redundant version of matrix(m).
                if (!firstArgument.TryGetValueAsDreamObject<DreamObjectMatrix>(out var argObject)) // Expecting a matrix here
                    throw new ArgumentException($"/matrix() called with invalid argument '{firstArgument}'");

                matrix = DreamObjectMatrix.MatrixClone(bundle.ObjectTree, argObject);
                return new DreamValue(matrix);
            case MatrixOpcode.Invert:
                if (!firstArgument.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrixInput)) // Expecting a matrix here
                    throw new ArgumentException($"/matrix() called with invalid argument '{firstArgument}'");
                //Choose whether we are inverting the original matrix or a clone of it
                var invertableMatrix = doModify ? matrixInput : DreamObjectMatrix.MatrixClone(bundle.ObjectTree, matrixInput);
                if (!DreamObjectMatrix.TryInvert(invertableMatrix))
                    throw new ArgumentException("/matrix provided for MATRIX_INVERT cannot be inverted");

                return new DreamValue(invertableMatrix);
            case MatrixOpcode.Rotate:
                var angleArgument = firstArgument;
                if (firstArgument.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrixToRotate))
                    angleArgument = secondArgument; //We have a matrix to rotate, and an angle to rotate it by.
                if (!angleArgument.TryGetValueAsFloat(out float rotationAngle))
                    throw new ArgumentException($"/matrix() called with invalid rotation angle '{firstArgument}'");

                // NOTE: Not sure if BYOND uses double or float precision in this specific case.
                var (angleSin, angleCos) = ((float, float))Math.SinCos(Math.PI / 180.0 * rotationAngle);
                if (float.IsSubnormal(angleSin)) // FIXME: Think of a better solution to bad results for some angles.
                    angleSin = 0;
                if (float.IsSubnormal(angleCos))
                    angleCos = 0;

                var rotationMatrix = DreamObjectMatrix.MakeMatrix(bundle.ObjectTree, angleCos, angleSin, 0, -angleSin, angleCos, 0);
                if (matrixToRotate == null)
                    return new DreamValue(rotationMatrix);

                if (!doModify)
                    matrixToRotate = DreamObjectMatrix.MatrixClone(bundle.ObjectTree, matrixToRotate);

                DreamObjectMatrix.MultiplyMatrix(matrixToRotate, rotationMatrix);
                return new DreamValue(matrixToRotate);
            case MatrixOpcode.Scale:
                //Four possible signatures: two to create a scale-matrix, and one to scale an existing matrix
                //matrix(scale, MATRIX_SCALE)
                //matrix(x,  y, MATRIX_SCALE)
                //
                //matrix(m1,scale,MATRIX_SCALE)
                //matrix(m1,x,y,MATRIX_SCALE)
                float horizontalScale;
                float verticalScale;
                if (firstArgument.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrixArgument)) { // scaling a matrix
                    var scaledMatrix = doModify ? matrixArgument : DreamObjectMatrix.MatrixClone(bundle.ObjectTree, matrixArgument);

                    if (!secondArgument.TryGetValueAsFloat(out horizontalScale))
                        throw new ArgumentException($"/matrix() called with invalid scaling factor '{secondArgument}'");
                    if (bundle.Arguments.Length == 4) {
                        if (!bundle.GetArgument(2, "c").TryGetValueAsFloat(out verticalScale))
                            throw new ArgumentException($"/matrix() called with invalid scaling factor '{bundle.GetArgument(2, "c")}'");
                    } else {
                        verticalScale = horizontalScale;
                    }

                    DreamObjectMatrix.ScaleMatrix(scaledMatrix, horizontalScale, verticalScale);
                    return new DreamValue(scaledMatrix);
                } else { // making a scale-matrix
                    if (!firstArgument.TryGetValueAsFloat(out horizontalScale))
                        throw new ArgumentException($"/matrix() called with invalid scaling factor '{firstArgument}'");
                    if (bundle.Arguments.Length == 3) { // The 3-argument version of scale. matrix(x,y, MATRIX_SCALE)
                        if (!secondArgument.TryGetValueAsFloat(out verticalScale))
                            throw new ArgumentException($"/matrix() called with invalid scaling factor '{secondArgument}'");
                    } else { // The 2-argument version. matrix(scale, MATRIX_SCALE)
                        verticalScale = horizontalScale;
                    }

                    //A scaling matrix has the form {s,0,0, 0,s,0}, where s is the scaling factor.
                    return new DreamValue(DreamObjectMatrix.MakeMatrix(bundle.ObjectTree, horizontalScale, 0, 0, 0, verticalScale, 0));
                }
            case MatrixOpcode.Translate:
                //Possible signatures:
                //matrix(x, MATRIX_TRANSLATE), although this one isn't even freaking documented in the blog post!!
                //matrix(x, y, MATRIX_TRANSLATE)
                //matrix(m1, x, y, MATRIX_TRANSLATE)
                if(bundle.Arguments.Length == 4) { // the 4-arg situation
                    if (!firstArgument.TryGetValueAsDreamObject<DreamObjectMatrix>(out var targetMatrix)) // Expecting a matrix here
                        throw new ArgumentException($"/matrix() called with invalid argument '{firstArgument}', expecting matrix");

                    DreamObjectMatrix translateMatrix;
                    if (doModify)
                        translateMatrix = targetMatrix;
                    else
                        translateMatrix = DreamObjectMatrix.MatrixClone(bundle.ObjectTree, targetMatrix);

                    bundle.GetArgument(1,"b").TryGetValueAsFloat(out float horizontalOffset);
                    translateMatrix.GetVariable("c").TryGetValueAsFloat(out float oldXOffset);
                    translateMatrix.SetVariableValue("c", new(horizontalOffset + oldXOffset));

                    bundle.GetArgument(2, "c").TryGetValueAsFloat(out float verticalOffset);
                    translateMatrix.GetVariable("f").TryGetValueAsFloat(out float oldYOffset);
                    translateMatrix.SetVariableValue("f", new(verticalOffset + oldYOffset));
                    return new DreamValue(translateMatrix);
                }

                float horizontalShift;
                float verticalShift;
                if (!firstArgument.TryGetValueAsFloat(out horizontalShift))
                    throw new ArgumentException($"/matrix() called with invalid translation factor '{firstArgument}'");
                if (bundle.Arguments.Length == 3) {
                    var secondArg = bundle.GetArgument(1, "b");
                    if (!secondArg.TryGetValueAsFloat(out verticalShift))
                        throw new ArgumentException($"/matrix() called with invalid translation factor '{secondArg}'");
                } else {
                    verticalShift = horizontalShift;
                }

                var translationMatrix = DreamObjectMatrix.MakeMatrix(bundle.ObjectTree, 1, 0, horizontalShift, 0, 1, verticalShift);
                return new DreamValue(translationMatrix);
            default: // Being here means that the opcode is defined but not yet implemented within this switch.
                throw new NotImplementedException($"/matrix() called with unimplemented opcode '{Enum.GetName(opcode)}'");
        }
    }

    private static DreamValue MaxComparison(DreamValue max, DreamValue value) {
        if (value.TryGetValueAsFloat(out var lFloat)) {
            if (max.IsNull && lFloat >= 0)
                max = value;
            else if (max.TryGetValueAsFloat(out var rFloat) && lFloat > rFloat)
                max = value;
        } else if (value.IsNull) {
            if (max.TryGetValueAsFloat(out var maxFloat) && maxFloat <= 0)
                max = value;
        } else if (value.TryGetValueAsString(out var lString)) {
            if (max.IsNull)
                max = value;
            else if (max.TryGetValueAsString(out var rString) && string.Compare(lString, rString, StringComparison.Ordinal) > 0)
                max = value;
        } else {
            throw new Exception($"Cannot compare {max} and {value}");
        }

        return max;
    }

    [DreamProc("max")]
    [DreamProcParameter("A")]
    public static DreamValue NativeProc_max(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue max;

        if (bundle.Arguments.Length == 1) {
            DreamValue arg = bundle.GetArgument(0, "A");
            if (!arg.TryGetValueAsDreamList(out var list))
                return arg;

            var values = list.GetValues();
            if (values.Count == 0)
                return DreamValue.Null;

            max = values[0];
            for (int i = 1; i < values.Count; i++) {
                max = MaxComparison(max, values[i]);
            }
        } else {
            if (bundle.Arguments.Length == 0)
                return DreamValue.Null;

            max = bundle.Arguments[0];
            for (int i = 1; i < bundle.Arguments.Length; i++) {
                max = MaxComparison(max, bundle.Arguments[i]);
            }
        }

        return max;
    }

    [DreamProc("md5")]
    [DreamProcParameter("T", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamResource)]
    public static DreamValue NativeProc_md5(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if(bundle.Arguments.Length > 1) throw new Exception("md5() only takes one argument");
        DreamValue arg = bundle.GetArgument(0, "T");

        byte[] bytes;

        if (arg.TryGetValueAsDreamResource(out DreamResource? resource)) {
            byte[]? filebytes = resource.ResourceData;

            if (filebytes == null) {
                return DreamValue.Null;
            }

            bytes = filebytes;
        } else if (arg.TryGetValueAsString(out string? textdata)) {
            bytes = Encoding.UTF8.GetBytes(textdata);
        } else {
            return DreamValue.Null;
        }

        MD5 md5 = MD5.Create();
        byte[] output = md5.ComputeHash(bytes);
        //Match BYOND formatting
        string hash = BitConverter.ToString(output).Replace("-", "").ToLower();
        return new DreamValue(hash);
    }

    private static DreamValue MinComparison(DreamValue min, DreamValue value) {
        if (value.TryGetValueAsFloat(out var lFloat)) {
            if (min.IsNull && lFloat <= 0)
                min = value;
            else if (min.TryGetValueAsFloat(out var rFloat) && lFloat <= rFloat)
                min = value;
        } else if (value.IsNull) {
            if (min.TryGetValueAsFloat(out var minFloat) && minFloat >= 0)
                min = value;
        } else if (value.TryGetValueAsString(out var lString)) {
            if (min.IsNull)
                min = value;
            else if (min.TryGetValueAsString(out var rString) && string.Compare(lString, rString, StringComparison.Ordinal) <= 0)
                min = value;
        } else {
            throw new Exception($"Cannot compare {min} and {value}");
        }

        return min;
    }

    [DreamProc("min")]
    [DreamProcParameter("A")]
    public static DreamValue NativeProc_min(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue min;

        if (bundle.Arguments.Length == 1) {
            DreamValue arg = bundle.GetArgument(0, "A");
            if (!arg.TryGetValueAsDreamList(out var list))
                return arg;

            var values = list.GetValues();
            if (values.Count == 0)
                return DreamValue.Null;

            min = values[0];
            for (int i = 1; i < values.Count; i++) {
                min = MinComparison(min, values[i]);
            }
        } else {
            if (bundle.Arguments.Length == 0)
                return DreamValue.Null;

            min = bundle.Arguments[0];
            for (int i = 1; i < bundle.Arguments.Length; i++) {
                min = MinComparison(min, bundle.Arguments[i]);
            }
        }

        return min;
    }

    [DreamProc("nonspantext")]
    [DreamProcParameter("Haystack", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Needles", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
    public static DreamValue NativeProc_nonspantext(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "Haystack").TryGetValueAsString(out var text))
            return new DreamValue(0);
        if (!bundle.GetArgument(1, "Needles").TryGetValueAsString(out var needles))
            return new DreamValue(1);
        bundle.GetArgument(2, "Start").TryGetValueAsInteger(out var start);

        if (start == 0 || start > text.Length)
            return new DreamValue(0);

        if (start < 0)
            start += text.Length + 1;

        var index = text.AsSpan(start - 1).IndexOfAny(needles);
        if (index == -1)
            index = text.Length - start + 1;

        return new DreamValue(index);
    }

    [DreamProc("num2text")]
    [DreamProcParameter("N")]
    [DreamProcParameter("A", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("B", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_num2text(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue number = bundle.GetArgument(0, "N");

        if (!number.TryGetValueAsFloat(out float floatNum)) {
            return new DreamValue("0");
        }

        if(bundle.Arguments.Length == 1) {
            return new DreamValue(floatNum.ToString("g6"));
        }

        if(bundle.Arguments.Length == 2) {
            if(!bundle.GetArgument(1, "A").TryGetValueAsInteger(out var sigFig)) {
                return new DreamValue(floatNum.ToString("g6"));
            }

            return new DreamValue(floatNum.ToString($"g{sigFig}"));
        }

        if(bundle.Arguments.Length == 3) {
            var digits = Math.Max(bundle.GetArgument(1, "A").MustGetValueAsInteger(), 1);
            var radix = bundle.GetArgument(2, "B").MustGetValueAsInteger();
            var intNum = (int)floatNum;

            return new DreamValue(DreamProcNativeHelpers.ToBase(intNum, radix).PadLeft(digits, '0'));
        }

        // Maybe an exception is better?
        return new DreamValue("0");
    }

    [DreamProc("ohearers")]
    [DreamProcParameter("Depth", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("Center", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_ohearers(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) { //TODO: Change depending on center
        return DreamProcNativeHelpers.HandleOviewersOhearers(bundle, usr, true);
    }

    [DreamProc("orange")]
    [DreamProcParameter("Dist", Type = DreamValueTypeFlag.Float, DefaultValue = 5)]
    [DreamProcParameter("Center", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_orange(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        (DreamObjectAtom? center, ViewRange range) = DreamProcNativeHelpers.ResolveViewArguments(bundle.DreamManager, usr as DreamObjectAtom, bundle.Arguments);
        if (center is null)
            return new DreamValue(bundle.ObjectTree.CreateList());
        DreamList rangeList = bundle.ObjectTree.CreateList(range.Height * range.Width);
        foreach (var turf in DreamProcNativeHelpers.MakeViewSpiral(center, range)) {
            rangeList.AddValue(new DreamValue(turf));
            foreach (DreamValue content in turf.Contents.EnumerateValues()) {
                rangeList.AddValue(content);
            }
        }

        return new DreamValue(rangeList);
    }

    [DreamProc("oview")]
    [DreamProcParameter("Dist", Type = DreamValueTypeFlag.Float, DefaultValue = 5)]
    [DreamProcParameter("Center", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_oview(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamList view = bundle.ObjectTree.CreateList();

        (DreamObjectAtom? center, ViewRange range) = DreamProcNativeHelpers.ResolveViewArguments(bundle.DreamManager, usr as DreamObjectAtom, bundle.Arguments);
        if (center is null)
            return new(view);

        var eyePos = bundle.AtomManager.GetAtomPosition(center);
        var viewData = DreamProcNativeHelpers.CollectViewData(bundle.AtomManager, bundle.MapManager, eyePos, range);

        ViewAlgorithm.CalculateVisibility(viewData);

        var mapManager = bundle.MapManager;
        foreach (var tile in DreamProcNativeHelpers.MakeViewSpiral(viewData, false)) {
            if (tile == null || tile.IsVisible == false)
                continue;
            if (!mapManager.TryGetCellAt((eyePos.X + tile.DeltaX, eyePos.Y + tile.DeltaY), eyePos.Z, out var cell))
                continue;

            view.AddValue(new(cell.Turf));
            foreach (var movable in cell.Movables) {
                view.AddValue(new(movable));
            }
        }

        return new DreamValue(view);
    }

    [DreamProc("oviewers")]
    [DreamProcParameter("Depth", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("Center", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_oviewers(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) { //TODO: Change depending on center
        return DreamProcNativeHelpers.HandleOviewersOhearers(bundle, usr, false);
    }

    private static string List2Params(IDreamList list) {
        StringBuilder paramBuilder = new StringBuilder();

        foreach (DreamValue entry in list.EnumerateValues()) {
            if (list.ContainsKey(entry)) {
                paramBuilder.Append(
                    $"{HttpUtility.UrlEncode(entry.Stringify())}={HttpUtility.UrlEncode(list.GetValue(entry).Stringify())}");
            } else {
                paramBuilder.Append(HttpUtility.UrlEncode(entry.Stringify()));
            }

            paramBuilder.Append('&');
        }

        //Remove trailing &
        if (paramBuilder.Length > 0) paramBuilder.Remove(paramBuilder.Length - 1, 1);
        return paramBuilder.ToString();
    }

    public static DreamList Params2List(DreamObjectTree objectTree, string queryString) {
        queryString = queryString.Replace(";", "&");
        NameValueCollection query = HttpUtility.ParseQueryString(queryString);
        DreamList list = objectTree.CreateList();

        foreach (string? queryKey in query.AllKeys) {
            string[]? queryValues = query.GetValues(queryKey);

            if (queryValues == null)
                continue;

            if (queryKey == null) { // queryValues contains every value without a key
                foreach (string value in queryValues.Distinct()) {
                    int count = queryValues.Count(item => item == value);

                    if (count > 1) { // "a;a;a" creates list(a=list("","",""))
                        var valueList = objectTree.CreateList(count);

                        for (int i = 0; i < count; i++)
                            valueList.AddValue(new(string.Empty));

                        list.SetValue(new(value), new(valueList));
                    } else {
                        list.SetValue(new(value), new(string.Empty));
                    }
                }
            } else {
                string queryValue = queryValues[^1]; //Use the last appearance of the key in the query

                list.SetValue(new DreamValue(queryKey), new DreamValue(queryValue));
            }
        }

        return list;
    }

    [DreamProc("params2list")]
    [DreamProcParameter("Params", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_params2list(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue paramsValue = bundle.GetArgument(0, "Params");
        DreamList result;

        if (paramsValue.TryGetValueAsString(out var paramsString)) {
            result = Params2List(bundle.ObjectTree, paramsString);
        } else {
            result = bundle.ObjectTree.CreateList();
        }

        return new DreamValue(result);
    }

    [DreamProc("rand")]
    [DreamProcParameter("L", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("H", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_rand(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (bundle.Arguments.Length == 0) {
            return new DreamValue(bundle.DreamManager.Random.NextSingle());
        } else if (bundle.Arguments.Length == 1) {
            bundle.GetArgument(0, "L").TryGetValueAsInteger(out var high);

            return new DreamValue(bundle.DreamManager.Random.Next(high+1)); // rand() is inclusive on both ends
        } else {
            bundle.GetArgument(0, "L").TryGetValueAsInteger(out var low);
            bundle.GetArgument(1, "H").TryGetValueAsInteger(out var high);

            return new DreamValue(bundle.DreamManager.Random.Next(Math.Min(low, high), Math.Max(low, high)+1)); // rand() is inclusive on both ends
        }
    }

    [DreamProc("rand_seed")]
    [DreamProcParameter("Seed", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_rand_seed(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        bundle.GetArgument(0, "Seed").TryGetValueAsInteger(out var seed);

        bundle.DreamManager.Random = new Random(seed);
        return DreamValue.Null;
    }

    [DreamProc("range")]
    [DreamProcParameter("Dist", Type = DreamValueTypeFlag.Float, DefaultValue = 5)]
    [DreamProcParameter("Center", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_range(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        (DreamObjectAtom? center, ViewRange range) = DreamProcNativeHelpers.ResolveViewArguments(bundle.DreamManager, usr as DreamObjectAtom, bundle.Arguments);
        if (center is null)
            return new DreamValue(bundle.ObjectTree.CreateList());
        DreamList rangeList = bundle.ObjectTree.CreateList(range.Height * range.Width);
        //Have to include centre
        rangeList.AddValue(new DreamValue(center));
        if(center.TryGetVariable("contents", out var centerContents) && centerContents.TryGetValueAsDreamList(out var centerContentsList)) {
            foreach(DreamValue content in centerContentsList.EnumerateValues()) {
                rangeList.AddValue(content);
            }
        }

        if (center is not DreamObjectTurf) { // If it's not a /turf, we have to include its loc and the loc's contents
            if(center.TryGetVariable("loc",out DreamValue centerLoc) && centerLoc.TryGetValueAsDreamObject<DreamObjectAtom>(out var centerLocObject)) {
                rangeList.AddValue(centerLoc);
                if(centerLocObject.GetVariable("contents").TryGetValueAsDreamList(out var locContentsList)) {
                    foreach (DreamValue content in locContentsList.EnumerateValues()) {
                        rangeList.AddValue(content);
                    }
                }
            }
        }

        //And then everything else
        foreach (var turf in DreamProcNativeHelpers.MakeViewSpiral(center, range)) {
            rangeList.AddValue(new DreamValue(turf));
            foreach (DreamValue content in turf.Contents.EnumerateValues()) {
                rangeList.AddValue(content);
            }
        }

        return new DreamValue(rangeList);
    }

    [DreamProc("ref")]
    [DreamProcParameter("Object", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_ref(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        return new DreamValue(bundle.DreamManager.CreateRef(bundle.GetArgument(0, "Object")));
    }

    [DreamProc("regex")]
    [DreamProcParameter("pattern", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("flags", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_regex(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var patternOrRegex = bundle.GetArgument(0, "pattern");
        var flags = bundle.GetArgument(1, "flags");
        if (flags.TryGetValueAsInteger(out var specialMode) && patternOrRegex.TryGetValueAsString(out var text)) {
            switch(specialMode) {
                case 1:
                    return new DreamValue(Regex.Escape(text));
                case 2:
                    return new DreamValue(text.Replace("$", "$$"));
            }
        }

        var newRegex = bundle.ObjectTree.CreateObject(bundle.ObjectTree.Regex);
        newRegex.InitSpawn(new DreamProcArguments(bundle.Arguments));
        return new DreamValue(newRegex);
    }

    [DreamProc("replacetext")]
    [DreamProcParameter("Haystack", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Needle", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Replacement", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
    [DreamProcParameter("End", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_replacetext(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue haystack = bundle.GetArgument(0, "Haystack");
        DreamValue needle = bundle.GetArgument(1, "Needle");
        DreamValue replacementArg = bundle.GetArgument(2, "Replacement");
        bundle.GetArgument(3, "Start").TryGetValueAsInteger(out var start); //1-indexed
        int end = bundle.GetArgument(4, "End").GetValueAsInteger(); //1-indexed

        if (needle.TryGetValueAsDreamObject<DreamObjectRegex>(out var regexObject)) {
            // According to the docs, this is the same as /regex.Replace()
            return DreamProcNativeRegex.RegexReplace(regexObject, haystack, replacementArg, start, end);
        }

        if (!haystack.TryGetValueAsString(out var text)) {
            return DreamValue.Null;
        }

        if (start == 0) { // Return unmodified if Start is 0
            return new(text);
        } else if (start < 0) { // Negative wrap-around
            start = Math.Max(start + text.Length + 1, 1);
        }

        var arg3 = replacementArg.TryGetValueAsString(out var replacement);

        if (end <= 0) { // Zero or negative wrap-around
            end = Math.Max(end + text.Length + 1, start);
        }

        if (needle.IsNull) { // Insert the replacement after each char except the last
            if (!arg3) { // No change if no Replacement was given
                return new DreamValue(text);
            }

            // A Start of 2 is the same as 1. This only happens when Needle is null.
            if (start == 1)
                start = 2;

            // End cannot reach the last char
            end = Math.Min(end, text.Length);

            StringBuilder result = new StringBuilder();
            for (int i = 0; i < text.Length; i++) {
                result.Append(text[i]);
                if (i >= start - 2 && i < end - 1)
                    result.Append(replacement);
            }

            return new DreamValue(result.ToString());
        }

        if (needle.TryGetValueAsString(out var needleStr)) {
            string before = text.Substring(0, start - 1);
            string after = text.Substring(end - 1);
            string textSub = text.Substring(start - 1, end - start);
            string replaced = textSub.Replace(needleStr, replacement, StringComparison.OrdinalIgnoreCase);

            StringBuilder newTextBuilder = new();
            newTextBuilder.Append(before);
            newTextBuilder.Append(replaced);
            newTextBuilder.Append(after);

            return new DreamValue(newTextBuilder.ToString());
        }

        throw new Exception($"Invalid needle {needle}");
    }

    [DreamProc("replacetextEx")]
    [DreamProcParameter("Haystack", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Needle", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Replacement", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
    [DreamProcParameter("End", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_replacetextEx(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "Haystack").TryGetValueAsString(out var text)) {
            return DreamValue.Null;
        }

        var arg3 = bundle.GetArgument(2, "Replacement").TryGetValueAsString(out var replacement);

        if (!bundle.GetArgument(1, "Needle").TryGetValueAsString(out var needle)) {
            if (!arg3) {
                return new DreamValue(text);
            }

            //Insert the replacement after each char except the last char
            //TODO: Properly support non-default start/end values
            StringBuilder result = new StringBuilder();
            var pos = 0;
            while (pos + 1 <= text.Length) {
                result.Append(text[pos]).Append(arg3);
                pos += 1;
            }

            result.Append(text[pos]);
            return new DreamValue(result.ToString());
        }

        int start = bundle.GetArgument(3, "Start").GetValueAsInteger(); //1-indexed
        int end = bundle.GetArgument(4, "End").GetValueAsInteger(); //1-indexed

        if (start == 0) { // Return unmodified
            return new(text);
        } else if (start < 0) { // Negative wrap-around
            start = Math.Max(start + text.Length + 1, 1);
        }

        if (end <= 0) { // Zero and negative wrap-around
            end = Math.Max(end + text.Length + 1, start);
        }

        return new DreamValue(text.Substring(start - 1, end - start).Replace(needle, replacement, StringComparison.Ordinal));
    }

    [DreamProc("rgb2num")]
    [DreamProcParameter("color", Type = DreamValueTypeFlag.String, DefaultValue = "#FFFFFF")]
    [DreamProcParameter("space", Type = DreamValueTypeFlag.Float, DefaultValue = 0)] // Same value as COLORSPACE_RGB
    public static DreamValue NativeProc_rgb2num(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if(!bundle.GetArgument(0, "color").TryGetValueAsString(out var color)) {
            Rgb2NumBadColor();
            return DreamValue.Null;
        }

        if (!bundle.GetArgument(1, "space").TryGetValueAsInteger(out var space)) {
            Rgb2NumBadColorspace(bundle.GetArgument(1, "space"));
            return DreamValue.Null;
        }

        if (!ColorHelpers.TryParseColor(color, out var c, defaultAlpha: string.Empty)) {
            Rgb2NumBadColor();
            return DreamValue.Null;
        }

        DreamList list = bundle.ObjectTree.CreateList();

        switch(space) {
            case 0: //rgb
                list.AddValue(new DreamValue(c.RByte));
                list.AddValue(new DreamValue(c.GByte));
                list.AddValue(new DreamValue(c.BByte));
                break;
            case 1: //hsv
                Vector4 hsvcolor = Color.ToHsv(c);
                list.AddValue(new DreamValue(hsvcolor.X * 360));
                list.AddValue(new DreamValue(hsvcolor.Y * 100));
                list.AddValue(new DreamValue(hsvcolor.Z * 100));
                break;
            case 2: //hsl
                Vector4 hslcolor = Color.ToHsl(c);
                list.AddValue(new DreamValue(hslcolor.X * 360));
                list.AddValue(new DreamValue(hslcolor.Y * 100));
                list.AddValue(new DreamValue(hslcolor.Z * 100));
                break;
            //case 3: //hcy
                // TODO Figure out why the chroma for #ca60db is 48 instead of 68
                /*
                Vector4 hcycolor = Color.ToHcy(c);
                list.AddValue(new DreamValue(hcycolor.X * 360));
                list.AddValue(new DreamValue(hcycolor.Y * 100));
                list.AddValue(new DreamValue(hcycolor.Z * 100));
                */
            default:
                Rgb2NumBadColorspace(new DreamValue(space));
                break;
        }

        if (color.Length == 9 || color.Length == 5) {
            list.AddValue(new DreamValue(c.AByte));
        }

        return new DreamValue(list);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Rgb2NumBadColor() {
        throw new Exception("bad color");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Rgb2NumBadColorspace(DreamValue space) {
        throw new NotImplementedException($"Failed to parse colorspace {space}");
    }

    [DreamProc("round")]
    [DreamProcParameter("A", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("B", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_round(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        bundle.GetArgument(0, "A").TryGetValueAsFloat(out var a);

        if (bundle.Arguments.Length == 1) {
            return new DreamValue((float)Math.Floor(a));
        } else {
            bundle.GetArgument(1, "B").TryGetValueAsFloat(out var b);

            return new DreamValue((float)Math.Round(a / b) * b);
        }
    }

    [DreamProc("roll")]
    [DreamProcParameter("ndice", Type = DreamValueTypeFlag.Float | DreamValueTypeFlag.String)]
    [DreamProcParameter("sides", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_roll(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        int dice = 1;
        int sides;
        int modifier = 0;
        if (bundle.Arguments.Length == 1) {
            var arg = bundle.GetArgument(0, "ndice");
            if(arg.TryGetValueAsString(out var diceInput)) {
                string[] diceList = diceInput.Split('d');
                if (diceList.Length < 2) {
                    if (!int.TryParse(diceList[0], out sides))
                        throw new Exception($"Invalid dice value: {diceInput}");
                } else {
                    if (!int.TryParse(diceList[0], out dice))
                        throw new Exception($"Invalid dice value: {diceInput}");

                    if (!int.TryParse(diceList[1], out sides)) {
                        string[] sideList = diceList[1].Split('+');

                        if (!int.TryParse(sideList[0], out sides) || !int.TryParse(sideList[1], out modifier))
                            throw new Exception($"Invalid dice value: {diceInput}");
                    }
                }
            } else if (arg.IsNull) {
                return new DreamValue(1);
            } else if (!arg.TryGetValueAsInteger(out sides)) {
                throw new Exception($"Invalid dice value: {arg}");
            }
        } else if (!bundle.GetArgument(0, "ndice").TryGetValueAsInteger(out dice) || !bundle.GetArgument(1, "sides").TryGetValueAsInteger(out sides)) {
            return new DreamValue(0);
        }

        float total = modifier; // Adds the modifier to start with
        for (int i = 0; i < dice; i++) {
            total += bundle.DreamManager.Random.Next(1, sides + 1);
        }

        return new DreamValue(total);
    }

    [DreamProc("sha1")]
    [DreamProcParameter("T", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamResource)]
    public static DreamValue NativeProc_sha1(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (bundle.Arguments.Length > 1) throw new Exception("sha1() only takes one argument");
        DreamValue arg = bundle.GetArgument(0, "T");
        byte[] bytes;

        if (arg.TryGetValueAsDreamResource(out var resource)) {
            byte[]? filebytes = resource.ResourceData;

            if (filebytes == null) {
                return DreamValue.Null;
            }

            bytes = filebytes;
        } else if (arg.TryGetValueAsString(out string? textdata)) {
            bytes = Encoding.UTF8.GetBytes(textdata);
        } else {
            return DreamValue.Null;
        }

        SHA1 sha1 = SHA1.Create();
        byte[] output = sha1.ComputeHash(bytes);
        //Match BYOND formatting
        string hash = BitConverter.ToString(output).Replace("-", "").ToLower();
        return new DreamValue(hash);
    }

    [DreamProc("shutdown")]
    [DreamProcParameter("Addr", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Natural", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_shutdown(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue addrValue = bundle.GetArgument(0, "Addr");

        if (addrValue.IsNull) {
            IoCManager.Resolve<ITaskManager>().RunOnMainThread(() => {
                IoCManager.Resolve<IBaseServer>().Shutdown("shutdown() was called from DM code");
            });
        } else {
            throw new NotImplementedException();
        }

        return DreamValue.Null;
    }

    [DreamProc("sign")]
    [DreamProcParameter("A", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_sign(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (bundle.Arguments.Length != 1) throw new Exception($"expected 1 argument (found {bundle.Arguments.Length})");
        DreamValue arg = bundle.GetArgument(0, "A");

        // Any non-num returns 0
        if (!arg.TryGetValueAsFloat(out var value)) return new DreamValue(0);

        return value switch {
            0 => new DreamValue(0),
            < 0 => new DreamValue(-1),
            _ => new DreamValue(1)
        };
    }

    [DreamProc("sleep")]
    [DreamProcParameter("Delay", Type = DreamValueTypeFlag.Float)]
    public static async Task<DreamValue> NativeProc_sleep(AsyncNativeProc.State state) {
        state.GetArgument(0, "Delay").TryGetValueAsFloat(out float delay);

        await state.ProcScheduler.CreateDelay(delay);

        return DreamValue.Null;
    }

    [DreamProc("sorttext")]
    [DreamProcParameter("T1", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("T2", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_sorttext(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        string? t2;
        if (!bundle.GetArgument(0, "T1").TryGetValueAsString(out var t1)) {
            if (!bundle.GetArgument(1, "T2").TryGetValueAsString(out _)) {
                return new DreamValue(0);
            }

            return new DreamValue(1);
        } else if (!bundle.GetArgument(1, "T2").TryGetValueAsString(out t2)) {
            return new DreamValue(-1);
        }

        int comparison = string.Compare(t2, t1, StringComparison.OrdinalIgnoreCase);
        int clamped = Math.Max(Math.Min(comparison, 1), -1); //Clamp return value between -1 and 1
        return new DreamValue(clamped);
    }

    [DreamProc("sorttextEx")]
    [DreamProcParameter("T1", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("T2", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_sorttextEx(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        string? t2;
        if (!bundle.GetArgument(0, "T1").TryGetValueAsString(out var t1)) {
            if (!bundle.GetArgument(1, "T2").TryGetValueAsString(out _)) {
                return new DreamValue(0);
            }

            return new DreamValue(1);
        } else if (!bundle.GetArgument(1, "T2").TryGetValueAsString(out t2)) {
            return new DreamValue(-1);
        }

        int comparison = string.Compare(t2, t1, StringComparison.Ordinal);
        int clamped = Math.Max(Math.Min(comparison, 1), -1); //Clamp return value between -1 and 1
        return new DreamValue(clamped);
    }

    [DreamProc("spantext")]
    [DreamProcParameter("Haystack", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Needles", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
    public static DreamValue NativeProc_spantext(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        //if any arguments are bad, return 0
        if (!bundle.GetArgument(0, "Haystack").TryGetValueAsString(out var text) ||
            !bundle.GetArgument(1, "Needles").TryGetValueAsString(out var needles) ||
            !bundle.GetArgument(2, "Start").TryGetValueAsInteger(out var start) ||
            start == 0) { // Start=0 is not valid
            return new DreamValue(0);
        }

        if(start < 0) {
            start = Math.Max(start + text.Length + 1, 1);
        }

        int result = 0;
        while(start <= text.Length) {
            if(text.AsSpan(start - 1, 1).IndexOfAny(needles) > -1) {
                result++;
            } else {
                break;
            }

            start++;
        }

        return new DreamValue(result);
    }

    [DreamProc("spantext_char")]
    [DreamProcParameter("Haystack", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Needles", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
    public static DreamValue NativeProc_spantext_char(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        //if any arguments are bad, return 0
        if (!bundle.GetArgument(0, "Haystack").TryGetValueAsString(out var text) ||
            !bundle.GetArgument(1, "Needles").TryGetValueAsString(out var needles) ||
            !bundle.GetArgument(2, "Start").TryGetValueAsInteger(out var start) ||
            start == 0) { // Start=0 is not valid
            return new DreamValue(0);
        }

        if(start > text.Length) {
            return new DreamValue(0);
        }

        StringInfo textStringInfo = new StringInfo(text);

        if(start < 0) {
            start = Math.Max(start + textStringInfo.LengthInTextElements + 1, 1);
        }

        int result = 0;

        TextElementEnumerator needlesElementEnumerator = StringInfo.GetTextElementEnumerator(needles);
        TextElementEnumerator textElementEnumerator = StringInfo.GetTextElementEnumerator(text, start - 1);

        while(textElementEnumerator.MoveNext()) {
            bool found = false;
            needlesElementEnumerator.Reset();

            //lol O(N*M)
            while (needlesElementEnumerator.MoveNext()) {
                if (textElementEnumerator.Current.Equals(needlesElementEnumerator.Current)) {
                    result++;
                    found = true;
                    break;
                }
            }

            if (!found) {
                break;
            }
        }

        return new DreamValue(result);
    }

    [DreamProc("sound")]
    [DreamProcParameter("file", Type = DreamValueTypeFlag.DreamResource)]
    [DreamProcParameter("repeat", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    [DreamProcParameter("wait", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("channel", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("volume", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_sound(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamObject soundObject = bundle.ObjectTree.CreateObject(bundle.ObjectTree.Sound);
        soundObject.InitSpawn(new DreamProcArguments(bundle.Arguments));
        return new DreamValue(soundObject);
    }

    [DreamProc("splicetext")]
    [DreamProcParameter("Text", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
    [DreamProcParameter("End", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    [DreamProcParameter("Insert", Type = DreamValueTypeFlag.String, DefaultValue = "")]
    public static DreamValue NativeProc_splicetext(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        bundle.GetArgument(0, "Text").TryGetValueAsString(out var text);
        bundle.GetArgument(1, "Start").TryGetValueAsInteger(out var start);
        bundle.GetArgument(2, "End").TryGetValueAsInteger(out var end);
        bundle.GetArgument(3, "Insert").TryGetValueAsString(out var insertText);

        if (text == null)
            if (string.IsNullOrEmpty(insertText))
                return DreamValue.Null;
            else
                return new DreamValue(insertText);
        else if(text == "")
            return new DreamValue(insertText);

        //runtime if start = 0 runtime error: bad text or out of bounds

        if(end == 0 || end > text.Length + 1)
            end = text.Length+1;
        if(start < 0)
            start = Math.Max(start + text.Length + 1, 1);
        if(end < 0)
            end = Math.Min(end + text.Length + 1, text.Length);

        if(start == 0 || start > text.Length || start > end)
            throw new Exception("bad text or out of bounds");

        string result = text.Remove(start - 1, (end-start)).Insert(start - 1, insertText);

        return new DreamValue(result);
    }

    [DreamProc("splicetext_char")]
    [DreamProcParameter("Text", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
    [DreamProcParameter("End", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    [DreamProcParameter("Insert", Type = DreamValueTypeFlag.String, DefaultValue = "")]
    public static DreamValue NativeProc_splicetext_char(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        bundle.GetArgument(0, "Text").TryGetValueAsString(out var text);
        bundle.GetArgument(1, "Start").TryGetValueAsInteger(out var start);
        bundle.GetArgument(2, "End").TryGetValueAsInteger(out var end);
        bundle.GetArgument(3, "Insert").TryGetValueAsString(out var insertText);

        if (text == null) //this is for BYOND compat, and causes the function to ignore start/end if text is null or empty
            if (string.IsNullOrEmpty(insertText))
                return DreamValue.Null;
            else
                return new DreamValue(insertText);
        else if(text == "")
            return new DreamValue(insertText);

        //runtime if start = 0 runtime error: bad text or out of bounds
        StringInfo textElements = new StringInfo(text);
        if(end == 0 || end > textElements.LengthInTextElements + 1)
            end = textElements.LengthInTextElements+1;
        if(start < 0)
            start = Math.Max(start + textElements.LengthInTextElements + 1, 1);
        if(end < 0)
            end = Math.Min(end + textElements.LengthInTextElements + 1, textElements.LengthInTextElements);

        if(start == 0 || start > textElements.LengthInTextElements || start > end)
            throw new Exception("bad text or out of bounds");

        string result = textElements.SubstringByTextElements(0, start - 1);
        result += insertText;
        if(end <= textElements.LengthInTextElements)
            result += textElements.SubstringByTextElements(end - 1);

        return new DreamValue(result);
    }

    [DreamProc("splittext")]
    [DreamProcParameter("Text", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Delimiter", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
    [DreamProcParameter("End", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    [DreamProcParameter("include_delimiters", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_splittext(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "Text").TryGetValueAsString(out var rawtext)) {
            return new DreamValue(bundle.ObjectTree.CreateList());
        }

        if (bundle.GetArgument(2, "Start").TryGetValueAsInteger(out int start))
            start -= 1; //1-indexed
        if (bundle.GetArgument(3, "End").TryGetValueAsInteger(out int end))
            if(end == 0)
                end = rawtext.Length;
            else
                end -= 1; //1-indexed
        bool includeDelimiters = false;
        if(bundle.GetArgument(4, "include_delimiters").TryGetValueAsInteger(out var includeDelimitersInt))
            includeDelimiters = includeDelimitersInt != 0; //idk why BYOND doesn't just use truthiness, but it doesn't, so...

        string text = rawtext;
        if (start > 0 || end < rawtext.Length)
            text = rawtext[Math.Max(start, 0)..Math.Min(end, text.Length)];

        var delim = bundle.GetArgument(1, "Delimiter"); //can either be a regex or string

        if (delim.TryGetValueAsDreamObject<DreamObjectRegex>(out var regexObject)) {
            if(includeDelimiters) {
                var values = new List<string>();
                int pos = 0;
                foreach (Match m in regexObject.Regex.Matches(text)) {
                    values.Add(text.Substring(pos, m.Index - pos));
                    values.Add(m.Value);
                    pos = m.Index + m.Length;
                }

                values.Add(text.Substring(pos));
                if (start > 0)
                    values[0] = rawtext.Substring(0, start) + values[0];
                if (end < rawtext.Length)
                    values[^1] += rawtext.Substring(end, rawtext.Length - end);
                return new DreamValue(bundle.ObjectTree.CreateList(values.ToArray()));
            } else {
                var values = regexObject.Regex.Split(text);
                if (start > 0)
                    values[0] = rawtext.Substring(0, start) + values[0];
                if (end < rawtext.Length)
                    values[^1] += rawtext.Substring(end, rawtext.Length - end);
                return new DreamValue(bundle.ObjectTree.CreateList(values));
            }
        } else if (delim.TryGetValueAsString(out var delimiter)) {
            string[] splitText;
            if(includeDelimiters) {
                //basically split on delimeter, and then add the delimiter back in after each split (except the last one)
                splitText= text.Split(delimiter);
                string[] longerSplitText = new string[splitText.Length * 2 - 1];
                for(int i = 0; i < splitText.Length; i++) {
                    longerSplitText[i * 2] = splitText[i];
                    if(i < splitText.Length - 1)
                        longerSplitText[i * 2 + 1] = delimiter;
                }

                splitText = longerSplitText;
            } else {
                splitText = text.Split(delimiter);
            }

            if (start > 0)
                splitText[0] = rawtext.Substring(0, start) + splitText[0];
            if (end < rawtext.Length)
                splitText[^1] += rawtext.Substring(end, rawtext.Length - end);
            return new DreamValue(bundle.ObjectTree.CreateList(splitText));
        } else {
            return new DreamValue(bundle.ObjectTree.CreateList());
        }
    }

    private static void OutputToStatPanel(DreamManager dreamManager, DreamConnection connection, DreamValue name, DreamValue value) {
        if (name.IsNull && value.TryGetValueAsDreamList(out var list)) {
            foreach (var item in list.EnumerateValues())
                OutputToStatPanel(dreamManager, connection, name, item);
        } else {
            string nameStr = name.Stringify();
            string? atomRef = null;
            if (value.TryGetValueAsDreamObject<DreamObjectAtom>(out _)) // Atoms are clickable
                atomRef = dreamManager.CreateRef(value);

            connection.AddStatPanelLine(nameStr, value.Stringify(), atomRef);
        }
    }

    [DreamProc("stat")]
    [DreamProcParameter("Name")]
    [DreamProcParameter("Value")]
    public static DreamValue NativeProc_stat(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue name = bundle.GetArgument(0, "Name");
        DreamValue value = bundle.GetArgument(1, "Value");

        if (usr is DreamObjectMob { Connection: {} usrConnection })
            OutputToStatPanel(bundle.DreamManager, usrConnection, name, value);

        return DreamValue.Null;
    }

    [DreamProc("statpanel")]
    [DreamProcParameter("Panel", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("Name")]
    [DreamProcParameter("Value")]
    public static DreamValue NativeProc_statpanel(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        string panel = bundle.GetArgument(0, "Panel").GetValueAsString();
        DreamValue name = bundle.GetArgument(1, "Name");
        DreamValue value = bundle.GetArgument(2, "Value");

        if (usr is DreamObjectMob { Connection: {} connection }) {
            connection.SetOutputStatPanel(panel);
            if (!name.IsNull || !value.IsNull) {
                OutputToStatPanel(bundle.DreamManager, connection, name, value);
            }

            return new DreamValue(connection.SelectedStatPanel == panel ? 1 : 0);
        }

        return DreamValue.False;
    }

    [DreamProc("text2ascii")]
    [DreamProcParameter("T", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("pos", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
    public static DreamValue NativeProc_text2ascii(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if(!bundle.GetArgument(0, "T").TryGetValueAsString(out var text)) {
            return new DreamValue(0);
        }

        bundle.GetArgument(1, "pos").TryGetValueAsInteger(out var pos); //1-indexed
        if (pos == 0) pos = 1; //0 is same as 1
        else if (pos < 0) pos += text.Length + 1; //Wraps around

        if (pos > text.Length || pos < 1) {
            return new DreamValue(0);
        } else {
            return new DreamValue((int)text[pos - 1]);
        }
    }

    [DreamProc("text2ascii_char")]
    [DreamProcParameter("T", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("pos", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
    public static DreamValue NativeProc_text2ascii_char(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "T").TryGetValueAsString(out var text)) {
            return new DreamValue(0);
        }

        StringInfo textElements = new StringInfo(text);

        bundle.GetArgument(1, "pos").TryGetValueAsInteger(out var pos); //1-indexed
        if (pos == 0) pos = 1; //0 is same as 1
        else if (pos < 0) pos += textElements.LengthInTextElements + 1; //Wraps around

        if (pos > textElements.LengthInTextElements || pos < 1) {
            return new DreamValue(0);
        } else {
            //practically identical to (our) text2ascii but more explicit about subchar indexing
            return new DreamValue((int)textElements.SubstringByTextElements(pos - 1, 1)[0]);
        }
    }

    [DreamProc("text2file")]
    [DreamProcParameter("Text", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("File", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_text2file(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "Text").TryGetValueAsString(out var text)) {
            text = string.Empty;
        }

        if (!bundle.GetArgument(1, "File").TryGetValueAsString(out var file)) {
            return new DreamValue(0);
        }

        return new DreamValue(bundle.ResourceManager.SaveTextToFile(file, text) ? 1 : 0);
    }

    [DreamProc("text2num")]
    [DreamProcParameter("T", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.Float | DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("radix", Type = DreamValueTypeFlag.Float, DefaultValue = 10)]
    public static DreamValue NativeProc_text2num(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue value = bundle.GetArgument(0, "T");

        if (value.TryGetValueAsString(out string? valueAsString)) {
            bundle.GetArgument(1, "radix").TryGetValueAsInteger(out var radix);
            valueAsString = valueAsString.Trim();

            double? valueAsDouble = DreamProcNativeHelpers.StringToDouble(valueAsString, radix);
            if (valueAsDouble.HasValue)
                return new DreamValue(valueAsDouble.Value);
            return DreamValue.Null;
        } else if (value.Type == DreamValueType.Float) {
            return value;
        } else if (value.IsNull) {
            return DreamValue.Null;
        } else {
            throw new Exception($"Invalid argument to text2num: {value}");
        }
    }

    [DreamProc("text2path")]
    [DreamProcParameter("T", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_text2path(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "T").TryGetValueAsString(out var path) || string.IsNullOrWhiteSpace(path)) {
            return DreamValue.Null;
        }

        bool isVerb = false;

        int procElementIndex = path.IndexOf("/proc/", StringComparison.Ordinal);
        if (procElementIndex == -1) {
            procElementIndex = path.IndexOf("/verb/", StringComparison.Ordinal);
            if (procElementIndex != -1)
                isVerb = true;
        }

        bool isProcPath = procElementIndex != -1;

        string? procName = null;
        if (isProcPath) {
            procName = path.Substring(path.LastIndexOf('/') + 1);

            if (procElementIndex == 0) { // global procs
                if (bundle.ObjectTree.TryGetGlobalProc(procName, out var globalProc) && globalProc.IsVerb == isVerb)
                    return new DreamValue(globalProc);
                else
                    return DreamValue.Null;
            }
        }

        string typePath = isProcPath ? path.Substring(0, procElementIndex) : path;

        if (!bundle.ObjectTree.TryGetTreeEntry(typePath, out var type) || type == bundle.ObjectTree.Root)
            return DreamValue.Null;

        if (!isProcPath || procName == null)
            return new DreamValue(type);

        // not using TryGetProc because that includes overrides
        if (type.ObjectDefinition.Procs.TryGetValue(procName, out int procId)) {
            DreamProc proc = bundle.ObjectTree.Procs[procId];
            if (proc.IsVerb == isVerb)
                return new DreamValue(proc);
        }

        return DreamValue.Null;
    }

    [DreamProc("time2text")]
    [DreamProcParameter("timestamp", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("format", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("timezone", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_time2text(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        bool hasTimezoneOffset = bundle.GetArgument(2, "timezone").TryGetValueAsFloat(out float timezoneOffset);

        if (!bundle.GetArgument(0, "timestamp").TryGetValueAsFloat(out var timestamp)) {
            // TODO This copes with nulls and is a sane default, but BYOND has weird returns for strings and stuff
            bundle.DreamManager.WorldInstance.GetVariable("timeofday").TryGetValueAsFloat(out timestamp);
        }

        if (!bundle.GetArgument(1, "format").TryGetValueAsString(out var format)) {
            format = "DDD MMM DD hh:mm:ss YYYY";
        }

        long ticks = (long)(timestamp * TimeSpan.TicksPerSecond / 10);

        // The DM reference says this is 0-864000. That's wrong, it's actually a 7-day range instead of 1
        if (timestamp >= 0 && timestamp < 864000*7) {
            ticks += new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day).Ticks;
        } else {
            // Offset from January 1st, 2020
            ticks += new DateTime(2000, 1, 1).Ticks;
        }

        DateTime time = new DateTime(ticks, DateTimeKind.Utc);
        if (hasTimezoneOffset) {
            time = time.AddHours(timezoneOffset);
        } else {
            time = time.ToLocalTime();
        }

        format = format.Replace("YYYY", time.Year.ToString());
        format = format.Replace("YY", (time.Year % 100).ToString("00"));
        format = format.Replace("Month", CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(time.Month));
        format = format.Replace("MMM", CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(time.Month));
        format = format.Replace("MM", time.Month.ToString("00"));
        format = format.Replace("Day", CultureInfo.InvariantCulture.DateTimeFormat.GetDayName(time.DayOfWeek));
        format = format.Replace("DDD", CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedDayName(time.DayOfWeek));
        format = format.Replace("DD", time.Day.ToString("00"));
        format = format.Replace("hh", time.Hour.ToString("00"));
        format = format.Replace("mm", time.Minute.ToString("00"));
        format = format.Replace("ss", time.Second.ToString("00"));
        return new DreamValue(format);
    }

    [DreamProc("trimtext")]
    [DreamProcParameter("Text", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_trimtext(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        return bundle.GetArgument(0, "Text").TryGetValueAsString(out var val) ? new DreamValue(val.Trim()) : DreamValue.Null;
    }

    [DreamProc("trunc")]
    [DreamProcParameter("n", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_trunc(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue arg = bundle.GetArgument(0, "n");
        if (arg.TryGetValueAsFloat(out float floatNum))
            return new DreamValue(MathF.Truncate(floatNum));

        return new DreamValue(0);
    }

    /// <summary> Global turn() proc </summary>
    /// <remarks> Take note that this turn proc is a counterclockwise rotation unlike the rest </remarks>
    [DreamProc("turn")]
    [DreamProcParameter("Dir", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("Angle", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_turn(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue dirArg = bundle.GetArgument(0, "dir");
        DreamValue angleArg = bundle.GetArgument(1, "angle");

        // Handle an invalid angle, defaults to 0
        if (!angleArg.TryGetValueAsFloat(out float angle)) {
            angle = 0;
        }

        // If Dir is actually an icon, call /icon.Turn
        if (dirArg.TryGetValueAsDreamObject<DreamObjectIcon>(out var icon)) {
            // Clone icon here since it's specified to return a new one
            DreamObjectIcon clonedIcon = icon.Clone();

            DreamProcNativeIcon._NativeProc_TurnInternal(clonedIcon, angle);
            return new(clonedIcon);
        }

        // If Dir is actually a matrix, call /matrix.Turn
        if (dirArg.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrix)) {
            // Clone matrix here since it's specified to return a new one
            var clonedMatrix = DreamObjectMatrix.MatrixClone(bundle.ObjectTree, matrix);

            return DreamProcNativeMatrix._NativeProc_TurnInternal(bundle.ObjectTree, clonedMatrix, angle);
        }

        // If Dir is not an integer, throw
        if (!dirArg.TryGetValueAsInteger(out int possibleDir)) {
            throw new Exception("expected icon, matrix or integer");
        }

        AtomDirection dir = (AtomDirection)possibleDir;
        float? dirAngle = dir switch {
                AtomDirection.East => 0,
                AtomDirection.Northeast => 45,
                AtomDirection.North => 90,
                AtomDirection.Northwest => 135,
                AtomDirection.West => 180,
                AtomDirection.Southwest => 225,
                AtomDirection.South => 270,
                AtomDirection.Southeast => 315,
                _ => null
        };

        // Is the dir invalid?
        if (dirAngle == null) {
            // If Dir is invalid and angle is zero, 0 is returned
            if (angle == 0) {
                return new DreamValue(0);
            }

            // Otherwise, it returns a random direction
            // Can't just select a random value from AtomDirection since that contains AtomDirection.None
            var selectedDirIndex = bundle.DreamManager.Random.Next(8);
            var selectedDir = selectedDirIndex switch {
                0 => AtomDirection.North,
                1 => AtomDirection.South,
                2 => AtomDirection.East,
                3 => AtomDirection.West,
                4 => AtomDirection.Northeast,
                5 => AtomDirection.Southeast,
                6 => AtomDirection.Southwest,
                7 => AtomDirection.Northwest,
                _ => throw new UnreachableException()
            };

            return new((int)selectedDir);
        }

        dirAngle += MathF.Truncate(angle / 45) * 45;
        dirAngle %= 360;

        if (dirAngle < 0) {
            dirAngle = 360 + dirAngle;
        }

        AtomDirection toReturn = dirAngle switch {
                45 => AtomDirection.Northeast,
                90 => AtomDirection.North,
                135 => AtomDirection.Northwest,
                180 => AtomDirection.West,
                225 => AtomDirection.Southwest,
                270 => AtomDirection.South,
                315 => AtomDirection.Southeast,
                _ => AtomDirection.East
        };
        return new DreamValue((int)toReturn);
    }

    [DreamProc("typesof")]
    [DreamProcParameter("Item1", Type = DreamValueTypeFlag.DreamType | DreamValueTypeFlag.DreamObject | DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_typesof(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamList list = bundle.ObjectTree.CreateList(bundle.Arguments.Length); // Assume every arg will add at least one type

        foreach (var typeValue in bundle.Arguments) {
            IEnumerable<int>? addingProcs = null;

            if (!typeValue.TryGetValueAsType(out var type)) {
                if (typeValue.TryGetValueAsDreamObject(out var typeObj)) {
                    if (typeObj is null or DreamList) // typesof() ignores nulls and lists
                        continue;

                    type = typeObj.ObjectDefinition.TreeEntry;
                } else if (typeValue.TryGetValueAsString(out var typeString)) {
                    if (typeString.EndsWith("/proc")) {
                        type = bundle.ObjectTree.GetTreeEntry(typeString.Substring(0, typeString.Length - 5));
                        addingProcs = type.ObjectDefinition.Procs.Values;
                    } else if (typeString.EndsWith("/verb")) {
                        type = bundle.ObjectTree.GetTreeEntry(typeString.Substring(0, typeString.Length - 5));
                        addingProcs = type.ObjectDefinition.Verbs?.Values ?? Enumerable.Empty<int>();
                    } else {
                        type = bundle.ObjectTree.GetTreeEntry(typeString);
                    }
                } else {
                    continue;
                }
            }

            if (addingProcs != null) {
                foreach (var procId in addingProcs) {
                    list.AddValue(new DreamValue(bundle.ObjectTree.Procs[procId]));
                }
            } else {
                var descendants = bundle.ObjectTree.GetAllDescendants(type);

                foreach (var descendant in descendants) {
                    list.AddValue(new DreamValue(descendant));
                }
            }
        }

        return new DreamValue(list);
    }

    [DreamProc("uppertext")]
    [DreamProcParameter("T", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_uppertext(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var arg = bundle.GetArgument(0, "T");
        if (!arg.TryGetValueAsString(out var text)) {
            return arg;
        }

        return new DreamValue(text.ToUpper());
    }

    [DreamProc("url_decode")]
    [DreamProcParameter("UrlText", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_url_decode(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "UrlText").TryGetValueAsString(out var urlText)) {
            return new DreamValue("");
        }

        return new DreamValue(HttpUtility.UrlDecode(urlText));
    }

    [DreamProc("url_encode")]
    [DreamProcParameter("PlainText", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("format", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_url_encode(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        string plainText = bundle.GetArgument(0, "PlainText").Stringify();
        bundle.GetArgument(1, "format").TryGetValueAsInteger(out var format);
        if (format != 0)
            throw new NotImplementedException("Only format 0 is supported");

        return new DreamValue(HttpUtility.UrlEncode(plainText));
    }

    [DreamProc("values_cut_over")]
    [DreamProcParameter("Alist", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Max", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("inclusive", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_values_cut_over(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (bundle.Arguments.Length < 2 || bundle.Arguments.Length > 3) throw new Exception($"expected 2-3 arguments (found {bundle.Arguments.Length})");

        DreamValue argList = bundle.GetArgument(0, "Alist");
        DreamValue argMax = bundle.GetArgument(1, "Max");
        DreamValue argInclusive = bundle.GetArgument(2, "inclusive");

        return values_cut_helper(argList, argMax, argInclusive, false);
    }

    [DreamProc("values_cut_under")]
    [DreamProcParameter("Alist", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Min", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("inclusive", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_values_cut_under(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (bundle.Arguments.Length < 2 || bundle.Arguments.Length > 3) throw new Exception($"expected 2-3 arguments (found {bundle.Arguments.Length})");

        DreamValue argList = bundle.GetArgument(0, "Alist");
        DreamValue argMin = bundle.GetArgument(1, "Min");
        DreamValue argInclusive = bundle.GetArgument(2, "inclusive");

        return values_cut_helper(argList, argMin, argInclusive, true);
    }

    private static DreamValue values_cut_helper(DreamValue argList, DreamValue argMin, DreamValue argInclusive, bool under) {
        // BYOND explicitly doesn't check for any truthy value
        bool inclusive = argInclusive.TryGetValueAsFloat(out var inclusiveValue) && inclusiveValue >= 1;

        var cutCount = 0; // number of values cut from the list
        var min = argMin.UnsafeGetValueAsFloat();

        if (argList.TryGetValueAsIDreamList(out var list)) {
            if (!list.IsAssociative) {
                cutCount = list.GetLength();
                list.Cut();
                return new DreamValue(cutCount);
            }

            var values = list.GetValues();
            var assocValues = list.GetAssociativeValues();

            // Nuke any keys without values
            if (values.Count != assocValues.Count) {
                for (var index = 0; index < values.Count; index++) {
                    var val = values[index];
                    if (!assocValues.ContainsKey(val)) {
                        cutCount += 1;
                        index -= 1;
                        list.RemoveValue(val);
                    }
                }
            }

            foreach (var (key,value) in assocValues) {
                if (value.TryGetValueAsFloat(out var valFloat)) {
                    switch (inclusive) {
                        case true when under && valFloat <= min:
                        case true when !under && valFloat >= min:
                        case false when under && valFloat < min:
                        case false when !under && valFloat > min:
                            list.RemoveValue(key);
                            cutCount += 1;
                            break;
                    }
                } else {
                    list.RemoveValue(key); // Keys without numeric values seem to always be removed
                    cutCount += 1;
                }
            }
        }

        return new DreamValue(cutCount);
    }

    [DreamProc("values_dot")]
    [DreamProcParameter("A", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("B", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_values_dot(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (bundle.Arguments.Length != 2) throw new Exception("expected 2 arguments");

        DreamValue argA = bundle.GetArgument(0, "A");
        DreamValue argB = bundle.GetArgument(1, "B");

        float sum = 0; // Default return is 0 for invalid args

        if (argA.TryGetValueAsIDreamList(out var listA) && listA.IsAssociative && argB.TryGetValueAsIDreamList(out var listB) && listB.IsAssociative) {
            var aValues = listA.GetAssociativeValues();
            var bValues = listB.GetAssociativeValues();

            // sum += valueA * valueB
            // for each assoc value whose key exists in both lists
            // and when both assoc values are floats
            foreach (var (key,value) in aValues) {
                if (value.TryGetValueAsFloat(out var aFloat) && bValues.TryGetValue(key, out var bVal) &&
                    bVal.TryGetValueAsFloat(out var bFloat)) {
                    sum += (aFloat * bFloat);
                }
            }
        }

        return new DreamValue(sum);
    }

    [DreamProc("values_product")]
    [DreamProcParameter("Alist", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_values_product(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (bundle.Arguments.Length != 1) throw new Exception("expected 1 argument");

        DreamValue arg = bundle.GetArgument(0, "Alist");

        float product = 1; // Default return is 1 for invalid args

        if (arg.TryGetValueAsIDreamList(out var list) && list.IsAssociative) {
            var assocValues = list.GetAssociativeValues();
            foreach (var (_,value) in assocValues) {
                if(value.TryGetValueAsFloat(out var valFloat)) product *= valFloat;
            }
        }

        return new DreamValue(product);
    }

    [DreamProc("values_sum")]
    [DreamProcParameter("Alist", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_values_sum(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (bundle.Arguments.Length != 1) throw new Exception("expected 1 argument");

        DreamValue arg = bundle.GetArgument(0, "Alist");

        float sum = 0; // Default return is 0 for invalid args

        if (arg.TryGetValueAsIDreamList(out var list) && list.IsAssociative) {
            var assocValues = list.GetAssociativeValues();
            foreach (var (_,value) in assocValues) {
                if(value.TryGetValueAsFloat(out var valFloat)) sum += valFloat;
            }
        }

        return new DreamValue(sum);
    }

    [DreamProc("view")]
    [DreamProcParameter("Dist", Type = DreamValueTypeFlag.Float, DefaultValue = 5)]
    [DreamProcParameter("Center", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_view(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamList view = bundle.ObjectTree.CreateList();

        (DreamObjectAtom? center, ViewRange range) = DreamProcNativeHelpers.ResolveViewArguments(bundle.DreamManager, usr as DreamObjectAtom, bundle.Arguments);
        if (center is null)
            return new(view);

        if (center.TryGetVariable("contents", out var centerContents) && centerContents.TryGetValueAsDreamList(out var centerContentsList)) {
            foreach (var item in centerContentsList.EnumerateValues()) {
                view.AddValue(item);
            }
        }

        // Center gets included during the walk through the tiles

        var eyePos = bundle.AtomManager.GetAtomPosition(center);
        var viewData = DreamProcNativeHelpers.CollectViewData(bundle.AtomManager, bundle.MapManager, eyePos, range);

        ViewAlgorithm.CalculateVisibility(viewData);

        foreach (var tile in DreamProcNativeHelpers.MakeViewSpiral(viewData, true)) {
            if (tile == null || tile.IsVisible == false)
                continue;
            if (!bundle.MapManager.TryGetCellAt((eyePos.X + tile.DeltaX, eyePos.Y + tile.DeltaY), eyePos.Z, out var cell))
                continue;

            view.AddValue(new(cell.Turf));
            foreach (var movable in cell.Movables) {
                view.AddValue(new(movable));
            }
        }

        return new DreamValue(view);
    }

    [DreamProc("viewers")]
    [DreamProcParameter("Depth", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("Center", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_viewers(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) { //TODO: Change depending on center
        return DreamProcNativeHelpers.HandleViewersHearers(bundle, usr, false);
    }

    [DreamProc("walk")]
    [DreamProcParameter("Ref", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Dir", Type = DreamValueTypeFlag.Float)]
    [DreamProcParameter("Lag", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    [DreamProcParameter("Speed", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_walk(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "Ref").TryGetValueAsDreamObject<DreamObjectMovable>(out var refAtom))
            return DreamValue.Null;

        // Per the ref, calling walk(Ref, 0) halts walking
        if (bundle.GetArgument(1, "Dir").TryGetValueAsInteger(out var dir) && dir == 0) {
            bundle.WalkManager.StopWalks(refAtom);
            return DreamValue.Null;
        }

        bundle.GetArgument(2, "Lag").TryGetValueAsInteger(out var lag);
        bundle.GetArgument(3, "Speed").TryGetValueAsInteger(out var speed);

        bundle.WalkManager.StartWalk(refAtom, dir, lag, speed);

        return DreamValue.Null;
    }

    [DreamProc("walk_rand")]
    [DreamProcParameter("Ref", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Lag", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    [DreamProcParameter("Speed", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_walk_rand(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "Ref").TryGetValueAsDreamObject<DreamObjectMovable>(out var refAtom))
            return DreamValue.Null;

        bundle.GetArgument(1, "Lag").TryGetValueAsInteger(out var lag);
        bundle.GetArgument(2, "Speed").TryGetValueAsInteger(out var speed);

        bundle.WalkManager.StartWalkRand(refAtom, lag, speed);

        return DreamValue.Null;
    }

    [DreamProc("walk_towards")]
    [DreamProcParameter("Ref", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Trg", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Lag", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    [DreamProcParameter("Speed", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_walk_towards(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "Ref").TryGetValueAsDreamObject<DreamObjectMovable>(out var refAtom))
            return DreamValue.Null;

        if (!bundle.GetArgument(1, "Trg").TryGetValueAsDreamObject<DreamObjectAtom>(out var trgAtom)) {
            bundle.WalkManager.StopWalks(refAtom);
            return DreamValue.Null;
        }

        bundle.GetArgument(2, "Lag").TryGetValueAsInteger(out var lag);
        bundle.GetArgument(3, "Speed").TryGetValueAsInteger(out var speed);

        bundle.WalkManager.StartWalkTowards(refAtom, trgAtom, lag, speed);
        return DreamValue.Null;
    }

    [DreamProc("walk_to")]
    [DreamProcParameter("Ref", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Trg", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Min", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    [DreamProcParameter("Lag", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    [DreamProcParameter("Speed", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
    public static DreamValue NativeProc_walk_to(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if (!bundle.GetArgument(0, "Ref").TryGetValueAsDreamObject<DreamObjectMovable>(out var refAtom))
            return DreamValue.Null;

        if (!bundle.GetArgument(1, "Trg").TryGetValueAsDreamObject<DreamObjectAtom>(out var trgAtom)) {
            bundle.WalkManager.StopWalks(refAtom);
            return DreamValue.Null;
        }

        bundle.GetArgument(2, "Min").TryGetValueAsInteger(out var min);
        bundle.GetArgument(3, "Lag").TryGetValueAsInteger(out var lag);
        bundle.GetArgument(4, "Speed").TryGetValueAsInteger(out var speed);

        bundle.WalkManager.StartWalkTo(refAtom, trgAtom, min, lag, speed);
        return DreamValue.Null;
    }

    [DreamProc("winclone")]
    [DreamProcParameter("player", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("window_name", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("clone_name", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_winclone(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if(!bundle.GetArgument(1, "window_name").TryGetValueAsString(out var windowName))
            return DreamValue.Null;
        if(!bundle.GetArgument(2, "clone_name").TryGetValueAsString(out var cloneName))
            return DreamValue.Null;

        DreamValue player = bundle.GetArgument(0, "player");

        DreamConnection? connection;

        if (player.TryGetValueAsDreamObject<DreamObjectMob>(out var mob)) {
            connection = mob.Connection;
        } else if (player.TryGetValueAsDreamObject<DreamObjectClient>(out var client)) {
            connection = client.Connection;
        } else {
            throw new ArgumentException($"Invalid \"player\" argument {player}");
        }

        connection?.WinClone(windowName, cloneName);
        return DreamValue.Null;
    }

    [DreamProc("winexists")]
    [DreamProcParameter("player", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("control_id", Type = DreamValueTypeFlag.String)]
    public static async Task<DreamValue> NativeProc_winexists(AsyncNativeProc.State state) {
        DreamValue player = state.GetArgument(0, "player");
        if (!state.GetArgument(1, "control_id").TryGetValueAsString(out var controlId)) {
            return new DreamValue("");
        }

        DreamConnection? connection = null;
        if (player.TryGetValueAsDreamObject<DreamObjectMob>(out var mob)) {
            connection = mob.Connection;
        } else if (player.TryGetValueAsDreamObject<DreamObjectClient>(out var client)) {
            connection = client.Connection;
        }

        if (connection == null) {
            throw new Exception($"Invalid client {player}");
        }

        return await connection.WinExists(controlId);
    }

    [DreamProc("winget")]
    [DreamProcParameter("player", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("control_id", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("params", Type = DreamValueTypeFlag.String)]
    public static async Task<DreamValue> NativeProc_winget(AsyncNativeProc.State state) {
        DreamValue player = state.GetArgument(0, "player");
        state.GetArgument(1, "control_id").TryGetValueAsString(out var controlId);
        if (!state.GetArgument(2, "params").TryGetValueAsString(out var paramsValue))
            return new(string.Empty);

        DreamConnection? connection = null;
        if (player.TryGetValueAsDreamObject<DreamObjectMob>(out var mob)) {
            connection = mob.Connection;
        } else if (player.TryGetValueAsDreamObject<DreamObjectClient>(out var client)) {
            connection = client.Connection;
        }

        if (connection == null) {
            throw new Exception($"Invalid client {player}");
        }

        if (string.IsNullOrEmpty(controlId) && paramsValue == "hwmode") {
            // Don't even bother querying the client, we don't have a non-hwmode
            return new("true");
        }

        return await connection.WinGet(controlId, paramsValue);
    }

    [DreamProc("winset")]
    [DreamProcParameter("player", Type = DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("control_id", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("params", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_winset(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        DreamValue player = bundle.GetArgument(0, "player");
        DreamValue controlId = bundle.GetArgument(1, "control_id");
        DreamValue winsetParams = bundle.GetArgument(2, "params");
        string? winsetControlId = (!controlId.IsNull) ? controlId.GetValueAsString() : null;

        DreamConnection? connection = null;
        if (player.TryGetValueAsDreamObject<DreamObjectMob>(out var mob)) {
            connection = mob.Connection;
        } else if (player.TryGetValueAsDreamObject<DreamObjectClient>(out var client)) {
            connection = client.Connection;
        }

        if (connection == null) {
            throw new ArgumentException($"Invalid \"player\" argument {player}");
        }

        if (!winsetParams.TryGetValueAsString(out var winsetParamsStr)) {
            if (winsetParams.TryGetValueAsIDreamList(out var winsetParamsList)) {
                winsetParamsStr = List2Params(winsetParamsList);
            } else {
                throw new ArgumentException($"Invalid \"params\" argument {winsetParams}");
            }
        }

        connection.WinSet(winsetControlId, winsetParamsStr);
        return DreamValue.Null;
    }
}
