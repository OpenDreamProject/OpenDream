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
    public DMFPropertyVec2 Pos = new DMFPropertyVec2(0, 0);
    [DataField("size")]
    public DMFPropertyVec2 Size = new DMFPropertyVec2(0, 0);
    [DataField("anchor1")]
    public DMFPropertyVec2? Anchor1;
    [DataField("anchor2")]
    public DMFPropertyVec2? Anchor2;

    [DataField("is-visible")]
    public DMFPropertyBool IsVisible = new DMFPropertyBool(true);
    [DataField("is-transparent")]
    public DMFPropertyBool IsTransparent = new DMFPropertyBool(false);
    [DataField("border")]
    public DMFPropertyString Border =  new DMFPropertyString("none");
    [DataField("flash")]
    public DMFPropertyNum Flash = new DMFPropertyNum(0);
    [DataField("saved-params")]
    public DMFPropertyString SavedParams; //default varies
    [DataField("text-color")]
    public DMFPropertyColor TextColor = new DMFPropertyColor(Color.Black);
    [DataField("background-color")]
    public DMFPropertyColor BackgroundColor = new DMFPropertyColor(Color.White);
    [DataField("is-default")]
    public DMFPropertyBool IsDefault = new DMFPropertyBool(false);
    [DataField("is-disabled")]
    public DMFPropertyBool IsDisabled = new DMFPropertyBool(false);
    [DataField("focus")]
    public DMFPropertyBool Focus = new DMFPropertyBool(false);
    [DataField("drop-zone")]
    public DMFPropertyBool DropZone; //default varies
    [DataField("right-click")]
    public DMFPropertyBool RightClick = new DMFPropertyBool(false);
    [DataField("font-family")]
    public DMFPropertyString FontFamily = new DMFPropertyString("");
    [DataField("font-size")]
    public DMFPropertyNum FontSize = new DMFPropertyNum(0);
    [DataField("font-style")]
    public DMFPropertyString FontStyle = new DMFPropertyString("");
    [DataField("on-size")]
    public DMFPropertyString OnSize = new DMFPropertyString("");

}

public sealed partial class WindowDescriptor : ControlDescriptor {
    [DataField("can-minimize")]
    public DMFPropertyBool CanMinimize = new DMFPropertyBool(true);
    [DataField("can-resize")]
    public DMFPropertyBool CanResize = new DMFPropertyBool(true);
    [DataField("is-minimized")]
    public DMFPropertyBool IsMinimized = new DMFPropertyBool(false);
    [DataField("is-maximized")]
    public DMFPropertyBool IsMaximized = new DMFPropertyBool(false);
    [DataField("alpha")]
    public DMFPropertyNum Alpha = new DMFPropertyNum(255);
    [DataField("statusbar")]
    public DMFPropertyBool StatusBar = new DMFPropertyBool(false);
    [DataField("transparent-color?")]
    public DMFPropertyColor? TransparentColor = null;
    [DataField("can-close")]
    public DMFPropertyBool CanClose = new DMFPropertyBool(true);
    [DataField("title")]
    public DMFPropertyString Title = new DMFPropertyString("");
    [DataField("titlebar")]
    public DMFPropertyBool TitleBar = new DMFPropertyBool(true);
    [DataField("icon")]
    public DMFPropertyString Icon = new DMFPropertyString("");
    [DataField("image")]
    public DMFPropertyString Image = new DMFPropertyString("");
    [DataField("image-mode")]
    public DMFPropertyString ImageMode = new DMFPropertyString("stretch");
    [DataField("keep-aspect")]
    public DMFPropertyBool KeepAspect = new DMFPropertyBool(false);
    [DataField("macro")]
    public DMFPropertyString Macro = new DMFPropertyString("");
    [DataField("menu")]
    public DMFPropertyString Menu = new DMFPropertyString("");
    [DataField("on-close")]
    public DMFPropertyString OnClose = new DMFPropertyString("");
    [DataField("can-scroll")]
    public DMFPropertyString CanScroll = new DMFPropertyString("none");
    [DataField("is-pane")]
    public DMFPropertyBool IsPane = new DMFPropertyBool(false);

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
    [DataField("multi-line")]
    public DMFPropertyBool MultiLine = new DMFPropertyBool(false);
    [DataField("is-password")]
    public DMFPropertyBool IsPassword = new DMFPropertyBool(false);
    [DataField("no-command")]
    public DMFPropertyBool NoCommand = new DMFPropertyBool(false);
    [DataField("text")]
    public DMFPropertyString Text = new DMFPropertyString("");
    [DataField("command")]
    public DMFPropertyString Command = new DMFPropertyString("");
}

public sealed partial class ControlDescriptorButton : ControlDescriptor {
    [DataField("is-flat")]
    public DMFPropertyBool IsFlat = new DMFPropertyBool(false);
    [DataField("is-checked")]
    public DMFPropertyBool IsChecked = new DMFPropertyBool(false);
    [DataField("group")]
    public DMFPropertyString Group = new DMFPropertyString("");
    [DataField("button-type")]
    public DMFPropertyString ButtonType = new DMFPropertyString("pushbutton");
    [DataField("text")]
    public DMFPropertyString Text = new DMFPropertyString("");
    [DataField("image")]
    public DMFPropertyString Image = new DMFPropertyString("");
    [DataField("command")]
    public DMFPropertyString Command = new DMFPropertyString("");
}

public sealed partial class ControlDescriptorOutput : ControlDescriptor {
    [DataField("legacy-size")]
    public DMFPropertyBool LegacySize = new DMFPropertyBool(false);
    [DataField("style")]
    public DMFPropertyString Style = new DMFPropertyString("");
    [DataField("max-lines")]
    public DMFPropertyNum MaxLines = new DMFPropertyNum(1000);
    [DataField("link-color")]
    public DMFPropertyColor LinkColor = new DMFPropertyColor(Color.Blue);
    [DataField("visited-color")]
    public DMFPropertyColor VisitedColor = new DMFPropertyColor(Color.Purple);
    [DataField("image")]
    public DMFPropertyString Image = new DMFPropertyString("");
    [DataField("enable-http-images")]
    public DMFPropertyBool EnableHTTPImages = new DMFPropertyBool(false);
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
    public DMFPropertyString Text = new DMFPropertyString("");
    [DataField("align")]
    public DMFPropertyString Align = new DMFPropertyString("center");
    [DataField("text-wrap")]
    public DMFPropertyBool TextWrap = new DMFPropertyBool(false);
    [DataField("image")]
    public DMFPropertyString Image = new DMFPropertyString("");
    [DataField("image-mode")]
    public DMFPropertyString ImageMode = new DMFPropertyString("stretch");
    [DataField("keep-aspect")]
    public DMFPropertyBool KeepAspect = new DMFPropertyBool(false);
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


