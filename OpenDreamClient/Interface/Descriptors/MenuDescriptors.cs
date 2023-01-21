using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamClient.Interface.Descriptors;

public sealed class MenuDescriptor : ElementDescriptor {
    public readonly List<MenuElementDescriptor> Elements = new();

    public MenuDescriptor(string name) {
        Name = name;
    }

    public override MenuElementDescriptor CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        var menuElement = serializationManager.Read<MenuElementDescriptor>(attributes);

        Elements.Add(menuElement);
        return menuElement;
    }
}

public sealed class MenuElementDescriptor : ElementDescriptor {
    [DataField("command")]
    public string Command;
    [DataField("category")]
    public string Category;
    [DataField("can-check")]
    public bool CanCheck;

    // Menu elements can have other menu elements as children
    public override MenuElementDescriptor CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        return serializationManager.Read<MenuElementDescriptor>(attributes);
    }
}
