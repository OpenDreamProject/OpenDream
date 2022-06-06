using JetBrains.Annotations;
using OpenDreamClient.States.Connecting;
using OpenDreamClient.States.MainMenu;
using Robust.Client;
using Robust.Client.State;

namespace OpenDreamClient.States
{
    /// <summary>
    ///     Handles changing the UI state depending on connection status.
    /// </summary>
    [UsedImplicitly]
    public sealed class DreamUserInterfaceStateManager
    {
        [Dependency] private readonly IGameController _gameController = default!;
        [Dependency] private readonly IBaseClient _client = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;

        public void Initialize()
        {
            _client.RunLevelChanged += ((_, args) =>
            {
                switch (args.NewLevel)
                {
                    case ClientRunLevel.InGame:
                    case ClientRunLevel.Connected:
                    case ClientRunLevel.SinglePlayerGame:
                        _stateManager.RequestStateChange<InGameState>();
                        break;

                    case ClientRunLevel.Initialize when args.OldLevel < ClientRunLevel.Connected:
                        _stateManager.RequestStateChange<MainMenuState>();
                        break;

                    // When we disconnect from the server:
                    case ClientRunLevel.Error:
                    case ClientRunLevel.Initialize when args.OldLevel >= ClientRunLevel.Connected:
                        if (_gameController.LaunchState.FromLauncher)
                        {
                            _stateManager.RequestStateChange<ConnectingState>();
                            break;
                        }

                        _stateManager.RequestStateChange<MainMenuState>();
                        break;

                    case ClientRunLevel.Connecting:
                        _stateManager.RequestStateChange<ConnectingState>();
                        break;
                }
            });
        }
    }
}
