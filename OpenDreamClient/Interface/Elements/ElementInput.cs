using OpenDreamShared.Interface;
using System.Windows;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Elements {
    class ElementInput : InterfaceElement {
        public ElementInput(WindowElementDescriptor elementDescriptor, ElementWindow window) : base(elementDescriptor, window) { }

        protected override FrameworkElement CreateUIElement() {
            return new TextBox();
        }
    }
}
