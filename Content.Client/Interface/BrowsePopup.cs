using System;
using Content.Client.Interface.Controls;
using Content.Shared.Interface;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client.Interface
{
    class BrowsePopup {
        public event Action Closed;

        public ControlBrowser Browser;
        public ControlWindow WindowElement;

        private OSWindow _window;

        public BrowsePopup(
            DreamInterfaceManager manager,
            string name,
            System.Drawing.Size size,
            IClydeWindow ownerWindow) {
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
            WindowElement.CreateChildControls(IoCManager.Resolve<DreamInterfaceManager>());

            _window = WindowElement.CreateWindow();
            _window.StartupLocation = WindowStartupLocation.CenterOwner;
            _window.Owner = ownerWindow;
            _window.Closed += OnWindowClosed;

            Browser = (ControlBrowser)WindowElement.ChildControls[0];
        }

        public void Open() {
            _window.Show();
            // _window.Focus();
        }

        public void Close() {
            _window.Close();
        }

        private void OnWindowClosed() {
            Closed?.Invoke();
        }
    }}
