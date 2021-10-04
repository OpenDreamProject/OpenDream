using OpenDreamRuntime.Objects;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using DreamValueType = OpenDreamRuntime.DreamValue.DreamValueType;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Resources;

namespace OpenDreamRuntime.Procs.Native {
    static class DreamProcNativeRoot {
        // I don't want to edit 100 procs to have the runtime passed to them
        public static DreamRuntime CurrentRuntime => RuntimeStack.Peek();
        public static Stack<DreamRuntime> RuntimeStack = new();

        [DreamProc("abs")]
        [DreamProcParameter("A", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_abs(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            float number = arguments.GetArgument(0, "A").GetValueAsFloat();

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
            DreamObject mob;
            string message, title, button1, button2, button3;

            DreamValue usrArgument = state.Arguments.GetArgument(0, "Usr");
            if (usrArgument.TryGetValueAsDreamObjectOfType(DreamPath.Mob, out mob)) {
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

            DreamConnection connection = state.Runtime.Server.GetConnectionFromMob(mob);
            return await connection.Alert(title, message, button1, button2, button3);
        }

        [DreamProc("animate")]
        [DreamProcParameter("Object", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("time", Type = DreamValueType.Float)]
        [DreamProcParameter("loop", Type = DreamValueType.Float)]
        [DreamProcParameter("easing", Type = DreamValueType.String)]
        [DreamProcParameter("flags", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_animate(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamObject obj = arguments.GetArgument(0, "Object").GetValueAsDreamObjectOfType(DreamPath.Atom);

            if (arguments.NamedArguments.TryGetValue("pixel_x", out DreamValue pixelX)) {
                obj.SetVariable("pixel_x", pixelX);
            }

            if (arguments.NamedArguments.TryGetValue("pixel_y", out DreamValue pixelY)) {
                obj.SetVariable("pixel_y", pixelY);
            }

            if (arguments.NamedArguments.TryGetValue("dir", out DreamValue dir)) {
                obj.SetVariable("dir", dir);
            }

            if (arguments.NamedArguments.TryGetValue("transform", out DreamValue transform)) {
                obj.SetVariable("transform", transform);
            }

            return DreamValue.Null;
        }

        [DreamProc("arccos")]
        [DreamProcParameter("X", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_arccos(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue xValue = arguments.GetArgument(0, "X");
            float x = (xValue.Value == null) ? 0 : xValue.GetValueAsFloat();
            double acos = Math.Acos(x);

            return new DreamValue((float)(acos * 180 / Math.PI));
        }

        [DreamProc("arcsin")]
        [DreamProcParameter("X", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_arcsin(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue xValue = arguments.GetArgument(0, "X");
            float x = (xValue.Value == null) ? 0 : xValue.GetValueAsFloat();
            double asin = Math.Asin(x);

            return new DreamValue((float)(asin * 180 / Math.PI));
        }

        [DreamProc("arctan")]
        [DreamProcParameter("A", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_arctan(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue aValue = arguments.GetArgument(0, "A");
            float a = (aValue.Value == null) ? 0 : aValue.GetValueAsFloat();
            double atan = Math.Atan(a);

            return new DreamValue((float)(atan * 180 / Math.PI));
        }

        [DreamProc("ascii2text")]
        [DreamProcParameter("N", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_ascii2text(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            int ascii = arguments.GetArgument(0, "N").GetValueAsInteger();

            return new DreamValue(Convert.ToChar(ascii).ToString());
        }

        [DreamProc("ckey")]
        [DreamProcParameter("Key", Type = DreamValueType.String)]
        public static DreamValue NativeProc_ckey(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string key = arguments.GetArgument(0, "Key").GetValueAsString();

            key = Regex.Replace(key.ToLower(), "[\\^]|[^a-z0-9@]", ""); //Remove all punctuation and make lowercase
            return new DreamValue(key);
        }

        [DreamProc("ckeyEx")]
        [DreamProcParameter("Text", Type = DreamValueType.String)]
        public static DreamValue NativeProc_ckeyEx(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string text = arguments.GetArgument(0, "Text").GetValueAsString();

            text = Regex.Replace(text, "[\\^]|[^A-z0-9@_-]", ""); //Remove all punctuation except - and _
            return new DreamValue(text);
        }

        [DreamProc("clamp")]
        [DreamProcParameter("Value", Type = DreamValueType.Float | DreamValueType.DreamObject)]
        [DreamProcParameter("Low", Type = DreamValueType.Float)]
        [DreamProcParameter("High", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_clamp(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            DreamValue value = arguments.GetArgument(0, "Value");
            float lVal = arguments.GetArgument(1, "Low").GetValueAsFloat();
            float hVal = arguments.GetArgument(2, "High").GetValueAsFloat();

            if (value.TryGetValueAsDreamList(out DreamList list))
            {
                DreamList tmp = DreamList.Create(CurrentRuntime);
                foreach (DreamValue val in list.GetValues())
                {
                    tmp.AddValue(new DreamValue(Math.Clamp(val.GetValueAsFloat(), lVal, hVal)));
                }
                return new DreamValue(tmp);
            }
            else
            {
                return new DreamValue(Math.Clamp(value.GetValueAsFloat(), lVal, hVal));
            }
        }

        [DreamProc("cmptext")]
        [DreamProcParameter("T1", Type = DreamValueType.String)]
        public static DreamValue NativeProc_cmptext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            List<DreamValue> values = arguments.GetAllArguments();
            string t1 = values[0].GetValueAsString().ToLower();

            for (int i = 1; i < values.Count; i++) {
                if (values[i].GetValueAsString().ToLower() != t1) return new DreamValue(0);
            }

            return new DreamValue(1);
        }

        [DreamProc("copytext")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_copytext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string text = arguments.GetArgument(0, "T").GetValueAsString();
            int start = arguments.GetArgument(1, "Start").GetValueAsInteger(); //1-indexed
            int end = arguments.GetArgument(2, "End").GetValueAsInteger(); //1-indexed

            if (end <= 0) end += text.Length + 1;
            else if (end > text.Length + 1) end = text.Length + 1;

            if (start == 0) return new DreamValue("");
            else if (start < 0) start += text.Length + 1;

            return new DreamValue(text.Substring(start - 1, end - start));
        }

        [DreamProc("cos")]
        [DreamProcParameter("X", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_cos(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue xValue = arguments.GetArgument(0, "X");
            float x = (xValue.Value == null) ? 0 : xValue.GetValueAsFloat();
            double rad = x * (Math.PI / 180);

            return new DreamValue((float)Math.Cos(rad));
        }

        [DreamProc("CRASH")]
        [DreamProcParameter("msg", Type = DreamValueType.String)]
        public static DreamValue NativeProc_CRASH(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string message = arguments.GetArgument(0, "msg").GetValueAsString();

            throw new PropagatingRuntime(message);
        }

        [DreamProc("fcopy")]
        [DreamProcParameter("Src", Type = DreamValueType.String)]
        [DreamProcParameter("Dst", Type = DreamValueType.String)]
        public static DreamValue NativeProc_fcopy(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string src = arguments.GetArgument(0, "Src").GetValueAsString();
            string dst = arguments.GetArgument(1, "Dst").GetValueAsString();

            return new DreamValue(CurrentRuntime.ResourceManager.CopyFile(src, dst) ? 1 : 0);
        }

        [DreamProc("fcopy_rsc")]
        [DreamProcParameter("File", Type = DreamValueType.String)]
        public static DreamValue NativeProc_fcopy_rsc(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string filePath = arguments.GetArgument(0, "File").GetValueAsString();

            return new DreamValue(CurrentRuntime.ResourceManager.LoadResource(filePath));
        }

        [DreamProc("fdel")]
        [DreamProcParameter("File", Type = DreamValueType.String)]
        public static DreamValue NativeProc_fdel(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string filePath = arguments.GetArgument(0, "File").GetValueAsString();

            bool successful;
            if (filePath.EndsWith("/")) {
                successful = CurrentRuntime.ResourceManager.DeleteDirectory(filePath);
            } else {
                successful = CurrentRuntime.ResourceManager.DeleteFile(filePath);
            }

            return new DreamValue(successful ? 1 : 0);
        }

        [DreamProc("fexists")]
        [DreamProcParameter("File", Type = DreamValueType.String | DreamValueType.DreamResource)]
        public static DreamValue NativeProc_fexists(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue file = arguments.GetArgument(0, "File");
            string filePath;

            if (!file.TryGetValueAsString(out filePath)) {
                filePath = file.GetValueAsDreamResource().ResourcePath;
            }

            return new DreamValue(CurrentRuntime.ResourceManager.DoesFileExist(filePath) ? 1 : 0);
        }

        [DreamProc("file")]
        [DreamProcParameter("Path", Type = DreamValueType.String | DreamValueType.DreamResource)]
        public static DreamValue NativeProc_file(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue path = arguments.GetArgument(0, "Path");

            if (path.Type == DreamValueType.String) {
                DreamResource resource = CurrentRuntime.ResourceManager.LoadResource(path.GetValueAsString());

                return new DreamValue(resource);
            } else if (path.Type == DreamValueType.DreamResource) {
                return path;
            } else {
                throw new Exception("Invalid path argument");
            }
        }

        [DreamProc("file2text")]
        [DreamProcParameter("File", Type = DreamValueType.String | DreamValueType.DreamResource)]
        public static DreamValue NativeProc_file2text(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue file = arguments.GetArgument(0, "File");
            DreamResource resource;

            if (file.Type == DreamValueType.String) {
                resource = CurrentRuntime.ResourceManager.LoadResource(file.GetValueAsString());
            } else {
                resource = file.GetValueAsDreamResource();
            }

            string text = resource.ReadAsString();
            if (text != null) return new DreamValue(text);
            else return DreamValue.Null;

        }

        [DreamProc("findtext")]
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needle", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_findtext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string text = arguments.GetArgument(0, "Haystack").GetValueAsString();
            string needle = arguments.GetArgument(1, "Needle").GetValueAsString();
            int start = arguments.GetArgument(2, "Start").GetValueAsInteger(); //1-indexed
            int end = arguments.GetArgument(3, "End").GetValueAsInteger(); //1-indexed

            if (end == 0) {
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
            string text = arguments.GetArgument(0, "Haystack").GetValueAsString();
            string needle = arguments.GetArgument(1, "Needle").GetValueAsString();
            int start = arguments.GetArgument(2, "Start").GetValueAsInteger(); //1-indexed
            int end = arguments.GetArgument(3, "End").GetValueAsInteger(); //1-indexed

            if (end == 0) {
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
            string text = arguments.GetArgument(0, "Haystack").GetValueAsString().ToLower();
            string needle = arguments.GetArgument(1, "Needle").GetValueAsString().ToLower();
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
            string text = arguments.GetArgument(0, "Haystack").GetValueAsString();
            string needle = arguments.GetArgument(1, "Needle").GetValueAsString();
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
            string path = arguments.GetArgument(0, "Path").GetValueAsString();
            var listing = CurrentRuntime.ResourceManager.GetListing(path);
            DreamList list = DreamList.Create(CurrentRuntime, listing);
            return new DreamValue(list);
        }

        [DreamProc("hascall")]
        [DreamProcParameter("Object", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("ProcName", Type = DreamValueType.String)]
        public static DreamValue NativeProc_hascall(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            var obj = arguments.GetArgument(0, "Object").GetValueAsDreamObject();
            var procName = arguments.GetArgument(1, "ProcName").GetValueAsString();
            return new DreamValue(obj.ObjectDefinition.HasProc(procName) ? 1 : 0);
        }

        [DreamProc("html_decode")]
        [DreamProcParameter("HtmlText", Type = DreamValueType.String)]
        public static DreamValue NativeProc_html_decode(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string htmlText = arguments.GetArgument(0, "HtmlText").GetValueAsString();

            return new DreamValue(HttpUtility.HtmlDecode(htmlText));
        }

        [DreamProc("html_encode")]
        [DreamProcParameter("PlainText", Type = DreamValueType.String)]
        public static DreamValue NativeProc_html_encode(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string plainText = arguments.GetArgument(0, "PlainText").GetValueAsString();

            return new DreamValue(HttpUtility.HtmlEncode(plainText));
        }

        [DreamProc("icon_states")]
        [DreamProcParameter("Icon", Type = DreamValueType.DreamResource)]
        [DreamProcParameter("mode", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_icon_states(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            var mode = arguments.GetArgument(1, "mode").GetValueAsInteger();
            if (mode != 0)
            {
                throw new NotImplementedException();
            }
            var resource = arguments.GetArgument(0, "Icon").GetValueAsDreamResource();
            var description = DMIParser.ReadDMIDescription(resource.ResourceData);
            var states = DMIParser.GetIconStatesFromDescription(description);
            return new DreamValue(DreamList.Create(CurrentRuntime, states));
        }

        [DreamProc("image")]
        [DreamProcParameter("icon", Type = DreamValueType.DreamResource)]
        [DreamProcParameter("loc", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("icon_state", Type = DreamValueType.String)]
        [DreamProcParameter("layer", Type = DreamValueType.Float)]
        [DreamProcParameter("dir", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_image(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamObject imageObject = CurrentRuntime.ObjectTree.CreateObject(DreamPath.Image);
            imageObject.InitSpawn(arguments);
            return new DreamValue(imageObject);
        }

        [DreamProc("isarea")]
        [DreamProcParameter("Loc1", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_isarea(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            List<DreamValue> locs = arguments.GetAllArguments();

            foreach (DreamValue loc in locs) {
                if (!loc.TryGetValueAsDreamObjectOfType(DreamPath.Area, out _)) return new DreamValue(0);
            }

            return new DreamValue(1);
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
            if (icon.TryGetValueAsDreamObjectOfType(DreamPath.Icon, out _))
                return new DreamValue(1);
            else if (icon.TryGetValueAsDreamResource(out DreamResource resource)) {
                string[] DMIendings = {".dmi", ".bmp", ".png", ".jpg", ".gif"};
                return new DreamValue(DMIendings.Any(x => resource.ResourcePath.EndsWith(x)) ? 1 : 0);
            } else {
                return new DreamValue(0);
            }
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
            List<DreamValue> locs = arguments.GetAllArguments();

            foreach (DreamValue loc in locs) {
                if (loc.TryGetValueAsDreamObject(out DreamObject locObject)) {
                    bool isLoc = locObject.IsSubtypeOf(DreamPath.Mob) || locObject.IsSubtypeOf(DreamPath.Obj) || locObject.IsSubtypeOf(DreamPath.Turf) || locObject.IsSubtypeOf(DreamPath.Area);

                    if (!isLoc) {
                        return new DreamValue(0);
                    }
                } else {
                    return new DreamValue(0);
                }
            }

            return new DreamValue(1);
        }

        [DreamProc("ismob")]
        [DreamProcParameter("Loc1", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_ismob(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            List<DreamValue> locs = arguments.GetAllArguments();

            foreach (DreamValue loc in locs) {
                if (loc.Type != DreamValueType.DreamObject || loc.Value == null || !loc.GetValueAsDreamObject().IsSubtypeOf(DreamPath.Mob)) {
                    return new DreamValue(0);
                }
            }

            return new DreamValue(1);
        }

        [DreamProc("ismovable")]
        [DreamProcParameter("Loc1", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_ismovable(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            List<DreamValue> locs = arguments.GetAllArguments();

            foreach (DreamValue loc in locs) {
                if (!loc.TryGetValueAsDreamObjectOfType(DreamPath.Movable, out _)) {
                    return new DreamValue(0);
                }
            }

            return new DreamValue(1);
        }

        [DreamProc("isnull")]
        [DreamProcParameter("Val")]
        public static DreamValue NativeProc_isnull(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue value = arguments.GetArgument(0, "Val");

            return new DreamValue((value.Value == null) ? 1 : 0);
        }

        [DreamProc("isnum")]
        [DreamProcParameter("Val")]
        public static DreamValue NativeProc_isnum(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue value = arguments.GetArgument(0, "Val");

            return new DreamValue((value.Type == DreamValueType.Float) ? 1 : 0);
        }

        [DreamProc("ispath")]
        [DreamProcParameter("Val")]
        [DreamProcParameter("Type", Type = DreamValueType.DreamPath)]
        public static DreamValue NativeProc_ispath(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue value = arguments.GetArgument(0, "Val");
            DreamValue type = arguments.GetArgument(1, "Type");


            if (value.TryGetValueAsPath(out DreamPath valuePath)) {
                if (type.TryGetValueAsPath(out DreamPath typePath)) {
                    DreamObjectDefinition valueDefinition = CurrentRuntime.ObjectTree.GetObjectDefinitionFromPath(valuePath);

                    return new DreamValue(valueDefinition.IsSubtypeOf(typePath) ? 1 : 0);
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
            List<DreamValue> locs = arguments.GetAllArguments();

            foreach (DreamValue loc in locs) {
                if (!loc.TryGetValueAsDreamObjectOfType(DreamPath.Turf, out _)) {
                    return new DreamValue(0);
                }
            }

            return new DreamValue(1);
        }

        private static DreamValue CreateValueFromJsonElement(JsonElement jsonElement) {
            if (jsonElement.ValueKind == JsonValueKind.Array) {
                DreamList list = DreamList.Create(CurrentRuntime);

                foreach (JsonElement childElement in jsonElement.EnumerateArray()) {
                    DreamValue value = CreateValueFromJsonElement(childElement);

                    list.AddValue(value);
                }

                return new DreamValue(list);
            } else if (jsonElement.ValueKind == JsonValueKind.Object) {
                DreamList list = DreamList.Create(CurrentRuntime);

                foreach (JsonProperty childProperty in jsonElement.EnumerateObject()) {
                    DreamValue value = CreateValueFromJsonElement(childProperty.Value);

                    list.SetValue(new DreamValue(childProperty.Name), value);
                }

                return new DreamValue(list);
            } else if (jsonElement.ValueKind == JsonValueKind.String) {
                return new DreamValue(jsonElement.GetString());
            } else if (jsonElement.ValueKind == JsonValueKind.Number) {
                return new DreamValue(jsonElement.GetUInt32());
            } else {
                throw new Exception("Invalid ValueKind " + jsonElement.ValueKind);
            }
        }

        public static object CreateJsonElementFromValue(DreamValue value) {
            if (value.TryGetValueAsFloat(out float floatValue)) {
                return floatValue;
            } else if (value.TryGetValueAsString(out string text)) {
                return HttpUtility.JavaScriptStringEncode(text);
            } else if (value.Type == DreamValueType.DreamPath) {
                return value.GetValueAsPath().ToString();
            } else if (value.TryGetValueAsDreamList(out DreamList list)) {
                if (list.IsAssociative()) {
                    Dictionary<Object, Object> jsonObject = new(list.GetLength());

                    foreach (DreamValue listValue in list.GetValues()) {
                        if (list.ContainsKey(listValue)) {
                            jsonObject.Add(listValue.Stringify(), CreateJsonElementFromValue(list.GetValue(listValue)));
                        } else {
                            jsonObject.Add(CreateJsonElementFromValue(listValue), null); // list[x] = null
                        }
                    }
                    return jsonObject;
                } else {
                    List<Object> jsonObject = new();

                    foreach (DreamValue listValue in list.GetValues()) {
                        jsonObject.Add(CreateJsonElementFromValue(listValue));
                    }

                    return jsonObject;
                }
            } else if (value.Type == DreamValueType.DreamObject) {
                if (value.Value == null) return null;

                return value.Stringify();
            } else {
                throw new Exception("Cannot json_encode " + value);
            }
        }

        [DreamProc("json_decode")]
        [DreamProcParameter("JSON", Type = DreamValueType.String)]
        public static DreamValue NativeProc_json_decode(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string jsonString = arguments.GetArgument(0, "JSON").GetValueAsString();
            JsonElement jsonRoot = JsonSerializer.Deserialize<JsonElement>(jsonString);

            return CreateValueFromJsonElement(jsonRoot);
        }

        [DreamProc("json_encode")]
        [DreamProcParameter("Value")]
        public static DreamValue NativeProc_json_encode(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            object jsonObject = CreateJsonElementFromValue(arguments.GetArgument(0, "Value"));
            string result = JsonSerializer.Serialize(jsonObject);

            return new DreamValue(result);
        }

        private static DreamValue _length(DreamValue value, bool countBytes)
        {
            return value.Type switch
            {
                DreamValueType.String when countBytes => new DreamValue(value.GetValueAsString().Length),
                DreamValueType.String => new DreamValue(value.GetValueAsString().EnumerateRunes().Count()),
                DreamValueType.Float => new DreamValue(0),
                DreamValueType.DreamObject when value.TryGetValueAsDreamObjectOfType(DreamPath.List,
                    out DreamObject listObject) => listObject.GetVariable("len"),
                DreamValueType.DreamObject => new DreamValue(0),
                DreamValueType.DreamPath => new DreamValue(0),
                _ => throw new Exception("Cannot check length of " + value + "")
            };
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
        public static DreamValue NativeProc_list2params(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            if (!arguments.GetArgument(0, "List").TryGetValueAsDreamList(out DreamList list)) return new DreamValue(string.Empty);

            StringBuilder paramBuilder = new StringBuilder();

            List<DreamValue> values = list.GetValues();
            foreach (DreamValue entry in values) {
                if (list.ContainsKey(entry))
                {
                    paramBuilder.Append($"{HttpUtility.UrlEncode(entry.Value.ToString())}={HttpUtility.UrlEncode(list.GetValue(entry).Value.ToString())}");
                    paramBuilder.Append('&');
                } else {
                    paramBuilder.Append(HttpUtility.UrlEncode(entry.Value.ToString()));
                    paramBuilder.Append('&');
                }
            }

            //Remove trailing &
            paramBuilder.Remove(paramBuilder.Length-1, 1);
            return new DreamValue(paramBuilder.ToString());
        }

        [DreamProc("log")]
        [DreamProcParameter("X", Type = DreamValueType.Float)]
        [DreamProcParameter("Y")]
        public static DreamValue NativeProc_log(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            float x = arguments.GetArgument(0, "X").GetValueAsFloat();
            DreamValue y = arguments.GetArgument(1, "Y");

            if (y.Value != null) {
                return new DreamValue((float)Math.Log(y.GetValueAsFloat(), x));
            } else {
                return new DreamValue(Math.Log(x));
            }
        }

        [DreamProc("lowertext")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        public static DreamValue NativeProc_lowertext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string text = arguments.GetArgument(0, "T").GetValueAsString();

            return new DreamValue(text.ToLower());
        }

        [DreamProc("max")]
        [DreamProcParameter("A")]
        public static DreamValue NativeProc_max(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            List<DreamValue> values;

            if (arguments.ArgumentCount == 1) {
                DreamList list = arguments.GetArgument(0, "A").GetValueAsDreamList();

                values = list.GetValues();
            } else {
                values = arguments.GetAllArguments();
            }

            DreamValue currentMax = values[0];

            for (int i = 1; i < values.Count; i++) {
                DreamValue value = values[i];

                if (value.Value == null) {
                    currentMax = value;
                } else if (value.Type == currentMax.Type) {
                    if (value.Type == DreamValueType.Float) {
                        if (value.GetValueAsFloat() > currentMax.GetValueAsFloat()) currentMax = value;
                    } else if (value.Type == DreamValueType.String) {
                        if (String.Compare(value.GetValueAsString(), currentMax.GetValueAsString()) > 0) currentMax = value;
                    }
                } else {
                    throw new Exception("Cannot compare " + currentMax + " and " + value);
                }
            }

            return currentMax;
        }

        [DreamProc("md5")]
        [DreamProcParameter("T", Type = DreamValueType.String | DreamValueType.DreamResource)]
        public static DreamValue NativeProc_md5(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if(arguments.ArgumentCount > 1) throw new Exception("md5() only takes one argument");
            DreamValue arg = arguments.GetArgument(0, "T");

            string text;
            if (arg.TryGetValueAsDreamResource(out DreamResource resource)) {
                text = resource.ReadAsString();
            } else if (!arg.TryGetValueAsString(out text)) {
                return DreamValue.Null;
            }

            MD5 md5 = MD5.Create();
            byte[] input = Encoding.UTF8.GetBytes(text);
            byte[] output = md5.ComputeHash(input);
            //Match BYOND formatting
            string hash = BitConverter.ToString(output).Replace("-", "").ToLower();
            return new DreamValue(hash);
        }

        [DreamProc("min")]
        [DreamProcParameter("A")]
        public static DreamValue NativeProc_min(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            List<DreamValue> values;

            if (arguments.ArgumentCount == 1) {
                DreamList list = arguments.GetArgument(0, "A").GetValueAsDreamList();

                values = list.GetValues();
            } else {
                values = arguments.GetAllArguments();
            }

            DreamValue currentMin = values[0];
            if (currentMin.Value == null) return currentMin;

            for (int i = 1; i < values.Count; i++) {
                DreamValue value = values[i];

                if (value.Type == currentMin.Type) {
                    if (value.Type == DreamValueType.Float) {
                        if (value.GetValueAsFloat() < currentMin.GetValueAsFloat()) currentMin = value;
                    } else if (value.Type == DreamValueType.String) {
                        if (String.Compare(value.GetValueAsString(), currentMin.GetValueAsString()) < 0) currentMin = value;
                    }
                } else if (value.Value == null) {
                    return value;
                } else {
                    throw new Exception("Cannot compare " + currentMin + " and " + value);
                }
            }

            return currentMin;
        }

        [DreamProc("num2text")]
        [DreamProcParameter("N")]
        [DreamProcParameter("Digits", Type = DreamValueType.Float)]
        [DreamProcParameter("Radix", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_num2text(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue number = arguments.GetArgument(0, "N");

            if (number.TryGetValueAsFloat(out float floatValue)) {
                return new DreamValue(floatValue.ToString());
            } else {
                return new DreamValue("0");
            }
        }

        [DreamProc("oview")]
        [DreamProcParameter("Dist", Type = DreamValueType.Float, DefaultValue = 5)]
        [DreamProcParameter("Center", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_oview(DreamObject instance, DreamObject usr, DreamProcArguments arguments) { //TODO: View obstruction (dense turfs)
            int distance = 5;
            DreamObject center = usr;

            //Arguments are optional and can be passed in any order
            if (arguments.ArgumentCount > 0) {
                DreamValue firstArgument = arguments.GetArgument(0, "Dist");

                if (firstArgument.Type == DreamValueType.DreamObject) {
                    center = firstArgument.GetValueAsDreamObject();

                    if (arguments.ArgumentCount > 1) {
                        distance = arguments.GetArgument(1, "Center").GetValueAsInteger();
                    }
                } else {
                    distance = firstArgument.GetValueAsInteger();

                    if (arguments.ArgumentCount > 1) {
                        center = arguments.GetArgument(1, "Center").GetValueAsDreamObject();
                    }
                }
            }

            DreamList view = DreamList.Create(CurrentRuntime);
            int centerX = center.GetVariable("x").GetValueAsInteger();
            int centerY = center.GetVariable("y").GetValueAsInteger();
            int centerZ = center.GetVariable("z").GetValueAsInteger();

            for (int x = Math.Max(centerX - distance, 1); x < Math.Min(centerX + distance, CurrentRuntime.Map.Width); x++) {
                for (int y = Math.Max(centerY - distance, 1); y < Math.Min(centerY + distance, CurrentRuntime.Map.Width); y++) {
                    if (x == centerX && y == centerY) continue;

                    DreamObject turf = CurrentRuntime.Map.GetTurfAt(x, y, centerZ);

                    view.AddValue(new DreamValue(turf));
                    foreach (DreamValue content in turf.GetVariable("contents").GetValueAsDreamList().GetValues()) {
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

            DreamList view = DreamList.Create(CurrentRuntime);
            int depth = (depthValue.Type == DreamValueType.Float) ? depthValue.GetValueAsInteger() : 5; //TODO: Default to world.view
            int centerX = center.GetVariable("x").GetValueAsInteger();
            int centerY = center.GetVariable("y").GetValueAsInteger();

            foreach (DreamObject mob in CurrentRuntime.Mobs) {
                int mobX = mob.GetVariable("x").GetValueAsInteger();
                int mobY = mob.GetVariable("y").GetValueAsInteger();

                if (mobX == centerX && mobY == centerY) continue;

                if (Math.Abs(centerX - mobX) <= depth && Math.Abs(centerY - mobY) <= depth) {
                    view.AddValue(new DreamValue(mob));
                }
            }

            return new DreamValue(view);
        }

        public static DreamList params2list(DreamRuntime runtime, string queryString) {
            queryString = queryString.Replace(";", "&");
            NameValueCollection query = HttpUtility.ParseQueryString(queryString);
            DreamList list = DreamList.Create(runtime);

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
                result = params2list(CurrentRuntime, paramsString);
            } else {
                result = DreamList.Create(CurrentRuntime);
            }

            return new DreamValue(result);
        }

        [DreamProc("prob")]
        [DreamProcParameter("P", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_prob(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            float probability = arguments.GetArgument(0, "P").GetValueAsFloat();

            return new DreamValue((CurrentRuntime.Random.Next(0, 100) <= probability) ? 1 : 0);
        }

        [DreamProc("rand")]
        [DreamProcParameter("L", Type = DreamValueType.Float)]
        [DreamProcParameter("H", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_rand(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (arguments.ArgumentCount == 0) {
                return new DreamValue((float)CurrentRuntime.Random.NextDouble());
            } else if (arguments.ArgumentCount == 1) {
                int high = (int)Math.Floor(arguments.GetArgument(0, "L").GetValueAsFloat());

                return new DreamValue(CurrentRuntime.Random.Next(high));
            } else {
                int low = (int)Math.Floor(arguments.GetArgument(0, "L").GetValueAsFloat());
                int high = (int)Math.Floor(arguments.GetArgument(1, "H").GetValueAsFloat());

                return new DreamValue(CurrentRuntime.Random.Next(Math.Min(low, high), Math.Max(low, high)));
            }
        }

        [DreamProc("rand_seed")]
        [DreamProcParameter("Seed", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_rand_seed(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            var seed = arguments.GetArgument(0, "Seed").GetValueAsInteger();
            CurrentRuntime.Random = new Random(seed);
            return DreamValue.Null;
        }

        [DreamProc("ref")]
        [DreamProcParameter("Object", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_ref(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            var obj = arguments.GetArgument(0, "Object").GetValueAsDreamObject();
            return new DreamValue(obj.CreateReferenceID());
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
            var newRegex = CurrentRuntime.ObjectTree.CreateObject(DreamPath.Regex);
            newRegex.InitSpawn(arguments);
            return new DreamValue(newRegex);
        }

        [DreamProc("replacetext")]
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needle", Type = DreamValueType.String)]
        [DreamProcParameter("Replacement", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_replacetext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string text = arguments.GetArgument(0, "Haystack").GetValueAsString();
            string needle = arguments.GetArgument(1, "Needle").GetValueAsString();
            string replacement = arguments.GetArgument(2, "Replacement").GetValueAsString();
            int start = arguments.GetArgument(3, "Start").GetValueAsInteger(); //1-indexed
            int end = arguments.GetArgument(4, "End").GetValueAsInteger(); //1-indexed

            if (end == 0) {
                end = text.Length + 1;
            }

            return new DreamValue(text.Substring(start - 1, end - start).Replace(needle, replacement, StringComparison.OrdinalIgnoreCase));
        }

        [DreamProc("rgb")]
        [DreamProcParameter("R", Type = DreamValueType.Float)]
        [DreamProcParameter("G", Type = DreamValueType.Float)]
        [DreamProcParameter("B", Type = DreamValueType.Float)]
        [DreamProcParameter("A", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_rgb(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            int r = (int)arguments.GetArgument(0, "R").GetValueAsFloat();
            int g = (int)arguments.GetArgument(1, "G").GetValueAsFloat();
            int b = (int)arguments.GetArgument(2, "B").GetValueAsFloat();
            DreamValue aValue = arguments.GetArgument(3, "A");

            if (aValue.Value == null) {
                return new DreamValue(String.Format("#{0:X2}{1:X2}{2:X2}", r, g, b));
            } else {
                int a = (int)aValue.GetValueAsFloat();

                return new DreamValue(String.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", r, g, b, a));
            }
        }

        [DreamProc("replacetextEx")]
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needle", Type = DreamValueType.String)]
        [DreamProcParameter("Replacement", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_replacetextEx(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string text = arguments.GetArgument(0, "Haystack").GetValueAsString();
            string needle = arguments.GetArgument(1, "Needle").GetValueAsString();
            string replacement = arguments.GetArgument(2, "Replacement").GetValueAsString();
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
            float a = arguments.GetArgument(0, "A").GetValueAsFloat();

            if (arguments.ArgumentCount == 1) {
                return new DreamValue((float)Math.Floor(a));
            } else {
                float b = arguments.GetArgument(1, "B").GetValueAsFloat();

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
                string diceInput = arguments.GetArgument(0, "ndice").GetValueAsString();
                string[] diceList = diceInput.Split('d');
                if (diceList.Length < 2) {
                    if (!Int32.TryParse(diceList[0], out sides)) { throw new Exception("Invalid dice value: " + diceInput); }
                } else {
                    if (!Int32.TryParse(diceList[0], out dice)) { throw new Exception("Invalid dice value: " + diceInput); }
                    if (!Int32.TryParse(diceList[1], out sides)) {
                        string[] sideList = diceList[1].Split('+');
                        if (!Int32.TryParse(sideList[0], out sides)) { throw new Exception("Invalid dice value: " + diceInput); }
                        if (!Int32.TryParse(sideList[1], out modifier)) { throw new Exception("Invalid dice value: " + diceInput); }
                    }
                }
            } else if (!arguments.GetArgument(0, "ndice").TryGetValueAsInteger(out dice) || !arguments.GetArgument(1, "sides").TryGetValueAsInteger(out sides)) {
                return new DreamValue(0);
            }
            float total = modifier; // Adds the modifier to start with
            for (int i = 0; i < dice; i++) {
                total += CurrentRuntime.Random.Next(1, sides + 1);
            }

            return new DreamValue(total);
        }

        [DreamProc("shutdown")]
        [DreamProcParameter("Addr", Type = DreamValueType.String | DreamValueType.DreamObject)]
        [DreamProcParameter("Natural", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_shutdown(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            DreamValue addrValue = arguments.GetArgument(0, "Addr");
            if (addrValue == DreamValue.Null) {
                CurrentRuntime.Shutdown = true;
            }
            else {
                throw new NotImplementedException();
            }
            return DreamValue.Null;
        }

        [DreamProc("sin")]
        [DreamProcParameter("X", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_sin(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue xValue = arguments.GetArgument(0, "X");
            float x = (xValue.Value == null) ? 0 : xValue.GetValueAsFloat();
            double rad = x * (Math.PI / 180);

            return new DreamValue((float)Math.Sin(rad));
        }

        [DreamProc("sleep")]
        [DreamProcParameter("Delay", Type = DreamValueType.Float)]
        public static async Task<DreamValue> NativeProc_sleep(AsyncNativeProc.State state) {
            float delay = state.Arguments.GetArgument(0, "Delay").GetValueAsFloat();
            int delayMilliseconds = (int)(delay * 100);

            // This is obviously not the proper behaviour
            await Task.Delay(delayMilliseconds);
            return DreamValue.Null;
        }

        [DreamProc("sorttext")]
        [DreamProcParameter("T1", Type = DreamValueType.String)]
        [DreamProcParameter("T2", Type = DreamValueType.String)]
        public static DreamValue NativeProc_sorttext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string t1 = arguments.GetArgument(0, "T1").GetValueAsString().ToLower();
            string t2 = arguments.GetArgument(1, "T2").GetValueAsString().ToLower();

            return new DreamValue(string.Compare(t2, t1));
        }

        [DreamProc("sorttextEx")]
        [DreamProcParameter("T1", Type = DreamValueType.String)]
        [DreamProcParameter("T2", Type = DreamValueType.String)]
        public static DreamValue NativeProc_sorttextEx(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string t1 = arguments.GetArgument(0, "T1").GetValueAsString();
            string t2 = arguments.GetArgument(1, "T2").GetValueAsString();

            return new DreamValue(string.Compare(t2, t1));
        }

        [DreamProc("sound")]
        [DreamProcParameter("file", Type = DreamValueType.DreamResource)]
        [DreamProcParameter("repeat", Type = DreamValueType.Float, DefaultValue = 0)]
        [DreamProcParameter("wait", Type = DreamValueType.Float)]
        [DreamProcParameter("channel", Type = DreamValueType.Float)]
        [DreamProcParameter("volume", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_sound(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamObject soundObject = CurrentRuntime.ObjectTree.CreateObject(DreamPath.Sound);
            soundObject.InitSpawn(arguments);
            return new DreamValue(soundObject);
        }

        [DreamProc("splittext")]
        [DreamProcParameter("Text", Type = DreamValueType.String)]
        [DreamProcParameter("Delimiter", Type = DreamValueType.String)]
        public static DreamValue NativeProc_splittext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string text = arguments.GetArgument(0, "Text").GetValueAsString();
            string delimiter = arguments.GetArgument(1, "Delimiter").GetValueAsString();
            string[] splitText = text.Split(delimiter);
            DreamList list = DreamList.Create(CurrentRuntime, splitText);

            return new DreamValue(list);
        }

        [DreamProc("sqrt")]
        [DreamProcParameter("A", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_sqrt(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            double a = arguments.GetArgument(0, "A").GetValueAsFloat();

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
            DreamConnection connection = CurrentRuntime.Server.GetConnectionFromMob(usr);

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
            DreamConnection connection = CurrentRuntime.Server.GetConnectionFromMob(usr);

            connection.SetOutputStatPanel(panel);
            if (name != DreamValue.Null || value != DreamValue.Null) {
                OutputToStatPanel(connection, name, value);
            }

            return new DreamValue(connection.SelectedStatPanel == panel ? 1 : 0);
        }

        [DreamProc("tan")]
        [DreamProcParameter("X", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_tan(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue xValue = arguments.GetArgument(0, "X");
            float x = (xValue.Value == null) ? 0 : xValue.GetValueAsFloat();
            double rad = x * (Math.PI / 180);

            return new DreamValue((float)Math.Tan(rad));
        }

        [DreamProc("text")]
        [DreamProcParameter("FormatText", Type = DreamValueType.String)]
        public static DreamValue NativeProc_text(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            return arguments.GetArgument(0, "FormatText"); //TODO: Format text
        }

        [DreamProc("text2ascii")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        [DreamProcParameter("pos", Type = DreamValueType.Float, DefaultValue = 1)]
        public static DreamValue NativeProc_text2ascii(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string text = arguments.GetArgument(0, "T").GetValueAsString();
            int pos = arguments.GetArgument(1, "pos").GetValueAsInteger(); //1-indexed

            return new DreamValue((int)text[pos - 1]);
        }

        [DreamProc("text2file")]
        [DreamProcParameter("Text", Type = DreamValueType.String)]
        [DreamProcParameter("File", Type = DreamValueType.String)]
        public static DreamValue NativeProc_text2file(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string text = arguments.GetArgument(0, "Text").GetValueAsString();
            string file = arguments.GetArgument(1, "File").GetValueAsString();

            return new DreamValue(CurrentRuntime.ResourceManager.SaveTextToFile(file, text) ? 1 : 0);
        }

        [DreamProc("text2num")]
        [DreamProcParameter("T", Type = DreamValueType.String | DreamValueType.Float | DreamValueType.DreamObject)]
        [DreamProcParameter("radix", Type = DreamValueType.Float, DefaultValue = 10)]
        public static DreamValue NativeProc_text2num(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue value = arguments.GetArgument(0, "T");

            if (value.TryGetValueAsString(out string text)) {
                int radix = arguments.GetArgument(1, "radix").GetValueAsInteger();

                text = text.Trim();
                if (text.Length != 0) {
                    try {
                        if (radix == 10) {
                            return new DreamValue(Convert.ToSingle(text));
                        } else {
                            return new DreamValue(Convert.ToInt32(text, radix));
                        }
                    } catch (FormatException) {
                        return DreamValue.Null; //No digits, return null
                    }
                } else {
                    return DreamValue.Null;
                }
            } else if (value.Type == DreamValueType.Float) {
                return value;
            } else if (value == DreamValue.Null) {
                return DreamValue.Null;
            } else {
                throw new Exception("Invalid argument to text2num: " + value);
            }
        }

        [DreamProc("text2path")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        public static DreamValue NativeProc_text2path(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string text = arguments.GetArgument(0, "T").GetValueAsString();
            DreamPath path = new DreamPath(text);

            if (CurrentRuntime.ObjectTree.HasTreeEntry(path)) {
                return new DreamValue(path);
            } else {
                return DreamValue.Null;
            }
        }

        [DreamProc("time2text")]
        [DreamProcParameter("timestamp", Type = DreamValueType.Float)]
        [DreamProcParameter("format", Type = DreamValueType.String)]
        public static DreamValue NativeProc_time2text(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            int timestamp = arguments.GetArgument(0, "timestamp").GetValueAsInteger();
            string format = arguments.GetArgument(1, "format").GetValueAsString();
            long ticks = timestamp * (TimeSpan.TicksPerSecond / 10);
            if (timestamp >= 0 && timestamp <= 864000) ticks += DateTime.Today.Ticks;
            DateTime time = new DateTime(ticks);

            format = format.Replace("YYYY", "yyyy");
            format = format.Replace("YY", "yy");
            format = format.Replace("Month", "MMMM");
            format = format.Replace("MM", "M");
            format = format.Replace("Day", "dddd");
            format = format.Replace("DDD", "ddd");
            format = format.Replace("DD", "d");
            return new DreamValue(time.ToString(format));
        }

        [DreamProc("typesof")]
        [DreamProcParameter("Item1")]
        public static DreamValue NativeProc_typesof(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamList list = DreamList.Create(CurrentRuntime);

            foreach (DreamValue type in arguments.GetAllArguments()) {
                DreamPath typePath = type.GetValueAsPath();

                if (typePath.LastElement == "proc") {
                    DreamPath objectTypePath = typePath.AddToPath("..");
                    DreamObjectDefinition objectDefinition = CurrentRuntime.ObjectTree.GetObjectDefinitionFromPath(objectTypePath);

                    foreach (KeyValuePair<string, DreamProc> proc in objectDefinition.Procs) {
                        list.AddValue(new DreamValue(proc.Key));
                    }
                } else {
                    DreamObjectTree.DreamObjectTreeEntry objectTreeEntry = CurrentRuntime.ObjectTree.GetTreeEntryFromPath(typePath);
                    List<DreamObjectTree.DreamObjectTreeEntry> objectTreeDescendants = objectTreeEntry.GetAllDescendants(true, true);

                    foreach (DreamObjectTree.DreamObjectTreeEntry objectTreeDescendant in objectTreeDescendants) {
                        list.AddValue(new DreamValue(objectTreeDescendant.ObjectDefinition.Type));
                    }
                }
            }

            return new DreamValue(list);
        }

        [DreamProc("uppertext")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        public static DreamValue NativeProc_uppertext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string text = arguments.GetArgument(0, "T").GetValueAsString();

            return new DreamValue(text.ToUpper());
        }

        [DreamProc("url_decode")]
        [DreamProcParameter("UrlText", Type = DreamValueType.String)]
        public static DreamValue NativeProc_url_decode(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string urlText = arguments.GetArgument(0, "UrlText").GetValueAsString();

            return new DreamValue(HttpUtility.UrlDecode(urlText));
        }

        [DreamProc("url_encode")]
        [DreamProcParameter("PlainText", Type = DreamValueType.String)]
        [DreamProcParameter("format", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_url_encode(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string plainText = arguments.GetArgument(0, "PlainText").GetValueAsString();
            int format = arguments.GetArgument(1, "format").GetValueAsInteger();

            return new DreamValue(HttpUtility.UrlEncode(plainText));
        }

        [DreamProc("view")]
        [DreamProcParameter("Dist", Type = DreamValueType.Float, DefaultValue = 5)]
        [DreamProcParameter("Center", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_view(DreamObject instance, DreamObject usr, DreamProcArguments arguments) { //TODO: View obstruction (dense turfs)
            int distance = 5;
            DreamObject center = usr;

            //Arguments are optional and can be passed in any order
            if (arguments.ArgumentCount > 0) {
                DreamValue firstArgument = arguments.GetArgument(0, "Dist");

                if (firstArgument.Type == DreamValueType.DreamObject) {
                    center = firstArgument.GetValueAsDreamObject();

                    if (arguments.ArgumentCount > 1) {
                        distance = arguments.GetArgument(1, "Center").GetValueAsInteger();
                    }
                } else {
                    distance = firstArgument.GetValueAsInteger();

                    if (arguments.ArgumentCount > 1) {
                        center = arguments.GetArgument(1, "Center").GetValueAsDreamObject();
                    }
                }
            }

            DreamList view = DreamList.Create(CurrentRuntime);
            int centerX = center.GetVariable("x").GetValueAsInteger();
            int centerY = center.GetVariable("y").GetValueAsInteger();
            int centerZ = center.GetVariable("z").GetValueAsInteger();

            for (int x = Math.Max(centerX - distance, 1); x < Math.Min(centerX + distance, CurrentRuntime.Map.Width); x++) {
                for (int y = Math.Max(centerY - distance, 1); y < Math.Min(centerY + distance, CurrentRuntime.Map.Width); y++) {
                    DreamObject turf = CurrentRuntime.Map.GetTurfAt(x, y, centerZ);

                    view.AddValue(new DreamValue(turf));
                    foreach (DreamValue content in turf.GetVariable("contents").GetValueAsDreamList().GetValues()) {
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

            DreamList view = DreamList.Create(CurrentRuntime);
            int depth = (depthValue.Type == DreamValueType.Float) ? depthValue.GetValueAsInteger() : 5; //TODO: Default to world.view
            int centerX = center.GetVariable("x").GetValueAsInteger();
            int centerY = center.GetVariable("y").GetValueAsInteger();

            foreach (DreamObject mob in CurrentRuntime.Mobs) {
                int mobX = mob.GetVariable("x").GetValueAsInteger();
                int mobY = mob.GetVariable("y").GetValueAsInteger();

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

            if (player.TryGetValueAsDreamObjectOfType(DreamPath.Mob, out DreamObject mob)) {
                connection = CurrentRuntime.Server.GetConnectionFromMob(mob);
            } else {
                DreamObject client = player.GetValueAsDreamObjectOfType(DreamPath.Client);

                connection = CurrentRuntime.Server.GetConnectionFromClient(client);
            }

            connection.WinSet(winsetControlId, winsetParams);
            return DreamValue.Null;
        }
    }
}
