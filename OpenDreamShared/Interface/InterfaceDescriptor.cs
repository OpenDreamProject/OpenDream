using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

namespace OpenDreamShared.Interface {
    public class InterfaceDescriptor {
        public List<WindowDescriptor> WindowDescriptors;
        public Dictionary<string, MenuDescriptor> MenuDescriptors;

        public InterfaceDescriptor(List<WindowDescriptor> windowDescriptors, Dictionary<string, MenuDescriptor> menuDescriptors) {
            WindowDescriptors = windowDescriptors;
            MenuDescriptors = menuDescriptors;
        }
    }

    public class WindowDescriptor {
        public string Name;
        public List<WindowElementDescriptor> ElementDescriptors;
        public ElementDescriptorMain MainElementDescriptor { get; private set; } = null;

        public WindowDescriptor(string name, List<WindowElementDescriptor> elementDescriptors) {
            Name = name;
            ElementDescriptors = elementDescriptors;

            foreach (WindowElementDescriptor elementDescriptor in ElementDescriptors) {
                if (elementDescriptor is ElementDescriptorMain) {
                    MainElementDescriptor = (ElementDescriptorMain)elementDescriptor;
                }
            }
        }
    }

    public class MenuDescriptor {
        public string Name;
        public List<MenuElementDescriptor> Elements;

        public MenuDescriptor(string name, List<MenuElementDescriptor> elements) {
            Name = name;
            Elements = elements;
        }
    }

    public class ElementDescriptor {
        [InterfaceAttribute("name")]
        public string Name;

        private Dictionary<string, FieldInfo> _attributeNameToField = new();

        public ElementDescriptor(string name) {
            Name = name;

            foreach (FieldInfo field in GetType().GetFields()) {
                InterfaceAttributeAttribute attribute = field.GetCustomAttribute<InterfaceAttributeAttribute>();

                if (attribute != null) _attributeNameToField.Add(attribute.Name, field);
            }
        }

        public bool HasAttribute(string name) {
            return _attributeNameToField.ContainsKey(name);
        }

        public void SetAttribute(string name, object value) {
            FieldInfo field = _attributeNameToField[name];

            try {
                field.SetValue(this, value);
            } catch (ArgumentException) {
                throw new Exception("Cannot set attribute '" + name + "' to " + value);
            }
        }
    }

    public class MenuElementDescriptor : ElementDescriptor {
        [InterfaceAttribute("command")]
        public string Command;
        [InterfaceAttribute("category")]
        public string Category;
        [InterfaceAttribute("can-check")]
        public bool CanCheck;

        public MenuElementDescriptor(string name) : base(name) { }
    }

    public class WindowElementDescriptor : ElementDescriptor {
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

        public WindowElementDescriptor(string name) : base(name) { }
    }

    public class ElementDescriptorMain : WindowElementDescriptor {
        [InterfaceAttribute("is-pane")]
        public bool IsPane = false;
        [InterfaceAttribute("icon")]
        public string Icon = null;
        [InterfaceAttribute("menu")]
        public string Menu = null;

        public ElementDescriptorMain(string name) : base(name) { }
    }

    public class ElementDescriptorChild : WindowElementDescriptor {
        [InterfaceAttribute("left")]
        public string Left = null;
        [InterfaceAttribute("right")]
        public string Right = null;
        [InterfaceAttribute("is-vert")]
        public bool IsVert = false;

        public ElementDescriptorChild(string name) : base(name) { }
    }

    public class ElementDescriptorInput : WindowElementDescriptor {
        public ElementDescriptorInput(string name) : base(name) { }
    }

    public class ElementDescriptorButton : WindowElementDescriptor {
        [InterfaceAttribute("text")]
        public string Text = null;
        [InterfaceAttribute("command")]
        public string Command = null;

        public ElementDescriptorButton(string name) : base(name) { }
    }

    public class ElementDescriptorOutput : WindowElementDescriptor {
        public ElementDescriptorOutput(string name) : base(name) { }
    }

    public class ElementDescriptorInfo : WindowElementDescriptor {
        public ElementDescriptorInfo(string name) : base(name) { }
    }

    public class ElementDescriptorMap : WindowElementDescriptor {
        public ElementDescriptorMap(string name) : base(name) { }
    }

    public class ElementDescriptorBrowser : WindowElementDescriptor {
        public ElementDescriptorBrowser(string name) : base(name) { }
    }
}
