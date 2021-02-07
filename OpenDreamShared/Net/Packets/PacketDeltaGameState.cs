using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;

namespace OpenDreamShared.Net.Packets {
    class PacketDeltaGameState : IPacket {
        private enum SectionID {
            AtomCreations = 0x0,
            AtomDeletions = 0x1,
            AtomDeltas = 0x2,
            AtomLocationDeltas = 0x3,
            TurfDeltas = 0x4,
            Client = 0x5,
            NewIconAppearances = 0x6
        }

        private enum AtomDeltaValueID {
            End = 0x0,
            ScreenLocation = 0x1,
            IconAppearance = 0x2
        }

        private enum ClientValueID {
            End = 0x0,
            Eye = 0x1,
            ScreenObjectAdditions = 0x2,
            ScreenObjectRemovals = 0x3
        }

        public PacketID PacketID => PacketID.DeltaGameState;

        public DreamDeltaState DeltaState;
        public DreamDeltaState.ClientDelta ClientDelta;

        public PacketDeltaGameState() { }

        public PacketDeltaGameState(DreamDeltaState deltaState, string targetCKey) {
            DeltaState = deltaState;
            ClientDelta = deltaState.ClientDeltas.ContainsKey(targetCKey) ? deltaState.ClientDeltas[targetCKey] : new DreamDeltaState.ClientDelta();
        }

        public void ReadFromStream(PacketStream stream) {
            DeltaState = new DreamDeltaState(stream.ReadUInt32());

            while (stream.Position < stream.Length) {
                SectionID sectionID = (SectionID)stream.ReadByte();

                switch (sectionID) {
                    case SectionID.AtomCreations: ReadAtomCreationsSection(stream); break;
                    case SectionID.AtomDeletions: ReadAtomDeletionsSection(stream); break;
                    case SectionID.AtomDeltas: ReadAtomDeltasSection(stream); break;
                    case SectionID.AtomLocationDeltas: ReadAtomLocationDeltasSection(stream); break;
                    case SectionID.TurfDeltas: ReadTurfDeltasSection(stream); break;
                    case SectionID.Client: ReadClientSection(stream); break;
                    case SectionID.NewIconAppearances: ReadNewIconAppearancesSection(stream); break;
                    default: throw new Exception("Invalid section ID in delta game state packet (" + sectionID + ")");
                }
            }
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteUInt32(DeltaState.ID);

            if (DeltaState.AtomCreations.Count > 0) WriteAtomCreationsSection(stream);
            if (DeltaState.AtomDeletions.Count > 0) WriteAtomDeletionsSection(stream);
            if (DeltaState.AtomDeltas.Count > 0) WriteAtomDeltasSection(stream);
            if (DeltaState.AtomLocationDeltas.Count > 0) WriteAtomLocationDeltasSection(stream);
            if (DeltaState.TurfDeltas.Count > 0) WriteTurfDeltasSection(stream);
            if (DeltaState.NewIconAppearances.Count > 0) WriteNewIconAppearancesSection(stream);
            WriteClientSection(stream);
        }

        private void ReadAtomCreationsSection(PacketStream stream) {
            UInt16 atomCreationsCount = stream.ReadUInt16();

            for (int i = 0; i < atomCreationsCount; i++) {
                UInt32 atomID = stream.ReadUInt32();
                DreamDeltaState.AtomCreation atomCreation = new DreamDeltaState.AtomCreation((AtomType)stream.ReadByte(), (int)stream.ReadUInt32());

                atomCreation.LocationID = stream.ReadUInt32();
                if (atomCreation.Type == AtomType.Movable) {
                    atomCreation.ScreenLocation = stream.ReadScreenLocation();
                }

                DeltaState.AtomCreations.Add(atomID, atomCreation);
            }
        }

        private void WriteAtomCreationsSection(PacketStream stream) {
            stream.WriteByte((byte)SectionID.AtomCreations);
            stream.WriteUInt16((UInt16)DeltaState.AtomCreations.Count);

            foreach (KeyValuePair<UInt32, DreamDeltaState.AtomCreation> atomCreationPair in DeltaState.AtomCreations) {
                DreamDeltaState.AtomCreation atomCreation = atomCreationPair.Value;

                stream.WriteUInt32(atomCreationPair.Key);
                stream.WriteByte((byte)atomCreation.Type);
                stream.WriteUInt32((UInt32)atomCreation.IconAppearanceID);
                stream.WriteUInt32(atomCreation.LocationID);
                if (atomCreation.Type == AtomType.Movable) {
                    stream.WriteScreenLocation(atomCreation.ScreenLocation);
                }
            }
        }

