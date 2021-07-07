using OpenDreamClient.Audio;
using OpenDreamClient.Dream;
//using OpenDreamClient.Interface;
using OpenDreamClient.Net;
using OpenDreamClient.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Net;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using Robust.Shared.ViewVariables;

namespace OpenDreamClient {
    delegate void ConnectedToServerEventHandler();
    delegate void DisconnectedFromServerEventHandler();
    delegate void ClientTickEventHandler();

    internal class OpenDream {
        public event ConnectedToServerEventHandler ConnectedToServer;
        public event DisconnectedFromServerEventHandler DisconnectedFromServer;
        public event ClientTickEventHandler ClientTick;

        private const float UpdateTime = 0.05f;

        [ViewVariables] public DreamSoundEngine SoundEngine = null;
        [ViewVariables] public DreamStateManager StateManager = null;
        [ViewVariables] public DreamResourceManager ResourceManager = null;
        //public DreamInterface Interface = null;
        [ViewVariables] public ClientConnection Connection = new ClientConnection();
        [ViewVariables] public ClientData ClientData = new ClientData(setDefaults: true);

        [ViewVariables] public Map Map;
        [ViewVariables] public ATOM Eye;
        [ViewVariables] public ClientPerspective Perspective;

        public string[] AvailableVerbs { get; private set; } = null;
        public List<IconAppearance> IconAppearances { get; private set; } = new();
        public Dictionary<UInt32, ATOM> ATOMs { get; private set; } = new();
        public List<ATOM> ScreenObjects { get; private set; } = new();

        private string _username;
        private float _updateTimer = 0f;

        public OpenDream() {
            // TODO ROBUST: Obviously, fix this.
            _username = DateTime.Now.GetHashCode().ToString();

            //Interface = new DreamInterface(this);
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

            _updateTimer = 0f;
            ConnectedToServer?.Invoke();
        }

        public void DisconnectFromServer() {
            if (!Connection.Connected) return;

            _updateTimer = 0f;
            Connection.Close();

            DisconnectedFromServer?.Invoke();
        }

        public void RunCommand(string command) {
            string[] split = command.Split(" ");
            string verb = split[0];

            switch (verb) {
                case ".quit": DisconnectFromServer(); break;
                //case ".screenshot": Interface.SaveScreenshot(split.Length == 1 || split[1] != "auto"); break;
                default: {
                    if (split.Length > 1) throw new NotImplementedException("Verb argument parsing is not implemented");

                    Connection.SendPacket(new PacketCallVerb(verb));
                    break;
                }
            }
        }

        private void RegisterPacketCallbacks() {
            Connection.RegisterPacketCallback<PacketConnectionResult>(PacketID.ConnectionResult, HandlePacketConnectionResult);
            Connection.RegisterPacketCallback<PacketOutput>(PacketID.Output, packet => {});
            //Connection.RegisterPacketCallback<PacketOutput>(PacketID.Output, packet => Interface.HandlePacketOutput(packet));
            Connection.RegisterPacketCallback<PacketResource>(PacketID.Resource, packet => ResourceManager.HandlePacketResource(packet));
            Connection.RegisterPacketCallback<PacketFullGameState>(PacketID.FullGameState, packet => StateManager.HandlePacketFullGameState(packet));
            Connection.RegisterPacketCallback<PacketDeltaGameState>(PacketID.DeltaGameState, packet => StateManager.HandlePacketDeltaGameState(packet));
            Connection.RegisterPacketCallback<PacketSound>(PacketID.Sound, packet => SoundEngine.HandlePacketSound(packet));
            Connection.RegisterPacketCallback<PacketBrowse>(PacketID.Browse, packet => {});
            //Connection.RegisterPacketCallback<PacketBrowse>(PacketID.Browse, packet => Interface.HandlePacketBrowse(packet));
            Connection.RegisterPacketCallback<PacketBrowseResource>(PacketID.BrowseResource, packet => ResourceManager.HandlePacketBrowseResource(packet));
            Connection.RegisterPacketCallback<PacketPrompt>(PacketID.Prompt, packet => {});
            //Connection.RegisterPacketCallback<PacketPrompt>(PacketID.Prompt, packet => Interface.HandlePacketPrompt(packet));
            Connection.RegisterPacketCallback<PacketAlert>(PacketID.Alert, packet => {});
            //Connection.RegisterPacketCallback<PacketAlert>(PacketID.Alert, packet => Interface.HandlePacketAlert(packet));
            Connection.RegisterPacketCallback<PacketUpdateAvailableVerbs>(PacketID.UpdateAvailableVerbs, packet => HandlePacketUpdateAvailableVerbs(packet));
            Connection.RegisterPacketCallback<PacketUpdateStatPanels>(PacketID.UpdateStatPanels, packet => {});
            //Connection.RegisterPacketCallback<PacketUpdateStatPanels>(PacketID.UpdateStatPanels, packet => Interface.HandlePacketUpdateStatPanels(packet));
            Connection.RegisterPacketCallback<PacketSelectStatPanel>(PacketID.SelectStatPanel, packet => {});
            //Connection.RegisterPacketCallback<PacketSelectStatPanel>(PacketID.SelectStatPanel, packet => Interface.HandlePacketSelectStatPanel(packet));
            Connection.RegisterPacketCallback<PacketWinSet>(PacketID.WinSet, packet => {});
            //Connection.RegisterPacketCallback<PacketWinSet>(PacketID.WinSet, packet => Interface.HandlePacketWinSet(packet));
        }

        public void Update(float frameTime)
        {
            if (!Connection.Connected)
                return;

            _updateTimer += frameTime;

            if (_updateTimer < UpdateTime)
                return;

            _updateTimer -= UpdateTime;

            ClientTick?.Invoke();

            if (Connection.Connected) {
                Connection.ProcessPackets();
            }
        }

        private void HandlePacketConnectionResult(PacketConnectionResult pConnectionResult) {
            if (pConnectionResult.ConnectionSuccessful) {
                //Interface.LoadInterfaceFromSource(pConnectionResult.InterfaceData);
            } else {
                Console.WriteLine("Connection was unsuccessful: " + pConnectionResult.ErrorMessage);
                DisconnectFromServer();
            }
        }

        public void HandlePacketUpdateAvailableVerbs(PacketUpdateAvailableVerbs pUpdateAvailableVerbs) {
            AvailableVerbs = pUpdateAvailableVerbs.AvailableVerbs;
            //Interface.DefaultInfo?.RefreshVerbs();
        }
    }
}
