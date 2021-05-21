﻿using OpenDreamShared.Interface;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamShared.Net.Packets {
    public class PacketInterfaceData : IPacket {
        enum AttributeType {
            End = 0x0,
            Pos = 0x1,
            Size = 0x2,
            Anchor1 = 0x3,
            Anchor2 = 0x4,
            IsDefault = 0x5,
            IsPane = 0x6,
            Left = 0x7,
            Right = 0x8,
            IsVert = 0x9,
            Text = 0xA,
            BackgroundColor = 0xB,
            IsVisible = 0xC
        }

        enum DescriptorType {
            Main = 0x0,
            Child = 0x1,
            Input = 0x2,
            Button = 0x3,
            Output = 0x4,
            Info = 0x5,
            Map = 0x6,
            Browser = 0x7
        }

        public PacketID PacketID => PacketID.InterfaceData;

        public InterfaceDescriptor InterfaceDescriptor;

        public PacketInterfaceData() { }

        public PacketInterfaceData(InterfaceDescriptor interfaceDescriptor) {
            InterfaceDescriptor = interfaceDescriptor;
        }

        public void ReadFromStream(PacketStream stream) {
            List<WindowDescriptor> windowDescriptors = new List<WindowDescriptor>();

            int windowCount = stream.ReadByte();
            for (int i = 0; i < windowCount; i++) {
                string windowName = stream.ReadString();
                List<ElementDescriptor> elementDescriptors = new List<ElementDescriptor>();

                int elementCount = stream.ReadByte();
                for (int j = 0; j < elementCount; j++) {
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

                    elementDescriptors.Add(elementDescriptor);

                    AttributeType valueType;
                    do {
                        valueType = (AttributeType)stream.ReadByte();
                        switch (valueType) {
                            case AttributeType.Pos: elementDescriptor.Pos = new Point(stream.ReadUInt16(), stream.ReadUInt16()); break;
                            case AttributeType.Size: elementDescriptor.Size = new Size(stream.ReadUInt16(), stream.ReadUInt16()); break;
                            case AttributeType.Anchor1: elementDescriptor.Anchor1 = new Point(stream.ReadUInt16(), stream.ReadUInt16()); break;
                            case AttributeType.Anchor2: elementDescriptor.Anchor2 = new Point(stream.ReadUInt16(), stream.ReadUInt16()); break;
                            case AttributeType.BackgroundColor: elementDescriptor.BackgroundColor = Color.FromArgb(stream.ReadByte(), stream.ReadByte(), stream.ReadByte()); break;
                            case AttributeType.IsVisible: elementDescriptor.IsVisible = stream.ReadBool(); break;
                            case AttributeType.IsDefault: elementDescriptor.IsDefault = stream.ReadBool(); break;
                            case AttributeType.IsPane: ((ElementDescriptorMain)elementDescriptor).IsPane = stream.ReadBool(); break;
                            case AttributeType.Left: ((ElementDescriptorChild)elementDescriptor).Left = stream.ReadString(); break;
                            case AttributeType.Right: ((ElementDescriptorChild)elementDescriptor).Right = stream.ReadString(); break;
                            case AttributeType.IsVert: ((ElementDescriptorChild)elementDescriptor).IsVert = stream.ReadBool(); break;
                            case AttributeType.Text:
                                if (elementDescriptor is ElementDescriptorButton)
                                    ((ElementDescriptorButton)elementDescriptor).Text = stream.ReadString();
                                break;


                            case AttributeType.End: break;
                            default: throw new Exception("Invalid attribute type '" + valueType + "'");
                        }
                    } while (valueType != AttributeType.End);
                }

                WindowDescriptor windowDescriptor = new WindowDescriptor(windowName, elementDescriptors);
                windowDescriptors.Add(windowDescriptor);
            }

            InterfaceDescriptor = new InterfaceDescriptor(windowDescriptors);
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteByte((byte)InterfaceDescriptor.WindowDescriptors.Count);

            foreach (WindowDescriptor windowDescriptor in InterfaceDescriptor.WindowDescriptors) {
                stream.WriteString(windowDescriptor.Name);

                stream.WriteByte((byte)windowDescriptor.ElementDescriptors.Count);
                foreach (ElementDescriptor elementDescriptor in windowDescriptor.ElementDescriptors) {
                    stream.WriteString(elementDescriptor.Name);

                    if (elementDescriptor is ElementDescriptorMain) stream.WriteByte((byte)DescriptorType.Main);
                    else if (elementDescriptor is ElementDescriptorChild) stream.WriteByte((byte)DescriptorType.Child);
                    else if (elementDescriptor is ElementDescriptorInput) stream.WriteByte((byte)DescriptorType.Input);
                    else if (elementDescriptor is ElementDescriptorButton) stream.WriteByte((byte)DescriptorType.Button);
                    else if (elementDescriptor is ElementDescriptorOutput) stream.WriteByte((byte)DescriptorType.Output);
                    else if (elementDescriptor is ElementDescriptorInfo) stream.WriteByte((byte)DescriptorType.Info);
                    else if (elementDescriptor is ElementDescriptorMap) stream.WriteByte((byte)DescriptorType.Map);
                    else if (elementDescriptor is ElementDescriptorBrowser) stream.WriteByte((byte)DescriptorType.Browser);
                    else throw new Exception("Invalid descriptor");

                    if (elementDescriptor.Pos.HasValue) {
                        stream.WriteByte((byte)AttributeType.Pos);
                        stream.WriteUInt16((UInt16)elementDescriptor.Pos.Value.X);
                        stream.WriteUInt16((UInt16)elementDescriptor.Pos.Value.Y);
                    }
					
                    if (elementDescriptor.Size.HasValue) {
                        stream.WriteByte((byte)AttributeType.Size);
                        stream.WriteUInt16((UInt16)elementDescriptor.Size.Value.Width);
                        stream.WriteUInt16((UInt16)elementDescriptor.Size.Value.Height);
                    }

                    if (elementDescriptor.Anchor1.HasValue) {
                        stream.WriteByte((byte)AttributeType.Anchor1);
                        stream.WriteUInt16((UInt16)elementDescriptor.Anchor1.Value.X);
                        stream.WriteUInt16((UInt16)elementDescriptor.Anchor1.Value.Y);
                    }
					
                    if (elementDescriptor.Anchor2.HasValue) {
                        stream.WriteByte((byte)AttributeType.Anchor2);
                        stream.WriteUInt16((UInt16)elementDescriptor.Anchor2.Value.X);
                        stream.WriteUInt16((UInt16)elementDescriptor.Anchor2.Value.Y);
                    }

                    if (elementDescriptor.BackgroundColor.HasValue) {
                        stream.WriteByte((byte)AttributeType.BackgroundColor);
                        stream.WriteByte(elementDescriptor.BackgroundColor.Value.R);
                        stream.WriteByte(elementDescriptor.BackgroundColor.Value.G);
                        stream.WriteByte(elementDescriptor.BackgroundColor.Value.B);
                    }

                    if (elementDescriptor.IsVisible != default) {
                        stream.WriteByte((byte)AttributeType.IsVisible);
                        stream.WriteBool(elementDescriptor.IsVisible.Value);
                    }

                    if (elementDescriptor.IsDefault != default) {
                        stream.WriteByte((byte)AttributeType.IsDefault);
                        stream.WriteBool(elementDescriptor.IsDefault);
                    }

                    ElementDescriptorMain elementMainDescriptor = elementDescriptor as ElementDescriptorMain;
                    if (elementMainDescriptor != null) {
                        if (elementMainDescriptor.IsPane != default) {
                            stream.WriteByte((byte)AttributeType.IsPane);
                            stream.WriteBool(elementMainDescriptor.IsPane);
                        }
                    }

                    ElementDescriptorChild elementChildDescriptor = elementDescriptor as ElementDescriptorChild;
                    if (elementChildDescriptor != null) {
                        if (elementChildDescriptor.Left != null) {
                            stream.WriteByte((byte)AttributeType.Left);
                            stream.WriteString(elementChildDescriptor.Left);
                        }

                        if (elementChildDescriptor.Right != null) {
                            stream.WriteByte((byte)AttributeType.Right);
                            stream.WriteString(elementChildDescriptor.Right);
                        }

                        if (elementChildDescriptor.IsVert != default) {
                            stream.WriteByte((byte)AttributeType.IsVert);
                            stream.WriteBool(elementChildDescriptor.IsVert);
                        }
                    }

                    ElementDescriptorButton elementButtonDescriptor = elementDescriptor as ElementDescriptorButton;
                    if (elementButtonDescriptor != null) {
                        if (elementButtonDescriptor.Text != null) {
                            stream.WriteByte((byte)AttributeType.Text);
                            stream.WriteString(elementButtonDescriptor.Text);
                        }
                    }

                    stream.WriteByte((byte)AttributeType.End);
                }
            }
        }
    }
}