        private void ReadAtomDeletionsSection(PacketStream stream) {
            UInt16 atomDeletionsCount = stream.ReadUInt16();

            for (int i = 0; i < atomDeletionsCount; i++) {
                DeltaState.AtomDeletions.Add(stream.ReadUInt32());
            }
        }

        private void WriteAtomDeletionsSection(PacketStream stream) {
            stream.WriteByte((byte)SectionID.AtomDeletions);
            stream.WriteUInt16((UInt16)DeltaState.AtomDeletions.Count);

            foreach (UInt32 atomDeletion in DeltaState.AtomDeletions) {
                stream.WriteUInt32(atomDeletion);
            }
        }

        private void ReadAtomDeltasSection(PacketStream stream) {
            UInt16 atomDeltasCount = stream.ReadUInt16();

            for (int i = 0; i < atomDeltasCount; i++) {
                UInt32 atomID = stream.ReadUInt32();
                DreamDeltaState.AtomDelta atomDelta = new DreamDeltaState.AtomDelta();

                AtomDeltaValueID valueID;
                do {
                    valueID = (AtomDeltaValueID)stream.ReadByte();

                    if (valueID == AtomDeltaValueID.ScreenLocation) {
                        atomDelta.ScreenLocation = stream.ReadScreenLocation();
                    } else if (valueID == AtomDeltaValueID.IconAppearance) {
                        atomDelta.NewIconAppearanceID = (int)stream.ReadUInt32();
                    } else if (valueID != AtomDeltaValueID.End) {
                        throw new Exception("Invalid atom delta value ID in delta game state packet (" + valueID.ToString() + ")");
                    }
                } while (valueID != AtomDeltaValueID.End);

                DeltaState.AtomDeltas.Add(atomID, atomDelta);
            }
        }

        private void WriteAtomDeltasSection(PacketStream stream) {
            stream.WriteByte((byte)SectionID.AtomDeltas);
            stream.WriteUInt16((UInt16)DeltaState.AtomDeltas.Count);

            foreach (KeyValuePair<UInt32, DreamDeltaState.AtomDelta> atomDeltaPair in DeltaState.AtomDeltas) {
                UInt32 atomID = atomDeltaPair.Key;
                DreamDeltaState.AtomDelta atomDelta = atomDeltaPair.Value;

                stream.WriteUInt32(atomID);

                if (atomDelta.ScreenLocation.HasValue) {
                    stream.WriteByte((byte)AtomDeltaValueID.ScreenLocation);
                    stream.WriteScreenLocation(atomDelta.ScreenLocation.Value);
                }

                if (atomDelta.NewIconAppearanceID.HasValue) {
                    stream.WriteByte((byte)AtomDeltaValueID.IconAppearance);
                    stream.WriteUInt32((UInt32)atomDelta.NewIconAppearanceID.Value);
                }

                stream.WriteByte((byte)AtomDeltaValueID.End);
            }
        }

        private void ReadAtomLocationDeltasSection(PacketStream stream) {
            UInt32 atomLocationDeltaCount = stream.ReadUInt32();

            DeltaState.AtomLocationDeltas = new List<DreamDeltaState.AtomLocationDelta>();
            for (int i = 0; i < atomLocationDeltaCount; i++) {
                DreamDeltaState.AtomLocationDelta atomLocationDelta = new DreamDeltaState.AtomLocationDelta();

                atomLocationDelta.AtomID = stream.ReadUInt32();
                atomLocationDelta.LocationID = stream.ReadUInt32();
                DeltaState.AtomLocationDeltas.Add(atomLocationDelta);
            }
        }

        private void WriteAtomLocationDeltasSection(PacketStream stream) {
            stream.WriteByte((byte)SectionID.AtomLocationDeltas);
            stream.WriteUInt32((UInt32)DeltaState.AtomLocationDeltas.Count);

            foreach (DreamDeltaState.AtomLocationDelta atomLocationDelta in DeltaState.AtomLocationDeltas) {
                stream.WriteUInt32(atomLocationDelta.AtomID);
                stream.WriteUInt32(atomLocationDelta.LocationID);
            }
        }

