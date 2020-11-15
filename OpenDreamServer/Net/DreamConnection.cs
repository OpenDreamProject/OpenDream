using OpenDreamServer.Dream;
using OpenDreamServer.Dream.Objects;
using OpenDreamShared.Dream;
using OpenDreamShared.Net.Packets;
using System;
using System.IO;
using System.Net.Sockets;

namespace OpenDreamServer.Net {
    class DreamConnection {
        public string CKey = null;
        public DreamObject ClientDreamObject = null;
        public DreamObject MobDreamObject {
            get => _mobDreamObject;
            set {
                if (_mobDreamObject != value) {
                    if (_mobDreamObject != null) _mobDreamObject.CallProc("Logout");

                    if (value != null && value.IsSubtypeOf(DreamPath.Mob)) {
                        DreamConnection oldMobConnection = Program.DreamServer.GetConnectionFromMob(value);
                        if (oldMobConnection != null) oldMobConnection.MobDreamObject = null;

                        _mobDreamObject = value;
                        ClientDreamObject?.SetVariable("eye", new DreamValue(_mobDreamObject));
                        _mobDreamObject.CallProc("Login");
                    } else {
                        _mobDreamObject = null;
                    }
                }
            }
        }

        private DreamObject _mobDreamObject = null;

        private TcpClient _tcpClient;
        private NetworkStream _tcpStream;
        private BinaryReader _tcpStreamBinaryReader;
        private BinaryWriter _tcpStreamBinaryWriter;
        private object _netLock = new object();

        public DreamConnection(TcpClient tcpClient) {
            _tcpClient = tcpClient;
            _tcpStream = _tcpClient.GetStream();
            _tcpStreamBinaryReader = new BinaryReader(_tcpStream);
            _tcpStreamBinaryWriter = new BinaryWriter(_tcpStream);
        }

        public byte[] ReadPacketData() {
            lock (_netLock) {
                if (_tcpClient.Connected && _tcpStream.DataAvailable) {
                    UInt32 packetDataLength = _tcpStreamBinaryReader.ReadUInt32();
                    byte[] packetData = new byte[packetDataLength];

                    int bytesRead = _tcpStream.Read(packetData, 0, (int)packetDataLength);
                    while (bytesRead < packetDataLength) {
                        bytesRead += _tcpStream.Read(packetData, bytesRead, (int)packetDataLength - bytesRead);
                    }

                    return packetData;
                } else {
                    return null;
                }
            }
        }

        public void SendPacket(IPacket packet) {
            PacketStream stream = new PacketStream();

            stream.WriteByte((byte)packet.PacketID);
            packet.WriteToStream(stream);

            lock (_netLock) {
                _tcpStreamBinaryWriter.Write((UInt32)stream.Length);
                _tcpStream.Write(stream.ToArray());
            }
        }

        public void OutputDreamValue(DreamValue value) {
            if (value.Type == DreamValue.DreamValueType.String) {
                SendPacket(new PacketOutput(value.GetValueAsString()));
            } else if (value.Type == DreamValue.DreamValueType.Integer) {
                SendPacket(new PacketOutput(value.GetValueAsInteger().ToString()));
            } else if (value.Type == DreamValue.DreamValueType.DreamObject) {
                DreamObject outputObject = value.GetValueAsDreamObject();

                if (outputObject != null) {
                    if (outputObject.IsSubtypeOf(DreamPath.Sound)) {
                        UInt16 channel = (UInt16)outputObject.GetVariable("channel").GetValueAsInteger();
                        DreamValue file = outputObject.GetVariable("file"); 
                        UInt16 volume = (UInt16)outputObject.GetVariable("volume").GetValueAsInteger();

                        if (file.IsType(DreamValue.DreamValueType.String) || file.Value == null) {
                            PacketOutput.OutputSound outputValue = new PacketOutput.OutputSound(channel, (string)file.Value, volume);

                            SendPacket(new PacketOutput(outputValue));
                        } else if (file.IsType(DreamValue.DreamValueType.DreamResource)) {
                            PacketOutput.OutputSound outputValue = new PacketOutput.OutputSound(channel, file.GetValueAsDreamResource().ResourcePath, volume);

                            SendPacket(new PacketOutput(outputValue));
                        } else {
                            throw new ArgumentException("Cannot output " + value, nameof(value));
                        }
                    }
                }
            } else {
                throw new ArgumentException("Cannot output " + value, nameof(value));
            }
        }
    }
}
