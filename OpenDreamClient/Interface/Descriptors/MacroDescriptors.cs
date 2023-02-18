using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamClient.Interface.Descriptors;

public sealed class MacroSetDescriptor : ElementDescriptor {
    public readonly List<MacroDescriptor> Macros = new();

    public MacroSetDescriptor(string name) {
        Name = name;
    }

    public override MacroDescriptor CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        var macro = serializationManager.Read<MacroDescriptor>(attributes);

        Macros.Add(macro);
        return macro;
    }
}

public sealed class MacroDescriptor : ElementDescriptor {
    public string Id {
        get => _id ?? Command;
        set => _id = value;
    }

    [DataField("id")]
    private string _id;

    [DataField("command")]
    public string Command;
}
