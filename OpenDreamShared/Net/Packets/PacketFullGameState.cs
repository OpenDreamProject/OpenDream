using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamShared.Net.Packets {
    class PacketFullGameState: IPacket {
        public PacketID PacketID => PacketID.FullGameState;

        public DreamFullState FullState;
        public UInt32 GameStateID;
        public UInt32 EyeID;
        public UInt32[] ScreenObjects;

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
            UInt32 atomCount = stream.ReadUInt32();

            for (int i = 0; i < atomCount; i++) {
                DreamFullState.Atom atom = new DreamFullState.Atom();
                atom.AtomID = stream.ReadUInt32();
                atom.Type = (AtomType)stream.ReadByte();
                atom.LocationID = stream.ReadUInt32();
                atom.IconAppearanceID = (int)stream.ReadUInt32();
                if (atom.Type == AtomType.Movable) {
                    atom.ScreenLocation = stream.ReadScreenLocation();
                }

                FullState.Atoms[atom.AtomID] = atom;
            }
        }

        private void WriteAtomsSection(PacketStream stream) {
            stream.WriteUInt32((UInt32)FullState.Atoms.Count);

            foreach (KeyValuePair<UInt32, DreamFullState.Atom> atom in FullState.Atoms) {
                stream.WriteUInt32(atom.Value.AtomID);
                stream.WriteByte((byte)atom.Value.Type);
                stream.WriteUInt32(atom.Value.LocationID);
                stream.WriteUInt32((UInt32)atom.Value.IconAppearanceID);
                if (atom.Value.Type == AtomType.Movable) {
                    stream.WriteScreenLocation(atom.Value.ScreenLocation);
                }
            }
        }

        private void ReadMapSection(PacketStream stream) {
            UInt16 MapWidth = stream.ReadUInt16();
            UInt16 MapHeight = stream.ReadUInt16();

            FullState.Turfs = new UInt32[MapWidth, MapHeight];
            for (int x = 0; x < MapWidth; x++) {
                for (int y = 0; y < MapHeight; y++) {
                    FullState.Turfs[x, y] = stream.ReadUInt32();
                }
            }
        }

        private void WriteMapSection(PacketStream stream) {
            stream.WriteUInt16((UInt16)FullState.Turfs.GetLength(0));
            stream.WriteUInt16((UInt16)FullState.Turfs.GetLength(1));

            for (int x = 0; x < FullState.Turfs.GetLength(0); x++) {
                for (int y = 0; y < FullState.Turfs.GetLength(1); y++) {
                    stream.WriteUInt32(FullState.Turfs[x, y]);
                }
            }
        }

        private void ReadClientSection(PacketStream stream) {
            EyeID = stream.ReadUInt32();

            UInt32 screenObjectCount = stream.ReadUInt32();
            ScreenObjects = new UInt32[screenObjectCount];
            for (int i = 0; i < screenObjectCount; i++) {
                ScreenObjects[i] = stream.ReadUInt32();
            }
        }

        private void WriteClientSection(PacketStream stream) {
            stream.WriteUInt32(UInt32.MaxValue);

            stream.WriteUInt32(0);
        }
    }
}
