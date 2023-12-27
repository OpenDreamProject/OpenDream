using OpenDreamClient.Interface.Controls;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace OpenDreamClient.Interface;

public static class ResCacheExtension
{
    public static Font GetFont(this IResourceCache cache, ResPath[] path, int size)
    {
        var fs = new Font[path.Length];
        for (var i = 0; i < path.Length; i++)
            fs[i] = new VectorFont(cache.GetResource<FontResource>(path[i]), size);

        return new StackedFont(fs);
    }

    public static Font GetFont(this IResourceCache cache, string[] path, int size)
    {
        var rp = new ResPath[path.Length];
        for (var i = 0; i < path.Length; i++)
            rp[i] = new ResPath(path[i]);

        return cache.GetFont(rp, size);
    }

    // diet notostack from ss14
    public static Font NotoStack(this IResourceCache resCache, string variation = "Regular", int size = 10, bool display = false)
    {
        var ds = display ? "Display" : "";
        return resCache.GetFont
        (
            // Ew, but ok
            [
                $"/Fonts/NotoSans{ds}-{variation}.ttf",
            ],
            size
        );
    }
}

public static class DreamStylesheet {

    public static Stylesheet Make() {
        var res = IoCManager.Resolve<IResourceCache>();
        var textureCloseButton = res.GetResource<TextureResource>("/cross.svg.png").Texture;
        var notoSansFont10 = res.NotoStack();
        var notoSansFont12 = res.NotoStack("Regular", 12);
        var notoSansBold14 = res.NotoStack("Bold", 14);
        var notoSansBold16 = res.NotoStack("Bold", 16);

        var scrollBarNormal = new StyleBoxFlat {
            BackgroundColor = Color.Gray.WithAlpha(0.35f), ContentMarginLeftOverride = 10,
            ContentMarginTopOverride = 10
        };

        var scrollBarHovered = new StyleBoxFlat {
            BackgroundColor = new Color(140, 140, 140).WithAlpha(0.35f), ContentMarginLeftOverride = 10,
            ContentMarginTopOverride = 10
        };

        var scrollBarGrabbed = new StyleBoxFlat {
            BackgroundColor = new Color(160, 160, 160).WithAlpha(0.35f), ContentMarginLeftOverride = 10,
            ContentMarginTopOverride = 10
        };

        return new Stylesheet(new StyleRule[] {
            Element<WindowRoot>()
                .Prop("background", Color.White),

            Element<PanelContainer>().Class("MapBackground")
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat { BackgroundColor = Color. Black}),

            Element<PanelContainer>().Class("ContextMenuBackground")
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat {
                    BackgroundColor = Color.White,
                    BorderColor = Color.DarkGray,
                    BorderThickness = new Thickness(1)
                }),

            // Default font.
            Element()
                .Prop("font", notoSansFont12)
                .Prop("font-color", Color.Black),

            // VScrollBar grabber normal
            Element<VScrollBar>()
                .Prop(ScrollBar.StylePropertyGrabber, scrollBarNormal),

            // VScrollBar grabber hovered
            Element<VScrollBar>().Pseudo(ScrollBar.StylePseudoClassHover)
                .Prop(ScrollBar.StylePropertyGrabber, scrollBarHovered),

            // VScrollBar grabber grabbed
            Element<VScrollBar>().Pseudo(ScrollBar.StylePseudoClassGrabbed)
                .Prop(ScrollBar.StylePropertyGrabber, scrollBarGrabbed),

            // HScrollBar grabber normal
            Element<HScrollBar>()
                .Prop(ScrollBar.StylePropertyGrabber, scrollBarNormal),

            // HScrollBar grabber hovered
            Element<HScrollBar>().Pseudo(ScrollBar.StylePseudoClassHover)
                .Prop(ScrollBar.StylePropertyGrabber, scrollBarHovered),

            // HScrollBar grabber grabbed
            Element<HScrollBar>().Pseudo(ScrollBar.StylePseudoClassGrabbed)
                .Prop(ScrollBar.StylePropertyGrabber, scrollBarGrabbed),

            // Window background default color.
            Element().Class(DefaultWindow.StyleClassWindowPanel)
                .Prop("panel", new StyleBoxFlat { BackgroundColor = Color.FromHex("#4A4A4A") }),

            // Window title properties
            Element().Class(DefaultWindow.StyleClassWindowTitle)
                // Color
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#000000"))
                // Font
                .Prop(Label.StylePropertyFont, notoSansBold14),

            // Window header color.
            Element().Class(DefaultWindow.StyleClassWindowHeader)
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat {
                    BackgroundColor = Color.FromHex("#636396"), Padding = new Thickness(1, 1)
                }),

            // Window close button
            Element().Class(DefaultWindow.StyleClassWindowCloseButton)
                // Button texture
                .Prop(TextureButton.StylePropertyTexture, textureCloseButton)
                // Normal button color
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#000000")),

            // Window close button hover color
            Element().Class(DefaultWindow.StyleClassWindowCloseButton).Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#505050")),

            // Window close button pressed color
            Element().Class(DefaultWindow.StyleClassWindowCloseButton).Pseudo(TextureButton.StylePseudoClassPressed)
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#808080")),

