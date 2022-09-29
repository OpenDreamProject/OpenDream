using System.Collections.Specialized;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Resources;
using DreamValueType = OpenDreamRuntime.DreamValue.DreamValueType;

namespace OpenDreamRuntime.Procs.Native {
    static class DreamProcNativeRoot {
        // I don't want to edit 100 procs to have the DreamManager passed to them
        public static IDreamManager DreamManager;

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
            if (usrArgument.TryGetValueAsDreamObjectOfType(DreamPath.Mob, out var mob)) {
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
            if (!arguments.GetArgument(0, "Object").TryGetValueAsDreamObjectOfType(DreamPath.Atom, out var obj))
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
                if (arguments.NamedArguments.TryGetValue("pixel_x", out DreamValue pixelX)) {
                    obj.SetVariableValue("pixel_x", pixelX);
                    pixelX.TryGetValueAsInteger(out appearance.PixelOffset.X);
                }

                if (arguments.NamedArguments.TryGetValue("pixel_y", out DreamValue pixelY)) {
                    obj.SetVariableValue("pixel_y", pixelY);
                    pixelY.TryGetValueAsInteger(out appearance.PixelOffset.Y);
                }

                if (arguments.NamedArguments.TryGetValue("dir", out DreamValue dir)) {
                    obj.SetVariableValue("dir", dir);
                    dir.TryGetValueAsInteger(out int dirValue);
                    appearance.Direction = (AtomDirection)dirValue;
                }

                // TODO: Rest of the animatable vars
            });

