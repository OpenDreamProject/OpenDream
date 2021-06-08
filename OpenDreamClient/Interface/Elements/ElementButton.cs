using OpenDreamShared.Interface;
using System.Windows;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Elements {
    class ElementButton : InterfaceElement {
        private Button _button;

        public ElementButton(ElementDescriptor elementDescriptor, ElementWindow window) : base(elementDescriptor, window) { }

        protected override FrameworkElement CreateUIElement() {
            _button = new Button();

            return _button;
        }

        public override void UpdateElementDescriptor() {
            base.UpdateElementDescriptor();

            ElementDescriptorButton elementDescriptor = (ElementDescriptorButton)_elementDescriptor;
            _button.Content = elementDescriptor.Text;
        }
    }
}
