using OpenDreamClient.Audio;
using OpenDreamClient.Audio.NAudio;
using OpenDreamClient.Dream;
using OpenDreamClient.Renderer;
using OpenDreamClient.Interface;
using OpenDreamClient.Net;
using OpenDreamClient.Resources;
using OpenDreamClient.Resources.ResourceTypes;
using OpenDreamShared.Dream;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;

namespace OpenDreamClient {
    class OpenDream : System.Windows.Application {
        public IDreamSoundEngine DreamSoundEngine = null;
        public DreamResourceManager DreamResourceManager = null;
        public ClientConnection Connection = new ClientConnection();
        public GameWindow GameWindow;
        public Map Map;
        public ATOM Eye;
        public Dictionary<UInt16, ATOM> ATOMs { get; private set; } = null;
        public List<ATOM> ScreenObjects = null;

        public OpenDream() {
            RegisterPacketCallbacks();
        }

        public void ConnectToServer(string ip, int port, string username) {
            if (Connection.Connected) throw new Exception("Already connected to a server!");
            Connection.Connect(ip, port);

            PacketRequestConnect pRequestConnect = new PacketRequestConnect(username);
            Connection.SendPacket(pRequestConnect);

            GameWindow = new GameWindow();
            GameWindow.Show();

            DreamSoundEngine = new NAudioSoundEngine();
            DreamResourceManager = new DreamResourceManager();

            ATOMs = new Dictionary<UInt16, ATOM>();
            ScreenObjects = new List<ATOM>();

            this.MainWindow.Hide();
        }

        public void DisconnectFromServer() {
            if (!Connection.Connected) return;

            DreamSoundEngine.StopAllChannels();
            Connection.Close();

            DreamSoundEngine = null;
            DreamResourceManager = null;

            Map = null;
            ATOMs = null;
            ScreenObjects = null;

            GameWindow.Close();
            this.MainWindow.Show();
        }

        public void AddATOM(ATOM atom) {
            ATOMs.Add(atom.ID, atom);
        }

        public void RemoveATOM(ATOM atom) {
            atom.Loc = null;
            ATOMs.Remove(atom.ID);
        }

        private void RegisterPacketCallbacks() {
            Connection.RegisterPacketCallback<PacketConnectionResult>(PacketID.ConnectionResult, HandlePacketConnectionResult);
            Connection.RegisterPacketCallback<PacketInterfaceData>(PacketID.InterfaceData, packet => GameWindow.HandlePacketInterfaceData(packet));
            Connection.RegisterPacketCallback<PacketOutput>(PacketID.Output, HandlePacketOutput);
            Connection.RegisterPacketCallback<PacketATOMTypes>(PacketID.AtomTypes, packet => ATOM.HandleAtomBasesPacket(packet));
            Connection.RegisterPacketCallback<PacketResource>(PacketID.Resource, packet => DreamResourceManager.HandleResourcePacket(packet));
            Connection.RegisterPacketCallback<PacketFullGameState>(PacketID.FullGameState, HandlePacketFullGameState);
            Connection.RegisterPacketCallback<PacketDeltaGameState>(PacketID.DeltaGameState, HandlePacketDeltaGameState);
        }

        private void HandlePacketConnectionResult(PacketConnectionResult pConnectionResult) {
            if (!pConnectionResult.ConnectionSuccessful) {
                Console.WriteLine("Connection was unsuccessful: " + pConnectionResult.ErrorMessage);
                DisconnectFromServer();
            }
        }

        private void HandlePacketOutput(PacketOutput pOutput) {
            if (pOutput.ValueType == PacketOutput.PacketOutputType.String) {
                PacketOutput.OutputString stringValue = (PacketOutput.OutputString)pOutput.Value;

                if (GameWindow.DefaultOutput != null) GameWindow.DefaultOutput.TextBox.AppendText(stringValue.Value + Environment.NewLine);
                else Console.WriteLine(stringValue.Value);
            } else if (pOutput.ValueType == PacketOutput.PacketOutputType.Sound) {
                PacketOutput.OutputSound soundValue = (PacketOutput.OutputSound)pOutput.Value;

                if (soundValue.File != null) {
                    DreamResourceManager.LoadResourceAsync<ResourceSound>(soundValue.File, (ResourceSound sound) => {
                        DreamSoundEngine.PlaySound(soundValue.Channel, sound);
                    });
                } else {
                    DreamSoundEngine.StopChannel(soundValue.Channel);
                }
            }
        }

