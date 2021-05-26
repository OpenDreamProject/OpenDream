using OpenDreamShared.Interface;

namespace OpenDreamClient.Interface.Elements {
    class ElementWindow : System.Windows.Controls.Canvas, IElement {
        public IElement[] ChildElements;
        public ElementDescriptor ElementDescriptor {
            get => _elementDescriptor;
            set {
                _elementDescriptor = (ElementDescriptorMain)value;
            }
        }

        private ElementDescriptorMain _elementDescriptor;

        public ElementWindow() {

        }

        public void UpdateVisuals() {
            foreach (IElement childElement in ChildElements) {
                childElement.UpdateVisuals();
            }
        }
    }
}
