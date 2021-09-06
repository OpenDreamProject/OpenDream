using Content.Shared.Interface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Interface.Controls
{
    class ControlLabel : InterfaceControl {
        private Label _label;

        public ControlLabel(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

        protected override Control CreateUIElement() {
            _label = new Label() {
                HorizontalAlignment = Control.HAlignment.Stretch,
                VerticalAlignment = Control.VAlignment.Stretch,
            };

            return _label;
        }

        public override void UpdateElementDescriptor() {
            base.UpdateElementDescriptor();

            ControlDescriptorLabel controlDescriptor = (ControlDescriptorLabel)_elementDescriptor;
            _label.Text = controlDescriptor.Text;
        }
    }
}
