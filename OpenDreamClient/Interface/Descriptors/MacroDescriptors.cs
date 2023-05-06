using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamClient.Interface.Descriptors;

public sealed class MacroSetDescriptor : ElementDescriptor {
    private readonly List<MacroDescriptor> _macros = new();
    public IReadOnlyList<MacroDescriptor> Macros => _macros;

    public MacroSetDescriptor(string name) {
        Type = "MACRO_SET";
        Name = name;
    }

    [UsedImplicitly]
    public MacroSetDescriptor() {

    }

    public override MacroDescriptor CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        var macro = serializationManager.Read<MacroDescriptor>(attributes, notNullableOverride: true);

        _macros.Add(macro);
        return macro;
    }

    public override ElementDescriptor CreateCopy(ISerializationManager serializationManager, string name) {
        var copy = serializationManager.CreateCopy(this, notNullableOverride: true);

        copy._name = name;
        return copy;
    }
}

[UsedImplicitly]
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
