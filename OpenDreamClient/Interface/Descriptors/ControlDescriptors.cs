using JetBrains.Annotations;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace OpenDreamClient.Interface.Descriptors;

[Virtual]
public partial class ControlDescriptor : ElementDescriptor {
    [DataField("pos")]
    public Vector2i? Pos;
    [DataField("size")]
    public Vector2i? Size;
    [DataField("anchor1")]
    public Vector2i? Anchor1;
    [DataField("anchor2")]
    public Vector2i? Anchor2;
    [DataField("background-color", customTypeSerializer: typeof(DMFColorSerializer))]
    public Color? BackgroundColor;
    [DataField("is-visible")]
    public bool IsVisible = true;
    [DataField("is-default")]
    public bool IsDefault;
    [DataField("is-disabled")]
    public bool IsDisabled;
}

public sealed partial class WindowDescriptor : ControlDescriptor {
    [DataField("is-pane")]
    public bool IsPane;
    [DataField("icon")]
    public string? Icon;
    [DataField("menu")]
    public string? Menu;
    [DataField("title")]
    public string? Title;
    [DataField("macro")]
    public string? Macro { get; private set; }
    [DataField("on-close")]
    public string? OnClose { get; private set; }

    public readonly List<ControlDescriptor> ControlDescriptors;

    public WindowDescriptor(string id, List<ControlDescriptor>? controlDescriptors = null) {
        ControlDescriptors = controlDescriptors ?? new();
        Id = id;
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
            "BAR" => typeof(ControlDescriptorBar),
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

    public override ElementDescriptor CreateCopy(ISerializationManager serializationManager, string id) {
        var copy = serializationManager.CreateCopy(this, notNullableOverride: true);

        copy._id = id;
        foreach(var child in this.ControlDescriptors)
            copy.ControlDescriptors.Add(serializationManager.CreateCopy(child, notNullableOverride: false));
        return copy;
    }

    public WindowDescriptor WithVisible(ISerializationManager serializationManager, bool visible) {
        WindowDescriptor copy = (WindowDescriptor)CreateCopy(serializationManager, Id);

        copy.IsVisible = visible;
        return copy;
    }
}

public sealed partial class ControlDescriptorChild : ControlDescriptor {
    [DataField("left")]
    public string? Left;
    [DataField("right")]
    public string? Right;
    [DataField("is-vert")]
    public bool IsVert;
    [DataField("splitter")]
    public float Splitter = 50f;
}

public sealed partial class ControlDescriptorInput : ControlDescriptor {
}

public sealed partial class ControlDescriptorButton : ControlDescriptor {
    [DataField("text")]
    public string? Text;
    [DataField("command")]
    public string? Command;
}

public sealed partial class ControlDescriptorOutput : ControlDescriptor {
}

public sealed partial class ControlDescriptorInfo : ControlDescriptor {
    [DataField("on-show")]
    public string? OnShowCommand;
    [DataField("on-hide")]
    public string? OnHideCommand;
    [DataField("allow-html")]
    public bool AllowHtml = true; // Supposedly false by default, but it isn't if you're not using BYOND's default skin
}

public sealed partial class ControlDescriptorMap : ControlDescriptor {
    [DataField("on-show")]
    public string? OnShowCommand;
    [DataField("on-hide")]
    public string? OnHideCommand;
    [DataField("zoom-mode")]
    public string ZoomMode = "normal";
}

public sealed partial class ControlDescriptorBrowser : ControlDescriptor {
    [DataField("on-show")]
    public string? OnShowCommand;
    [DataField("on-hide")]
    public string? OnHideCommand;
}

public sealed partial class ControlDescriptorLabel : ControlDescriptor {
    [DataField("text")]
    public string? Text;
}

public sealed partial class ControlDescriptorGrid : ControlDescriptor {
}

public sealed partial class ControlDescriptorTab : ControlDescriptor {
    [DataField("tabs")]
    public string? Tabs;
    [DataField("current-tab")]
    public string? CurrentTab;
}


public sealed partial class ControlDescriptorBar : ControlDescriptor {
    [DataField("width")]
    public int? Width = 10; //width of the progress bar in pixels. In the default EAST dir, this is more accurately thought of as "height"
    [DataField("dir")]
    public string? Dir = "east"; //valid values: north/east/south/west/clockwise/cw/counterclockwise/ccw
    [DataField("angle1")]
    public int? Angle1 = 0; //start angle
    [DataField("angle2")]
    public int? Angle2 = 180; //end angle
    [DataField("bar-color")]
    public Color? BarColor = null; //insanely, the default is null which causes the bar not to render regardless of value
    [DataField("is-slider")]
    public bool IsSlider = false;
    [DataField("value")]
    public float? Value = 0f; //position of the progress bar
    [DataField("on-change")]
    public string? OnChange = null;

}

public sealed class DMFColorSerializer : ITypeReader<Color, ValueDataNode> {
    public Color Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<Color>? instanceProvider = null) {

        if(node.Value.Equals("none", StringComparison.OrdinalIgnoreCase))
            return Color.Transparent;

        var deserializedColor = Color.TryFromName(node.Value, out var color)
                ? color :
                Color.TryFromHex(node.Value);

        if (deserializedColor is null)
            throw new Exception($"Value {node.Value} was not a valid DMF color value!");
        else
            return deserializedColor.Value;
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies, ISerializationContext? context = null) {
        throw new NotImplementedException();
    }
}

