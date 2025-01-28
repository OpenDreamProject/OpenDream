using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace OpenDreamClient.Interface.Controls.UI;

public sealed class StyleBoxInfoPanel : StyleBoxTexture {
    public Color BackgroundColor;

    public StyleBoxInfoPanel(Color backgroundColor) {
        BackgroundColor = backgroundColor;

        Texture = IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>("/Textures/Interface/PanelBorder.png");
        PatchMarginLeft = 6;
        PatchMarginBottom = 6;
        PatchMarginRight = 6;
        PatchMarginTop = 6;
    }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale) {
        var innerBox = new Thickness(PatchMarginLeft, PatchMarginTop - 2, PatchMarginRight, PatchMarginBottom).Deflate(box);

        handle.DrawRect(innerBox, BackgroundColor);
        base.DoDraw(handle, box, uiScale);
    }
}
