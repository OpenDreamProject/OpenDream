using OpenDreamShared.Interface;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Elements {
    class ElementButton : Button, IElement {
        public ElementDescriptor ElementDescriptor {
            get => _elementDescriptor;
            set {
                _elementDescriptor = (ElementDescriptorButton)value;
                UpdateVisuals();
            }
        }

        private ElementDescriptorButton _elementDescriptor;

        private void UpdateVisuals() {
            this.Content = _elementDescriptor.Text;
        }
    }
}
