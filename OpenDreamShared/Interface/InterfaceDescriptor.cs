using System.Collections.Generic;

namespace OpenDreamShared.Interface {
    class InterfaceDescriptor {
        public List<InterfaceWindowDescriptor> WindowDescriptors;
        public InterfaceWindowDescriptor DefaultWindowDescriptor { get; private set; } = null;

        public InterfaceDescriptor(List<InterfaceWindowDescriptor> windowDescriptors) {
            WindowDescriptors = windowDescriptors;

            foreach (InterfaceWindowDescriptor windowDescriptor in WindowDescriptors) {
                InterfaceElementDescriptor mainElementDescriptor = windowDescriptor.MainElementDescriptor;

                if (mainElementDescriptor.BoolAttributes.ContainsKey("is-default") && mainElementDescriptor.BoolAttributes["is-default"] == true) {
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
        public List<InterfaceElementDescriptor> ElementDescriptors;
        public InterfaceElementDescriptor MainElementDescriptor { get; private set; } = null;

        public InterfaceWindowDescriptor(string name, List<InterfaceElementDescriptor> elementDescriptors) {
            Name = name;
            ElementDescriptors = elementDescriptors;

            foreach (InterfaceElementDescriptor elementDescriptor in ElementDescriptors) {
                if (elementDescriptor.Type == InterfaceElementDescriptor.InterfaceElementDescriptorType.Main) {
                    MainElementDescriptor = elementDescriptor;
                }
            }
        }
    }

    class InterfaceElementDescriptor {
        public enum InterfaceElementDescriptorType {
            Main = 0,
            Child = 1,
            Input = 2,
            Button = 3,
            Output = 4,
            Info = 5,
            Map = 6,
            Browser = 7
        }

        public string Name;
        public InterfaceElementDescriptorType Type;
        public Dictionary<string, string> StringAttributes = new Dictionary<string, string>();
        public Dictionary<string, bool> BoolAttributes = new Dictionary<string, bool>();
        public Dictionary<string, int> IntegerAttributes = new Dictionary<string, int>();
        public Dictionary<string, System.Drawing.Point> CoordinateAttributes = new Dictionary<string, System.Drawing.Point>();
        public Dictionary<string, System.Drawing.Size> DimensionAttributes = new Dictionary<string, System.Drawing.Size>();
        public System.Drawing.Point Pos = new System.Drawing.Point(0, 0);
        public System.Drawing.Size Size = new System.Drawing.Size(0, 0);

        public InterfaceElementDescriptor(string name, InterfaceElementDescriptorType type) {
            Name = name;
            Type = type;
        }
    }
}
