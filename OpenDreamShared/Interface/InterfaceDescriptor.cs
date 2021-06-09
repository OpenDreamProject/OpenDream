using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

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

        private enum DescriptorType {
            Main = 0x0,
            Child = 0x1,
            Input = 0x2,
            Button = 0x3,
            Output = 0x4,
            Info = 0x5,
            Map = 0x6,
            Browser = 0x7
        }

        public void WriteToPacket(PacketStream stream) {
            stream.WriteString(Name);

            switch (this) {
                case ElementDescriptorMain: stream.WriteByte((byte)DescriptorType.Main); break;
                case ElementDescriptorInput: stream.WriteByte((byte)DescriptorType.Input); break;
                case ElementDescriptorButton: stream.WriteByte((byte)DescriptorType.Button); break;
                case ElementDescriptorChild: stream.WriteByte((byte)DescriptorType.Child); break;
                case ElementDescriptorOutput: stream.WriteByte((byte)DescriptorType.Output); break;
                case ElementDescriptorInfo: stream.WriteByte((byte)DescriptorType.Info); break;
                case ElementDescriptorMap: stream.WriteByte((byte)DescriptorType.Map); break;
                case ElementDescriptorBrowser: stream.WriteByte((byte)DescriptorType.Browser); break;
                default: throw new Exception("Cannot serialize element descriptor of type " + this.GetType());
            }

            foreach (KeyValuePair<string, FieldInfo> pair in _attributeNameToField) {
                FieldInfo field = pair.Value;
                object value = field.GetValue(this);

                if (value != null) {
                    stream.WriteString(pair.Key);

                    switch (value) {
                        case string stringValue: stream.WriteString(stringValue); break;
                        case int intValue: stream.WriteInt32(intValue); break;
                        case bool boolValue: stream.WriteBool(boolValue); break;
                        case Point position: stream.WriteInt32(position.X); stream.WriteInt32(position.Y); break;
                        case Size dimension: stream.WriteInt32(dimension.Width); stream.WriteInt32(dimension.Height); break;
                        case Color color: stream.WriteByte(color.R); stream.WriteByte(color.G); stream.WriteByte(color.B); break;
                        default: throw new Exception("Cannot serialize attribute value (" + value + ")");
                    }
                }
            }

            stream.WriteString("end");
        }

        public static ElementDescriptor ReadFromPacket(PacketStream stream) {
            string elementName = stream.ReadString();
            DescriptorType elementType = (DescriptorType)stream.ReadByte();
            ElementDescriptor elementDescriptor;

            switch (elementType) {
                case DescriptorType.Main: elementDescriptor = new ElementDescriptorMain(elementName); break;
                case DescriptorType.Input: elementDescriptor = new ElementDescriptorInput(elementName); break;
                case DescriptorType.Button: elementDescriptor = new ElementDescriptorButton(elementName); break;
                case DescriptorType.Child: elementDescriptor = new ElementDescriptorChild(elementName); break;
                case DescriptorType.Output: elementDescriptor = new ElementDescriptorOutput(elementName); break;
                case DescriptorType.Info: elementDescriptor = new ElementDescriptorInfo(elementName); break;
                case DescriptorType.Map: elementDescriptor = new ElementDescriptorMap(elementName); break;
                case DescriptorType.Browser: elementDescriptor = new ElementDescriptorBrowser(elementName); break;
                default: throw new Exception("Invalid descriptor type '" + elementType + "'");
            }

            string attributeName;
            while ((attributeName = stream.ReadString()) != "end") {
                FieldInfo field = elementDescriptor._attributeNameToField[attributeName];

                if (field.FieldType == typeof(string)) {
                    field.SetValue(elementDescriptor, stream.ReadString());
                } else if (field.FieldType == typeof(int)) {
                    field.SetValue(elementDescriptor, stream.ReadInt32());
                } else if (field.FieldType == typeof(bool)) {
                    field.SetValue(elementDescriptor, stream.ReadBool());
                } else if (field.FieldType == typeof(Point?)) {
                    field.SetValue(elementDescriptor, new Point(stream.ReadInt32(), stream.ReadInt32()));
                } else if (field.FieldType == typeof(Size?)) {
                    field.SetValue(elementDescriptor, new Size(stream.ReadInt32(), stream.ReadInt32()));
                } else if (field.FieldType == typeof(Color?)) {
                    field.SetValue(elementDescriptor, Color.FromArgb(stream.ReadByte(), stream.ReadByte(), stream.ReadByte()));
                } else {
                    throw new Exception("Cannot deserialize attribute '" + attributeName + "'");
                }
            }

            return elementDescriptor;
        }
    }

    public class ElementDescriptorMain : ElementDescriptor {
        [InterfaceAttribute("is-pane")]
        public bool IsPane = false;
        [InterfaceAttribute("icon")]
        public string Icon = null;

        public ElementDescriptorMain(string name) : base(name) { }
    }

    public class ElementDescriptorChild : ElementDescriptor {
        [InterfaceAttribute("left")]
        public string Left = null;
        [InterfaceAttribute("right")]
        public string Right = null;
        [InterfaceAttribute("is-vert")]
        public bool IsVert = false;

        public ElementDescriptorChild(string name) : base(name) { }
    }

    public class ElementDescriptorInput : ElementDescriptor {
        public ElementDescriptorInput(string name) : base(name) { }
    }

    public class ElementDescriptorButton : ElementDescriptor {
        [InterfaceAttribute("text")]
        public string Text = null;
        [InterfaceAttribute("command")]
        public string Command = null;

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
