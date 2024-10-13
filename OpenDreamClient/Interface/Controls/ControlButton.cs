using OpenDreamClient.Interface.Controls.UI;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlButton(ControlDescriptor controlDescriptor, ControlWindow window) : InterfaceControl(controlDescriptor, window) {
    [Dependency] private readonly IResourceCache _resCache = default!;
    public const string StyleClassDMFButton = "DMFbutton";

    private Button _button;

    protected override Control CreateUIElement() {
        _button = new Button() {
            ClipText = true
        };

        _button.OnPressed += OnButtonClick;
        _button.Label.Margin = new Thickness(0, -2, 0, 0);
        _button.Label.AddStyleClass(StyleClassDMFButton);

        return _button;
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();

        ControlDescriptorButton controlDescriptor = (ControlDescriptorButton)ElementDescriptor;

        var buttonTexturePath = controlDescriptor.IsChecked.Value
            ? "/Textures/Interface/ButtonPressed.png"
            : "/Textures/Interface/Button.png";

        _button.Text = controlDescriptor.Text.Value;
        _button.StyleBoxOverride = new StyleBoxColoredTexture {
            Texture = _resCache.GetResource<TextureResource>(buttonTexturePath),
            BackgroundColor = controlDescriptor.BackgroundColor.Value,
            PatchMarginTop = 2,
            PatchMarginBottom = 2,
            PatchMarginLeft = 2,
            PatchMarginRight = 2
        };
    }

    private void OnButtonClick(BaseButton.ButtonEventArgs args) {
        ControlDescriptorButton controlDescriptor = (ControlDescriptorButton)ElementDescriptor;

        if (controlDescriptor.ButtonType.Value == "pushbox") {
            controlDescriptor.IsChecked.Value = !controlDescriptor.IsChecked.Value;
            UpdateElementDescriptor();
        }

        if (!string.IsNullOrEmpty(controlDescriptor.Command.Value)) {
            _interfaceManager.RunCommand(controlDescriptor.Command.AsRaw());
        }
    }
}
