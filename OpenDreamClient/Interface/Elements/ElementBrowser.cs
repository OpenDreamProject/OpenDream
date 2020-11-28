using System;
using Microsoft.Web.WebView2.Wpf;
using System.Windows.Controls;
using OpenDreamShared.Interface;

namespace OpenDreamClient.Interface.Elements {
    class ElementBrowser : DockPanel, IElement {
        public WebView2 webView;
        public ElementDescriptor ElementDescriptor {
            get => _elementDescriptor;
            set {
                _elementDescriptor = (ElementDescriptorBrowser)value;
                UpdateVisuals();
            }
        }

        private Label _loadingLabel;
        private string _htmlSource;
        private ElementDescriptorBrowser _elementDescriptor;

        public ElementBrowser() {
            _loadingLabel = new Label();
            _loadingLabel.Content = "Loading WebView2\nIf nothing happens, you may need to install the WebView2 runtime";
            webView = new WebView2();

            this.Children.Add(webView);
            this.Children.Add(_loadingLabel);
            webView.CoreWebView2Ready += OnWebView2Ready;
            webView.EnsureCoreWebView2Async();
        }

        public void SetHtmlSource(string htmlSource) {
            _htmlSource = htmlSource;
            if (webView.CoreWebView2 != null) webView.CoreWebView2.NavigateToString(htmlSource);
        }

        private void UpdateVisuals() {
            
        }

        private void OnWebView2Ready(object sender, EventArgs e) {
            this.Children.Remove(_loadingLabel);
            if (_htmlSource != null) webView.NavigateToString(_htmlSource);
        }
    }
}
