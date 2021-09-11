using System;
using OpenDreamShared.Interface;
using OpenDreamClient.Interface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace OpenDreamClient.Interface
{
    class BrowsePopup {
        public event Action Closed;

        public ControlBrowser Browser;
        public ControlWindow WindowElement;

        private OSWindow _window;

        public BrowsePopup(
            DreamInterfaceManager manager,
            string name,
            Vector2i size,
            IClydeWindow ownerWindow) {
            WindowDescriptor popupWindowDescriptor = new WindowDescriptor(name, new() {
                new ControlDescriptorMain("main") {
                    Size = size
                },
                new ControlDescriptorBrowser("browser") {
                    Size = size,
                    Anchor1 = new Vector2i(0, 0),
                    Anchor2 = new Vector2i(100, 100)
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
