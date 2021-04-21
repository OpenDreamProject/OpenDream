using System;
using System.Collections.Generic;

namespace OpenDreamShared.Dream {
    class DreamDeltaState {
        public class AtomCreation {
            public AtomType Type;
            public UInt32 LocationID = UInt32.MaxValue;
            public int IconAppearanceID;
            public ScreenLocation ScreenLocation = new ScreenLocation();

            public AtomCreation(AtomType type, int appearanceID) {
                Type = type;
                IconAppearanceID = appearanceID;
            }
        }

        public struct AtomLocationDelta {
            public UInt32 AtomID;
            public UInt32 LocationID;

            public AtomLocationDelta(UInt32 atomID, UInt32 locationID) {
                AtomID = atomID;
                LocationID = locationID;
            }
        }

        public class AtomDelta {
            public int? NewIconAppearanceID = null;
            public ScreenLocation? ScreenLocation;
        }

        public class ClientDelta {
            public UInt32? NewEyeID;
            public List<UInt32> ScreenObjectAdditions;
            public List<UInt32> ScreenObjectRemovals;
        }

        public UInt32 ID;
        public List<IconAppearance> NewIconAppearances = new();
        public Dictionary<UInt32, AtomCreation> AtomCreations = new();
        public List<UInt32> AtomDeletions = new();
        public List<AtomLocationDelta> AtomLocationDeltas = new();
        public Dictionary<UInt32, AtomDelta> AtomDeltas = new();
        public Dictionary<(int X, int Y, int Z), UInt32> TurfDeltas = new();
        public Dictionary<string, ClientDelta> ClientDeltas = new();

        public DreamDeltaState(UInt32 id) {
            ID = id;
        }

        public void AddIconAppearance(IconAppearance iconAppearance) {
            NewIconAppearances.Add(iconAppearance);
        }

        public void AddAtomCreation(UInt32 atomID, AtomType type, int appearanceId) {
            AtomCreations.Add(atomID, new AtomCreation(type, appearanceId));
        }

        public void AddAtomDeletion(UInt32 atomID) {
            if (AtomCreations.ContainsKey(atomID)) AtomCreations.Remove(atomID);
            else AtomDeletions.Add(atomID);
        }

        public void AddAtomLocationDelta(UInt32 atomID, UInt32 newLocationID) {
            if (AtomCreations.TryGetValue(atomID, out AtomCreation atomCreation)) {
                atomCreation.LocationID = newLocationID;
            } else {
                AtomLocationDelta atomLocationDelta = new AtomLocationDelta(atomID, newLocationID);

                RemoveExistingAtomLocationDelta(atomID);
                AtomLocationDeltas.Add(atomLocationDelta);
            }
        }

        public void AddAtomIconAppearanceDelta(UInt32 atomID, int iconAppearanceID) {
            if (AtomCreations.TryGetValue(atomID, out AtomCreation atomCreation)) {
                atomCreation.IconAppearanceID = iconAppearanceID;
            } else {
                AtomDelta atomDelta = GetAtomDelta(atomID);

                atomDelta.NewIconAppearanceID = iconAppearanceID;
            }
        }

        public void AddAtomScreenLocDelta(UInt32 atomID, ScreenLocation newScreenLoc) {
            if (AtomCreations.TryGetValue(atomID, out AtomCreation atomCreation)) {
                atomCreation.ScreenLocation = newScreenLoc;
            } else {
                GetAtomDelta(atomID).ScreenLocation = newScreenLoc;
            }
        }

        public void AddTurfDelta(int x, int y, int z, UInt32 newTurfAtomID) {
            TurfDeltas[(x, y, z)] = newTurfAtomID;
        }

        public void AddClient(string ckey) {
            if (!ClientDeltas.ContainsKey(ckey)) {
                ClientDeltas[ckey] = new ClientDelta();
            }
        }

        public void AddClientEyeIDDelta(string ckey, UInt32 newClientEyeID) {
            ClientDelta clientDelta = GetClientDelta(ckey);

            clientDelta.NewEyeID = newClientEyeID;
        }

        public void AddClientScreenObject(string ckey, UInt32 screenObjectID) {
            ClientDelta clientDelta = GetClientDelta(ckey);

            if (clientDelta.ScreenObjectAdditions == null) clientDelta.ScreenObjectAdditions = new List<UInt32>();
            if (clientDelta.ScreenObjectRemovals != null) clientDelta.ScreenObjectRemovals.Remove(screenObjectID);
            clientDelta.ScreenObjectAdditions.Add(screenObjectID);
        }

        public void RemoveClientScreenObject(string ckey, UInt32 screenObjectID) {
            ClientDelta clientDelta = GetClientDelta(ckey);

            if (clientDelta.ScreenObjectRemovals == null) clientDelta.ScreenObjectRemovals = new List<UInt32>();
            if (clientDelta.ScreenObjectAdditions != null) clientDelta.ScreenObjectAdditions.Remove(screenObjectID);
            clientDelta.ScreenObjectRemovals.Add(screenObjectID);
        }

        public bool ContainsChanges() {
            return (NewIconAppearances.Count > 0)
                    || (AtomCreations.Count > 0)
                    ||(AtomDeletions.Count > 0)
                    || (AtomLocationDeltas.Count > 0)
                    || (AtomDeltas.Count > 0)
                    || (TurfDeltas.Count > 0)
                    || (ClientDeltas.Count > 0);
        }

        private void RemoveExistingAtomLocationDelta(UInt32 atomID) {
            for (int i = 0; i < AtomLocationDeltas.Count; i++) {
                AtomLocationDelta existingAtomLocationDelta = AtomLocationDeltas[i];

                if (existingAtomLocationDelta.AtomID == atomID) {
                    AtomLocationDeltas.RemoveAt(i);

                    return;
                }
            }
        }

        private AtomDelta GetAtomDelta(UInt32 atomID) {
            AtomDelta atomDelta;

            if (!AtomDeltas.TryGetValue(atomID, out atomDelta)) {
                atomDelta = new AtomDelta();

                AtomDeltas.Add(atomID, atomDelta);
            }

            return atomDelta;
        }

        private ClientDelta GetClientDelta(string ckey) {
            ClientDelta clientDelta;
            if (!ClientDeltas.TryGetValue(ckey, out clientDelta)) {
                clientDelta = new ClientDelta();

                ClientDeltas.Add(ckey, clientDelta);
            }

            return clientDelta;
        }
    }
}
