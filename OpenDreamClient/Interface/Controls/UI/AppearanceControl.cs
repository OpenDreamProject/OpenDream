using OpenDreamClient.Rendering;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace OpenDreamClient.Interface.Controls.UI;

/// <summary>
/// Draws an icon with an appearance ID
/// </summary>
public sealed class AppearanceControl : Control {
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IDreamInterfaceManager _interfaceManager = default!;

    private readonly DreamViewOverlay _overlay;
    private readonly DreamIcon _icon;

    public AppearanceControl(uint appearanceId) {
        IoCManager.InjectDependencies(this);

        var dmiSpriteSystem = _entitySystemManager.GetEntitySystem<DMISpriteSystem>();
        var appearanceSystem = _entitySystemManager.GetEntitySystem<ClientAppearanceSystem>();
        _overlay = _overlayManager.GetOverlay<DreamViewOverlay>();
        _icon = new DreamIcon(dmiSpriteSystem.RenderTargetPool, _interfaceManager,
            _gameTiming, _clyde, appearanceSystem, appearanceId);

        _icon.SizeChanged += InvalidateMeasure;
    }

    protected override void Draw(IRenderHandle renderHandle) {
        var world = renderHandle.DrawingHandleWorld;
        var screen = renderHandle.DrawingHandleScreen;

        var texture = _icon.GetTexture(_overlay, world, new RendererMetaData {MainIcon = _icon}, null, null);
        if (texture is null)
            return;

        var position = new Vector2(0, PixelPosition.Y); // For, uh, some reason
        screen.DrawTextureRect(texture, new UIBox2(position, position + PixelSize));
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize) {
        // TODO: Would be ideal if we could always render at the texture's size
        //       RichTextLabel doesn't make that possible right now though (enforces the font's line height)
        var size = Math.Min(_icon.DMI?.IconSize.Y ?? 0, availableSize.Y);

        return new(size);
    }
}
