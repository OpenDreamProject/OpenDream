using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;

namespace OpenDreamShared.Net.Packets {
    public class PacketFullGameState: IPacket {
        public PacketID PacketID => PacketID.FullGameState;

        public DreamFullState FullState;
        public DreamFullState.Client ClientState;

        public PacketFullGameState() { }

        public PacketFullGameState(DreamFullState fullState, string ckey) {
            FullState = fullState;
            FullState.Clients.TryGetValue(ckey, out ClientState);
        }

        public void ReadFromStream(PacketStream stream) {
            FullState = new DreamFullState(stream.ReadUInt32());

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
                    atom.ScreenLocation = ScreenLocation.ReadFromPacket(stream);
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
                    atom.Value.ScreenLocation.WriteToPacket(stream);
                }
            }
        }

        private void ReadMapSection(PacketStream stream) {
            UInt16 mapWidth = stream.ReadUInt16();
            UInt16 mapHeight = stream.ReadUInt16();
            UInt16 levels = stream.ReadUInt16();

            FullState.Levels = new List<DreamFullState.Level>(levels);
            for (int z = 0; z < levels; z++) {
                FullState.Levels.Add(new DreamFullState.Level(mapWidth, mapHeight));

                for (int x = 0; x < mapWidth; x++) {
                    for (int y = 0; y < mapHeight; y++) {
                        FullState.Levels[z].Turfs[x, y] = stream.ReadUInt32();
                    }
                }
            }
        }

        private void WriteMapSection(PacketStream stream) {
            int mapWidth = FullState.Levels[0].Turfs.GetLength(0);
            int mapHeight = FullState.Levels[0].Turfs.GetLength(1);

            stream.WriteUInt16((UInt16)mapWidth);
            stream.WriteUInt16((UInt16)mapHeight);
            stream.WriteUInt16((UInt16)FullState.Levels.Count);

            foreach (DreamFullState.Level level in FullState.Levels) {
                for (int x = 0; x < mapWidth; x++) {
                    for (int y = 0; y < mapHeight; y++) {
                        stream.WriteUInt32(level.Turfs[x, y]);
                    }
                }
            }
        }

        private void ReadClientSection(PacketStream stream) {
            if (!stream.ReadBool()) return;

            ClientState = new DreamFullState.Client();
            ClientState.EyeID = stream.ReadUInt32();
            ClientState.Perspective = (ClientPerspective)stream.ReadByte();
            ClientState.SeeInvisible = (byte)stream.ReadByte();
            ClientState.ScreenObjects = new List<UInt32>();

            UInt32 screenObjectCount = stream.ReadUInt32();
            for (int i = 0; i < screenObjectCount; i++) {
                ClientState.ScreenObjects[i] = stream.ReadUInt32();
            }
        }

        private void WriteClientSection(PacketStream stream) {
            stream.WriteBool(ClientState != null);

            if (ClientState != null) {
                stream.WriteUInt32(ClientState.EyeID);
                stream.WriteByte((byte)ClientState.Perspective);
                stream.WriteByte(ClientState.SeeInvisible);

                stream.WriteUInt32((UInt32)ClientState.ScreenObjects.Count);
                foreach (UInt32 screenObject in ClientState.ScreenObjects) {
                    stream.WriteUInt32(screenObject);
                }
            }
        }
    }
}
