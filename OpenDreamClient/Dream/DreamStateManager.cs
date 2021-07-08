using OpenDreamShared.Dream;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.IoC;

namespace OpenDreamClient.Dream {
    [UsedImplicitly]
    internal class DreamStateManager
    {
        [Dependency] private readonly OpenDream _openDream = default!;

        public void HandlePacketFullGameState(PacketFullGameState pFullGameState) {
            DreamFullState fullState = pFullGameState.FullState;

            _openDream.IconAppearances.Clear();
            foreach (IconAppearance iconAppearance in fullState.IconAppearances) {
                _openDream.IconAppearances.Add(iconAppearance);
            }

            _openDream.ATOMs.Clear();
            Dictionary<ATOM, UInt32> atomLocations = new();
            foreach (KeyValuePair<UInt32, DreamFullState.Atom> stateAtom in fullState.Atoms) {
                ATOM atom = new ATOM(stateAtom.Key, stateAtom.Value.Type, stateAtom.Value.IconAppearanceID);

                atom.Icon.Appearance = _openDream.IconAppearances[stateAtom.Value.IconAppearanceID];
                atom.ScreenLocation = stateAtom.Value.ScreenLocation;
                atomLocations.Add(atom, stateAtom.Value.LocationID);
            }

            foreach (KeyValuePair<ATOM, UInt32> atomLocation in atomLocations) {
                UInt32 locationId = atomLocation.Value;

                if (locationId != UInt32.MaxValue) {
                    if (_openDream.ATOMs.ContainsKey(locationId)) {
                        atomLocation.Key.Loc = _openDream.ATOMs[locationId];
                    } else {
                        Console.WriteLine("Full game state packet gave an atom an invalid location, which was ignored (ID " + atomLocation.Key.ID + ")(Location ID " + locationId + ")");
                    }
                } else {
                    atomLocation.Key.Loc = null;
                }
            }

            _openDream.Map = new Map();
            for (int z = 0; z < fullState.Levels.Count; z++) {
                DreamFullState.Level level = fullState.Levels[z];
                int levelWidth = level.Turfs.GetLength(0);
                int levelHeight = level.Turfs.GetLength(1);

                ATOM[,] turfs = new ATOM[levelWidth, levelHeight];
                for (int x = 0; x < levelWidth; x++) {
                    for (int y = 0; y < levelHeight; y++) {
                        UInt32 turfAtomID = level.Turfs[x, y];

                        if (_openDream.ATOMs.ContainsKey(turfAtomID)) {
                            ATOM turf = _openDream.ATOMs[turfAtomID];

                            turf.X = x;
                            turf.Y = y;
                            turf.Z = z;
                            turfs[x, y] = turf;
                        } else {
                            Console.WriteLine("Full game state packet defines a turf as an atom that doesn't exist, and was ignored (ID " + turfAtomID + ")(Location " + x + ", " + y + ", " + z + ")");
                        }
                    }
                }

                _openDream.Map.Levels.Add(new Map.Level() {
                    Turfs = turfs
                });
            }

            if (pFullGameState.ClientState != null) {
                UInt32 eyeId = pFullGameState.ClientState.EyeID;
                if (eyeId != UInt32.MaxValue) {
                    if (_openDream.ATOMs.TryGetValue(eyeId, out ATOM eye)) {
                        _openDream.Eye = eye;
                    } else {
                        Console.WriteLine("Full game state packet gives an invalid atom for the eye (ID " + eyeId + ")");
                        _openDream.Eye = null;
                    }
                } else {
                    _openDream.Eye = null;
                }

                _openDream.Perspective = pFullGameState.ClientState.Perspective;

                _openDream.ScreenObjects.Clear();
                foreach (UInt32 screenObjectAtomID in pFullGameState.ClientState.ScreenObjects) {
                    if (_openDream.ATOMs.ContainsKey(screenObjectAtomID)) {
                        _openDream.ScreenObjects.Add(_openDream.ATOMs[screenObjectAtomID]);
                    } else {
                        Console.WriteLine("Full games state packet defines a screen object that doesn't exist, and was ignored (ID " + screenObjectAtomID + ")");
                    }
                }
            }
        }

