using OpenDreamShared.Interface;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Elements {
    class ElementButton : Button, IElement {
        public ElementDescriptor ElementDescriptor {
            get => _elementDescriptor;
            set {
                _elementDescriptor = (ElementDescriptorButton)value;
            }
        }

        private ElementDescriptorButton _elementDescriptor;

        public void UpdateVisuals() {
            this.Content = _elementDescriptor.Text;
        }
    }
}
