using OpenDreamClient.Audio;
using OpenDreamClient.Dream;
using OpenDreamClient.Interface;
using OpenDreamClient.Net;
using OpenDreamClient.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Net;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace OpenDreamClient {
    delegate void ConnectedToServerEventHandler();
    delegate void DisconnectedFromServerEventHandler();
    delegate void ClientTickEventHandler();

    class OpenDream {
        public event ConnectedToServerEventHandler ConnectedToServer;
        public event DisconnectedFromServerEventHandler DisconnectedFromServer;
        public event ClientTickEventHandler ClientTick;

        public DreamSoundEngine SoundEngine = null;
        public DreamStateManager StateManager = null;
        public DreamResourceManager ResourceManager = null;
        public DreamInterface Interface = null;
        public ClientConnection Connection = new ClientConnection();
        public ClientData ClientData = new ClientData(setDefaults: true);

        public Map Map;
        public ATOM Eye;
        public ClientPerspective Perspective;

        public string[] AvailableVerbs { get; private set; } = null;
        public List<IconAppearance> IconAppearances { get; private set; } = new();
        public Dictionary<UInt32, ATOM> ATOMs { get; private set; } = new();
        public List<ATOM> ScreenObjects { get; private set; } = new();

        private string _username;
        private DispatcherTimer _updateTimer = new DispatcherTimer();

        public OpenDream(string username) {
            _username = username;
            _updateTimer.Interval = TimeSpan.FromMilliseconds(1000 / 20);
            _updateTimer.Tick += UpdateTimerTick;

            Interface = new DreamInterface(this);
            SoundEngine = new DreamSoundEngine(this);
            ResourceManager = new DreamResourceManager(this);
            StateManager = new DreamStateManager();

            RegisterPacketCallbacks();
        }

        public void AddATOM(ATOM atom) {
            ATOMs.Add(atom.ID, atom);
        }

        public void ConnectToServer(string ip, int port) {
            if (Connection.Connected) throw new InvalidOperationException("Already connected to a server!");
            Connection.Connect(ip, port);

            PacketRequestConnect pRequestConnect = new PacketRequestConnect(_username, ClientData);
            Connection.SendPacket(pRequestConnect);

            _updateTimer.Start();
            ConnectedToServer?.Invoke();
        }

        public void DisconnectFromServer() {
            if (!Connection.Connected) return;

            _updateTimer.Stop();
            Connection.Close();

            DisconnectedFromServer?.Invoke();
        }

        public void RunCommand(string command) {
            string[] split = command.Split(" ");
            string verb = split[0];

            switch (verb) {
                case ".quit": DisconnectFromServer(); break;
                case ".screenshot": Interface.SaveScreenshot(split.Length == 1 || split[1] != "auto"); break;
                default: {
                    if (split.Length > 1)
                    {
                        Console.Error.WriteLine("Verb argument parsing is not implemented yet.");
                        return;
                    }

                    Connection.SendPacket(new PacketCallVerb(verb));
                    break;
                }
            }
        }

        private void RegisterPacketCallbacks() {
            Connection.RegisterPacketCallback<PacketConnectionResult>(PacketID.ConnectionResult, HandlePacketConnectionResult);
            Connection.RegisterPacketCallback<PacketOutput>(PacketID.Output, packet => Interface.HandlePacketOutput(packet));
            Connection.RegisterPacketCallback<PacketResource>(PacketID.Resource, packet => ResourceManager.HandlePacketResource(packet));
            Connection.RegisterPacketCallback<PacketFullGameState>(PacketID.FullGameState, packet => StateManager.HandlePacketFullGameState(packet));
            Connection.RegisterPacketCallback<PacketDeltaGameState>(PacketID.DeltaGameState, packet => StateManager.HandlePacketDeltaGameState(packet));
            Connection.RegisterPacketCallback<PacketSound>(PacketID.Sound, packet => SoundEngine.HandlePacketSound(packet));
            Connection.RegisterPacketCallback<PacketBrowse>(PacketID.Browse, packet => Interface.HandlePacketBrowse(packet));
            Connection.RegisterPacketCallback<PacketBrowseResource>(PacketID.BrowseResource, packet => ResourceManager.HandlePacketBrowseResource(packet));
            Connection.RegisterPacketCallback<PacketPrompt>(PacketID.Prompt, packet => Interface.HandlePacketPrompt(packet));
            Connection.RegisterPacketCallback<PacketAlert>(PacketID.Alert, packet => Interface.HandlePacketAlert(packet));
            Connection.RegisterPacketCallback<PacketUpdateAvailableVerbs>(PacketID.UpdateAvailableVerbs, packet => HandlePacketUpdateAvailableVerbs(packet));
            Connection.RegisterPacketCallback<PacketUpdateStatPanels>(PacketID.UpdateStatPanels, packet => Interface.HandlePacketUpdateStatPanels(packet));
            Connection.RegisterPacketCallback<PacketSelectStatPanel>(PacketID.SelectStatPanel, packet => Interface.HandlePacketSelectStatPanel(packet));
            Connection.RegisterPacketCallback<PacketWinSet>(PacketID.WinSet, packet => Interface.HandlePacketWinSet(packet));
        }

        private void UpdateTimerTick(object sender, EventArgs e) {
            ClientTick?.Invoke();

            if (Connection.Connected) {
                Connection.ProcessPackets();
            }
        }

        private void HandlePacketConnectionResult(PacketConnectionResult pConnectionResult) {
            if (pConnectionResult.ConnectionSuccessful) {
                Interface.LoadInterfaceFromSource(pConnectionResult.InterfaceData);
            } else {
                Console.WriteLine("Connection was unsuccessful: " + pConnectionResult.ErrorMessage);
                DisconnectFromServer();
            }
        }

        public void HandlePacketUpdateAvailableVerbs(PacketUpdateAvailableVerbs pUpdateAvailableVerbs) {
            AvailableVerbs = pUpdateAvailableVerbs.AvailableVerbs;
            Interface.DefaultInfo?.RefreshVerbs();
        }
    }
}