        public void HandlePacketDeltaGameState(PacketDeltaGameState pDeltaGameState) {
            DreamDeltaState deltaState = pDeltaGameState.DeltaState;

            foreach (IconAppearance appearance in deltaState.NewIconAppearances) {
                _openDream.IconAppearances.Add(appearance);
            }

            Dictionary<ATOM, UInt32> atomLocations = new();
            foreach (KeyValuePair<UInt32, DreamDeltaState.AtomCreation> atomCreationPair in deltaState.AtomCreations) {
                UInt32 atomID = atomCreationPair.Key;
                DreamDeltaState.AtomCreation atomCreation = atomCreationPair.Value;

                if (!_openDream.ATOMs.ContainsKey(atomID)) {
                    ATOM atom = new ATOM(atomID, atomCreation.Type, atomCreation.IconAppearanceID);

                    atom.Icon.Appearance = _openDream.IconAppearances[atomCreation.IconAppearanceID];
                    atom.ScreenLocation = atomCreation.ScreenLocation;
                    atomLocations.Add(atom, atomCreation.LocationID);
                } else {
                    Console.WriteLine("Delta state packet created a new atom that already exists, and was ignored (ID " + atomID + ")");
                }
            }

            foreach (KeyValuePair<ATOM, UInt32> atomLocation in atomLocations) {
                UInt32 locationId = atomLocation.Value;

                if (locationId != UInt32.MaxValue) {
                    if (_openDream.ATOMs.ContainsKey(locationId)) {
                        atomLocation.Key.Loc = _openDream.ATOMs[locationId];
                    } else {
                        Console.WriteLine("Full game state packet gave an atom an invalid location, which was ignored (ID " + atomLocation.Key.ID + ")(Location ID " + locationId + ")");
                    }
                } else {
                    atomLocation.Key.Loc = null;
                }
            }

            foreach (UInt32 atomID in deltaState.AtomDeletions) {
                if (_openDream.ATOMs.ContainsKey(atomID)) {
                    ATOM atom = _openDream.ATOMs[atomID];

                    atom.Loc = null;
                    _openDream.ATOMs.Remove(atomID);
                } else {
                    Console.WriteLine("Delta state packet gives an atom deletion for an invalid atom, and was ignored (ID " + atomID + ")");
                }
            }

            foreach (KeyValuePair<UInt32, DreamDeltaState.AtomDelta> atomDeltaPair in deltaState.AtomDeltas) {
                UInt32 atomID = atomDeltaPair.Key;
                DreamDeltaState.AtomDelta atomDelta = atomDeltaPair.Value;

                if (_openDream.ATOMs.ContainsKey(atomID)) {
                    ATOM atom = _openDream.ATOMs[atomID];

                    if (atomDelta.NewIconAppearanceID.HasValue) {
                        if (_openDream.IconAppearances.Count > atomDelta.NewIconAppearanceID.Value) {
                            atom.Icon.Appearance = _openDream.IconAppearances[atomDelta.NewIconAppearanceID.Value];
                        } else {
                            Console.WriteLine("Invalid appearance ID " + atomDelta.NewIconAppearanceID.Value);
                        }
                    }

                    if (atomDelta.ScreenLocation != null) {
                        atom.ScreenLocation = atomDelta.ScreenLocation;
                    }
                } else {
                    Console.WriteLine("Delta state packet contains delta values for an invalid ATOM, and was ignored (ID " + atomID + ")");
                }
            }

            foreach (DreamDeltaState.AtomLocationDelta atomLocationDelta in deltaState.AtomLocationDeltas) {
                if (_openDream.ATOMs.ContainsKey(atomLocationDelta.AtomID)) {
                    ATOM atom = _openDream.ATOMs[atomLocationDelta.AtomID];

                    if (atomLocationDelta.LocationID != UInt32.MaxValue) {
                        if (_openDream.ATOMs.ContainsKey(atomLocationDelta.LocationID)) {
                            atom.Loc = _openDream.ATOMs[atomLocationDelta.LocationID];
                        } else {
                            Console.WriteLine("Delta state packet gave an atom a new invalid location, so it was not changed (ID " + atomLocationDelta.AtomID + ")(Location ID " + atomLocationDelta.LocationID + ")");
                        }
                    } else {
                        atom.Loc = null;
                    }
                } else {
                    Console.WriteLine("Delta state packet contains a location delta for an invalid ATOM, and was ignored (ID " + atomLocationDelta.AtomID + ")");
                }
            }

            foreach (KeyValuePair<(int X, int Y, int Z), UInt32> turfDelta in deltaState.TurfDeltas) {
                int x = turfDelta.Key.X;
                int y = turfDelta.Key.Y;
                int z = turfDelta.Key.Z;
                UInt32 turfAtomID = turfDelta.Value;

                if (z >= _openDream.Map.Levels.Count) { //Z-Level doesn't exist, create it
                    while (_openDream.Map.Levels.Count <= z) {
                        int levelWidth = _openDream.Map.Levels[0].Turfs.GetLength(0);
                        int levelHeight = _openDream.Map.Levels[0].Turfs.GetLength(1);

                        _openDream.Map.Levels.Add(new Map.Level() {
                            Turfs = new ATOM[levelWidth, levelHeight]
                        });
                    }
                }

                if (_openDream.ATOMs.ContainsKey(turfAtomID)) {
                    ATOM turf = _openDream.ATOMs[turfAtomID];

                    turf.X = x;
                    turf.Y = y;
                    turf.Z = z;
                    _openDream.Map.Levels[z].Turfs[x, y] = turf;
                } else {
                    Console.WriteLine("Delta state packet sets a turf to an invalid atom, and was ignored (ID " + turfAtomID + ")(Location " + x + ", " + y + ", " + z + ")");
                }
            }

            if (pDeltaGameState.ClientDelta != null) {
                ApplyClientDelta(pDeltaGameState.ClientDelta);
            }
        }

