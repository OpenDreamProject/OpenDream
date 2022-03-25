﻿using Robust.Shared.Maths;
using System.Collections.Generic;
using Robust.Shared.Serialization.Manager.Attributes;

namespace OpenDreamShared.Interface {
    public class WindowDescriptor {
        public string Name;
        public List<ControlDescriptor> ControlDescriptors;
        public ControlDescriptorMain MainControlDescriptor { get; private set; } = null;

        public WindowDescriptor(string name, List<ControlDescriptor> controlDescriptors) {
            Name = name;
            ControlDescriptors = controlDescriptors;

            foreach (ControlDescriptor controlDescriptor in ControlDescriptors) {
                if (controlDescriptor is ControlDescriptorMain mainControlDescriptor) {
                    MainControlDescriptor = mainControlDescriptor;
                }
            }
        }
    }

    public class ControlDescriptor : ElementDescriptor {
        [DataField("pos")]
        public Vector2i? Pos = null;
        [DataField("size")]
        public Vector2i? Size = null;
        [DataField("anchor1")]
        public Vector2i? Anchor1 = null;
        [DataField("anchor2")]
        public Vector2i? Anchor2 = null;
        [DataField("background-color")]
        public Color? BackgroundColor = null;
        [DataField("is-visible")]
        public bool IsVisible = true;
        [DataField("is-default")]
        public bool IsDefault = false;
        [DataField("is-disabled")]
        public bool IsDisabled = false;
    }

    public class ControlDescriptorMain : ControlDescriptor {
        [DataField("is-pane")]
        public bool IsPane = false;
        [DataField("icon")]
        public string Icon = null;
        [DataField("menu")]
        public string Menu = null;
        [DataField("title")]
        public string Title = null;
    }

    public class ControlDescriptorChild : ControlDescriptor {
        [DataField("left")]
        public string Left = null;
        [DataField("right")]
        public string Right = null;
        [DataField("is-vert")]
        public bool IsVert = false;
    }

    public class ControlDescriptorInput : ControlDescriptor {
    }

    public class ControlDescriptorButton : ControlDescriptor {
        [DataField("text")]
        public string Text = null;
        [DataField("command")]
        public string Command = null;

    }

    public class ControlDescriptorOutput : ControlDescriptor {
    }

    public class ControlDescriptorInfo : ControlDescriptor {
    }

    public class ControlDescriptorMap : ControlDescriptor {
    }

    public class ControlDescriptorBrowser : ControlDescriptor {
    }

    public class ControlDescriptorLabel : ControlDescriptor {
        [DataField("text")]
        public string Text = null;
    }
}
