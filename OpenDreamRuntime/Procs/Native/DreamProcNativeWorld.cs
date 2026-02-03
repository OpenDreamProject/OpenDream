using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Byond.TopicSender;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using Robust.Server;

namespace OpenDreamRuntime.Procs.Native;

internal static class DreamProcNativeWorld {
    [DreamProc("Export")]
    [DreamProcParameter("Addr", Type = DreamValue.DreamValueTypeFlag.String)]
    [DreamProcParameter("File", Type = DreamValue.DreamValueTypeFlag.DreamObject)]
    [DreamProcParameter("Persist", Type = DreamValue.DreamValueTypeFlag.Float, DefaultValue = 0)]
    [DreamProcParameter("Clients", Type = DreamValue.DreamValueTypeFlag.DreamObject)]
    public static async Task<DreamValue> NativeProc_Export(AsyncNativeProc.AsyncNativeProcState state) {
        var addr = state.GetArgument(0, "Addr").Stringify();

        if (!Uri.TryCreate(addr, UriKind.RelativeOrAbsolute, out var uri))
            throw new ArgumentException("Unable to parse URI.");

        if (uri.Scheme is not ("http" or "https" or "byond"))
            throw new NotSupportedException($"Unknown scheme for world.Export: '{uri.Scheme}'");

        if (uri.Scheme is "byond") {
            var tenSecondTimeout = TimeSpan.FromSeconds(10);
            var topicClient = new TopicClient(new SocketParameters {
                ConnectTimeout = tenSecondTimeout,
                DisconnectTimeout = tenSecondTimeout,
                ReceiveTimeout = tenSecondTimeout,
                SendTimeout = tenSecondTimeout,
            });

            var topicResponse = await topicClient.SendTopic(uri.Host, uri.Query[1..], Convert.ToUInt16(uri.Port));
            switch (topicResponse.ResponseType) {
                case TopicResponseType.FloatResponse:
                    return new DreamValue(topicResponse.FloatData!.Value);

                case TopicResponseType.StringResponse:
                    return new DreamValue(topicResponse.StringData!);

                case TopicResponseType.UnknownResponse:
                    var byteList = state.ObjectTree.CreateList();
                    foreach (var @byte in topicResponse.RawData)
                        byteList.AddValue(new DreamValue(@byte));
                    return new DreamValue(byteList);

                default:
                    throw new IOException($"Topic returned an unknown response type: '{topicResponse.ResponseType}'");
            }
        }

        // TODO: Definitely cache HttpClient.
        using var client = new HttpClient();
        using var response = await client.GetAsync(uri);
        var contentBytes = await response.Content.ReadAsByteArrayAsync();

        var list = state.ObjectTree.CreateList();
        foreach (var header in response.Headers) {
            // TODO: How to handle headers with multiple values?
            list.SetValue(new DreamValue(header.Key), new DreamValue(header.Value.First()));
        }

        var content = state.ResourceManager.CreateResource(contentBytes);
        list.SetValue(new DreamValue("STATUS"), new DreamValue(((int) response.StatusCode).ToString()));
        list.SetValue(new DreamValue("CONTENT"), new DreamValue(content));

        return new DreamValue(list);
    }

    [DreamProc("Error")]
    [DreamProcParameter("exception", Type = DreamValue.DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_Error(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var exceptionArg = bundle.GetArgument(0, "exception");
        if (!exceptionArg.TryGetValueAsDreamObject<DreamObjectException>(out var exception)) // Ignore anything not an /exception
            return DreamValue.Null;
        if (!exception.Desc.TryGetValueAsString(out var exceptionDesc))
            return DreamValue.Null;

        bundle.DreamManager.WriteWorldLog(exceptionDesc, LogLevel.Error);
        return DreamValue.Null;
    }

    [DreamProc("GetConfig")]
    [DreamProcParameter("config_set", Type = DreamValue.DreamValueTypeFlag.String)]
    [DreamProcParameter("param", Type = DreamValue.DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_GetConfig(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        bundle.GetArgument(0, "config_set").TryGetValueAsString(out var configSetArg);
        var param = bundle.GetArgument(1, "param");

        ProcessConfigSet(configSetArg, out _, out var configSet);

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
        bundle.GetArgument(0, "config_set").TryGetValueAsString(out var configSetArg);
        bundle.GetArgument(1, "param").TryGetValueAsString(out var param);
        var value = bundle.GetArgument(2, "value");

        ProcessConfigSet(configSetArg, out _, out var configSet);

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

    [DreamProc("ODHotReloadInterface")]
    public static DreamValue NativeProc_ODHotReloadInterface(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var dreamManager = IoCManager.Resolve<DreamManager>();
        dreamManager.HotReloadInterface();
        return DreamValue.Null;
    }

    [DreamProc("ODHotReloadResource")]
    [DreamProcParameter("file_name", Type = DreamValue.DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_ODHotReloadResource(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        if(!bundle.GetArgument(0, "file_name").TryGetValueAsString(out var fileName))
            throw new ArgumentException("file_name must be a string");
        var dreamManager = IoCManager.Resolve<DreamManager>();
        dreamManager.HotReloadResource(fileName);
        return DreamValue.Null;
    }

    /// <summary>
    /// Determines the specified configuration space and configuration set in a config_set argument
    /// </summary>
    private static void ProcessConfigSet(string value, out string? configSpace, out string configSet) {
        int slash = value.IndexOf('/');

        // No specified config space, default to USER
        // TODO: Supposedly defaults to HOME in safe mode
        if (slash == -1) {
            configSpace = "USER";
            configSet = value;
            return;
        }

        configSpace = value.Substring(0, slash).ToUpperInvariant();
        configSet = value.Substring(slash + 1);
        switch (configSpace) {
            case "SYSTEM":
            case "USER":
            case "HOME":
            case "APP":
                return;
            default:
                throw new ArgumentException($"There is no \"{configSpace}\" configuration space");
        }
    }
}
