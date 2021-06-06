using OpenDreamShared.Interface;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamShared.Net.Packets {
    public class PacketInterfaceData : IPacket {
        public PacketID PacketID => PacketID.InterfaceData;

        public InterfaceDescriptor InterfaceDescriptor;

        public PacketInterfaceData() { }

        public PacketInterfaceData(InterfaceDescriptor interfaceDescriptor) {
            InterfaceDescriptor = interfaceDescriptor;
        }

        public void ReadFromStream(PacketStream stream) {
            List<WindowDescriptor> windowDescriptors = new();

            int windowCount = stream.ReadByte();
            for (int i = 0; i < windowCount; i++) {
                string windowName = stream.ReadString();
                List<ElementDescriptor> elementDescriptors = new();

                int elementCount = stream.ReadByte();
                for (int j = 0; j < elementCount; j++) {
                    ElementDescriptor elementDescriptor = ElementDescriptor.ReadFromPacket(stream);

                    elementDescriptors.Add(elementDescriptor);
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
                    elementDescriptor.WriteToPacket(stream);
                }
            }
        }
    }
}
