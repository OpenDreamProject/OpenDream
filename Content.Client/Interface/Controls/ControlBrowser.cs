using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using Content.Shared.Interface;
using Robust.Client.CEF;
using Robust.Client.UserInterface;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
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
        private BrowserControl _webView;

        public ControlBrowser(ControlDescriptor controlDescriptor, ControlWindow window)
            : base(controlDescriptor, window)
        {
            IoCManager.InjectDependencies(this);
        }

        protected override Control CreateUIElement() {
            _webView = new BrowserControl();

            _webView.AddResourceRequestHandler(RequestHandler);

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

        private void RequestHandler(RequestHandlerContext obj)
        {
            Uri oldUri = new Uri(_webView.Url);
            Uri newUri = new Uri(obj.Url);

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

                if (!FileExtensionMimeTypes.TryGetValue(path.Extension, out var mimeType))
                    mimeType = "application/octet-stream";

                obj.DoRespondStream(stream, mimeType, status);
                return;
            }

            if (newUri.Scheme == "byond" || (newUri.AbsolutePath == oldUri.AbsolutePath && newUri.Query != String.Empty)) {
                obj.DoCancel();

                // todo:
                // _openDream.Connection.SendPacket(new PacketTopic(newUri.Query));
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
