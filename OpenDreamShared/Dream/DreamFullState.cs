using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace OpenDreamShared.Dream {
    class DreamFullState {
        public struct Atom {
            public UInt16 AtomID;
            public UInt16 BaseID;
            public UInt16 LocationID;
            public IconVisualProperties VisualProperties;
            public Point ScreenLocation;
            public Dictionary<UInt16, IconVisualProperties> Overlays;
        }

        public UInt32 ID;
        public Dictionary<UInt16, Atom> Atoms = new Dictionary<UInt16, Atom>();
        public UInt16[,] Turfs = new UInt16[0, 0];

        public DreamFullState(UInt32 id) {
            ID = id;
        }

        public void SetFromFullState(DreamFullState fullState) {
            foreach (KeyValuePair<UInt16, Atom> atom in fullState.Atoms) {
                Atoms.Add(atom.Key, atom.Value);
            }

            Turfs = fullState.Turfs;
        }

        public void ApplyDeltaStates(List<DreamDeltaState> deltaStates) {
            foreach (DreamDeltaState deltaState in deltaStates) {
                foreach (DreamDeltaState.AtomCreation atomCreation in deltaState.AtomCreations) {
                    Atom atom = new Atom();

                    atom.AtomID = atomCreation.AtomID;
                    atom.BaseID = atomCreation.BaseID;
                    atom.LocationID = atomCreation.LocationID;
                    atom.VisualProperties = atomCreation.VisualProperties;
                    atom.ScreenLocation = new Point(0, 0);
                    atom.Overlays = new Dictionary<ushort, IconVisualProperties>();

                    Atoms[atom.AtomID] = atom;
                }

                foreach (UInt16 atomDeletion in deltaState.AtomDeletions) {
                    Atoms.Remove(atomDeletion);
                }

                foreach (DreamDeltaState.AtomDelta atomDelta in deltaState.AtomDeltas) {
                    Atom atom = Atoms[atomDelta.AtomID];

                    if (!atomDelta.ChangedVisualProperties.IsDefault()) atom.VisualProperties = atom.VisualProperties.Merge(atomDelta.ChangedVisualProperties);
                    
                    Atoms[atomDelta.AtomID] = atom;
                }

                foreach (DreamDeltaState.AtomLocationDelta atomLocationDelta in deltaState.AtomLocationDeltas) {
                    Atom atom = Atoms[atomLocationDelta.AtomID];

                    atom.LocationID = atomLocationDelta.LocationID;
                    Atoms[atomLocationDelta.AtomID] = atom;
                }

                foreach (DreamDeltaState.TurfDelta turfDelta in deltaState.TurfDeltas) {
                    Turfs[turfDelta.X, turfDelta.Y] = turfDelta.TurfAtomID;
                }
            }
        }
    }
}
