using OpenDreamShared.Interface;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamClient.Interface
{
    public class InterfaceElement {
        public string Name { get => ElementDescriptor.Name; }

        protected ElementDescriptor ElementDescriptor;

        public InterfaceElement(ElementDescriptor elementDescriptor) {
            ElementDescriptor = elementDescriptor;
        }

        public void PopulateElementDescriptor(MappingDataNode node, ISerializationManager serializationManager)
        {
            var result = (ElementDescriptor)serializationManager.Read(ElementDescriptor.GetType(), node);
            ElementDescriptor = serializationManager.Copy(result, ElementDescriptor);
            UpdateElementDescriptor();
        }

        public virtual void UpdateElementDescriptor() {

        }

        public virtual void Shutdown() {

        }
    }
}
