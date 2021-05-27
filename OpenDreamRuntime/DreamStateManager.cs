using OpenDreamRuntime.Objects;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;

namespace OpenDreamRuntime {
    public delegate void DreamStateManagerDeltaStateFinalizedDelegate(DreamDeltaState deltaState);

    public class DreamStateManager {
        public DreamRuntime Runtime { get; }
        public DreamFullState FullState;

        public event DreamStateManagerDeltaStateFinalizedDelegate DeltaStateFinalized;

        private UInt32 _stateIDCounter = 0;
        private DreamDeltaState _currentDeltaState;

        public DreamStateManager(DreamRuntime runtime) {
            Runtime = runtime;
            CreateNewDeltaState();
        }

        public void FinalizeCurrentDeltaState() {
            if (_currentDeltaState.ContainsChanges()) {
                if (FullState == null) {
                    FullState = new DreamFullState(0);

                    FullState.Levels = new List<DreamFullState.Level>(Runtime.Map.Levels.Count);
                    for (int z = 0; z < Runtime.Map.Levels.Count; z++) {
                        FullState.Levels.Add(new DreamFullState.Level(Runtime.Map.Width, Runtime.Map.Height));
                    }
                }

                FullState.ApplyDeltaState(_currentDeltaState);
                FullState.ID = _currentDeltaState.ID;

                DeltaStateFinalized.Invoke(_currentDeltaState);
                CreateNewDeltaState();
            }
        }

        public void AddIconAppearance(IconAppearance iconAppearance) {
            _currentDeltaState.AddIconAppearance(iconAppearance);
        }

        public void AddAtomCreation(DreamObject atom, ServerIconAppearance appearance) {
            AtomType atomType = AtomType.Atom;
            if (atom.IsSubtypeOf(DreamPath.Area)) atomType = AtomType.Area;
            else if (atom.IsSubtypeOf(DreamPath.Turf)) atomType = AtomType.Turf;
            else if (atom.IsSubtypeOf(DreamPath.Mob)) atomType = AtomType.Movable;
            else if (atom.IsSubtypeOf(DreamPath.Obj)) atomType = AtomType.Movable;

            _currentDeltaState.AddAtomCreation(Runtime.AtomIDs[atom], atomType, appearance.GetID());
        }

        public void AddAtomDeletion(DreamObject atom) {
            _currentDeltaState.AddAtomDeletion(Runtime.AtomIDs[atom]);
        }

        public void AddAtomIconAppearanceDelta(DreamObject atom, ServerIconAppearance appearance) {
            _currentDeltaState.AddAtomIconAppearanceDelta(Runtime.AtomIDs[atom], appearance.GetID());
        }

        public void AddAtomScreenLocationDelta(DreamObject atom, ScreenLocation screenLocation) {
            _currentDeltaState.AddAtomScreenLocDelta(Runtime.AtomIDs[atom], screenLocation);
        }

        public void AddAtomLocationDelta(DreamObject atom, DreamObject newLocation) {
            _currentDeltaState.AddAtomLocationDelta(Runtime.AtomIDs[atom], (newLocation != null) ? Runtime.AtomIDs[newLocation] : UInt32.MaxValue);
        }

        public void AddTurfDelta(int x, int y, int z, DreamObject turf) {
            _currentDeltaState.AddTurfDelta(x, y, z, Runtime.AtomIDs[turf]);
        }

        public void AddClient(string ckey) {
            _currentDeltaState.AddClient(ckey);
        }

        public void AddClientEyeIDDelta(string ckey, UInt32 newEyeID) {
            _currentDeltaState.AddClientEyeIDDelta(ckey, newEyeID);
        }

        public void AddClientScreenObject(string ckey, DreamObject atom) {
            _currentDeltaState.AddClientScreenObject(ckey, Runtime.AtomIDs[atom]);
        }

        public void RemoveClientScreenObject(string ckey, DreamObject atom) {
            _currentDeltaState.RemoveClientScreenObject(ckey, Runtime.AtomIDs[atom]);
        }

        private void CreateNewDeltaState() {
            _currentDeltaState = new DreamDeltaState(_stateIDCounter++);
        }
    }
}
