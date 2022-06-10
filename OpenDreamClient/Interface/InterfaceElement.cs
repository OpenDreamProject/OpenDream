using OpenDreamShared.Interface;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamClient.Interface
{
    [Virtual]
    public class InterfaceElement {
        public string Name { get => ElementDescriptor.Name; }

        protected ElementDescriptor ElementDescriptor;

        public InterfaceElement(ElementDescriptor elementDescriptor) {
            ElementDescriptor = elementDescriptor;
        }

        public void PopulateElementDescriptor(MappingDataNode node, ISerializationManager serializationManager)
        {
            MappingDataNode original = (MappingDataNode)serializationManager.WriteValue(ElementDescriptor.GetType(), ElementDescriptor);
            foreach (var key in node.Keys) {
                original.Remove(key);
            }

            MappingDataNode newNode = original.Merge(node);
            ElementDescriptor = (ElementDescriptor)serializationManager.Read(ElementDescriptor.GetType(), newNode);
            UpdateElementDescriptor();
        }

        public virtual void UpdateElementDescriptor() {

        }

        public virtual void Shutdown() {

        }
    }
}
