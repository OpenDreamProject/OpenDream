using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;
using OpenDreamShared.Network.Messages;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;

namespace OpenDreamRuntime
{
    sealed partial class DreamManager
    {
        [Dependency] private readonly IServerNetManager _netManager;

        private readonly Dictionary<IPlayerSession, DreamConnection> _connections = new();
        private readonly Dictionary<DreamObject, DreamConnection> _clientToConnection = new();

        public DreamConnection GetConnectionBySession(IPlayerSession session) => _connections[session];
        public IEnumerable<DreamConnection> Connections => _connections.Values;

        private void InitializeConnectionManager()
        {
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

            _netManager.RegisterNetMessage<MsgUpdateStatPanels>();
            _netManager.RegisterNetMessage<MsgUpdateAvailableVerbs>();
            _netManager.RegisterNetMessage<MsgSelectStatPanel>(RxSelectStatPanel);
            _netManager.RegisterNetMessage<MsgOutput>();
            _netManager.RegisterNetMessage<MsgAlert>();
            _netManager.RegisterNetMessage<MsgPrompt>();
            _netManager.RegisterNetMessage<MsgPromptResponse>(RxPromptResponse);
            _netManager.RegisterNetMessage<MsgBrowseResource>();
            _netManager.RegisterNetMessage<MsgBrowse>();
            _netManager.RegisterNetMessage<MsgTopic>(RxTopic);
            _netManager.RegisterNetMessage<MsgWinSet>();
            _netManager.RegisterNetMessage<MsgLoadInterface>();
            _netManager.RegisterNetMessage<MsgAckLoadInterface>(RxAckLoadInterface);
            _netManager.RegisterNetMessage<MsgSound>();
        }

        private void RxSelectStatPanel(MsgSelectStatPanel message)
        {
            var connection = ConnectionForChannel(message.MsgChannel);
            connection.HandleMsgSelectStatPanel(message);
        }

        private void RxPromptResponse(MsgPromptResponse message)
        {
            var connection = ConnectionForChannel(message.MsgChannel);
            connection.HandleMsgPromptResponse(message);
        }

        private void RxTopic(MsgTopic message)
        {
            var connection = ConnectionForChannel(message.MsgChannel);
            connection.HandleMsgTopic(message);
        }

        private void RxAckLoadInterface(MsgAckLoadInterface message)
        {
            // Once the client loaded the interface, move them to in-game.
            var player = _playerManager.GetSessionByChannel(message.MsgChannel);
            player.JoinGame();
        }

        private DreamConnection ConnectionForChannel(INetChannel channel)
        {
            return _connections[_playerManager.GetSessionByChannel(channel)];
        }

        private void OnPlayerStatusChanged(object sender, SessionStatusEventArgs e)
        {
            switch (e.NewStatus)
            {
                case SessionStatus.Connected:
                    var interfaceResource = _dreamResourceManager.LoadResource(_compiledJson.Interface);
                    var msgLoadInterface = new MsgLoadInterface() {
                        InterfaceText = interfaceResource.ReadAsString()
                    };

                    e.Session.ConnectedClient.SendMessage(msgLoadInterface);
                    break;
                case SessionStatus.InGame:
                {
                    var connection = new DreamConnection(e.Session);
                    var client = ObjectTree.CreateObject(DreamPath.Client);
                    connection.ClientDreamObject = client;

                    _clientToConnection.Add(client, connection);
                    _connections.Add(e.Session, connection);
                    client.InitSpawn(new DreamProcArguments(new() { DreamValue.Null }));

                    break;
                }
            }
        }

        private void UpdateStat()
        {
            foreach (var connection in _connections.Values)
            {
                connection.UpdateStat();
            }
        }

        public IPlayerSession GetSessionFromClient(DreamObject client)
        {
            return _clientToConnection[client].Session;
        }

        public DreamObject GetClientFromMob(DreamObject mob)
        {
            foreach (DreamObject client in _clientToConnection.Keys)
            {
                if (client.GetVariable("mob").GetValueAsDreamObject() == mob)
                    return client;
            }

            return null;
        }

        public DreamConnection GetConnectionFromMob(DreamObject mob)
        {
            foreach (var connection in _connections.Values)
            {
                if (connection.MobDreamObject == mob)
                    return connection;
            }

            return null;
        }

        public DreamConnection GetConnectionFromClient(DreamObject client)
        {
            foreach (var connection in _connections.Values)
            {
                if (connection.ClientDreamObject == client)
                    return connection;
            }

            return null;
        }
    }
}
