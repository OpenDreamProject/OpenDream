using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using OpenDreamRuntime.Objects;

namespace OpenDreamRuntime.Procs.Native {
    internal static class DreamProcNativeWorld {
        [DreamProc("Export")]
        [DreamProcParameter("Addr", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("File", Type = DreamValue.DreamValueType.DreamObject)]
        [DreamProcParameter("Persist", Type = DreamValue.DreamValueType.Float, DefaultValue = 0)]
        [DreamProcParameter("Clients", Type = DreamValue.DreamValueType.DreamObject)]
        public static async Task<DreamValue> NativeProc_Export(AsyncNativeProc.State state) {
            var addr = state.Arguments.GetArgument(0, "Addr").Stringify();

            if (!Uri.TryCreate(addr, UriKind.RelativeOrAbsolute, out var uri))
                throw new ArgumentException("Unable to parse URI.");

            if (uri.Scheme is not ("http" or "https"))
                throw new NotSupportedException("non-HTTP world.Export is not supported.");

            // TODO: Maybe cache HttpClient.
            var client = new HttpClient();
            var response = await client.GetAsync(uri);

            var list = DreamProcNativeRoot.ObjectTree.CreateList();
            foreach (var header in response.Headers) {
                // TODO: How to handle headers with multiple values?
                list.SetValue(new DreamValue(header.Key), new DreamValue(header.Value.First()));
            }

            list.SetValue(new DreamValue("STATUS"), new DreamValue(((int) response.StatusCode).ToString()));
            list.SetValue(new DreamValue("CONTENT"), new DreamValue(await response.Content.ReadAsStringAsync()));

            return new DreamValue(list);
        }

        [DreamProc("GetConfig")]
        [DreamProcParameter("config_set", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("param", Type = DreamValue.DreamValueType.String)]
        public static DreamValue NativeProc_GetConfig(DreamObject src, DreamObject usr, DreamProcArguments arguments)
        {
            arguments.GetArgument(0, "config_set").TryGetValueAsString(out string config_set);
            var param = arguments.GetArgument(1, "param");

            switch (config_set) {
                case "env":
                    if (param == DreamValue.Null) {
                        // DM ref says: "If no parameter is specified, a list of the names of all available parameters is returned."
                        // but apparently it's actually just null for "env".
                        return DreamValue.Null;
                    } else if (param.TryGetValueAsString(out string paramString) && Environment.GetEnvironmentVariable(paramString) is string strValue) {
                        return new DreamValue(strValue);
                    } else {
                        return DreamValue.Null;
                    }
                case "admin":
                    throw new NotSupportedException("Unsupported GetConfig config_set: " + config_set);
                case "ban":
                case "keyban":
                case "ipban":
                    throw new NotSupportedException("Unsupported GetConfig config_set: " + config_set);
                default:
                    throw new ArgumentException("Incorrect GetConfig config_set: " + config_set);
            }
        }

        [DreamProc("SetConfig")]
        [DreamProcParameter("config_set", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("param", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("value", Type = DreamValue.DreamValueType.String)]
        public static DreamValue NativeProc_SetConfig(DreamObject src, DreamObject usr, DreamProcArguments arguments)
        {
            arguments.GetArgument(0, "config_set").TryGetValueAsString(out string config_set);
            arguments.GetArgument(1, "param").TryGetValueAsString(out string param);
            var value = arguments.GetArgument(2, "value");

            switch (config_set) {
                case "env":
                    value.TryGetValueAsString(out string valueString);
                    Environment.SetEnvironmentVariable(param, valueString);
                    return DreamValue.Null;
                case "admin":
                    throw new NotSupportedException("Unsupported SetConfig config_set: " + config_set);
                case "ban":
                case "keyban":
                case "ipban":
                    throw new NotSupportedException("Unsupported SetConfig config_set: " + config_set);
                default:
                    throw new ArgumentException("Incorrect SetConfig config_set: " + config_set);
            }
        }
    }
}
