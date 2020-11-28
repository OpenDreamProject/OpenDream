using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamShared.Interface {
    class InterfaceDescriptor {
        public List<InterfaceWindowDescriptor> WindowDescriptors;
        public InterfaceWindowDescriptor DefaultWindowDescriptor { get; private set; } = null;

        public InterfaceDescriptor(List<InterfaceWindowDescriptor> windowDescriptors) {
            WindowDescriptors = windowDescriptors;

            foreach (InterfaceWindowDescriptor windowDescriptor in WindowDescriptors) {
                ElementDescriptor mainElementDescriptor = windowDescriptor.MainElementDescriptor;

                if (mainElementDescriptor.IsDefault) {
                    DefaultWindowDescriptor = windowDescriptor;
                }
            }
        }

        public InterfaceWindowDescriptor GetWindowDescriptorFromName(string name) {
            foreach (InterfaceWindowDescriptor windowDescriptor in WindowDescriptors) {
                if (windowDescriptor.Name == name) return windowDescriptor;
            }

            return null;
        }
    }

    class InterfaceWindowDescriptor {
        public string Name;
        public List<ElementDescriptor> ElementDescriptors;
        public ElementDescriptorMain MainElementDescriptor { get; private set; } = null;

        public InterfaceWindowDescriptor(string name, List<ElementDescriptor> elementDescriptors) {
            Name = name;
            ElementDescriptors = elementDescriptors;

            foreach (ElementDescriptor elementDescriptor in ElementDescriptors) {
                if (elementDescriptor is ElementDescriptorMain) {
                    MainElementDescriptor = (ElementDescriptorMain)elementDescriptor;
                }
            }
        }
    }

    class ElementDescriptor {
        public string Name;

        public Point? Pos = null;
        public Size? Size = null;
        public Point? Anchor1 = null;
        public Point? Anchor2 = null;

        public bool IsDefault = false;

        public ElementDescriptor(string name) {
            Name = name;
        }
    }

    class ElementDescriptorMain : ElementDescriptor {
        public bool IsPane = false;

        public ElementDescriptorMain(string name) : base(name) { }
    }

    class ElementDescriptorChild : ElementDescriptor {
        public string Left = null;
        public string Right = null;
        public bool IsVert = false;

        public ElementDescriptorChild(string name) : base(name) { }
    }

    class ElementDescriptorInput : ElementDescriptor {
        public ElementDescriptorInput(string name) : base(name) { }
    }

    class ElementDescriptorButton : ElementDescriptor {
        public string Text = null;

        public ElementDescriptorButton(string name) : base(name) { }
    }

    class ElementDescriptorOutput : ElementDescriptor {
        public ElementDescriptorOutput(string name) : base(name) { }
    }

    class ElementDescriptorInfo : ElementDescriptor {
        public ElementDescriptorInfo(string name) : base(name) { }
    }

    class ElementDescriptorMap : ElementDescriptor {
        public ElementDescriptorMap(string name) : base(name) { }
    }
    
    class ElementDescriptorBrowser : ElementDescriptor {
        public ElementDescriptorBrowser(string name) : base(name) { }
    }
}
