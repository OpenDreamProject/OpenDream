using OpenDreamClient.Interface;
using OpenDreamClient.Rendering;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace OpenDreamClient;

internal sealed class DreamClientSystem : EntitySystem {
    [Dependency] private readonly IDreamInterfaceManager _interfaceManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly ClientAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly ClientScreenOverlaySystem _screenOverlaySystem = default!;
    [Dependency] private readonly ClientImagesSystem _clientImagesSystem = default!;

    public override void Initialize() {
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);

        var mapOverlay = new DreamViewOverlay(_transformSystem, _lookupSystem, _appearanceSystem, _screenOverlaySystem, _clientImagesSystem);
        _overlayManager.AddOverlay(mapOverlay);
    }

    public override void Shutdown() {
        _overlayManager.RemoveOverlay<DreamViewOverlay>();
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent e) {
        // The active input context gets reset to "common" when a new player is attached
        // So we have to set it again
        _interfaceManager.DefaultWindow?.Macro.SetActive();
    }
}