            // Button style normal
            Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Pseudo(ContainerButton.StylePseudoClassNormal)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat { BackgroundColor = Color.FromHex("#C0C0C0"), BorderThickness = new Thickness(1), BorderColor = Color.FromHex("#707070")}),

            // Button style hovered
            Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat { BackgroundColor = Color.FromHex("#D0D0D0"), BorderThickness = new Thickness(1), BorderColor = Color.FromHex("#707070")}),

            // Button style pressed
            Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Pseudo(ContainerButton.StylePseudoClassPressed)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat { BackgroundColor = Color.FromHex("#E0E0E0"), BorderThickness = new Thickness(1), BorderColor = Color.FromHex("#707070") }),

            // Button style disabled
            Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Pseudo(ContainerButton.StylePseudoClassDisabled)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat { BackgroundColor = Color.FromHex("#FAFAFA"), BorderThickness = new Thickness(1), BorderColor = Color.FromHex("#707070")}),

            // DMF ControlButton
            Element<Label>().Class(ControlButton.StyleClassDMFButton)
                .Prop(Label.StylePropertyAlignMode, Label.AlignMode.Center)
                .Prop(Label.StylePropertyFont, notoSansFont10),

            // CheckBox unchecked
            Element<TextureRect>().Class(CheckBox.StyleClassCheckBox)
                .Prop(TextureRect.StylePropertyTexture, Texture.Black), // TODO: Add actual texture instead of this.

            // CheckBox unchecked
            Element<TextureRect>().Class(CheckBox.StyleClassCheckBox, CheckBox.StyleClassCheckBoxChecked)
                .Prop(TextureRect.StylePropertyTexture, Texture.White), // TODO: Add actual texture instead of this.

            // LineEdit
            Element<LineEdit>()
                // background color
                .Prop(LineEdit.StylePropertyStyleBox, new StyleBoxFlat{ BackgroundColor = Color.FromHex("#D3B5B5"), BorderThickness = new Thickness(1), BorderColor = Color.FromHex("#abadb3")})
                // default font color
                .Prop("font-color", Color.Black)
                .Prop("cursor-color", Color.Black),

            // LineEdit non-editable text color
            Element<LineEdit>().Class(LineEdit.StyleClassLineEditNotEditable)
                .Prop("font-color", Color.FromHex("#363636")),

            // LineEdit placeholder text color
            Element<LineEdit>().Pseudo(LineEdit.StylePseudoClassPlaceholder)
                .Prop("font-color", Color.FromHex("#7d7d7d")),

            // ItemList selected item
            Element<ItemList>()
                .Prop(ItemList.StylePropertySelectedItemBackground, new StyleBoxFlat { BackgroundColor = Color.Blue }),

            // TabContainer
            Element<TabContainer>()
                // Panel style
                .Prop(TabContainer.StylePropertyPanelStyleBox, new StyleBoxFlat { BackgroundColor = Color.White, BorderThickness = new Thickness(1), BorderColor = Color.Black})
                // Active tab style
                .Prop(TabContainer.StylePropertyTabStyleBox, new StyleBoxFlat {
                    BackgroundColor = Color.FromHex("#707070"), PaddingLeft = 1, PaddingRight = 1, ContentMarginLeftOverride = 5, ContentMarginRightOverride = 5
                })
                // Inactive tab style
                .Prop(TabContainer.StylePropertyTabStyleBoxInactive, new StyleBoxFlat {
                    BackgroundColor = Color.FromHex("#D0D0D0"), PaddingLeft = 1, PaddingRight = 1, ContentMarginLeftOverride = 5, ContentMarginRightOverride = 5
                })
                .Prop("font", notoSansFont10),

            //BarControl - composed of ProgressBar and Slider
            Element<ProgressBar>()
                .Prop(ProgressBar.StylePropertyBackground, new StyleBoxFlat { BackgroundColor = Color.LightGray, BorderThickness = new Thickness(1), BorderColor = Color.Black})
                .Prop(ProgressBar.StylePropertyForeground, new StyleBoxFlat { BackgroundColor = Color.Transparent, BorderThickness = new Thickness(1), BorderColor = Color.Black}),
            Element<Slider>()
                .Prop(Slider.StylePropertyBackground, new StyleBoxFlat { BackgroundColor = Color.Transparent, BorderThickness = new Thickness(1), BorderColor = Color.Black})
                .Prop(Slider.StylePropertyForeground, new StyleBoxFlat { BackgroundColor = Color.LightGray, BorderThickness = new Thickness(1), BorderColor = Color.Black})
                .Prop(Slider.StylePropertyGrabber, new StyleBoxFlat { BackgroundColor = Color.Transparent, BorderThickness = new Thickness(1), BorderColor = Color.Black, ContentMarginLeftOverride=10, ContentMarginRightOverride=10})
                .Prop(Slider.StylePropertyFill, new StyleBoxFlat { BackgroundColor = Color.Transparent, BorderThickness = new Thickness(0), BorderColor = Color.Black}),


            // main menu UI
            Element<Label>().Class("od-important")
                .Prop(Label.StylePropertyFont, notoSansBold14)
                .Prop(Label.StylePropertyFontColor, Color.DarkRed),

            Element<Label>().Class("od-wip")
                .Prop(Label.StylePropertyFont, notoSansFont12)
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#aaaaaabb")),

            Element<ContainerButton>().Class("od-button").Pseudo(ContainerButton.StylePseudoClassNormal)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat {
                    BackgroundColor = Color.FromHex("#C0C0C0"),
                }),
            Element<ContainerButton>().Class("od-button").Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat {
                    BackgroundColor = Color.FromHex("#d9d9d9"),
                }),

            new StyleRule(new SelectorChild(
                    new SelectorElement(typeof(Button), null, "mainMenu", null),
                    new SelectorElement(typeof(Label), null, null, null)),
                new[]
                {
                    new StyleProperty("font", notoSansBold16),
                }),

        });
    }
}
