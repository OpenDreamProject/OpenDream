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
using OpenDreamRuntime.Objects.Types;
using DreamValueType = OpenDreamRuntime.DreamValue.DreamValueType;
using DreamValueTypeFlag = OpenDreamRuntime.DreamValue.DreamValueTypeFlag;
using Robust.Server;
using Robust.Shared.Asynchronous;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace OpenDreamRuntime.Procs.Native {
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

        [DreamProc("animate")]
        [DreamProcParameter("Object", Type = DreamValueTypeFlag.DreamObject)]
        [DreamProcParameter("time", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("loop", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("easing", Type = DreamValueTypeFlag.String)]
        [DreamProcParameter("flags", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("pixel_x", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("pixel_y", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("pixel_z", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("maptext_x", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("maptext_y", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("dir", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("alpha", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("transform", Type = DreamValueTypeFlag.DreamObject)]
        [DreamProcParameter("color", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamObject)]
        public static DreamValue NativeProc_animate(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            // TODO: Leaving out the Object var adds a new step to the previous animation
            if (!bundle.GetArgument(0, "Object").TryGetValueAsDreamObject<DreamObjectAtom>(out var obj))
                return DreamValue.Null;
            // TODO: Is this the correct behavior for invalid time?
            if (!bundle.GetArgument(1, "time").TryGetValueAsFloat(out float time))
                return DreamValue.Null;
            if (bundle.GetArgument(2, "loop").TryGetValueAsInteger(out int loop))
                return DreamValue.Null; // TODO: Looped animations are not implemented
            if (bundle.GetArgument(3, "easing").TryGetValueAsInteger(out int easing) && easing != 1) // LINEAR_EASING only
                return DreamValue.Null; // TODO: Non-linear animation easing types are not implemented"
            if (bundle.GetArgument(4, "flags").TryGetValueAsInteger(out int flags) && flags != 0)
                return DreamValue.Null; // TODO: Animation flags are not implemented

            var pixelX = bundle.GetArgument(5, "pixel_x");
            var pixelY = bundle.GetArgument(6, "pixel_y");
            var dir = bundle.GetArgument(7, "dir");

            bundle.AtomManager.AnimateAppearance(obj, TimeSpan.FromMilliseconds(time * 100), appearance => {
                if (!pixelX.IsNull) {
                    obj.SetVariableValue("pixel_x", pixelX);
                    pixelX.TryGetValueAsInteger(out appearance.PixelOffset.X);
                }

                if (!pixelY.IsNull) {
                    obj.SetVariableValue("pixel_y", pixelY);
                    pixelY.TryGetValueAsInteger(out appearance.PixelOffset.Y);
                }

                if (!dir.IsNull) {
                    obj.SetVariableValue("dir", dir);
                    dir.TryGetValueAsInteger(out int dirValue);
                    appearance.Direction = (AtomDirection)dirValue;
                }

                // TODO: Rest of the animatable vars
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

        [DreamProc("block")]
        [DreamProcParameter("Start", Type = DreamValueTypeFlag.DreamObject | DreamValueTypeFlag.Float)]
        [DreamProcParameter("End", Type = DreamValueTypeFlag.DreamObject | DreamValueTypeFlag.Float)]
        [DreamProcParameter("StartZ", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("EndX", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("EndY", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("EndZ", Type = DreamValueTypeFlag.Float)]
        public static DreamValue NativeProc_block(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            (int X, int Y, int Z) startPos;
            (int X, int Y, int Z) endPos;
            if (bundle.GetArgument(0, "Start").TryGetValueAsDreamObject<DreamObjectTurf>(out var startT)) {
                if (!bundle.GetArgument(1, "End").TryGetValueAsDreamObject<DreamObjectTurf>(out var endT))
                    return new DreamValue(bundle.ObjectTree.CreateList());

                startPos = (startT.X, startT.Y, startT.Z);
                endPos = (endT.X, endT.Y, endT.Z);
            } else {
                // Need to check that we weren't passed something like block("cat", turf) which should return an empty list
                if (bundle.GetArgument(1, "End").TryGetValueAsDreamObject<DreamObjectTurf>(out _)) {
                    return new DreamValue(bundle.ObjectTree.CreateList());
                }
                // coordinate-style
                if (!bundle.GetArgument(0, "Start").TryGetValueAsInteger(out startPos.X)) {
                    startPos.X = 1; // First three default to 1 when passed null or invalid
                }
                if (!bundle.GetArgument(1, "End").TryGetValueAsInteger(out startPos.Y)) {
                    startPos.Y = 1;
                }
                if (!bundle.GetArgument(2, "StartZ").TryGetValueAsInteger(out startPos.Z)) {
                    startPos.Z = 1;
                }
                if (!bundle.GetArgument(3, "EndX").TryGetValueAsInteger(out endPos.X)) {
                    endPos.X = startPos.X; // Last three default to the start coords if null or invalid
                }
                if (!bundle.GetArgument(4, "EndY").TryGetValueAsInteger(out endPos.Y)) {
                    endPos.Y = startPos.Y;
                }
                if (!bundle.GetArgument(5, "EndZ").TryGetValueAsInteger(out endPos.Z)) {
                    endPos.Z = startPos.Z;
                }
            }

            int startX = Math.Min(startPos.X, endPos.X);
            int startY = Math.Min(startPos.Y, endPos.Y);
            int startZ = Math.Min(startPos.Z, endPos.Z);
            int endX = Math.Max(startPos.X, endPos.X);
            int endY = Math.Max(startPos.Y, endPos.Y);
            int endZ = Math.Max(startPos.Z, endPos.Z);

            DreamList turfs = bundle.ObjectTree.CreateList((endX - startX + 1) * (endY - startY + 1) * (endZ - startZ + 1));

            // Collected in z-y-x order
            for (int z = startZ; z <= endZ; z++) {
                for (int y = startY; y <= endY; y++) {
                    for (int x = startX; x <= endX; x++) {
                        if (bundle.MapManager.TryGetTurfAt((x, y), z, out var turf)) {
                            turfs.AddValue(new DreamValue(turf));
                        }
                    }
                }
            }

            return new DreamValue(turfs);
        }

        [DreamProc("ceil")]
        [DreamProcParameter("A", Type = DreamValueTypeFlag.Float)]
        public static DreamValue NativeProc_ceil(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            bundle.GetArgument(0, "A").TryGetValueAsFloat(out float floatnum);

            return new DreamValue(MathF.Ceiling(floatnum));
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
                foreach (DreamValue val in list.GetValues()) {
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

            string filePath;
            if (file.TryGetValueAsDreamResource(out var resource)) {
                filePath = resource.ResourcePath;
            } else if(!file.TryGetValueAsString(out filePath)) {
                throw new Exception($"{file} is not a valid file");
            }

            bool successful;
            if (filePath.EndsWith("/")) {
                successful = bundle.ResourceManager.DeleteDirectory(filePath);
            } else {
                successful = bundle.ResourceManager.DeleteFile(filePath);
            }

            return new DreamValue(successful ? 1 : 0);
        }

        [DreamProc("fexists")]
        [DreamProcParameter("File", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamResource)]
        public static DreamValue NativeProc_fexists(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            DreamValue file = bundle.GetArgument(0, "File");

            string filePath;
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
            DreamResource resource;


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
            if (!bundle.GetArgument(0, "type").TryGetValueAsString(out var filterTypeName))
                return DreamValue.Null;

            Type? filterType = DreamFilter.GetType(filterTypeName);
            if (filterType == null)
                return DreamValue.Null;

            var serializationManager = IoCManager.Resolve<ISerializationManager>();

            MappingDataNode attributes = new();
            for (int i = 0; i < bundle.Proc.ArgumentNames.Count; i++) { // Every argument is a filter property
                var propertyName = bundle.Proc.ArgumentNames[i];
                var property = bundle.Arguments[i];
                if (property.IsNull)
                    continue;

                attributes.Add(propertyName, new DreamValueDataNode(property));
            }

            DreamFilter? filter = serializationManager.Read(filterType, attributes) as DreamFilter;
            if (filter is null)
                throw new Exception($"Failed to create filter of type {filterType}");

            var filterObject = bundle.ObjectTree.CreateObject<DreamObjectFilter>(bundle.ObjectTree.Filter);
            filterObject.Filter = filter;
            return new DreamValue(filterObject);
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

            if (failCount > 0) {
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
                Match match = regex.Regex.Match(text, start - 1, end - start);

                return match.Success ? new DreamValue(match.Index + 1) : new DreamValue(0);
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

            if (failCount > 0) {
                return new DreamValue(failCount == 2 ? 1 : 0);
            }

            int start = bundle.GetArgument(2, "Start").MustGetValueAsInteger(); //1-indexed
            int end = bundle.GetArgument(3, "End").MustGetValueAsInteger(); //1-indexed

            if (start <= 0 || start > text.Length || end < 0) return new DreamValue(0);

            if (end == 0 || end > text.Length + 1) {
                end = text.Length + 1;
            }

            if (regex is not null) {
                Match match = regex.Regex.Match(text, start - 1, end - start);

                return match.Success ? new DreamValue(match.Index + 1) : new DreamValue(0);
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
            if (!bundle.GetArgument(0, "Haystack").TryGetValueAsString(out var text)) {
                failCount++;
            }
            if (!bundle.GetArgument(1, "Needle").TryGetValueAsString(out var needle)) {
                failCount++;
            }
            if (failCount > 0) {
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

            if(end > 0)
                actualcount = actualstart - (end-1);
            else
                actualcount  = actualstart - ((text.Length-1) + (end));
            actualcount += needle.Length-1;
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
            if (!bundle.GetArgument(0, "Haystack").TryGetValueAsString(out var text)) {
                failCount++;
            }
            if (!bundle.GetArgument(1, "Needle").TryGetValueAsString(out var needle)) {
                failCount++;
            }
            if (failCount > 0) {
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
                actualcount = actualstart - (end-1);
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
            //TODO: Implement flick()

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

        [DreamProc("hascall")]
        [DreamProcParameter("Object", Type = DreamValueTypeFlag.DreamObject)]
        [DreamProcParameter("ProcName", Type = DreamValueTypeFlag.String)]
        public static DreamValue NativeProc_hascall(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            if (!bundle.GetArgument(0, "Object").TryGetValueAsDreamObject(out var obj))
                return new DreamValue(0);
            if(!bundle.GetArgument(1, "ProcName").TryGetValueAsString(out var procName))
                return new DreamValue(0);

            return new DreamValue(obj.ObjectDefinition.HasProc(procName) ? 1 : 0);
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
                if (!arg.TryGetValueAsDreamObject(out var loc))
                    return DreamValue.False;
                if (loc is null)
                    return DreamValue.False;

                bool isLoc = loc is DreamObjectMob or DreamObjectTurf or DreamObjectArea ||
                             loc.IsSubtypeOf(bundle.ObjectTree.Obj);

                if (!isLoc)
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
            if (bundle.GetArgument(0, "n").TryGetValueAsFloat(out float floatnum)) {
                return new DreamValue(float.IsNaN(floatnum) ? 1 : 0);
            }
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
                            default: break;
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
                    return new DreamValue(jsonElement.GetString());
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
            else if (value.TryGetValueAsDreamList(out var list)) {
                if (list.IsAssociative) {
                    writer.WriteStartObject();

                    foreach (DreamValue listValue in list.GetValues()) {
                        var key = listValue.Stringify();

                        if (list.ContainsKey(listValue)) {
                            writer.WritePropertyName(key);
                            JsonEncode(writer, list.GetValue(listValue));
                        } else {
                            writer.WriteNull(key);
                        }
                    }

                    writer.WriteEndObject();
                } else {
                    writer.WriteStartArray();

                    foreach (DreamValue listValue in list.GetValues()) {
                        JsonEncode(writer, listValue);
                    }

                    writer.WriteEndArray();
                }
            } else if (value.TryGetValueAsDreamObject(out var dreamObject)) {
                if (dreamObject == null)
                    writer.WriteNullValue();
                else if (dreamObject is DreamObjectMatrix matrix) { // Special behaviour for /matrix values
                    writer.WriteStartArray();

                    foreach (var f in DreamObjectMatrix.EnumerateMatrix(matrix)) {
                        writer.WriteNumberValue(f);
                    }

                    writer.WriteEndArray();
                    // This doesn't have any corresponding snowflaking in CreateValueFromJsonElement()
                    // because BYOND actually just forgets that this was a matrix after doing json encoding.
                } else
                    writer.WriteStringValue(value.Stringify());
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
        public static DreamValue NativeProc_json_encode(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            using MemoryStream stream = new MemoryStream();
            using Utf8JsonWriter jsonWriter = new(stream, new JsonWriterOptions {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // "\"" instead of "\u0022"
            });

            JsonEncode(jsonWriter, bundle.GetArgument(0, "Value"));
            jsonWriter.Flush();

            return new DreamValue(Encoding.UTF8.GetString(stream.AsSpan()));
        }

        public static DreamValue _length(DreamValue value, bool countBytes) {
            if (value.TryGetValueAsString(out var str)) {
                return new DreamValue(countBytes ? str.Length : str.EnumerateRunes().Count());
            } else if (value.TryGetValueAsDreamList(out var list)) {
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

        [DreamProc("list2params")]
        [DreamProcParameter("List")]
        public static DreamValue NativeProc_list2params(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            if (!bundle.GetArgument(0, "List").TryGetValueAsDreamList(out DreamList list))
                return new DreamValue(string.Empty);
            return new DreamValue(list2params(list));
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
                    if (!DreamObjectMatrix.TryInvert(invertableMatrix)) {
                        throw new ArgumentException("/matrix provided for MATRIX_INVERT cannot be inverted");
                    }
                    return new DreamValue(invertableMatrix);
                case MatrixOpcode.Rotate:
                    var angleArgument = firstArgument;
                    if (firstArgument.TryGetValueAsDreamObject<DreamObjectMatrix>(out var matrixToRotate)) {
                        //We have a matrix to rotate, and an angle to rotate it by.
                        angleArgument = secondArgument;
                    }
                    if (!angleArgument.TryGetValueAsFloat(out float rotationAngle))
                        throw new ArgumentException($"/matrix() called with invalid rotation angle '{firstArgument}'");
                    var (angleSin, angleCos) = ((float, float))Math.SinCos(Math.PI / 180.0 * rotationAngle); // NOTE: Not sure if BYOND uses double or float precision in this specific case.
                    if (float.IsSubnormal(angleSin)) // FIXME: Think of a better solution to bad results for some angles.
                        angleSin = 0;
                    if (float.IsSubnormal(angleCos))
                        angleCos = 0;
                    var rotationMatrix = DreamObjectMatrix.MakeMatrix(bundle.ObjectTree, angleCos, angleSin, 0, -angleSin, angleCos, 0);
                    if (matrixToRotate == null) return new DreamValue(rotationMatrix);
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

            if (arg.TryGetValueAsDreamResource(out DreamResource resource)) {
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
            if (value.TryGetValueAsFloat(out var lFloat) && min.TryGetValueAsFloat(out var rFloat)) {
                if (lFloat < rFloat)
                    min = value;
            } else if (value.TryGetValueAsString(out var lString) && min.TryGetValueAsString(out var rString)) {
                if (string.Compare(lString, rString, StringComparison.Ordinal) < 0)
                    min = value;
            } else if (value.IsNull) {
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

        [DreamProc("orange")]
        [DreamProcParameter("Dist", Type = DreamValueTypeFlag.Float, DefaultValue = 5)]
        [DreamProcParameter("Center", Type = DreamValueTypeFlag.DreamObject)]
        public static DreamValue NativeProc_orange(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            (DreamObjectAtom? center, ViewRange range) = DreamProcNativeHelpers.ResolveViewArguments(bundle.DreamManager, usr as DreamObjectAtom, bundle.Arguments);
            if (center is null)
                return DreamValue.Null; // NOTE: Not sure if parity
            DreamList rangeList = bundle.ObjectTree.CreateList(range.Height * range.Width);
            foreach (var turf in DreamProcNativeHelpers.MakeViewSpiral(center, range)) {
                rangeList.AddValue(new DreamValue(turf));
                foreach (DreamValue content in turf.Contents.GetValues()) {
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

                view.AddValue(new(cell.Turf!));
                foreach (var movable in cell.Movables) {
                    view.AddValue(new(movable));
                }
            }

            return new DreamValue(view);
        }

        [DreamProc("oviewers")]
        [DreamProcParameter("Depth", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("Center", Type = DreamValueTypeFlag.DreamObject)]
        public static DreamValue NativeProc_oviewers(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) { //TODO: View obstruction (dense turfs)
            DreamValue depthValue = new DreamValue(5);
            DreamObjectAtom? center = null;

            //Arguments are optional and can be passed in any order
            if (bundle.Arguments.Length > 0) {
                DreamValue firstArgument = bundle.GetArgument(0, "Depth");

                if (firstArgument.TryGetValueAsDreamObject(out center)) {
                    if (bundle.Arguments.Length > 1) {
                        depthValue = bundle.GetArgument(1, "Center");
                    }
                } else {
                    depthValue = firstArgument;

                    if (bundle.Arguments.Length > 1) {
                        bundle.GetArgument(1, "Center").TryGetValueAsDreamObject(out center);
                    }
                }
            }

            center ??= usr as DreamObjectAtom;

            DreamList view = bundle.ObjectTree.CreateList();
            if (center == null)
                return new(view);

            var centerPos = bundle.AtomManager.GetAtomPosition(center);
            if (!depthValue.TryGetValueAsInteger(out var depth))
                depth = 5; //TODO: Default to world.view

            foreach (var atom in bundle.AtomManager.EnumerateAtoms(bundle.ObjectTree.Mob)) {
                var mob = (DreamObjectMob)atom;

                if (mob.X == centerPos.X && mob.Y == centerPos.Y)
                    continue;

                if (Math.Abs(centerPos.X - mob.X) <= depth && Math.Abs(centerPos.Y - mob.Y) <= depth) {
                    view.AddValue(new DreamValue(mob));
                }
            }

            return new DreamValue(view);
        }

        public static string list2params(DreamList list) {
            StringBuilder paramBuilder = new StringBuilder();

            List<DreamValue> values = list.GetValues();
            foreach (DreamValue entry in values) {
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

        public static DreamList params2list(DreamObjectTree objectTree, string queryString) {
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
                result = params2list(bundle.ObjectTree, paramsString);
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
                return DreamValue.Null; // NOTE: Not sure if parity
            DreamList rangeList = bundle.ObjectTree.CreateList(range.Height * range.Width);
            //Have to include centre
            rangeList.AddValue(new DreamValue(center));
            if(center.TryGetVariable("contents", out var centerContents) && centerContents.TryGetValueAsDreamList(out var centerContentsList)) {
                foreach(DreamValue content in centerContentsList.GetValues()) {
                    rangeList.AddValue(content);
                }
            }
            if(center is not DreamObjectTurf) { // If it's not a /turf, we have to include its loc and the loc's contents
                if(center.TryGetVariable("loc",out DreamValue centerLoc) && centerLoc.TryGetValueAsDreamObject(out var centerLocObject)) {
                    rangeList.AddValue(centerLoc);
                    if(centerLocObject.GetVariable("contents").TryGetValueAsDreamList(out var locContentsList)) {
                        foreach (DreamValue content in locContentsList.GetValues()) {
                            rangeList.AddValue(content);
                        }
                    }
                }
            }
            //And then everything else
            foreach (var turf in DreamProcNativeHelpers.MakeViewSpiral(center, range)) {
                rangeList.AddValue(new DreamValue(turf));
                foreach (DreamValue content in turf.Contents.GetValues()) {
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
        public static async Task<DreamValue> NativeProc_replacetext(AsyncNativeProc.State state) {
            DreamValue haystack = state.GetArgument(0, "Haystack");
            DreamValue needle = state.GetArgument(1, "Needle");
            DreamValue replacementArg = state.GetArgument(2, "Replacement");
            state.GetArgument(3, "Start").TryGetValueAsInteger(out var start); //1-indexed
            int end = state.GetArgument(4, "End").GetValueAsInteger(); //1-indexed

            if (needle.TryGetValueAsDreamObject<DreamObjectRegex>(out var regexObject)) {
                // According to the docs, this is the same as /regex.Replace()
                return await DreamProcNativeRegex.RegexReplace(state, regexObject, haystack, replacementArg, start, end);
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

        [DreamProc("rgb")]
        [DreamProcParameter("R", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("G", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("B", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("A", Type = DreamValueTypeFlag.Float)]
        public static DreamValue NativeProc_rgb(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            // TODO: accept lowercase named arguments here too
            // NOTE(moony): This only actually checks for the first character of it's parameter's names, rgb(RaUNCHY=1, bAd=2, gIrLs=3) is a valid rgb call.
            bundle.GetArgument(0, "R").TryGetValueAsInteger(out var r);
            bundle.GetArgument(1, "G").TryGetValueAsInteger(out var g);
            bundle.GetArgument(2, "B").TryGetValueAsInteger(out var b);
            DreamValue aValue = bundle.GetArgument(3, "A");

            r = Math.Clamp(r, 0, 255);
            g = Math.Clamp(g, 0, 255);
            b = Math.Clamp(b, 0, 255);

            // TODO: There is a difference between passing null and not passing a fourth arg at all
            // Likely a compile-time difference
            if (aValue.IsNull) {
                return new DreamValue($"#{r:X2}{g:X2}{b:X2}");
            } else {
                aValue.TryGetValueAsInteger(out var a);
                a = Math.Clamp(a, 0, 255);

                return new DreamValue($"#{r:X2}{g:X2}{b:X2}{a:X2}");
            }
        }

        [DreamProc("rgb2num")]
        [DreamProcParameter("color", Type = DreamValueTypeFlag.String)]
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

            if (!ColorHelpers.TryParseColor(color, out var c, defaultAlpha: null)) {
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
                    /// TODO Figure out why the chroma for #ca60db is 48 instead of 68
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
                        if (!Int32.TryParse(diceList[0], out sides)) { throw new Exception($"Invalid dice value: {diceInput}"); }
                    } else {
                        if (!Int32.TryParse(diceList[0], out dice)) { throw new Exception($"Invalid dice value: {diceInput}"); }
                        if (!Int32.TryParse(diceList[1], out sides)) {
                            string[] sideList = diceList[1].Split('+');

                            if (!Int32.TryParse(sideList[0], out sides) || !Int32.TryParse(sideList[1], out modifier))
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
            string t2;
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
            string t2;
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

            if(text == null) //this is for BYOND compat, and causes the function to ignore start/end if text is null or empty
                if(String.IsNullOrEmpty(insertText))
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


            String result = textElements.SubstringByTextElements(0, start - 1);
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
            if (!bundle.GetArgument(0, "Text").TryGetValueAsString(out var text)) {
                return new DreamValue(bundle.ObjectTree.CreateList());
            }

            int start = 0;
            int end = 0;
            if(bundle.GetArgument(2, "Start").TryGetValueAsInteger(out start))
                start -= 1; //1-indexed
            if(bundle.GetArgument(3, "End").TryGetValueAsInteger(out end))
                if(end == 0)
                    end = text.Length;
                else
                    end -= 1; //1-indexed
            bool includeDelimiters = false;
            if(bundle.GetArgument(4, "include_delimiters").TryGetValueAsInteger(out var includeDelimitersInt))
                includeDelimiters = includeDelimitersInt != 0; //idk why BYOND doesn't just use truthiness, but it doesn't, so...

            if(start > 0 || end < text.Length)
                text = text[Math.Max(start,0)..Math.Min(end, text.Length)];

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
                    return new DreamValue(bundle.ObjectTree.CreateList(values.ToArray()));
                } else {
                    return new DreamValue(bundle.ObjectTree.CreateList(regexObject.Regex.Split(text)));
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
                return new DreamValue(bundle.ObjectTree.CreateList(splitText));
            } else {
                return new DreamValue(bundle.ObjectTree.CreateList());
            }
        }

        private static void OutputToStatPanel(DreamManager dreamManager, DreamConnection connection, DreamValue name, DreamValue value) {
            if (name.IsNull && value.TryGetValueAsDreamList(out var list)) {
                foreach (var item in list.GetValues())
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
            if (arg.TryGetValueAsFloat(out float floatnum)) {
                return new DreamValue(MathF.Truncate(floatnum));
            }
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

            dirArg.TryGetValueAsInteger(out int possibleDir);

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
                            addingProcs = type.ObjectDefinition.Verbs;
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

        [DreamProc("view")]
        [DreamProcParameter("Dist", Type = DreamValueTypeFlag.Float, DefaultValue = 5)]
        [DreamProcParameter("Center", Type = DreamValueTypeFlag.DreamObject)]
        public static DreamValue NativeProc_view(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            DreamList view = bundle.ObjectTree.CreateList();

            (DreamObjectAtom? center, ViewRange range) = DreamProcNativeHelpers.ResolveViewArguments(bundle.DreamManager, usr as DreamObjectAtom, bundle.Arguments);
            if (center is null)
                return new(view);

            var eyePos = bundle.AtomManager.GetAtomPosition(center);
            var viewData = DreamProcNativeHelpers.CollectViewData(bundle.AtomManager, bundle.MapManager, eyePos, range);

            ViewAlgorithm.CalculateVisibility(viewData);

            foreach (var tile in DreamProcNativeHelpers.MakeViewSpiral(viewData, true)) {
                if (tile == null || tile.IsVisible == false)
                    continue;
                if (!bundle.MapManager.TryGetCellAt((eyePos.X + tile.DeltaX, eyePos.Y + tile.DeltaY), eyePos.Z, out var cell))
                    continue;

                view.AddValue(new(cell.Turf!));
                foreach (var movable in cell.Movables) {
                    view.AddValue(new(movable));
                }
            }

            return new DreamValue(view);
        }

        [DreamProc("viewers")]
        [DreamProcParameter("Depth", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("Center", Type = DreamValueTypeFlag.DreamObject)]
        public static DreamValue NativeProc_viewers(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) { //TODO: View obstruction (dense turfs)
            DreamValue depthValue = new DreamValue(5);
            DreamObject? center = null;

            //Arguments are optional and can be passed in any order
            if (bundle.Arguments.Length > 0) {
                DreamValue firstArgument = bundle.GetArgument(0, "Depth");

                if (firstArgument.TryGetValueAsDreamObject(out var firstObj)) {
                    center = firstObj;

                    if (bundle.Arguments.Length > 1) {
                        depthValue = bundle.GetArgument(1, "Center");
                    }
                } else {
                    depthValue = firstArgument;

                    if (bundle.Arguments.Length > 1) {
                        bundle.GetArgument(1, "Center").TryGetValueAsDreamObject(out center);
                    }
                }
            }

            center ??= usr;

            DreamList view = bundle.ObjectTree.CreateList();
            if (center == null)
                return new(view);

            int centerX = center.GetVariable("x").MustGetValueAsInteger();
            int centerY = center.GetVariable("y").MustGetValueAsInteger();
            if (!depthValue.TryGetValueAsInteger(out var depth))
                depth = 5; //TODO: Default to world.view

            foreach (var atom in bundle.AtomManager.EnumerateAtoms(bundle.ObjectTree.Mob)) {
                var mob = (DreamObjectMob)atom;

                if (Math.Abs(centerX - mob.X) <= depth && Math.Abs(centerY - mob.Y) <= depth) {
                    view.AddValue(new DreamValue(mob));
                }
            }

            return new DreamValue(view);
        }

        [DreamProc("walk")]
        [DreamProcParameter("Ref", Type = DreamValueTypeFlag.DreamObject)]
        [DreamProcParameter("Dir", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("Lag", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
        [DreamProcParameter("Speed", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_walk(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            //TODO: Implement walk()

            return DreamValue.Null;
        }

        [DreamProc("walk_to")]
        [DreamProcParameter("Ref", Type = DreamValueTypeFlag.DreamObject)]
        [DreamProcParameter("Trg", Type = DreamValueTypeFlag.DreamObject)]
        [DreamProcParameter("Min", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
        [DreamProcParameter("Lag", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
        [DreamProcParameter("Speed", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_walk_to(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            //TODO: Implement walk_to()

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
            bundle.GetArgument(3, "Speed").TryGetValueAsInteger(out var speed); // TODO: Use this. Speed=0 uses Ref.step_size

            bundle.WalkManager.StartWalkTowards(refAtom, trgAtom, lag);
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
            string winsetControlId = (!controlId.IsNull) ? controlId.GetValueAsString() : null;
            string winsetParams = bundle.GetArgument(2, "params").GetValueAsString();

            DreamConnection? connection = null;
            if (player.TryGetValueAsDreamObject<DreamObjectMob>(out var mob)) {
                connection = mob.Connection;
            } else if (player.TryGetValueAsDreamObject<DreamObjectClient>(out var client)) {
                connection = client.Connection;
            }

            if (connection == null) {
                throw new ArgumentException($"Invalid \"player\" argument {player}");
            }

            connection.WinSet(winsetControlId, winsetParams);
            return DreamValue.Null;
        }
    }
}
