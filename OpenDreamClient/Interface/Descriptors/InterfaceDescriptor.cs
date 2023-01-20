using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamClient.Interface.Descriptors;

public sealed class InterfaceDescriptor {
    public readonly List<WindowDescriptor> WindowDescriptors;
    public readonly List<MacroSetDescriptor> MacroSetDescriptors;
    public readonly List<MenuDescriptor> MenuDescriptors;

    public InterfaceDescriptor(List<WindowDescriptor> windowDescriptors, List<MacroSetDescriptor> macroSetDescriptors, List<MenuDescriptor> menuDescriptors) {
        WindowDescriptors = windowDescriptors;
        MacroSetDescriptors = macroSetDescriptors;
        MenuDescriptors = menuDescriptors;
    }
}

[Virtual, ImplicitDataDefinitionForInheritors]
public class ElementDescriptor {
    [DataField("type")]
    public string Type;
    [DataField("name")]
    public string Name;

    public virtual ElementDescriptor CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        throw new InvalidOperationException($"{this} cannot create a child descriptor");
    }

    public override string ToString() {
        return $"{GetType().Name}(Type={Type},Name={Name})";
    }

    public ElementDescriptor WithName(ISerializationManager serializationManager, string name) {
        var copy = serializationManager.CreateCopy(this);
        copy.Name = name;
        return copy;
    }
}
