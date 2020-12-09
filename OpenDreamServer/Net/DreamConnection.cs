using OpenDreamServer.Dream;
using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;

namespace OpenDreamServer.Net {
    class DreamConnection {
        public string CKey = null;
        public List<int> PressedKeys = new List<int>();
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
                SendPacket(new PacketOutput(value.GetValueAsString(), null));
            } else if (value.Type == DreamValue.DreamValueType.Integer) {
                SendPacket(new PacketOutput(value.GetValueAsInteger().ToString(), null));
            } else if (value.Type == DreamValue.DreamValueType.DreamObject) {
                DreamObject outputObject = value.GetValueAsDreamObject();

                if (outputObject != null) {
                    if (outputObject.IsSubtypeOf(DreamPath.Sound)) {
                        UInt16 channel = (UInt16)outputObject.GetVariable("channel").GetValueAsInteger();
                        DreamValue file = outputObject.GetVariable("file"); 
                        UInt16 volume = (UInt16)outputObject.GetVariable("volume").GetValueAsInteger();
                        
                        if (file.IsType(DreamValue.DreamValueType.String) || file.Value == null) {
                            SendPacket(new PacketSound(channel, (string)file.Value, volume));
                        } else if (file.IsType(DreamValue.DreamValueType.DreamResource)) {
                            SendPacket(new PacketSound(channel, file.GetValueAsDreamResource().ResourcePath, volume));
                        } else {
                            throw new ArgumentException("Cannot output " + value, nameof(value));
                        }
                    }
                }
            } else {
                throw new ArgumentException("Cannot output " + value, nameof(value));
            }
        }

        public void Browse(string body, string options) {
            string window = null;
            Size size = new Size(480, 480);

            string[] separated = options.Split(',', ';', '&');
            foreach (string option in separated) {
                string optionTrimmed = option.Trim();

                if (optionTrimmed != String.Empty) {
                    string[] optionSeparated = optionTrimmed.Split("=");
                    string key = optionSeparated[0];
                    string value = optionSeparated[1];

                    if (key == "window") window = value;
                    if (key == "size") {
                        string[] sizeSeparated = value.Split("x");

                        size = new Size(int.Parse(sizeSeparated[0]), int.Parse(sizeSeparated[1]));
                    }
                }
            }

            SendPacket(new PacketBrowse(window, body) {
                Size = size
            });
        }

        public void BrowseResource(DreamResource resource, string filename) {
            SendPacket(new PacketBrowseResource(filename, resource.ResourceData));
        }

        public void OutputControl(string message, string control) {
            SendPacket(new PacketOutput(message, control));
        }
    }
}
