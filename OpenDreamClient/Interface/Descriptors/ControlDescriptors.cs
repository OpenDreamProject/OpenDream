using JetBrains.Annotations;
using OpenDreamClient.Interface.DMF;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace OpenDreamClient.Interface.Descriptors;

[Virtual]
public partial class ControlDescriptor : ElementDescriptor {
    [DataField("pos")]
    public DMFPropertyVec2 Pos = new(0, 0);
    [DataField("size")]
    public DMFPropertyVec2 Size = new(0, 0);
    [DataField("anchor1")]
    public DMFPropertyVec2? Anchor1;
    [DataField("anchor2")]
    public DMFPropertyVec2? Anchor2;

    [DataField("is-visible")]
    public DMFPropertyBool IsVisible = new(true);
    [DataField("is-transparent")]
    public DMFPropertyBool IsTransparent = new(false);
    [DataField("border")]
    public DMFPropertyString Border =  new("none");
    [DataField("flash")]
    public DMFPropertyNum Flash = new(0);
    [DataField("saved-params")]
    public DMFPropertyString SavedParams; //default varies
    [DataField("text-color")]
    public DMFPropertyColor TextColor = new(Color.Black);
    [DataField("background-color")]
    public DMFPropertyColor BackgroundColor = new(Color.Transparent);
    [DataField("is-default")]
    public DMFPropertyBool IsDefault = new(false);
    [DataField("is-disabled")]
    public DMFPropertyBool IsDisabled = new(false);
    [DataField("focus")]
    public DMFPropertyBool Focus = new(false);
    [DataField("drop-zone")]
    public DMFPropertyBool DropZone; //default varies
    [DataField("right-click")]
    public DMFPropertyBool RightClick = new(false);
    [DataField("font-family")]
    public DMFPropertyString FontFamily = new("");
    [DataField("font-size")]
    public DMFPropertyNum FontSize = new(0);
    [DataField("font-style")]
    public DMFPropertyString FontStyle = new("");
    [DataField("on-size")]
    public DMFPropertyString OnSize = new("");
}

public sealed partial class WindowDescriptor : ControlDescriptor {
    [DataField("can-minimize")]
    public DMFPropertyBool CanMinimize = new(true);
    [DataField("can-resize")]
    public DMFPropertyBool CanResize = new(true);
    [DataField("is-minimized")]
    public DMFPropertyBool IsMinimized = new(false);
    [DataField("is-maximized")]
    public DMFPropertyBool IsMaximized = new(false);
    [DataField("alpha")]
    public DMFPropertyNum Alpha = new(255);
    [DataField("statusbar")]
    public DMFPropertyBool StatusBar = new(false);
    [DataField("transparent-color")]
    public DMFPropertyColor TransparentColor = new(Color.Transparent);
    [DataField("can-close")]
    public DMFPropertyBool CanClose = new(true);
    [DataField("title")]
    public DMFPropertyString Title = new("");
    [DataField("titlebar")]
    public DMFPropertyBool TitleBar = new(true);
    [DataField("icon")]
    public DMFPropertyString Icon = new("");
    [DataField("image")]
    public DMFPropertyString Image = new("");
    [DataField("image-mode")]
    public DMFPropertyString ImageMode = new("stretch");
    [DataField("keep-aspect")]
    public DMFPropertyBool KeepAspect = new(false);
    [DataField("macro")]
    public DMFPropertyString Macro = new("");
    [DataField("menu")]
    public DMFPropertyString Menu = new("");
    [DataField("on-close")]
    public DMFPropertyString OnClose = new("");
    [DataField("can-scroll")]
    public DMFPropertyString CanScroll = new("none");
    [DataField("is-pane")]
    public DMFPropertyBool IsPane = new(false);

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
    [DataField("lock")]
    public DMFPropertyString Lock = new("none");
    [DataField("is-vert")]
    public DMFPropertyBool IsVert = new(false);
    [DataField("splitter")]
    public DMFPropertyNum Splitter = new(50f);
    [DataField("show-splitter")]
    public DMFPropertyBool ShowSplitter = new(true);
    [DataField("left")]
    public DMFPropertyString Left = new("");
    [DataField("right")]
    public DMFPropertyString Right = new("");


}

