using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamShared.Net.Packets {
    class PacketFullGameState: IPacket {
        public PacketID PacketID => PacketID.FullGameState;
        public DreamFullState FullState;
        public UInt32 GameStateID;
        public UInt16 EyeID;
        public UInt16[] ScreenObjects;

        public PacketFullGameState() { }

        public PacketFullGameState(DreamFullState fullState) {
            FullState = fullState;
        }

        public void ReadFromStream(PacketStream stream) {
            GameStateID = stream.ReadUInt32();
            FullState = new DreamFullState(GameStateID);

            ReadIconAppearancesSection(stream);
            ReadAtomsSection(stream);
            ReadMapSection(stream);
            ReadClientSection(stream);
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteUInt32(FullState.ID);

            WriteIconAppearancesSection(stream);
            WriteAtomsSection(stream);
            WriteMapSection(stream);
            WriteClientSection(stream);
        }

        private void ReadIconAppearancesSection(PacketStream stream) {
            UInt32 appearancesCount = stream.ReadUInt32();

            for (int i = 0; i < appearancesCount; i++) {
                FullState.IconAppearances.Add(IconAppearance.ReadFromPacket(stream));
            }
        }

        private void WriteIconAppearancesSection(PacketStream stream) {
            stream.WriteUInt32((UInt32)FullState.IconAppearances.Count);

            foreach (IconAppearance iconAppearance in FullState.IconAppearances) {
                iconAppearance.WriteToPacket(stream);
            }
        }

        private void ReadAtomsSection(PacketStream stream) {
            UInt16 atomCount = stream.ReadUInt16();

            for (int i = 0; i < atomCount; i++) {
                DreamFullState.Atom atom = new DreamFullState.Atom();
                atom.AtomID = stream.ReadUInt16();
                atom.Type = (AtomType)stream.ReadByte();
                atom.LocationID = stream.ReadUInt16();
                atom.IconAppearanceID = (int)stream.ReadUInt32();
                atom.Overlays = stream.ReadOverlays();
                if (atom.Type == AtomType.Movable) {
                    atom.ScreenLocation = stream.ReadScreenLocation();
                }

                FullState.Atoms[atom.AtomID] = atom;
            }
        }

        private void WriteAtomsSection(PacketStream stream) {
            stream.WriteUInt16((UInt16)FullState.Atoms.Count);

            foreach (KeyValuePair<UInt16, DreamFullState.Atom> atom in FullState.Atoms) {
                stream.WriteUInt16(atom.Value.AtomID);
                stream.WriteByte((byte)atom.Value.Type);
                stream.WriteUInt16(atom.Value.LocationID);
                stream.WriteUInt32((UInt32)atom.Value.IconAppearanceID);
                stream.WriteOverlays(atom.Value.Overlays);
                if (atom.Value.Type == AtomType.Movable) {
                    stream.WriteScreenLocation(atom.Value.ScreenLocation);
                }
            }
        }

        private void ReadMapSection(PacketStream stream) {
            UInt16 MapWidth = stream.ReadUInt16();
            UInt16 MapHeight = stream.ReadUInt16();

            FullState.Turfs = new UInt16[MapWidth, MapHeight];
            for (int x = 0; x < MapWidth; x++) {
                for (int y = 0; y < MapHeight; y++) {
                    FullState.Turfs[x, y] = stream.ReadUInt16();
                }
            }
        }

        private void WriteMapSection(PacketStream stream) {
            stream.WriteUInt16((UInt16)FullState.Turfs.GetLength(0));
            stream.WriteUInt16((UInt16)FullState.Turfs.GetLength(1));

            for (int x = 0; x < FullState.Turfs.GetLength(0); x++) {
                for (int y = 0; y < FullState.Turfs.GetLength(1); y++) {
                    stream.WriteUInt16(FullState.Turfs[x, y]);
                }
            }
        }

        private void ReadClientSection(PacketStream stream) {
            EyeID = stream.ReadUInt16();

            UInt16 screenObjectCount = stream.ReadUInt16();
            ScreenObjects = new UInt16[screenObjectCount];
            for (int i = 0; i < screenObjectCount; i++) {
                ScreenObjects[i] = stream.ReadUInt16();
            }
        }

        private void WriteClientSection(PacketStream stream) {
            stream.WriteUInt16(0xFFFF);

            stream.WriteUInt16(0);
        }
    }
}
