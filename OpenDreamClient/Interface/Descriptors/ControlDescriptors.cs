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
    public DMFPropertyVec2 Pos;
    [DataField("size")]
    public DMFPropertyVec2 Size;
    [DataField("anchor1")]
    public DMFPropertyVec2? Anchor1;
    [DataField("anchor2")]
    public DMFPropertyVec2? Anchor2;
    [DataField("background-color")]
    public DMFPropertyColor BackgroundColor;
    [DataField("is-visible")]
    public DMFPropertyBool IsVisible = new DMFPropertyBool(true);
    [DataField("is-default")]
    public DMFPropertyBool IsDefault = new DMFPropertyBool(false);
    [DataField("is-disabled")]
    public DMFPropertyBool IsDisabled = new DMFPropertyBool(false);
    [DataField("saved-params")]
    public DMFPropertyString SavedParams;
}

public sealed partial class WindowDescriptor : ControlDescriptor {
    [DataField("is-pane")]
    public DMFPropertyBool IsPane = new DMFPropertyBool(false);
    [DataField("icon")]
    public DMFPropertyString Icon;
    [DataField("menu")]
    public DMFPropertyString Menu;
    [DataField("title")]
    public DMFPropertyString Title;
    [DataField("macro")]
    public DMFPropertyString Macro { get; private set; }
    [DataField("on-close")]
    public DMFPropertyString OnClose { get; private set; }

    public readonly List<ControlDescriptor> ControlDescriptors;

    public WindowDescriptor(string id, List<ControlDescriptor>? controlDescriptors = null) {
        ControlDescriptors = controlDescriptors ?? new();
        Id = new DMFPropertyString(id);
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
            attributes["name"] = new ValueDataNode(Name.Value);

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

        var child = (ControlDescriptor?)serializationManager.Read(descriptorType, attributes);
        if (child == null)
            return null;

        ControlDescriptors.Add(child);
        return child;
    }

    public override ElementDescriptor CreateCopy(ISerializationManager serializationManager, string id) {
        var copy = serializationManager.CreateCopy(this, notNullableOverride: true);

        copy._id = new DMFPropertyString(id);
        foreach(var child in this.ControlDescriptors)
            copy.ControlDescriptors.Add(serializationManager.CreateCopy(child, notNullableOverride: false));
        return copy;
    }

    public WindowDescriptor WithVisible(ISerializationManager serializationManager, bool visible) {
        WindowDescriptor copy = (WindowDescriptor)CreateCopy(serializationManager, Id.AsRaw());

        copy.IsVisible = new DMFPropertyBool(visible);
        return copy;
    }
}

public sealed partial class ControlDescriptorChild : ControlDescriptor {
    [DataField("left")]
    public DMFPropertyString Left;
    [DataField("right")]
    public DMFPropertyString Right;
    [DataField("is-vert")]
    public DMFPropertyBool IsVert;
    [DataField("splitter")]
    public DMFPropertyNum Splitter = new DMFPropertyNum(50f);
}

public sealed partial class ControlDescriptorInput : ControlDescriptor {
    [DataField("text")]
    public DMFPropertyString Text;
}

public sealed partial class ControlDescriptorButton : ControlDescriptor {
    [DataField("text")]
    public DMFPropertyString Text;
    [DataField("command")]
    public DMFPropertyString Command;
}

public sealed partial class ControlDescriptorOutput : ControlDescriptor {
}

public sealed partial class ControlDescriptorInfo : ControlDescriptor {
    [DataField("on-show")]
    public DMFPropertyString OnShowCommand;
    [DataField("on-hide")]
    public DMFPropertyString OnHideCommand;
    [DataField("allow-html")]
    public DMFPropertyBool AllowHtml = new DMFPropertyBool(true); // Supposedly false by default, but it isn't if you're not using BYOND's default skin
}

public sealed partial class ControlDescriptorMap : ControlDescriptor {
    [DataField("on-show")]
    public DMFPropertyString OnShowCommand;
    [DataField("on-hide")]
    public DMFPropertyString OnHideCommand;
    [DataField("zoom-mode")]
    public DMFPropertyString ZoomMode = new DMFPropertyString("normal");
    [DataField("icon-size")]
    public DMFPropertyNum IconSize;
}

public sealed partial class ControlDescriptorBrowser : ControlDescriptor {
    [DataField("on-show")]
    public DMFPropertyString OnShowCommand;
    [DataField("on-hide")]
    public DMFPropertyString OnHideCommand;
}

public sealed partial class ControlDescriptorLabel : ControlDescriptor {
    [DataField("text")]
    public DMFPropertyString Text;
}

public sealed partial class ControlDescriptorGrid : ControlDescriptor {
}

public sealed partial class ControlDescriptorTab : ControlDescriptor {
    [DataField("tabs")]
    public DMFPropertyString Tabs;
    [DataField("current-tab")]
    public DMFPropertyString CurrentTab;
}


public sealed partial class ControlDescriptorBar : ControlDescriptor {
    [DataField("width")]
    public DMFPropertyNum Width = new DMFPropertyNum(10); //width of the progress bar in pixels. In the default EAST dir, this is more accurately thought of as "height"
    [DataField("dir")]
    public DMFPropertyString Dir = new DMFPropertyString("east"); //valid values: north/east/south/west/clockwise/cw/counterclockwise/ccw
    [DataField("angle1")]
    public DMFPropertyNum Angle1 = new DMFPropertyNum(0); //start angle
    [DataField("angle2")]
    public DMFPropertyNum Angle2 = new DMFPropertyNum(180); //end angle
    [DataField("bar-color")]
    public DMFPropertyColor BarColor = new DMFPropertyColor(Color.Transparent); //insanely, the default causes the bar not to render regardless of value
    [DataField("is-slider")]
    public DMFPropertyBool IsSlider = new DMFPropertyBool(false);
    [DataField("value")]
    public DMFPropertyNum Value = new DMFPropertyNum(0f); //position of the progress bar
    [DataField("on-change")]
    public DMFPropertyString OnChange = new DMFPropertyString(null);

}


