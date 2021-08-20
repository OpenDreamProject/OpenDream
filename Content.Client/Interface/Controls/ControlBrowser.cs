using System;
using System.Web;
using Content.Shared.Interface;
using Robust.Client.CEF;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client.Interface.Controls {
    class ControlBrowser : InterfaceControl
    {
        // [Dependency]
        // private readonly OpenDream _openDream = default!;
        private BrowserControl _webView;
        private Control _wrap;
        private Label _loadingLabel;
        private string _fileSource;
        private bool _webViewReady;

        public ControlBrowser(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

        protected override Control CreateUIElement() {
            _wrap = new Control();
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

        public void SetFileSource(string filepath) {
            _fileSource = filepath;

            _webView.Url = "file://" + _fileSource;
        }

        private void RequestHandler(RequestHandlerContext obj)
        {
            Uri oldUri = new Uri(_webView.Url);
            Uri newUri = new Uri(obj.Url);

            if (newUri.Scheme == "byond" || (newUri.AbsolutePath == oldUri.AbsolutePath && newUri.Query != String.Empty)) {
                obj.DoCancel();

                // todo:
                // _openDream.Connection.SendPacket(new PacketTopic(newUri.Query));
            }
        }
    }
}
