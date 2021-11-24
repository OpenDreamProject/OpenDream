using System.Collections.Generic;
using System.Drawing;

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
        [InterfaceAttribute("pos")]
        public Point? Pos = null;
        [InterfaceAttribute("size")]
        public Size? Size = null;
        [InterfaceAttribute("anchor1")]
        public Point? Anchor1 = null;
        [InterfaceAttribute("anchor2")]
        public Point? Anchor2 = null;
        [InterfaceAttribute("background-color")]
        public Color? BackgroundColor = null;
        [InterfaceAttribute("is-visible")]
        public bool IsVisible = true;
        [InterfaceAttribute("is-default")]
        public bool IsDefault = false;
        [InterfaceAttribute("is-disabled")]
        public bool IsDisabled = false;

        public ControlDescriptor(string name) : base(name) { }
    }

    public class ControlDescriptorMain : ControlDescriptor {
        [InterfaceAttribute("is-pane")]
        public bool IsPane = false;
        [InterfaceAttribute("icon")]
        public string Icon = null;
        [InterfaceAttribute("menu")]
        public string Menu = null;
        [InterfaceAttribute("title")]
        public string Title = null;

        public ControlDescriptorMain(string name) : base(name) { }
    }

    public class ControlDescriptorChild : ControlDescriptor {
        [InterfaceAttribute("left")]
        public string Left = null;
        [InterfaceAttribute("right")]
        public string Right = null;
        [InterfaceAttribute("is-vert")]
        public bool IsVert = false;

        public ControlDescriptorChild(string name) : base(name) { }
    }

    public class ControlDescriptorInput : ControlDescriptor {
        public ControlDescriptorInput(string name) : base(name) { }
    }

    public class ControlDescriptorButton : ControlDescriptor {
        [InterfaceAttribute("text")]
        public string Text = null;
        [InterfaceAttribute("command")]
        public string Command = null;

        public ControlDescriptorButton(string name) : base(name) { }
    }

    public class ControlDescriptorOutput : ControlDescriptor {
        public ControlDescriptorOutput(string name) : base(name) { }
    }

    public class ControlDescriptorInfo : ControlDescriptor {
        public ControlDescriptorInfo(string name) : base(name) { }
    }

    public class ControlDescriptorMap : ControlDescriptor {
        public ControlDescriptorMap(string name) : base(name) { }
    }

    public class ControlDescriptorBrowser : ControlDescriptor {
        public ControlDescriptorBrowser(string name) : base(name) { }
    }

    public class ControlDescriptorLabel : ControlDescriptor {
        [InterfaceAttribute("text")]
        public string Text = null;

        public ControlDescriptorLabel(string name) : base(name) { }
    }
}