        private void HandlePacketFullGameState(PacketFullGameState pFullGameState) {
            DreamFullState fullState = pFullGameState.FullState;

            ATOMs.Clear();
            foreach (KeyValuePair<UInt16, DreamFullState.Atom> stateAtom in fullState.Atoms) {
                ATOM atom = new ATOM(stateAtom.Key, ATOMBase.AtomBases[stateAtom.Value.BaseID]);

                atom.Icon.VisualProperties = atom.Icon.VisualProperties.Merge(stateAtom.Value.VisualProperties);
                foreach (KeyValuePair<UInt16, IconVisualProperties> overlay in stateAtom.Value.Overlays) {
                    atom.Icon.AddOverlay(overlay.Key, overlay.Value);
                }

                atom.ScreenLocation = stateAtom.Value.ScreenLocation;

                if (stateAtom.Value.LocationID != 0xFFFF) {
                    if (ATOMs.ContainsKey(stateAtom.Value.LocationID)) {
                        atom.Loc = ATOMs[stateAtom.Value.LocationID];
                    } else {
                        Console.WriteLine("Full game state packet gave an atom an invalid location, which was ignored (ID " + stateAtom.Value.AtomID + ")(Location ID " + stateAtom.Value.LocationID + ")");
                    }
                } else {
                    atom.Loc = null;
                }
            }

            ATOM[,] turfs = new ATOM[fullState.Turfs.GetLength(0), fullState.Turfs.GetLength(0)];
            for (int x = 0; x < turfs.GetLength(0); x++) {
                for (int y = 0; y < turfs.GetLength(1); y++) {
                    UInt16 turfAtomID = fullState.Turfs[x, y];

                    if (ATOMs.ContainsKey(turfAtomID)) {
                        ATOM turf = ATOMs[turfAtomID];

                        turf.X = x;
                        turf.Y = y;
                        turfs[x, y] = turf;
                    } else {
                        Console.WriteLine("Full game state packet defines a turf as an atom that doesn't exist, and was ignored (ID " + turfAtomID + ")(Location " + x + ", " + y + ")");
                    }
                }
            }

            Map = new Map(turfs);

            if (pFullGameState.EyeID != 0xFFFF) {
                if (ATOMs.ContainsKey(pFullGameState.EyeID)) {
                    Eye = ATOMs[pFullGameState.EyeID];
                } else {
                    Console.WriteLine("Full game state packet gives an invalid atom for the eye (ID " + pFullGameState.EyeID + ")");
                    Eye = null;
                }
            } else {
                Eye = null;
            }

            ScreenObjects.Clear();
            foreach (UInt16 screenObjectAtomID in pFullGameState.ScreenObjects) {
                if (ATOMs.ContainsKey(screenObjectAtomID)) {
                    ScreenObjects.Add(ATOMs[screenObjectAtomID]);
                } else {
                    Console.WriteLine("Full games state packet defines a screen object that doesn't exist, and was ignored (ID " + screenObjectAtomID + ")");
                }
            }
        }

