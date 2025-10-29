using System.Diagnostics.CodeAnalysis;
using OpenDreamClient.Interface.Controls;
using OpenDreamShared.Interface.Descriptors;
using OpenDreamShared.Interface.DMF;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamClient.Interface;

[Virtual]
public class InterfaceElement {
    public DMFPropertyString Type => ElementDescriptor.Type;
    public DMFPropertyString Id => ElementDescriptor.Id;

    public ElementDescriptor ElementDescriptor;

    [Dependency] protected readonly IDreamInterfaceManager _interfaceManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    protected InterfaceElement(ElementDescriptor elementDescriptor) {
        ElementDescriptor = elementDescriptor;
        IoCManager.InjectDependencies(this);
    }

    public void PopulateElementDescriptor(MappingDataNode node, ISerializationManager serializationManager) {
        if (GetType() == typeof(ControlChild)) {
            // CHILD's top/bottom attributes alias to left/right
            // Code is duplicated in WindowDescriptor.CreateChildDescriptor()
            // TODO: A bit hacky. Remove this (may be worth abandoning RT's serialization manager)
            if (node.TryGet("top", out var topValue))
                node["left"] = topValue;
            if (node.TryGet("bottom", out var bottomValue))
                node["right"] = bottomValue;
        }

        try {
            MappingDataNode original =
                (MappingDataNode)serializationManager.WriteValue(ElementDescriptor.GetType(), ElementDescriptor);
            foreach (var key in node.Keys) {
                original.Remove(key);
            }

            MappingDataNode newNode = original.Merge(node);

            var descriptor = serializationManager.Read(ElementDescriptor.GetType(), newNode);
            if(descriptor is null) throw new NullReferenceException(); // We're in a try/catch anyway, just play it safe
            ElementDescriptor = (ElementDescriptor)descriptor;
            UpdateElementDescriptor();
        } catch (Exception e) {
            Logger.GetSawmill("opendream.interface").Error($"Error while populating values of \"{Id}\": {e}");
        }
    }

    /// <summary>
    /// Attempt to get a DMF property
    /// You only need to create an override for this if the property can't be straight read from the ElementDescriptor
    /// </summary>
    public virtual bool TryGetProperty(string property, [NotNullWhen(true)] out IDMFProperty? value) {
        MappingDataNode original =
                (MappingDataNode)_serializationManager.WriteValue(ElementDescriptor.GetType(), ElementDescriptor, alwaysWrite: true); //alwayswrite because we want to access all properties, even defaults
        if (original.TryGet(property, out var node) && _serializationManager.TryGetVariableType(ElementDescriptor.GetType(), property, out var propertyDef)) {
            value = (IDMFProperty?) _serializationManager.Read(propertyDef, node);
            if(value is not null)
                return true;
        }

        value = null;
        return false;
    }

    // TODO: Replace PopulateElementDescriptor with this
    public virtual void SetProperty(string property, string value, bool manualWinset = false) {
        MappingDataNode node = new() {
            {property, value}
        };

        PopulateElementDescriptor(node, _serializationManager);
    }

    protected virtual void UpdateElementDescriptor() {
    }

    public virtual void AddChild(ElementDescriptor descriptor) {
        throw new InvalidOperationException($"{this} cannot add a child");
    }

    public virtual void Shutdown() {
    }
}
