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

    public InterfaceDescriptor Copy(ISerializationManager serializationManager) {
        return new InterfaceDescriptor(serializationManager.CreateCopy(WindowDescriptors),
            serializationManager.CreateCopy(MacroSetDescriptors), serializationManager.CreateCopy(MenuDescriptors));
    }
}

[Virtual, ImplicitDataDefinitionForInheritors]
public class ElementDescriptor {
    [DataField("type")]
    public readonly string Type;

    [DataField("name")]
    private string _name;

    public string Name {
        get => _name;
        init => _name = value;
    }

    public virtual ElementDescriptor CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        throw new InvalidOperationException($"{this} cannot create a child descriptor");
    }

    public override string ToString() {
        return $"{GetType().Name}(Type={Type},Name={Name})";
    }

    public ElementDescriptor WithName(ISerializationManager serializationManager, string name) {
        var copy = serializationManager.CreateCopy(this);
        copy._name = name;
        return copy;
    }
}
