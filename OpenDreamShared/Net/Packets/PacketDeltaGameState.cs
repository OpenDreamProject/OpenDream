using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamShared.Net.Packets {
    class PacketDeltaGameState : IPacket {
        private enum PacketDeltaGameStateSectionID {
            AtomCreations = 0x0,
            AtomDeletions = 0x1,
            AtomDeltas = 0x2,
            AtomLocationDeltas = 0x3,
            TurfDeltas = 0x4,
            Client = 0x5
        }

        private enum PacketDeltaGameStateAtomDeltaValueID {
            End = 0x0,
            VisualProperties = 0x1,
            OverlayAdditions = 0x2,
            OverlayRemovals = 0x3,
            ScreenLocation = 0x4
        }

        private enum PacketDeltaGameStateClientValueID {
            End = 0x0,
            Eye = 0x1,
            ScreenObjectAdditions = 0x2,
            ScreenObjectRemovals = 0x3
        }

        public struct PacketDeltaGameStateTurfDelta {
            public UInt16 X, Y;
            public UInt16 AtomID;
        }

        public PacketID PacketID => PacketID.DeltaGameState;
        public DreamDeltaState DeltaState;
        public PacketDeltaGameStateTurfDelta[] TurfDeltas = null;
        public bool HasNewEye = false;
        public UInt16 NewEyeAtomID = 0xFFFF;
        public UInt16[] ScreenObjectAdditions;
        public UInt16[] ScreenObjectRemovals;

        public PacketDeltaGameState() { }

        public PacketDeltaGameState(DreamDeltaState deltaState) {
            DeltaState = deltaState;
        }

        public void ReadFromStream(PacketStream stream) {
            DeltaState = new DreamDeltaState(stream.ReadUInt32());

            while (stream.Position < stream.Length) {
                PacketDeltaGameStateSectionID sectionID = (PacketDeltaGameStateSectionID)stream.ReadByte();

                switch (sectionID) {
                    case PacketDeltaGameStateSectionID.AtomCreations: ReadAtomCreationsSection(stream); break;
                    case PacketDeltaGameStateSectionID.AtomDeletions: ReadAtomDeletionsSection(stream); break;
                    case PacketDeltaGameStateSectionID.AtomDeltas: ReadAtomDeltasSection(stream); break;
                    case PacketDeltaGameStateSectionID.AtomLocationDeltas: ReadAtomLocationDeltasSection(stream); break;
                    case PacketDeltaGameStateSectionID.TurfDeltas: ReadTurfDeltasSection(stream); break;
                    case PacketDeltaGameStateSectionID.Client: ReadClientSection(stream); break;
                    default: throw new Exception("Invalid section ID in delta game state packet (" + sectionID.ToString() + ")");
                }
            }
        }

        public void WriteToStream(PacketStream stream) {
            stream.WriteUInt32(DeltaState.ID);

            if (DeltaState.AtomCreations.Count > 0) WriteAtomCreationsSection(stream);
            if (DeltaState.AtomDeletions.Count > 0) WriteAtomDeletionsSection(stream);
            if (DeltaState.AtomDeltas.Count > 0) WriteAtomDeltasSection(stream);
            if (DeltaState.AtomLocationDeltas.Count > 0) WriteAtomLocationDeltasSection(stream);
        }

        private void ReadAtomCreationsSection(PacketStream stream) {
            UInt16 atomCreationsCount = stream.ReadUInt16();

            for (int i = 0; i < atomCreationsCount; i++) {
                DreamDeltaState.AtomCreation atomCreation = new DreamDeltaState.AtomCreation();

                atomCreation.AtomID = stream.ReadUInt16();
                atomCreation.BaseID = stream.ReadUInt16();
                atomCreation.LocationID = stream.ReadUInt16();
                atomCreation.VisualProperties = stream.ReadIconVisualProperties();
                atomCreation.Overlays = stream.ReadOverlays();
                if (ATOMBase.AtomBases[atomCreation.BaseID].Type == ATOMType.Movable) {
                    atomCreation.ScreenLocation = new Point(stream.ReadUInt16(), stream.ReadUInt16());
                }
                
                DeltaState.AtomCreations.Add(atomCreation);
            }
        }

        private void WriteAtomCreationsSection(PacketStream stream) {
            stream.WriteByte((byte)PacketDeltaGameStateSectionID.AtomCreations);
            stream.WriteUInt16((UInt16)DeltaState.AtomCreations.Count);

            foreach (DreamDeltaState.AtomCreation atomCreation in DeltaState.AtomCreations) {
                stream.WriteUInt16(atomCreation.AtomID);
                stream.WriteUInt16(atomCreation.BaseID);
                stream.WriteUInt16(atomCreation.LocationID);
                stream.WriteIconVisualProperties(atomCreation.VisualProperties);
                stream.WriteOverlays(atomCreation.Overlays);
                if (ATOMBase.AtomBases[atomCreation.BaseID].Type == ATOMType.Movable) {
                    stream.WriteUInt16((UInt16)atomCreation.ScreenLocation.X);
                    stream.WriteUInt16((UInt16)atomCreation.ScreenLocation.Y);
                }
            }
        }

        private void ReadAtomDeletionsSection(PacketStream stream) {
            UInt16 atomDeletionsCount = stream.ReadUInt16();

            for (int i = 0; i < atomDeletionsCount; i++) {
                DeltaState.AtomDeletions.Add(stream.ReadUInt16());
            }
        }

        private void WriteAtomDeletionsSection(PacketStream stream) {
            stream.WriteUInt16((UInt16)DeltaState.AtomDeletions.Count);

            foreach (UInt16 atomDeletion in DeltaState.AtomDeletions) {
                stream.WriteUInt16(atomDeletion);
            }
        }

        private void ReadAtomDeltasSection(PacketStream stream) {
            UInt16 atomDeltasCount = stream.ReadUInt16();

            for (int i = 0; i < atomDeltasCount; i++) {
                DreamDeltaState.AtomDelta atomDelta = new DreamDeltaState.AtomDelta();
                atomDelta.AtomID = stream.ReadUInt16();
                atomDelta.HasChangedScreenLocation = false;

                PacketDeltaGameStateAtomDeltaValueID valueID;
                do {
                    valueID = (PacketDeltaGameStateAtomDeltaValueID)stream.ReadByte();

                    if (valueID == PacketDeltaGameStateAtomDeltaValueID.VisualProperties) {
                        atomDelta.ChangedVisualProperties = stream.ReadIconVisualProperties();
                    } else if (valueID == PacketDeltaGameStateAtomDeltaValueID.OverlayAdditions) {
                        atomDelta.OverlayAdditions = stream.ReadOverlays();
                    } else if (valueID == PacketDeltaGameStateAtomDeltaValueID.OverlayRemovals) {
                        int overlayRemovalCount = stream.ReadByte();

                        atomDelta.OverlayRemovals = new List<UInt16>();
                        for (int k = 0; k < overlayRemovalCount; k++) {
                            atomDelta.OverlayRemovals.Add((UInt16)stream.ReadByte());
                        }
                    } else if (valueID == PacketDeltaGameStateAtomDeltaValueID.ScreenLocation) {
                        atomDelta.HasChangedScreenLocation = true;
                        atomDelta.ScreenLocation = new Point(stream.ReadUInt16(), stream.ReadUInt16());
                    } else if (valueID != PacketDeltaGameStateAtomDeltaValueID.End) {
                        throw new Exception("Invalid atom delta value ID in delta game state packet (" + valueID.ToString() + ")");
                    }
                } while (valueID != PacketDeltaGameStateAtomDeltaValueID.End);

                DeltaState.AtomDeltas.Add(atomDelta);
            }
        }

        private void WriteAtomDeltasSection(PacketStream stream) {
            stream.WriteByte((byte)PacketDeltaGameStateSectionID.AtomDeltas);
            stream.WriteUInt16((UInt16)DeltaState.AtomDeltas.Count);

            int wrote = 0;
            foreach (DreamDeltaState.AtomDelta atomDelta in DeltaState.AtomDeltas) {
                wrote++;
                stream.WriteUInt16(atomDelta.AtomID);

                if (!atomDelta.ChangedVisualProperties.IsDefault()) {
                    stream.WriteByte((byte)PacketDeltaGameStateAtomDeltaValueID.VisualProperties);
                    stream.WriteIconVisualProperties(atomDelta.ChangedVisualProperties);
                }

                stream.WriteByte((byte)PacketDeltaGameStateAtomDeltaValueID.End);
            }
        }

        private void ReadAtomLocationDeltasSection(PacketStream stream) {
            UInt16 atomLocationDeltaCount = stream.ReadUInt16();

            DeltaState.AtomLocationDeltas = new List<DreamDeltaState.AtomLocationDelta>();
            for (int i = 0; i < atomLocationDeltaCount; i++) {
                DreamDeltaState.AtomLocationDelta atomLocationDelta = new DreamDeltaState.AtomLocationDelta();

                atomLocationDelta.AtomID = stream.ReadUInt16();
                atomLocationDelta.LocationID = stream.ReadUInt16();
                DeltaState.AtomLocationDeltas.Add(atomLocationDelta);
            }
        }

        private void WriteAtomLocationDeltasSection(PacketStream stream) {
            stream.WriteByte((byte)PacketDeltaGameStateSectionID.AtomLocationDeltas);
            stream.WriteUInt16((UInt16)DeltaState.AtomLocationDeltas.Count);

            foreach (DreamDeltaState.AtomLocationDelta atomLocationDelta in DeltaState.AtomLocationDeltas) {
                stream.WriteUInt16(atomLocationDelta.AtomID);
                stream.WriteUInt16(atomLocationDelta.LocationID);
            }
        }

        private void ReadTurfDeltasSection(PacketStream stream) {
            UInt16 turfDeltasCount = stream.ReadUInt16();

            TurfDeltas = new PacketDeltaGameStateTurfDelta[turfDeltasCount];
            for (int i = 0; i < turfDeltasCount; i++) {
                PacketDeltaGameStateTurfDelta turfDelta = new PacketDeltaGameStateTurfDelta();
                turfDelta.X = stream.ReadUInt16();
                turfDelta.Y = stream.ReadUInt16();
                turfDelta.AtomID = stream.ReadUInt16();

                TurfDeltas[i] = turfDelta;
            }
        }

        private void ReadClientSection(PacketStream stream) {
            PacketDeltaGameStateClientValueID valueID;

            do {
                valueID = (PacketDeltaGameStateClientValueID)stream.ReadByte();

                if (valueID == PacketDeltaGameStateClientValueID.Eye) {
                    HasNewEye = true;
                    NewEyeAtomID = stream.ReadUInt16();
                } else if (valueID == PacketDeltaGameStateClientValueID.ScreenObjectAdditions) {
                    UInt16 screenObjectAdditionCount = stream.ReadUInt16();

                    ScreenObjectAdditions = new ushort[screenObjectAdditionCount];
                    for (int i = 0; i < screenObjectAdditionCount; i++) {
                        ScreenObjectAdditions[i] = stream.ReadUInt16();
                    }
                } else if (valueID == PacketDeltaGameStateClientValueID.ScreenObjectRemovals) {
                    UInt16 screenObjectRemovalCount = stream.ReadUInt16();

                    ScreenObjectRemovals = new ushort[screenObjectRemovalCount];
                    for (int i = 0; i < screenObjectRemovalCount; i++) {
                        ScreenObjectRemovals[i] = stream.ReadUInt16();
                    }
                } else if (valueID != PacketDeltaGameStateClientValueID.End) {
                    throw new Exception("Invalid client value ID in delta game state packet (" + valueID.ToString() + ")");
                }
            } while (valueID != PacketDeltaGameStateClientValueID.End);
        }
    }
}
