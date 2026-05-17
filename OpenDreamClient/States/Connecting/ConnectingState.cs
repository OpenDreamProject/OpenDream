using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace OpenDreamClient.States.Connecting;

public sealed partial class ConnectingState : State {
    [Dependency] private IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private IConfigurationManager _configurationManager = default!;

    private ConnectingControl _connectingControl = default!;

    protected override void Startup() {
        _connectingControl = new ConnectingControl(_resourceCache, _configurationManager);
        _userInterfaceManager.StateRoot.AddChild(_connectingControl);
    }

    protected override void Shutdown() {
        _connectingControl.Dispose();
    }
}
