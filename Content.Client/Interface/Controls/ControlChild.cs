using System;
using Content.Shared.Interface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client.Interface.Controls {
    class ControlChild : InterfaceControl
    {
        // todo: robust needs GridSplitter.
        // and a non-shit grid control.

        [Dependency] private readonly DreamInterfaceManager _dreamInterface;

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

            ControlDescriptorChild controlDescriptor = (ControlDescriptorChild)_elementDescriptor;

            _grid.Children.Remove(_leftElement?.UIElement);
            _grid.Children.Remove(_rightElement?.UIElement);

            if (!String.IsNullOrEmpty(controlDescriptor.Left)) {
                _leftElement = _dreamInterface.Windows[controlDescriptor.Left];
                _grid.Children.Add(_leftElement.UIElement);
            } else {
                _leftElement = null;
            }

            if (!String.IsNullOrEmpty(controlDescriptor.Right)) {
                _rightElement = _dreamInterface.Windows[controlDescriptor.Right];
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
                ? SplitContainer.SplitOrientation.Vertical
                : SplitContainer.SplitOrientation.Horizontal;
        }
    }
}