        private void ApplyClientDelta(DreamDeltaState.ClientDelta clientDelta) {
            if (clientDelta.NewEyeID.HasValue) {
                UInt32 newEyeId = clientDelta.NewEyeID.Value;

                if (newEyeId != UInt32.MaxValue) {
                    if (!_openDream.ATOMs.TryGetValue(newEyeId, out _openDream.Eye)) {
                        Console.WriteLine("Delta state packet gives a new eye with an invalid ATOM  (ID " + newEyeId + ")");
                    }
                } else {
                    _openDream.Eye = null;
                }
            }

            if (clientDelta.NewPerspective.HasValue) {
                _openDream.Perspective = clientDelta.NewPerspective.Value;
            }

            if (clientDelta.ScreenObjectAdditions != null) {
                foreach (UInt32 screenObjectAtomID in clientDelta.ScreenObjectAdditions) {
                    if (_openDream.ATOMs.ContainsKey(screenObjectAtomID)) {
                        ATOM screenObject = _openDream.ATOMs[screenObjectAtomID];

                        if (!_openDream.ScreenObjects.Contains(screenObject)) {
                            _openDream.ScreenObjects.Add(screenObject);
                        } else {
                            Console.WriteLine("Delta state packet says to add a screen object that's already there, and was ignored (ID " + screenObjectAtomID + ")");
                        }
                    } else {
                        Console.WriteLine("Delta state packet says to add a screen object that doesn't exist, and was ignored (ID " + screenObjectAtomID + ")");
                    }
                }
            }

            if (clientDelta.ScreenObjectRemovals != null) {
                foreach (UInt32 screenObjectAtomID in clientDelta.ScreenObjectRemovals) {
                    if (_openDream.ATOMs.ContainsKey(screenObjectAtomID)) {
                        ATOM screenObject = _openDream.ATOMs[screenObjectAtomID];

                        if (_openDream.ScreenObjects.Contains(screenObject)) {
                            _openDream.ScreenObjects.Remove(screenObject);
                        } else {
                            Console.WriteLine("Delta state packet says to remove a screen object that's not there, and was ignored (ID " + screenObjectAtomID + ")");
                        }
                    } else {
                        Console.WriteLine("Delta state packet says to remove a screen object that doesn't exist, and was ignored (ID " + screenObjectAtomID + ")");
                    }
                }
            }
        }
    }
}
