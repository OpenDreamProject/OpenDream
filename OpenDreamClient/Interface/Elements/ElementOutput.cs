using OpenDreamShared.Interface;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenDreamClient.Interface.Elements {
    class ElementOutput : Border, IElement {
        public TextBox TextBox = new TextBox();
        public ElementDescriptor ElementDescriptor {
            get => _elementDescriptor;
            set {
                _elementDescriptor = (ElementDescriptorOutput)value;
                UpdateVisuals();
            }
        }

        private ElementDescriptorOutput _elementDescriptor;

        public ElementOutput() {
            this.BorderBrush = Brushes.Black;
            this.BorderThickness = new System.Windows.Thickness(1);

            this.TextBox.IsReadOnly = true;
            this.Child = TextBox;
            Program.OpenDream.GameWindow.DefaultOutput = this;
        }

        private void UpdateVisuals() {

        }
    }
}
