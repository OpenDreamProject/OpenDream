using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using OpenDreamRuntime.Objects.Types;

namespace OpenDreamRuntime.Procs.Native {
    internal static class DreamProcNativeWorld {
        [DreamProc("Export")]
        [DreamProcParameter("Addr", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("File", Type = DreamValue.DreamValueType.DreamObject)]
        [DreamProcParameter("Persist", Type = DreamValue.DreamValueType.Float, DefaultValue = 0)]
        [DreamProcParameter("Clients", Type = DreamValue.DreamValueType.DreamObject)]
        public static async Task<DreamValue> NativeProc_Export(AsyncNativeProc.State state) {
            var addr = state.GetArgument(0, "Addr").Stringify();

            if (!Uri.TryCreate(addr, UriKind.RelativeOrAbsolute, out var uri))
                throw new ArgumentException("Unable to parse URI.");

            if (uri.Scheme is not ("http" or "https"))
                throw new NotSupportedException("non-HTTP world.Export is not supported.");

            // TODO: Maybe cache HttpClient.
            var client = new HttpClient();
            var response = await client.GetAsync(uri);

            var list = state.ObjectTree.CreateList();
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
        public static DreamValue NativeProc_GetConfig(NativeProc.State state) {
            state.GetArgument(0, "config_set").TryGetValueAsString(out string config_set);
            var param = state.GetArgument(1, "param");

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

        [DreamProc("Profile")]
        [DreamProcParameter("command", Type = DreamValue.DreamValueType.Float)]
        [DreamProcParameter("type", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("format", Type = DreamValue.DreamValueType.String)]
        public static DreamValue NativeProc_Profile(NativeProc.State state) {
            state.GetArgument(0, "command").TryGetValueAsInteger(out var command);

            string? type, format;
            switch (state.Arguments.Count) {
                case 3:
                    state.GetArgument(1, "type").TryGetValueAsString(out type);
                    state.GetArgument(2, "format").TryGetValueAsString(out format);
                    break;
                case 2:
                    type = null;
                    state.GetArgument(1, "type").TryGetValueAsString(out format);
                    break;
                default:
                    type = null;
                    format = null;
                    break;
            }

            // TODO: Actually return profiling data

            if (format == "json") {
                return new("[]");
            } else { // Anything else gives a /list
                DreamList dataList = state.ObjectTree.CreateList();

                if (type == "sendmaps") {
                    dataList.AddValue(new("name"));
                    dataList.AddValue(new("value"));
                    dataList.AddValue(new("calls"));
                } else { // Anything else is a proc profile
                    dataList.AddValue(new("name"));
                    dataList.AddValue(new("self"));
                    dataList.AddValue(new("total"));
                    dataList.AddValue(new("real"));
                    dataList.AddValue(new("over"));
                    dataList.AddValue(new("calls"));
                }

                return new(dataList);
            }
        }

        [DreamProc("SetConfig")]
        [DreamProcParameter("config_set", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("param", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("value", Type = DreamValue.DreamValueType.String)]
        public static DreamValue NativeProc_SetConfig(NativeProc.State state) {
            state.GetArgument(0, "config_set").TryGetValueAsString(out string config_set);
            state.GetArgument(1, "param").TryGetValueAsString(out string param);
            var value = state.GetArgument(2, "value");

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
