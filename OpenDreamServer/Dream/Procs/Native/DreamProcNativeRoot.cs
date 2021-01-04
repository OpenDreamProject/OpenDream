using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
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
        public static DreamValue NativeProc_abs(DreamProcScope scope, DreamProcArguments arguments) {
            float number = scope.GetValue("A").GetValueAsNumber();

            return new DreamValue(Math.Abs(number));
        }

        [DreamProc("animate")]
        [DreamProcParameter("Object", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("time", Type = DreamValueType.Integer)]
        [DreamProcParameter("loop", Type = DreamValueType.Integer)]
        [DreamProcParameter("easing", Type = DreamValueType.String)]
        [DreamProcParameter("flags", Type = DreamValueType.Integer)]
        public static DreamValue NativeProc_animate(DreamProcScope scope, DreamProcArguments arguments) {
            return new DreamValue((DreamObject)null);
        }

        [DreamProc("ascii2text")]
        [DreamProcParameter("N", Type = DreamValueType.Integer)]
        public static DreamValue NativeProc_ascii2text(DreamProcScope scope, DreamProcArguments arguments) {
            int ascii = scope.GetValue("N").GetValueAsInteger();

            return new DreamValue(Convert.ToChar(ascii).ToString());
        }

        [DreamProc("ckey")]
        [DreamProcParameter("Key", Type = DreamValueType.String)]
        public static DreamValue NativeProc_ckey(DreamProcScope scope, DreamProcArguments arguments) {
            string key = scope.GetValue("Key").GetValueAsString();

            key = Regex.Replace(key.ToLower(), "[^a-z]", ""); //Remove all punctuation and make lowercase
            return new DreamValue(key);
        }

        [DreamProc("copytext")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Integer, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Integer, DefaultValue = 0)]
        public static DreamValue NativeProc_copytext(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("T").GetValueAsString();
            int start = scope.GetValue("Start").GetValueAsInteger(); //1-indexed
            int end = scope.GetValue("End").GetValueAsInteger(); //1-indexed

            if (end <= 0) {
                end += text.Length + 1;
            } else if (end > text.Length + 1) {
                end = text.Length + 1;
            }

            return new DreamValue(text.Substring(start - 1, end - start));
        }

        [DreamProc("CRASH")]
        [DreamProcParameter("msg", Type = DreamValueType.String)]
        public static DreamValue NativeProc_CRASH(DreamProcScope scope, DreamProcArguments arguments) {
            string message = scope.GetValue("msg").GetValueAsString();

            throw new Exception(message);
        }

        [DreamProc("fcopy")]
        [DreamProcParameter("Src", Type = DreamValueType.String)]
        [DreamProcParameter("Dst", Type = DreamValueType.String)]
        public static DreamValue NativeProc_fcopy(DreamProcScope scope, DreamProcArguments arguments) {
            string src = scope.GetValue("Src").GetValueAsString();
            string dst = scope.GetValue("Dst").GetValueAsString();

            return new DreamValue(Program.DreamResourceManager.CopyFile(src, dst) ? 1 : 0);
        }

        [DreamProc("fcopy_rsc")]
        [DreamProcParameter("File", Type = DreamValueType.String)]
        public static DreamValue NativeProc_fcopy_rsc(DreamProcScope scope, DreamProcArguments arguments) {
            string filePath = scope.GetValue("File").GetValueAsString();

            return new DreamValue(Program.DreamResourceManager.LoadResource(filePath));
        }

        [DreamProc("fdel")]
        [DreamProcParameter("File", Type = DreamValueType.String)]
        public static DreamValue NativeProc_fdel(DreamProcScope scope, DreamProcArguments arguments) {
            string filePath = scope.GetValue("File").GetValueAsString();

            bool successful;
            if (filePath.EndsWith("/")) {
                successful = Program.DreamResourceManager.DeleteDirectory(filePath);
            } else {
                successful = Program.DreamResourceManager.DeleteFile(filePath);
            }

            return new DreamValue(successful ? 1 : 0);
        }

        [DreamProc("fexists")]
        [DreamProcParameter("File", Type = DreamValueType.String)]
        public static DreamValue NativeProc_fexists(DreamProcScope scope, DreamProcArguments arguments) {
            string filePath = scope.GetValue("File").GetValueAsString();

            return new DreamValue(Program.DreamResourceManager.DoesResourceExist(filePath) ? 1 : 0);
        }

        [DreamProc("file")]
        [DreamProcParameter("Path", Type = DreamValueType.String | DreamValueType.DreamResource)]
        public static DreamValue NativeProc_file(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue path = scope.GetValue("Path");

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
        public static DreamValue NativeProc_file2text(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue file = scope.GetValue("File");
            DreamResource resource;

            if (file.Type == DreamValueType.String) {
                resource = Program.DreamResourceManager.LoadResource(file.GetValueAsString());
            } else {
                resource = file.GetValueAsDreamResource();
            }

            return new DreamValue(resource.ReadAsString());
        }

        [DreamProc("findtext")]
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needle", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Integer, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Integer, DefaultValue = 0)]
        public static DreamValue NativeProc_findtext(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("Haystack").GetValueAsString().ToLower();
            string needle = scope.GetValue("Needle").GetValueAsString().ToLower();
            int start = scope.GetValue("Start").GetValueAsInteger(); //1-indexed
            int end = scope.GetValue("End").GetValueAsInteger(); //1-indexed

            if (end == 0) {
                end = text.Length + 1;
            }

            int needleIndex = text.Substring(start - 1, end - start).IndexOf(needle);
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
        public static DreamValue NativeProc_findtextEx(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("Haystack").GetValueAsString();
            string needle = scope.GetValue("Needle").GetValueAsString();
            int start = scope.GetValue("Start").GetValueAsInteger(); //1-indexed
            int end = scope.GetValue("End").GetValueAsInteger(); //1-indexed

            if (end == 0) {
                end = text.Length + 1;
            }

            int needleIndex = text.Substring(start - 1, end - start).IndexOf(needle);
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
        public static DreamValue NativeProc_findlasttext(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("Haystack").GetValueAsString().ToLower();
            string needle = scope.GetValue("Needle").GetValueAsString().ToLower();
            int start = scope.GetValue("Start").GetValueAsInteger(); //1-indexed
            int end = scope.GetValue("End").GetValueAsInteger(); //1-indexed

            if (end == 0) {
                end = text.Length + 1;
            }

            int needleIndex = text.Substring(start - 1, end - start).LastIndexOf(needle);
            if (needleIndex != -1) {
                return new DreamValue(needleIndex + 1); //1-indexed
            } else {
                return new DreamValue(0);
            }
        }

        [DreamProc("get_dist")]
        [DreamProcParameter("Loc1", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("Loc2", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_get_dist(DreamProcScope scope, DreamProcArguments arguments) {
            DreamObject loc1 = scope.GetValue("Loc1").GetValueAsDreamObjectOfType(DreamPath.Atom);
            DreamObject loc2 = scope.GetValue("Loc2").GetValueAsDreamObjectOfType(DreamPath.Atom);
            
            if (loc1 != loc2) {
                int x1 = loc1.GetVariable("x").GetValueAsInteger();
                int x2 = loc2.GetVariable("x").GetValueAsInteger();
                int y1 = loc1.GetVariable("y").GetValueAsInteger();
                int y2 = loc2.GetVariable("y").GetValueAsInteger();
                int dist = (int)Math.Floor(Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2)));

                return new DreamValue(dist);
            } else {
                return new DreamValue(-1);
            }
        }

        [DreamProc("html_decode")]
        [DreamProcParameter("HtmlText", Type = DreamValueType.String)]
        public static DreamValue NativeProc_html_decode(DreamProcScope scope, DreamProcArguments arguments) {
            string htmlText = scope.GetValue("HtmlText").GetValueAsString();

            return new DreamValue(HttpUtility.HtmlDecode(htmlText));
        }

        [DreamProc("html_encode")]
        [DreamProcParameter("PlainText", Type = DreamValueType.String)]
        public static DreamValue NativeProc_html_encode(DreamProcScope scope, DreamProcArguments arguments) {
            string plainText = scope.GetValue("PlainText").GetValueAsString();

            return new DreamValue(HttpUtility.HtmlEncode(plainText));
        }

        [DreamProc("image")]
        [DreamProcParameter("icon", Type = DreamValueType.DreamResource)]
        [DreamProcParameter("loc", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("icon_state", Type = DreamValueType.String)]
        [DreamProcParameter("layer", Type = DreamValueType.Number)]
        [DreamProcParameter("dir", Type = DreamValueType.Integer)]
        public static DreamValue NativeProc_image(DreamProcScope scope, DreamProcArguments arguments) {
            DreamObject imageObject = Program.DreamObjectTree.CreateObject(DreamPath.Image, arguments);

            return new DreamValue(imageObject);
        }

        [DreamProc("isarea")]
        [DreamProcParameter("Loc1", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_isarea(DreamProcScope scope, DreamProcArguments arguments) {
            List<DreamValue> locs = arguments.GetAllArguments();

            foreach (DreamValue loc in locs) {
                if (loc.TryGetValueAsDreamObject(out DreamObject locObject)) {
                    if (!locObject.IsSubtypeOf(DreamPath.Area)) {
                        return new DreamValue(0);
                    }
                } else {
                    return new DreamValue(0);
                }
            }

            return new DreamValue(1);
        }

        [DreamProc("isloc")]
        [DreamProcParameter("Loc1", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_isloc(DreamProcScope scope, DreamProcArguments arguments) {
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
        public static DreamValue NativeProc_ismob(DreamProcScope scope, DreamProcArguments arguments) {
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
        public static DreamValue NativeProc_isnull(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue value = scope.GetValue("Val");

            return new DreamValue((value.Value == null) ? 1 : 0);
        }

        [DreamProc("isnum")]
        [DreamProcParameter("Val")]
        public static DreamValue NativeProc_isnum(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue value = scope.GetValue("Val");

            return new DreamValue(value.IsType(DreamValueType.Number) ? 1 : 0);
        }

        [DreamProc("ispath")]
        [DreamProcParameter("Val")]
        [DreamProcParameter("Type", Type = DreamValueType.DreamPath)]
        public static DreamValue NativeProc_ispath(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue value = scope.GetValue("Val");
            DreamValue type = scope.GetValue("Type");

            if (value.Type == DreamValueType.DreamPath) {
                if (type.Value != null) {
                    if (value.GetValueAsPath().IsDescendantOf(type.GetValueAsPath())) {
                        return new DreamValue(1);
                    }
                } else {
                    return new DreamValue(1);
                }
            }
            
            return new DreamValue(0);
        }

        [DreamProc("istext")]
        [DreamProcParameter("Val")]
        public static DreamValue NativeProc_istext(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue value = scope.GetValue("Val");
            
            return new DreamValue((value.Type == DreamValueType.String) ? 1 : 0);
        }
        
        [DreamProc("isturf")]
        [DreamProcParameter("Loc1", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_isturf(DreamProcScope scope, DreamProcArguments arguments) {
            List<DreamValue> locs = arguments.GetAllArguments();

            foreach (DreamValue loc in locs) {
                if (loc.Type != DreamValueType.DreamObject || loc.Value == null || !loc.GetValueAsDreamObject().IsSubtypeOf(DreamPath.Turf)) {
                    return new DreamValue(0);
                }
            }

            return new DreamValue(1);
        }

        [DreamProc("istype")]
        [DreamProcParameter("Val")]
        [DreamProcParameter("Type", Type = DreamValueType.DreamPath)]
        public static DreamValue NativeProc_istype(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue value = scope.GetValue("Val");
            DreamValue type = scope.GetValue("Type");

            if (type.Value == null) {
                throw new NotImplementedException("Implicit type checking is not implemented");
            }

            if (value.TryGetValueAsDreamObjectOfType(type.GetValueAsPath(), out _)) {
                return new DreamValue(1);
            } else {
                return new DreamValue(0);
            }
        }

        private static DreamValue CreateValueFromJsonElement(JsonElement jsonElement) {
            if (jsonElement.ValueKind == JsonValueKind.Array) {
                DreamObject listObject = Program.DreamObjectTree.CreateObject(DreamPath.List);
                DreamList list = DreamMetaObjectList.DreamLists[listObject];

                foreach (JsonElement childElement in jsonElement.EnumerateArray()) {
                    DreamValue value = CreateValueFromJsonElement(childElement);

                    list.AddValue(value);
                }

                return new DreamValue(listObject);
            } else if (jsonElement.ValueKind == JsonValueKind.Object) {
                DreamObject listObject = Program.DreamObjectTree.CreateObject(DreamPath.List);
                DreamList list = DreamMetaObjectList.DreamLists[listObject];

                foreach (JsonProperty childProperty in jsonElement.EnumerateObject()) {
                    DreamValue value = CreateValueFromJsonElement(childProperty.Value);

                    list.SetValue(new DreamValue(childProperty.Name), value);
                }

                return new DreamValue(listObject);
            } else if (jsonElement.ValueKind == JsonValueKind.String) {
                return new DreamValue(jsonElement.GetString());
            } else if (jsonElement.ValueKind == JsonValueKind.Number) {
                return new DreamValue(jsonElement.GetUInt32());
            } else {
                throw new Exception("Invalid ValueKind " + jsonElement.ValueKind);
            }
        }

        public static object CreateJsonElementFromValue(DreamValue value) {
            if (value.IsType(DreamValueType.String | DreamValueType.Integer)) {
                return value.Value;
            } else if (value.Value == null) {
                return null;
            } else if (value.TryGetValueAsDreamObjectOfType(DreamPath.List, out DreamObject listObject)) {
                DreamList list = DreamMetaObjectList.DreamLists[listObject];

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
            } else {
                throw new Exception("Cannot json_encode " + value);
            }
        }

        [DreamProc("json_decode")]
        [DreamProcParameter("JSON", Type = DreamValueType.String)]
        public static DreamValue NativeProc_json_decode(DreamProcScope scope, DreamProcArguments arguments) {
            string jsonString = scope.GetValue("JSON").GetValueAsString();
            JsonElement jsonRoot = JsonSerializer.Deserialize<JsonElement>(jsonString);

            return CreateValueFromJsonElement(jsonRoot);
        }

        [DreamProc("json_encode")]
        [DreamProcParameter("Value")]
        public static DreamValue NativeProc_json_encode(DreamProcScope scope, DreamProcArguments arguments) {
            object jsonObject = CreateJsonElementFromValue(scope.GetValue("Value"));
            string result = JsonSerializer.Serialize(jsonObject);

            return new DreamValue(result);
        }

        [DreamProc("length")]
        [DreamProcParameter("E")]
        public static DreamValue NativeProc_length(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue value = scope.GetValue("E");

            if (value.Type == DreamValueType.String) {
                return new DreamValue(value.GetValueAsString().Length);
            } else if (value.Type == DreamValueType.DreamObject) {
                if (value.Value != null) {
                    DreamObject dreamObject = value.GetValueAsDreamObject();

                    if (dreamObject.IsSubtypeOf(DreamPath.List)) {
                        return dreamObject.GetVariable("len");
                    }
                } else {
                    return new DreamValue(0);
                }
            } else if (value.Type == DreamValueType.DreamPath) {
                return new DreamValue(0);
            }

            throw new Exception("Cannot check length of " + value + "");
        }

        [DreamProc("locate")]
        [DreamProcParameter("X", Type = DreamValueType.Integer)]
        [DreamProcParameter("Y", Type = DreamValueType.Integer)]
        [DreamProcParameter("Z", Type = DreamValueType.Integer)]
        public static DreamValue NativeProc_locate(DreamProcScope scope, DreamProcArguments arguments) {
            int x = scope.GetValue("X").GetValueAsInteger(); //1-indexed
            int y = scope.GetValue("Y").GetValueAsInteger(); //1-indexed
            int z = scope.GetValue("Z").GetValueAsInteger(); //1-indexed

            return new DreamValue(Program.DreamMap.GetTurfAt(x, y)); //TODO: Z
        }

        [DreamProc("log")]
        [DreamProcParameter("X", Type = DreamValueType.Number)]
        [DreamProcParameter("Y")]
        public static DreamValue NativeProc_log(DreamProcScope scope, DreamProcArguments arguments) {
            float x = scope.GetValue("X").GetValueAsNumber();
            DreamValue y = scope.GetValue("Y");

            if (y.Value != null) {
                return new DreamValue((float)Math.Log(y.GetValueAsNumber(), x));
            } else {
                return new DreamValue(Math.Log(x));
            }
        }

        [DreamProc("lowertext")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        public static DreamValue NativeProc_lowertext(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("T").GetValueAsString();

            return new DreamValue(text.ToLower());
        }

        [DreamProc("max")]
        [DreamProcParameter("A")]
        public static DreamValue NativeProc_max(DreamProcScope scope, DreamProcArguments arguments) {
            List<DreamValue> values;

            if (arguments.ArgumentCount == 1) {
                DreamObject listObject = scope.GetValue("A").GetValueAsDreamObjectOfType(DreamPath.List);
                DreamList list = DreamMetaObjectList.DreamLists[listObject];

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
                        if (value.GetValueAsFloat() < currentMax.GetValueAsFloat()) currentMax = value;
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
        public static DreamValue NativeProc_min(DreamProcScope scope, DreamProcArguments arguments) {
            List<DreamValue> values;

            if (arguments.ArgumentCount == 1) {
                DreamObject listObject = scope.GetValue("A").GetValueAsDreamObjectOfType(DreamPath.List);
                DreamList list = DreamMetaObjectList.DreamLists[listObject];

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
        public static DreamValue NativeProc_num2text(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue number = scope.GetValue("N");

            if (number.IsType(DreamValueType.Number)) {
                return new DreamValue(number.GetValueAsNumber().ToString());
            } else {
                return new DreamValue("0");
            }
        }

        [DreamProc("orange")]
        [DreamProcParameter("Dist", Type = DreamValueType.Integer)]
        [DreamProcParameter("Center", Type = DreamValueType.DreamObject)] //TODO: Default to usr
        public static DreamValue NativeProc_orange(DreamProcScope scope, DreamProcArguments arguments) {
            int distance = scope.GetValue("Dist").GetValueAsInteger();
            DreamObject center = scope.GetValue("Center").GetValueAsDreamObjectOfType(DreamPath.Atom);
            DreamObject orangeList = Program.DreamObjectTree.CreateObject(DreamPath.List);
            int centerX = center.GetVariable("x").GetValueAsInteger();
            int centerY = center.GetVariable("y").GetValueAsInteger();

            for (int x = Math.Max(centerX - distance, 1); x < Math.Min(centerX + distance, Program.DreamMap.Width); x++) {
                for (int y = Math.Max(centerY - distance, 1); y < Math.Min(centerY + distance, Program.DreamMap.Width); y++) {
                    if (x != centerX && y != centerY) {
                        DreamObject turf = Program.DreamMap.GetTurfAt(x, y);

                        orangeList.CallProc("Add", new DreamProcArguments(new List<DreamValue>() { new DreamValue(turf), turf.GetVariable("contents") }));
                    }
                }
            }

            return new DreamValue(orangeList);
        }

        public static DreamObject params2list(string queryString) {
            queryString = queryString.Replace(";", "&");
            NameValueCollection query = HttpUtility.ParseQueryString(queryString);
            DreamObject listObject = Program.DreamObjectTree.CreateObject(DreamPath.List);
            DreamList list = DreamMetaObjectList.DreamLists[listObject];

            foreach (string queryKey in query.AllKeys) {
                string queryValue = query.Get(queryKey);

                list.SetValue(new DreamValue(queryKey), new DreamValue(queryValue));
            }

            return listObject;
        }

        [DreamProc("params2list")]
        [DreamProcParameter("Params", Type = DreamValueType.String)]
        public static DreamValue NativeProc_params2list(DreamProcScope scope, DreamProcArguments arguments) {
            string paramsString = scope.GetValue("Params").GetValueAsString();
            
            return new DreamValue(params2list(paramsString));
        }

        [DreamProc("pick")]
        [DreamProcParameter("Val1")]
        public static DreamValue NativeProc_pick(DreamProcScope scope, DreamProcArguments arguments) {
            List<DreamValue> values;

            if (arguments.ArgumentCount == 1) {
                DreamObject listObject = scope.GetValue("Val1").GetValueAsDreamObjectOfType(DreamPath.List);
                DreamList list = DreamMetaObjectList.DreamLists[listObject];

                values = list.GetValues();
            } else {
                values = arguments.GetAllArguments();
            }

            return values[new Random().Next(0, values.Count)];
        }

        [DreamProc("prob")]
        [DreamProcParameter("P", Type = DreamValueType.Number)]
        public static DreamValue NativeProc_prob(DreamProcScope scope, DreamProcArguments arguments) {
            float probability = scope.GetValue("P").GetValueAsNumber();

            return new DreamValue((new Random().Next(0, 100) <= probability) ? 1 : 0);
        }

        [DreamProc("rand")]
        [DreamProcParameter("L", Type = DreamValueType.Integer)]
        [DreamProcParameter("H", Type = DreamValueType.Integer)]
        public static DreamValue NativeProc_rand(DreamProcScope scope, DreamProcArguments arguments) {
            if (arguments.ArgumentCount == 0) {
                return new DreamValue(new Random().NextDouble());
            } else {
                int low = arguments.GetArgument(0, "L").GetValueAsInteger();
                int high = arguments.GetArgument(1, "H").GetValueAsInteger();

                return new DreamValue(new Random().Next(Math.Min(low, high), Math.Max(low, high)));
            }
        }

        [DreamProc("replacetext")]
        [DreamProcParameter("Haystack", Type = DreamValueType.String)]
        [DreamProcParameter("Needle", Type = DreamValueType.String)]
        [DreamProcParameter("Replacement", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Integer, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Integer, DefaultValue = 0)]
        public static DreamValue NativeProc_replacetext(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("Haystack").GetValueAsString();
            string needle = scope.GetValue("Needle").GetValueAsString();
            string replacement = scope.GetValue("Replacement").GetValueAsString();
            int start = scope.GetValue("Start").GetValueAsInteger(); //1-indexed
            int end = scope.GetValue("End").GetValueAsInteger(); //1-indexed

            if (end == 0) {
                end = text.Length + 1;
            }

            return new DreamValue(text.Substring(start - 1, end - start).Replace(needle, replacement, StringComparison.OrdinalIgnoreCase));
        }

        [DreamProc("round")]
        [DreamProcParameter("A", Type = DreamValueType.Number)]
        [DreamProcParameter("B", Type = DreamValueType.Number)]
        public static DreamValue NativeProc_round(DreamProcScope scope, DreamProcArguments arguments) {
            float a = scope.GetValue("A").GetValueAsNumber();

            if (arguments.ArgumentCount == 1) {
                return new DreamValue((int)Math.Floor(a));
            } else {
                float b = scope.GetValue("B").GetValueAsNumber();

                return new DreamValue((float)Math.Round(a / b) * b);
            }
        }

        [DreamProc("sleep")]
        [DreamProcParameter("Delay", Type = DreamValueType.Number)]
        public static DreamValue NativeProc_sleep(DreamProcScope scope, DreamProcArguments arguments) {
            float delay = scope.GetValue("Delay").GetValueAsNumber();
            int delayMilliseconds = (int)(delay * 100);
            int ticksToSleep = (int)Math.Ceiling(delayMilliseconds / (Program.WorldInstance.GetVariable("tick_lag").GetValueAsNumber() * 100));

            CountdownEvent tickEvent = new CountdownEvent(ticksToSleep);
            Program.TickEvents.Add(tickEvent);
            tickEvent.Wait();

            return new DreamValue((DreamObject)null);
        }

        [DreamProc("sorttext")]
        [DreamProcParameter("T1", Type = DreamValueType.String)]
        [DreamProcParameter("T2", Type = DreamValueType.String)]
        public static DreamValue NativeProc_sorttext(DreamProcScope scope, DreamProcArguments arguments) {
            string t1 = scope.GetValue("T1").GetValueAsString().ToLower();
            string t2 = scope.GetValue("T2").GetValueAsString().ToLower();

            return new DreamValue(string.Compare(t2, t1));
        }

        [DreamProc("sorttextEx")]
        [DreamProcParameter("T1", Type = DreamValueType.String)]
        [DreamProcParameter("T2", Type = DreamValueType.String)]
        public static DreamValue NativeProc_sorttextEx(DreamProcScope scope, DreamProcArguments arguments) {
            string t1 = scope.GetValue("T1").GetValueAsString();
            string t2 = scope.GetValue("T2").GetValueAsString();

            return new DreamValue(string.Compare(t2, t1));
        }

        [DreamProc("sound")]
        [DreamProcParameter("file", Type = DreamValueType.DreamResource)]
        [DreamProcParameter("repeat", Type = DreamValueType.Integer, DefaultValue = 0)]
        [DreamProcParameter("wait", Type = DreamValueType.Integer)]
        [DreamProcParameter("channel", Type = DreamValueType.Integer)]
        [DreamProcParameter("volume", Type = DreamValueType.Integer)]
        public static DreamValue NativeProc_sound(DreamProcScope scope, DreamProcArguments arguments) {
            DreamObject soundObject = Program.DreamObjectTree.CreateObject(DreamPath.Sound, arguments);

            return new DreamValue(soundObject);
        }

        [DreamProc("splittext")]
        [DreamProcParameter("Text", Type = DreamValueType.String)]
        [DreamProcParameter("Delimiter", Type = DreamValueType.String)]
        public static DreamValue NativeProc_splittext(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("Text").GetValueAsString();
            string delimiter = scope.GetValue("Delimiter").GetValueAsString();
            string[] splitText = text.Split(delimiter);
            DreamObject listObject = Program.DreamObjectTree.CreateObject(DreamPath.List);
            DreamList list = DreamMetaObjectList.DreamLists[listObject];

            foreach (string value in splitText) {
                list.AddValue(new DreamValue(value));
            }

            return new DreamValue(listObject);
        }

        [DreamProc("text")]
        [DreamProcParameter("FormatText", Type = DreamValueType.String)]
        public static DreamValue NativeProc_text(DreamProcScope scope, DreamProcArguments arguments) {
            return scope.GetValue("FormatText"); //TODO: Format text
        }

        [DreamProc("text2ascii")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        [DreamProcParameter("pos", Type = DreamValueType.Integer, DefaultValue = 1)]
        public static DreamValue NativeProc_text2ascii(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("T").GetValueAsString();
            int pos = scope.GetValue("pos").GetValueAsInteger(); //1-indexed

            return new DreamValue((int)text[pos - 1]);
        }

        [DreamProc("text2file")]
        [DreamProcParameter("Text", Type = DreamValueType.String)]
        [DreamProcParameter("File", Type = DreamValueType.String)]
        public static DreamValue NativeProc_text2file(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("Text").GetValueAsString();
            string file = scope.GetValue("File").GetValueAsString();

            return new DreamValue(Program.DreamResourceManager.SaveTextToFile(file, text) ? 1 : 0);
        }

        [DreamProc("text2num")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        [DreamProcParameter("radix", Type = DreamValueType.Integer, DefaultValue = 10)]
        public static DreamValue NativeProc_text2num(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("T").GetValueAsString();
            int radix = scope.GetValue("radix").GetValueAsInteger();

            if (text.Length != 0) {
                if (text.Contains(".") && radix == 10) {
                    return new DreamValue(Convert.ToSingle(text));
                } else {
                    return new DreamValue(Convert.ToInt32(text, radix));
                }
            } else {
                return new DreamValue((DreamObject)null);
            }
        }

        [DreamProc("text2path")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        public static DreamValue NativeProc_text2path(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("T").GetValueAsString();
            DreamPath path = new DreamPath(text);

            if (Program.DreamObjectTree.HasTreeEntry(path)) {
                return new DreamValue(path);
            } else {
                return new DreamValue((DreamObject)null);
            }
        }

        [DreamProc("time2text")]
        [DreamProcParameter("timestamp", Type = DreamValueType.Integer)]
        [DreamProcParameter("format", Type = DreamValueType.String)]
        public static DreamValue NativeProc_time2text(DreamProcScope scope, DreamProcArguments arguments) {
            int timestamp = scope.GetValue("timestamp").GetValueAsInteger();
            string format = scope.GetValue("format").GetValueAsString();
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
        public static DreamValue NativeProc_typesof(DreamProcScope scope, DreamProcArguments arguments) {
            DreamObject listObject = Program.DreamObjectTree.CreateObject(DreamPath.List);
            DreamList list = DreamMetaObjectList.DreamLists[listObject];

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

            return new DreamValue(listObject);
        }

        [DreamProc("uppertext")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        public static DreamValue NativeProc_uppertext(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("T").GetValueAsString();

            return new DreamValue(text.ToUpper());
        }

        [DreamProc("url_encode")]
        [DreamProcParameter("PlainText", Type = DreamValueType.String)]
        [DreamProcParameter("format", Type = DreamValueType.Integer, DefaultValue = 0)]
        public static DreamValue NativeProc_url_encode(DreamProcScope scope, DreamProcArguments arguments) {
            string plainText = scope.GetValue("PlainText").GetValueAsString();
            int format = scope.GetValue("format").GetValueAsInteger();

            return new DreamValue(HttpUtility.UrlEncode(plainText));
        }

        [DreamProc("view")]
        [DreamProcParameter("Dist", Type = DreamValueType.Integer, DefaultValue = 4)]
        [DreamProcParameter("Center", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_view(DreamProcScope scope, DreamProcArguments arguments) { //TODO: View obstruction (dense turfs)
            int distance = 5;
            DreamObject center = scope.Usr;

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

            DreamObject viewList = Program.DreamObjectTree.CreateObject(DreamPath.List);
            int centerX = center.GetVariable("x").GetValueAsInteger();
            int centerY = center.GetVariable("y").GetValueAsInteger();

            for (int x = Math.Max(centerX - distance, 1); x < Math.Min(centerX + distance, Program.DreamMap.Width); x++) {
                for (int y = Math.Max(centerY - distance, 1); y < Math.Min(centerY + distance, Program.DreamMap.Width); y++) {
                    DreamObject turf = Program.DreamMap.GetTurfAt(x, y);

                    viewList.CallProc("Add", new DreamProcArguments(new List<DreamValue>() { new DreamValue(turf), turf.GetVariable("contents") }));
                }
            }

            return new DreamValue(viewList);
        }

        [DreamProc("viewers")]
        [DreamProcParameter("Depth", Type = DreamValueType.Integer)]
        [DreamProcParameter("Center", Type = DreamValueType.DreamObject)]
        public static DreamValue NativeProc_viewers(DreamProcScope scope, DreamProcArguments arguments) { //TODO: View obstruction (dense turfs)
            int depth = 5; //TODO: Default to world.view
            DreamObject center = scope.Usr;

            //Arguments are optional and can be passed in any order
            if (arguments.ArgumentCount > 0) {
                DreamValue firstArgument = arguments.GetArgument(0, "Depth");

                if (firstArgument.Type == DreamValueType.DreamObject) {
                    center = firstArgument.GetValueAsDreamObject();

                    if (arguments.ArgumentCount > 1) {
                        depth = arguments.GetArgument(1, "Center").GetValueAsInteger();
                    }
                } else {
                    depth = firstArgument.GetValueAsInteger();

                    if (arguments.ArgumentCount > 1) {
                        center = arguments.GetArgument(1, "Center").GetValueAsDreamObject();
                    }
                }
            }

            DreamObject viewList = Program.DreamObjectTree.CreateObject(DreamPath.List);
            int centerX = center.GetVariable("x").GetValueAsInteger();
            int centerY = center.GetVariable("y").GetValueAsInteger();

            foreach (DreamObject mob in DreamMetaObjectMob.Mobs) {
                int mobX = mob.GetVariable("x").GetValueAsInteger();
                int mobY = mob.GetVariable("y").GetValueAsInteger();

                if (Math.Abs(centerX - mobX) <= depth && Math.Abs(centerY - mobY) <= depth) {
                    viewList.CallProc("Add", new DreamProcArguments(new() { new DreamValue(mob) }));
                }
            }

            return new DreamValue(viewList);
        }

        [DreamProc("walk")]
        [DreamProcParameter("Ref", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("Dir", Type = DreamValueType.Integer)]
        [DreamProcParameter("Lag", Type = DreamValueType.Integer, DefaultValue = 0)]
        [DreamProcParameter("Speed", Type = DreamValueType.Integer, DefaultValue = 0)]
        public static DreamValue NativeProc_walk(DreamProcScope scope, DreamProcArguments arguments) {
            //TODO: Implement walk()

            return new DreamValue((DreamObject)null);
        }

        [DreamProc("walk_to")]
        [DreamProcParameter("Ref", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("Trg", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("Min", Type = DreamValueType.Integer, DefaultValue = 0)]
        [DreamProcParameter("Lag", Type = DreamValueType.Integer, DefaultValue = 0)]
        [DreamProcParameter("Speed", Type = DreamValueType.Integer, DefaultValue = 0)]
        public static DreamValue NativeProc_walk_to(DreamProcScope scope, DreamProcArguments arguments) {
            //TODO: Implement walk_to()

            return new DreamValue((DreamObject)null);
        }
    }
}
