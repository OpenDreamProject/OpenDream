/*using OpenDreamShared.Interface;
using System.Windows;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Controls {
    class ControlButton : InterfaceControl {
        private Button _button;

        public ControlButton(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

        protected override FrameworkElement CreateUIElement() {
            _button = new Button();
            _button.Click += OnButtonClick;

            return _button;
        }

        public override void UpdateElementDescriptor() {
            base.UpdateElementDescriptor();

            ControlDescriptorButton controlDescriptor = (ControlDescriptorButton)_elementDescriptor;
            _button.Content = controlDescriptor.Text;
        }

        private void OnButtonClick(object sender, RoutedEventArgs e) {
            ControlDescriptorButton controlDescriptor = (ControlDescriptorButton)_elementDescriptor;

            if (controlDescriptor.Command != null) {
                Program.OpenDream.RunCommand(controlDescriptor.Command);
            }
        }
    }
}
*/
