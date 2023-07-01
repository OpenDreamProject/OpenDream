using OpenDreamClient.Interface.Descriptors;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamClient.Interface;

[Virtual]
public class InterfaceElement {
    public string Type => ElementDescriptor.Type;
    public string Id => ElementDescriptor.Id;

    public ElementDescriptor ElementDescriptor;

    protected InterfaceElement(ElementDescriptor elementDescriptor) {
        ElementDescriptor = elementDescriptor;
    }

    public void PopulateElementDescriptor(MappingDataNode node, ISerializationManager serializationManager) {
        MappingDataNode original = (MappingDataNode)serializationManager.WriteValue(ElementDescriptor.GetType(), ElementDescriptor);
        foreach (var key in node.Keys) {
            original.Remove(key);
        }

        MappingDataNode newNode = original.Merge(node);
        ElementDescriptor = (ElementDescriptor)serializationManager.Read(ElementDescriptor.GetType(), newNode);
        UpdateElementDescriptor();
    }

    protected virtual void UpdateElementDescriptor() {

    }

    public virtual void AddChild(ElementDescriptor descriptor) {
        throw new InvalidOperationException($"{this} cannot add a child");
    }

    public virtual void Shutdown() {

    }
}