public sealed partial class ControlDescriptorInput : ControlDescriptor {
    [DataField("multi-line")]
    public DMFPropertyBool MultiLine = new(false);
    [DataField("is-password")]
    public DMFPropertyBool IsPassword = new(false);
    [DataField("no-command")]
    public DMFPropertyBool NoCommand = new(false);
    [DataField("text")]
    public DMFPropertyString Text = new("");
    [DataField("command")]
    public DMFPropertyString Command = new("");
}

public sealed partial class ControlDescriptorButton : ControlDescriptor {
    [DataField("is-flat")]
    public DMFPropertyBool IsFlat = new(false);
    [DataField("is-checked")]
    public DMFPropertyBool IsChecked = new(false);
    [DataField("group")]
    public DMFPropertyString Group = new("");
    [DataField("button-type")]
    public DMFPropertyString ButtonType = new("pushbutton");
    [DataField("text")]
    public DMFPropertyString Text = new("");
    [DataField("image")]
    public DMFPropertyString Image = new("");
    [DataField("command")]
    public DMFPropertyString Command = new("");
}

public sealed partial class ControlDescriptorOutput : ControlDescriptor {
    [DataField("legacy-size")]
    public DMFPropertyBool LegacySize = new(false);
    [DataField("style")]
    public DMFPropertyString Style = new("");
    [DataField("max-lines")]
    public DMFPropertyNum MaxLines = new(1000);
    [DataField("link-color")]
    public DMFPropertyColor LinkColor = new(Color.Blue);
    [DataField("visited-color")]
    public DMFPropertyColor VisitedColor = new(Color.Purple);
    [DataField("image")]
    public DMFPropertyString Image = new("");
    [DataField("enable-http-images")]
    public DMFPropertyBool EnableHttpImages = new(false);
}

public sealed partial class ControlDescriptorInfo : ControlDescriptor {
    [DataField("multi-line")]
    public DMFPropertyBool MultiLine = new(true);
    [DataField("highlight-color")]
    public DMFPropertyColor HighlightColor = new(Color.Green);
    [DataField("tab-text-color")]
    public DMFPropertyColor TabTextColor = new(Color.Transparent);
    [DataField("tab-background-color")]
    public DMFPropertyColor TabBackgroundColor = new(Color.Transparent);
    [DataField("prefix-color")]
    public DMFPropertyColor PrefixColor = new(Color.Transparent);
    [DataField("suffix-color")]
    public DMFPropertyColor SuffixColor = new(Color.Transparent);
    [DataField("allow-html")]
    public DMFPropertyBool AllowHtml = new(true); // Supposedly false by default, but it isn't if you're not using BYOND's default skin
    [DataField("tab-font-family")]
    public DMFPropertyString TabFontFamily = new("");
    [DataField("tab-font-size")]
    public DMFPropertyNum TabFontSize = new(0);
    [DataField("tab-font-style")]
    public DMFPropertyString TabFontStyle = new("");
    [DataField("on-show")]
    public DMFPropertyString OnShowCommand = new("");
    [DataField("on-hide")]
    public DMFPropertyString OnHideCommand = new("");

}

public sealed partial class ControlDescriptorMap : ControlDescriptor {
    [DataField("view-size")]
    public DMFPropertyNum ViewSize = new(0);
    [DataField("style")]
    public DMFPropertyString Style = new("");
    [DataField("text-mode")]
    public DMFPropertyBool TextMode = new(false);
    [DataField("icon-size")]
    public DMFPropertyNum IconSize = new(0);
    [DataField("letterbox")]
    public DMFPropertyBool Letterbox = new(true);
    [DataField("zoom")]
    public DMFPropertyNum Zoom = new(0);
    [DataField("zoom-mode")]
    public DMFPropertyString ZoomMode = new("normal");
    [DataField("on-show")]
    public DMFPropertyString OnShowCommand = new("");
    [DataField("on-hide")]
    public DMFPropertyString OnHideCommand = new("");

}

