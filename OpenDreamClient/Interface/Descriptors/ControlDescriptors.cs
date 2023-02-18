using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace OpenDreamClient.Interface.Descriptors;

[Virtual]
public class ControlDescriptor : ElementDescriptor {
    [DataField("pos")]
    public Vector2i? Pos { get; init; } = null;
    [DataField("size")]
    public Vector2i? Size { get; init; } = null;
    [DataField("anchor1")]
    public Vector2i? Anchor1 { get; init; } = null;
    [DataField("anchor2")]
    public Vector2i? Anchor2 { get; init; } = null;
    [DataField("background-color")]
    public Color? BackgroundColor { get; init; } = null;
    [DataField("is-visible")]
    private bool _isVisible = true;
    [DataField("is-default")]
    public bool IsDefault { get; init; } = false;
    [DataField("is-disabled")]
    public bool IsDisabled { get; init; } = false;

    public bool IsVisible {
        get => _isVisible;
        init => _isVisible = value;
    }

    public ControlDescriptor WithVisible(ISerializationManager serializationManager, bool isVisible) {
        var copy = serializationManager.CreateCopy(this);
        copy._isVisible = isVisible;
        return copy;
    }
}

public sealed class WindowDescriptor : ControlDescriptor {
    [DataField("is-pane")]
    public bool IsPane { get; init; } = false;
    [DataField("icon")]
    public string Icon { get; init; } = null;
    [DataField("menu")]
    public string Menu { get; init; } = null;
    [DataField("title")]
    public string Title { get; init; } = null;
    [DataField("macro")]
    public string Macro { get; init; } = null;

    public readonly List<ControlDescriptor> ControlDescriptors;

    public WindowDescriptor(string name, List<ControlDescriptor> controlDescriptors = null) {
        ControlDescriptors = controlDescriptors ?? new();
        Name = name;
    }

    public WindowDescriptor() {

    }

    public override ControlDescriptor CreateChildDescriptor(ISerializationManager serializationManager, MappingDataNode attributes) {
        if (!attributes.TryGet("type", out var elementType) || elementType is not ValueDataNode elementTypeValue)
            return null;

        if (elementTypeValue.Value == "MAIN") {
            attributes.Remove("name");
            attributes["name"] = new ValueDataNode(Name);

            // Read the attributes into this descriptor
            serializationManager.Read(attributes, instanceProvider: () => this);
            return this;
        }


        Type descriptorType = elementTypeValue.Value switch {
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

        ControlDescriptor child = (ControlDescriptor) serializationManager.Read(descriptorType, attributes);
        ControlDescriptors.Add(child);
        return child;
    }
}

public sealed class ControlDescriptorChild : ControlDescriptor {
    [DataField("left")]
    public string Left { get; init; } = null;
    [DataField("right")]
    public string Right { get; init; } = null;
    [DataField("is-vert")]
    public bool IsVert { get; init; } = false;
}

public sealed class ControlDescriptorInput : ControlDescriptor {
}

public sealed class ControlDescriptorButton : ControlDescriptor {
    [DataField("text")]
    public string Text { get; init; } = null;
    [DataField("command")]
    public string Command { get; init; } = null;
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
    public string Text { get; init; } = null;
}

public sealed class ControlDescriptorGrid : ControlDescriptor {
}

public sealed class ControlDescriptorTab : ControlDescriptor {
}
