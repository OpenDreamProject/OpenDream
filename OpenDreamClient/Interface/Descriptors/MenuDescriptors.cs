using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamClient.Interface.Descriptors;

public sealed class MenuDescriptor : ElementDescriptor {
    private readonly List<MenuElementDescriptor> _elements = new();
    public IReadOnlyList<MenuElementDescriptor> Elements => _elements;

    public MenuDescriptor(string name) {
        Type = "MENU";
        Name = name;
    }

    [UsedImplicitly]
    public MenuDescriptor() {

    }

    public override MenuElementDescriptor CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        var menuElement = serializationManager.Read<MenuElementDescriptor>(attributes, notNullableOverride: true);

        _elements.Add(menuElement);
        return menuElement;
    }

    public override ElementDescriptor CreateCopy(ISerializationManager serializationManager, string name) {
        var copy = serializationManager.CreateCopy(this, notNullableOverride: true);

        copy._name = name;
        return copy;
    }
}

public sealed class MenuElementDescriptor : ElementDescriptor {
    private string _category;

    [DataField("command")]
    public string Command { get; init; }

    [DataField("category")]
    public string Category {
        get => _category;
        init => _category = value;
    }

    [DataField("can-check")]
    public bool CanCheck { get; init; }

    public MenuElementDescriptor WithCategory(ISerializationManager serialization, string category) {
        var copy = serialization.CreateCopy(this, notNullableOverride: true);

        copy._category = category;
        return copy;
    }

    // Menu elements can have other menu elements as children
    public override MenuElementDescriptor CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        return serializationManager.Read<MenuElementDescriptor>(attributes, notNullableOverride: true);
    }
}
