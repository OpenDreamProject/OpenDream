using OpenDreamShared.Interface;

namespace OpenDreamClient.Interface.Elements {
    class ElementWindow : System.Windows.Controls.Canvas, IElement {
        public InterfaceElementDescriptor ElementDescriptor {
            get => _elementDescriptor;
            set {
                _elementDescriptor = value;
                UpdateVisuals();
            }
        }

        private InterfaceElementDescriptor _elementDescriptor;

        public ElementWindow() {
            
        }

        private void UpdateVisuals() {
            
        }
    }
}