        private void ReadTurfDeltasSection(PacketStream stream) {
            UInt32 turfDeltasCount = stream.ReadUInt32();

            for (int i = 0; i < turfDeltasCount; i++) {
                UInt16 x = stream.ReadUInt16();
                UInt16 y = stream.ReadUInt16();
                UInt32 turfAtomID = stream.ReadUInt32();

                DeltaState.TurfDeltas[(x, y)] = turfAtomID;
            }
        }

        private void WriteTurfDeltasSection(PacketStream stream) {
            stream.WriteByte((byte)SectionID.TurfDeltas);
            stream.WriteUInt32((UInt32)DeltaState.TurfDeltas.Count);

            foreach (KeyValuePair<(int X, int Y), UInt32> turfDelta in DeltaState.TurfDeltas) {
                stream.WriteUInt16((UInt16)turfDelta.Key.X);
                stream.WriteUInt16((UInt16)turfDelta.Key.Y);
                stream.WriteUInt32(turfDelta.Value);
            }
        }

        private void ReadClientSection(PacketStream stream) {
            ClientValueID valueID;
            ClientDelta = new DreamDeltaState.ClientDelta();

            do {
                valueID = (ClientValueID)stream.ReadByte();

                if (valueID == ClientValueID.Eye) {
                    ClientDelta.NewEyeID = stream.ReadUInt32();
                } else if (valueID == ClientValueID.ScreenObjectAdditions) {
                    UInt32 screenObjectAdditionCount = stream.ReadUInt32();

                    ClientDelta.ScreenObjectAdditions = new List<UInt32>();
                    for (int i = 0; i < screenObjectAdditionCount; i++) {
                        ClientDelta.ScreenObjectAdditions.Add(stream.ReadUInt32());
                    }
                } else if (valueID == ClientValueID.ScreenObjectRemovals) {
                    UInt32 screenObjectRemovalCount = stream.ReadUInt32();

                    ClientDelta.ScreenObjectRemovals = new List<UInt32>();
                    for (int i = 0; i < screenObjectRemovalCount; i++) {
                        ClientDelta.ScreenObjectRemovals.Add(stream.ReadUInt32());
                    }
                } else if (valueID != ClientValueID.End) {
                    throw new Exception("Invalid client value ID in delta game state packet (" + valueID.ToString() + ")");
                }
            } while (valueID != ClientValueID.End);
        }

        private void WriteClientSection(PacketStream stream) {
            bool newEye = ClientDelta.NewEyeID.HasValue;
            bool screenAdditions = (ClientDelta.ScreenObjectAdditions != null && ClientDelta.ScreenObjectAdditions.Count > 0);
            bool screenRemovals = (ClientDelta.ScreenObjectRemovals != null && ClientDelta.ScreenObjectRemovals.Count > 0);

            if (newEye || screenAdditions || screenRemovals) {
                stream.WriteByte((byte)SectionID.Client);

                if (newEye) {
                    stream.WriteByte((byte)ClientValueID.Eye);
                    stream.WriteUInt32(ClientDelta.NewEyeID.Value);
                }
                
                if (screenAdditions) {
                    stream.WriteByte((byte)ClientValueID.ScreenObjectAdditions);

                    stream.WriteUInt32((UInt32)ClientDelta.ScreenObjectAdditions.Count);
                    foreach (UInt32 screenObjectID in ClientDelta.ScreenObjectAdditions) {
                        stream.WriteUInt32(screenObjectID);
                    }
                }

                if (screenRemovals) {
                    stream.WriteByte((byte)ClientValueID.ScreenObjectRemovals);

                    stream.WriteUInt32((UInt32)ClientDelta.ScreenObjectRemovals.Count);
                    foreach (UInt32 screenObjectID in ClientDelta.ScreenObjectRemovals) {
                        stream.WriteUInt32(screenObjectID);
                    }
                }

                stream.WriteByte((byte)ClientValueID.End);
            }
        }
        
        private void ReadNewIconAppearancesSection(PacketStream stream) {
            UInt32 newIconAppearancesCount = stream.ReadUInt32();

            for (int i = 0; i < newIconAppearancesCount; i++) {
                DeltaState.NewIconAppearances.Add(IconAppearance.ReadFromPacket(stream));
            }
        }

        private void WriteNewIconAppearancesSection(PacketStream stream) {
            stream.WriteByte((byte)SectionID.NewIconAppearances);

            stream.WriteUInt32((UInt32)DeltaState.NewIconAppearances.Count);
            foreach (IconAppearance iconAppearance in DeltaState.NewIconAppearances) {
                iconAppearance.WriteToPacket(stream);
            }
        }
    }
}
