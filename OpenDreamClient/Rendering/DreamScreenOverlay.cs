using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace OpenDreamClient.Rendering;

/// <summary>
/// Overlay for rendering screen objects
/// </summary>
public sealed class DreamScreenOverlay : Overlay {
    // Drawn in world space instead of screen space so that it scales the same as everything else
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly RenderOrderComparer _renderOrderComparer = new();

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public DreamScreenOverlay() {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args) {
        EntityUid? eye = _playerManager.LocalPlayer?.Session.AttachedEntity;
        if (eye == null) return;

        if (!_entityManager.TryGetComponent<TransformComponent>(eye, out var eyeTransform))
            return;

        ClientScreenOverlaySystem screenOverlaySystem = EntitySystem.Get<ClientScreenOverlaySystem>();

        Vector2 viewOffset = eyeTransform.WorldPosition - (args.WorldAABB.Size / 2f);

        List<DMISpriteComponent> sprites = new();
        foreach (DMISpriteComponent sprite in screenOverlaySystem.EnumerateScreenObjects()) {
            if (!sprite.IsVisible(checkWorld: false, mapManager: _mapManager)) continue;

            sprites.Add(sprite);
        }

        DrawingHandleWorld worldHandle = args.WorldHandle;

        sprites.Sort(_renderOrderComparer);
        foreach (DMISpriteComponent sprite in sprites) {
            Vector2 position = sprite.ScreenLocation.GetViewPosition(viewOffset, EyeManager.PixelsPerMeter);
            Vector2 iconSize = sprite.Icon.DMI.IconSize / (float)EyeManager.PixelsPerMeter;

            for (int x = 0; x < sprite.ScreenLocation.RepeatX; x++) {
                for (int y = 0; y < sprite.ScreenLocation.RepeatY; y++) {
                    sprite.Icon.Draw(worldHandle, position + iconSize * (x, y));
                }
            }
        }
    }
}
