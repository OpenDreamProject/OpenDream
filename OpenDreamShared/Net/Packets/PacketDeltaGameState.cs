using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Objects;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamShared.Net.Packets {
    class PacketDeltaGameState : IPacket {
        private enum SectionID {
            AtomCreations = 0x0,
            AtomDeletions = 0x1,
            AtomDeltas = 0x2,
            AtomLocationDeltas = 0x3,
            TurfDeltas = 0x4,
            Client = 0x5
        }

        private enum AtomDeltaValueID {
            End = 0x0,
            VisualProperties = 0x1,
            OverlayAdditions = 0x2,
            OverlayRemovals = 0x3,
            ScreenLocation = 0x4
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
            WriteClientSection(stream);
        }

        private void ReadAtomCreationsSection(PacketStream stream) {
            UInt16 atomCreationsCount = stream.ReadUInt16();

            for (int i = 0; i < atomCreationsCount; i++) {
                DreamDeltaState.AtomCreation atomCreation = new DreamDeltaState.AtomCreation(stream.ReadAtomID(), stream.ReadUInt16());

                atomCreation.LocationID = stream.ReadAtomID();
                atomCreation.VisualProperties = stream.ReadIconVisualProperties();
                atomCreation.Overlays = stream.ReadOverlays();
                if (ATOMBase.AtomBases[atomCreation.BaseID].Type == ATOMType.Movable) {
                    atomCreation.ScreenLocation = stream.ReadScreenLocation();
                }
                
                DeltaState.AtomCreations.Add(atomCreation);
            }
        }

        private void WriteAtomCreationsSection(PacketStream stream) {
            stream.WriteByte((byte)SectionID.AtomCreations);
            stream.WriteUInt16((UInt16)DeltaState.AtomCreations.Count);

            foreach (DreamDeltaState.AtomCreation atomCreation in DeltaState.AtomCreations) {
                stream.WriteAtomID(atomCreation.AtomID);
                stream.WriteUInt16(atomCreation.BaseID);
                stream.WriteAtomID(atomCreation.LocationID);
                stream.WriteIconVisualProperties(atomCreation.VisualProperties);
                stream.WriteOverlays(atomCreation.Overlays);
                if (ATOMBase.AtomBases[atomCreation.BaseID].Type == ATOMType.Movable) {
                    stream.WriteScreenLocation(atomCreation.ScreenLocation);
                }
            }
        }

        private void ReadAtomDeletionsSection(PacketStream stream) {
            UInt16 atomDeletionsCount = stream.ReadUInt16();

            for (int i = 0; i < atomDeletionsCount; i++) {
                DeltaState.AtomDeletions.Add(stream.ReadAtomID());
            }
        }

        private void WriteAtomDeletionsSection(PacketStream stream) {
            stream.WriteByte((byte)SectionID.AtomDeletions);
            stream.WriteUInt16((UInt16)DeltaState.AtomDeletions.Count);

            foreach (AtomID atomDeletion in DeltaState.AtomDeletions) {
                stream.WriteAtomID(atomDeletion);
            }
        }

        private void ReadAtomDeltasSection(PacketStream stream) {
            UInt16 atomDeltasCount = stream.ReadUInt16();

            for (int i = 0; i < atomDeltasCount; i++) {
                DreamDeltaState.AtomDelta atomDelta = new DreamDeltaState.AtomDelta(stream.ReadAtomID());

                AtomDeltaValueID valueID;
                do {
                    valueID = (AtomDeltaValueID)stream.ReadByte();

                    if (valueID == AtomDeltaValueID.VisualProperties) {
                        atomDelta.ChangedVisualProperties = stream.ReadIconVisualProperties();
                    } else if (valueID == AtomDeltaValueID.OverlayAdditions) {
                        atomDelta.OverlayAdditions = stream.ReadOverlays();
                    } else if (valueID == AtomDeltaValueID.OverlayRemovals) {
                        int overlayRemovalCount = stream.ReadByte();

                        atomDelta.OverlayRemovals = new List<UInt16>();
                        for (int k = 0; k < overlayRemovalCount; k++) {
                            atomDelta.OverlayRemovals.Add((UInt16)stream.ReadByte());
                        }
                    } else if (valueID == AtomDeltaValueID.ScreenLocation) {
                        atomDelta.ScreenLocation = stream.ReadScreenLocation();
                    } else if (valueID != AtomDeltaValueID.End) {
                        throw new Exception("Invalid atom delta value ID in delta game state packet (" + valueID.ToString() + ")");
                    }
                } while (valueID != AtomDeltaValueID.End);

                DeltaState.AtomDeltas.Add(atomDelta);
            }
        }

        private void WriteAtomDeltasSection(PacketStream stream) {
            stream.WriteByte((byte)SectionID.AtomDeltas);
            stream.WriteUInt16((UInt16)DeltaState.AtomDeltas.Count);

            foreach (DreamDeltaState.AtomDelta atomDelta in DeltaState.AtomDeltas) {
                stream.WriteAtomID(atomDelta.AtomID);

                if (atomDelta.ChangedVisualProperties.HasValue) {
                    stream.WriteByte((byte)AtomDeltaValueID.VisualProperties);
                    stream.WriteIconVisualProperties(atomDelta.ChangedVisualProperties.Value);
                }

                if (atomDelta.ScreenLocation.HasValue) {
                    stream.WriteByte((byte)AtomDeltaValueID.ScreenLocation);
                    stream.WriteScreenLocation(atomDelta.ScreenLocation.Value);
                }

                if (atomDelta.OverlayAdditions.Count > 0) {
                    stream.WriteByte((byte)AtomDeltaValueID.OverlayAdditions);
                    stream.WriteOverlays(atomDelta.OverlayAdditions);
                }

                if (atomDelta.OverlayRemovals.Count > 0) {
                    stream.WriteByte((byte)AtomDeltaValueID.OverlayRemovals);

                    stream.WriteByte((byte)atomDelta.OverlayRemovals.Count);
                    foreach (UInt16 overlayID in atomDelta.OverlayRemovals) {
                        stream.WriteByte((byte)overlayID);
                    }
                }

                stream.WriteByte((byte)AtomDeltaValueID.End);
            }
        }

        private void ReadAtomLocationDeltasSection(PacketStream stream) {
            UInt16 atomLocationDeltaCount = stream.ReadUInt16();

            DeltaState.AtomLocationDeltas = new List<DreamDeltaState.AtomLocationDelta>();
            for (int i = 0; i < atomLocationDeltaCount; i++) {
                DreamDeltaState.AtomLocationDelta atomLocationDelta = new DreamDeltaState.AtomLocationDelta();

                atomLocationDelta.AtomID = stream.ReadAtomID();
                atomLocationDelta.LocationID = stream.ReadAtomID();
                DeltaState.AtomLocationDeltas.Add(atomLocationDelta);
            }
        }

        private void WriteAtomLocationDeltasSection(PacketStream stream) {
            stream.WriteByte((byte)SectionID.AtomLocationDeltas);
            stream.WriteUInt16((UInt16)DeltaState.AtomLocationDeltas.Count);

            foreach (DreamDeltaState.AtomLocationDelta atomLocationDelta in DeltaState.AtomLocationDeltas) {
                stream.WriteAtomID(atomLocationDelta.AtomID);
                stream.WriteAtomID(atomLocationDelta.LocationID);
            }
        }

        private void ReadTurfDeltasSection(PacketStream stream) {
            UInt16 turfDeltasCount = stream.ReadUInt16();

            for (int i = 0; i < turfDeltasCount; i++) {
                DreamDeltaState.TurfDelta turfDelta = new DreamDeltaState.TurfDelta();
                turfDelta.X = stream.ReadUInt16();
                turfDelta.Y = stream.ReadUInt16();
                turfDelta.TurfAtomID = stream.ReadAtomID();

                DeltaState.TurfDeltas.Add(turfDelta);
            }
        }

        private void WriteTurfDeltasSection(PacketStream stream) {
            stream.WriteByte((byte)SectionID.TurfDeltas);
            stream.WriteUInt16((UInt16)DeltaState.TurfDeltas.Count);

            foreach (DreamDeltaState.TurfDelta turfDelta in DeltaState.TurfDeltas) {
                stream.WriteUInt16((UInt16)turfDelta.X);
                stream.WriteUInt16((UInt16)turfDelta.Y);
                stream.WriteAtomID(turfDelta.TurfAtomID);
            }
        }

        private void ReadClientSection(PacketStream stream) {
            ClientValueID valueID;
            ClientDelta = new DreamDeltaState.ClientDelta();

            do {
                valueID = (ClientValueID)stream.ReadByte();

                if (valueID == ClientValueID.Eye) {
                    ClientDelta.NewEyeID = stream.ReadAtomID();
                } else if (valueID == ClientValueID.ScreenObjectAdditions) {
                    UInt16 screenObjectAdditionCount = stream.ReadUInt16();

                    ClientDelta.ScreenObjectAdditions = new List<AtomID>();
                    for (int i = 0; i < screenObjectAdditionCount; i++) {
                        ClientDelta.ScreenObjectAdditions.Add(stream.ReadAtomID());
                    }
                } else if (valueID == ClientValueID.ScreenObjectRemovals) {
                    UInt16 screenObjectRemovalCount = stream.ReadUInt16();

                    ClientDelta.ScreenObjectRemovals = new List<AtomID>();
                    for (int i = 0; i < screenObjectRemovalCount; i++) {
                        ClientDelta.ScreenObjectRemovals.Add(stream.ReadAtomID());
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
                    stream.WriteAtomID(ClientDelta.NewEyeID.Value);
                }
                
                if (screenAdditions) {
                    stream.WriteByte((byte)ClientValueID.ScreenObjectAdditions);

                    stream.WriteUInt16((UInt16)ClientDelta.ScreenObjectAdditions.Count);
                    foreach (AtomID screenObjectID in ClientDelta.ScreenObjectAdditions) {
                        stream.WriteAtomID(screenObjectID);
                    }
                }

                if (screenRemovals) {
                    stream.WriteByte((byte)ClientValueID.ScreenObjectRemovals);

                    stream.WriteUInt16((UInt16)ClientDelta.ScreenObjectRemovals.Count);
                    foreach (AtomID screenObjectID in ClientDelta.ScreenObjectRemovals) {
                        stream.WriteAtomID(screenObjectID);
                    }
                }

                stream.WriteByte((byte)ClientValueID.End);
            }
        }
    }
}
