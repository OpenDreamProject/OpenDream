using OpenDreamShared.Dream.Objects;
using System;
using System.Collections.Generic;

namespace OpenDreamShared.Dream {
    class DreamDeltaState {
        public class AtomCreation {
            public AtomID AtomID;
            public UInt16 BaseID;
            public AtomID LocationID = AtomID.NullAtom;
            public IconVisualProperties VisualProperties = new IconVisualProperties();
            public Dictionary<UInt16, IconVisualProperties> Overlays = new();
            public ScreenLocation ScreenLocation = new ScreenLocation();

            public AtomCreation(AtomID atomID, UInt16 baseID) {
                AtomID = atomID;
                BaseID = baseID;
            }
        }

        public struct AtomLocationDelta {
            public AtomID AtomID;
            public AtomID LocationID;

            public AtomLocationDelta(AtomID atomID, AtomID locationID) {
                AtomID = atomID;
                LocationID = locationID;
            }
        }

        public class AtomDelta {
            public AtomID AtomID;
            public IconVisualProperties? ChangedVisualProperties = null;
            public Dictionary<UInt16, IconVisualProperties> OverlayAdditions = new();
            public List<UInt16> OverlayRemovals = new();
            public ScreenLocation? ScreenLocation;

            public AtomDelta(AtomID atomID) {
                AtomID = atomID;
            }
        }

        public struct TurfDelta {
            public int X, Y;
            public AtomID TurfAtomID;

            public TurfDelta(int x, int y, AtomID turfAtomID) {
                X = x;
                Y = y;
                TurfAtomID = turfAtomID;
            }
        }

        public class ClientDelta {
            public AtomID? NewEyeID;
            public List<AtomID> ScreenObjectAdditions;
            public List<AtomID> ScreenObjectRemovals;
        }

        public UInt32 ID;
        public List<AtomCreation> AtomCreations = new();
        public List<AtomID> AtomDeletions = new();
        public List<AtomLocationDelta> AtomLocationDeltas = new();
        public List<AtomDelta> AtomDeltas = new();
        public List<TurfDelta> TurfDeltas = new();
        public Dictionary<string, ClientDelta> ClientDeltas = new();

        public DreamDeltaState(UInt32 id) {
            ID = id;
        }

        public void AddAtomCreation(AtomID atomID, UInt16 baseID) {
            AtomCreations.Add(new AtomCreation(atomID, baseID));
        }

        public void AddAtomDeletion(AtomID atomID) {
            AtomDeletions.Add(atomID);
        }

        public void AddAtomLocationDelta(AtomID atomID, AtomID newLocationID) {
            AtomCreation atomCreation = GetAtomCreation(atomID);

            if (atomCreation != null) {
                atomCreation.LocationID = newLocationID;
            } else {
                AtomLocationDelta atomLocationDelta = new AtomLocationDelta(atomID, newLocationID);

                RemoveExistingAtomLocationDelta(atomID);
                AtomLocationDeltas.Add(atomLocationDelta);
            }
        }

        public void AddAtomIconDelta(AtomID atomID, string icon) {
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

        public void AddAtomIconStateDelta(AtomID atomID, string iconState) {
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

        public void AddAtomColorDelta(AtomID atomID, string color) {
            AtomCreation atomCreation = GetAtomCreation(atomID);

            if (atomCreation != null) {
                atomCreation.VisualProperties.SetColor(color);
            } else {
                AtomDelta atomDelta = GetAtomDelta(atomID);
                IconVisualProperties visualProperties = atomDelta.ChangedVisualProperties ?? new IconVisualProperties();

                visualProperties.SetColor(color);
                atomDelta.ChangedVisualProperties = visualProperties;
            }
        }

        public void AddAtomDirectionDelta(AtomID atomID, AtomDirection direction) {
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

        public void AddAtomOverlay(AtomID atomID, UInt16 overlayID, IconVisualProperties overlay) {
            AtomCreation atomCreation = GetAtomCreation(atomID);

            if (atomCreation != null) {
                atomCreation.Overlays[overlayID] = overlay;
            } else {
                GetAtomDelta(atomID).OverlayAdditions[overlayID] = overlay;
            }
        }

        public void RemoveAtomOverlay(AtomID atomID, UInt16 overlayID) {
            AtomCreation atomCreation = GetAtomCreation(atomID);

            if (atomCreation != null) {
                atomCreation.Overlays.Remove(overlayID);
            } else {
                GetAtomDelta(atomID).OverlayRemovals.Add(overlayID);
            }
        }

        public void AddAtomScreenLocDelta(AtomID atomID, ScreenLocation newScreenLoc) {
            AtomCreation atomCreation = GetAtomCreation(atomID);

            if (atomCreation != null) {
                atomCreation.ScreenLocation = newScreenLoc;
            } else {
                GetAtomDelta(atomID).ScreenLocation = newScreenLoc;
            }
        }

        public void AddTurfDelta(int x, int y, AtomID newTurfAtomID) {
            TurfDelta turfDelta = new TurfDelta(x, y, newTurfAtomID);

            RemoveExistingTurfDelta(x, y);
            TurfDeltas.Add(turfDelta);
        }

        public void AddClient(string ckey) {
            if (!ClientDeltas.ContainsKey(ckey)) {
                ClientDeltas[ckey] = new ClientDelta();
            }
        }

        public void AddClientEyeIDDelta(string ckey, AtomID? newClientEyeID) {
            ClientDelta clientDelta = GetClientDelta(ckey);

            clientDelta.NewEyeID = newClientEyeID;
        }

        public void AddClientScreenObject(string ckey, AtomID screenObjectID) {
            ClientDelta clientDelta = GetClientDelta(ckey);

            if (clientDelta.ScreenObjectAdditions == null) clientDelta.ScreenObjectAdditions = new List<AtomID>();
            if (clientDelta.ScreenObjectRemovals != null) clientDelta.ScreenObjectRemovals.Remove(screenObjectID);
            clientDelta.ScreenObjectAdditions.Add(screenObjectID);
        }

        public void RemoveClientScreenObject(string ckey, AtomID screenObjectID) {
            ClientDelta clientDelta = GetClientDelta(ckey);

            if (clientDelta.ScreenObjectRemovals == null) clientDelta.ScreenObjectRemovals = new List<AtomID>();
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

        private void RemoveExistingAtomLocationDelta(AtomID atomID) {
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

        private AtomCreation GetAtomCreation(AtomID atomID) {
            foreach (AtomCreation atomCreation in AtomCreations) {
                if (atomCreation.AtomID == atomID) return atomCreation;
            }

            return null;
        }

        private AtomDelta GetAtomDelta(AtomID atomID) {
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
