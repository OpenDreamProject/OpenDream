using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenDreamShared;
using OpenDreamShared.Network.Messages;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;

namespace OpenDreamRuntime {
    public sealed partial class DreamManager {
        private static readonly byte[] ByondTopicHeaderRaw = { 0x00, 0x83 };
        private static readonly byte[] ByondTopicHeaderEncrypted = { 0x00, 0x15 };

        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IConfigurationManager _config = default!;

        private readonly Dictionary<NetUserId, DreamConnection> _connections = new Dictionary<NetUserId, DreamConnection>();

        public IEnumerable<DreamConnection> Connections => _connections.Values;

        private Socket? _worldTopicSocket;

        private Task? _worldTopicListener;
        private CancellationTokenSource? _worldTopicCancellationToken;

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
            _netManager.RegisterNetMessage<MsgWinGet>();
            _netManager.RegisterNetMessage<MsgFtp>();
            _netManager.RegisterNetMessage<MsgLoadInterface>();
            _netManager.RegisterNetMessage<MsgAckLoadInterface>(RxAckLoadInterface);
            _netManager.RegisterNetMessage<MsgSound>();
            _netManager.RegisterNetMessage<MsgUpdateClientInfo>();

            var worldTopicAddress = new IPEndPoint(IPAddress.Loopback, _config.GetCVar(OpenDreamCVars.TopicPort));
            _sawmill.Debug($"Binding World Topic at {worldTopicAddress}");
            _worldTopicSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) {
                ReceiveTimeout = 5000,
                SendTimeout = 5000,
                ExclusiveAddressUse = false,
            };
            _worldTopicSocket.Bind(worldTopicAddress);
            _worldTopicSocket.Listen();
            _worldTopicCancellationToken = new CancellationTokenSource();
            _worldTopicListener = WorldTopicListener(_worldTopicCancellationToken.Token);
        }

        private void ShutdownConnectionManager() {
            _worldTopicSocket!.Dispose();
            _worldTopicCancellationToken!.Cancel();
        }

        private async Task ConsumeAndHandleWorldTopicSocket(Socket remote, CancellationToken cancellationToken) {
            try {
                async Task<string?> ParseByondTopic(Socket from) {
                    var buffer = new byte[2];
                    await from.ReceiveAsync(buffer, cancellationToken);
                    if (!buffer.SequenceEqual(ByondTopicHeaderRaw)) {
                        if (buffer.SequenceEqual(ByondTopicHeaderEncrypted))
                            _sawmill.Warning("Encrypted World Topic request is not implemented.");
                        return null;
                    }

                    await from.ReceiveAsync(buffer, cancellationToken);
                    if (BitConverter.IsLittleEndian)
                        buffer = buffer.Reverse().ToArray();
                    var length = BitConverter.ToUInt16(buffer);

                    buffer = new byte[length];
                    var read = await from.ReceiveAsync(buffer, cancellationToken);
                    if (read != buffer.Length) {
                        _sawmill.Warning("failed to parse byond topic due to insufficient data read");
                        return null;
                    }

                    return Encoding.ASCII.GetString(buffer[6..^1]);
                }

                var topic = await ParseByondTopic(remote);
                if (topic is null) {
                    return;
                }

                var remoteAddress = (remote.RemoteEndPoint as IPEndPoint)!.Address.ToString();
                _sawmill.Debug($"World Topic: '{remoteAddress}' -> '{topic}'");
                var topicResponse = WorldInstance.SpawnProc("Topic", null, new DreamValue(topic), new DreamValue(remoteAddress));
                if (topicResponse.IsNull) {
                    return;
                }

                byte[] responseData;
                byte responseType;
                switch (topicResponse.Type) {
                    case DreamValue.DreamValueType.Float:
                        responseType = 0x2a;
                        responseData = BitConverter.GetBytes(topicResponse.MustGetValueAsFloat());
                        break;

                    case DreamValue.DreamValueType.String:
                        responseType = 0x06;
                        responseData = Encoding.ASCII.GetBytes(topicResponse.MustGetValueAsString().Replace("\0", "")).Append((byte)0x00).ToArray();
                        break;

                    case DreamValue.DreamValueType.DreamResource:
                    case DreamValue.DreamValueType.DreamObject:
                    case DreamValue.DreamValueType.DreamType:
                    case DreamValue.DreamValueType.DreamProc:
                    case DreamValue.DreamValueType.Appearance:
                    case DreamValue.DreamValueType.ProcStub:
                    case DreamValue.DreamValueType.VerbStub:
                    default:
                        _sawmill.Warning($"Unimplemented /world/Topic response type: {topicResponse.Type}");
                        return;
                }

                var totalLength = (ushort)(responseData.Length + 1);
                var lengthData = BitConverter.GetBytes(totalLength);
                if (BitConverter.IsLittleEndian)
                    lengthData = lengthData.Reverse().ToArray();

                var responseBuffer = new List<byte>(ByondTopicHeaderRaw);
                responseBuffer.AddRange(lengthData);
                responseBuffer.Add(responseType);
                responseBuffer.AddRange(responseData);
                var responseActual = responseBuffer.ToArray();

                var sent = await remote.SendAsync(responseActual, cancellationToken);
                if (sent != responseActual.Length)
                    _sawmill.Warning("Failed to reply to /world/Topic: response buffer not fully sent");

            }
            finally {
                await remote.DisconnectAsync(false, cancellationToken);
            }
        }

        private async Task WorldTopicListener(CancellationToken cancellationToken) {
            if (_worldTopicSocket is null)
                throw new InvalidOperationException("Attempted to start the World Topic Listener without a valid socket bind address.");

            while (!cancellationToken.IsCancellationRequested) {
                var pending = await _worldTopicSocket.AcceptAsync(cancellationToken);
                _ = ConsumeAndHandleWorldTopicSocket(pending, cancellationToken);
            }

            _worldTopicSocket!.Dispose();
            _worldTopicSocket = null!;
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
