using OpenDreamShared.Interface;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamShared.Net.Packets {
    class PacketInterfaceData : IPacket {
		enum PacketInterfaceDataAttributeType {
			String = 0x0,
			Boolean = 0x1,
			Integer = 0x2,
			Coordinate = 0x3,
			Dimension = 0x4
		}

        public PacketID PacketID => PacketID.InterfaceData;
		public InterfaceDescriptor InterfaceDescriptor;

        public PacketInterfaceData() { }

		public PacketInterfaceData(InterfaceDescriptor interfaceDescriptor) {
			InterfaceDescriptor = interfaceDescriptor;
		}

        public void ReadFromStream(PacketStream stream) {
			List<InterfaceWindowDescriptor> windowDescriptors = new List<InterfaceWindowDescriptor>();

			int windowCount = stream.ReadByte();
			for (int i = 0; i < windowCount; i++) {
				string windowName = stream.ReadString();
				List<InterfaceElementDescriptor> elementDescriptors = new List<InterfaceElementDescriptor>();

				int elementCount = stream.ReadByte();
				for (int j = 0; j < elementCount; j++) {
					InterfaceElementDescriptor elementDescriptor = new InterfaceElementDescriptor(stream.ReadString(), (InterfaceElementDescriptor.InterfaceElementDescriptorType)stream.ReadByte());
					elementDescriptors.Add(elementDescriptor);

					int attributeCount = stream.ReadByte();
					for (int k = 0; k < attributeCount; k++) {
						string attributeName = stream.ReadString();

						PacketInterfaceDataAttributeType valueType = (PacketInterfaceDataAttributeType)stream.ReadByte();
						if (valueType == PacketInterfaceDataAttributeType.String) {
							elementDescriptor.StringAttributes.Add(attributeName, stream.ReadString());
						} else if (valueType == PacketInterfaceDataAttributeType.Boolean) {
							elementDescriptor.BoolAttributes.Add(attributeName, stream.ReadBool());
						} else if (valueType == PacketInterfaceDataAttributeType.Integer) {
							elementDescriptor.IntegerAttributes.Add(attributeName, stream.ReadUInt16());
						} else if (valueType == PacketInterfaceDataAttributeType.Coordinate) {
							System.Drawing.Point coordinate = new System.Drawing.Point(stream.ReadUInt16(), stream.ReadUInt16());

							elementDescriptor.CoordinateAttributes.Add(attributeName, coordinate);
							if (attributeName == "pos") {
								elementDescriptor.Pos = coordinate;
							}
						} else if (valueType == PacketInterfaceDataAttributeType.Dimension) {
							System.Drawing.Size dimensions = new System.Drawing.Size(stream.ReadUInt16(), stream.ReadUInt16());

							elementDescriptor.DimensionAttributes.Add(attributeName, dimensions);
							if (attributeName == "size") {
								elementDescriptor.Size = dimensions;
							}
						} else {
							throw new Exception("Invalid attribute value type '" + valueType + "'");
						}
					}
				}

				InterfaceWindowDescriptor windowDescriptor = new InterfaceWindowDescriptor(windowName, elementDescriptors);
				windowDescriptors.Add(windowDescriptor);
			}

			InterfaceDescriptor = new InterfaceDescriptor(windowDescriptors);
        }

        public void WriteToStream(PacketStream stream) {
			stream.WriteByte((byte)InterfaceDescriptor.WindowDescriptors.Count);

			foreach (InterfaceWindowDescriptor windowDescriptor in InterfaceDescriptor.WindowDescriptors) {
				stream.WriteString(windowDescriptor.Name);

				stream.WriteByte((byte)windowDescriptor.ElementDescriptors.Count);
				foreach (InterfaceElementDescriptor elementDescriptor in windowDescriptor.ElementDescriptors) {
					stream.WriteString(elementDescriptor.Name);
					stream.WriteByte((byte)elementDescriptor.Type);
					stream.WriteByte((byte)(elementDescriptor.StringAttributes.Count
											+ elementDescriptor.BoolAttributes.Count
											+ elementDescriptor.IntegerAttributes.Count
											+ elementDescriptor.CoordinateAttributes.Count
											+ elementDescriptor.DimensionAttributes.Count));

					foreach (KeyValuePair<string, string> attribute in elementDescriptor.StringAttributes) {
						stream.WriteString(attribute.Key);
						stream.WriteByte((byte)PacketInterfaceDataAttributeType.String);
						stream.WriteString(attribute.Value);
					}

					foreach (KeyValuePair<string, bool> attribute in elementDescriptor.BoolAttributes) {
						stream.WriteString(attribute.Key);
						stream.WriteByte((byte)PacketInterfaceDataAttributeType.Boolean);
						stream.WriteBool(attribute.Value);
					}

					foreach (KeyValuePair<string, int> attribute in elementDescriptor.IntegerAttributes) {
						stream.WriteString(attribute.Key);
						stream.WriteByte((byte)PacketInterfaceDataAttributeType.Integer);
						stream.WriteUInt16((UInt16)attribute.Value);
					}

					foreach (KeyValuePair<string, Point> attribute in elementDescriptor.CoordinateAttributes) {
						stream.WriteString(attribute.Key);
						stream.WriteByte((byte)PacketInterfaceDataAttributeType.Coordinate);
						stream.WriteUInt16((UInt16)attribute.Value.X);
						stream.WriteUInt16((UInt16)attribute.Value.Y);
					}

					foreach (KeyValuePair<string, Size> attribute in elementDescriptor.DimensionAttributes) {
						stream.WriteString(attribute.Key);
						stream.WriteByte((byte)PacketInterfaceDataAttributeType.Dimension);
						stream.WriteUInt16((UInt16)attribute.Value.Width);
						stream.WriteUInt16((UInt16)attribute.Value.Height);
					}
				}
			}
		}
    }
}
