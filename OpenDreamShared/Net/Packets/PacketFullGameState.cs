using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Objects;
using System;
using System.Collections.Generic;

namespace OpenDreamShared.Net.Packets {
    class PacketFullGameState: IPacket {
        public PacketID PacketID => PacketID.FullGameState;
        public DreamFullState FullState;
        public UInt32 GameStateID;
        public AtomID EyeID;
        public AtomID[] ScreenObjects;

        public PacketFullGameState() { }

        public PacketFullGameState(DreamFullState fullState) {
            FullState = fullState;
        }

        public void ReadFromStream(PacketStream stream) {
            GameStateID = stream.ReadUInt32();
            FullState = new DreamFullState(GameStateID);

            ReadAtomsSection(stream);
            ReadMapSection(stream);
            ReadClientSection(stream);
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteUInt32(FullState.ID);

            WriteAtomsSection(stream);
            WriteMapSection(stream);
            WriteClientSection(stream);
        }

        private void ReadAtomsSection(PacketStream stream) {
            UInt16 atomCount = stream.ReadUInt16();

            for (int i = 0; i < atomCount; i++) {
                DreamFullState.Atom atom = new DreamFullState.Atom();
                atom.AtomID = stream.ReadAtomID();
                atom.BaseID = stream.ReadUInt16();
                atom.LocationID = stream.ReadAtomID();
                atom.VisualProperties = stream.ReadIconVisualProperties();
                atom.Overlays = stream.ReadOverlays();
                if (ATOMBase.AtomBases[atom.BaseID].Type == ATOMType.Movable) {
                    atom.ScreenLocation = stream.ReadScreenLocation();
                }

                FullState.Atoms[atom.AtomID] = atom;
            }
        }

        private void WriteAtomsSection(PacketStream stream) {
            stream.WriteUInt16((UInt16)FullState.Atoms.Count);

            foreach (KeyValuePair<AtomID, DreamFullState.Atom> atom in FullState.Atoms) {
                stream.WriteAtomID(atom.Value.AtomID);
                stream.WriteUInt16(atom.Value.BaseID);
                stream.WriteAtomID(atom.Value.LocationID);
                stream.WriteIconVisualProperties(atom.Value.VisualProperties, ATOMBase.AtomBases[atom.Value.BaseID].VisualProperties);
                stream.WriteOverlays(atom.Value.Overlays);
                if (ATOMBase.AtomBases[atom.Value.BaseID].Type == ATOMType.Movable) {
                    stream.WriteScreenLocation(atom.Value.ScreenLocation);
                }
            }
        }

        private void ReadMapSection(PacketStream stream) {
            UInt16 MapWidth = stream.ReadUInt16();
            UInt16 MapHeight = stream.ReadUInt16();

            FullState.Turfs = new AtomID[MapWidth, MapHeight];
            for (int x = 0; x < MapWidth; x++) {
                for (int y = 0; y < MapHeight; y++) {
                    FullState.Turfs[x, y] = stream.ReadAtomID();
                }
            }
        }

        private void WriteMapSection(PacketStream stream) {
            stream.WriteUInt16((UInt16)FullState.Turfs.GetLength(0));
            stream.WriteUInt16((UInt16)FullState.Turfs.GetLength(1));

            for (int x = 0; x < FullState.Turfs.GetLength(0); x++) {
                for (int y = 0; y < FullState.Turfs.GetLength(1); y++) {
                    stream.WriteAtomID(FullState.Turfs[x, y]);
                }
            }
        }

        private void ReadClientSection(PacketStream stream) {
            EyeID = stream.ReadAtomID();

            UInt16 screenObjectCount = stream.ReadUInt16();
            ScreenObjects = new AtomID[screenObjectCount];
            for (int i = 0; i < screenObjectCount; i++) {
                ScreenObjects[i] = stream.ReadAtomID();
            }
        }

        private void WriteClientSection(PacketStream stream) {
            stream.WriteAtomID(AtomID.NullAtom);

            stream.WriteUInt16(0);
        }
    }
}
