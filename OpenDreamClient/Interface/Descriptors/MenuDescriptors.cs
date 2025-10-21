using JetBrains.Annotations;
using OpenDreamClient.Interface.DMF;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamClient.Interface.Descriptors;

public sealed partial class MenuDescriptor : ElementDescriptor {
    private readonly List<MenuElementDescriptor> _elements = new();
    public IReadOnlyList<MenuElementDescriptor> Elements => _elements;

    public MenuDescriptor(string id) {
        Type = new DMFPropertyString("MENU");
        Id = new DMFPropertyString(id);
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

        copy._id = new DMFPropertyString(id);
        return copy;
    }
}

public sealed partial class MenuElementDescriptor : ElementDescriptor {
    [DataField("command")]
    public DMFPropertyString Command { get; private set; }

    [DataField("category")]
    public DMFPropertyString Category { get; private set; }

    [DataField("can-check")]
    public DMFPropertyBool CanCheck { get; private set; }

    [DataField("is-checked")]
    public DMFPropertyBool IsChecked { get; set; }

    [DataField("group")]
    public DMFPropertyString Group { get; private set; }

    [DataField("index")]
    public DMFPropertyNum Index { get; private set; }

    public MenuElementDescriptor WithCategory(ISerializationManager serialization, DMFPropertyString category) {
        var copy = serialization.CreateCopy(this, notNullableOverride: true);

        copy.Category = category;
        return copy;
    }

    // Menu elements can have other menu elements as children
    public override MenuElementDescriptor CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        return serializationManager.Read<MenuElementDescriptor>(attributes, notNullableOverride: true);
    }
}
