using OpenDreamShared.Interface;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenDreamClient.Interface.Elements {
    class ElementInfo : Border, IElement {
        public ElementDescriptor ElementDescriptor {
            get => _elementDescriptor;
            set {
                _elementDescriptor = (ElementDescriptorInfo)value;
                UpdateVisuals();
            }
        }

        private ElementDescriptorInfo _elementDescriptor;

        public ElementInfo() {
            this.BorderBrush = Brushes.Black;
            this.BorderThickness = new Thickness(1);
        }

        private void UpdateVisuals() {

        }
    }
}
