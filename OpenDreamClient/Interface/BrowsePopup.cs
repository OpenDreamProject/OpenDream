using OpenDreamClient.Interface.Controls;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface;

internal sealed class BrowsePopup {
    public event Action Closed;

    public ControlBrowser Browser;
    public ControlWindow WindowElement;

    private OSWindow _window;

    public BrowsePopup(
        string name,
        Vector2i size,
        IClydeWindow ownerWindow) {
        WindowDescriptor popupWindowDescriptor = new WindowDescriptor(name,
            new() {
                new ControlDescriptorBrowser {
                    Id = new DMFPropertyString("browser"),
                    Size = new DMFPropertyVec2(size),
                    Anchor1 = new DMFPropertyVec2(0, 0),
                    Anchor2 = new DMFPropertyVec2(100, 100)
                }
            }) {
                Size = new DMFPropertyVec2(size)
            };

        WindowElement = new ControlWindow(popupWindowDescriptor);
        WindowElement.CreateChildControls();

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
}
