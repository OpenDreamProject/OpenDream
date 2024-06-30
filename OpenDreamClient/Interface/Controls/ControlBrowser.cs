using System.Diagnostics;
using System.IO;
using System.Net;
using System.Web;
using OpenDreamClient.Interface.Descriptors;
using OpenDreamClient.Resources;
using OpenDreamShared.Network.Messages;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.WebView;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlBrowser : InterfaceControl {
    private static readonly Dictionary<string, string> FileExtensionMimeTypes = new() {
        { "css", "text/css" },
        { "html", "text/html" },
        { "htm", "text/html" },
        { "png", "image/png" },
        { "svg", "image/svg+xml" },
        { "jpeg", "image/jpeg" },
        { "jpg", "image/jpeg" },
        { "js", "application/javascript" },
        { "json", "application/json" },
        { "ttf", "font/ttf" },
        { "txt", "text/plain" }
    };

    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IDreamResourceManager _dreamResource = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("opendream.browser");

    private PanelContainer _panel;
    private WebViewControl _webView;

    public ControlBrowser(ControlDescriptor controlDescriptor, ControlWindow window)
        : base(controlDescriptor, window) {
        IoCManager.InjectDependencies(this);
    }

    protected override Control CreateUIElement() {
        _panel = new PanelContainer {
            Children = {
                (_webView = new WebViewControl())
            }
        };

        _webView.AddResourceRequestHandler(RequestHandler);
        _webView.AddBeforeBrowseHandler(BeforeBrowseHandler);
        _webView.OnVisibilityChanged += (args) => {
            if (args.Visible) {
                OnShowEvent();
            } else {
                OnHideEvent();
            }
        };

        if(ControlDescriptor.IsVisible.Value)
            OnShowEvent();
        else
            OnHideEvent();

        return _panel;
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();

        _panel.PanelOverride = new StyleBoxFlat(Color.White); // Always white background
    }

    public override void Output(string value, string? jsFunction) {
        if (jsFunction == null) return;

        // Prepare the argument to be used in JS
        //output is formatted by list2params sometimes, which means raw strings are url encoded, but the message contains & chars which are not encoded that are the params
        //so we split on &, url decode the parts, and then join them back together with , as the separator for the JS params
        var parts = value.Split('&');
        for (var i = 0; i < parts.Length; i++) {
            parts[i] = "\""+HttpUtility.JavaScriptStringEncode(HttpUtility.UrlDecode(parts[i]))+"\""; //wrap in quotes and encode for JS
        }

        // Insert the values directly into JS and execute it (what could go wrong??)
        _webView.ExecuteJavaScript($"{jsFunction}({string.Join(",", parts)})");
    }

    public void SetFileSource(ResPath filepath) {
        _webView.Url = "http://127.0.0.1/" + filepath; // hostname must be the localhost IP for TGUI to work properly
    }

    private void BeforeBrowseHandler(IBeforeBrowseContext context) {
        // An exception in here will freeze up / crash CEF, so catch any
        try {
            if (string.IsNullOrEmpty(_webView.Url))
                return;

            Uri oldUri = new Uri(_webView.Url);
            Uri newUri = new Uri(context.Url);

            if (newUri.Scheme == "byond" || (newUri.AbsolutePath == oldUri.AbsolutePath && newUri.Query != string.Empty)) {
                context.DoCancel();

                switch (newUri.Host) {
                    case "winset":
                        HandleEmbeddedWinset(newUri.Query);
                        return;
                    case "winget":
                        HandleEmbeddedWinget(newUri.Query);
                        return;
                    default: {
                        var msg = new MsgTopic { Query = newUri.Query };
                        _netManager.ClientSendMessage(msg);
                        break;
                    }
                }
            }
        } catch (Exception e) {
            _sawmill.Error($"Exception in BeforeBrowseHandler: {e}");
        }
    }

    private void RequestHandler(IRequestHandlerContext context) {
        Uri newUri = new Uri(context.Url);

        if (newUri is { Scheme: "http", Host: "127.0.0.1" }) {
            Stream stream;
            HttpStatusCode status;
            var path = new ResPath(newUri.AbsolutePath);
            if(!_dreamResource.EnsureCacheFile(newUri.AbsolutePath)) {
                stream = Stream.Null;
                status = HttpStatusCode.NotFound;
            } else {
                try {
                    stream = _resourceManager.UserData.OpenRead(_dreamResource.GetCacheFilePath(newUri.AbsolutePath));
                    status = HttpStatusCode.OK;
                } catch (FileNotFoundException) {
                    stream = Stream.Null;
                    status = HttpStatusCode.NotFound;
                } catch (Exception e) {
                    _sawmill.Error($"Exception while loading file from {newUri}:\n{e}");
                    stream = Stream.Null;
                    status = HttpStatusCode.InternalServerError;
                }
            }

            if (!FileExtensionMimeTypes.TryGetValue(path.Extension, out var mimeType))
                mimeType = "application/octet-stream";

            context.DoRespondStream(stream, mimeType, status);
        }
    }

    /// <summary>
    /// Handles an embedded winset
    /// <code>byond://winset?command=.quit</code>
    /// </summary>
    /// <param name="query">The query portion of the embedded winset</param>
    private void HandleEmbeddedWinset(string query) {
        // Strip the question mark out before parsing
        var queryParams = HttpUtility.ParseQueryString(query.Substring(1));

        // We need to extract the control element (if one was included)
        string? element = queryParams.Get("element");
        queryParams.Remove("element");

        // Wrap each parameter in quotes so the entire value is used
        foreach (var paramKey in queryParams.AllKeys) {
            var paramValue = queryParams[paramKey];
            if (paramValue == null)
                continue;

            queryParams.Set(paramKey, $"\"{paramValue}\"");
        }

        // Reassemble the query params without element then convert to winset syntax
        var modifiedQuery = queryParams.ToString();
        modifiedQuery = HttpUtility.UrlDecode(modifiedQuery);
        modifiedQuery = modifiedQuery!.Replace('&', ';'); // TODO: More robust parsing

        // We can finally call winset
        _interfaceManager.WinSet(element, modifiedQuery);
    }

    /// <summary>
    /// Handles an embedded winget
    /// </summary>
    /// <param name="query">The query portion of the embedded winget</param>
    // Example: byond://winget?id=browseroutput&property=size&callback=JSFunction
    // (Not in the XML comment because '&' breaks that apparently)
    private void HandleEmbeddedWinget(string query) {
        // Run this later to ensure any pending UI measurements have occured
        IoCManager.Resolve<ITimerManager>().AddTimer(new Timer(200, false, () => {
            // Strip the question mark out before parsing
            var queryParams = HttpUtility.ParseQueryString(query.Substring(1));

            var elementId = queryParams.Get("id");
            var property = queryParams.Get("property");
            var callback = queryParams.Get("callback");
            if (elementId == null || property == null || callback == null) {
                _sawmill.Error($"Required arg 'id', 'property', or 'callback' not provided in embedded winget ({query})");
                return;
            }

            // TG uses property=* but really just wants size
            // TODO: Actual winget * support
            bool forceJson = true;
            if (property == "*") {
                property = "size";
                forceJson = false; // property=* does not return "as json" values (why?!)
            }

            var result = _interfaceManager.WinGet(elementId, property, forceJson: forceJson);

            // Execute the callback
            var propertyEncoded = HttpUtility.JavaScriptStringEncode(property);
            var resultEncoded = HttpUtility.JavaScriptStringEncode(result);
            var jsonArgument = $"{{ \"{propertyEncoded}\": \"{resultEncoded}\" }}";
            _webView.ExecuteJavaScript($"{callback}({jsonArgument})");
        }));
    }

    private void OnShowEvent() {
        ControlDescriptorBrowser controlDescriptor = (ControlDescriptorBrowser)ControlDescriptor;
        if (!string.IsNullOrWhiteSpace(controlDescriptor.OnShowCommand.Value)) {
            _interfaceManager.RunCommand(controlDescriptor.OnShowCommand.AsRaw());
        }
    }

    private void OnHideEvent() {
        ControlDescriptorBrowser controlDescriptor = (ControlDescriptorBrowser)ControlDescriptor;
        if (!string.IsNullOrWhiteSpace(controlDescriptor.OnHideCommand.Value)) {
            _interfaceManager.RunCommand(controlDescriptor.OnHideCommand.AsRaw());
        }
    }
}

public sealed class BrowseWinCommand : IConsoleCommand {
    public string Command => "browsewin";
    public string Description => "";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        if (args.Length != 1) {
            shell.WriteError("Incorrect amount of arguments! Must be a single one.");
            return;
        }

        var parameters = new BrowserWindowCreateParameters(1280, 720) {Url = args[0]};

        var cef = IoCManager.Resolve<IWebViewManager>();
        cef.CreateBrowserWindow(parameters);
    }
}
