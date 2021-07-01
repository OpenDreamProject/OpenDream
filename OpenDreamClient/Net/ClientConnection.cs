using System;
using System.Collections.Generic;
using System.Net.Sockets;
using OpenDreamShared.Net.Packets;
using System.IO;

namespace OpenDreamClient.Net {
    class ClientConnection {
        private string _serverIP;
        private int _serverPort;
        private TcpClient _tcpClient;
        private NetworkStream _tcpStream;
        private BinaryReader _tcpStreamBinaryReader;
        private BinaryWriter _tcpStreamBinaryWriter;
        private Dictionary<PacketID, Action<IPacket>> _packetIDToCallback = new Dictionary<PacketID, Action<IPacket>>();

        public bool Connected => _tcpClient is { Connected: true };

        public void Connect(string serverIP, int serverPort) {
            _serverIP = serverIP;
            _serverPort = serverPort;

            _tcpClient = new TcpClient(_serverIP, _serverPort);
            _tcpStream = _tcpClient.GetStream();
            _tcpStreamBinaryReader = new BinaryReader(_tcpStream);
            _tcpStreamBinaryWriter = new BinaryWriter(_tcpStream);
        }

        public void Close() {
            _tcpStream.Close();
            _tcpClient.Close();
        }

        public void SendPacket(IPacket packet) {
            PacketStream stream = new PacketStream();

            stream.WriteByte((byte)packet.PacketID);
            packet.WriteToStream(stream);
            _tcpStreamBinaryWriter.Write((UInt32)stream.Length);
            _tcpStream.Write(stream.ToArray());
        }

        public void RegisterPacketCallback<PacketClass>(PacketID packetID, Action<PacketClass> packetCallback) where PacketClass:IPacket, new() {
            if (_packetIDToCallback.ContainsKey(packetID)) throw new Exception("Packet ID '" + packetID + "' already has a callback");

            if (packetCallback != null) {
                _packetIDToCallback[packetID] = (IPacket packet) => {
                    packetCallback.Invoke((PacketClass)packet);
                };
            } else {
                throw new ArgumentNullException("packetCallback");
            }
        }

        public void ProcessPackets() {
            while (this.Connected && _tcpStream.DataAvailable) {
                try {
                    UInt32 packetLength = _tcpStreamBinaryReader.ReadUInt32();
                    byte[] packetBuffer = new byte[packetLength];

                    int bytesRead = _tcpStream.Read(packetBuffer, 0, (int)packetLength);
                    while (bytesRead < packetLength) {
                        bytesRead += _tcpStream.Read(packetBuffer, bytesRead, (int)packetLength - bytesRead);
                    }

                    IPacket packet = IPacket.CreatePacketFromData(packetBuffer);
                    try {
                        _packetIDToCallback[packet.PacketID]?.Invoke(packet);
                    } catch (Exception e) {
                        Console.Error.WriteLine("Error while handling received packet (" + packet.PacketID + "): " + e);
                    }
                } catch (Exception e) {
                    Console.WriteLine("Error while processing packets: " + e);
                }
            }
        }
    }
}
