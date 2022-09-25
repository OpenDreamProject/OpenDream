using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using OpenDreamRuntime.Objects;

namespace OpenDreamRuntime.Procs.Native
{
    internal static class DreamProcNativeWorld
    {
        [DreamProc("Export")]
        [DreamProcParameter("Addr", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("File", Type = DreamValue.DreamValueType.DreamObject, DefaultValue = null)]
        [DreamProcParameter("Persist", Type = DreamValue.DreamValueType.Float, DefaultValue = 0)]
        [DreamProcParameter("Clients", Type = DreamValue.DreamValueType.DreamObject, DefaultValue = null)]
        public static async Task<DreamValue> NativeProc_Export(AsyncNativeProc.State state)
        {
            var addr = state.Arguments.GetArgument(0, "Addr").Stringify();

            if (!Uri.TryCreate(addr, UriKind.RelativeOrAbsolute, out var uri))
                throw new ArgumentException("Unable to parse URI.");

            if (uri.Scheme is not ("http" or "https"))
                throw new NotSupportedException("non-HTTP world.Export is not supported.");

            // TODO: Maybe cache HttpClient.
            var client = new HttpClient();
            var response = await client.GetAsync(uri);

            var list = DreamList.Create();
            foreach (var header in response.Headers)
            {
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
            var config_set = arguments.GetArgument(0, "config_set").Stringify();
            var param = arguments.GetArgument(1, "param").Stringify();

            switch (config_set) {
                case "env":
                    if (Environment.GetEnvironmentVariable(param) is string strValue) {
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
        [DreamProcParameter("value", Type = DreamValue.DreamValueType.String, DefaultValue = null)]
        public static DreamValue NativeProc_SetConfig(DreamObject src, DreamObject usr, DreamProcArguments arguments)
        {
            var config_set = arguments.GetArgument(0, "config_set").Stringify();
            var param = arguments.GetArgument(1, "param").Stringify();
            var value = arguments.GetArgument(2, "value");

            switch (config_set) {
                case "env":
                    if (value == DreamValue.Null) {
                        Environment.SetEnvironmentVariable(param, null);
                    } else {
                        Environment.SetEnvironmentVariable(param, value.Stringify());
                    }
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
