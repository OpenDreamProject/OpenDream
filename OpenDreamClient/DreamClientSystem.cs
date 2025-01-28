using OpenDreamClient.Interface;
using Robust.Shared.Player;

namespace OpenDreamClient;

internal sealed class DreamClientSystem : EntitySystem {
    [Dependency] private readonly IDreamInterfaceManager _interfaceManager = default!;

    public override void Initialize() {
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent e) {
        // The active input context gets reset to "common" when a new player is attached
        // So we have to set it again
        _interfaceManager.DefaultWindow?.Macro.SetActive();
    }
}
