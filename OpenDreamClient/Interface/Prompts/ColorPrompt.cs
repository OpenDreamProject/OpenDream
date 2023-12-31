using System.Linq;
using Linguini.Bundle.Errors;
using OpenDreamShared.Dream;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Stylesheets;

namespace OpenDreamClient.Interface.Prompts;

internal sealed class ColorPrompt : InputWindow {
    private readonly BoxContainer _baseControl;
    private readonly ColorSelectorSliders _colorSelector;
    private readonly LineEdit _hexColor;
    private readonly Button _preview;
    private readonly bool _nullable;
    private readonly Color _originalColor;

    public ColorPrompt(string title, string message, string defaultValue, bool canCancel,
        Action<DMValueType, object?>? onClose, bool alpha = false) : base(title, message, true, onClose) {
        _nullable = canCancel;
        _originalColor = Color.FromHex(defaultValue, Color.White);
        _colorSelector = new() {
            Color = _originalColor,
            VerticalAlignment = VAlignment.Top,
            Stylesheet = ColorPromptStylesheet.Make(),
            IsAlphaVisible = alpha,
            OnColorChanged = ColorSelectorSliders_OnColorChanged
        };
        var defaultHex = _colorSelector.IsAlphaVisible ? _originalColor.ToHex() : _originalColor.ToHexNoAlpha();
        _hexColor = new LineEdit {
            Text = defaultHex,
            HorizontalExpand = true,
            PlaceHolder = _colorSelector.IsAlphaVisible ? "#RRGGBBAA" : "#RRGGBB",
            IsValid = (string text) => {
                text = text.Trim().TrimStart('#');
                return text.Length <= (_colorSelector.IsAlphaVisible ? 8 : 6) && text.All(char.IsAsciiHexDigit);
            }
        };
        _hexColor.OnFocusExit += LineEdit_OnFinishInput;
        _hexColor.OnTextEntered += LineEdit_OnFinishInput;

        _preview = new Button {
            SetSize = new Vector2(32, 32),
            Modulate = _originalColor,
            MouseFilter = MouseFilterMode.Ignore,
        };

        _baseControl = new BoxContainer {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children = {
                _colorSelector,
                new BoxContainer {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    HorizontalExpand = true,
                    SeparationOverride = 8,
                    Children = { _preview, _hexColor }
                }
            }
        };

        SetPromptControl(_baseControl, grabKeyboard: false);
    }

    private void ColorSelectorSliders_OnColorChanged(Color color) {
        _hexColor.Text = _colorSelector.IsAlphaVisible ? color.ToHex() : color.ToHexNoAlpha();
        _preview.Modulate = color;
    }

    private void LineEdit_OnFinishInput(LineEdit.LineEditEventArgs args) {
        var text = args.Text.Trim();
        if (!text.StartsWith('#')) text = '#' + text;
        var newColor = Color.TryFromHex(text);
        if (newColor.HasValue) {
            _colorSelector.Color = newColor.Value;
        }
    }

    protected override void ButtonClicked(string button) {
        if (button == "Ok") {
            FinishPrompt(DMValueType.Color, _colorSelector.Color);
        } else if (!_nullable) {
            FinishPrompt(DMValueType.Color, _originalColor);
        } else {
            FinishPrompt(DMValueType.Null, null);
        }
        Close();
    }
}

// WHY IS ColorSelectorSliders A PART OF ROBUST, BUT THE STYLESHEET IT USES IS IN SS14?!
internal static class ColorPromptStylesheet {
    private static readonly Color PanelDark = Color.FromHex("#1E1E22");
    private const string StyleClassSliderRed = "Red";
    private const string StyleClassSliderGreen = "Green";
    private const string StyleClassSliderBlue = "Blue";
    private const string StyleClassSliderWhite = "White";

    private static StyleBoxTexture MakeSliderFill(Color color) {
        return new() {
            Texture = IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>("/Textures/Interface/Nano/slider_fill.svg.96dpi.png").Texture,
            Modulate = color,
            PatchMarginLeft = 12,
            PatchMarginRight = 12,
            PatchMarginTop = 12,
            PatchMarginBottom = 12,
        };
    }

    private static StyleBoxTexture MakeSliderGrab() {
        return new() {
            Texture = IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>("/Textures/Interface/Nano/slider_grabber.svg.96dpi.png").Texture,
            PatchMarginLeft = 12,
            PatchMarginRight = 12,
            PatchMarginTop = 12,
            PatchMarginBottom = 12,
        };
    }

    private static StyleBoxTexture MakeSliderOutline(Color color) {
        return new() {
            Texture = IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>("/Textures/Interface/Nano/slider_outline.svg.96dpi.png").Texture,
            Modulate = color,
            PatchMarginLeft = 12,
            PatchMarginRight = 12,
            PatchMarginTop = 12,
            PatchMarginBottom = 12,
        };
    }


    public static Stylesheet Make() {
        var sliderFillBox = MakeSliderFill(Color.FromHex("#3E6C45"));
        var sliderBackBox = MakeSliderOutline(PanelDark);
        var sliderForeBox = MakeSliderOutline(Color.FromHex("#494949"));
        var sliderGrabBox = MakeSliderGrab();

        var sliderFillGreen = new StyleBoxTexture(sliderFillBox) { Modulate = Color.LimeGreen };
        var sliderFillRed = new StyleBoxTexture(sliderFillBox) { Modulate = Color.Red };
        var sliderFillBlue = new StyleBoxTexture(sliderFillBox) { Modulate = Color.Blue };
        var sliderFillWhite = new StyleBoxTexture(sliderFillBox) { Modulate = Color.White };

        var styles = new DefaultStylesheet(IoCManager.Resolve<IResourceCache>(), IoCManager.Resolve<IUserInterfaceManager>()).Stylesheet.Rules.ToList();
        var newStyles = new StyleRule[] {
            // Slider
            new StyleRule(SelectorElement.Type(typeof(Slider)), new []
            {
                new StyleProperty(Slider.StylePropertyBackground, sliderBackBox),
                new StyleProperty(Slider.StylePropertyForeground, sliderForeBox),
                new StyleProperty(Slider.StylePropertyGrabber, sliderGrabBox),
                new StyleProperty(Slider.StylePropertyFill, sliderFillBox),
            }),

            new StyleRule(SelectorElement.Type(typeof(ColorableSlider)), new []
            {
                new StyleProperty(ColorableSlider.StylePropertyFillWhite, sliderFillWhite),
                new StyleProperty(ColorableSlider.StylePropertyBackgroundWhite, sliderFillWhite),
            }),

            new StyleRule(new SelectorElement(typeof(Slider), new []{StyleClassSliderRed}, null, null), new []
            {
                new StyleProperty(Slider.StylePropertyFill, sliderFillRed),
            }),

            new StyleRule(new SelectorElement(typeof(Slider), new []{StyleClassSliderGreen}, null, null), new []
            {
                new StyleProperty(Slider.StylePropertyFill, sliderFillGreen),
            }),

            new StyleRule(new SelectorElement(typeof(Slider), new []{StyleClassSliderBlue}, null, null), new []
            {
                new StyleProperty(Slider.StylePropertyFill, sliderFillBlue),
            }),

            new StyleRule(new SelectorElement(typeof(Slider), new []{StyleClassSliderWhite}, null, null), new []
            {
                new StyleProperty(Slider.StylePropertyFill, sliderFillWhite),
            })
        };
        styles.AddRange(newStyles);
        return new Stylesheet(styles);
    }
}
