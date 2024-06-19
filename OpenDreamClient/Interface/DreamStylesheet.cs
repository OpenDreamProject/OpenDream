using OpenDreamClient.Interface.Controls;
using OpenDreamClient.Interface.Controls.UI;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace OpenDreamClient.Interface;

public static class DreamStylesheet {
    public static Color DefaultBackgroundColor = Color.FromHex("#F0F0F0");

    public static Stylesheet Make() {
        var res = IoCManager.Resolve<IResourceCache>();
        var textureCloseButton = res.GetResource<TextureResource>("/cross.svg.png").Texture;
        var notoSansFont = res.GetResource<FontResource>("/Fonts/NotoSans-Regular.ttf");
        var notoSansBoldFont = res.GetResource<FontResource>("/Fonts/NotoSans-Bold.ttf");
        var notoSansFont8 = new VectorFont(notoSansFont, 8);
        var notoSansFont10 = new VectorFont(notoSansFont, 10);
        var notoSansFont12 = new VectorFont(notoSansFont, 12);
        var notoSansBoldFont14 = new VectorFont(notoSansBoldFont, 14);

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

        var tabButtonActive = new StyleBoxColoredTexture {
            Texture = res.GetResource<TextureResource>("/Textures/Interface/TabButton.png"),
            PatchMarginTop = 2,
            PatchMarginBottom = 2,
            PatchMarginLeft = 2,
            PatchMarginRight = 2,
            ContentMarginLeftOverride = 5,
            ContentMarginRightOverride = 5,
            ContentMarginBottomOverride = 5 // The selected button is positioned higher than the rest
        };

        var tabButtonInactive = new StyleBoxColoredTexture {
            Texture = res.GetResource<TextureResource>("/Textures/Interface/TabButton.png"),
            PatchMarginTop = 2,
            PatchMarginBottom = 2,
            PatchMarginLeft = 2,
            PatchMarginRight = 2,
            ContentMarginLeftOverride = 5,
            ContentMarginRightOverride = 5,
            PaddingTop = 3 // Account for the active button's ContentMarginBottomOverride
        };

        return new Stylesheet(new StyleRule[] {
            Element<WindowRoot>()
                .Prop(UIRoot.StylePropBackground, DefaultBackgroundColor),

            Element<PanelContainer>().Class("MapBackground")
                .Prop("panel", new StyleBoxFlat { BackgroundColor = Color. Black}),

            Element<PanelContainer>().Class("ContextMenuBackground")
                .Prop("panel", new StyleBoxFlat() {
                    BackgroundColor = Color.White,
                    BorderColor = Color.DarkGray,
                    BorderThickness = new Thickness(1)
                }),

            Element<PanelContainer>().Class(ControlInfo.StyleClassDMFInfo)
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat(DefaultBackgroundColor)),

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
                .Prop(Label.StylePropertyFont, notoSansBoldFont14),

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
                .Prop(Label.StylePropertyFont, notoSansFont8),

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
                // Active tab style
                .Prop(TabContainer.StylePropertyTabStyleBox, tabButtonActive)
                // Inactive tab style
                .Prop(TabContainer.StylePropertyTabStyleBoxInactive, tabButtonInactive)
                .Prop(TabContainer.stylePropertyTabFontColor, Color.Black)
                .Prop(TabContainer.StylePropertyTabFontColorInactive, Color.Black)
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
        });
    }
}
