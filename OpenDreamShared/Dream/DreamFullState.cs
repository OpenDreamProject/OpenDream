using System;
using System.Collections.Generic;

namespace OpenDreamShared.Dream {
    public class DreamFullState {
        public class Atom {
            public UInt32 AtomID;
            public AtomType Type;
            public UInt32 LocationID;
            public int IconAppearanceID;
            public ScreenLocation ScreenLocation;
        }

        public class Client {
            public UInt32 EyeID = UInt32.MaxValue;
            public ClientPerspective Perspective = ClientPerspective.Mob;
            public List<UInt32> ScreenObjects = new();
        }

        public struct Level {
            public UInt32[,] Turfs;

            public Level(int width, int height) {
                Turfs = new uint[width, height];
            }
        }

        public UInt32 ID;
        public List<IconAppearance> IconAppearances = new();
        public Dictionary<UInt32, Atom> Atoms = new();
        public Dictionary<string, Client> Clients = new();
        public List<Level> Levels = new();

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

                atom.IconAppearanceID = atomDelta.NewIconAppearanceID ?? atom.IconAppearanceID;
                atom.ScreenLocation = atomDelta.ScreenLocation ?? atom.ScreenLocation;
            }

            foreach (KeyValuePair<(int X, int Y, int Z), UInt32> turfDelta in deltaState.TurfDeltas) {
                if (turfDelta.Key.Z >= Levels.Count) {
                    while (Levels.Count <= turfDelta.Key.Z) Levels.Add(new Level(Levels[0].Turfs.GetLength(0), Levels[0].Turfs.GetLength(1)));
                }

                Levels[turfDelta.Key.Z].Turfs[turfDelta.Key.X, turfDelta.Key.Y] = turfDelta.Value;
            }

            foreach (KeyValuePair<string, DreamDeltaState.ClientDelta> clientDelta in deltaState.ClientDeltas) {
                Client client;
                if (!Clients.TryGetValue(clientDelta.Key, out client)) {
                    client = new Client();

                    Clients[clientDelta.Key] = client;
                }

                client.EyeID = clientDelta.Value.NewEyeID ?? client.EyeID;
                client.Perspective = clientDelta.Value.NewPerspective ?? client.Perspective;

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
