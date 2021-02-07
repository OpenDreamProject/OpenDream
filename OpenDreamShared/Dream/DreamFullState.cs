using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenDreamShared.Dream {
    class DreamFullState {
        public struct Atom {
            public UInt32 AtomID;
            public AtomType Type;
            public UInt32 LocationID;
            public int IconAppearanceID;
            public ScreenLocation ScreenLocation;
        }

        public class Client {
            public UInt32 EyeID = UInt32.MaxValue;
            public List<UInt32> ScreenObjects = new();

            public Client CreateCopy() {
                return new Client() {
                    EyeID = this.EyeID,
                    ScreenObjects = this.ScreenObjects
                };
            }
        }

        public UInt32 ID;
        public List<IconAppearance> IconAppearances = new();
        public Dictionary<UInt32, Atom> Atoms = new();
        public Dictionary<string, Client> Clients = new();
        public UInt32[,] Turfs = new UInt32[0, 0];

        public DreamFullState(UInt32 id) {
            ID = id;
        }

        public void SetFromFullState(DreamFullState fullState) {
            foreach (IconAppearance iconAppearance in fullState.IconAppearances) {
                IconAppearances.Add(iconAppearance);
            }

            foreach (KeyValuePair<UInt32, Atom> atom in fullState.Atoms) {
                Atoms.Add(atom.Key, atom.Value);
            }

            foreach (KeyValuePair<string, Client> client in fullState.Clients) {
                Clients.Add(client.Key, client.Value.CreateCopy());
            }

            Turfs = fullState.Turfs;
        }

        public void ApplyDeltaStates(List<DreamDeltaState> deltaStates) {
            foreach (DreamDeltaState deltaState in deltaStates) {
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
                    Atom atom = Atoms[atomLocationDelta.AtomID];

                    atom.LocationID = atomLocationDelta.LocationID;
                    Atoms[atomLocationDelta.AtomID] = atom;
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

                    Atoms[atomID] = atom;
                }

                foreach (KeyValuePair<(int X, int Y), UInt32> turfDelta in deltaState.TurfDeltas) {
                    Turfs[turfDelta.Key.X, turfDelta.Key.Y] = turfDelta.Value;
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
}
