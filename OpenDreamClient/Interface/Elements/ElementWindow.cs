using OpenDreamShared.Interface;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Elements {
    class ElementWindow : Canvas, IElement {
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

        public void Shutdown() {
            foreach (IElement element in ChildElements) {
                element.Shutdown();
            }
        }
    }
}
