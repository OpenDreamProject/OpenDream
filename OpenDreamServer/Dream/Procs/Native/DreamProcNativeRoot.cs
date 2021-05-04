using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamServer.Net;
using OpenDreamServer.Resources;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using DreamValueType = OpenDreamServer.Dream.DreamValue.DreamValueType;

namespace OpenDreamServer.Dream.Procs.Native {
    static class DreamProcNativeRoot {
        [DreamProc("abs")]
        [DreamProcParameter("A")]
        public static DreamValue NativeProc_abs(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            float number = arguments.GetArgument(0, "A").GetValueAsNumber();

            return new DreamValue(Math.Abs(number));
        }

        [DreamProc("animate")]
        [DreamProcParameter("Object", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("time", Type = DreamValueType.Integer)]
        [DreamProcParameter("loop", Type = DreamValueType.Integer)]
        [DreamProcParameter("easing", Type = DreamValueType.String)]
        [DreamProcParameter("flags", Type = DreamValueType.Integer)]
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
        [DreamProcParameter("X", Type = DreamValueType.Number)]
        public static DreamValue NativeProc_arccos(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue xValue = arguments.GetArgument(0, "X");
            float x = (xValue.Value == null) ? 0 : xValue.GetValueAsNumber();
            double acos = Math.Acos(x);

            return new DreamValue((float)(acos * 180 / Math.PI));
        }

        [DreamProc("arctan")]
        [DreamProcParameter("A", Type = DreamValueType.Number)]
        public static DreamValue NativeProc_arctan(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue aValue = arguments.GetArgument(0, "A");
            float a = (aValue.Value == null) ? 0 : aValue.GetValueAsNumber();
            double atan = Math.Atan(a);

            return new DreamValue((float)(atan * 180 / Math.PI));
        }

        [DreamProc("ascii2text")]
        [DreamProcParameter("N", Type = DreamValueType.Integer)]
        public static DreamValue NativeProc_ascii2text(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            int ascii = arguments.GetArgument(0, "N").GetValueAsInteger();

            return new DreamValue(Convert.ToChar(ascii).ToString());
        }

        [DreamProc("ckey")]
        [DreamProcParameter("Key", Type = DreamValueType.String)]
        public static DreamValue NativeProc_ckey(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string key = arguments.GetArgument(0, "Key").GetValueAsString();

            key = Regex.Replace(key.ToLower(), "[^a-z]", ""); //Remove all punctuation and make lowercase
            return new DreamValue(key);
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
        [DreamProcParameter("Start", Type = DreamValueType.Integer, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Integer, DefaultValue = 0)]
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
        [DreamProcParameter("X", Type = DreamValueType.Number)]
        public static DreamValue NativeProc_cos(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue xValue = arguments.GetArgument(0, "X");
            float x = (xValue.Value == null) ? 0 : xValue.GetValueAsNumber();
            double rad = x * Math.PI / 180;

            return new DreamValue((float)Math.Cos(rad));
        }

        [DreamProc("CRASH")]
        [DreamProcParameter("msg", Type = DreamValueType.String)]
        public static DreamValue NativeProc_CRASH(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string message = arguments.GetArgument(0, "msg").GetValueAsString();

            throw new Exception(message);
        }

        [DreamProc("fcopy")]
        [DreamProcParameter("Src", Type = DreamValueType.String)]
        [DreamProcParameter("Dst", Type = DreamValueType.String)]
        public static DreamValue NativeProc_fcopy(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string src = arguments.GetArgument(0, "Src").GetValueAsString();
            string dst = arguments.GetArgument(1, "Dst").GetValueAsString();

            return new DreamValue(Program.DreamResourceManager.CopyFile(src, dst) ? 1 : 0);
        }

        [DreamProc("fcopy_rsc")]
        [DreamProcParameter("File", Type = DreamValueType.String)]
        public static DreamValue NativeProc_fcopy_rsc(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string filePath = arguments.GetArgument(0, "File").GetValueAsString();

            return new DreamValue(Program.DreamResourceManager.LoadResource(filePath));
        }

        [DreamProc("fdel")]
        [DreamProcParameter("File", Type = DreamValueType.String)]
        public static DreamValue NativeProc_fdel(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string filePath = arguments.GetArgument(0, "File").GetValueAsString();

            bool successful;
            if (filePath.EndsWith("/")) {
                successful = Program.DreamResourceManager.DeleteDirectory(filePath);
            } else {
                successful = Program.DreamResourceManager.DeleteFile(filePath);
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

            return new DreamValue(Program.DreamResourceManager.DoesFileExist(filePath) ? 1 : 0);
        }

        [DreamProc("file")]
        [DreamProcParameter("Path", Type = DreamValueType.String | DreamValueType.DreamResource)]
        public static DreamValue NativeProc_file(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue path = arguments.GetArgument(0, "Path");

            if (path.Type == DreamValueType.String) {
                DreamResource resource = Program.DreamResourceManager.LoadResource(path.GetValueAsString());

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
                resource = Program.DreamResourceManager.LoadResource(file.GetValueAsString());
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
        [DreamProcParameter("Start", Type = DreamValueType.Integer, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Integer, DefaultValue = 0)]
        public static DreamValue NativeProc_findtext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string text = arguments.GetArgument(0, "Haystack").GetValueAsString().ToLower();
            string needle = arguments.GetArgument(1, "Needle").GetValueAsString().ToLower();
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

        [DreamProc("findtextEx")]
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needle", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Integer, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Integer, DefaultValue = 0)]
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
        [DreamProcParameter("Start", Type = DreamValueType.Integer, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Integer, DefaultValue = 0)]
        public static DreamValue NativeProc_findlasttext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string text = arguments.GetArgument(0, "Haystack").GetValueAsString().ToLower();
            string needle = arguments.GetArgument(1, "Needle").GetValueAsString().ToLower();
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

        [DreamProc("flist")]
        [DreamProcParameter("Path", Type = DreamValueType.String)]
        public static DreamValue NativeProc_flist(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string path = arguments.GetArgument(0, "Path").GetValueAsString();

            string[] listing = Program.DreamResourceManager.GetListing(path);
            DreamList list = new DreamList(listing);
            return new DreamValue(list);
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

        [DreamProc("image")]
        [DreamProcParameter("icon", Type = DreamValueType.DreamResource)]
        [DreamProcParameter("loc", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("icon_state", Type = DreamValueType.String)]
        [DreamProcParameter("layer", Type = DreamValueType.Number)]
        [DreamProcParameter("dir", Type = DreamValueType.Integer)]
        public static DreamValue NativeProc_image(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamObject imageObject = Program.DreamObjectTree.CreateObject(DreamPath.Image, arguments);

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

            return new DreamValue(file.IsType(DreamValueType.DreamResource) ? 1 : 0);
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

            return new DreamValue(value.IsType(DreamValueType.Number) ? 1 : 0);
        }

        [DreamProc("ispath")]
        [DreamProcParameter("Val")]
        [DreamProcParameter("Type", Type = DreamValueType.DreamPath)]
        public static DreamValue NativeProc_ispath(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue value = arguments.GetArgument(0, "Val");
            DreamValue type = arguments.GetArgument(1, "Type");
            

            if (value.TryGetValueAsPath(out DreamPath valuePath)) {
                if (type.TryGetValueAsPath(out DreamPath typePath)) {
                    DreamObjectDefinition valueDefinition = Program.DreamObjectTree.GetObjectDefinitionFromPath(valuePath);

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
                if (loc.Type != DreamValueType.DreamObject || loc.Value == null || !loc.GetValueAsDreamObject().IsSubtypeOf(DreamPath.Turf)) {
                    return new DreamValue(0);
                }
            }

            return new DreamValue(1);
        }

        private static DreamValue CreateValueFromJsonElement(JsonElement jsonElement) {
            if (jsonElement.ValueKind == JsonValueKind.Array) {
                DreamList list = new DreamList();

                foreach (JsonElement childElement in jsonElement.EnumerateArray()) {
                    DreamValue value = CreateValueFromJsonElement(childElement);

                    list.AddValue(value);
                }

                return new DreamValue(list);
            } else if (jsonElement.ValueKind == JsonValueKind.Object) {
                DreamList list = new DreamList();

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
            if (value.IsType(DreamValueType.Number)) {
                return value.Value;
            } else if (value.TryGetValueAsString(out string text)) {
                return HttpUtility.JavaScriptStringEncode(text);
            } else if (value.Type == DreamValueType.DreamPath) {
                return value.GetValueAsPath().ToString();
            } else if (value.TryGetValueAsDreamList(out DreamList list)) {
                if (list.IsAssociative()) {
                    Dictionary<object, object> jsonObject = new();

                    foreach (KeyValuePair<DreamValue, DreamValue> listValue in list.GetAssociativeValues()) {
                        jsonObject.Add(listValue.Key.Stringify(), CreateJsonElementFromValue(listValue.Value));
                    }

                    return jsonObject;
                } else {
                    List<object> jsonObject = new();

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

        [DreamProc("length")]
        [DreamProcParameter("E")]
        public static DreamValue NativeProc_length(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue value = arguments.GetArgument(0, "E");

            if (value.Type == DreamValueType.String) {
                return new DreamValue(value.GetValueAsString().Length);
            } else if (value.Type == DreamValueType.Integer) {
                return new DreamValue(0);
            } else if (value.Type == DreamValueType.DreamObject) {
                if (value.TryGetValueAsDreamObjectOfType(DreamPath.List, out DreamObject listObject)) {
                    return listObject.GetVariable("len");
                } else {
                    return new DreamValue(0);
                }
            } else if (value.Type == DreamValueType.DreamPath) {
                return new DreamValue(0);
            }

            throw new Exception("Cannot check length of " + value + "");
        }

        [DreamProc("log")]
        [DreamProcParameter("X", Type = DreamValueType.Number)]
        [DreamProcParameter("Y")]
        public static DreamValue NativeProc_log(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            float x = arguments.GetArgument(0, "X").GetValueAsNumber();
            DreamValue y = arguments.GetArgument(1, "Y");

            if (y.Value != null) {
                return new DreamValue((float)Math.Log(y.GetValueAsNumber(), x));
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
                    if (value.Type == DreamValueType.Integer) {
                        if (value.GetValueAsInteger() > currentMax.GetValueAsInteger()) currentMax = value;
                    } else if (value.Type == DreamValueType.Float) {
                        if (value.GetValueAsFloat() > currentMax.GetValueAsFloat()) currentMax = value;
                    } else if (value.Type == DreamValueType.String) {
                        if (String.Compare(value.GetValueAsString(), currentMax.GetValueAsString()) > 0) currentMax = value;
                    }
                } else if (value.Type == DreamValueType.Integer && currentMax.Type == DreamValueType.Float) {
                    if (value.GetValueAsInteger() > currentMax.GetValueAsFloat()) currentMax = value;
                } else if (value.Type == DreamValueType.Float && currentMax.Type == DreamValueType.Integer) {
                    if (value.GetValueAsFloat() > currentMax.GetValueAsInteger()) currentMax = value;
                } else {
                    throw new Exception("Cannot compare " + currentMax + " and " + value);
                }
            }

            return currentMax;
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
                    if (value.Type == DreamValueType.Integer) {
                        if (value.GetValueAsInteger() < currentMin.GetValueAsInteger()) currentMin = value;
                    } else if (value.Type == DreamValueType.Float) {
                        if (value.GetValueAsFloat() < currentMin.GetValueAsFloat()) currentMin = value;
                    } else if (value.Type == DreamValueType.String) {
                        if (String.Compare(value.GetValueAsString(), currentMin.GetValueAsString()) < 0) currentMin = value;
                    }
                } else if (value.Type == DreamValueType.Integer && currentMin.Type == DreamValueType.Float) {
                    if (value.GetValueAsInteger() < currentMin.GetValueAsFloat()) currentMin = value;
                } else if (value.Type == DreamValueType.Float && currentMin.Type == DreamValueType.Integer) {
                    if (value.GetValueAsFloat() < currentMin.GetValueAsInteger()) currentMin = value;
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
        [DreamProcParameter("Digits", Type = DreamValueType.Integer)]
        [DreamProcParameter("Radix", Type = DreamValueType.Integer)]
        public static DreamValue NativeProc_num2text(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue number = arguments.GetArgument(0, "N");

            if (number.IsType(DreamValueType.Number)) {
                return new DreamValue(number.GetValueAsNumber().ToString());
            } else {
                return new DreamValue("0");
            }
        }

        [DreamProc("oview")]
        [DreamProcParameter("Dist", Type = DreamValueType.Integer, DefaultValue = 5)]
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

            DreamList view = new DreamList();
            int centerX = center.GetVariable("x").GetValueAsInteger();
            int centerY = center.GetVariable("y").GetValueAsInteger();
            int centerZ = center.GetVariable("z").GetValueAsInteger();

            for (int x = Math.Max(centerX - distance, 1); x < Math.Min(centerX + distance, Program.DreamMap.Width); x++) {
                for (int y = Math.Max(centerY - distance, 1); y < Math.Min(centerY + distance, Program.DreamMap.Width); y++) {
                    if (x == centerX && y == centerY) continue;

                    DreamObject turf = Program.DreamMap.GetTurfAt(x, y, centerZ);

                    view.AddValue(new DreamValue(turf));
                    foreach (DreamValue content in turf.GetVariable("contents").GetValueAsDreamList().GetValues()) {
                        view.AddValue(content);
                    }
                }
            }

            return new DreamValue(view);
        }

        public static DreamList params2list(string queryString) {
            queryString = queryString.Replace(";", "&");
            NameValueCollection query = HttpUtility.ParseQueryString(queryString);
            DreamList list = new DreamList();

            foreach (string queryKey in query.AllKeys) {
                string[] queryValues = query.GetValues(queryKey);
                string queryValue = queryValues[queryValues.Length - 1]; //Use the last appearance of the key in the query

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
                result = new DreamList();
            }

            return new DreamValue(result);
        }

        [DreamProc("pick")]
        [DreamProcParameter("Val1")]
        public static DreamValue NativeProc_pick(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            List<DreamValue> values;

            if (arguments.ArgumentCount == 1) {
                DreamList list = arguments.GetArgument(0, "Val1").GetValueAsDreamList();

                values = list.GetValues();
            } else {
                values = arguments.GetAllArguments();
            }

            return values[new Random().Next(0, values.Count)];
        }

        [DreamProc("prob")]
        [DreamProcParameter("P", Type = DreamValueType.Number)]
        public static DreamValue NativeProc_prob(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            float probability = arguments.GetArgument(0, "P").GetValueAsNumber();

            return new DreamValue((new Random().Next(0, 100) <= probability) ? 1 : 0);
        }

        [DreamProc("rand")]
        [DreamProcParameter("L", Type = DreamValueType.Integer)]
        [DreamProcParameter("H", Type = DreamValueType.Integer)]
        public static DreamValue NativeProc_rand(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (arguments.ArgumentCount == 0) {
                return new DreamValue((float)new Random().NextDouble());
            } else if (arguments.ArgumentCount == 1) {
                int high = (int)Math.Floor(arguments.GetArgument(0, "L").GetValueAsNumber());

                return new DreamValue(new Random().Next(high));
            } else {
                int low = (int)Math.Floor(arguments.GetArgument(0, "L").GetValueAsNumber());
                int high = (int)Math.Floor(arguments.GetArgument(1, "H").GetValueAsNumber());

                return new DreamValue(new Random().Next(Math.Min(low, high), Math.Max(low, high)));
            }
        }

        [DreamProc("replacetext")]
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needle", Type = DreamValueType.String)]
        [DreamProcParameter("Replacement", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Integer, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Integer, DefaultValue = 0)]
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
        [DreamProcParameter("R", Type = DreamValueType.Number)]
        [DreamProcParameter("G", Type = DreamValueType.Number)]
        [DreamProcParameter("B", Type = DreamValueType.Number)]
        [DreamProcParameter("A", Type = DreamValueType.Number)]
        public static DreamValue NativeProc_rgb(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            int r = (int)arguments.GetArgument(0, "R").GetValueAsNumber();
            int g = (int)arguments.GetArgument(1, "G").GetValueAsNumber();
            int b = (int)arguments.GetArgument(2, "B").GetValueAsNumber();
            DreamValue aValue = arguments.GetArgument(3, "A");

            if (aValue.Value == null) {
                return new DreamValue(String.Format("#{0:X2}{1:X2}{2:X2}", r, g, b));
            } else {
                int a = (int)aValue.GetValueAsNumber();

                return new DreamValue(String.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", r, g, b, a));
            }
        }

        [DreamProc("replacetextEx")]
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needle", Type = DreamValueType.String)]
        [DreamProcParameter("Replacement", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Integer, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Integer, DefaultValue = 0)]
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
        [DreamProcParameter("A", Type = DreamValueType.Number)]
        [DreamProcParameter("B", Type = DreamValueType.Number)]
        public static DreamValue NativeProc_round(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            float a = arguments.GetArgument(0, "A").GetValueAsNumber();

            if (arguments.ArgumentCount == 1) {
                return new DreamValue((float)Math.Floor(a));
            } else {
                float b = arguments.GetArgument(1, "B").GetValueAsNumber();

                return new DreamValue((float)Math.Round(a / b) * b);
            }
        }

        [DreamProc("sin")]
        [DreamProcParameter("X", Type = DreamValueType.Number)]
        public static DreamValue NativeProc_sin(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue xValue = arguments.GetArgument(0, "X");
            float x = (xValue.Value == null) ? 0 : xValue.GetValueAsNumber();
            double rad = x * Math.PI / 180;

            return new DreamValue((float)Math.Sin(rad));
        }

        [DreamProc("sleep")]
        [DreamProcParameter("Delay", Type = DreamValueType.Number)]
        public static DreamValue NativeProc_sleep(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            float delay = arguments.GetArgument(0, "Delay").GetValueAsNumber();
            int delayMilliseconds = (int)(delay * 100);
            int ticksToSleep = (int)Math.Ceiling(delayMilliseconds / (Program.WorldInstance.GetVariable("tick_lag").GetValueAsNumber() * 100));

            CountdownEvent tickEvent = new CountdownEvent(ticksToSleep);
            Program.TickEvents.Add(tickEvent);
            tickEvent.Wait();

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
        [DreamProcParameter("repeat", Type = DreamValueType.Integer, DefaultValue = 0)]
        [DreamProcParameter("wait", Type = DreamValueType.Integer)]
        [DreamProcParameter("channel", Type = DreamValueType.Integer)]
        [DreamProcParameter("volume", Type = DreamValueType.Integer)]
        public static DreamValue NativeProc_sound(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamObject soundObject = Program.DreamObjectTree.CreateObject(DreamPath.Sound, arguments);

            return new DreamValue(soundObject);
        }

        [DreamProc("splittext")]
        [DreamProcParameter("Text", Type = DreamValueType.String)]
        [DreamProcParameter("Delimiter", Type = DreamValueType.String)]
        public static DreamValue NativeProc_splittext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string text = arguments.GetArgument(0, "Text").GetValueAsString();
            string delimiter = arguments.GetArgument(1, "Delimiter").GetValueAsString();
            string[] splitText = text.Split(delimiter);
            DreamList list = new DreamList(splitText);

            return new DreamValue(list);
        }

        [DreamProc("sqrt")]
        [DreamProcParameter("A", Type = DreamValueType.Number)]
        public static DreamValue NativeProc_sqrt(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            double a = arguments.GetArgument(0, "A").GetValueAsNumber();

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
            DreamConnection connection = Program.DreamServer.GetConnectionFromMob(usr);

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
            DreamConnection connection = Program.DreamServer.GetConnectionFromMob(usr);

            connection.SelectStatPanel(panel);
            if (name != DreamValue.Null || value != DreamValue.Null) {
                OutputToStatPanel(connection, name, value);
            }

            return new DreamValue(1); //TODO: Know when the client is looking at the panel
        }

        [DreamProc("text")]
        [DreamProcParameter("FormatText", Type = DreamValueType.String)]
        public static DreamValue NativeProc_text(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            return arguments.GetArgument(0, "FormatText"); //TODO: Format text
        }

        [DreamProc("text2ascii")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        [DreamProcParameter("pos", Type = DreamValueType.Integer, DefaultValue = 1)]
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

            return new DreamValue(Program.DreamResourceManager.SaveTextToFile(file, text) ? 1 : 0);
        }

        [DreamProc("text2num")]
        [DreamProcParameter("T", Type = DreamValueType.String | DreamValueType.Number | DreamValueType.DreamObject)]
        [DreamProcParameter("radix", Type = DreamValueType.Integer, DefaultValue = 10)]
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
            } else if (value.IsType(DreamValueType.Number)) {
                return new DreamValue(value.Value);
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

            if (Program.DreamObjectTree.HasTreeEntry(path)) {
                return new DreamValue(path);
            } else {
                return DreamValue.Null;
            }
        }

        [DreamProc("time2text")]
        [DreamProcParameter("timestamp", Type = DreamValueType.Integer)]
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
            DreamList list = new DreamList();

            foreach (DreamValue type in arguments.GetAllArguments()) {
                DreamPath typePath = type.GetValueAsPath();

                if (typePath.LastElement == "proc") {
                    DreamPath objectTypePath = typePath.AddToPath("..");
                    DreamObjectDefinition objectDefinition = Program.DreamObjectTree.GetObjectDefinitionFromPath(objectTypePath);

                    foreach (KeyValuePair<string, DreamProc> proc in objectDefinition.Procs) {
                        list.AddValue(new DreamValue(proc.Key));
                    }
                } else {
                    DreamObjectTree.DreamObjectTreeEntry objectTreeEntry = Program.DreamObjectTree.GetTreeEntryFromPath(typePath);
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

        [DreamProc("url_encode")]
        [DreamProcParameter("PlainText", Type = DreamValueType.String)]
        [DreamProcParameter("format", Type = DreamValueType.Integer, DefaultValue = 0)]
        public static DreamValue NativeProc_url_encode(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            string plainText = arguments.GetArgument(0, "PlainText").GetValueAsString();
            int format = arguments.GetArgument(1, "format").GetValueAsInteger();

            return new DreamValue(HttpUtility.UrlEncode(plainText));
        }

        [DreamProc("view")]
        [DreamProcParameter("Dist", Type = DreamValueType.Integer, DefaultValue = 5)]
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

            DreamList view = new DreamList();
            int centerX = center.GetVariable("x").GetValueAsInteger();
            int centerY = center.GetVariable("y").GetValueAsInteger();
            int centerZ = center.GetVariable("z").GetValueAsInteger();

            for (int x = Math.Max(centerX - distance, 1); x < Math.Min(centerX + distance, Program.DreamMap.Width); x++) {
                for (int y = Math.Max(centerY - distance, 1); y < Math.Min(centerY + distance, Program.DreamMap.Width); y++) {
                    DreamObject turf = Program.DreamMap.GetTurfAt(x, y, centerZ);

                    view.AddValue(new DreamValue(turf));
                    foreach (DreamValue content in turf.GetVariable("contents").GetValueAsDreamList().GetValues()) {
                        view.AddValue(content);
                    }
                }
            }

            return new DreamValue(view);
        }

        [DreamProc("viewers")]
        [DreamProcParameter("Depth", Type = DreamValueType.Integer)]
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

            DreamList view = new DreamList();
            int depth = (depthValue.Type == DreamValueType.Integer) ? depthValue.GetValueAsInteger() : 5; //TODO: Default to world.view
            int centerX = center.GetVariable("x").GetValueAsInteger();
            int centerY = center.GetVariable("y").GetValueAsInteger();

            foreach (DreamObject mob in DreamMetaObjectMob.Mobs) {
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
        [DreamProcParameter("Dir", Type = DreamValueType.Integer)]
        [DreamProcParameter("Lag", Type = DreamValueType.Integer, DefaultValue = 0)]
        [DreamProcParameter("Speed", Type = DreamValueType.Integer, DefaultValue = 0)]
        public static DreamValue NativeProc_walk(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            //TODO: Implement walk()

            return DreamValue.Null;
        }

        [DreamProc("walk_to")]
        [DreamProcParameter("Ref", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("Trg", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("Min", Type = DreamValueType.Integer, DefaultValue = 0)]
        [DreamProcParameter("Lag", Type = DreamValueType.Integer, DefaultValue = 0)]
        [DreamProcParameter("Speed", Type = DreamValueType.Integer, DefaultValue = 0)]
        public static DreamValue NativeProc_walk_to(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            //TODO: Implement walk_to()

            return DreamValue.Null;
        }
    }
}
