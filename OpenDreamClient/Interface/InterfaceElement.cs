using OpenDreamShared.Interface;

namespace OpenDreamClient.Interface
{
    class InterfaceElement {
        public string Name { get => _elementDescriptor.Name; }

        protected ElementDescriptor _elementDescriptor;

        public InterfaceElement(ElementDescriptor elementDescriptor) {
            _elementDescriptor = elementDescriptor;
        }

        public void SetAttribute(string name, object value) {
            //_elementDescriptor.SetAttribute(name, value);
            UpdateElementDescriptor();
        }

        public virtual void UpdateElementDescriptor() {

        }

        public virtual void Shutdown() {

        }
    }
}
