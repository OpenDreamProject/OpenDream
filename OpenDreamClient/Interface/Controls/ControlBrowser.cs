using System.IO;
using System.Net;
using System.Web;
using OpenDreamClient.Resources;
using OpenDreamShared.Interface;
using OpenDreamShared.Network.Messages;
using Robust.Client.UserInterface;
using Robust.Client.WebView;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace OpenDreamClient.Interface.Controls
{
    sealed class ControlBrowser : InterfaceControl
    {
        private static readonly Dictionary<string, string> FileExtensionMimeTypes = new Dictionary<string, string>
        {
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

        private ISawmill _sawmill = Logger.GetSawmill("opendream.browser");

        private WebViewControl _webView;

        public ControlBrowser(ControlDescriptor controlDescriptor, ControlWindow window)
            : base(controlDescriptor, window)
        {
            IoCManager.InjectDependencies(this);
        }

        protected override Control CreateUIElement() {
            _webView = new WebViewControl();

            _webView.AddResourceRequestHandler(RequestHandler);
            _webView.AddBeforeBrowseHandler(BeforeBrowseHandler);

            return _webView;
        }

        public override void Output(string value, string jsFunction) {
            if (jsFunction == null) return;

            value = HttpUtility.UrlDecode(value);
            value = value.Replace("\"", "\\\"");
            _webView.ExecuteJavaScript(jsFunction + "(\"" + value + "\")");
        }

        public void SetFileSource(ResourcePath filepath, bool userData) {
            _webView.Url = (userData ? "usr://_/" : "res://_/") + filepath;
        }

        private void BeforeBrowseHandler(IBeforeBrowseContext context)
        {
            if (string.IsNullOrEmpty(_webView.Url))
                return;

            Uri oldUri = new Uri(_webView.Url);
            Uri newUri = new Uri(context.Url);

            if (newUri.Scheme == "byond" || (newUri.AbsolutePath == oldUri.AbsolutePath && newUri.Query != String.Empty)) {
                context.DoCancel();

                var msg = new MsgTopic() { Query = newUri.Query };
                _netManager.ClientSendMessage(msg);
            }
        }

        private void RequestHandler(IRequestHandlerContext context)
        {
            Uri newUri = new Uri(context.Url);

            if (newUri.Scheme == "usr")
            {
                Stream stream;
                HttpStatusCode status;
                var path = new ResourcePath(newUri.AbsolutePath);
                try
                {
                    stream = _resourceManager.UserData.OpenRead(_dreamResource.GetCacheFilePath(newUri.AbsolutePath));
                    status = HttpStatusCode.OK;
                }
                catch (FileNotFoundException)
                {
                    stream = Stream.Null;
                    status = HttpStatusCode.NotFound;
                }
                catch (Exception e)
                {
                    _sawmill.Error($"Exception while loading file from usr://:\n{e}");
                    stream = Stream.Null;
                    status = HttpStatusCode.InternalServerError;
                }

                if (!FileExtensionMimeTypes.TryGetValue(path.Extension, out var mimeType))
                    mimeType = "application/octet-stream";

                context.DoRespondStream(stream, mimeType, status);
                return;
            }
        }
    }

    public sealed class BrowseWinCommand : IConsoleCommand
    {
        public string Command => "browsewin";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError("Incorrect amount of arguments! Must be a single one.");
                return;
            }

            var parameters = new BrowserWindowCreateParameters(1280, 720)
            {
                Url = args[0]
            };

            var cef = IoCManager.Resolve<IWebViewManager>();
            cef.CreateBrowserWindow(parameters);
        }
    }
}
