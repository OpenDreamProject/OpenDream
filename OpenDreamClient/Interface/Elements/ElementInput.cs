using OpenDreamShared.Interface;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Elements {
    class ElementInput : TextBox, IElement {
        public ElementDescriptor ElementDescriptor {
            get => _elementDescriptor;
            set {
                _elementDescriptor = (ElementDescriptorInput)value;
            }
        }

        private ElementDescriptorInput _elementDescriptor;

        public void UpdateVisuals() {

        }
    }
}
