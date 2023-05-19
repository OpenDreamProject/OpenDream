using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace OpenDreamClient.Interface.Descriptors;

[Virtual]
public class ControlDescriptor : ElementDescriptor {
    [DataField("pos")]
    public Vector2i? Pos;
    [DataField("size")]
    public Vector2i? Size;
    [DataField("anchor1")]
    public Vector2i? Anchor1;
    [DataField("anchor2")]
    public Vector2i? Anchor2;
    [DataField("background-color")]
    public Color? BackgroundColor;
    [DataField("is-visible")]
    public bool IsVisible = true;
    [DataField("is-default")]
    public bool IsDefault;
    [DataField("is-disabled")]
    public bool IsDisabled;
}

public sealed class WindowDescriptor : ControlDescriptor {
    [DataField("is-pane")]
    public bool IsPane;
    [DataField("icon")]
    public string? Icon;
    [DataField("menu")]
    public string? Menu;
    [DataField("title")]
    public string? Title;
    [DataField("macro")]
    public string? Macro { get; init; }
    [DataField("on-close")]
    public string? OnClose { get; init; }

    public readonly List<ControlDescriptor> ControlDescriptors;

    public WindowDescriptor(string name, List<ControlDescriptor>? controlDescriptors = null) {
        ControlDescriptors = controlDescriptors ?? new();
        Name = name;
    }

    [UsedImplicitly]
    public WindowDescriptor() {
        ControlDescriptors = new();
    }

    public override ControlDescriptor? CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        if (!attributes.TryGet("type", out var elementType) || elementType is not ValueDataNode elementTypeValue)
            return null;

        if (elementTypeValue.Value == "MAIN") {
            attributes.Remove("name");
            attributes["name"] = new ValueDataNode(Name);

            // Read the attributes into this descriptor
            serializationManager.Read(attributes, notNullableOverride: true, instanceProvider: () => this);
            return this;
        }


        Type? descriptorType = elementTypeValue.Value switch {
            "MAP" => typeof(ControlDescriptorMap),
            "CHILD" => typeof(ControlDescriptorChild),
            "OUTPUT" => typeof(ControlDescriptorOutput),
            "INFO" => typeof(ControlDescriptorInfo),
            "INPUT" => typeof(ControlDescriptorInput),
            "BUTTON" => typeof(ControlDescriptorButton),
            "BROWSER" => typeof(ControlDescriptorBrowser),
            "LABEL" => typeof(ControlDescriptorLabel),
            "GRID" => typeof(ControlDescriptorGrid),
            "TAB" => typeof(ControlDescriptorTab),
            _ => null
        };

        if (descriptorType == null)
            return null;

        var child = (ControlDescriptor?) serializationManager.Read(descriptorType, attributes);
        if (child == null)
            return null;

        ControlDescriptors.Add(child);
        return child;
    }

    public override ElementDescriptor CreateCopy(ISerializationManager serializationManager, string name) {
        var copy = serializationManager.CreateCopy(this, notNullableOverride: true);

        copy._name = name;
        return copy;
    }

    public WindowDescriptor WithVisible(ISerializationManager serializationManager, bool visible) {
        WindowDescriptor copy = (WindowDescriptor)CreateCopy(serializationManager, Name);

        copy.IsVisible = visible;
        return copy;
    }
}

public sealed class ControlDescriptorChild : ControlDescriptor {
    [DataField("left")]
    public string? Left;
    [DataField("right")]
    public string? Right;
    [DataField("is-vert")]
    public bool IsVert;
}

public sealed class ControlDescriptorInput : ControlDescriptor {
}

public sealed class ControlDescriptorButton : ControlDescriptor {
    [DataField("text")]
    public string? Text;
    [DataField("command")]
    public string? Command;
}

public sealed class ControlDescriptorOutput : ControlDescriptor {
}

public sealed class ControlDescriptorInfo : ControlDescriptor {
}

public sealed class ControlDescriptorMap : ControlDescriptor {
}

public sealed class ControlDescriptorBrowser : ControlDescriptor {
}

public sealed class ControlDescriptorLabel : ControlDescriptor {
    [DataField("text")]
    public string? Text;
}

public sealed class ControlDescriptorGrid : ControlDescriptor {
}

public sealed class ControlDescriptorTab : ControlDescriptor {
}
