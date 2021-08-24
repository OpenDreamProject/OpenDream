using System;
using System.IO;
using System.Net;
using System.Web;
using Content.Shared.Interface;
using JetBrains.Annotations;
using Robust.Client.CEF;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.Interface.Controls {
    class ControlBrowser : InterfaceControl
    {
        [Dependency]
        private readonly IResourceManager _resourceManager = default!;
        private BrowserControl _webView;
        private Control _wrap;
        private Label _loadingLabel;
        private (ResourcePath path, bool userData) _fileSource;
        private bool _webViewReady;

        public ControlBrowser(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor,
            window)
        {
            IoCManager.InjectDependencies(this);
        }

        protected override Control CreateUIElement() {
            _wrap = new PanelContainer { PanelOverride = new StyleBoxFlat { BackgroundColor = Color.White}};
            _webView = new BrowserControl();
            _loadingLabel = new Label() {
                Text = "Loading WebView2\nIf nothing happens, you may need to install the WebView2 runtime"
            };

            _wrap.Children.Add(_webView);
            _wrap.Children.Add(_loadingLabel);
            _webView.AddResourceRequestHandler(RequestHandler);

            return _wrap;
        }

        public override void Output(string value, string jsFunction) {
            if (!_webViewReady) return;
            if (jsFunction == null) return;

            value = HttpUtility.UrlDecode(value);
            value = value.Replace("\"", "\\\"");
            // todo:
            //_webView.CoreWebView2.ExecuteScriptAsync(jsFunction + "(\"" + value + "\")");
        }

        public void SetFileSource(ResourcePath filepath, bool userData) {
            _fileSource = (filepath, userData);

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

                var mimeType = path.Extension switch
                {
                    "html" => "text/html",
                    _ => "text/plain"
                };

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