public sealed partial class ControlDescriptorBrowser : ControlDescriptor {
    [DataField("show-history")]
    public DMFPropertyBool ShowHistory = new(false);
    [DataField("show-url")]
    public DMFPropertyBool ShowUrl = new(false);
    [DataField("use-title")]
    public DMFPropertyBool UseTitle = new(false);
    [DataField("auto-format")]
    public DMFPropertyBool AutoFormat = new(true);
    [DataField("on-show")]
    public DMFPropertyString OnShowCommand = new("");
    [DataField("on-hide")]
    public DMFPropertyString OnHideCommand = new("");
}

public sealed partial class ControlDescriptorLabel : ControlDescriptor {
    [DataField("text")]
    public DMFPropertyString Text = new("");
    [DataField("align")]
    public DMFPropertyString Align = new("center");
    [DataField("text-wrap")]
    public DMFPropertyBool TextWrap = new(false);
    [DataField("image")]
    public DMFPropertyString Image = new("");
    [DataField("image-mode")]
    public DMFPropertyString ImageMode = new("stretch");
    [DataField("keep-aspect")]
    public DMFPropertyBool KeepAspect = new(false);
}

public sealed partial class ControlDescriptorGrid : ControlDescriptor {
    [DataField("cells")]
    public DMFPropertyVec2 Cells = new(0,0);
    [DataField("cell-span")]
    public DMFPropertyVec2 CellSpan = new(1,1);
    [DataField("is-list")]
    public DMFPropertyBool IsList = new(false);
    [DataField("show-lines")]
    public DMFPropertyString ShowLines = new("both");
    [DataField("style")]
    public DMFPropertyString Style = new("");
    [DataField("highlight-color")]
    public DMFPropertyColor HighlightColor = new(Color.Green);
    [DataField("line-color")]
    public DMFPropertyColor LineColor = new("#c0c0c0");
    [DataField("link-color")]
    public DMFPropertyColor LinkColor = new(Color.Blue);
    [DataField("visited-color")]
    public DMFPropertyColor VisitedCOlor = new(Color.Purple);
    [DataField("current-cell")]
    public DMFPropertyVec2 CurrentCell = new(0,0);
    [DataField("show-names")]
    public DMFPropertyBool ShowNames = new(true);
    [DataField("small-icons")]
    public DMFPropertyBool SmallIcons = new(false);
    [DataField("enable-http-images")]
    public DMFPropertyBool EnableHttpImages = new(false);
}

public sealed partial class ControlDescriptorTab : ControlDescriptor {
    [DataField("multi-line")]
    public DMFPropertyBool MultiLine = new(true);
    [DataField("current-tab")]
    public DMFPropertyString CurrentTab = new("");
    [DataField("on-tab")]
    public DMFPropertyString OnTab = new("");
    [DataField("tabs")]
    public DMFPropertyString Tabs = new("");
}


public sealed partial class ControlDescriptorBar : ControlDescriptor {
    [DataField("width")]
    public DMFPropertyNum Width = new(10); //width of the progress bar in pixels. In the default EAST dir, this is more accurately thought of as "height"
    [DataField("dir")]
    public DMFPropertyString Dir = new("east"); //valid values: north/east/south/west/clockwise/cw/counterclockwise/ccw
    [DataField("angle1")]
    public DMFPropertyNum Angle1 = new(0); //start angle
    [DataField("angle2")]
    public DMFPropertyNum Angle2 = new(180); //end angle
    [DataField("bar-color")]
    public DMFPropertyColor BarColor = new(Color.Transparent); //insanely, the default causes the bar not to render regardless of value
    [DataField("is-slider")]
    public DMFPropertyBool IsSlider = new(false);
    [DataField("value")]
    public DMFPropertyNum Value = new(0f); //position of the progress bar
    [DataField("on-change")]
    public DMFPropertyString OnChange = new("");
}


