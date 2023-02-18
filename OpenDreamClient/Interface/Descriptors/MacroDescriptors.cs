using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamClient.Interface.Descriptors;

public sealed class MacroSetDescriptor : ElementDescriptor {
    private readonly List<MacroDescriptor> _macros = new();
    public IReadOnlyList<MacroDescriptor> Macros => _macros;

    public MacroSetDescriptor(string name) {
        Name = name;
    }

    public override MacroDescriptor CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        var macro = serializationManager.Read<MacroDescriptor>(attributes);

        _macros.Add(macro);
        return macro;
    }
}

public sealed class MacroDescriptor : ElementDescriptor {
    public string Id {
        get => _id ?? Command;
        init => _id = value;
    }

    [DataField("id")]
    private readonly string _id;

    [DataField("command")]
    public string Command  { get; init; }
}
