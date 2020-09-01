﻿using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenDreamServer.Dream {
    delegate void DreamStateManagerDeltaStateFinalizedDelegate(DreamDeltaState deltaState);

    class DreamStateManager {
        public List<DreamFullState> FullStates = new List<DreamFullState>();
        public List<DreamDeltaState> DeltaStates = new List<DreamDeltaState>();

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
                    DeltaStates.Add(_currentDeltaState);
                    DeltaStateFinalized.Invoke(_currentDeltaState);
                    CreateNewDeltaState();

                    if (DeltaStates.Count % 5 == 0) {
                        CreateLatestFullState();
                    }
                }
            }
        }

        public DreamFullState CreateLatestFullState() {
            lock (_dreamStateManagerLock) {
                DreamFullState lastFullState = FullStates.LastOrDefault();
                DreamDeltaState lastDeltaState = DeltaStates.Last();

                if (lastFullState != null && lastFullState.ID == lastDeltaState.ID) {
                    return lastFullState;
                }

                DreamFullState fullState = new DreamFullState(lastDeltaState.ID);

                if (lastFullState != null) {
                    fullState.SetFromFullState(lastFullState);
                    fullState.ApplyDeltaStates(GetDeltaStatesSince(lastFullState.ID));
                } else {
                    fullState.Turfs = new UInt16[Program.DreamMap.Width, Program.DreamMap.Height];
                    fullState.ApplyDeltaStates(DeltaStates);
                }

                FullStates.Add(fullState);
                return fullState;
            }
        }

        public void AddAtomCreation(DreamObject atom) {
            lock (_dreamStateManagerLock) {
                _currentDeltaState.AddAtomCreation(DreamMetaObjectAtom.AtomIDs[atom], Program.AtomBaseIDs[atom.ObjectDefinition]);
            }
        }

        public void AddAtomDeletion(DreamObject atom) {
            lock (_dreamStateManagerLock) {
                _currentDeltaState.AddAtomDeletion(DreamMetaObjectAtom.AtomIDs[atom]);
            }
        }

        public void AddAtomIconStateDelta(DreamObject atom) {
            lock (_dreamStateManagerLock) {
                _currentDeltaState.AddAtomIconStateDelta(DreamMetaObjectAtom.AtomIDs[atom], atom.GetVariable("icon_state").GetValueAsString());
            }
        }

        public void AddAtomLocationDelta(DreamObject atom, DreamObject newLocation) {
            lock (_dreamStateManagerLock) {
                _currentDeltaState.AddAtomLocationDelta(DreamMetaObjectAtom.AtomIDs[atom], (newLocation != null) ? DreamMetaObjectAtom.AtomIDs[newLocation] : (UInt16)0xFFFF);
            }
        }

        public void AddTurfDelta(int x, int y, UInt16 newTurfAtomID) {
            lock (_dreamStateManagerLock) {
                _currentDeltaState.AddTurfDelta(x, y, newTurfAtomID);
            }
        }

        private void CreateNewDeltaState() {
            lock (_dreamStateManagerLock) {
                _currentDeltaState = new DreamDeltaState(_stateIDCounter++);
            }
        }

        private List<DreamDeltaState> GetDeltaStatesSince(UInt32 stateID) {
            List<DreamDeltaState> deltaStates = new List<DreamDeltaState>();

            for (int i = DeltaStates.Count - 1; i > 0; i--) {
                DreamDeltaState deltaState = DeltaStates[i];

                if (deltaState.ID > stateID) {
                    deltaStates.Add(deltaState);
                } else {
                    break;
                }
            }

            deltaStates.Reverse();
            return deltaStates;
        }
    }
}
