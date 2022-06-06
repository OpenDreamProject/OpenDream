using OpenDreamShared.Interface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace OpenDreamClient.Interface.Controls {
    sealed class ControlChild : InterfaceControl
    {
        // todo: robust needs GridSplitter.
        // and a non-shit grid control.

        [Dependency] private readonly IDreamInterfaceManager _dreamInterface = default!;

        private SplitContainer _grid;
        private ControlWindow _leftElement, _rightElement;

        public ControlChild(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window)
        {
        }

        protected override Control CreateUIElement() {
            _grid = new SplitContainer();

            return _grid;
        }

        public override void UpdateElementDescriptor() {
            base.UpdateElementDescriptor();

            ControlDescriptorChild controlDescriptor = (ControlDescriptorChild)ElementDescriptor;

            _grid.Children.Remove(_leftElement?.UIElement);
            _grid.Children.Remove(_rightElement?.UIElement);

            if (!String.IsNullOrEmpty(controlDescriptor.Left)) {
                _leftElement = _dreamInterface.Windows[controlDescriptor.Left];
                _leftElement.UIElement.HorizontalExpand = true;
                _leftElement.UIElement.VerticalExpand = true;
                _grid.Children.Add(_leftElement.UIElement);
            } else {
                _leftElement = null;
            }

            if (!String.IsNullOrEmpty(controlDescriptor.Right)) {
                _rightElement = _dreamInterface.Windows[controlDescriptor.Right];
                _rightElement.UIElement.HorizontalExpand = true;
                _rightElement.UIElement.VerticalExpand = true;
                _grid.Children.Add(_rightElement.UIElement);
            } else {
                _rightElement = null;
            }

            UpdateGrid(controlDescriptor.IsVert);
        }

        public override void Shutdown() {
            _leftElement?.Shutdown();
            _rightElement?.Shutdown();
        }

        private void UpdateGrid(bool isVert) {
            _grid.Orientation = isVert
                ? SplitContainer.SplitOrientation.Horizontal
                : SplitContainer.SplitOrientation.Vertical;
        }
    }
}
