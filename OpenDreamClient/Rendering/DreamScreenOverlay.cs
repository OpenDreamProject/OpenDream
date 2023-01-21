using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace OpenDreamClient.Rendering;

/// <summary>
/// Overlay for rendering screen objects
/// </summary>
public sealed class DreamScreenOverlay : Overlay {
    // Drawn in world space instead of screen space so that it scales the same as everything else
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    /// <summary>
    /// Whether or not this overlay should draw anything. Controlled by the "togglescreenoverlay" command.
    /// </summary>
    public bool Enabled = true;

    private readonly RenderOrderComparer _renderOrderComparer = new();

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public DreamScreenOverlay() {
        IoCManager.InjectDependencies(this);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args) {
        return Enabled;
    }

    protected override void Draw(in OverlayDrawArgs args) {
        EntityUid? eye = _playerManager.LocalPlayer?.Session.AttachedEntity;
        if (eye == null) return;

        if (!_entityManager.TryGetComponent<TransformComponent>(eye, out var eyeTransform))
            return;

        ClientScreenOverlaySystem screenOverlaySystem = EntitySystem.Get<ClientScreenOverlaySystem>();

        Vector2 viewOffset = eyeTransform.WorldPosition - (args.WorldAABB.Size / 2f);

        List<(DreamIcon, Vector2, EntityUid)> sprites = new();
        foreach (DMISpriteComponent sprite in screenOverlaySystem.EnumerateScreenObjects()) {
            if (!sprite.IsVisible(checkWorld: false, mapManager: _mapManager))
                continue;
            if (sprite.ScreenLocation.MapControl != null) // Don't render screen objects meant for other map controls
                continue;
            Vector2 position = sprite.ScreenLocation.GetViewPosition(viewOffset, EyeManager.PixelsPerMeter);
            Vector2 iconSize = sprite.Icon.DMI.IconSize / (float)EyeManager.PixelsPerMeter;
            for (int x = 0; x < sprite.ScreenLocation.RepeatX; x++) {
                for (int y = 0; y < sprite.ScreenLocation.RepeatY; y++) {
                    sprites.Add((sprite.Icon, position + iconSize * (x, y), sprite.Owner));
                }
            }
        }

        DrawingHandleWorld worldHandle = args.WorldHandle;

        sprites.Sort(_renderOrderComparer);
        foreach ((DreamIcon, Vector2, EntityUid) sprite in sprites) {
            sprite.Item1.Draw(worldHandle, sprite.Item2);
        }
    }
}

public sealed class ToggleScreenOverlayCommand : IConsoleCommand {
    public string Command => "togglescreenoverlay";
    public string Description => "Toggle rendering of screen objects";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        if (args.Length != 0) {
            shell.WriteError("This command does not take any arguments!");
            return;
        }

        IOverlayManager overlayManager = IoCManager.Resolve<IOverlayManager>();
        if (overlayManager.TryGetOverlay(typeof(DreamScreenOverlay), out var overlay) &&
            overlay is DreamScreenOverlay screenOverlay) {
            screenOverlay.Enabled = !screenOverlay.Enabled;
        }
    }
}
