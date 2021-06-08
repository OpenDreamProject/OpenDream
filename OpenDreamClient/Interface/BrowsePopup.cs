using OpenDreamClient.Interface.Elements;
using OpenDreamShared.Interface;
using System;
using System.Windows;

namespace OpenDreamClient.Interface {

    class BrowsePopup {
        public event EventHandler Closed;

        public ElementBrowser Browser;
        public ElementWindow WindowElement;

        private Window _window;

        public BrowsePopup(string name, System.Drawing.Size size, Window ownerWindow) {
            WindowDescriptor popupWindowDescriptor = new WindowDescriptor(name, new() {
                new ElementDescriptorMain("main") {
                    Size = size
                },
                new ElementDescriptorBrowser("browser") {
                    Size = size,
                    Anchor1 = new System.Drawing.Point(0, 0),
                    Anchor2 = new System.Drawing.Point(100, 100)
                }
            });

            WindowElement = new ElementWindow(popupWindowDescriptor);
            WindowElement.CreateChildElements();

            _window = WindowElement.CreateWindow();
            _window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _window.Owner = ownerWindow;
            _window.Closed += OnWindowClosed;

            Browser = (ElementBrowser)WindowElement.ChildElements[0];
        }

        public void Open() {
            _window.Show();
            _window.Focus();
        }

        public void Close() {
            _window.Close();
        }

        private void OnWindowClosed(object sender, EventArgs e) {
            Closed?.Invoke(sender, e);
        }
    }
}
