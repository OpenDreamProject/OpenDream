using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using Robust.Server;

namespace OpenDreamRuntime.Procs.Native {
    internal static class DreamProcNativeWorld {
        [DreamProc("Export")]
        [DreamProcParameter("Addr", Type = DreamValue.DreamValueTypeFlag.String)]
        [DreamProcParameter("File", Type = DreamValue.DreamValueTypeFlag.DreamObject)]
        [DreamProcParameter("Persist", Type = DreamValue.DreamValueTypeFlag.Float, DefaultValue = 0)]
        [DreamProcParameter("Clients", Type = DreamValue.DreamValueTypeFlag.DreamObject)]
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
        [DreamProcParameter("config_set", Type = DreamValue.DreamValueTypeFlag.String)]
        [DreamProcParameter("param", Type = DreamValue.DreamValueTypeFlag.String)]
        public static DreamValue NativeProc_GetConfig(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            bundle.GetArgument(0, "config_set").TryGetValueAsString(out var configSet);
            var param = bundle.GetArgument(1, "param");

            switch (configSet) {
                case "env":
                    if (param.IsNull) {
                        // DM ref says: "If no parameter is specified, a list of the names of all available parameters is returned."
                        // but apparently it's actually just null for "env".
                        return DreamValue.Null;
                    } else if (param.TryGetValueAsString(out var paramString) && Environment.GetEnvironmentVariable(paramString) is string strValue) {
                        return new DreamValue(strValue);
                    } else {
                        return DreamValue.Null;
                    }
                case "ban":
                case "keyban":
                case "ipban":
                case "admin":
                    Logger.GetSawmill("opendream.world").Warning("Unsupported GetConfig config_set: " + configSet);
                    return new(bundle.ObjectTree.CreateList());
                default:
                    throw new ArgumentException("Incorrect GetConfig config_set: " + configSet);
            }
        }

        [DreamProc("Profile")]
        [DreamProcParameter("command", Type = DreamValue.DreamValueTypeFlag.Float)]
        [DreamProcParameter("type", Type = DreamValue.DreamValueTypeFlag.String)]
        [DreamProcParameter("format", Type = DreamValue.DreamValueTypeFlag.String)]
        public static DreamValue NativeProc_Profile(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            bundle.GetArgument(0, "command").TryGetValueAsInteger(out var command);

            string? type, format;
            switch (bundle.Arguments.Length) {
                case 3:
                    bundle.GetArgument(1, "type").TryGetValueAsString(out type);
                    bundle.GetArgument(2, "format").TryGetValueAsString(out format);
                    break;
                case 2:
                    type = null;
                    bundle.GetArgument(1, "type").TryGetValueAsString(out format);
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
                DreamList dataList = bundle.ObjectTree.CreateList();

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

        [DreamProc("Reboot")]
        [DreamProcParameter("reason", Type = DreamValue.DreamValueTypeFlag.Float)]
        public static DreamValue NativeProc_Reboot(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            var server = IoCManager.Resolve<IBaseServer>();

            server.Shutdown("/world.Reboot() was called but restarting is very broken");
            return DreamValue.Null;
        }

        [DreamProc("SetConfig")]
        [DreamProcParameter("config_set", Type = DreamValue.DreamValueTypeFlag.String)]
        [DreamProcParameter("param", Type = DreamValue.DreamValueTypeFlag.String)]
        [DreamProcParameter("value", Type = DreamValue.DreamValueTypeFlag.String)]
        public static DreamValue NativeProc_SetConfig(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            bundle.GetArgument(0, "config_set").TryGetValueAsString(out var configSet);
            bundle.GetArgument(1, "param").TryGetValueAsString(out var param);
            var value = bundle.GetArgument(2, "value");

            switch (configSet) {
                case "env":
                    value.TryGetValueAsString(out var valueString);
                    Environment.SetEnvironmentVariable(param, valueString);
                    break;
                case "ban":
                case "keyban":
                case "ipban":
                case "admin":
                    Logger.GetSawmill("opendream.world").Warning("Unsupported SetConfig config_set: " + configSet);
                    break;
                default:
                    throw new ArgumentException("Incorrect SetConfig config_set: " + configSet);
            }

            return DreamValue.Null;
        }
    }
}
