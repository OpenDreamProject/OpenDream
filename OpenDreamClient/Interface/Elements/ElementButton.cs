using OpenDreamShared.Interface;
using System.Windows.Controls;

namespace OpenDreamClient.Interface.Elements {
    class ElementButton : Button, IElement {
        public InterfaceElementDescriptor ElementDescriptor {
            get => _elementDescriptor;
            set {
                _elementDescriptor = value;
                UpdateVisuals();
            }
        }

        private InterfaceElementDescriptor _elementDescriptor;

        private void UpdateVisuals() {
            if (_elementDescriptor.StringAttributes.ContainsKey("text")) {
                this.Content = _elementDescriptor.StringAttributes["text"];
            }
        }
    }
}
