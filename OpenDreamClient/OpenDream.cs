using OpenDreamClient.Audio;
using OpenDreamClient.Audio.NAudio;
using OpenDreamClient.Dream;
using OpenDreamClient.Interface;
using OpenDreamClient.Net;
using OpenDreamClient.Resources;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace OpenDreamClient {
    class OpenDream : Application {
        public IDreamSoundEngine SoundEngine = null;
        public DreamStateManager StateManager = null;
        public DreamResourceManager ResourceManager = null;
        public DreamInterface  Interface = null;
        public ClientConnection Connection = new ClientConnection();

        public Map Map;
        public ATOM Eye;
        public Dictionary<UInt16, ATOM> ATOMs { get; private set; } = null;
        public List<ATOM> ScreenObjects { get; private set; } = null;

        private DispatcherTimer _updateTimer = new DispatcherTimer();

        public OpenDream() {
            _updateTimer.Interval = TimeSpan.FromMilliseconds(1000 / 20);
            _updateTimer.Tick += UpdateTimerTick;

            RegisterPacketCallbacks();
        }

        public void AddATOM(ATOM atom) {
            ATOMs.Add(atom.ID, atom);
        }

        public void RemoveATOM(ATOM atom) {
            atom.Loc = null;
            ATOMs.Remove(atom.ID);
        }

        public void ConnectToServer(string ip, int port, string username) {
            if (Connection.Connected) throw new InvalidOperationException("Already connected to a server!");
            Connection.Connect(ip, port);

            PacketRequestConnect pRequestConnect = new PacketRequestConnect(username);
            Connection.SendPacket(pRequestConnect);

            Interface = new DreamInterface();
            SoundEngine = new NAudioSoundEngine();
            ResourceManager = new DreamResourceManager();
            StateManager = new DreamStateManager();

            ATOMs = new Dictionary<UInt16, ATOM>();
            ScreenObjects = new List<ATOM>();

            MainWindow.Hide();
            _updateTimer.Start();
        }

        public void DisconnectFromServer() {
            if (!Connection.Connected) return;

            _updateTimer.Stop();
            SoundEngine.StopAllChannels();
            Connection.Close();

            Interface = null;
            SoundEngine = null;
            ResourceManager = null;
            StateManager = null;

            Map = null;
            ATOMs = null;
            ScreenObjects = null;

            MainWindow.Show();
        }

        private void RegisterPacketCallbacks() {
            Connection.RegisterPacketCallback<PacketConnectionResult>(PacketID.ConnectionResult, HandlePacketConnectionResult);
            Connection.RegisterPacketCallback<PacketInterfaceData>(PacketID.InterfaceData, packet => Interface.HandlePacketInterfaceData(packet));
            Connection.RegisterPacketCallback<PacketOutput>(PacketID.Output, packet => Interface.HandlePacketOutput(packet));
            Connection.RegisterPacketCallback<PacketATOMTypes>(PacketID.AtomTypes, packet => ATOM.HandlePacketAtomBases(packet));
            Connection.RegisterPacketCallback<PacketResource>(PacketID.Resource, packet => ResourceManager.HandlePacketResource(packet));
            Connection.RegisterPacketCallback<PacketFullGameState>(PacketID.FullGameState, packet => StateManager.HandlePacketFullGameState(packet));
            Connection.RegisterPacketCallback<PacketDeltaGameState>(PacketID.DeltaGameState, packet => StateManager.HandlePacketDeltaGameState(packet));
            Connection.RegisterPacketCallback<PacketSound>(PacketID.Sound, packet => SoundEngine.HandlePacketSound(packet));
            Connection.RegisterPacketCallback<PacketBrowse>(PacketID.Browse, packet => Interface.HandlePacketBrowse(packet));
        }

        private void UpdateTimerTick(object sender, EventArgs e) {
            if (Connection.Connected) {
                Connection.ProcessPackets();
            }
        }

        private void HandlePacketConnectionResult(PacketConnectionResult pConnectionResult) {
            if (!pConnectionResult.ConnectionSuccessful) {
                Console.WriteLine("Connection was unsuccessful: " + pConnectionResult.ErrorMessage);
                DisconnectFromServer();
            }
        }
    }
}