            return DreamValue.Null;
        }

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
        public static DreamValue NativeProc_clamp(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            DreamValue value = arguments.GetArgument(0, "Value");

            if (!arguments.GetArgument(1, "Low").TryGetValueAsFloat(out float lVal))
                throw new Exception("Lower bound is not a number");
            if (!arguments.GetArgument(2, "High").TryGetValueAsFloat(out float hVal))
                throw new Exception("Upper bound is not a number");

            // BYOND supports switching low/high args around
            if (lVal > hVal)
            {
                (hVal, lVal) = (lVal, hVal);
            }

            if (value.TryGetValueAsDreamList(out DreamList list))
            {
                DreamList tmp = DreamList.Create();
                foreach (DreamValue val in list.GetValues())
                {
                    if (!val.TryGetValueAsFloat(out float floatVal))
                        continue;

                    tmp.AddValue(new DreamValue(Math.Clamp(floatVal, lVal, hVal)));
                }
                return new DreamValue(tmp);
            }
            else if (value.TryGetValueAsFloat(out float floatVal))
            {
                return new DreamValue(Math.Clamp(floatVal, lVal, hVal));
            }
            else
            {
                throw new Exception("Clamp expects a number or list");
            }
        }

        [DreamProc("cmptext")]
        [DreamProcParameter("T1", Type = DreamValueType.String)]
        public static DreamValue NativeProc_cmptext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            List<DreamValue> values = arguments.GetAllArguments();
            if (!values[0].TryGetValueAsString(out var t1))
            {
                return new DreamValue(0);
            }

            t1 = t1.ToLower();

            for (int i = 1; i < values.Count; i++) {
                if (!values[i].TryGetValueAsString(out var t2) || t2.ToLower() != t1) return new DreamValue(0);
            }

            return new DreamValue(1);
        }

        [DreamProc("copytext")]
        [DreamProcParameter("T", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_copytext(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            arguments.GetArgument(2, "End").TryGetValueAsInteger(out var end); //1-indexed

            if (!arguments.GetArgument(0, "T").TryGetValueAsString(out string text))
                return (end == 0) ? DreamValue.Null : new DreamValue("");
            if (!arguments.GetArgument(1, "Start").TryGetValueAsInteger(out int start)) //1-indexed
                return new DreamValue("");

            if (end <= 0) end += text.Length + 1;
            else if (end > text.Length + 1) end = text.Length + 1;

            if (start == 0) return new DreamValue("");
            else if (start < 0) start += text.Length + 1;

            return new DreamValue(text.Substring(start - 1, end - start));
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
            if (arguments.GetArgument(0, "msg").TryGetValueAsString(out var message))
            {
                throw new PropagatingRuntime(message);
            }

            throw new PropagatingRuntime("");
        }

        [DreamProc("fcopy")]
        [DreamProcParameter("Src", Type = DreamValueType.String | DreamValueType.DreamResource)]
        [DreamProcParameter("Dst", Type = DreamValueType.String)]
        public static DreamValue NativeProc_fcopy(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            var arg1 = arguments.GetArgument(0, "Src");

            string src;
            if (arg1.TryGetValueAsDreamResource(out DreamResource arg1Rsc)) {
                src = arg1Rsc.ResourcePath;
            } else if (!arg1.TryGetValueAsString(out src)) {
                throw new Exception("bad src file");
            }

            if (!arguments.GetArgument(1, "Dst").TryGetValueAsString(out var dst))
            {
                throw new Exception("bad dst file");
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
        [DreamProcParameter("size", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("color", Type = DreamValueType.String, DefaultValue = "#FFFFF")] // Must be a valid color
        [DreamProcParameter("flags", Type = DreamValueType.Float, DefaultValue = 0)] // No requirement to be a sane value, but will be rounded down to nearest integer*
        [DreamProcParameter("x", Type = DreamValueType.Float)]
        [DreamProcParameter("y", Type = DreamValueType.Float)]
        [DreamProcParameter("offset", Type = DreamValueType.Float)]
        [DreamProcParameter("threshold", Type = DreamValueType.String)] // Color string.
        [DreamProcParameter("alpha", Type = DreamValueType.Float, DefaultValue = 2255)]
        [DreamProcParameter("space", Type = DreamValueType.Float)] // Color spaces for filters are integers. Default value is RGB
        [DreamProcParameter("transform", Type = DreamValueType.DreamObject)] // transformation matrix
        [DreamProcParameter("blend_mode", Type = DreamValueType.Float)]
        [DreamProcParameter("factor", Type = DreamValueType.Float)]
        [DreamProcParameter("repeat", Type = DreamValueType.Float)]
        [DreamProcParameter("radius", Type = DreamValueType.Float)]
        [DreamProcParameter("falloff", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_filter(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {

            if (!arguments.GetArgument(0, "type").TryGetValueAsString(out var filter_type))
                return DreamValue.Null;

            DreamObject result;

            float x;
            float y;
            DreamResource icon;
            DreamResource render_source;
            float flags;
            float size;
            string color_string;
            string threshold_color;
            float threshold_strength;
            float offset;
            float alpha;
            DreamObject color_matrix = null;
            float space;
            DreamObject transform;
            float blend_mode;
            float density;
            float factor;
            float repeat;
            float radius;
            float falloff;

            switch(filter_type)
            {
                case "alpha":
                    if(!arguments.GetArgument(1, "x").TryGetValueAsFloat(out x)) //Horizontal offset of mask (defaults to 0)
                        x = 0;

                    if(!arguments.GetArgument(1, "y").TryGetValueAsFloat(out y)) //Vertical offset of mask (defaults to 0)
                        y = 0;

                    if(!arguments.GetArgument(2, "icon").TryGetValueAsDreamResource(out icon)) //Outline color (defaults to black)
                        icon = null; //TODO should this error?

                    if(!arguments.GetArgument(2, "render_source").TryGetValueAsDreamResource(out render_source)) //Outline color (defaults to black)
                        render_source = null; //TODO should this error?

                    if(!arguments.GetArgument(1, "flags").TryGetValueAsFloat(out flags)) //Defaults to 0
                        flags = 0;

                    result = DreamManager.ObjectTree.CreateObject(DreamPath.Filter.AddToPath(filter_type));
                    result.SetVariableValue("type", new DreamValue(filter_type));
                    result.SetVariableValue("x", new DreamValue(x));
                    result.SetVariableValue("y", new DreamValue(y));
                    result.SetVariableValue("icon", new DreamValue(icon));
                    result.SetVariableValue("render_source", new DreamValue(render_source));
                    result.SetVariableValue("flags", new DreamValue(flags));
                    return new DreamValue(result);
                case "angular_blur":
                    if(!arguments.GetArgument(1, "x").TryGetValueAsFloat(out x)) //Horizontal offset of mask (defaults to 0)
                        x = 0;

                    if(!arguments.GetArgument(1, "y").TryGetValueAsFloat(out y)) //Vertical offset of mask (defaults to 0)
                        y = 0;

                    if(!arguments.GetArgument(1, "size").TryGetValueAsFloat(out size)) //Width in pixels (defaults to 1)
                        size = 1;

                    result = DreamManager.ObjectTree.CreateObject(DreamPath.Filter.AddToPath(filter_type));
                    result.SetVariableValue("type", new DreamValue(filter_type));
                    result.SetVariableValue("x", new DreamValue(x));
                    result.SetVariableValue("y", new DreamValue(y));
                    result.SetVariableValue("size", new DreamValue(size));
                    return new DreamValue(result);
                case "bloom":
                    if(!arguments.GetArgument(2, "threshold").TryGetValueAsString(out threshold_color)) //Color threshold for bloom
                        threshold_color = "#000000";
                    if(!arguments.GetArgument(1, "size").TryGetValueAsFloat(out size)) //Blur radius of bloom effect (see Gaussian blur)
                        size = 1;
                    if(!arguments.GetArgument(1, "offset").TryGetValueAsFloat(out offset)) //Growth/outline radius of bloom effect before blur
                        offset = 1;
                    if(!arguments.GetArgument(1, "alpha").TryGetValueAsFloat(out alpha)) //Opacity of effect (default is 255, max opacity)
                        alpha = 255;
                    result = DreamManager.ObjectTree.CreateObject(DreamPath.Filter.AddToPath(filter_type));
                    result.SetVariableValue("type", new DreamValue(filter_type));
                    result.SetVariableValue("threshold", new DreamValue(threshold_color));
                    result.SetVariableValue("size", new DreamValue(size));
                    result.SetVariableValue("offset", new DreamValue(offset));
                    result.SetVariableValue("alpha", new DreamValue(alpha));
                    return new DreamValue(result);
                case "blur":
                    if(!arguments.GetArgument(1, "size").TryGetValueAsFloat(out size)) //Amount of blur (defaults to 1)
                        size = 1;
                    result = DreamManager.ObjectTree.CreateObject(DreamPath.Filter.AddToPath(filter_type));
                    result.SetVariableValue("type", new DreamValue(filter_type));
                    result.SetVariableValue("size", new DreamValue(size));

                    return new DreamValue(result);
                case "color":
                    if(!arguments.GetArgument(2, "color").TryGetValueAsDreamObjectOfType(DreamPath.Matrix, out color_matrix)) //A color matrix
                        color_matrix = DreamManager.ObjectTree.CreateObject(DreamPath.Matrix);
                    if(!arguments.GetArgument(1, "space").TryGetValueAsFloat(out space)) //Value indicating color space: defaults to FILTER_COLOR_RGB
                        space = 0; //#define FILTER_COLOR_RGB 0

                    result = DreamManager.ObjectTree.CreateObject(DreamPath.Filter.AddToPath(filter_type));
                    result.SetVariableValue("type", new DreamValue(filter_type));
                    result.SetVariableValue("color", new DreamValue(color_matrix));
                    result.SetVariableValue("space", new DreamValue(space));
                    return new DreamValue(result);
                case "displace":
                    if(!arguments.GetArgument(1, "x").TryGetValueAsFloat(out x)) //Horizontal offset of mask (defaults to 0)
                        x = 0;

                    if(!arguments.GetArgument(1, "y").TryGetValueAsFloat(out y)) //Vertical offset of mask (defaults to 0)
                        y = 0;
                    if(!arguments.GetArgument(1, "size").TryGetValueAsFloat(out size)) //Maximum distortion, in pixels
                        size = 1;
                    if(!arguments.GetArgument(2, "icon").TryGetValueAsDreamResource(out icon)) //Icon to use as a displacement map
                        icon = null; //TODO should this error?

                    if(!arguments.GetArgument(2, "render_source").TryGetValueAsDreamResource(out render_source)) //render_target to use as a displacement map
                        render_source = null; //TODO should this error?

                    result = DreamManager.ObjectTree.CreateObject(DreamPath.Filter.AddToPath(filter_type));
                    result.SetVariableValue("type", new DreamValue(filter_type));
                    result.SetVariableValue("x", new DreamValue(x));
                    result.SetVariableValue("y", new DreamValue(y));
                    result.SetVariableValue("size", new DreamValue(size));
                    result.SetVariableValue("icon", new DreamValue(icon));
                    result.SetVariableValue("render_source", new DreamValue(render_source));

                    return new DreamValue(result);
                case "drop_shadow":
                    if(!arguments.GetArgument(1, "x").TryGetValueAsFloat(out x)) //Shadow horizontal offset (defaults to 1)
                        x = 1;
                    if(!arguments.GetArgument(1, "y").TryGetValueAsFloat(out y)) //Shadow horizontal offset (defaults to -1)
                        y = -1;
                    if(!arguments.GetArgument(1, "size").TryGetValueAsFloat(out size)) //Blur amount (defaults to 1; negative values create inset shadows)
                        size = 1;
                    if(!arguments.GetArgument(1, "offset").TryGetValueAsFloat(out offset)) //Size increase before blur (defaults to 0)
                        offset = 0;
                    if(!arguments.GetArgument(2, "color").TryGetValueAsString(out color_string)) //Shadow color (defaults to 50% transparent black)
                        color_string = "#00000088";
                    result = DreamManager.ObjectTree.CreateObject(DreamPath.Filter.AddToPath(filter_type));
                    result.SetVariableValue("type", new DreamValue(filter_type));
                    result.SetVariableValue("x", new DreamValue(x));
                    result.SetVariableValue("y", new DreamValue(y));
                    result.SetVariableValue("size", new DreamValue(size));
                    result.SetVariableValue("offset", new DreamValue(offset));
                    result.SetVariableValue("color", new DreamValue(color_string));
                    return new DreamValue(result);
                case "layer":
                    if(!arguments.GetArgument(1, "x").TryGetValueAsFloat(out x)) //Horizontal offset of second image (defaults to 0)
                        x = 0;
                    if(!arguments.GetArgument(1, "y").TryGetValueAsFloat(out y)) //Vertical offset of second image (defaults to 0)
                        y = 0;
                    if(!arguments.GetArgument(2, "icon").TryGetValueAsDreamResource(out icon)) //Icon to use as a second image
                        icon = null; //TODO should this error?
                    if(!arguments.GetArgument(2, "render_source").TryGetValueAsDreamResource(out render_source)) //Icon to use as a second image
                        render_source = null; //TODO should this error?
                    if(!arguments.GetArgument(1, "flags").TryGetValueAsFloat(out flags)) //FILTER_OVERLAY (default) or FILTER_UNDERLAY
                        flags = 0; //#define FILTER_OVERLAY 0
                    if(!arguments.GetArgument(2, "color").TryGetValueAsString(out color_string)) //Color or color matrix to apply to second image
                        if(!arguments.GetArgument(2, "color").TryGetValueAsDreamObjectOfType(DreamPath.Matrix, out color_matrix)) //A color matrix
                            color_matrix = DreamManager.ObjectTree.CreateObject(DreamPath.Matrix);
                        else
                            color_string = "#00000088";
                    if(!arguments.GetArgument(2, "transform").TryGetValueAsDreamObjectOfType(DreamPath.Matrix, out transform)) //Transform to apply to second image
                        transform = DreamManager.ObjectTree.CreateObject(DreamPath.Matrix);
                    if(!arguments.GetArgument(1, "blend_mode").TryGetValueAsFloat(out blend_mode)) //Blend mode to apply to the top image
                        blend_mode = 0;

                    result = DreamManager.ObjectTree.CreateObject(DreamPath.Filter.AddToPath(filter_type));
                    result.SetVariableValue("type", new DreamValue(filter_type));
                    result.SetVariableValue("x", new DreamValue(x));
                    result.SetVariableValue("y", new DreamValue(y));
                    result.SetVariableValue("icon", new DreamValue(icon));
                    result.SetVariableValue("render_source", new DreamValue(render_source));
                    result.SetVariableValue("flags", new DreamValue(flags));
                    result.SetVariableValue("color", new DreamValue(color_matrix == null ? color_string : color_matrix));
                    result.SetVariableValue("transform", new DreamValue(transform));
                    result.SetVariableValue("blend_mode", new DreamValue(blend_mode));
                    return new DreamValue(result);
                case "motion_blur":
                    if(!arguments.GetArgument(1, "x").TryGetValueAsFloat(out x)) //Blur vector on the X axis (defaults to 0)
                        x = 0;

                    if(!arguments.GetArgument(1, "y").TryGetValueAsFloat(out y)) //Blur vector on the Y axis (defaults to 0)
                        y = 0;

                    result = DreamManager.ObjectTree.CreateObject(DreamPath.Filter.AddToPath(filter_type));
                    result.SetVariableValue("type", new DreamValue(filter_type));
                    result.SetVariableValue("x", new DreamValue(x));
                    result.SetVariableValue("y", new DreamValue(y));

                    return new DreamValue(result);
                case "outline":
                    if(!arguments.GetArgument(1, "size").TryGetValueAsFloat(out size)) //Width in pixels (defaults to 1)
                        size = 1;
                    if(!arguments.GetArgument(2, "color").TryGetValueAsString(out color_string)) //Outline color (defaults to black)
                        color_string = "#000000";
                    if(!arguments.GetArgument(1, "flags").TryGetValueAsFloat(out flags)) //Defaults to 0
                        flags = 0;

                    result = DreamManager.ObjectTree.CreateObject(DreamPath.Filter.AddToPath(filter_type));
                    result.SetVariableValue("type", new DreamValue(filter_type));
                    result.SetVariableValue("size", new DreamValue(size));
                    result.SetVariableValue("color", new DreamValue(color_string));
                    result.SetVariableValue("flags", new DreamValue(flags));

                    return new DreamValue(result);
                case "radial_blur":
                    if(!arguments.GetArgument(1, "x").TryGetValueAsFloat(out x)) //Horizontal center of effect, in pixels, relative to image center
                        x = 0;

                    if(!arguments.GetArgument(1, "y").TryGetValueAsFloat(out y)) //Vertical center of effect, in pixels, relative to image center
                        y = 0;

                    if(!arguments.GetArgument(1, "size").TryGetValueAsFloat(out size)) //Amount of blur per pixel of distance (defaults to 0.01)
                        size = 0.01f;

                    result = DreamManager.ObjectTree.CreateObject(DreamPath.Filter.AddToPath(filter_type));
                    result.SetVariableValue("type", new DreamValue(filter_type));
                    result.SetVariableValue("x", new DreamValue(x));
                    result.SetVariableValue("y", new DreamValue(y));
                    result.SetVariableValue("size", new DreamValue(size));
                    return new DreamValue(result);
                case "rays":
                    if(!arguments.GetArgument(1, "x").TryGetValueAsFloat(out x)) //Horiztonal position of ray center, relative to image center (defaults to 0)
                        x = 0;
                    if(!arguments.GetArgument(1, "y").TryGetValueAsFloat(out y)) //Vertical position of ray center, relative to image center (defaults to 0)
                        y = 0;
                    if(!arguments.GetArgument(1, "size").TryGetValueAsFloat(out size)) //Maximum length of rays (defaults to 1/2 tile width)
                        size = 1;
                    if(!arguments.GetArgument(2, "color").TryGetValueAsString(out color_string)) //Ray color (defaults to white)
                        color_string = "#FFFFFF";
                    if(!arguments.GetArgument(1, "offset").TryGetValueAsFloat(out offset)) //"Time" offset of rays (defaults to 0, repeats after 1000)
                        offset = 0;
                    if(!arguments.GetArgument(1, "density").TryGetValueAsFloat(out density)) //Higher values mean more, narrower rays (defaults to 10, must be whole number)
                        density = 0;
                    if(!arguments.GetArgument(1, "threshold").TryGetValueAsFloat(out threshold_strength)) //Low-end cutoff for ray strength (defaults to 0.5, can be 0 to 1)
                        threshold_strength = 0;
                    if(!arguments.GetArgument(1, "factor").TryGetValueAsFloat(out factor)) //How much ray strength is related to ray length (defaults to 0, can be 0 to 1)
                        factor = 0;
                    if(!arguments.GetArgument(1, "flags").TryGetValueAsFloat(out flags)) //Defaults to FILTER_OVERLAY | FILTER_UNDERLAY
                        flags = 1;

                    result = DreamManager.ObjectTree.CreateObject(DreamPath.Filter.AddToPath(filter_type));
                    result.SetVariableValue("type", new DreamValue(filter_type));
                    result.SetVariableValue("x", new DreamValue(x));
                    result.SetVariableValue("y", new DreamValue(y));
                    result.SetVariableValue("size", new DreamValue(size));
                    result.SetVariableValue("color", new DreamValue(color_string));
                    result.SetVariableValue("offset", new DreamValue(offset));
                    result.SetVariableValue("density", new DreamValue(density));
                    result.SetVariableValue("threshold", new DreamValue(threshold_strength));
                    result.SetVariableValue("factor", new DreamValue(factor));
                    result.SetVariableValue("flags", new DreamValue(flags));
                    return new DreamValue(result);
                case "ripple":
                    if(!arguments.GetArgument(1, "x").TryGetValueAsFloat(out x)) //Horiztonal position of ripple center, relative to image center (defaults to 0)
                        x = 1;
                    if(!arguments.GetArgument(1, "y").TryGetValueAsFloat(out y)) //Vertical position of ripple center, relative to image center (defaults to 0)
                        y = -1;
                    if(!arguments.GetArgument(1, "size").TryGetValueAsFloat(out size)) //Maximum distortion in pixels (defaults to 1)
                        size = 1;
                    if(!arguments.GetArgument(1, "repeat").TryGetValueAsFloat(out repeat)) //Wave period, in pixels (defaults to 2)
                        repeat = 0;
                    if(!arguments.GetArgument(1, "radius").TryGetValueAsFloat(out radius)) //Outer radius of ripple, in pixels (defaults to 0)
                        radius = 0;
                    if(!arguments.GetArgument(1, "falloff").TryGetValueAsFloat(out falloff)) //How quickly ripples lose strength away from the outer edge (defaults to 1)
                        falloff = 0;
                    if(!arguments.GetArgument(1, "flags").TryGetValueAsFloat(out flags)) //Defaults to 0; use WAVE_BOUNDED to keep distortion within the image
                        flags = 1;
                    result = DreamManager.ObjectTree.CreateObject(DreamPath.Filter.AddToPath(filter_type));
                    result.SetVariableValue("type", new DreamValue(filter_type));
                    result.SetVariableValue("x", new DreamValue(x));
                    result.SetVariableValue("y", new DreamValue(y));
                    result.SetVariableValue("size", new DreamValue(size));
                    result.SetVariableValue("repeat", new DreamValue(repeat));
                    result.SetVariableValue("radius", new DreamValue(radius));
                    result.SetVariableValue("falloff", new DreamValue(falloff));
                    result.SetVariableValue("flags", new DreamValue(flags));

                    return new DreamValue(result);
                case "wave":
                    if(!arguments.GetArgument(1, "x").TryGetValueAsFloat(out x)) //Horiztonal direction and period of wave
                        x = 1;
                    if(!arguments.GetArgument(1, "y").TryGetValueAsFloat(out y)) //Vertical direction and period of wave
                        y = -1;
                    if(!arguments.GetArgument(1, "size").TryGetValueAsFloat(out size)) //Maximum distortion in pixels (defaults to 1)
                        size = 1;
                    if(!arguments.GetArgument(1, "offset").TryGetValueAsFloat(out offset)) //Phase of wave, in periods (e.g., 0 to 1)
                        offset = 0;
                    if(!arguments.GetArgument(1, "flags").TryGetValueAsFloat(out flags)) //Defaults to 0; see below for other flags
                        flags = 1;
                    result = DreamManager.ObjectTree.CreateObject(DreamPath.Filter.AddToPath(filter_type));
                    result.SetVariableValue("type", new DreamValue(filter_type));
                    result.SetVariableValue("x", new DreamValue(x));
                    result.SetVariableValue("y", new DreamValue(y));
                    result.SetVariableValue("size", new DreamValue(size));
                    result.SetVariableValue("offset", new DreamValue(offset));
                    result.SetVariableValue("flags", new DreamValue(flags));

                    return new DreamValue(result);
                case "greyscale":
                    result = DreamManager.ObjectTree.CreateObject(DreamPath.Filter.AddToPath(filter_type));
                    result.SetVariableValue("type", new DreamValue(filter_type));
                    return new DreamValue(result);
                default:
                    return DreamValue.Null; //no valid type? You get a null
            }
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
            if(!arguments.GetArgument(0, "Path").TryGetValueAsString(out var path))
            {
                path = IoCManager.Resolve<DreamResourceManager>().RootPath + Path.DirectorySeparatorChar;
            }
            var resourceManager = IoCManager.Resolve<DreamResourceManager>();
            var listing = resourceManager.EnumerateListing(path);
            DreamList list = DreamList.Create(listing);
            return new DreamValue(list);
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
        public static DreamValue NativeProc_icon_states(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            var mode = arguments.GetArgument(1, "mode").GetValueAsInteger();
            if (mode != 0)
            {
                throw new NotImplementedException("Only mode 0 is implemented");
            }

            var arg = arguments.GetArgument(0, "Icon");

            if (arg.Equals(DreamValue.Null))
            {
                return DreamValue.Null;
            }

            if (!arg.TryGetValueAsDreamResource(out var resource))
            {
                throw new Exception("bad icon");
            }

            DMIParser.ParsedDMIDescription parsedDMI = DMIParser.ParseDMI(new MemoryStream(resource.ResourceData));

            return new DreamValue(DreamList.Create(parsedDMI.States.Keys.ToArray()));
        }

        [DreamProc("image")]
        [DreamProcParameter("icon", Type = DreamValueType.DreamResource)]
        [DreamProcParameter("loc", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("icon_state", Type = DreamValueType.String)]
        [DreamProcParameter("layer", Type = DreamValueType.Float)]
        [DreamProcParameter("dir", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_image(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamObject imageObject = DreamManager.ObjectTree.CreateObject(DreamPath.Image);
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
                if (loc.TryGetValueAsDreamObject(out DreamObject? locObject) && locObject is not null) {
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
                if (!loc.TryGetValueAsDreamObjectOfType(DreamPath.Mob, out _))
                    return new DreamValue(0);
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
        [DreamProcParameter("Type", Type = DreamValueType.DreamPath)]
        public static DreamValue NativeProc_ispath(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue value = arguments.GetArgument(0, "Val");
            DreamValue type = arguments.GetArgument(1, "Type");

            if (value.TryGetValueAsPath(out DreamPath valuePath)) {
                if (type.TryGetValueAsPath(out DreamPath typePath)) {
                    DreamObjectDefinition valueDefinition = DreamManager.ObjectTree.GetObjectDefinition(valuePath);

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

        private static DreamValue CreateValueFromJsonElement(JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Array:
                {
                    DreamList list = DreamList.Create();

                    foreach (JsonElement childElement in jsonElement.EnumerateArray()) {
                        DreamValue value = CreateValueFromJsonElement(childElement);

                        list.AddValue(value);
                    }

                    return new DreamValue(list);
                }
                case JsonValueKind.Object:
                {
                    DreamList list = DreamList.Create();

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
        /// A helper function for /proc/json_encode(). Takes in a value and returns its json-able equivalent.
        /// </summary>
        /// <returns>Something that <see cref="JsonSerializer.Serialize"/> can parse.</returns>
        public static object? CreateJsonElementFromValue(DreamValue value) {
            return CreateJsonElementFromValueRecursive(value, 0);
        }

        /// <remarks> This exists to allow for some control over the recursion.<br/>
        /// DM is actually not very smart about deep recursion or lists referencing their parents; it just goes to ~19 depth and gives up.</remarks>
        private static object? CreateJsonElementFromValueRecursive(DreamValue value, int recursionLevel) {
            const int maximumRecursions = 20; // In parity with DM, we give up and just print a 'null' at the maximum recursion.
            if (recursionLevel == maximumRecursions)
                return null; // This will be turned into the string "null" higher up in the stack.

            if(value.TryGetValueAsFloat(out float floatValue))
                return floatValue;
            if (value.TryGetValueAsString(out string text))
                return HttpUtility.JavaScriptStringEncode(text);
            if (value.TryGetValueAsPath(out var path))
                return HttpUtility.JavaScriptStringEncode(path.ToString());
            if (value.TryGetValueAsDreamList(out DreamList list)) {
                if (list.IsAssociative) {
                    Dictionary<Object, Object?> jsonObject = new(list.GetLength());

                    foreach (DreamValue listValue in list.GetValues()) {
                        if (list.ContainsKey(listValue)) {
                            jsonObject.Add(HttpUtility.JavaScriptStringEncode(listValue.Stringify()), // key
                                           CreateJsonElementFromValueRecursive(list.GetValue(listValue), recursionLevel+1)); // value
                        } else {
                            jsonObject.Add(CreateJsonElementFromValueRecursive(listValue, recursionLevel + 1), null); // list[x] = null
                        }
                    }

                    return jsonObject;
                }
                List<Object?> jsonArray = new();
                foreach (DreamValue listValue in list.GetValues()) {
                    jsonArray.Add(CreateJsonElementFromValueRecursive(listValue, recursionLevel + 1));
                }

                return jsonArray;
            }
            if (value.Type == DreamValueType.DreamObject) {
                if (value.Value == null) return null;
                return value.Stringify();
            }
            if(value.Type == DreamValueType.DreamResource) {
                DreamResource dreamResource = (DreamResource)value.Value;
                string? output = dreamResource.ReadAsString();
                if (output == null)
                    return "";
                return output;
            }
            throw new Exception("Cannot json_encode " + value);
        }

        [DreamProc("json_decode")]
        [DreamProcParameter("JSON", Type = DreamValueType.String)]
        public static DreamValue NativeProc_json_decode(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if (!arguments.GetArgument(0, "JSON").TryGetValueAsString(out var jsonString))
            {
                throw new Exception("Unknown value");
            }
            JsonElement jsonRoot = JsonSerializer.Deserialize<JsonElement>(jsonString);

            return CreateValueFromJsonElement(jsonRoot);
        }

        [DreamProc("json_encode")]
        [DreamProcParameter("Value")]
        public static DreamValue NativeProc_json_encode(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            object? jsonObject = CreateJsonElementFromValue(arguments.GetArgument(0, "Value"));
            string result = JsonSerializer.Serialize(jsonObject);

            return new DreamValue(result);
        }

        private static DreamValue _length(DreamValue value, bool countBytes)
        {
            if (value.TryGetValueAsString(out var str)) {
                return new DreamValue(countBytes ? str.Length : str.EnumerateRunes().Count());
            } else if (value.TryGetValueAsDreamList(out var list)) {
                return new DreamValue(list.GetLength());
            } else if (value.Type is DreamValueType.Float or DreamValueType.DreamObject or DreamValueType.DreamPath) {
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
        public static DreamValue NativeProc_list2params(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            if (!arguments.GetArgument(0, "List").TryGetValueAsDreamList(out DreamList list)) return new DreamValue(string.Empty);

            StringBuilder paramBuilder = new StringBuilder();

            List<DreamValue> values = list.GetValues();
            foreach (DreamValue entry in values) {
                if (list.ContainsKey(entry))
                {
                    paramBuilder.Append($"{HttpUtility.UrlEncode(entry.Value.ToString())}={HttpUtility.UrlEncode(list.GetValue(entry).Value.ToString())}");
                } else {
                    paramBuilder.Append(HttpUtility.UrlEncode(entry.Value.ToString()));
                }

                paramBuilder.Append('&');
            }

            //Remove trailing &
            if (paramBuilder.Length > 0) paramBuilder.Remove(paramBuilder.Length-1, 1);
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
            List<DreamValue> values;

            if (arguments.ArgumentCount == 1) {
                DreamValue arg = arguments.GetArgument(0, "A");
                if (!arg.TryGetValueAsDreamList(out var list))
                    return arg;

                values = list.GetValues();
            } else {
                values = arguments.GetAllArguments();
            }

            DreamValue max = values[0];

            for (int i = 1; i < values.Count; i++) {
                DreamValue value = values[i];

                if (value == DreamValue.Null) {
                    max = value;
                } else if (value.TryGetValueAsFloat(out var lFloat) && max.TryGetValueAsFloat(out float rFloat)) {
                    if (lFloat > rFloat) max = value;
                } else if (value.TryGetValueAsString(out var lString) && max.TryGetValueAsString(out var rString)) {
                    if (string.Compare(lString, rString, StringComparison.Ordinal) > 0) max = value;
                } else {
                    throw new Exception($"Cannot compare {max} and {value}");
                }
            }

            return max;
        }

        [DreamProc("md5")]
        [DreamProcParameter("T", Type = DreamValueType.String | DreamValueType.DreamResource)]
        public static DreamValue NativeProc_md5(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            if(arguments.ArgumentCount > 1) throw new Exception("md5() only takes one argument");
            DreamValue arg = arguments.GetArgument(0, "T");

            string? text;
            if (arg.TryGetValueAsDreamResource(out DreamResource resource)) {
                text = resource.ReadAsString();

                if (text == null)
                    return DreamValue.Null;
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
                DreamValue arg = arguments.GetArgument(0, "A");
                if (!arg.TryGetValueAsDreamList(out var list))
                    return arg;

                values = list.GetValues();
            } else {
                values = arguments.GetAllArguments();
            }

            DreamValue min = values[0];
            if (min == DreamValue.Null) return min;

            for (int i = 1; i < values.Count; i++) {
                DreamValue value = values[i];

                if (value.TryGetValueAsFloat(out var lFloat) && min.TryGetValueAsFloat(out var rFloat)) {
                    if (lFloat < rFloat) min = value;
                } else if (value.TryGetValueAsString(out var lString) && min.TryGetValueAsString(out var rString)) {
                    if (string.Compare(lString, rString, StringComparison.Ordinal) < 0) min = value;
                } else if (value == DreamValue.Null) {
                    return value;
                } else {
                    throw new Exception($"Cannot compare {min} and {value}");
                }
            }

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

            DreamList view = DreamList.Create();
            int centerX = center.GetVariable("x").GetValueAsInteger();
            int centerY = center.GetVariable("y").GetValueAsInteger();
            int centerZ = center.GetVariable("z").GetValueAsInteger();

            var mapMgr = IoCManager.Resolve<IDreamMapManager>();

            for (int x = Math.Max(centerX - distance, 1); x < Math.Min(centerX + distance, mapMgr.Size.X); x++) {
                for (int y = Math.Max(centerY - distance, 1); y < Math.Min(centerY + distance, mapMgr.Size.Y); y++) {
                    if (x == centerX && y == centerY)
                        continue;
                    if (!mapMgr.TryGetTurfAt((x, y), centerZ, out var turf))
                        continue;

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

            DreamList view = DreamList.Create();
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
            DreamList list = DreamList.Create();

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
                result = DreamList.Create();
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
            var newRegex = DreamManager.ObjectTree.CreateObject(DreamPath.Regex);
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
            if (!arguments.GetArgument(0, "Haystack").TryGetValueAsString(out var text))
            {
                return DreamValue.Null;
            }

            var arg3 = arguments.GetArgument(2, "Replacement").TryGetValueAsString(out var replacement);

            //TODO: Regex support
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

            return new DreamValue(text.Substring(start - 1, end - start).Replace(needle, replacement, StringComparison.OrdinalIgnoreCase));
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

            if (arguments.GetArgument(1, "space").TryGetValueAsInteger(out var space) && space != 0) {
                //TODO implement other colorspace support
                throw new NotImplementedException("rgb2num() currently only supports COLORSPACE_RGB");
            }

            if (!ColorHelpers.TryParseColor(color, out var c, defaultAlpha: null)) {
                throw new Exception("bad color");
            }

            DreamList list = DreamList.Create();

            list.AddValue(new DreamValue(c.RByte));
            list.AddValue(new DreamValue(c.GByte));
            list.AddValue(new DreamValue(c.BByte));

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
            string? text;

            if (arg.TryGetValueAsDreamResource(out DreamResource resource)) {
                text = resource.ReadAsString();

                if (text == null)
                    return DreamValue.Null;
            } else if (!arg.TryGetValueAsString(out text)) {
                return DreamValue.Null;
            }

            SHA1 sha1 = SHA1.Create();
            byte[] input = Encoding.UTF8.GetBytes(text);
            byte[] output = sha1.ComputeHash(input);
            //Match BYOND formatting
            string hash = BitConverter.ToString(output).Replace("-", "").ToLower();
            return new DreamValue(hash);
        }

        [DreamProc("shutdown")]
        [DreamProcParameter("Addr", Type = DreamValueType.String | DreamValueType.DreamObject)]
        [DreamProcParameter("Natural", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_shutdown(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            DreamValue addrValue = arguments.GetArgument(0, "Addr");
            if (addrValue == DreamValue.Null) {
                //DreamManager.Shutdown = true;
            }
            else {
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

        [DreamProc("sound")]
        [DreamProcParameter("file", Type = DreamValueType.DreamResource)]
        [DreamProcParameter("repeat", Type = DreamValueType.Float, DefaultValue = 0)]
        [DreamProcParameter("wait", Type = DreamValueType.Float)]
        [DreamProcParameter("channel", Type = DreamValueType.Float)]
        [DreamProcParameter("volume", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_sound(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamObject soundObject = DreamManager.ObjectTree.CreateObject(DreamPath.Sound);
            soundObject.InitSpawn(arguments);
            return new DreamValue(soundObject);
        }

        [DreamProc("splittext")]
        [DreamProcParameter("Text", Type = DreamValueType.String)]
        [DreamProcParameter("Delimiter", Type = DreamValueType.String)]
        public static DreamValue NativeProc_splittext(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            if (!arguments.GetArgument(0, "Text").TryGetValueAsString(out var text))
            {
                return new DreamValue(DreamList.Create());
            }
            var arg2 = arguments.GetArgument(1, "Delimiter");
            if(!arg2.TryGetValueAsString(out var delimiter))
            {
                if (!arg2.Equals(DreamValue.Null))
                {
                    return new DreamValue(DreamList.Create());
                }
            }
            string[] splitText = text.Split(delimiter);
            DreamList list = DreamList.Create(splitText);

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
            if(!arguments.GetArgument(0, "T").TryGetValueAsString(out var text))
            {
                return DreamValue.Null;
            }
            DreamPath path = new DreamPath(text);

            if (DreamManager.ObjectTree.HasTreeEntry(path)) {
                return new DreamValue(path);
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

            if (!arguments.GetArgument(0, "timestamp").TryGetValueAsInteger(out var timestamp)) {
                // TODO This copes with nulls and is a sane default, but BYOND has weird returns for strings and stuff
                DreamManager.WorldInstance.GetVariable("timeofday").TryGetValueAsInteger(out timestamp);
            }

            if (!arguments.GetArgument(1, "format").TryGetValueAsString(out var format)) {
                format = "DDD MMM DD hh:mm:ss YYYY";
            }

            long ticks = timestamp * (TimeSpan.TicksPerSecond / 10);

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

        [DreamProc("typesof")]
        [DreamProcParameter("Item1")]
        public static DreamValue NativeProc_typesof(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamList list = DreamList.Create();

            foreach (DreamValue type in arguments.GetAllArguments()) {
                DreamPath typePath = type.GetValueAsPath();

                if (typePath.LastElement == "proc") {
                    DreamPath objectTypePath = typePath.AddToPath("..");
                    DreamObjectDefinition objectDefinition = DreamManager.ObjectTree.GetObjectDefinition(objectTypePath);

                    foreach (KeyValuePair<string, int> proc in objectDefinition.Procs) {
                        list.AddValue(new DreamValue(proc.Key));
                    }
                } else {
                    var descendants = DreamManager.ObjectTree.GetAllDescendants(typePath);

                    foreach (var descendant in descendants) {
                        list.AddValue(new DreamValue(descendant.ObjectDefinition.Type));
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

            DreamList view = DreamList.Create();
            int centerX = center.GetVariable("x").GetValueAsInteger();
            int centerY = center.GetVariable("y").GetValueAsInteger();
            int centerZ = center.GetVariable("z").GetValueAsInteger();

            var mapMgr = IoCManager.Resolve<IDreamMapManager>();

            for (int x = Math.Max(centerX - distance, 1); x < Math.Min(centerX + distance, mapMgr.Size.X); x++) {
                for (int y = Math.Max(centerY - distance, 1); y < Math.Min(centerY + distance, mapMgr.Size.Y); y++) {
                    if (!mapMgr.TryGetTurfAt((x, y), centerZ, out var turf))
                        continue;

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

            DreamList view = DreamList.Create();
            int depth = (depthValue.Type == DreamValueType.Float) ? depthValue.GetValueAsInteger() : 5; //TODO: Default to world.view
            int centerX = center.GetVariable("x").GetValueAsInteger();
            int centerY = center.GetVariable("y").GetValueAsInteger();

            foreach (DreamObject mob in DreamManager.Mobs) {
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

        [DreamProc("winexists")]
        [DreamProcParameter("player", Type = DreamValueType.DreamObject)]
        [DreamProcParameter("control_id", Type = DreamValueType.String)]
        public static async Task<DreamValue> NativeProc_winexists(AsyncNativeProc.State state) {
            DreamValue player = state.Arguments.GetArgument(0, "player");
            if (!state.Arguments.GetArgument(1, "control_id").TryGetValueAsString(out string controlId)) {
                return new DreamValue("");
            }

            DreamConnection connection;
            if (player.TryGetValueAsDreamObjectOfType(DreamPath.Mob, out DreamObject mob)) {
                connection = DreamManager.GetConnectionFromMob(mob);
            } else if (player.TryGetValueAsDreamObjectOfType(DreamPath.Client, out DreamObject client)) {
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

            if (player.TryGetValueAsDreamObjectOfType(DreamPath.Mob, out var mob)) {
                connection = DreamManager.GetConnectionFromMob(mob);
            } else if (player.TryGetValueAsDreamObjectOfType(DreamPath.Client, out var client)) {
                connection = DreamManager.GetConnectionFromClient(client);
            } else {
                throw new ArgumentException($"Invalid \"player\" argument {player}");
            }

            connection.WinSet(winsetControlId, winsetParams);
            return DreamValue.Null;
        }
    }
}
