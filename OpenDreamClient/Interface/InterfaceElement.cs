using OpenDreamShared.Interface;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamClient.Interface
{
    public class InterfaceElement {
        public string Name { get => ElementDescriptor.Name; }

        protected readonly ElementDescriptor ElementDescriptor;

        public InterfaceElement(ElementDescriptor elementDescriptor) {
            ElementDescriptor = elementDescriptor;
        }

        public void PopulateElementDescriptor(MappingDataNode node, ISerializationManager serializationManager)
        {
            var result = (IDeserializedDefinition)serializationManager.Read(ElementDescriptor.GetType(), node);
            serializationManager.PopulateDataDefinition(ElementDescriptor, result);
            UpdateElementDescriptor();
        }

        public virtual void UpdateElementDescriptor() {

        }

        public virtual void Shutdown() {

        }
    }
}
