using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace OpenDreamShared.Dream {
    class DreamDeltaState {
        public struct AtomCreation {
            public UInt16 AtomID;
            public UInt16 BaseID;
            public UInt16 LocationID;
            public IconVisualProperties VisualProperties;
            public Dictionary<UInt16, IconVisualProperties> Overlays;
            public Point ScreenLocation;
        }

        public struct AtomLocationDelta {
            public UInt16 AtomID;
            public UInt16 LocationID;

            public AtomLocationDelta(UInt16 atomID, UInt16 locationID) {
                AtomID = atomID;
                LocationID = locationID;
            }
        }

        public struct AtomDelta {
            public UInt16 AtomID;
            public IconVisualProperties ChangedVisualProperties;
            public Dictionary<UInt16, IconVisualProperties> OverlayAdditions;
            public List<UInt16> OverlayRemovals;
            public bool HasChangedScreenLocation;
            public Point ScreenLocation;

            public AtomDelta(UInt16 atomID) {
                AtomID = atomID;
                ChangedVisualProperties = new IconVisualProperties();
                OverlayAdditions = new Dictionary<ushort, IconVisualProperties>();
                OverlayRemovals = new List<UInt16>();
                HasChangedScreenLocation = false;
                ScreenLocation = new Point(0, 0);
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

        public UInt32 ID;
        public List<AtomCreation> AtomCreations = new List<AtomCreation>();
        public List<UInt16> AtomDeletions = new List<UInt16>();
        public List<AtomLocationDelta> AtomLocationDeltas = new List<AtomLocationDelta>();
        public List<AtomDelta> AtomDeltas = new List<AtomDelta>();
        public List<TurfDelta> TurfDeltas = new List<TurfDelta>();

        public DreamDeltaState(UInt32 id) {
            ID = id;
        }

        public void AddAtomCreation(UInt16 atomID, UInt16 baseID) {
            DreamDeltaState.AtomCreation atomCreation = new DreamDeltaState.AtomCreation();
            atomCreation.AtomID = atomID;
            atomCreation.BaseID = baseID;
            atomCreation.LocationID = 0xFFFF;
            atomCreation.VisualProperties = new IconVisualProperties();

            AtomCreations.Add(atomCreation);
        }

        public void AddAtomDeletion(UInt16 atomID) {
            AtomDeletions.Add(atomID);
        }

        public void AddAtomLocationDelta(UInt16 atomID, UInt16 newLocationID) {
            int existingAtomCreationIndex = GetExistingAtomCreationIndex(atomID);

            if (existingAtomCreationIndex == -1) {
                AtomLocationDelta atomLocationDelta = new AtomLocationDelta(atomID, newLocationID);

                RemoveExistingAtomLocationDelta(atomID);
                AtomLocationDeltas.Add(atomLocationDelta);
            } else {
                AtomCreation atomCreation = AtomCreations[existingAtomCreationIndex];

                atomCreation.LocationID = newLocationID;
                AtomCreations[existingAtomCreationIndex] = atomCreation;
            }
        }

        public void AddAtomIconStateDelta(UInt16 atomID, string iconState) {
            int existingAtomCreationIndex = GetExistingAtomCreationIndex(atomID);

            if (existingAtomCreationIndex == -1) {
                int existingAtomDeltaIndex = GetExistingAtomDeltaIndex(atomID);

                if (existingAtomDeltaIndex == -1) {
                    AtomDelta atomDelta = new AtomDelta(atomID);

                    atomDelta.ChangedVisualProperties.IconState = iconState;
                    AtomDeltas.Add(atomDelta);
                } else {
                    AtomDelta atomDelta = AtomDeltas[existingAtomDeltaIndex];

                    atomDelta.ChangedVisualProperties.IconState = iconState;
                    AtomDeltas[existingAtomDeltaIndex] = atomDelta;
                }
                
            } else {
                AtomCreation atomCreation = AtomCreations[existingAtomCreationIndex];

                atomCreation.VisualProperties.IconState = iconState;
                AtomCreations[existingAtomCreationIndex] = atomCreation;
            }
        }

        public void AddTurfDelta(int x, int y, UInt16 newTurfAtomID) {
            TurfDelta turfDelta = new TurfDelta(x, y, newTurfAtomID);

            RemoveExistingTurfDelta(x, y);
            TurfDeltas.Add(turfDelta);
        }

        public bool ContainsChanges() {
            return (AtomCreations.Count > 0) || (AtomLocationDeltas.Count > 0) || (TurfDeltas.Count > 0);
        }

        private int GetExistingAtomCreationIndex(UInt16 atomID) {
            for (int i = 0; i < AtomCreations.Count; i++) {
                if (AtomCreations[i].AtomID == atomID) return i;
            }

            return -1;
        }

        private int GetExistingAtomDeltaIndex(UInt16 atomID) {
            for (int i = 0; i < AtomDeltas.Count; i++) {
                if (AtomDeltas[i].AtomID == atomID) return i;
            }

            return -1;
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
    }
}
