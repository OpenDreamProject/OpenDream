using System.Linq;
using OpenDreamClient.Interface.DMF;
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

    public ElementDescriptor? GetElementDescriptor(string name) {
        return WindowDescriptors.Concat<ElementDescriptor>(MacroSetDescriptors)
            .Concat(MenuDescriptors).FirstOrDefault(descriptor => descriptor.Id.Value == name);
    }
}

[Virtual, ImplicitDataDefinitionForInheritors]
public partial class ElementDescriptor {
    [DataField("type")]
    public DMFPropertyString _type;

    [DataField("id")]
    protected DMFPropertyString _id;

    [DataField("name")]
    protected DMFPropertyString _name;

    public DMFPropertyString Id {
        get => string.IsNullOrEmpty(_id.Value) ? _id = new DMFPropertyString(Guid.NewGuid().ToString()) : _id; //ensure unique ID for all elements. Empty ID elements aren't addressible anyway.
        init => _id = value;
    }

    public DMFPropertyString Name => new(_name.Value);

    public DMFPropertyString Type {
        get => _type;
        protected init => _type = value;
    }

    public virtual ElementDescriptor? CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        throw new InvalidOperationException($"{this} cannot create a child descriptor");
    }

    public ElementDescriptor? CreateChildDescriptor(ISerializationManager serializationManager, Dictionary<string, string> attributes) {
        var node = new MappingDataNode();

        foreach (var pair in attributes) {
            node.Add(pair.Key, pair.Value);
        }

        return CreateChildDescriptor(serializationManager, node);
    }

    public virtual ElementDescriptor CreateCopy(ISerializationManager serializationManager, string id) {
        throw new InvalidOperationException($"{this} cannot create a copy of itself");
    }

    public override string ToString() {
        return $"{GetType().Name}(Type={Type},Name={Name})";
    }
}
