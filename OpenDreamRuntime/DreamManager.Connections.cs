using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenDreamShared;
using OpenDreamShared.Network.Messages;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace OpenDreamRuntime {
    public sealed partial class DreamManager {
        private static readonly byte[] ByondTopicHeaderRaw = { 0x00, 0x83 };
        private static readonly byte[] ByondTopicHeaderEncrypted = { 0x00, 0x15 };

        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IConfigurationManager _config = default!;

        private readonly Dictionary<NetUserId, DreamConnection> _connections = new Dictionary<NetUserId, DreamConnection>();

        public IEnumerable<DreamConnection> Connections => _connections.Values;

        public ushort? ActiveTopicPort {
            get {
                if (_worldTopicSocket is null)
                    return null;

                if (_worldTopicSocket.LocalEndPoint is not IPEndPoint boundEndpoint) {
                    throw new NotSupportedException($"Cannot retrieve bound topic port! Endpoint: {_worldTopicSocket.LocalEndPoint}");
                }

                return (ushort)boundEndpoint.Port;
            }
        }

        private Socket? _worldTopicSocket;

        private Task? _worldTopicListener;
        private CancellationTokenSource? _worldTopicCancellationToken;

        private ulong _topicsProcessed;

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

            var topicPort = _config.GetCVar(OpenDreamCVars.TopicPort);
            var worldTopicAddress = new IPEndPoint(IPAddress.Loopback, topicPort);
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
            var topicId = ++_topicsProcessed;
            try {
                using (remote)
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
                            var totalRead = 0;
                            do {
                                var read = await from.ReceiveAsync(
                                    new Memory<byte>(buffer, totalRead, length - totalRead),
                                    cancellationToken);
                                if(read == 0 && totalRead != length) {
                                    _sawmill.Warning("failed to parse byond topic due to insufficient data read");
                                    return null;
                                }

                                totalRead += read;
                            } while (totalRead < length);

                            return Encoding.ASCII.GetString(buffer[6..^1]);
                        }

                        var topic = await ParseByondTopic(remote);
                        if (topic is null) {
                            return;
                        }

                        var remoteAddress = (remote.RemoteEndPoint as IPEndPoint)!.Address.ToString();
                        _sawmill.Debug($"World Topic #{topicId}: '{remoteAddress}' -> '{topic}'");
                        var tcs = new TaskCompletionSource<DreamValue>();
                        DreamThread.Run("Topic Handler", async state => {
                            var topicProc = WorldInstance.GetProc("Topic");

                            var result = await state.Call(topicProc, WorldInstance, null, new DreamValue(topic), new DreamValue(remoteAddress));
                            tcs.SetResult(result);
                            return result;
                        });

                        var topicResponse = await tcs.Task;
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
            } catch (Exception ex) {
                _sawmill.Warning("Error processing topic #{0}: {1}", topicId, ex);
            } finally {
                _sawmill.Debug("Finished world topic #{0}", topicId);
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
            _playerManager.JoinGame(player);
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

        public DreamConnection GetConnectionBySession(ICommonSession session) {
            return _connections[session.UserId];
        }
    }
}
