using System;
using System.Collections.Generic;

namespace OpenDreamShared.Dream {
    class DreamFullState {
        public class Atom {
            public UInt32 AtomID;
            public AtomType Type;
            public UInt32 LocationID;
            public int IconAppearanceID;
            public ScreenLocation ScreenLocation;
        }

        public class Client {
            public UInt32 EyeID = UInt32.MaxValue;
            public List<UInt32> ScreenObjects = new();
        }

        public UInt32 ID;
        public List<IconAppearance> IconAppearances = new();
        public Dictionary<UInt32, Atom> Atoms = new();
        public Dictionary<string, Client> Clients = new();
        public UInt32[,,] Turfs = new UInt32[0, 0, 0];

        public DreamFullState(UInt32 id) {
            ID = id;
        }

        public void ApplyDeltaState(DreamDeltaState deltaState) {
            foreach (IconAppearance iconAppearance in deltaState.NewIconAppearances) {
                IconAppearances.Add(iconAppearance);
            }

            foreach (KeyValuePair<UInt32, DreamDeltaState.AtomCreation> atomCreationPair in deltaState.AtomCreations) {
                DreamDeltaState.AtomCreation atomCreation = atomCreationPair.Value;
                Atom atom = new Atom();

                atom.AtomID = atomCreationPair.Key;
                atom.Type = atomCreation.Type;
                atom.LocationID = atomCreation.LocationID;
                atom.IconAppearanceID = atomCreation.IconAppearanceID;
                atom.ScreenLocation = atomCreation.ScreenLocation;

                Atoms[atom.AtomID] = atom;
            }

            foreach (DreamDeltaState.AtomLocationDelta atomLocationDelta in deltaState.AtomLocationDeltas) {
                Atoms[atomLocationDelta.AtomID].LocationID = atomLocationDelta.LocationID;
            }

            foreach (UInt32 atomDeletion in deltaState.AtomDeletions) {
                Atoms.Remove(atomDeletion);
            }

            foreach (KeyValuePair<UInt32, DreamDeltaState.AtomDelta> atomDeltaPair in deltaState.AtomDeltas) {
                UInt32 atomID = atomDeltaPair.Key;
                DreamDeltaState.AtomDelta atomDelta = atomDeltaPair.Value;
                Atom atom = Atoms[atomID];

                if (atomDelta.NewIconAppearanceID.HasValue) {
                    atom.IconAppearanceID = atomDelta.NewIconAppearanceID.Value;
                }

                if (atomDelta.ScreenLocation.HasValue) {
                    atom.ScreenLocation = atomDelta.ScreenLocation.Value;
                }
            }

            foreach (KeyValuePair<(int X, int Y, int Z), UInt32> turfDelta in deltaState.TurfDeltas) {
                Turfs[turfDelta.Key.X, turfDelta.Key.Y, turfDelta.Key.Z] = turfDelta.Value;
            }

            foreach (KeyValuePair<string, DreamDeltaState.ClientDelta> clientDelta in deltaState.ClientDeltas) {
                Client client;
                if (!Clients.TryGetValue(clientDelta.Key, out client)) {
                    client = new Client();

                    Clients[clientDelta.Key] = client;
                }

                if (clientDelta.Value.NewEyeID.HasValue) {
                    client.EyeID = clientDelta.Value.NewEyeID.Value;
                }

                if (clientDelta.Value.ScreenObjectAdditions != null) {
                    foreach (UInt32 screenObjectID in clientDelta.Value.ScreenObjectAdditions) {
                        client.ScreenObjects.Add(screenObjectID);
                    }
                }

                if (clientDelta.Value.ScreenObjectRemovals != null) {
                    foreach (UInt32 screenObjectID in clientDelta.Value.ScreenObjectRemovals) {
                        client.ScreenObjects.Remove(screenObjectID);
                    }
                }
            }
        }
    }
}
