using OpenDreamClient.Interface.Controls.UI;
using OpenDreamClient.Interface.Descriptors;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlButton(ControlDescriptor controlDescriptor, ControlWindow window) : InterfaceControl(controlDescriptor, window) {
    public const string StyleClassDMFButton = "DMFbutton";

    private Button _button;

    protected override Control CreateUIElement() {
        _button = new Button() {
            ClipText = true
        };

        _button.OnPressed += OnButtonClick;
        _button.Label.Margin = new Thickness(0, -4, 0, 0);
        _button.Label.AddStyleClass(StyleClassDMFButton);

        return _button;
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();

        ControlDescriptorButton controlDescriptor = (ControlDescriptorButton)ElementDescriptor;

        _button.Text = controlDescriptor.Text.Value;
        _button.StyleBoxOverride = new StyleBoxColoredTexture {
            Texture = IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>("/Textures/Interface/Button.png"),
            BackgroundColor = controlDescriptor.BackgroundColor.Value,
            PatchMarginTop = 2,
            PatchMarginBottom = 2,
            PatchMarginLeft = 2,
            PatchMarginRight = 2
        };
    }

    private void OnButtonClick(BaseButton.ButtonEventArgs args) {
        ControlDescriptorButton controlDescriptor = (ControlDescriptorButton)ElementDescriptor;

        if (!string.IsNullOrEmpty(controlDescriptor.Command.Value)) {
            _interfaceManager.RunCommand(controlDescriptor.Command.AsRaw());
        }
    }
}
