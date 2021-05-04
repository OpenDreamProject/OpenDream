using OpenDreamServer.Dream.Objects;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace OpenDreamServer.Net {
    delegate void DreamConnectionReadyEventHandler(DreamConnection connection);

    class DreamServer {
        public List<DreamConnection> DreamConnections = new List<DreamConnection>();
        public event DreamConnectionReadyEventHandler DreamConnectionRequest;

        private TcpListener _tcpListener;
        private Dictionary<PacketID, Action<DreamConnection, IPacket>> _packetIDToCallback = new Dictionary<PacketID, Action<DreamConnection, IPacket>>();
        private Dictionary<string, DreamConnection> _ckeyToConnection = new Dictionary<string, DreamConnection>();
        
        public DreamServer(int port) {
            _tcpListener = new TcpListener(IPAddress.Any, port);

            RegisterPacketCallback<PacketRequestConnect>(PacketID.RequestConnect, OnPacketRequestConnect);
        }

        public void Start() {
            _ckeyToConnection.Clear();
            _tcpListener.Start();
        }

        public void RegisterPacketCallback<PacketClass>(PacketID packetID, Action<DreamConnection, PacketClass> packetCallback) where PacketClass : IPacket, new() {
            if (_packetIDToCallback.ContainsKey(packetID)) throw new Exception("Packet ID '" + packetID.ToString() + "' already has a callback");

            if (packetCallback != null) {
                _packetIDToCallback[packetID] = (DreamConnection connection, IPacket packet) => {
                    packetCallback.Invoke(connection, (PacketClass)packet);
                };
            } else {
                throw new ArgumentNullException("packetCallback");
            }
        }

        public DreamConnection GetConnectionFromCKey(string cKey) {
            foreach (DreamConnection connection in DreamConnections) {
                if (connection.CKey == cKey) return connection;
            }

            return null;
        }

        public DreamConnection GetConnectionFromMob(DreamObject mobObject) {
            foreach (DreamConnection connection in DreamConnections) {
                if (connection.MobDreamObject == mobObject) return connection;
            }

            return null;
        }

        public void Process() {
            ProcessConnections();
            ProcessPackets();
        }

        private void ProcessConnections() {
            while (_tcpListener.Pending()) {
                TcpClient tcpClient = _tcpListener.AcceptTcpClient();
                DreamConnection dreamConnection = new DreamConnection(tcpClient);

                DreamConnections.Add(dreamConnection);
            }
        }

        private void ProcessPackets() {
            foreach (DreamConnection dreamConnection in DreamConnections) {
                byte[] packetData;

                try {
                    while ((packetData = dreamConnection.ReadPacketData()) != null) {
                        IPacket packet = IPacket.CreatePacketFromData(packetData);

                        try {
                            _packetIDToCallback[packet.PacketID]?.Invoke(dreamConnection, packet);
                        } catch (Exception e) {
                            Console.Error.WriteLine("Error while handling received packet (" + packet.PacketID.ToString() + "): " + e.Message);
                        }
                    }
                } catch (Exception e) {
                    Console.Error.WriteLine("Error while processing recieved packet from user '" + dreamConnection.CKey + "': " + e.Message);
                }
            }
        }

        private void OnPacketRequestConnect(DreamConnection connection, PacketRequestConnect pRequestConnect) {
            if (!_ckeyToConnection.ContainsKey(pRequestConnect.CKey)) {
                connection.CKey = pRequestConnect.CKey;

                DreamConnectionRequest.Invoke(connection);
            } else {
                connection.SendPacket(new PacketConnectionResult(false, "A connection with your ckey already exists"));
            }
        }
    }
}
