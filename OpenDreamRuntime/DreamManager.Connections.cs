using OpenDreamShared.Network.Messages;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;

namespace OpenDreamRuntime {
    internal sealed partial class DreamManager {
        [Dependency] private readonly IServerNetManager _netManager = default!;

        private readonly Dictionary<NetUserId, DreamConnection> _connections = new();

        public IEnumerable<DreamConnection> Connections => _connections.Values;

        private void InitializeConnectionManager() {
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

            _netManager.RegisterNetMessage<MsgUpdateStatPanels>();
            _netManager.RegisterNetMessage<MsgUpdateAvailableVerbs>();
            _netManager.RegisterNetMessage<MsgSelectStatPanel>(RxSelectStatPanel);
            _netManager.RegisterNetMessage<MsgOutput>();
            _netManager.RegisterNetMessage<MsgAlert>();
            _netManager.RegisterNetMessage<MsgPrompt>();
            _netManager.RegisterNetMessage<MsgPromptList>();
            _netManager.RegisterNetMessage<MsgPromptResponse>(RxPromptResponse);
            _netManager.RegisterNetMessage<MsgBrowseResource>();
            _netManager.RegisterNetMessage<MsgBrowse>();
            _netManager.RegisterNetMessage<MsgTopic>(RxTopic);
            _netManager.RegisterNetMessage<MsgWinSet>();
            _netManager.RegisterNetMessage<MsgWinClone>();
            _netManager.RegisterNetMessage<MsgWinExists>();
            _netManager.RegisterNetMessage<MsgFtp>();
            _netManager.RegisterNetMessage<MsgLoadInterface>();
            _netManager.RegisterNetMessage<MsgAckLoadInterface>(RxAckLoadInterface);
            _netManager.RegisterNetMessage<MsgSound>();
        }

        private void RxSelectStatPanel(MsgSelectStatPanel message) {
            var connection = ConnectionForChannel(message.MsgChannel);
            connection.HandleMsgSelectStatPanel(message);
        }

        private void RxPromptResponse(MsgPromptResponse message) {
            var connection = ConnectionForChannel(message.MsgChannel);
            connection.HandleMsgPromptResponse(message);
        }

        private void RxTopic(MsgTopic message) {
            var connection = ConnectionForChannel(message.MsgChannel);
            connection.HandleMsgTopic(message);
        }

        private void RxAckLoadInterface(MsgAckLoadInterface message) {
            // Once the client loaded the interface, move them to in-game.
            var player = _playerManager.GetSessionByChannel(message.MsgChannel);
            player.JoinGame();
        }

        private DreamConnection ConnectionForChannel(INetChannel channel) {
            return _connections[_playerManager.GetSessionByChannel(channel).UserId];
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e) {
            switch (e.NewStatus) {
                case SessionStatus.Connected:
                    string? interfaceText = null;
                    if (_compiledJson.Interface != null)
                        interfaceText = _dreamResourceManager.LoadResource(_compiledJson.Interface).ReadAsString();

                    var msgLoadInterface = new MsgLoadInterface() {
                        InterfaceText = interfaceText
                    };

                    e.Session.ConnectedClient.SendMessage(msgLoadInterface);
                    break;
                case SessionStatus.InGame: {
                    if (!_connections.TryGetValue(e.Session.UserId, out var connection)) {
                        connection = new DreamConnection();

                        _connections.Add(e.Session.UserId, connection);
                    }

                    connection.HandleConnection(e.Session);
                    break;
                }
                case SessionStatus.Disconnected: {
                    if (_connections.TryGetValue(e.Session.UserId, out var connection))
                        connection.HandleDisconnection();

                    break;
                }
            }
        }

        private void UpdateStat() {
            foreach (var connection in _connections.Values) {
                connection.UpdateStat();
            }
        }

        public DreamConnection GetConnectionBySession(IPlayerSession session) {
            return _connections[session.UserId];
        }
    }
}
