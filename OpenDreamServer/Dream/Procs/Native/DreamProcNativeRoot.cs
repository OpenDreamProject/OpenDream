using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamServer.Resources;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace OpenDreamServer.Dream.Procs.Native {
    static class DreamProcNativeRoot {
        public static DreamValue NativeProc_abs(DreamProcScope scope, DreamProcArguments arguments) {
            double number = scope.GetValue("A").GetValueAsNumber();

            return new DreamValue(Math.Abs(number));
        }

        public static DreamValue NativeProc_animate(DreamProcScope scope, DreamProcArguments arguments) {
            return new DreamValue((DreamObject)null);
        }

        public static DreamValue NativeProc_ascii2text(DreamProcScope scope, DreamProcArguments arguments) {
            int ascii = scope.GetValue("N").GetValueAsInteger();

            return new DreamValue(Convert.ToChar(ascii).ToString());
        }

        public static DreamValue NativeProc_ckey(DreamProcScope scope, DreamProcArguments arguments) {
            string key = scope.GetValue("Key").GetValueAsString();

            key = Regex.Replace(key.ToLower(), "[^a-z]", ""); //Remove all punctuation and make lowercase
            return new DreamValue(key);
        }

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

        public static DreamValue NativeProc_CRASH(DreamProcScope scope, DreamProcArguments arguments) {
            string message = scope.GetValue("msg").GetValueAsString();

            throw new Exception(message);
        }

        public static DreamValue NativeProc_fcopy(DreamProcScope scope, DreamProcArguments arguments) {
            string src = scope.GetValue("Src").GetValueAsString();
            string dst = scope.GetValue("Dst").GetValueAsString();

            return new DreamValue(Program.DreamResourceManager.CopyFile(src, dst) ? 1 : 0);
        }

        public static DreamValue NativeProc_fcopy_rsc(DreamProcScope scope, DreamProcArguments arguments) {
            string filePath = scope.GetValue("File").GetValueAsString();

            return new DreamValue(Program.DreamResourceManager.LoadResource(filePath));
        }

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

        public static DreamValue NativeProc_fexists(DreamProcScope scope, DreamProcArguments arguments) {
            string filePath = scope.GetValue("File").GetValueAsString();

            return new DreamValue(Program.DreamResourceManager.DoesResourceExist(filePath) ? 1 : 0);
        }

        public static DreamValue NativeProc_file(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue path = scope.GetValue("Path");

            if (path.Type == DreamValue.DreamValueType.String) {
                DreamResource resource = Program.DreamResourceManager.LoadResource(path.GetValueAsString());

                return new DreamValue(resource);
            } else if (path.Type == DreamValue.DreamValueType.DreamResource) {
                return path;
            } else {
                throw new Exception("Invalid path argument");
            }
        }

        public static DreamValue NativeProc_file2text(DreamProcScope scope, DreamProcArguments arguments) {
            string filePath = scope.GetValue("File").GetValueAsString();
            DreamResource resource = Program.DreamResourceManager.LoadResource(filePath);

            return new DreamValue(resource.ReadAsString());
        }

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

        public static DreamValue NativeProc_get_dir(DreamProcScope scope, DreamProcArguments arguments) {
            DreamObject loc1 = scope.GetValue("Loc1").GetValueAsDreamObjectOfType(DreamPath.Atom);
            DreamObject loc2 = scope.GetValue("Loc2").GetValueAsDreamObjectOfType(DreamPath.Atom);
            int loc1X = loc1.GetVariable("x").GetValueAsInteger();
            int loc2X = loc2.GetVariable("x").GetValueAsInteger();
            int loc1Y = loc1.GetVariable("y").GetValueAsInteger();
            int loc2Y = loc2.GetVariable("y").GetValueAsInteger();
            AtomDirection direction = AtomDirection.South;

            if (loc2X < loc1X) {
                if (loc2Y == loc1Y) direction = AtomDirection.West;
                else direction = (loc2Y > loc1Y) ? AtomDirection.Northwest : AtomDirection.Southwest;
            } else if (loc2X > loc1X) {
                if (loc2Y == loc1Y) direction = AtomDirection.East;
                else direction = (loc2Y > loc1Y) ? AtomDirection.Northeast : AtomDirection.Southeast;
            } else if (loc2Y > loc1Y) {
                direction = AtomDirection.North;
            }

            return new DreamValue((int)direction);
        }

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

        public static DreamValue NativeProc_html_decode(DreamProcScope scope, DreamProcArguments arguments) {
            string htmlText = scope.GetValue("HtmlText").GetValueAsString();

            return new DreamValue(HttpUtility.HtmlDecode(htmlText));
        }

        public static DreamValue NativeProc_html_encode(DreamProcScope scope, DreamProcArguments arguments) {
            string plainText = scope.GetValue("PlainText").GetValueAsString();

            return new DreamValue(HttpUtility.HtmlEncode(plainText));
        }

        public static DreamValue NativeProc_image(DreamProcScope scope, DreamProcArguments arguments) {
            DreamObject imageObject = Program.DreamObjectTree.CreateObject(DreamPath.Image, arguments);

            return new DreamValue(imageObject);
        }

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

        public static DreamValue NativeProc_ismob(DreamProcScope scope, DreamProcArguments arguments) {
            List<DreamValue> locs = arguments.GetAllArguments();

            foreach (DreamValue loc in locs) {
                if (loc.Type != DreamValue.DreamValueType.DreamObject || loc.Value == null || !loc.GetValueAsDreamObject().IsSubtypeOf(DreamPath.Mob)) {
                     return new DreamValue(0);
                }
            }

            return new DreamValue(1);
        }

        public static DreamValue NativeProc_isnull(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue value = scope.GetValue("Val");

            return new DreamValue((value.Value == null) ? 1 : 0);
        }

        public static DreamValue NativeProc_isnum(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue value = scope.GetValue("Val");

            return new DreamValue(value.IsType(DreamValue.DreamValueType.Integer | DreamValue.DreamValueType.Double) ? 1 : 0);
        }

        public static DreamValue NativeProc_ispath(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue value = scope.GetValue("Val");
            DreamValue type = scope.GetValue("Type");

            if (value.Type == DreamValue.DreamValueType.DreamPath) {
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

        public static DreamValue NativeProc_istext(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue value = scope.GetValue("Val");
            
            return new DreamValue((value.Type == DreamValue.DreamValueType.String) ? 1 : 0);
        }
        
        public static DreamValue NativeProc_isturf(DreamProcScope scope, DreamProcArguments arguments) {
            List<DreamValue> locs = arguments.GetAllArguments();

            foreach (DreamValue loc in locs) {
                if (loc.Type != DreamValue.DreamValueType.DreamObject || loc.Value == null || !loc.GetValueAsDreamObject().IsSubtypeOf(DreamPath.Turf)) {
                    return new DreamValue(0);
                }
            }

            return new DreamValue(1);
        }

        public static DreamValue NativeProc_istype(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue value = scope.GetValue("Val");
            DreamValue type = scope.GetValue("Type");

            if (type.Value == null) {
                throw new NotImplementedException("Implicit type checking is not implemented");
            }

            if (value.TryGetValueAsDreamObject(out DreamObject valueObject) && valueObject != null) {
                return new DreamValue(valueObject.IsSubtypeOf(type.GetValueAsPath()) ? 1 : 0);
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
            if (value.IsType(DreamValue.DreamValueType.String | DreamValue.DreamValueType.Integer)) {
                return value.Value;
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

        public static DreamValue NativeProc_json_decode(DreamProcScope scope, DreamProcArguments arguments) {
            string jsonString = scope.GetValue("JSON").GetValueAsString();
            JsonElement jsonRoot = JsonSerializer.Deserialize<JsonElement>(jsonString);

            return CreateValueFromJsonElement(jsonRoot);
        }

        public static DreamValue NativeProc_json_encode(DreamProcScope scope, DreamProcArguments arguments) {
            object jsonObject = CreateJsonElementFromValue(scope.GetValue("Value"));
            string result = JsonSerializer.Serialize(jsonObject);

            return new DreamValue(result);
        }

        public static DreamValue NativeProc_length(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue value = scope.GetValue("E");

            if (value.Type == DreamValue.DreamValueType.String) {
                return new DreamValue(value.GetValueAsString().Length);
            } else if (value.Type == DreamValue.DreamValueType.DreamObject) {
                if (value.Value != null) {
                    DreamObject dreamObject = value.GetValueAsDreamObject();

                    if (dreamObject.IsSubtypeOf(DreamPath.List)) {
                        return dreamObject.GetVariable("len");
                    }
                } else {
                    return new DreamValue(0);
                }
            } else if (value.Type == DreamValue.DreamValueType.DreamPath) {
                return new DreamValue(0);
            }

            throw new Exception("Cannot check length of " + value + "");
        }

        public static DreamValue NativeProc_locate(DreamProcScope scope, DreamProcArguments arguments) {
            int x = scope.GetValue("X").GetValueAsInteger(); //1-indexed
            int y = scope.GetValue("Y").GetValueAsInteger(); //1-indexed
            int z = scope.GetValue("Z").GetValueAsInteger(); //1-indexed

            return new DreamValue(Program.DreamMap.GetTurfAt(x, y)); //TODO: Z
        }

        public static DreamValue NativeProc_lowertext(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("T").GetValueAsString();

            return new DreamValue(text.ToLower());
        }

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
                    if (value.Type == DreamValue.DreamValueType.Integer) {
                        if (value.GetValueAsInteger() > currentMax.GetValueAsInteger()) currentMax = value;
                    } else if (value.Type == DreamValue.DreamValueType.Double) {
                        if (value.GetValueAsDouble() < currentMax.GetValueAsDouble()) currentMax = value;
                    } else if (value.Type == DreamValue.DreamValueType.String) {
                        if (String.Compare(value.GetValueAsString(), currentMax.GetValueAsString()) > 0) currentMax = value;
                    }
                } else if (value.Type == DreamValue.DreamValueType.Integer && currentMax.Type == DreamValue.DreamValueType.Double) {
                    if (value.GetValueAsInteger() > currentMax.GetValueAsDouble()) currentMax = value;
                } else if (value.Type == DreamValue.DreamValueType.Double && currentMax.Type == DreamValue.DreamValueType.Integer) {
                    if (value.GetValueAsDouble() > currentMax.GetValueAsInteger()) currentMax = value;
                } else {
                    throw new Exception("Cannot compare " + currentMax + " and " + value);
                }
            }

            return currentMax;
        }

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
                    if (value.Type == DreamValue.DreamValueType.Integer) {
                        if (value.GetValueAsInteger() < currentMin.GetValueAsInteger()) currentMin = value;
                    } else if (value.Type == DreamValue.DreamValueType.Double) {
                        if (value.GetValueAsDouble() < currentMin.GetValueAsDouble()) currentMin = value;
                    } else if (value.Type == DreamValue.DreamValueType.String) {
                        if (String.Compare(value.GetValueAsString(), currentMin.GetValueAsString()) < 0) currentMin = value;
                    }
                } else if (value.Type == DreamValue.DreamValueType.Integer && currentMin.Type == DreamValue.DreamValueType.Double) {
                    if (value.GetValueAsInteger() < currentMin.GetValueAsDouble()) currentMin = value;
                } else if (value.Type == DreamValue.DreamValueType.Double && currentMin.Type == DreamValue.DreamValueType.Integer) {
                    if (value.GetValueAsDouble() < currentMin.GetValueAsInteger()) currentMin = value;
                } else if (value.Value == null) {
                    return value;
                } else {
                    throw new Exception("Cannot compare " + currentMin + " and " + value);
                }
            }

            return currentMin;
        }

        public static DreamValue NativeProc_num2text(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue number = scope.GetValue("N");

            if (number.IsType(DreamValue.DreamValueType.Integer | DreamValue.DreamValueType.Double)) {
                return new DreamValue(number.GetValueAsNumber().ToString());
            } else {
                return new DreamValue("0");
            }
        }

        public static DreamValue NativeProc_orange(DreamProcScope scope, DreamProcArguments arguments) {
            int distance = scope.GetValue("Dist").GetValueAsInteger();
            DreamObject center = scope.GetValue("Center").GetValueAsDreamObjectOfType(DreamPath.Atom); //TODO: Default to usr
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

        public static DreamValue NativeProc_params2list(DreamProcScope scope, DreamProcArguments arguments) {
            string paramsString = scope.GetValue("Params").GetValueAsString();
            
            return new DreamValue(params2list(paramsString));
        }

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

        public static DreamValue NativeProc_prob(DreamProcScope scope, DreamProcArguments arguments) {
            double probability = scope.GetValue("P").GetValueAsNumber();

            return new DreamValue((new Random().Next(0, 100) <= probability) ? 1 : 0);
        }

        public static DreamValue NativeProc_rand(DreamProcScope scope, DreamProcArguments arguments) {
            if (arguments.ArgumentCount == 0) {
                return new DreamValue(new Random().NextDouble());
            } else {
                int low = arguments.GetArgument(0, "L").GetValueAsInteger();
                int high = arguments.GetArgument(1, "H").GetValueAsInteger();

                return new DreamValue(new Random().Next(Math.Min(low, high), Math.Max(low, high)));
            }
        }

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

        public static DreamValue NativeProc_round(DreamProcScope scope, DreamProcArguments arguments) {
            double a = scope.GetValue("A").GetValueAsNumber();

            if (arguments.ArgumentCount == 1) {
                return new DreamValue((int)Math.Floor(a));
            } else {
                double b = scope.GetValue("B").GetValueAsNumber();

                return new DreamValue(Math.Round(a / b) * b);
            }
        }

        public static DreamValue NativeProc_sleep(DreamProcScope scope, DreamProcArguments arguments) {
            double delay = scope.GetValue("Delay").GetValueAsNumber();
            int delayMilliseconds = (int)(delay * 100);
            int ticksToSleep = (int)Math.Ceiling(delayMilliseconds / (Program.WorldInstance.GetVariable("tick_lag").GetValueAsNumber() * 100));

            CountdownEvent tickEvent = new CountdownEvent(ticksToSleep);
            Program.TickEvents.Add(tickEvent);
            tickEvent.Wait();

            return new DreamValue((DreamObject)null);
        }

        public static DreamValue NativeProc_sound(DreamProcScope scope, DreamProcArguments arguments) {
            DreamObject soundObject = Program.DreamObjectTree.CreateObject(DreamPath.Sound, arguments);

            return new DreamValue(soundObject);
        }

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

        public static DreamValue NativeProc_text(DreamProcScope scope, DreamProcArguments arguments) {
            return scope.GetValue("FormatText"); //TODO: Format text
        }

        public static DreamValue NativeProc_text2ascii(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("T").GetValueAsString();
            int pos = scope.GetValue("pos").GetValueAsInteger(); //1-indexed

            return new DreamValue((int)text[pos - 1]);
        }

        public static DreamValue NativeProc_text2file(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("Text").GetValueAsString();
            string file = scope.GetValue("File").GetValueAsString();

            return new DreamValue(Program.DreamResourceManager.SaveTextToFile(file, text) ? 1 : 0);
        }

        public static DreamValue NativeProc_text2num(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("T").GetValueAsString();
            int radix = scope.GetValue("radix").GetValueAsInteger();

            if (text.Length != 0) {
                return new DreamValue(Convert.ToInt32(text, radix));
            } else {
                return new DreamValue((DreamObject)null);
            }
        }

        public static DreamValue NativeProc_text2path(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("T").GetValueAsString();
            DreamPath path = new DreamPath(text);

            if (Program.DreamObjectTree.HasTreeEntry(path)) {
                return new DreamValue(path);
            } else {
                return new DreamValue((DreamObject)null);
            }
        }

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

        public static DreamValue NativeProc_uppertext(DreamProcScope scope, DreamProcArguments arguments) {
            string text = scope.GetValue("T").GetValueAsString();

            return new DreamValue(text.ToUpper());
        }

        public static DreamValue NativeProc_url_encode(DreamProcScope scope, DreamProcArguments arguments) {
            string plainText = scope.GetValue("PlainText").GetValueAsString();
            int format = scope.GetValue("format").GetValueAsInteger();

            return new DreamValue(HttpUtility.UrlEncode(plainText));
        }

        public static DreamValue NativeProc_view(DreamProcScope scope, DreamProcArguments arguments) { //TODO: View obstruction (dense turfs)
            int distance = 5;
            DreamObject center = scope.GetValue("usr").GetValueAsDreamObject();

            //Arguments are optional and can be passed in any order
            if (arguments.ArgumentCount > 0) {
                DreamValue firstArgument = arguments.GetArgument(0, "Dist");

                if (firstArgument.Type == DreamValue.DreamValueType.DreamObject) {
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

        public static DreamValue NativeProc_viewers(DreamProcScope scope, DreamProcArguments arguments) { //TODO: View obstruction (dense turfs)
            int depth = 5; //TODO: Default to world.view
            DreamObject center = scope.GetValue("usr").GetValueAsDreamObject();

            //Arguments are optional and can be passed in any order
            if (arguments.ArgumentCount > 0) {
                DreamValue firstArgument = arguments.GetArgument(0, "Depth");

                if (firstArgument.Type == DreamValue.DreamValueType.DreamObject) {
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

        public static DreamValue NativeProc_walk(DreamProcScope scope, DreamProcArguments arguments) {
            //TODO: Implement walk()

            return new DreamValue((DreamObject)null);
        }

        public static DreamValue NativeProc_walk_to(DreamProcScope scope, DreamProcArguments arguments) {
            //TODO: Implement walk_to()

            return new DreamValue((DreamObject)null);
        }
    }
}
