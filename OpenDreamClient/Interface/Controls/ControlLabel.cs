using OpenDreamShared.Interface;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenDreamClient.Interface.Controls {
    class ControlLabel : InterfaceControl {
        private Label _label;

        public ControlLabel(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

        protected override FrameworkElement CreateUIElement() {
            _label = new Label() {
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Courier New")
            };

            return _label;
        }

        public override void UpdateElementDescriptor() {
            base.UpdateElementDescriptor();

            ControlDescriptorLabel controlDescriptor = (ControlDescriptorLabel)_elementDescriptor;
            _label.Content = controlDescriptor.Text;
        }
    }
}
