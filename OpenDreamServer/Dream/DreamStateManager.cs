using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamShared.Dream;
using System;

namespace OpenDreamServer.Dream {
    delegate void DreamStateManagerDeltaStateFinalizedDelegate(DreamDeltaState deltaState);

    class DreamStateManager {
        public DreamFullState FullState;

        public event DreamStateManagerDeltaStateFinalizedDelegate DeltaStateFinalized;

        private UInt32 _stateIDCounter = 0;
        private DreamDeltaState _currentDeltaState;
        private readonly object _dreamStateManagerLock = new object();

        public DreamStateManager() {
            CreateNewDeltaState();
        }

        public void FinalizeCurrentDeltaState() {
            lock (_dreamStateManagerLock) {
                if (_currentDeltaState.ContainsChanges()) {
                    if (FullState == null) {
                        FullState = new DreamFullState(0);
                        FullState.Turfs = new UInt32[Program.DreamMap.Width, Program.DreamMap.Height, Program.DreamMap.Levels.Length];
                    }

                    FullState.ApplyDeltaState(_currentDeltaState);
                    FullState.ID = _currentDeltaState.ID;

                    DeltaStateFinalized.Invoke(_currentDeltaState);
                    CreateNewDeltaState();
                }
            }
        }

        public void AddIconAppearance(IconAppearance iconAppearance) {
            lock (_dreamStateManagerLock) {
                _currentDeltaState.AddIconAppearance(iconAppearance);
            }
        }

        public void AddAtomCreation(DreamObject atom, ServerIconAppearance appearance) {
            AtomType atomType = AtomType.Atom;
            if (atom.IsSubtypeOf(DreamPath.Area)) atomType = AtomType.Area;
            else if (atom.IsSubtypeOf(DreamPath.Turf)) atomType = AtomType.Turf;
            else if (atom.IsSubtypeOf(DreamPath.Mob)) atomType = AtomType.Movable;
            else if (atom.IsSubtypeOf(DreamPath.Obj)) atomType = AtomType.Movable;

            lock (_dreamStateManagerLock) {
                _currentDeltaState.AddAtomCreation(DreamMetaObjectAtom.AtomIDs[atom], atomType, appearance.GetID());
            }
        }

        public void AddAtomDeletion(DreamObject atom) {
            lock (_dreamStateManagerLock) {
                _currentDeltaState.AddAtomDeletion(DreamMetaObjectAtom.AtomIDs[atom]);
            }
        }

        public void AddAtomIconAppearanceDelta(DreamObject atom, ServerIconAppearance appearance) {
            lock (_dreamStateManagerLock) {
                _currentDeltaState.AddAtomIconAppearanceDelta(DreamMetaObjectAtom.AtomIDs[atom], appearance.GetID());
            }
        }

        public void AddAtomScreenLocationDelta(DreamObject atom, ScreenLocation screenLocation) {
            lock (_dreamStateManagerLock) {
                _currentDeltaState.AddAtomScreenLocDelta(DreamMetaObjectAtom.AtomIDs[atom], screenLocation);
            }
        }

        public void AddAtomLocationDelta(DreamObject atom, DreamObject newLocation) {
            lock (_dreamStateManagerLock) {
                _currentDeltaState.AddAtomLocationDelta(DreamMetaObjectAtom.AtomIDs[atom], (newLocation != null) ? DreamMetaObjectAtom.AtomIDs[newLocation] : UInt32.MaxValue);
            }
        }

        public void AddTurfDelta(int x, int y, int z, DreamObject turf) {
            lock (_dreamStateManagerLock) {
                _currentDeltaState.AddTurfDelta(x, y, z, DreamMetaObjectAtom.AtomIDs[turf]);
            }
        }

        public void AddClient(string ckey) {
            lock (_dreamStateManagerLock) {
                _currentDeltaState.AddClient(ckey);
            }
        }

        public void AddClientEyeIDDelta(string ckey, UInt32 newEyeID) {
            lock (_dreamStateManagerLock) {
                _currentDeltaState.AddClientEyeIDDelta(ckey, newEyeID);
            }
        }

        public void AddClientScreenObject(string ckey, DreamObject atom) {
            lock (_dreamStateManagerLock) {
                _currentDeltaState.AddClientScreenObject(ckey, DreamMetaObjectAtom.AtomIDs[atom]);
            }
        }

        public void RemoveClientScreenObject(string ckey, DreamObject atom) {
            lock (_dreamStateManagerLock) {
                _currentDeltaState.RemoveClientScreenObject(ckey, DreamMetaObjectAtom.AtomIDs[atom]);
            }
        }

        private void CreateNewDeltaState() {
            lock (_dreamStateManagerLock) {
                _currentDeltaState = new DreamDeltaState(_stateIDCounter++);
            }
        }
    }
}
