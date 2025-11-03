using System.Diagnostics.CodeAnalysis;
using OpenDreamClient.Interface.Controls.UI;
using OpenDreamShared.Interface.Descriptors;
using OpenDreamShared.Interface.DMF;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlChild(ControlDescriptor controlDescriptor, ControlWindow window) : InterfaceControl(controlDescriptor, window) {
    private ControlDescriptorChild ChildDescriptor => (ControlDescriptorChild)ElementDescriptor;

    private Splitter _splitter = default!;

    protected override Control CreateUIElement() {
        _splitter = new Splitter();

        return _splitter;
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();

        var newLeftElement = _interfaceManager.Windows.TryGetValue(ChildDescriptor.Left.Value, out var leftWindow)
            ? leftWindow.UIElement
            : null;
        var newRightElement = _interfaceManager.Windows.TryGetValue(ChildDescriptor.Right.Value, out var rightWindow)
            ? rightWindow.UIElement
            : null;

        _splitter.Left = newLeftElement;
        _splitter.Right = newRightElement;
        _splitter.Vertical = ChildDescriptor.IsVert.Value;
        _splitter.SplitterPercentage = ChildDescriptor.Splitter.Value / 100f;
        _splitter.DragStyleBoxOverride = new StyleBoxColoredTexture {
            BackgroundColor = (ChildDescriptor.BackgroundColor.Value != Color.Transparent)
                ? ChildDescriptor.BackgroundColor.Value
                : DreamStylesheet.DefaultBackgroundColor,
            Texture = IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>("/Textures/Interface/SplitterBorder.png"),
            PatchMarginTop = 1,
            PatchMarginLeft = 1,
            PatchMarginRight = 1,
            PatchMarginBottom = 1
        };
    }

    public override void Shutdown() {
        if (_interfaceManager.Windows.TryGetValue(ChildDescriptor.Left.Value, out var left))
            left.Shutdown();
        if (_interfaceManager.Windows.TryGetValue(ChildDescriptor.Right.Value, out var right))
            right.Shutdown();
    }

    public override bool TryGetProperty(string property, [NotNullWhen(true)] out IDMFProperty? value) {
        switch (property) {
            case "splitter":
                value = new DMFPropertyNum(_splitter.SplitterPercentage * 100);
                return true;
            default:
                return base.TryGetProperty(property, out value);
        }
    }
}
