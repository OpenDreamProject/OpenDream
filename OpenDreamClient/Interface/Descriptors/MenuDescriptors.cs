using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamClient.Interface.Descriptors;

public sealed partial class MenuDescriptor : ElementDescriptor {
    private readonly List<MenuElementDescriptor> _elements = new();
    public IReadOnlyList<MenuElementDescriptor> Elements => _elements;

    public MenuDescriptor(string id) {
        Type = "MENU";
        Id = id;
    }

    [UsedImplicitly]
    public MenuDescriptor() {

    }

    public override MenuElementDescriptor CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        var menuElement = serializationManager.Read<MenuElementDescriptor>(attributes, notNullableOverride: true);

        _elements.Add(menuElement);
        return menuElement;
    }

    public override ElementDescriptor CreateCopy(ISerializationManager serializationManager, string id) {
        var copy = serializationManager.CreateCopy(this, notNullableOverride: true);

        copy._id = id;
        return copy;
    }
}

public sealed partial class MenuElementDescriptor : ElementDescriptor {
    private string? _category;

    [DataField("command")]
    public string Command { get; private set; }

    [DataField("category")]
    public string? Category {
        get => _category;
        private set { _category = value; }
    }

    [DataField("can-check")]
    public bool CanCheck { get; private set; }

    [DataField("is-checked")]
    public bool IsChecked { get; set; }

    [DataField("group")]
    public string? Group { get; private set; }
    [DataField("index")]
    public int Index { get; private set; }

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
