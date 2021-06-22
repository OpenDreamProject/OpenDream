using OpenDreamShared.Interface;
using System.Windows;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Controls {
    class ControlInput : InterfaceControl {
        public ControlInput(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) { }

        protected override FrameworkElement CreateUIElement() {
            return new TextBox();
        }
    }
}
