using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using Content.Shared.Interface;
using Content.Shared.Network.Messages;
using Robust.Client.CEF;
using Robust.Client.UserInterface;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client.Interface.Controls
{
    class ControlBrowser : InterfaceControl
    {
        private static readonly Dictionary<string, string> FileExtensionMimeTypes = new Dictionary<string, string>
        {
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

        private ISawmill _sawmill = Logger.GetSawmill("opendream.browser");

        private BrowserControl _webView;

        public ControlBrowser(ControlDescriptor controlDescriptor, ControlWindow window)
            : base(controlDescriptor, window)
        {
            IoCManager.InjectDependencies(this);
        }

        protected override Control CreateUIElement() {
            _webView = new BrowserControl();

            _webView.AddResourceRequestHandler(RequestHandler);
            _webView.AddBeforeBrowseHandler(BeforeBrowseHandler);

            return _webView;
        }

        public override void Output(string value, string jsFunction) {
            if (jsFunction == null) return;

            value = HttpUtility.UrlDecode(value);
            value = value.Replace("\"", "\\\"");
            // todo:
            //_webView.CoreWebView2.ExecuteScriptAsync(jsFunction + "(\"" + value + "\")");
        }

        public void SetFileSource(ResourcePath filepath, bool userData) {
            _webView.Url = (userData ? "usr://" : "res://") + filepath;
        }

        private void BeforeBrowseHandler(BeforeBrowseContext context)
        {
            if (string.IsNullOrEmpty(_webView.Url))
                return;

            Uri oldUri = new Uri(_webView.Url);
            Uri newUri = new Uri(context.Url);

            if (newUri.Scheme == "byond" || (newUri.AbsolutePath == oldUri.AbsolutePath && newUri.Query != String.Empty)) {
                context.DoCancel();

                var msg = _netManager.CreateNetMessage<MsgTopic>();
                msg.Query = newUri.Query;
                _netManager.ClientSendMessage(msg);
            }
        }

        private void RequestHandler(RequestHandlerContext context)
        {
            Uri newUri = new Uri(context.Url);

            if (newUri.Scheme == "usr")
            {
                Stream stream;
                HttpStatusCode status;
                var path = new ResourcePath(newUri.AbsolutePath);
                try
                {
                    stream = _resourceManager.UserData.OpenRead(path);
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

            var cef = IoCManager.Resolve<CefManager>();
            cef.CreateBrowserWindow(parameters);
        }
    }
}
