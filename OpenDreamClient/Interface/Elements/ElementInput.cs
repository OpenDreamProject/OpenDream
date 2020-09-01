using OpenDreamShared.Interface;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Elements {
    class ElementInput : TextBox, IElement {
        public InterfaceElementDescriptor ElementDescriptor {
            get => _elementDescriptor;
            set {
                _elementDescriptor = value;
                UpdateVisuals();
            }
        }

        private InterfaceElementDescriptor _elementDescriptor;

        public void UpdateVisuals() {
            
        }
    }
}
