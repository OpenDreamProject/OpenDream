using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamShared.Interface {
    public class InterfaceDescriptor {
        public List<WindowDescriptor> WindowDescriptors;

        public InterfaceDescriptor(List<WindowDescriptor> windowDescriptors) {
            WindowDescriptors = windowDescriptors;
        }
    }

    public class WindowDescriptor {
        public string Name;
        public List<ElementDescriptor> ElementDescriptors;
        public ElementDescriptorMain MainElementDescriptor { get; private set; } = null;

        public WindowDescriptor(string name, List<ElementDescriptor> elementDescriptors) {
            Name = name;
            ElementDescriptors = elementDescriptors;

            foreach (ElementDescriptor elementDescriptor in ElementDescriptors) {
                if (elementDescriptor is ElementDescriptorMain) {
                    MainElementDescriptor = (ElementDescriptorMain)elementDescriptor;
                }
            }
        }
    }

    public class ElementDescriptor {
        public string Name;

        public Point? Pos = null;
        public Size? Size = null;
        public Point? Anchor1 = null;
        public Point? Anchor2 = null;
        public Color? BackgroundColor = null;
        public bool? IsVisible = null;

        public bool IsDefault = false;

        public ElementDescriptor(string name) {
            Name = name;
        }
    }

    public class ElementDescriptorMain : ElementDescriptor {
        public bool IsPane = false;

        public ElementDescriptorMain(string name) : base(name) { }
    }

    public class ElementDescriptorChild : ElementDescriptor {
        public string Left = null;
        public string Right = null;
        public bool IsVert = false;

        public ElementDescriptorChild(string name) : base(name) { }
    }

    public class ElementDescriptorInput : ElementDescriptor {
        public ElementDescriptorInput(string name) : base(name) { }
    }

    public class ElementDescriptorButton : ElementDescriptor {
        public string Text = null;

        public ElementDescriptorButton(string name) : base(name) { }
    }

    public class ElementDescriptorOutput : ElementDescriptor {
        public ElementDescriptorOutput(string name) : base(name) { }
    }

    public class ElementDescriptorInfo : ElementDescriptor {
        public ElementDescriptorInfo(string name) : base(name) { }
    }

    public class ElementDescriptorMap : ElementDescriptor {
        public ElementDescriptorMap(string name) : base(name) { }
    }
    
    public class ElementDescriptorBrowser : ElementDescriptor {
        public ElementDescriptorBrowser(string name) : base(name) { }
    }
}
