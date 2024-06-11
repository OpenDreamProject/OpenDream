using System.Diagnostics.CodeAnalysis;
using OpenDreamClient.Interface.Controls.UI;
using OpenDreamClient.Interface.Descriptors;
using OpenDreamClient.Interface.DMF;
using Robust.Client.UserInterface;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlChild(ControlDescriptor controlDescriptor, ControlWindow window) : InterfaceControl(controlDescriptor, window) {
    private ControlDescriptorChild ChildDescriptor => (ControlDescriptorChild)ElementDescriptor;

    private Splitter _splitter;

    protected override Control CreateUIElement() {
        _splitter = new Splitter();

        return _splitter;
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();

        var newLeftElement = ChildDescriptor.Left.Value != null && _interfaceManager.Windows.TryGetValue(ChildDescriptor.Left.Value, out var leftWindow)
            ? leftWindow.UIElement
            : null;
        var newRightElement = ChildDescriptor.Right.Value != null && _interfaceManager.Windows.TryGetValue(ChildDescriptor.Right.Value, out var rightWindow)
            ? rightWindow.UIElement
            : null;

        _splitter.Left = newLeftElement;
        _splitter.Right = newRightElement;
        _splitter.Vertical = ChildDescriptor.IsVert.Value;
        _splitter.SplitterPercentage = ChildDescriptor.Splitter.Value / 100f;
    }

    public override void Shutdown() {
        if (ChildDescriptor.Left.Value != null && _interfaceManager.Windows.TryGetValue(ChildDescriptor.Left.Value, out var left))
            left.Shutdown();
        if (ChildDescriptor.Right.Value != null && _interfaceManager.Windows.TryGetValue(ChildDescriptor.Right.Value, out var right))
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
