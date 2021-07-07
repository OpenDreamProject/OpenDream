/*using System;
using Microsoft.Web.WebView2.Wpf;
using System.Windows.Controls;
using OpenDreamShared.Interface;
using Microsoft.Web.WebView2.Core;
using OpenDreamShared.Net.Packets;
using System.Web;
using System.Windows;

namespace OpenDreamClient.Interface.Controls {
    class ControlBrowser : InterfaceControl {
        private WebView2 _webView;
        private DockPanel _dockPanel;
        private Label _loadingLabel;
        private string _fileSource;
        private bool _webViewReady;

        public ControlBrowser(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

        protected override FrameworkElement CreateUIElement() {
            _dockPanel = new DockPanel();
            _webView = new WebView2();
            _loadingLabel = new Label() {
                Content = "Loading WebView2\nIf nothing happens, you may need to install the WebView2 runtime"
            };

            _dockPanel.Children.Add(_webView);
            _dockPanel.Children.Add(_loadingLabel);
            _webView.CoreWebView2InitializationCompleted += OnWebView2InitializationCompleted;
            _webView.NavigationStarting += OnWebViewNavigationStarting;
            _webView.EnsureCoreWebView2Async();

            return _dockPanel;
        }

        public override void Output(string value, string jsFunction) {
            if (!_webViewReady) return;
            if (jsFunction == null) return;

            value = HttpUtility.UrlDecode(value);
            value = value.Replace("\"", "\\\"");
            _webView.CoreWebView2.ExecuteScriptAsync(jsFunction + "(\"" + value + "\")");
        }

        public void SetFileSource(string filepath) {
            _fileSource = filepath;
            if (_webViewReady) _webView.CoreWebView2.Navigate("file://" + _fileSource);
        }

        private void OnWebView2InitializationCompleted(object sender, EventArgs e) {
            _webViewReady = true;

            _dockPanel.Children.Remove(_loadingLabel);
            if (_fileSource != null) _webView.CoreWebView2.Navigate("file://" + _fileSource);
        }

        private void OnWebViewNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e) {
            Uri oldUri = new Uri(_webView.CoreWebView2.Source);
            Uri newUri = new Uri(e.Uri);

            if (newUri.Scheme == "byond" || (newUri.AbsolutePath == oldUri.AbsolutePath && newUri.Query != String.Empty)) {
                e.Cancel = true;

                Program.OpenDream.Connection.SendPacket(new PacketTopic(newUri.Query));
            }
        }
    }
}*/
