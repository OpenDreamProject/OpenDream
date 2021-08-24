using System.Collections.Generic;
using Content.Server.DM;
using Content.Shared.Dream;
using Content.Shared.Network.Messages;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Server.Dream
{
    partial class DreamManager
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
        }

        private void RxSelectStatPanel(MsgSelectStatPanel message)
        {
            var connection = _connections[_playerManager.GetSessionByChannel(message.MsgChannel)];
            connection.HandleMsgSelectStatPanel(message);
        }

        private void RxPromptResponse(MsgPromptResponse message)
        {
            var connection = _connections[_playerManager.GetSessionByChannel(message.MsgChannel)];
            connection.HandleMsgPromptResponse(message);
        }

        private void OnPlayerStatusChanged(object sender, SessionStatusEventArgs e)
        {
            switch (e.NewStatus)
            {
                case SessionStatus.Connected:
                    e.Session.JoinGame();
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
