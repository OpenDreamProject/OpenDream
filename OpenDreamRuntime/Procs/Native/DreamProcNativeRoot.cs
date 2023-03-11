using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using Robust.Shared.Utility;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using DreamValueType = OpenDreamRuntime.DreamValue.DreamValueType;
using Robust.Server;
using Robust.Shared.Asynchronous;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamRuntime.Procs.Native {
    static class DreamProcNativeRoot {
        // I don't want to edit 100 procs to have the DreamManager passed to them
        // TODO: Pass NativeProc.State to every native proc
        public static IDreamManager DreamManager;
        public static DreamResourceManager ResourceManager;
        public static IDreamObjectTree ObjectTree;

        [DreamProc("abs")]
        [DreamProcParameter("A", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_abs(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(0, "A").TryGetValueAsFloat(out float number);

            return new DreamValue(Math.Abs(number));
        }

        [DreamProc("alert")]
        [DreamProcParameter("Usr", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("Message", Type = DreamValueType.String)]
        [DreamProcParameter("Title", Type = DreamValueType.String)]
        [DreamProcParameter("Button1", Type = DreamValueType.String)]
        [DreamProcParameter("Button2", Type = DreamValueType.String)]
        [DreamProcParameter("Button3", Type = DreamValueType.String)]
        public static async Task<DreamValue> NativeProc_alert(AsyncNativeProc.State state) {
            string message, title, button1, button2, button3;

            DreamValue usrArgument = state.Arguments.GetArgument(0, "Usr");
            if (usrArgument.TryGetValueAsDreamObjectOfType(ObjectTree.Mob, out var mob)) {
                message = state.Arguments.GetArgument(1, "Message").Stringify();
                title = state.Arguments.GetArgument(2, "Title").Stringify();
                button1 = state.Arguments.GetArgument(3, "Button1").Stringify();
                button2 = state.Arguments.GetArgument(4, "Button2").Stringify();
                button3 = state.Arguments.GetArgument(5, "Button3").Stringify();
            } else {
                mob = state.Usr;
                message = usrArgument.Stringify();
                title = state.Arguments.GetArgument(1, "Message").Stringify();
                button1 = state.Arguments.GetArgument(2, "Title").Stringify();
                button2 = state.Arguments.GetArgument(3, "Button1").Stringify();
                button3 = state.Arguments.GetArgument(4, "Button2").Stringify();
            }

            if (String.IsNullOrEmpty(button1)) button1 = "Ok";

            DreamConnection connection = DreamManager.GetConnectionFromMob(mob);
            return await connection.Alert(title, message, button1, button2, button3);
        }

        [DreamProc("animate")]
        [DreamProcParameter("Object", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("time", Type = DreamValueType.Float)]
        [DreamProcParameter("loop", Type = DreamValueType.Float)]
        [DreamProcParameter("easing", Type = DreamValueType.String)]
        [DreamProcParameter("flags", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_animate(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            // TODO: Leaving out the Object var adds a new step to the previous animation
            if (!arguments.GetArgument(0, "Object").TryGetValueAsDreamObjectOfType(ObjectTree.Atom, out var obj))
                return DreamValue.Null;
            // TODO: Is this the correct behavior for invalid time?
            if (!arguments.GetArgument(0, "time").TryGetValueAsFloat(out float time))
                return DreamValue.Null;
            if (arguments.GetArgument(0, "loop").TryGetValueAsInteger(out int loop))
                throw new NotImplementedException("Looped animations are not implemented");
            if (arguments.GetArgument(0, "easing").TryGetValueAsInteger(out int easing) && easing != 1) // LINEAR_EASING only
                throw new NotImplementedException("Non-linear easing types are not implemented");
            if (arguments.GetArgument(0, "flags").TryGetValueAsInteger(out int flags) && flags != 0)
                throw new NotImplementedException("Flags are not implemented");

            IAtomManager atomManager = IoCManager.Resolve<IAtomManager>();
            atomManager.AnimateAppearance(obj, TimeSpan.FromMilliseconds(time * 100), appearance => {
                if (arguments.NamedArguments?.TryGetValue("pixel_x", out DreamValue pixelX) is true) {
                    obj.SetVariableValue("pixel_x", pixelX);
                    pixelX.TryGetValueAsInteger(out appearance.PixelOffset.X);
                }

                if (arguments.NamedArguments?.TryGetValue("pixel_y", out DreamValue pixelY) is true) {
                    obj.SetVariableValue("pixel_y", pixelY);
                    pixelY.TryGetValueAsInteger(out appearance.PixelOffset.Y);
                }

                if (arguments.NamedArguments?.TryGetValue("dir", out DreamValue dir) is true) {
                    obj.SetVariableValue("dir", dir);
                    dir.TryGetValueAsInteger(out int dirValue);
                    appearance.Direction = (AtomDirection)dirValue;
                }

                // TODO: Rest of the animatable vars
            });

            return DreamValue.Null;
        }

        /* NOTE ABOUT THE TRIG FUNCTIONS:
         * If you have a sharp eye, you may notice that our trigonometry functions make use of the *double*-precision versions of those functions,
         * even though this is a single-precision language.
         *
         * DO NOT replace them with the single-precision ones in MathF!!!
         *
         * BYOND erroneously calls the double-precision versions in its code, in a way that does honestly affect behaviour in some circumstances.
         * Replicating that REQUIRES us to do the same error! You will break a unit test or two if you try to change this.
         */

        [DreamProc("arccos")]
        [DreamProcParameter("X", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_arccos(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(0, "X").TryGetValueAsFloat(out float x);
            double acos = Math.Acos(x);

            return new DreamValue((float)(acos * 180 / Math.PI));
        }

        [DreamProc("arcsin")]
        [DreamProcParameter("X", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_arcsin(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(0, "X").TryGetValueAsFloat(out float x);
            double asin = Math.Asin(x);

            return new DreamValue((float)(asin * 180 / Math.PI));
        }

        [DreamProc("arctan")]
        [DreamProcParameter("A", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_arctan(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(0, "A").TryGetValueAsFloat(out float a);
            double atan = Math.Atan(a);

            return new DreamValue((float)(atan * 180 / Math.PI));
        }

        [DreamProc("ascii2text")]
        [DreamProcParameter("N", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_ascii2text(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue ascii = arguments.GetArgument(0, "N");
            if (!ascii.TryGetValueAsInteger(out int asciiValue))
                throw new Exception($"{ascii} is not a number");

            return new DreamValue(Convert.ToChar(asciiValue).ToString());
        }

        [DreamProc("ceil")]
        [DreamProcParameter("A", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_ceil(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue arg = arguments.GetArgument(0, "A");
            if (arg.TryGetValueAsFloat(out float floatnum)) {
                return new DreamValue(MathF.Ceiling(floatnum));
            }
            return new DreamValue(0);
        }

        [DreamProc("ckey")]
        [DreamProcParameter("Key", Type = DreamValueType.String)]
        public static DreamValue NativeProc_ckey(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (!arguments.GetArgument(0, "Key").TryGetValueAsString(out var key))
            {
                return DreamValue.Null;
            }

            key = Regex.Replace(key.ToLower(), "[\\^]|[^a-z0-9@]", ""); //Remove all punctuation and make lowercase
            return new DreamValue(key);
        }

        [DreamProc("ckeyEx")]
        [DreamProcParameter("Text", Type = DreamValueType.String)]
        public static DreamValue NativeProc_ckeyEx(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (!arguments.GetArgument(0, "Text").TryGetValueAsString(out var text))
            {
                return DreamValue.Null;
            }

            text = Regex.Replace(text, "[\\^]|[^A-z0-9@_-]", ""); //Remove all punctuation except - and _
            return new DreamValue(text);
        }

        [DreamProc("clamp")]
        [DreamProcParameter("Value", Type = DreamValueType.Float | DreamValueType.DreamObject)]
        [DreamProcParameter("Low", Type = DreamValueType.Float)]
        [DreamProcParameter("High", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_clamp(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue value = arguments.GetArgument(0, "Value");

            if (!arguments.GetArgument(1, "Low").TryGetValueAsFloat(out float lVal))
                throw new Exception("Lower bound is not a number");
            if (!arguments.GetArgument(2, "High").TryGetValueAsFloat(out float hVal))
                throw new Exception("Upper bound is not a number");

            // BYOND supports switching low/high args around
            if (lVal > hVal) {
                (hVal, lVal) = (lVal, hVal);
            }

            if (value.TryGetValueAsDreamList(out var list)) {
                DreamList tmp = ObjectTree.CreateList();
                foreach (DreamValue val in list.GetValues()) {
                    if (!val.TryGetValueAsFloat(out float floatVal))
                        continue;

                    tmp.AddValue(new DreamValue(Math.Clamp(floatVal, lVal, hVal)));
                }

                return new DreamValue(tmp);
            } else if (value.TryGetValueAsFloat(out float floatVal)) {
                return new DreamValue(Math.Clamp(floatVal, lVal, hVal));
            } else if (value == DreamValue.Null) {
                return new DreamValue(Math.Clamp(0.0, lVal, hVal));
            } else {
                throw new Exception("Clamp expects a number or list");
            }
        }

        [DreamProc("cmptext")]
        [DreamProcParameter("T1", Type = DreamValueType.String)]
        public static DreamValue NativeProc_cmptext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            var argEnumerator = arguments.AllArgumentsEnumerator();

            if (!argEnumerator.MoveNext() || !argEnumerator.Current.TryGetValueAsString(out var t1))
                return DreamValue.False;

            while (argEnumerator.MoveNext()) {
                DreamValue arg = argEnumerator.Current;

                if (!arg.TryGetValueAsString(out var t2))
                    return DreamValue.False;

                if (!t2.Equals(t1, StringComparison.InvariantCultureIgnoreCase))
                    return DreamValue.False;
            }

            return DreamValue.True;
        }

        [DreamProc("copytext")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_copytext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(2, "End").TryGetValueAsInteger(out var end); //1-indexed

            if (!arguments.GetArgument(0, "T").TryGetValueAsString(out string? text))
                return (end == 0) ? DreamValue.Null : new DreamValue("");
            if (!arguments.GetArgument(1, "Start").TryGetValueAsInteger(out int start)) //1-indexed
                return new DreamValue("");

            if (end <= 0) end += text.Length + 1;
            else if (end > text.Length + 1) end = text.Length + 1;

            if (start == 0) return new DreamValue("");
            else if (start < 0) start += text.Length + 1;

            return new DreamValue(text.Substring(start - 1, end - start));
        }

        [DreamProc("copytext_char")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_copytext_char(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(2, "End").TryGetValueAsInteger(out var end); //1-indexed

            if (!arguments.GetArgument(0, "T").TryGetValueAsString(out string? text))
                return (end == 0) ? DreamValue.Null : new DreamValue("");
            if (!arguments.GetArgument(1, "Start").TryGetValueAsInteger(out int start)) //1-indexed
                return new DreamValue("");

            StringInfo textElements = new StringInfo(text);

            if (end <= 0) end += textElements.LengthInTextElements + 1;
            else if (end > textElements.LengthInTextElements + 1) end = textElements.LengthInTextElements + 1;

            if (start == 0) return new DreamValue("");
            else if (start < 0) start += textElements.LengthInTextElements + 1;

            return new DreamValue(textElements.SubstringByTextElements(start - 1, end - start));
        }

        [DreamProc("cos")]
        [DreamProcParameter("X", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_cos(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(0, "X").TryGetValueAsFloat(out float x);
            double rad = x * (Math.PI / 180);

            return new DreamValue((float)Math.Cos(rad));
        }

        [DreamProc("CRASH")]
        [DreamProcParameter("msg", Type = DreamValueType.String)]
        public static DreamValue NativeProc_CRASH(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(0, "msg").TryGetValueAsString(out var message);

            throw new DMCrashRuntime(new DreamValue(message ?? String.Empty));
        }

        [DreamProc("fcopy")]
        [DreamProcParameter("Src", Type = DreamValueType.String | DreamValueType.DreamResource)]
        [DreamProcParameter("Dst", Type = DreamValueType.String)]
        public static DreamValue NativeProc_fcopy(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            var arg1 = arguments.GetArgument(0, "Src");

            string? src;
            if (arg1.TryGetValueAsDreamResource(out DreamResource? arg1Rsc)) {
                src = arg1Rsc.ResourcePath;
            } else if (arg1.TryGetValueAsDreamObjectOfType(ObjectTree.Savefile, out var savefile)) {
                src = DreamMetaObjectSavefile.ObjectToSavefile[savefile].Resource.ResourcePath;
            } else if (!arg1.TryGetValueAsString(out src)) {
                throw new Exception($"Bad src file {arg1}");
            }

            var arg2 = arguments.GetArgument(1, "Dst");
            if (!arg2.TryGetValueAsString(out var dst)) {
                throw new Exception($"Bad dst file {arg2}");
            }

            var resourceManager = IoCManager.Resolve<DreamResourceManager>();
            return new DreamValue(resourceManager.CopyFile(src, dst) ? 1 : 0);
        }

        [DreamProc("fcopy_rsc")]
        [DreamProcParameter("File", Type = DreamValueType.String | DreamValueType.DreamResource)]
        public static DreamValue NativeProc_fcopy_rsc(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            var arg1 = arguments.GetArgument(0, "File");

            string filePath;
            if (arg1.TryGetValueAsDreamResource(out DreamResource arg1Rsc)) {
                filePath = arg1Rsc.ResourcePath;
            } else if (!arg1.TryGetValueAsString(out filePath)) {
                return DreamValue.Null;
            }

            var resourceManager = IoCManager.Resolve<DreamResourceManager>();
            return new DreamValue(resourceManager.LoadResource(filePath));
        }

        [DreamProc("fdel")]
        [DreamProcParameter("File", Type = DreamValueType.String)]
        public static DreamValue NativeProc_fdel(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue file = arguments.GetArgument(0, "File");

            string filePath;
            if (file.TryGetValueAsDreamResource(out var resource)) {
                filePath = resource.ResourcePath;
            } else if(!file.TryGetValueAsString(out filePath)) {
                throw new Exception($"{file} is not a valid file");
            }

            var resourceManager = IoCManager.Resolve<DreamResourceManager>();
            bool successful;
            if (filePath.EndsWith("/")) {
                successful = resourceManager.DeleteDirectory(filePath);
            } else {
                successful = resourceManager.DeleteFile(filePath);
            }

            return new DreamValue(successful ? 1 : 0);
        }

        [DreamProc("fexists")]
        [DreamProcParameter("File", Type = DreamValueType.String | DreamValueType.DreamResource)]
        public static DreamValue NativeProc_fexists(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue file = arguments.GetArgument(0, "File");

            string filePath;
            if (file.TryGetValueAsDreamResource(out var rsc)) {
                filePath = rsc.ResourcePath;
            } else if (!file.TryGetValueAsString(out filePath)) {
                return DreamValue.Null;
            }

            var resourceManager = IoCManager.Resolve<DreamResourceManager>();
            return new DreamValue(resourceManager.DoesFileExist(filePath) ? 1 : 0);
        }

        [DreamProc("file")]
        [DreamProcParameter("Path", Type = DreamValueType.String | DreamValueType.DreamResource)]
        public static DreamValue NativeProc_file(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue path = arguments.GetArgument(0, "Path");

            if (path.TryGetValueAsString(out var rscPath)) {
                var resourceManager = IoCManager.Resolve<DreamResourceManager>();
                var resource = resourceManager.LoadResource(rscPath);

                return new DreamValue(resource);
            }

            if (path.Type == DreamValueType.DreamResource) {
                return path;
            }

            throw new Exception("Invalid path argument");
        }

        [DreamProc("file2text")]
        [DreamProcParameter("File", Type = DreamValueType.String | DreamValueType.DreamResource)]
        public static DreamValue NativeProc_file2text(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue file = arguments.GetArgument(0, "File");
            DreamResource resource;


            if (file.TryGetValueAsString(out var rscPath)) {
                var resourceManager = IoCManager.Resolve<DreamResourceManager>();

                resource = resourceManager.LoadResource(rscPath);
            } else if (!file.TryGetValueAsDreamResource(out resource)) {
                return DreamValue.Null;
            }

            string? text = resource.ReadAsString();
            return (text != null) ? new DreamValue(text) : DreamValue.Null;
        }

        [DreamProc("filter")]
        [DreamProcParameter("type", Type = DreamValueType.String)] // Must be from a valid list
        [DreamProcParameter("size", Type = DreamValueType.Float)]
        [DreamProcParameter("color", Type = DreamValueType.String)]
        [DreamProcParameter("flags", Type = DreamValueType.Float)]
        [DreamProcParameter("x", Type = DreamValueType.Float)]
        [DreamProcParameter("y", Type = DreamValueType.Float)]
        [DreamProcParameter("offset", Type = DreamValueType.Float)]
        [DreamProcParameter("threshold", Type = DreamValueType.String)]
        [DreamProcParameter("alpha", Type = DreamValueType.Float)]
        [DreamProcParameter("space", Type = DreamValueType.Float)]
        [DreamProcParameter("transform", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("blend_mode", Type = DreamValueType.Float)]
        [DreamProcParameter("factor", Type = DreamValueType.Float)]
        [DreamProcParameter("repeat", Type = DreamValueType.Float)]
        [DreamProcParameter("radius", Type = DreamValueType.Float)]
        [DreamProcParameter("falloff", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_filter(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (!arguments.GetArgument(0, "type").TryGetValueAsString(out var filterTypeName))
                return DreamValue.Null;

            Type? filterType = DreamFilter.GetType(filterTypeName);
            if (filterType == null)
                return DreamValue.Null;

            var serializationManager = IoCManager.Resolve<ISerializationManager>();

            MappingDataNode attributes = new();
            foreach (KeyValuePair<string, DreamValue> attribute in arguments.NamedArguments) {
                DreamValue value = attribute.Value;

                attributes.Add(attribute.Key, new DreamValueDataNode(value));
            }

            DreamFilter? filter = serializationManager.Read(filterType, attributes) as DreamFilter;
            if (filter == null)
                throw new Exception($"Failed to create filter of type {filterType}");

            DreamObject filterObject = ObjectTree.CreateObject(ObjectTree.Filter);
            DreamMetaObjectFilter.DreamObjectToFilter[filterObject] = filter;
            return new DreamValue(filterObject);
        }

        [DreamProc("findtext")]
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needle", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_findtext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            // TODO This is for handling nulls, check if it works right for other bad types
            int failCount = 0;
            if (!arguments.GetArgument(0, "Haystack").TryGetValueAsString(out var text))
            {
                failCount++;
            }
            if (!arguments.GetArgument(1, "Needle").TryGetValueAsString(out var needle))
            {
                failCount++;
            }
            if (failCount > 0)
            {
                return new DreamValue(failCount == 2 ? 1 : 0);
            }

            int start = arguments.GetArgument(2, "Start").GetValueAsInteger(); //1-indexed
            int end = arguments.GetArgument(3, "End").GetValueAsInteger(); //1-indexed

            if (start > text.Length || start == 0) return new DreamValue(0);

            if (start < 0)
            {
                start = text.Length + start + 1; //1-indexed
            }

            if (end < 0)
            {
                end = text.Length + end + 1; //1-indexed
            }

            if (end == 0 || end > text.Length + 1) {
                end = text.Length + 1;
            }

            int needleIndex = text.IndexOf(needle, start - 1, end - start, StringComparison.OrdinalIgnoreCase);
            return new DreamValue(needleIndex + 1); //1-indexed
        }

        [DreamProc("findtextEx")]
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needle", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_findtextEx(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            // TODO This is for handling nulls, check if it works right for other bad types
            int failCount = 0;
            if (!arguments.GetArgument(0, "Haystack").TryGetValueAsString(out var text))
            {
                failCount++;
            }
            if (!arguments.GetArgument(1, "Needle").TryGetValueAsString(out var needle))
            {
                failCount++;
            }
            if (failCount > 0)
            {
                return new DreamValue(failCount == 2 ? 1 : 0);
            }

            int start = arguments.GetArgument(2, "Start").GetValueAsInteger(); //1-indexed
            int end = arguments.GetArgument(3, "End").GetValueAsInteger(); //1-indexed

            if (start <= 0 || start > text.Length || end < 0) return new DreamValue(0);

            if (end == 0 || end > text.Length + 1) {
                end = text.Length + 1;
            }

            int needleIndex = text.IndexOf(needle, start - 1, end - start);
            if (needleIndex != -1) {
                return new DreamValue(needleIndex + 1); //1-indexed
            } else {
                return new DreamValue(0);
            }
        }

        [DreamProc("findlasttext")]
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needle", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_findlasttext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            // TODO This is for handling nulls, check if it works right for other bad types
            int failCount = 0;
            if (!arguments.GetArgument(0, "Haystack").TryGetValueAsString(out var text))
            {
                failCount++;
            }
            if (!arguments.GetArgument(1, "Needle").TryGetValueAsString(out var needle))
            {
                failCount++;
            }
            if (failCount > 0)
            {
                return new DreamValue(failCount == 2 ? 1 : 0);
            }

            int start = arguments.GetArgument(2, "Start").GetValueAsInteger(); //1-indexed
            int end = arguments.GetArgument(3, "End").GetValueAsInteger(); //1-indexed

            if (end == 0) {
                end = text.Length + 1;
            }

            int needleIndex = text.LastIndexOf(needle, end - 1, end - start, StringComparison.OrdinalIgnoreCase);
            if (needleIndex != -1) {
                return new DreamValue(needleIndex + 1); //1-indexed
            } else {
                return new DreamValue(0);
            }
        }

        [DreamProc("findlasttextEx")]
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needle", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_findlasttextEx(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            // TODO This is for handling nulls, check if it works right for other bad types
            int failCount = 0;
            if (!arguments.GetArgument(0, "Haystack").TryGetValueAsString(out var text))
            {
                failCount++;
            }
            if (!arguments.GetArgument(1, "Needle").TryGetValueAsString(out var needle))
            {
                failCount++;
            }
            if (failCount > 0)
            {
                return new DreamValue(failCount == 2 ? 1 : 0);
            }

            int start = arguments.GetArgument(2, "Start").GetValueAsInteger(); //1-indexed
            int end = arguments.GetArgument(3, "End").GetValueAsInteger(); //1-indexed

            if (end == 0) {
                end = text.Length + 1;
            }

            int needleIndex = text.LastIndexOf(needle, end - 1, end - start);
            if (needleIndex != -1) {
                return new DreamValue(needleIndex + 1); //1-indexed
            } else {
                return new DreamValue(0);
            }
        }

        [DreamProc("flick")]
        [DreamProcParameter("Icon", Type = DreamValueType.String | DreamValueType.DreamResource)]
        [DreamProcParameter("Object", Type = DreamValueType.String | DreamValueType.DreamResource)]
        public static DreamValue NativeProc_flick(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            //TODO: Implement flick()

            return DreamValue.Null;
        }

        [DreamProc("flist")]
        [DreamProcParameter("Path", Type = DreamValueType.String)]
        public static DreamValue NativeProc_flist(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (!arguments.GetArgument(0, "Path").TryGetValueAsString(out var path)) {
                path = IoCManager.Resolve<DreamResourceManager>().RootPath + Path.DirectorySeparatorChar;
            }

            var resourceManager = IoCManager.Resolve<DreamResourceManager>();
            var listing = resourceManager.EnumerateListing(path);
            DreamList list = ObjectTree.CreateList(listing);
            return new DreamValue(list);
        }

        [DreamProc("floor")]
        [DreamProcParameter("A", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_floor(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue arg = arguments.GetArgument(0, "A");
            if (arg.TryGetValueAsFloat(out float floatnum)) {
                return new DreamValue(MathF.Floor(floatnum));
            }
            return new DreamValue(0);
        }

        [DreamProc("fract")]
        [DreamProcParameter("n", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_fract(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue arg = arguments.GetArgument(0, "n");
            if (arg.TryGetValueAsFloat(out float floatnum)) {
                if(float.IsInfinity(floatnum)) {
                    return new DreamValue(0);
                }
                return new DreamValue(floatnum - MathF.Truncate(floatnum));
            }
            return new DreamValue(0);
        }

        [DreamProc("gradient")]
        [DreamProcParameter("A", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("index", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_gradient(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            // We dont want keyword arguments screwing with this
            DreamValue dreamIndex;

            int colorSpace = 0;

            List<DreamValue> gradientList;

            if (arguments.GetArgument(0, "A").TryGetValueAsDreamList(out DreamList? gradList)) {
                gradientList = gradList.GetValues();
                arguments.TryGetPositionalArgument(1, out dreamIndex);

                DreamValue dictSpace = gradList.GetValue(new("space"));
                dictSpace.TryGetValueAsInteger(out colorSpace);
            } else {
                if (!arguments.TryGetNamedArgument("index", out dreamIndex)) {
                    arguments.TryGetPositionalArgument(arguments.OrderedArgumentCount - 1, out dreamIndex);
                    arguments.OrderedArguments?.Pop();
                }
                gradientList = arguments.GetAllArguments();
            }

            if (!dreamIndex.TryGetValueAsFloat(out float index)) {
                throw new FormatException("Failed to parse index as float");
            }

            bool loop = gradientList.Contains(new("loop"));
            if (arguments.TryGetNamedArgument("space", out DreamValue namedLookup)) {
                namedLookup.TryGetValueAsInteger(out colorSpace);
            }

            // true: look for int: false look for color
            bool colorOrInt = true;

            float workingFloat = 0;
            float maxValue = 1;
            float minValue = 0;
            float leftBound = 0;
            float rightBound = 1;

            Color? left = null;
            Color? right = null;

            foreach (DreamValue value in gradientList) {
                if (colorOrInt && value.TryGetValueAsFloat(out float flt)) { // Int
                    colorOrInt = false;
                    workingFloat = flt;
                    maxValue = Math.Max(maxValue, flt);
                    minValue = Math.Min(minValue, flt);
                    continue; // Successful parse
                }

                if (!value.TryGetValueAsString(out string? strValue)) {
                    strValue = "#00000000";
                }

                if (strValue == "loop") continue;

                if (!ColorHelpers.TryParseColor(strValue, out Color color))
                    color = new(0, 0, 0, 0);


                if (loop && index >= maxValue) {
                    index %= maxValue;
                }

                if (workingFloat >= index) {
                    right = color;
                    rightBound = workingFloat;
                    break;
                }
                else {
                    left = color;
                    leftBound = workingFloat;
                }

                if (colorOrInt) {
                    workingFloat = 1;
                }

                colorOrInt = true;
            }

            // Convert the index to a 0-1 range
            float normalized = (index - leftBound) / (rightBound - leftBound);

            // Cheap way to make sure the gradient works at the extremes (eg 1 and 0)
            if (!left.HasValue || (right.HasValue && normalized == 1) || (right.HasValue && normalized == 0)) {
                if (right?.AByte == 255) {
                    return new DreamValue(right?.ToHexNoAlpha().ToLower() ?? "#00000000");
                }
                return new DreamValue(right?.ToHex().ToLower() ?? "#00000000");
            } else if (!right.HasValue) {
                if (left?.AByte == 255) {
                    return new DreamValue(left?.ToHexNoAlpha().ToLower() ?? "#00000000");
                }
                return new DreamValue(left?.ToHex().ToLower() ?? "#00000000");
            } else if (!left.HasValue && !right.HasValue) {
                throw new InvalidOperationException("Failed to find any colors");
            }

            Color returnval;
            switch (colorSpace) {
                case 0: // RGB
                    returnval = Color.InterpolateBetween(left.GetValueOrDefault(), right.GetValueOrDefault(), normalized);
                    break;
                case 1 or 2: // HSV/HSL
                    Vector4 vect1 = new(Color.ToHsv(left.GetValueOrDefault()));
                    Vector4 vect2 = new(Color.ToHsv(right.GetValueOrDefault()));

                    // Some precision is lost when coverting back to HSV at very small values this fixes that issue
                    if (normalized < 0.05f) {
                        normalized += 0.001f;
                    }

                    // This time it's overshooting
                    // dw these numbers are insanely arbitrary
                    if(normalized > 0.9f) {
                        normalized -= 0.00445f;
                    }

                    float newhue;
                    float delta = vect2.X - vect1.X;
                    if (vect1.X > vect2.X) {
                        (vect1.X, vect2.X) = (vect2.X, vect1.X);
                        delta = -delta;
                        normalized = 1 - normalized;
                    }
                    if (delta > 0.5f) // 180deg
                    {
                        vect1.X += 1f; // 360deg
                        newhue = (vect1.X + normalized * (vect2.X - vect1.X)) % 1; // 360deg
                    } else {
                        newhue = vect1.X + normalized * delta;
                    }

                    Vector4 holder = new(
                        newhue,
                        vect1.Y + normalized * (vect2.Y - vect1.Y),
                        vect1.Z + normalized * (vect2.Z - vect1.Z),
                        vect1.W + normalized * (vect2.W - vect1.W));

                    returnval = Color.FromHsv(holder);
                    break;
                default:
                    throw new NotSupportedException("Cannot interpolate colorspace");
            }

            if (returnval.AByte == 255) {
                return new DreamValue(returnval.ToHexNoAlpha().ToLower());
            }
            return new DreamValue(returnval.ToHex().ToLower());
        }

        [DreamProc("ftime")]
        [DreamProcParameter("File", Type = DreamValueType.String)]
        [DreamProcParameter("IsCreationTime", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_ftime(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue file = arguments.GetArgument(0, "File");
            DreamValue isCreationTime = arguments.GetArgument(1, "IsCreationTime");

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
        [DreamProcParameter("Object", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("ProcName", Type = DreamValueType.String)]
        public static DreamValue NativeProc_hascall(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (!arguments.GetArgument(0, "Object").TryGetValueAsDreamObject(out var obj))
                return new DreamValue(0);
            if(!arguments.GetArgument(1, "ProcName").TryGetValueAsString(out var procName))
                return new DreamValue(0);

            return new DreamValue(obj.ObjectDefinition.HasProc(procName) ? 1 : 0);
        }

        [DreamProc("html_decode")]
        [DreamProcParameter("HtmlText", Type = DreamValueType.String)]
        public static DreamValue NativeProc_html_decode(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string htmlText = arguments.GetArgument(0, "HtmlText").Stringify();

            return new DreamValue(HttpUtility.HtmlDecode(htmlText));
        }

        [DreamProc("html_encode")]
        [DreamProcParameter("PlainText", Type = DreamValueType.String)]
        public static DreamValue NativeProc_html_encode(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string plainText = arguments.GetArgument(0, "PlainText").Stringify();

            return new DreamValue(HttpUtility.HtmlEncode(plainText));
        }

        [DreamProc("icon_states")]
        [DreamProcParameter("Icon", Type = DreamValueType.DreamResource)]
        [DreamProcParameter("mode", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_icon_states(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            var mode = arguments.GetArgument(1, "mode").GetValueAsInteger();
            if (mode != 0) {
                throw new NotImplementedException("Only mode 0 is implemented");
            }

            var arg = arguments.GetArgument(0, "Icon");

            if (arg.TryGetValueAsDreamObjectOfType(ObjectTree.Icon, out var iconObj)) {
                // Fast path for /icon, we don't need to generate the entire DMI
                return new DreamValue(ObjectTree.CreateList(DreamMetaObjectIcon.ObjectToDreamIcon[iconObj].States.Keys.ToArray()));
            } else if (ResourceManager.TryLoadIcon(arg, out var iconRsc)) {
                return new DreamValue(ObjectTree.CreateList(iconRsc.DMI.States.Keys.ToArray()));
            } else if (arg == DreamValue.Null) {
                return DreamValue.Null;
            } else {
                throw new Exception($"Bad icon {arg}");
            }
        }

        [DreamProc("image")]
        [DreamProcParameter("icon", Type = DreamValueType.DreamResource)]
        [DreamProcParameter("loc", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("icon_state", Type = DreamValueType.String)]
        [DreamProcParameter("layer", Type = DreamValueType.Float)]
        [DreamProcParameter("dir", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_image(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamObject imageObject = ObjectTree.CreateObject(ObjectTree.Image);
            imageObject.InitSpawn(arguments);
            return new DreamValue(imageObject);
        }

        [DreamProc("isarea")]
        [DreamProcParameter("Loc1", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_isarea(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            var argEnumerator = arguments.AllArgumentsEnumerator();

            while (argEnumerator.MoveNext()) {
                if (!argEnumerator.Current.TryGetValueAsDreamObjectOfType(ObjectTree.Area, out _))
                    return DreamValue.False;
            }

            return DreamValue.True;
        }

        [DreamProc("isfile")]
        [DreamProcParameter("File")]
        public static DreamValue NativeProc_isfile(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue file = arguments.GetArgument(0, "File");

            return new DreamValue((file.Type == DreamValueType.DreamResource) ? 1 : 0);
        }

        [DreamProc("isicon")]
        [DreamProcParameter("Icon")]
        public static DreamValue NativeProc_isicon(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue icon = arguments.GetArgument(0, "Icon");
            if (icon.TryGetValueAsDreamObjectOfType(ObjectTree.Icon, out _))
                return new DreamValue(1);
            else if (icon.TryGetValueAsDreamResource(out DreamResource resource)) {
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
        [DreamProcParameter("n", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_isinf(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if(arguments.GetArgument(0, "n").TryGetValueAsFloat(out float floatnum)) {
                return new DreamValue(float.IsInfinity(floatnum) ? 1 : 0);
            }
            return new DreamValue(0);
        }

        [DreamProc("islist")]
        [DreamProcParameter("Object")]
        public static DreamValue NativeProc_islist(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            bool isList = arguments.GetArgument(0, "Object").TryGetValueAsDreamList(out _);
            return new DreamValue(isList ? 1 : 0);
        }

        [DreamProc("isloc")]
        [DreamProcParameter("Loc1", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_isloc(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            var argEnumerator = arguments.AllArgumentsEnumerator();

            while (argEnumerator.MoveNext()) {
                if (!argEnumerator.Current.TryGetValueAsDreamObject(out var loc))
                    return DreamValue.False;
                if (loc is null)
                    return DreamValue.False;

                bool isLoc = loc.IsSubtypeOf(ObjectTree.Mob) || loc.IsSubtypeOf(ObjectTree.Obj) ||
                             loc.IsSubtypeOf(ObjectTree.Turf) || loc.IsSubtypeOf(ObjectTree.Area);

                if (!isLoc)
                    return DreamValue.False;
            }

            return DreamValue.True;
        }

        [DreamProc("ismob")]
        [DreamProcParameter("Loc1", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_ismob(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            var argEnumerator = arguments.AllArgumentsEnumerator();

            while (argEnumerator.MoveNext()) {
                if (!argEnumerator.Current.TryGetValueAsDreamObjectOfType(ObjectTree.Mob, out _))
                    return DreamValue.False;
            }

            return DreamValue.True;
        }

        [DreamProc("ismovable")]
        [DreamProcParameter("Loc1", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_ismovable(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            var argEnumerator = arguments.AllArgumentsEnumerator();

            while (argEnumerator.MoveNext()) {
                if (!argEnumerator.Current.TryGetValueAsDreamObjectOfType(ObjectTree.Movable, out _))
                    return DreamValue.False;
            }

            return DreamValue.True;
        }

        [DreamProc("isnan")]
        [DreamProcParameter("n", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_isnan(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (arguments.GetArgument(0, "n").TryGetValueAsFloat(out float floatnum)) {
                return new DreamValue(float.IsNaN(floatnum) ? 1 : 0);
            }
            return new DreamValue(0);
        }

        [DreamProc("isnull")]
        [DreamProcParameter("Val")]
        public static DreamValue NativeProc_isnull(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue value = arguments.GetArgument(0, "Val");

            return new DreamValue((value == DreamValue.Null) ? 1 : 0);
        }

        [DreamProc("isnum")]
        [DreamProcParameter("Val")]
        public static DreamValue NativeProc_isnum(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue value = arguments.GetArgument(0, "Val");

            return new DreamValue((value.Type == DreamValueType.Float) ? 1 : 0);
        }

        [DreamProc("ispath")]
        [DreamProcParameter("Val")]
        [DreamProcParameter("Type", Type = DreamValueType.DreamType)]
        public static DreamValue NativeProc_ispath(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue value = arguments.GetArgument(0, "Val");
            DreamValue type = arguments.GetArgument(1, "Type");

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
        public static DreamValue NativeProc_istext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue value = arguments.GetArgument(0, "Val");

            return new DreamValue((value.Type == DreamValueType.String) ? 1 : 0);
        }

        [DreamProc("isturf")]
        [DreamProcParameter("Loc1", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_isturf(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            var argEnumerator = arguments.AllArgumentsEnumerator();

            while (argEnumerator.MoveNext()) {
                if (!argEnumerator.Current.TryGetValueAsDreamObjectOfType(ObjectTree.Turf, out _))
                    return DreamValue.False;
            }

            return DreamValue.True;
        }

        private static DreamValue CreateValueFromJsonElement(JsonElement jsonElement) {
            switch (jsonElement.ValueKind) {
                case JsonValueKind.Array: {
                    DreamList list = ObjectTree.CreateList(jsonElement.GetArrayLength());

                    foreach (JsonElement childElement in jsonElement.EnumerateArray()) {
                        DreamValue value = CreateValueFromJsonElement(childElement);

                        list.AddValue(value);
                    }

                    return new DreamValue(list);
                }
                case JsonValueKind.Object: {
                    DreamList list = ObjectTree.CreateList();

                    foreach (JsonProperty childProperty in jsonElement.EnumerateObject()) {
                        DreamValue value = CreateValueFromJsonElement(childProperty.Value);

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

            if (value.TryGetValueAsFloat(out float floatValue))
                writer.WriteNumberValue(floatValue);
            else if (value.TryGetValueAsString(out var text))
                writer.WriteStringValue(text);
            else if (value.TryGetValueAsType(out var type))
                writer.WriteStringValue(HttpUtility.JavaScriptStringEncode(type.Path.PathString));
            else if (value.TryGetValueAsDreamList(out var list)) {
                if (list.IsAssociative) {
                    writer.WriteStartObject();

                    foreach (DreamValue listValue in list.GetValues()) {
                        var key = HttpUtility.JavaScriptStringEncode(listValue.Stringify());

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
                else if (dreamObject.IsSubtypeOf(ObjectTree.Matrix)) { // Special behaviour for /matrix values
                    writer.WriteStartArray();

                    foreach (var f in DreamMetaObjectMatrix.EnumerateMatrix(dreamObject)) {
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
        [DreamProcParameter("JSON", Type = DreamValueType.String)]
        public static DreamValue NativeProc_json_decode(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (!arguments.GetArgument(0, "JSON").TryGetValueAsString(out var jsonString)) {
                throw new Exception("Unknown value");
            }

            JsonElement jsonRoot = JsonSerializer.Deserialize<JsonElement>(jsonString);

            return CreateValueFromJsonElement(jsonRoot);
        }

        [DreamProc("json_encode")]
        [DreamProcParameter("Value")]
        public static DreamValue NativeProc_json_encode(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            using MemoryStream stream = new MemoryStream();
            using Utf8JsonWriter jsonWriter = new(stream, new JsonWriterOptions {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // "\"" instead of "\u0022"
            });

            JsonEncode(jsonWriter, arguments.GetArgument(0, "Value"));
            jsonWriter.Flush();

            return new DreamValue(Encoding.UTF8.GetString(stream.AsSpan()));
        }

        private static DreamValue _length(DreamValue value, bool countBytes) {
            if (value.TryGetValueAsString(out var str)) {
                return new DreamValue(countBytes ? str.Length : str.EnumerateRunes().Count());
            } else if (value.TryGetValueAsDreamList(out var list)) {
                return new DreamValue(list.GetLength());
            } else if (value.Type is DreamValueType.Float or DreamValueType.DreamObject or DreamValueType.DreamType) {
                return new DreamValue(0);
            }

            throw new Exception($"Cannot check length of {value}");
        }

        [DreamProc("length")]
        [DreamProcParameter("E")]
        public static DreamValue NativeProc_length(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            DreamValue value = arguments.GetArgument(0, "E");
            return _length(value, true);
        }

        [DreamProc("length_char")]
        [DreamProcParameter("E")]
        public static DreamValue NativeProc_length_char(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            DreamValue value = arguments.GetArgument(0, "E");
            return _length(value, false);
        }

        [DreamProc("list2params")]
        [DreamProcParameter("List")]
        public static DreamValue NativeProc_list2params(DreamObject instance, DreamObject usr,
            DreamProcArguments arguments) {
            if (!arguments.GetArgument(0, "List").TryGetValueAsDreamList(out DreamList list))
                return new DreamValue(string.Empty);

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
            return new DreamValue(paramBuilder.ToString());
        }

        [DreamProc("log")]
        [DreamProcParameter("X", Type = DreamValueType.Float)]
        [DreamProcParameter("Y")]
        public static DreamValue NativeProc_log(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(0, "X").TryGetValueAsFloat(out float x);
            DreamValue yValue = arguments.GetArgument(1, "Y");

            if (yValue != DreamValue.Null) {
                yValue.TryGetValueAsFloat(out float y);

                return new DreamValue((float)Math.Log(y, x));
            } else {
                return new DreamValue(Math.Log(x));
            }
        }

        [DreamProc("lowertext")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        public static DreamValue NativeProc_lowertext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            var arg = arguments.GetArgument(0, "T");
            if (!arg.TryGetValueAsString(out var text))
            {
                return arg;
            }

            return new DreamValue(text.ToLower());
        }

        [DreamProc("max")]
        [DreamProcParameter("A")]
        public static DreamValue NativeProc_max(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            IEnumerator<DreamValue> values;

            if (arguments.ArgumentCount == 1) {
                DreamValue arg = arguments.GetArgument(0, "A");
                if (!arg.TryGetValueAsDreamList(out var list))
                    return arg;

                values = list.GetValues().GetEnumerator();
            } else {
                values = arguments.AllArgumentsEnumerator();
            }

            if (!values.MoveNext())
                return DreamValue.Null;

            DreamValue max = values.Current;

            while (values.MoveNext()) {
                DreamValue value = values.Current;

                if (value.TryGetValueAsFloat(out var lFloat)) {
                    if (max == DreamValue.Null && lFloat >= 0)
                        max = value;
                    else if (max.TryGetValueAsFloat(out var rFloat) && lFloat > rFloat)
                        max = value;
                } else if (value == DreamValue.Null) {
                    if (max.TryGetValueAsFloat(out var maxFloat) && maxFloat <= 0)
                        max = value;
                } else if (value.TryGetValueAsString(out var lString)) {
                    if (max == DreamValue.Null)
                        max = value;
                    else if (max.TryGetValueAsString(out var rString) && string.Compare(lString, rString, StringComparison.Ordinal) > 0)
                        max = value;
                } else {
                    throw new Exception($"Cannot compare {max} and {value}");
                }
            }

            values.Dispose();
            return max;
        }

        [DreamProc("md5")]
        [DreamProcParameter("T", Type = DreamValueType.String | DreamValueType.DreamResource)]
        public static DreamValue NativeProc_md5(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if(arguments.ArgumentCount > 1) throw new Exception("md5() only takes one argument");
            DreamValue arg = arguments.GetArgument(0, "T");

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

        [DreamProc("min")]
        [DreamProcParameter("A")]
        public static DreamValue NativeProc_min(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            IEnumerator<DreamValue> values;

            if (arguments.ArgumentCount == 1) {
                DreamValue arg = arguments.GetArgument(0, "A");
                if (!arg.TryGetValueAsDreamList(out var list))
                    return arg;

                values = list.GetValues().GetEnumerator();
            } else {
                values = arguments.AllArgumentsEnumerator();
            }

            if (!values.MoveNext())
                return DreamValue.Null;

            DreamValue min = values.Current;

            while (values.MoveNext()) {
                DreamValue value = values.Current;

                if (value.TryGetValueAsFloat(out var lFloat) && min.TryGetValueAsFloat(out var rFloat)) {
                    if (lFloat < rFloat) min = value;
                } else if (value.TryGetValueAsString(out var lString) && min.TryGetValueAsString(out var rString)) {
                    if (string.Compare(lString, rString, StringComparison.Ordinal) < 0) min = value;
                } else if (value == DreamValue.Null) {
                    min = value;
                    break;
                } else {
                    throw new Exception($"Cannot compare {min} and {value}");
                }
            }

            values.Dispose();
            return min;
        }

        [DreamProc("nonspantext")]
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needles", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        public static DreamValue NativeProc_nonspantext(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            if (!arguments.GetArgument(0, "Haystack").TryGetValueAsString(out var text))
            {
                return new DreamValue(0);
            }

            if (!arguments.GetArgument(1, "Needles").TryGetValueAsString(out var needles))
            {
                return new DreamValue(1);
            }

            int start = (int)arguments.GetArgument(2, "Start").GetValueAsFloat();

            if (start == 0 || start > text.Length) return new DreamValue(0);

            if (start < 0)
            {
                start += text.Length + 1;
            }
            var index = text.AsSpan(start - 1).IndexOfAny(needles);
            if (index == -1)
            {
                index = text.Length - start + 1;
            }

            return new DreamValue(index);
        }

        [DreamProc("num2text")]
        [DreamProcParameter("N")]
        [DreamProcParameter("Digits", Type = DreamValueType.Float)]
        [DreamProcParameter("Radix", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_num2text(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue number = arguments.GetArgument(0, "N");

            if (number.TryGetValueAsFloat(out float floatValue)) {
                return new DreamValue(floatValue.ToString(CultureInfo.InvariantCulture));
            } else {
                return new DreamValue("0");
            }
        }

        [DreamProc("orange")]
        [DreamProcParameter("Dist", Type = DreamValueType.Float, DefaultValue = 5)]
        [DreamProcParameter("Center", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_orange(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            (DreamObject center, ViewRange range) = DreamProcNativeHelpers.ResolveViewArguments(usr, arguments);
            if (center is null)
                return DreamValue.Null; // NOTE: Not sure if parity
            DreamList rangeList = ObjectTree.CreateList(range.Height * range.Width);
            foreach (DreamObject turf in DreamProcNativeHelpers.MakeViewSpiral(center, range)) {
                rangeList.AddValue(new DreamValue(turf));
                if (turf.GetVariable("contents").TryGetValueAsDreamList(out var contentsList)) {
                    foreach (DreamValue content in contentsList.GetValues()) {
                        rangeList.AddValue(content);
                    }
                }
            }
            return new DreamValue(rangeList);
        }

        [DreamProc("oview")]
        [DreamProcParameter("Dist", Type = DreamValueType.Float, DefaultValue = 5)]
        [DreamProcParameter("Center", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_oview(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            (DreamObject center, ViewRange range) = DreamProcNativeHelpers.ResolveViewArguments(usr, arguments);
            if (center is null)
                return DreamValue.Null; // NOTE: Not sure if parity

            DreamList view = ObjectTree.CreateList(range.Height * range.Width); // Should be a reasonable approximation for the list size.
            foreach (DreamObject turf in DreamProcNativeHelpers.MakeViewSpiral(center, range)) {
                if(!DreamProcNativeHelpers.IsObjectVisible(turf,center)) { //NOTE: I'm assuming here that a turf being invisible means its contents are, too
                    continue;
                }
                view.AddValue(new DreamValue(turf));
                if(turf.GetVariable("contents").TryGetValueAsDreamList(out var contentsList)) {
                    foreach (DreamValue content in contentsList.GetValues()) {
                        if (content.TryGetValueAsDreamObject(out DreamObject contentObject)) {
                            if (!DreamProcNativeHelpers.IsObjectVisible(contentObject, center)) {
                                continue;
                            }
                        }
                        view.AddValue(content);
                    }
                }
            }
            return new DreamValue(view);
        }

        [DreamProc("oviewers")]
        [DreamProcParameter("Depth", Type = DreamValueType.Float)]
        [DreamProcParameter("Center", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_oviewers(DreamObject instance, DreamObject usr, DreamProcArguments arguments) { //TODO: View obstruction (dense turfs)
            DreamValue depthValue = new DreamValue(5);
            DreamObject center = usr;

            //Arguments are optional and can be passed in any order
            if (arguments.ArgumentCount > 0) {
                DreamValue firstArgument = arguments.GetArgument(0, "Depth");

                if (firstArgument.Type == DreamValueType.DreamObject) {
                    center = firstArgument.GetValueAsDreamObject();

                    if (arguments.ArgumentCount > 1) {
                        depthValue = arguments.GetArgument(1, "Center");
                    }
                } else {
                    depthValue = firstArgument;

                    if (arguments.ArgumentCount > 1) {
                        center = arguments.GetArgument(1, "Center").GetValueAsDreamObject();
                    }
                }
            }

            DreamList view = ObjectTree.CreateList();
            int depth = (depthValue.Type == DreamValueType.Float) ? depthValue.GetValueAsInteger() : 5; //TODO: Default to world.view
            int centerX = center.GetVariable("x").GetValueAsInteger();
            int centerY = center.GetVariable("y").GetValueAsInteger();

            foreach (DreamObject mob in DreamManager.Mobs) {
                int mobX = mob.GetVariable("x").GetValueAsInteger();
                int mobY = mob.GetVariable("y").GetValueAsInteger();

                if (mobX == centerX && mobY == centerY) continue;

                if (Math.Abs(centerX - mobX) <= depth && Math.Abs(centerY - mobY) <= depth) {
                    view.AddValue(new DreamValue(mob));
                }
            }

            return new DreamValue(view);
        }

        public static DreamList params2list(string queryString) {
            queryString = queryString.Replace(";", "&");
            NameValueCollection query = HttpUtility.ParseQueryString(queryString);
            DreamList list = ObjectTree.CreateList();

            foreach (string queryKey in query.AllKeys) {
                string[] queryValues = query.GetValues(queryKey);
                string queryValue = queryValues[^1]; //Use the last appearance of the key in the query

                if (queryKey != null) {
                    list.SetValue(new DreamValue(queryKey), new DreamValue(queryValue));
                } else {
                    list.AddValue(new DreamValue(queryValue));
                }
            }

            return list;
        }

        [DreamProc("params2list")]
        [DreamProcParameter("Params", Type = DreamValueType.String)]
        public static DreamValue NativeProc_params2list(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue paramsValue = arguments.GetArgument(0, "Params");
            DreamList result;

            if (paramsValue.TryGetValueAsString(out string paramsString)) {
                result = params2list(paramsString);
            } else {
                result = ObjectTree.CreateList();
            }

            return new DreamValue(result);
        }

        [DreamProc("rand")]
        [DreamProcParameter("L", Type = DreamValueType.Float)]
        [DreamProcParameter("H", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_rand(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (arguments.ArgumentCount == 0) {
                return new DreamValue(DreamManager.Random.NextSingle());
            } else if (arguments.ArgumentCount == 1) {
                arguments.GetArgument(0, "L").TryGetValueAsInteger(out var high);

                return new DreamValue(DreamManager.Random.Next(high));
            } else {
                arguments.GetArgument(0, "L").TryGetValueAsInteger(out var low);
                arguments.GetArgument(1, "H").TryGetValueAsInteger(out var high);

                return new DreamValue(DreamManager.Random.Next(Math.Min(low, high), Math.Max(low, high)));
            }
        }

        [DreamProc("rand_seed")]
        [DreamProcParameter("Seed", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_rand_seed(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(0, "Seed").TryGetValueAsInteger(out var seed);

            DreamManager.Random = new Random(seed);
            return DreamValue.Null;
        }

        [DreamProc("range")]
        [DreamProcParameter("Dist", Type = DreamValueType.Float, DefaultValue = 5)]
        [DreamProcParameter("Center", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_range(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            (DreamObject center, ViewRange range) = DreamProcNativeHelpers.ResolveViewArguments(usr, arguments);
            if (center is null)
                return DreamValue.Null; // NOTE: Not sure if parity
            DreamList rangeList = ObjectTree.CreateList(range.Height * range.Width);
            //Have to include centre
            rangeList.AddValue(new DreamValue(center));
            if(center.TryGetVariable("contents", out var centerContents) && centerContents.TryGetValueAsDreamList(out var centerContentsList)) {
                foreach(DreamValue content in centerContentsList.GetValues()) {
                    rangeList.AddValue(content);
                }
            }
            if(!center.IsSubtypeOf(ObjectTree.Turf)) { // If it's not a /turf, we have to include its loc and the loc's contents
                if(center.TryGetVariable("loc",out DreamValue centerLoc) && centerLoc.TryGetValueAsDreamObject(out DreamObject centerLocObject)) {
                    rangeList.AddValue(centerLoc);
                    if(centerLocObject.GetVariable("contents").TryGetValueAsDreamList(out var locContentsList)) {
                        foreach (DreamValue content in locContentsList.GetValues()) {
                            rangeList.AddValue(content);
                        }
                    }
                }
            }
            //And then everything else
            foreach (DreamObject turf in DreamProcNativeHelpers.MakeViewSpiral(center, range)) {
                rangeList.AddValue(new DreamValue(turf));
                if (turf.GetVariable("contents").TryGetValueAsDreamList(out var contentsList)) {
                    foreach (DreamValue content in contentsList.GetValues()) {
                        rangeList.AddValue(content);
                    }
                }
            }
            return new DreamValue(rangeList);
        }

        [DreamProc("ref")]
        [DreamProcParameter("Object", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_ref(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            return new DreamValue(DreamManager.CreateRef(arguments.GetArgument(0, "Object")));
        }

        [DreamProc("regex")]
        [DreamProcParameter("pattern", Type = DreamValueType.String | DreamValueType.DreamObject)]
        [DreamProcParameter("flags", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_regex(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            var patternOrRegex = arguments.GetArgument(0, "pattern");
            var flags = arguments.GetArgument(1, "flags");
            if (flags.TryGetValueAsInteger(out var specialMode) && patternOrRegex.TryGetValueAsString(out var text))
            {
                switch(specialMode)
                {
                    case 1:
                        return new DreamValue(Regex.Escape(text));
                    case 2:
                        return new DreamValue(text.Replace("$", "$$"));
                };
            }
            var newRegex = ObjectTree.CreateObject(ObjectTree.Regex);
            newRegex.InitSpawn(arguments);
            return new DreamValue(newRegex);
        }

        [DreamProc("replacetext")]
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needle", Type = DreamValueType.String)]
        [DreamProcParameter("Replacement", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static async Task<DreamValue> NativeProc_replacetext(AsyncNativeProc.State state) {
            DreamValue haystack = state.Arguments.GetArgument(0, "Haystack");
            DreamValue needle = state.Arguments.GetArgument(1, "Needle");
            DreamValue replacementArg = state.Arguments.GetArgument(2, "Replacement");
            int start = state.Arguments.GetArgument(3, "Start").GetValueAsInteger(); //1-indexed
            int end = state.Arguments.GetArgument(4, "End").GetValueAsInteger(); //1-indexed

            if (needle.TryGetValueAsDreamObjectOfType(ObjectTree.Regex, out var regexObject)) {
                // According to the docs, this is the same as /regex.Replace()
                return await DreamProcNativeRegex.RegexReplace(state, regexObject, haystack, replacementArg, start, end);
            }

            if (!haystack.TryGetValueAsString(out var text)) {
                return DreamValue.Null;
            }

            var arg3 = replacementArg.TryGetValueAsString(out var replacement);

            if (end == 0) {
                end = text.Length + 1;
            }

            if (needle == DreamValue.Null) { // Insert the replacement after each char except the last
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

        [DreamProc("rgb")]
        [DreamProcParameter("R", Type = DreamValueType.Float)]
        [DreamProcParameter("G", Type = DreamValueType.Float)]
        [DreamProcParameter("B", Type = DreamValueType.Float)]
        [DreamProcParameter("A", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_rgb(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(0, "R").TryGetValueAsInteger(out var r);
            arguments.GetArgument(1, "G").TryGetValueAsInteger(out var g);
            arguments.GetArgument(2, "B").TryGetValueAsInteger(out var b);
            DreamValue aValue = arguments.GetArgument(3, "A");

            // TODO: There is a difference between passing null and not passing a fourth arg at all
            // Likely a compile-time difference
            if (aValue == DreamValue.Null) {
                return new DreamValue($"#{r:X2}{g:X2}{b:X2}");
            } else {
                aValue.TryGetValueAsInteger(out var a);

                return new DreamValue($"#{r:X2}{g:X2}{b:X2}{a:X2}");
            }
        }

        [DreamProc("rgb2num")]
        [DreamProcParameter("color", Type = DreamValueType.String)]
        [DreamProcParameter("space", Type = DreamValueType.Float, DefaultValue = 0)] // Same value as COLORSPACE_RGB
        public static DreamValue NativeProc_rgb2num(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if(!arguments.GetArgument(0, "color").TryGetValueAsString(out var color))
            {
                throw new Exception("bad color");
            }

            if (!arguments.GetArgument(1, "space").TryGetValueAsInteger(out var space)) {
                throw new NotImplementedException($"Failed to parse colorspace {arguments.GetArgument(1, "space")}");
            }

            if (!ColorHelpers.TryParseColor(color, out var c, defaultAlpha: null)) {
                throw new Exception("bad color");
            }

            DreamList list = ObjectTree.CreateList();

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
                case 3: //hcy
                    /// TODO Figure out why the chroma for #ca60db is 48 instead of 68
                    throw new NotImplementedException("HCY Colorspace is not implemented");
                    /*
                    Vector4 hcycolor = Color.ToHcy(c);
                    list.AddValue(new DreamValue(hcycolor.X * 360));
                    list.AddValue(new DreamValue(hcycolor.Y * 100));
                    list.AddValue(new DreamValue(hcycolor.Z * 100));
                    */
                default:
                    throw new NotImplementedException($"Colorspace {space} is not implemented");
            }

            if (color.Length == 9 || color.Length == 5) {
                list.AddValue(new DreamValue(c.AByte));
            }

            return new DreamValue(list);
        }

        [DreamProc("replacetextEx")]
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needle", Type = DreamValueType.String)]
        [DreamProcParameter("Replacement", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_replacetextEx(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (!arguments.GetArgument(0, "Haystack").TryGetValueAsString(out var text))
            {
                return DreamValue.Null;
            }

            var arg3 = arguments.GetArgument(2, "Replacement").TryGetValueAsString(out var replacement);

            if (!arguments.GetArgument(1, "Needle").TryGetValueAsString(out var needle))
            {
                if (!arg3)
                {
                    return new DreamValue(text);
                }

                //Insert the replacement after each char except the last char
                //TODO: Properly support non-default start/end values
                StringBuilder result = new StringBuilder();
                var pos = 0;
                while (pos + 1 <= text.Length)
                {
                    result.Append(text[pos]).Append(arg3);
                    pos += 1;
                }
                result.Append(text[pos]);
                return new DreamValue(result.ToString());
            }

            int start = arguments.GetArgument(3, "Start").GetValueAsInteger(); //1-indexed
            int end = arguments.GetArgument(4, "End").GetValueAsInteger(); //1-indexed

            if (end == 0) {
                end = text.Length + 1;
            }

            return new DreamValue(text.Substring(start - 1, end - start).Replace(needle, replacement, StringComparison.Ordinal));
        }

        [DreamProc("round")]
        [DreamProcParameter("A", Type = DreamValueType.Float)]
        [DreamProcParameter("B", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_round(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(0, "A").TryGetValueAsFloat(out var a);

            if (arguments.ArgumentCount == 1) {
                return new DreamValue((float)Math.Floor(a));
            } else {
                arguments.GetArgument(1, "B").TryGetValueAsFloat(out var b);

                return new DreamValue((float)Math.Round(a / b) * b);
            }
        }

        [DreamProc("roll")]
        [DreamProcParameter("ndice", Type = DreamValueType.Float | DreamValueType.String)]
        [DreamProcParameter("sides", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_roll(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            int dice = 1;
            int sides;
            int modifier = 0;
            if (arguments.ArgumentCount == 1) {
                if(!arguments.GetArgument(0, "ndice").TryGetValueAsString(out var diceInput))
                {
                    return new DreamValue(1);
                }
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
            } else if (!arguments.GetArgument(0, "ndice").TryGetValueAsInteger(out dice) || !arguments.GetArgument(1, "sides").TryGetValueAsInteger(out sides)) {
                return new DreamValue(0);
            }
            float total = modifier; // Adds the modifier to start with
            for (int i = 0; i < dice; i++) {
                total += DreamManager.Random.Next(1, sides + 1);
            }

            return new DreamValue(total);
        }
        [DreamProc("sha1")]
        [DreamProcParameter("T", Type = DreamValueType.String | DreamValueType.DreamResource)]
        public static DreamValue NativeProc_sha1(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            if (arguments.ArgumentCount > 1) throw new Exception("sha1() only takes one argument");
            DreamValue arg = arguments.GetArgument(0, "T");
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

            SHA1 sha1 = SHA1.Create();
            byte[] output = sha1.ComputeHash(bytes);
            //Match BYOND formatting
            string hash = BitConverter.ToString(output).Replace("-", "").ToLower();
            return new DreamValue(hash);
        }

        [DreamProc("shutdown")]
        [DreamProcParameter("Addr", Type = DreamValueType.String | DreamValueType.DreamObject)]
        [DreamProcParameter("Natural", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_shutdown(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue addrValue = arguments.GetArgument(0, "Addr");

            if (addrValue == DreamValue.Null) {
                IoCManager.Resolve<ITaskManager>().RunOnMainThread(() => {
                    IoCManager.Resolve<IBaseServer>().Shutdown("shutdown() was called from DM code");
                });
            } else {
                throw new NotImplementedException();
            }

            return DreamValue.Null;
        }

        [DreamProc("sin")]
        [DreamProcParameter("X", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_sin(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(0, "X").TryGetValueAsFloat(out var x);
            double rad = x * (Math.PI / 180);

            return new DreamValue((float)Math.Sin(rad));
        }

        [DreamProc("sleep")]
        [DreamProcParameter("Delay", Type = DreamValueType.Float)]
        public static async Task<DreamValue> NativeProc_sleep(AsyncNativeProc.State state) {
            state.Arguments.GetArgument(0, "Delay").TryGetValueAsFloat(out float delay);
            int delayMilliseconds = (int)(delay * 100);

            // TODO: This may not be the proper behaviour, see https://www.byond.com/docs/ref/#/proc/sleep
            // sleep(0) should sleep for the minimum amount of time possible, whereas
            // sleep called with a negative value should do a backlog check, meaning it only sleeps
            // when other events are backlogged
            if (delayMilliseconds > 0) {
                await Task.Delay(delayMilliseconds);
            } else {
                // TODO: This postpones execution until the next tick.
                // It should instead start again in the current tick if possible.
                await Task.Yield();
            }

            return DreamValue.Null;
        }

        [DreamProc("sorttext")]
        [DreamProcParameter("T1", Type = DreamValueType.String)]
        [DreamProcParameter("T2", Type = DreamValueType.String)]
        public static DreamValue NativeProc_sorttext(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            string t2;
            if (!arguments.GetArgument(0, "T1").TryGetValueAsString(out var t1))
            {
                if (!arguments.GetArgument(1, "T2").TryGetValueAsString(out _))
                {
                    return new DreamValue(0);
                }

                return new DreamValue(1);
            } else if (!arguments.GetArgument(1, "T2").TryGetValueAsString(out t2))
            {
                return new DreamValue(-1);
            }

            int comparison = string.Compare(t2, t1, StringComparison.OrdinalIgnoreCase);
            int clamped = Math.Max(Math.Min(comparison, 1), -1); //Clamp return value between -1 and 1
            return new DreamValue(clamped);
        }

        [DreamProc("sorttextEx")]
        [DreamProcParameter("T1", Type = DreamValueType.String)]
        [DreamProcParameter("T2", Type = DreamValueType.String)]
        public static DreamValue NativeProc_sorttextEx(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string t2;
            if (!arguments.GetArgument(0, "T1").TryGetValueAsString(out var t1))
            {
                if (!arguments.GetArgument(1, "T2").TryGetValueAsString(out _))
                {
                    return new DreamValue(0);
                }

                return new DreamValue(1);
            } else if (!arguments.GetArgument(1, "T2").TryGetValueAsString(out t2))
            {
                return new DreamValue(-1);
            }

            int comparison = string.Compare(t2, t1, StringComparison.Ordinal);
            int clamped = Math.Max(Math.Min(comparison, 1), -1); //Clamp return value between -1 and 1
            return new DreamValue(clamped);
        }

        [DreamProc("spantext")]
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needles", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        public static DreamValue NativeProc_spantext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            //if any arguments are bad, return 0
            if (!arguments.GetArgument(0, "Haystack").TryGetValueAsString(out var text) ||
                !arguments.GetArgument(1, "Needles").TryGetValueAsString(out var needles) ||
                !arguments.GetArgument(2, "Start").TryGetValueAsInteger(out var start) ||
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
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needles", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        public static DreamValue NativeProc_spantext_char(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            //if any arguments are bad, return 0
            if (!arguments.GetArgument(0, "Haystack").TryGetValueAsString(out var text) ||
                !arguments.GetArgument(1, "Needles").TryGetValueAsString(out var needles) ||
                !arguments.GetArgument(2, "Start").TryGetValueAsInteger(out var start) ||
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
        [DreamProcParameter("file", Type = DreamValueType.DreamResource)]
        [DreamProcParameter("repeat", Type = DreamValueType.Float, DefaultValue = 0)]
        [DreamProcParameter("wait", Type = DreamValueType.Float)]
        [DreamProcParameter("channel", Type = DreamValueType.Float)]
        [DreamProcParameter("volume", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_sound(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamObject soundObject = ObjectTree.CreateObject(ObjectTree.Sound);
            soundObject.InitSpawn(arguments);
            return new DreamValue(soundObject);
        }

        [DreamProc("splicetext")]
        [DreamProcParameter("Text", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        [DreamProcParameter("Insert", Type = DreamValueType.String, DefaultValue = "")]
        public static DreamValue NativeProc_splicetext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(0, "Text").TryGetValueAsString(out var text);
            arguments.GetArgument(1, "Start").TryGetValueAsInteger(out var start);
            arguments.GetArgument(2, "End").TryGetValueAsInteger(out var end);
            arguments.GetArgument(3, "Insert").TryGetValueAsString(out var insertText);

            if(text == null)
                if(String.IsNullOrEmpty(insertText))
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
        [DreamProcParameter("Text", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        [DreamProcParameter("Insert", Type = DreamValueType.String, DefaultValue = "")]
        public static DreamValue NativeProc_splicetext_char(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(0, "Text").TryGetValueAsString(out var text);
            arguments.GetArgument(1, "Start").TryGetValueAsInteger(out var start);
            arguments.GetArgument(2, "End").TryGetValueAsInteger(out var end);
            arguments.GetArgument(3, "Insert").TryGetValueAsString(out var insertText);

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
        [DreamProcParameter("Text", Type = DreamValueType.String)]
        [DreamProcParameter("Delimiter", Type = DreamValueType.String)]
        public static DreamValue NativeProc_splittext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (!arguments.GetArgument(0, "Text").TryGetValueAsString(out var text)) {
                return new DreamValue(ObjectTree.CreateList());
            }

            var arg2 = arguments.GetArgument(1, "Delimiter");
            if (!arg2.TryGetValueAsString(out var delimiter)) {
                if (!arg2.Equals(DreamValue.Null)) {
                    return new DreamValue(ObjectTree.CreateList());
                }
            }

            string[] splitText = text.Split(delimiter);
            DreamList list = ObjectTree.CreateList(splitText);

            return new DreamValue(list);
        }

        [DreamProc("sqrt")]
        [DreamProcParameter("A", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_sqrt(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(0, "A").TryGetValueAsFloat(out var a);

            return new DreamValue((float)Math.Sqrt(a));
        }

        private static void OutputToStatPanel(DreamConnection connection, DreamValue name, DreamValue value) {
            if (name != DreamValue.Null) {
                connection.AddStatPanelLine(name.Stringify() + "\t" + value.Stringify());
            } else {
                connection.AddStatPanelLine(value.Stringify());
            }
        }

        [DreamProc("stat")]
        [DreamProcParameter("Name")]
        [DreamProcParameter("Value")]
        public static DreamValue NativeProc_stat(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue name = arguments.GetArgument(0, "Name");
            DreamValue value = arguments.GetArgument(1, "Value");
            DreamConnection connection = DreamManager.GetConnectionFromMob(usr);

            OutputToStatPanel(connection, name, value);
            return DreamValue.Null;
        }

        [DreamProc("statpanel")]
        [DreamProcParameter("Panel", Type = DreamValueType.String)]
        [DreamProcParameter("Name")]
        [DreamProcParameter("Value")]
        public static DreamValue NativeProc_statpanel(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string panel = arguments.GetArgument(0, "Panel").GetValueAsString();
            DreamValue name = arguments.GetArgument(1, "Name");
            DreamValue value = arguments.GetArgument(2, "Value");
            DreamConnection connection = DreamManager.GetConnectionFromMob(usr);

            connection.SetOutputStatPanel(panel);
            if (name != DreamValue.Null || value != DreamValue.Null) {
                OutputToStatPanel(connection, name, value);
            }

            return new DreamValue(connection.SelectedStatPanel == panel ? 1 : 0);
        }

        [DreamProc("tan")]
        [DreamProcParameter("X", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_tan(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(0, "X").TryGetValueAsFloat(out var x);
            double rad = x * (Math.PI / 180);

            return new DreamValue((float)Math.Tan(rad));
        }

        [DreamProc("text2ascii")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        [DreamProcParameter("pos", Type = DreamValueType.Float, DefaultValue = 1)]
        public static DreamValue NativeProc_text2ascii(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if(!arguments.GetArgument(0, "T").TryGetValueAsString(out var text))
            {
                return new DreamValue(0);
            }

            arguments.GetArgument(1, "pos").TryGetValueAsInteger(out var pos); //1-indexed
            if (pos == 0) pos = 1; //0 is same as 1
            else if (pos < 0) pos += text.Length + 1; //Wraps around

            if (pos > text.Length || pos < 1) {
                return new DreamValue(0);
            } else {
                return new DreamValue((int)text[pos - 1]);
            }
        }

        [DreamProc("text2file")]
        [DreamProcParameter("Text", Type = DreamValueType.String)]
        [DreamProcParameter("File", Type = DreamValueType.String)]
        public static DreamValue NativeProc_text2file(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (!arguments.GetArgument(0, "Text").TryGetValueAsString(out var text))
            {
                text = string.Empty;
            }
            if(!arguments.GetArgument(1, "File").TryGetValueAsString(out var file))
            {
                return new DreamValue(0);
            }

            var resourceManager = IoCManager.Resolve<DreamResourceManager>();
            return new DreamValue(resourceManager.SaveTextToFile(file, text) ? 1 : 0);
        }

        [DreamProc("text2num")]
        [DreamProcParameter("T", Type = DreamValueType.String | DreamValueType.Float | DreamValueType.DreamObject)]
        [DreamProcParameter("radix", Type = DreamValueType.Float, DefaultValue = 10)]
        public static DreamValue NativeProc_text2num(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue value = arguments.GetArgument(0, "T");

            if (value.TryGetValueAsString(out string text)) {
                arguments.GetArgument(1, "radix").TryGetValueAsInteger(out var radix);
                if (radix < 2)
                    throw new Exception($"Invalid radix: {radix}");

                text = text.Trim();
                if (text.Length == 0)
                    return DreamValue.Null;

                try {
                    if (radix == 10) {
                        return new DreamValue(Convert.ToSingle(text, CultureInfo.InvariantCulture));
                    } else {
                        return new DreamValue(Convert.ToInt32(text, radix));
                    }
                } catch (FormatException) {
                    return DreamValue.Null; //No digits, return null
                }
            } else if (value.Type == DreamValueType.Float) {
                return value;
            } else if (value == DreamValue.Null) {
                return DreamValue.Null;
            } else {
                throw new Exception($"Invalid argument to text2num: {value}");
            }
        }

        [DreamProc("text2path")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        public static DreamValue NativeProc_text2path(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (!arguments.GetArgument(0, "T").TryGetValueAsString(out var text)) {
                return DreamValue.Null;
            }

            DreamPath path = new DreamPath(text);
            if (path.FindElement("proc") != -1 || path.FindElement("verb") != -1)
                throw new NotImplementedException("text2path() for procs is not implemented");

            if (ObjectTree.TryGetTreeEntry(path, out var type)) {
                return new DreamValue(type);
            } else {
                return DreamValue.Null;
            }
        }

        [DreamProc("time2text")]
        [DreamProcParameter("timestamp", Type = DreamValueType.Float)]
        [DreamProcParameter("format", Type = DreamValueType.String)]
        [DreamProcParameter("timezone", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_time2text(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            bool hasTimezoneOffset = arguments.GetArgument(2, "timezone").TryGetValueAsFloat(out float timezoneOffset);

            if (!arguments.GetArgument(0, "timestamp").TryGetValueAsFloat(out var timestamp)) {
                // TODO This copes with nulls and is a sane default, but BYOND has weird returns for strings and stuff
                DreamManager.WorldInstance.GetVariable("timeofday").TryGetValueAsFloat(out timestamp);
            }

            if (!arguments.GetArgument(1, "format").TryGetValueAsString(out var format)) {
                format = "DDD MMM DD hh:mm:ss YYYY";
            }

            long ticks = (long)(timestamp * TimeSpan.TicksPerSecond / 10);

            // The DM reference says this is 0-864000. That's wrong, it's actually a 7-day range instead of 1
            if (timestamp >= 0 && timestamp < 864000*7) {
                ticks += DateTime.Today.Ticks;
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
        [DreamProcParameter("Text", Type = DreamValueType.String)]
        public static DreamValue NativeProc_trimtext(DreamObject instance, DreamObject usr,
            DreamProcArguments arguments)
        {
            return arguments.GetArgument(0, "Text").TryGetValueAsString(out var val) ? new DreamValue(val.Trim()) : DreamValue.Null;
        }

        [DreamProc("trunc")]
        [DreamProcParameter("n", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_trunc(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue arg = arguments.GetArgument(0, "n");
            if (arg.TryGetValueAsFloat(out float floatnum)) {
                return new DreamValue(MathF.Truncate(floatnum));
            }
            return new DreamValue(0);
        }

        [DreamProc("typesof")]
        [DreamProcParameter("Item1", Type = DreamValueType.DreamType | DreamValueType.DreamObject | DreamValueType.ProcStub | DreamValueType.VerbStub)]
        public static DreamValue NativeProc_typesof(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamList list = ObjectTree.CreateList(arguments.ArgumentCount); // Assume every arg will add at least one type
            var argEnumerator = arguments.AllArgumentsEnumerator();

            while (argEnumerator.MoveNext()) {
                DreamValue typeValue = argEnumerator.Current;
                IEnumerable<int>? addingProcs = null;

                if (!typeValue.TryGetValueAsType(out var type)) {
                    if (typeValue.TryGetValueAsDreamObject(out var typeObj)) {
                        if (typeObj is null or DreamList) // typesof() ignores nulls and lists
                            continue;

                        type = typeObj.ObjectDefinition.TreeEntry;
                    } else if (typeValue.TryGetValueAsString(out var typeString)) {
                        DreamPath path = new DreamPath(typeString);

                        if (path.LastElement is "proc" or "verb") {
                            type = ObjectTree.GetTreeEntry(path.FromElements(0, -2));
                            addingProcs = type.ObjectDefinition.Procs.Values;
                        } else {
                            type = ObjectTree.GetTreeEntry(path);
                        }
                    } else if (typeValue.TryGetValueAsProcStub(out var owner)) {
                        type = owner;
                        addingProcs = type.ObjectDefinition.Procs.Values;
                    } else if (typeValue.TryGetValueAsVerbStub(out owner)) {
                        type = owner;
                        addingProcs = type.ObjectDefinition.Verbs;
                    } else {
                        continue;
                    }
                }

                if (addingProcs != null) {
                    foreach (var procId in addingProcs) {
                        list.AddValue(new DreamValue(ObjectTree.Procs[procId]));
                    }
                } else {
                    var descendants = ObjectTree.GetAllDescendants(type);

                    foreach (var descendant in descendants) {
                        list.AddValue(new DreamValue(descendant));
                    }
                }
            }

            return new DreamValue(list);
        }

        [DreamProc("uppertext")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        public static DreamValue NativeProc_uppertext(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            var arg = arguments.GetArgument(0, "T");
            if (!arg.TryGetValueAsString(out var text))
            {
                return arg;
            }

            return new DreamValue(text.ToUpper());
        }

        [DreamProc("url_decode")]
        [DreamProcParameter("UrlText", Type = DreamValueType.String)]
        public static DreamValue NativeProc_url_decode(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (!arguments.GetArgument(0, "UrlText").TryGetValueAsString(out var urlText))
            {
                return new DreamValue("");
            }

            return new DreamValue(HttpUtility.UrlDecode(urlText));
        }

        [DreamProc("url_encode")]
        [DreamProcParameter("PlainText", Type = DreamValueType.String)]
        [DreamProcParameter("format", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_url_encode(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string plainText = arguments.GetArgument(0, "PlainText").Stringify();
            arguments.GetArgument(1, "format").TryGetValueAsInteger(out var format);
            if (format != 0)
                throw new NotImplementedException("Only format 0 is supported");

            return new DreamValue(HttpUtility.UrlEncode(plainText));
        }

        [DreamProc("view")]
        [DreamProcParameter("Dist", Type = DreamValueType.Float, DefaultValue = 5)]
        [DreamProcParameter("Center", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_view(DreamObject instance, DreamObject usr, DreamProcArguments arguments) { //TODO: View obstruction (dense turfs)
            (DreamObject center, ViewRange range) = DreamProcNativeHelpers.ResolveViewArguments(usr, arguments);
            if (center is null)
                return DreamValue.Null; // NOTE: Not sure if parity
            DreamList view = ObjectTree.CreateList(range.Height * range.Width); // Should be a reasonable approximation for the list size.
            //Have to include centre
            if(DreamProcNativeHelpers.IsObjectVisible(center,center)) // NOTE: I think this is always true, but I'm not 100% sure.
                view.AddValue(new DreamValue(center));
            if (center.TryGetVariable("contents", out var centerContents) && centerContents.TryGetValueAsDreamList(out var centerContentsList)) {
                foreach (DreamValue content in centerContentsList.GetValues()) {
                    if (content.TryGetValueAsDreamObject(out DreamObject contentObject)) {
                        if (!DreamProcNativeHelpers.IsObjectVisible(contentObject, center)) {
                            continue;
                        }
                    }
                    view.AddValue(content);
                }
            }
            if (!center.IsSubtypeOf(ObjectTree.Turf)) { // If it's not a /turf, we have to include its loc and the loc's contents
                if (center.TryGetVariable("loc", out DreamValue centerLoc) && centerLoc.TryGetValueAsDreamObject(out DreamObject centerLocObject)) {
                    view.AddValue(centerLoc);
                    if (centerLocObject.GetVariable("contents").TryGetValueAsDreamList(out var locContentsList)) {
                        foreach (DreamValue content in locContentsList.GetValues()) {
                            view.AddValue(content);
                        }
                    }
                }
            }
            //and then everything else
            foreach (DreamObject turf in DreamProcNativeHelpers.MakeViewSpiral(center, range)) {
                if (!DreamProcNativeHelpers.IsObjectVisible(turf, center)) { //NOTE: I'm assuming here that a turf being invisible means its contents are, too
                    continue;
                }
                view.AddValue(new DreamValue(turf));
                if (turf.GetVariable("contents").TryGetValueAsDreamList(out var contentsList)) {
                    foreach (DreamValue content in contentsList.GetValues()) {
                        if (content.TryGetValueAsDreamObject(out DreamObject contentObject)) {
                            if (!DreamProcNativeHelpers.IsObjectVisible(contentObject, center)) {
                                continue;
                            }
                        }
                        view.AddValue(content);
                    }
                }
            }
            return new DreamValue(view);
        }

        [DreamProc("viewers")]
        [DreamProcParameter("Depth", Type = DreamValueType.Float)]
        [DreamProcParameter("Center", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_viewers(DreamObject instance, DreamObject usr, DreamProcArguments arguments) { //TODO: View obstruction (dense turfs)
            DreamValue depthValue = new DreamValue(5);
            DreamObject center = usr;

            //Arguments are optional and can be passed in any order
            if (arguments.ArgumentCount > 0) {
                DreamValue firstArgument = arguments.GetArgument(0, "Depth");

                if (firstArgument.TryGetValueAsDreamObject(out var firstObj)) {
                    center = firstObj;

                    if (arguments.ArgumentCount > 1) {
                        depthValue = arguments.GetArgument(1, "Center");
                    }
                } else {
                    depthValue = firstArgument;

                    if (arguments.ArgumentCount > 1) {
                        center = arguments.GetArgument(1, "Center").GetValueAsDreamObject();
                    }
                }
            }

            DreamList view = ObjectTree.CreateList();
            int depth = (depthValue.Type == DreamValueType.Float) ? depthValue.MustGetValueAsInteger() : 5; //TODO: Default to world.view
            int centerX = center.GetVariable("x").MustGetValueAsInteger();
            int centerY = center.GetVariable("y").MustGetValueAsInteger();

            foreach (DreamObject mob in DreamManager.Mobs) {
                int mobX = mob.GetVariable("x").MustGetValueAsInteger();
                int mobY = mob.GetVariable("y").MustGetValueAsInteger();

                if (Math.Abs(centerX - mobX) <= depth && Math.Abs(centerY - mobY) <= depth) {
                    view.AddValue(new DreamValue(mob));
                }
            }

            return new DreamValue(view);
        }

        [DreamProc("walk")]
        [DreamProcParameter("Ref", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("Dir", Type = DreamValueType.Float)]
        [DreamProcParameter("Lag", Type = DreamValueType.Float, DefaultValue = 0)]
        [DreamProcParameter("Speed", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_walk(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            //TODO: Implement walk()

            return DreamValue.Null;
        }

        [DreamProc("walk_to")]
        [DreamProcParameter("Ref", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("Trg", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("Min", Type = DreamValueType.Float, DefaultValue = 0)]
        [DreamProcParameter("Lag", Type = DreamValueType.Float, DefaultValue = 0)]
        [DreamProcParameter("Speed", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_walk_to(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            //TODO: Implement walk_to()

            return DreamValue.Null;
        }

        [DreamProc("winclone")]
        [DreamProcParameter("player", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("window_name", Type = DreamValueType.String)]
        [DreamProcParameter("clone_name", Type = DreamValueType.String)]
        public static DreamValue NativeProc_winclone(DreamObject? instance, DreamObject? usr, DreamProcArguments arguments) {
            if(!arguments.GetArgument(1, "window_name").TryGetValueAsString(out var windowName))
                return DreamValue.Null;
            if(!arguments.GetArgument(2, "clone_name").TryGetValueAsString(out var cloneName))
                return DreamValue.Null;

            DreamValue player = arguments.GetArgument(0, "player");

            DreamConnection? connection;

            if (player.TryGetValueAsDreamObjectOfType(ObjectTree.Mob, out var mob)) {
                connection = DreamManager.GetConnectionFromMob(mob);
            } else if (player.TryGetValueAsDreamObjectOfType(ObjectTree.Client, out var client)) {
                connection = DreamManager.GetConnectionFromClient(client);
            } else {
                throw new ArgumentException($"Invalid \"player\" argument {player}");
            }

            connection?.WinClone(windowName, cloneName);
            return DreamValue.Null;
        }

        [DreamProc("winexists")]
        [DreamProcParameter("player", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("control_id", Type = DreamValueType.String)]
        public static async Task<DreamValue> NativeProc_winexists(AsyncNativeProc.State state) {
            DreamValue player = state.Arguments.GetArgument(0, "player");
            if (!state.Arguments.GetArgument(1, "control_id").TryGetValueAsString(out string controlId)) {
                return new DreamValue("");
            }

            DreamConnection connection;
            if (player.TryGetValueAsDreamObjectOfType(ObjectTree.Mob, out DreamObject mob)) {
                connection = DreamManager.GetConnectionFromMob(mob);
            } else if (player.TryGetValueAsDreamObjectOfType(ObjectTree.Client, out DreamObject client)) {
                connection = DreamManager.GetConnectionFromClient(client);
            } else {
                throw new Exception($"Invalid client {player}");
            }

            return await connection.WinExists(controlId);
        }

        [DreamProc("winset")]
        [DreamProcParameter("player", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("control_id", Type = DreamValueType.String)]
        [DreamProcParameter("params", Type = DreamValueType.String)]
        public static DreamValue NativeProc_winset(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue player = arguments.GetArgument(0, "player");
            DreamValue controlId = arguments.GetArgument(1, "control_id");
            string winsetControlId = (controlId != DreamValue.Null) ? controlId.GetValueAsString() : null;
            string winsetParams = arguments.GetArgument(2, "params").GetValueAsString();
            DreamConnection connection;

            if (player.TryGetValueAsDreamObjectOfType(ObjectTree.Mob, out var mob)) {
                connection = DreamManager.GetConnectionFromMob(mob);
            } else if (player.TryGetValueAsDreamObjectOfType(ObjectTree.Client, out var client)) {
                connection = DreamManager.GetConnectionFromClient(client);
            } else {
                throw new ArgumentException($"Invalid \"player\" argument {player}");
            }

            connection.WinSet(winsetControlId, winsetParams);
            return DreamValue.Null;
        }
    }
}
