using System;
using Microsoft.Web.WebView2.Wpf;
using System.Windows.Controls;
using OpenDreamShared.Interface;
using Microsoft.Web.WebView2.Core;
using OpenDreamShared.Net.Packets;
using System.Web;

namespace OpenDreamClient.Interface.Elements {
    class ElementBrowser : DockPanel, IElement {
        public WebView2 WebView;
        public ElementDescriptor ElementDescriptor {
            get => _elementDescriptor;
            set {
                _elementDescriptor = (ElementDescriptorBrowser)value;
            }
        }

        private Label _loadingLabel;
        private string _fileSource;
        private ElementDescriptorBrowser _elementDescriptor;
        private bool _webViewReady;

        public ElementBrowser() {
            _loadingLabel = new Label();
            _loadingLabel.Content = "Loading WebView2\nIf nothing happens, you may need to install the WebView2 runtime";
            WebView = new WebView2();

            this.Children.Add(WebView);
            this.Children.Add(_loadingLabel);
            WebView.CoreWebView2InitializationCompleted += OnWebView2InitializationCompleted;
            WebView.NavigationStarting += OnWebViewNavigationStarting;
            WebView.EnsureCoreWebView2Async();
        }

        public void SetFileSource(string filepath) {
            _fileSource = filepath;
            if (_webViewReady) WebView.CoreWebView2.Navigate("file://" + _fileSource);
        }

        public void UpdateVisuals() {

        }

        public void Output(string value, string jsFunction) {
            if (!_webViewReady) return;
            if (jsFunction == null) return;

            value = HttpUtility.UrlDecode(value);
            value = value.Replace("\"", "\\\"");
            WebView.CoreWebView2.ExecuteScriptAsync(jsFunction + "(\"" + value + "\")");
        }

        private void OnWebView2InitializationCompleted(object sender, EventArgs e) {
            _webViewReady = true;

            this.Children.Remove(_loadingLabel);
            if (_fileSource != null) WebView.CoreWebView2.Navigate("file://" + _fileSource);
        }

        private void OnWebViewNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e) {
            Uri oldUri = new Uri(WebView.CoreWebView2.Source);
            Uri newUri = new Uri(e.Uri);

            if (newUri.Scheme == "byond" || (newUri.AbsolutePath == oldUri.AbsolutePath && newUri.Query != String.Empty)) {
                e.Cancel = true;

                Program.OpenDream.Connection.SendPacket(new PacketTopic(newUri.Query));
            }
        }
    }
}
