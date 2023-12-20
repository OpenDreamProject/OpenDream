using OpenDreamClient.Interface.Descriptors;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamClient.Interface;

[Virtual]
public class InterfaceElement {
    public string Type => ElementDescriptor.Type;
    public string Id => ElementDescriptor.Id;

    public ElementDescriptor ElementDescriptor;
    [Dependency] protected readonly IDreamInterfaceManager _interfaceManager = default!;
    protected InterfaceElement(ElementDescriptor elementDescriptor) {
        ElementDescriptor = elementDescriptor;
        IoCManager.InjectDependencies(this);
    }

    public void PopulateElementDescriptor(MappingDataNode node, ISerializationManager serializationManager) {
        try {
            MappingDataNode original =
                (MappingDataNode)serializationManager.WriteValue(ElementDescriptor.GetType(), ElementDescriptor);
            foreach (var key in node.Keys) {
                original.Remove(key);
            }

            MappingDataNode newNode = original.Merge(node);
            ElementDescriptor = (ElementDescriptor)serializationManager.Read(ElementDescriptor.GetType(), newNode);
            UpdateElementDescriptor();
        } catch (Exception e) {
            Logger.GetSawmill("opendream.interface").Error($"Error while populating values of \"{Id}\": {e}");
        }
    }

    /// <summary>
    /// Attempt to get a DMF property
    /// </summary>
    public virtual bool TryGetProperty(string property, out string value) {
        value = string.Empty;
        return false;
    }

    protected virtual void UpdateElementDescriptor() {

    }

    public virtual void AddChild(ElementDescriptor descriptor) {
        throw new InvalidOperationException($"{this} cannot add a child");
    }

    public virtual void Shutdown() {

    }
}
