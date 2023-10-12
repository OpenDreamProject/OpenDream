using OpenDreamClient.Interface.Descriptors;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls;

internal sealed class ControlChild : InterfaceControl {
    // todo: robust needs GridSplitter.
    // and a non-shit grid control.

    [Dependency] private readonly IDreamInterfaceManager _dreamInterface = default!;

    private ControlDescriptorChild ChildDescriptor => (ControlDescriptorChild)ElementDescriptor;

    private SplitContainer _grid;
    private Control? _leftElement, _rightElement;

    public ControlChild(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

    protected override Control CreateUIElement() {
        _grid = new SplitContainer();

        return _grid;
    }

    protected override void UpdateElementDescriptor() {
        base.UpdateElementDescriptor();

        var newLeftElement = ChildDescriptor.Left != null && _dreamInterface.Windows.TryGetValue(ChildDescriptor.Left, out var leftWindow)
            ? leftWindow.UIElement
            : null;
        var newRightElement = ChildDescriptor.Right != null && _dreamInterface.Windows.TryGetValue(ChildDescriptor.Right, out var rightWindow)
            ? rightWindow.UIElement
            : null;

        if (newLeftElement != _leftElement || _grid.ChildCount == 0) {
            if (_leftElement != null)
                _grid.Children.Remove(_leftElement);

            if (newLeftElement != null) {
                _leftElement = newLeftElement;
                _leftElement.HorizontalExpand = true;
                _leftElement.VerticalExpand = true;
            } else {
                // SplitContainer will have a size of 0x0 if there aren't 2 controls
                _leftElement = new Control();
            }

            _grid.Children.Add(_leftElement);
        }

        if (newRightElement != _rightElement || _grid.ChildCount == 1) {
            if (_rightElement != null)
                _grid.Children.Remove(_rightElement);

            if (newRightElement != null) {
                _rightElement = newRightElement;
                _rightElement.HorizontalExpand = true;
                _rightElement.VerticalExpand = true;
            } else {
                // SplitContainer will have a size of 0x0 if there aren't 2 controls
                _rightElement = new Control();
            }

            _grid.Children.Add(_rightElement);
        }

        if(_leftElement is not null)
            _leftElement.SetPositionInParent(0);
        if (_rightElement is not null)
            _rightElement.SetPositionInParent(1);

        UpdateGrid();
    }

    public override void Shutdown() {
        if (ChildDescriptor.Left != null && _dreamInterface.Windows.TryGetValue(ChildDescriptor.Left, out var left))
            left.Shutdown();
        if (ChildDescriptor.Right != null && _dreamInterface.Windows.TryGetValue(ChildDescriptor.Right, out var right))
            right.Shutdown();
    }

    private void UpdateGrid() {
        _grid.Orientation = ChildDescriptor.IsVert
            ? SplitContainer.SplitOrientation.Horizontal
            : SplitContainer.SplitOrientation.Vertical;

        if (_grid.Size == Vector2.Zero)
            return;

        _grid.SplitFraction = ChildDescriptor.Splitter / 100f;
    }
}