        private void HandlePacketDeltaGameState(PacketDeltaGameState pDeltaGameState) {
            DreamDeltaState deltaState = pDeltaGameState.DeltaState;

            foreach (DreamDeltaState.AtomCreation atomCreation in deltaState.AtomCreations) {
                if (!ATOMs.ContainsKey(atomCreation.AtomID)) {
                    ATOM atom = new ATOM(atomCreation.AtomID, ATOMBase.AtomBases[atomCreation.BaseID]);

                    atom.Icon.VisualProperties = atom.Icon.VisualProperties.Merge(atomCreation.VisualProperties);
                    atom.ScreenLocation = atomCreation.ScreenLocation;

                    if (atomCreation.LocationID != 0xFFFF) {
                        if (ATOMs.ContainsKey(atomCreation.LocationID)) {
                            atom.Loc = ATOMs[atomCreation.LocationID];
                        } else {
                            Console.WriteLine("Delta state packet gave a new atom an invalid location, so it was not assigned one (ID " + atomCreation.AtomID + ")(Location ID " + atomCreation.LocationID + ")");
                        }
                    } else {
                        atom.Loc = null;
                    }

                    foreach (KeyValuePair<UInt16, IconVisualProperties> overlay in atomCreation.Overlays) {
                        atom.Icon.AddOverlay(overlay.Key, overlay.Value);
                    }
                } else {
                    Console.WriteLine("Delta state packet created a new atom that already exists, and was ignored (ID " + atomCreation.AtomID + ")");
                }
            }

            if (deltaState.AtomDeletions != null) {
                foreach (UInt16 atomID in deltaState.AtomDeletions) {
                    if (ATOMs.ContainsKey(atomID)) {
                        ATOM atom = ATOMs[atomID];

                        atom.Loc = null;
                        ATOMs.Remove(atomID);
                    } else {
                        Console.WriteLine("Delta state packet gives an atom deletion for an invalid atom, and was ignored (ID " + atomID + ")");
                    }
                }
            }

            foreach (DreamDeltaState.AtomDelta atomDelta in deltaState.AtomDeltas) {
                if (ATOMs.ContainsKey(atomDelta.AtomID)) {
                    ATOM atom = ATOMs[atomDelta.AtomID];

                    atom.Icon.VisualProperties = atom.Icon.VisualProperties.Merge(atomDelta.ChangedVisualProperties);

                    if (atomDelta.OverlayAdditions != null) {
                        foreach (KeyValuePair<UInt16, IconVisualProperties> overlay in atomDelta.OverlayAdditions) {
                            atom.Icon.AddOverlay(overlay.Key, overlay.Value);
                        }
                    }

                    if (atomDelta.OverlayRemovals != null) {
                        foreach (UInt16 overlayID in atomDelta.OverlayRemovals) {
                            atom.Icon.RemoveOverlay(overlayID);
                        }
                    }

                    if (atomDelta.HasChangedScreenLocation) {
                        atom.ScreenLocation = atomDelta.ScreenLocation;
                    }
                } else {
                    Console.WriteLine("Delta state packet contains delta values for an invalid ATOM, and was ignored (ID " + atomDelta.AtomID + ")");
                }
            }

            foreach (DreamDeltaState.AtomLocationDelta atomLocationDelta in deltaState.AtomLocationDeltas) {
                if (ATOMs.ContainsKey(atomLocationDelta.AtomID)) {
                    ATOM atom = ATOMs[atomLocationDelta.AtomID];

                    if (atomLocationDelta.LocationID != 0xFFFF) {
                        if (ATOMs.ContainsKey(atomLocationDelta.LocationID)) {
                            atom.Loc = ATOMs[atomLocationDelta.LocationID];
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

            foreach (DreamDeltaState.TurfDelta turfDelta in deltaState.TurfDeltas) {
                if (ATOMs.ContainsKey(turfDelta.TurfAtomID)) {
                    Map.Turfs[turfDelta.X, turfDelta.Y] = ATOMs[turfDelta.TurfAtomID];
                } else {
                    Console.WriteLine("Delta state packet sets a turf to an invalid atom, and was ignored (ID " + turfDelta.TurfAtomID + ")(Location " + turfDelta.X + ", " + turfDelta.Y + ")");
                }
            }

            if (pDeltaGameState.ClientDelta.NewEyeID != null) {
                ATOM newEye = null;

                if (pDeltaGameState.ClientDelta.NewEyeID.Value != 0xFFFF) {
                    if (ATOMs.ContainsKey(pDeltaGameState.ClientDelta.NewEyeID.Value)) {
                        newEye = ATOMs[pDeltaGameState.ClientDelta.NewEyeID.Value];
                    } else {
                        Console.WriteLine("Delta state packet gives a new eye with an invalid ATOM, and was ignored (ID " + pDeltaGameState.ClientDelta.NewEyeID.Value + ")");
                    }
                }

                Eye = newEye;
            }

            if (pDeltaGameState.ScreenObjectAdditions != null) {
                foreach (UInt16 screenObjectAtomID in pDeltaGameState.ScreenObjectAdditions) {
                    if (ATOMs.ContainsKey(screenObjectAtomID)) {
                        ATOM screenObject = ATOMs[screenObjectAtomID];

                        if (!ScreenObjects.Contains(screenObject)) {
                            ScreenObjects.Add(screenObject);
                        } else {
                            Console.WriteLine("Delta state packet says to add a screen object that's already there, and was ignored (ID " + screenObjectAtomID + ")");
                        }
                    } else {
                        Console.WriteLine("Delta state packet says to add a screen object that doesn't exist, and was ignored (ID " + screenObjectAtomID + ")");
                    }
                }
            }

            if (pDeltaGameState.ScreenObjectRemovals != null) {
                foreach (UInt16 screenObjectAtomID in pDeltaGameState.ScreenObjectRemovals) {
                    if (ATOMs.ContainsKey(screenObjectAtomID)) {
                        ATOM screenObject = ATOMs[screenObjectAtomID];

                        if (ScreenObjects.Contains(screenObject)) {
                            ScreenObjects.Remove(screenObject);
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
