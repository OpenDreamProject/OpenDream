/*using OpenDreamClient.Interface.Controls;
using OpenDreamShared.Interface;
using System;
using System.Windows;

namespace OpenDreamClient.Interface {
    class BrowsePopup {
        public event EventHandler Closed;

        public ControlBrowser Browser;
        public ControlWindow WindowElement;

        private Window _window;

        public BrowsePopup(string name, System.Drawing.Size size, Window ownerWindow) {
            WindowDescriptor popupWindowDescriptor = new WindowDescriptor(name, new() {
                new ControlDescriptorMain("main") {
                    Size = size
                },
                new ControlDescriptorBrowser("browser") {
                    Size = size,
                    Anchor1 = new System.Drawing.Point(0, 0),
                    Anchor2 = new System.Drawing.Point(100, 100)
                }
            });

            WindowElement = new ControlWindow(popupWindowDescriptor);
            WindowElement.CreateChildControls();

            _window = WindowElement.CreateWindow();
            _window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _window.Owner = ownerWindow;
            _window.Closed += OnWindowClosed;

            Browser = (ControlBrowser)WindowElement.ChildControls[0];
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
*/
