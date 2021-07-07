using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace OpenDreamClient
{
    public static class DreamStylesheet
    {
        public static Stylesheet Make()
        {
            var res = IoCManager.Resolve<IResourceCache>();
            var textureCloseButton = res.GetResource<TextureResource>("/cross.svg.png").Texture;
            var notoSansFont = res.GetResource<FontResource>("/Fonts/NotoSans-Regular.ttf");
            var notoSans12 = new VectorFont(notoSansFont, 12);

            return new Stylesheet(new StyleRule[]
            {
                Element().Prop("font", notoSans12),

                Element().Class(SS14Window.StyleClassWindowPanel)
                    .Prop("panel", new StyleBoxFlat {BackgroundColor = Color.DarkSlateGray}),

                // Window close button base texture.
                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] {SS14Window.StyleClassWindowCloseButton}, null,
                        null),
                    new[]
                    {
                        new StyleProperty(TextureButton.StylePropertyTexture, textureCloseButton),
                        new StyleProperty(Control.StylePropertyModulateSelf, Color.FromHex("#4B596A")),
                    }),
                // Window close button hover.
                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] {SS14Window.StyleClassWindowCloseButton}, null,
                        new[] {TextureButton.StylePseudoClassHover}),
                    new[]
                    {
                        new StyleProperty(Control.StylePropertyModulateSelf, Color.FromHex("#7F3636")),
                    }),
                // Window close button pressed.
                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] {SS14Window.StyleClassWindowCloseButton}, null,
                        new[] {TextureButton.StylePseudoClassPressed}),
                    new[]
                    {
                        new StyleProperty(Control.StylePropertyModulateSelf, Color.FromHex("#753131")),
                    }),

            });
        }
    }
}
