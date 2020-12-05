using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace OpenDreamShared.Dream {
    class DreamDeltaState {
        public class AtomCreation {
            public UInt16 AtomID;
            public UInt16 BaseID;
            public UInt16 LocationID = 0xFFFF;
            public IconVisualProperties VisualProperties = new IconVisualProperties();
            public Dictionary<UInt16, IconVisualProperties> Overlays = new();
            public ScreenLocation ScreenLocation = new ScreenLocation();

            public AtomCreation(UInt16 atomID, UInt16 baseID) {
                AtomID = atomID;
                BaseID = baseID;
            }
        }

        public struct AtomLocationDelta {
            public UInt16 AtomID;
            public UInt16 LocationID;

            public AtomLocationDelta(UInt16 atomID, UInt16 locationID) {
                AtomID = atomID;
                LocationID = locationID;
            }
        }

        public class AtomDelta {
            public UInt16 AtomID;
            public IconVisualProperties? ChangedVisualProperties = null;
            public Dictionary<UInt16, IconVisualProperties> OverlayAdditions = new();
            public List<UInt16> OverlayRemovals = new();
            public ScreenLocation? ScreenLocation;

            public AtomDelta(UInt16 atomID) {
                AtomID = atomID;
            }
        }

        public struct TurfDelta {
            public int X, Y;
            public UInt16 TurfAtomID;

            public TurfDelta(int x, int y, UInt16 turfAtomID) {
                X = x;
                Y = y;
                TurfAtomID = turfAtomID;
            }
        }

        public class ClientDelta {
            public UInt16? NewEyeID;
            public List<UInt16> ScreenObjectAdditions;
            public List<UInt16> ScreenObjectRemovals;
        }

        public UInt32 ID;
        public List<AtomCreation> AtomCreations = new List<AtomCreation>();
        public List<UInt16> AtomDeletions = new List<UInt16>();
        public List<AtomLocationDelta> AtomLocationDeltas = new List<AtomLocationDelta>();
        public List<AtomDelta> AtomDeltas = new List<AtomDelta>();
        public List<TurfDelta> TurfDeltas = new List<TurfDelta>();
        public Dictionary<string, ClientDelta> ClientDeltas = new Dictionary<string, ClientDelta>();

        public DreamDeltaState(UInt32 id) {
            ID = id;
        }

        public void AddAtomCreation(UInt16 atomID, UInt16 baseID) {
            AtomCreations.Add(new AtomCreation(atomID, baseID));
        }

        public void AddAtomDeletion(UInt16 atomID) {
            AtomDeletions.Add(atomID);
        }

        public void AddAtomLocationDelta(UInt16 atomID, UInt16 newLocationID) {
            AtomCreation atomCreation = GetAtomCreation(atomID);

            if (atomCreation != null) {
                atomCreation.LocationID = newLocationID;
            } else {
                AtomLocationDelta atomLocationDelta = new AtomLocationDelta(atomID, newLocationID);

                RemoveExistingAtomLocationDelta(atomID);
                AtomLocationDeltas.Add(atomLocationDelta);
            }
        }

        public void AddAtomIconDelta(UInt16 atomID, string icon) {
            AtomCreation atomCreation = GetAtomCreation(atomID);

            if (atomCreation != null) {
                atomCreation.VisualProperties.Icon = icon;
            } else {
                AtomDelta atomDelta = GetAtomDelta(atomID);
                IconVisualProperties visualProperties = atomDelta.ChangedVisualProperties ?? new IconVisualProperties();

                visualProperties.Icon = icon;
                atomDelta.ChangedVisualProperties = visualProperties;
            }
        }

        public void AddAtomIconStateDelta(UInt16 atomID, string iconState) {
            AtomCreation atomCreation = GetAtomCreation(atomID);

            if (atomCreation != null) {
                atomCreation.VisualProperties.IconState = iconState;
            } else {
                AtomDelta atomDelta = GetAtomDelta(atomID);
                IconVisualProperties visualProperties = atomDelta.ChangedVisualProperties ?? new IconVisualProperties();

                visualProperties.IconState = iconState;
                atomDelta.ChangedVisualProperties = visualProperties;
            }
        }

        public void AddAtomDirectionDelta(UInt16 atomID, AtomDirection direction) {
            AtomCreation atomCreation = GetAtomCreation(atomID);

            if (atomCreation != null) {
                atomCreation.VisualProperties.Direction = direction;
            } else {
                AtomDelta atomDelta = GetAtomDelta(atomID);
                IconVisualProperties visualProperties = atomDelta.ChangedVisualProperties ?? new IconVisualProperties();

                visualProperties.Direction = direction;
                atomDelta.ChangedVisualProperties = visualProperties;
            }
        }

        public void AddAtomOverlay(UInt16 atomID, UInt16 overlayID, IconVisualProperties overlay) {
            AtomCreation atomCreation = GetAtomCreation(atomID);

            if (atomCreation != null) {
                atomCreation.Overlays[overlayID] = overlay;
            } else {
                GetAtomDelta(atomID).OverlayAdditions[overlayID] = overlay;
            }
        }

        public void RemoveAtomOverlay(UInt16 atomID, UInt16 overlayID) {
            AtomCreation atomCreation = GetAtomCreation(atomID);

            if (atomCreation != null) {
                atomCreation.Overlays.Remove(overlayID);
            } else {
                GetAtomDelta(atomID).OverlayRemovals.Add(overlayID);
            }
        }

        public void AddAtomScreenLocDelta(UInt16 atomID, ScreenLocation newScreenLoc) {
            AtomCreation atomCreation = GetAtomCreation(atomID);

            if (atomCreation != null) {
                atomCreation.ScreenLocation = newScreenLoc;
            } else {
                GetAtomDelta(atomID).ScreenLocation = newScreenLoc;
            }
        }

        public void AddTurfDelta(int x, int y, UInt16 newTurfAtomID) {
            TurfDelta turfDelta = new TurfDelta(x, y, newTurfAtomID);

            RemoveExistingTurfDelta(x, y);
            TurfDeltas.Add(turfDelta);
        }

        public void AddClient(string ckey) {
            if (!ClientDeltas.ContainsKey(ckey)) {
                ClientDeltas[ckey] = new ClientDelta();
            }
        }

        public void AddClientEyeIDDelta(string ckey, UInt16 newClientEyeID) {
            ClientDelta clientDelta = GetClientDelta(ckey);

            clientDelta.NewEyeID = newClientEyeID;
        }

        public void AddClientScreenObject(string ckey, UInt16 screenObjectID) {
            ClientDelta clientDelta = GetClientDelta(ckey);

            if (clientDelta.ScreenObjectAdditions == null) clientDelta.ScreenObjectAdditions = new List<UInt16>();
            if (clientDelta.ScreenObjectRemovals != null) clientDelta.ScreenObjectRemovals.Remove(screenObjectID);
            clientDelta.ScreenObjectAdditions.Add(screenObjectID);
        }

        public void RemoveClientScreenObject(string ckey, UInt16 screenObjectID) {
            ClientDelta clientDelta = GetClientDelta(ckey);

            if (clientDelta.ScreenObjectRemovals == null) clientDelta.ScreenObjectRemovals = new List<UInt16>();
            if (clientDelta.ScreenObjectAdditions != null) clientDelta.ScreenObjectAdditions.Remove(screenObjectID);
            clientDelta.ScreenObjectRemovals.Add(screenObjectID);
        }

        public bool ContainsChanges() {
            return (AtomCreations.Count > 0)
                    ||(AtomDeletions.Count > 0)
                    || (AtomLocationDeltas.Count > 0)
                    || (AtomDeltas.Count > 0)
                    || (TurfDeltas.Count > 0)
                    || (ClientDeltas.Count > 0);
        }

        private void RemoveExistingAtomLocationDelta(UInt16 atomID) {
            for (int i = 0; i < AtomLocationDeltas.Count; i++) {
                AtomLocationDelta existingAtomLocationDelta = AtomLocationDeltas[i];

                if (existingAtomLocationDelta.AtomID == atomID) {
                    AtomLocationDeltas.RemoveAt(i);

                    return;
                }
            }
        }

        private void RemoveExistingTurfDelta(int x, int y) {
            for (int i = 0; i < TurfDeltas.Count; i++) {
                TurfDelta existingTurfDelta = TurfDeltas[i];

                if (existingTurfDelta.X == x && existingTurfDelta.Y == y) {
                    TurfDeltas.RemoveAt(i);

                    return;
                }
            }
        }

        private AtomCreation GetAtomCreation(UInt16 atomID) {
            foreach (AtomCreation atomCreation in AtomCreations) {
                if (atomCreation.AtomID == atomID) return atomCreation;
            }

            return null;
        }

        private AtomDelta GetAtomDelta(UInt16 atomID) {
            foreach (AtomDelta existingAtomDelta in AtomDeltas) {
                if (existingAtomDelta.AtomID == atomID) return existingAtomDelta;
            }

            AtomDelta atomDelta = new AtomDelta(atomID);
            AtomDeltas.Add(atomDelta);
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
