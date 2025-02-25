using Robust.Client.Graphics;

namespace OpenDreamClient.Interface.Controls.UI;

/// <summary>
/// Same as StyleBoxTexture, but fills a colored box behind the texture first.
/// </summary>
public sealed class StyleBoxColoredTexture : StyleBoxTexture {
    public Color BackgroundColor = Color.Transparent;

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale) {
        var scaledBox = new UIBox2(
            box.Left - ExpandMarginLeft * uiScale,
            box.Top - ExpandMarginTop * uiScale,
            box.Right + ExpandMarginRight * uiScale,
            box.Bottom + ExpandMarginBottom * uiScale);

        handle.DrawRect(scaledBox, BackgroundColor);
        base.DoDraw(handle, box, uiScale);
    }
}
