using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;

namespace OpenDreamShared.Net.Packets {
    class PacketATOMTypes : IPacket {
        public PacketID PacketID => PacketID.AtomTypes;
		public Dictionary<UInt16, ATOMBase> AtomBases;

		public PacketATOMTypes() { }

		public PacketATOMTypes(Dictionary<UInt16, ATOMBase> atomBases) {
			AtomBases = atomBases;
		}

        public void ReadFromStream(PacketStream stream) {
			UInt16 atomBaseCount = stream.ReadUInt16();

			AtomBases = new Dictionary<UInt16, ATOMBase>();
			for (int i = 0; i < atomBaseCount; i++) {
				UInt16 baseID = stream.ReadUInt16();
				ATOMType type = (ATOMType)stream.ReadByte();
				IconVisualProperties visualProperties = stream.ReadIconVisualProperties();

				AtomBases[baseID] = new ATOMBase(baseID, type, visualProperties);
			}
		}

        public void WriteToStream(PacketStream stream) {
			stream.WriteUInt16((UInt16)AtomBases.Count);

			foreach (KeyValuePair<UInt16, ATOMBase> atomBase in AtomBases) {
				stream.WriteUInt16(atomBase.Key);
				stream.WriteByte((byte)atomBase.Value.Type);
				stream.WriteIconVisualProperties(atomBase.Value.VisualProperties);
			}
        }
    }
}
