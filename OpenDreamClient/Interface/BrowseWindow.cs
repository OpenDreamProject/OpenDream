using OpenDreamClient.Interface.Elements;
using OpenDreamShared.Interface;
using System;

namespace OpenDreamClient.Interface {
    class BrowseWindow : System.Windows.Window {
        private ElementBrowser _browser;

        public BrowseWindow(string htmlSource) {
            InterfaceWindowDescriptor browseWindowDescriptor = new InterfaceWindowDescriptor("browseWindow", new() {
                new ElementDescriptorMain("browseWindowMain") {
                    Size = new System.Drawing.Size(480, 480)
                },
                new ElementDescriptorBrowser("browseWindowBrowser") {
                    Size = new System.Drawing.Size(480, 480),
                    Anchor1 = new System.Drawing.Point(0, 0),
                    Anchor2 = new System.Drawing.Point(100, 100)
                }
            });

            ElementWindow defaultWindow = InterfaceHelpers.CreateWindowFromDescriptor(browseWindowDescriptor);
            this.Width = defaultWindow.ElementDescriptor.Size.Value.Width;
            this.Height = defaultWindow.ElementDescriptor.Size.Value.Height;
            this.Content = defaultWindow;

            _browser = (ElementBrowser)defaultWindow.ChildElements[0];
            _browser.SetHtmlSource(htmlSource);
        }
    }
}