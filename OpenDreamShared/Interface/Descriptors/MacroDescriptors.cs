using System.Collections.Generic;
using JetBrains.Annotations;
using OpenDreamShared.Interface.DMF;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamShared.Interface.Descriptors;

public sealed partial class MacroSetDescriptor : ElementDescriptor {
    private readonly List<MacroDescriptor> _macros = new();
    public IReadOnlyList<MacroDescriptor> Macros => _macros;

    public MacroSetDescriptor(string id) {
        Type = new DMFPropertyString("MACRO_SET");
        Id = new DMFPropertyString(id);
    }

    [UsedImplicitly]
    public MacroSetDescriptor() {

    }

    public override MacroDescriptor CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        var macro = serializationManager.Read<MacroDescriptor>(attributes, notNullableOverride: true);

        _macros.Add(macro);
        return macro;
    }

    public override ElementDescriptor CreateCopy(ISerializationManager serializationManager, string id) {
        var copy = serializationManager.CreateCopy(this, notNullableOverride: true);

        copy._id = new DMFPropertyString(id);
        return copy;
    }
}

[UsedImplicitly]
public sealed partial class MacroDescriptor : ElementDescriptor {
    [DataField("command")]
    public string Command { get; private set; } = default!;
}
