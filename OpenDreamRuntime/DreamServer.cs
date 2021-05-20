using OpenDreamVM.Objects;
using OpenDreamShared.Net.Packets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace OpenDreamVM {
    public delegate void DreamConnectionReadyEventHandler(DreamConnection connection);
    public abstract class DreamServer {
        // Interface
        public abstract void Start();
        public abstract void Process();

        // Implementation
        public readonly List<DreamConnection> Connections = new();
        public event DreamConnectionReadyEventHandler DreamConnectionRequest;

        protected Dictionary<PacketID, Action<DreamConnection, IPacket>> _packetIDToCallback = new Dictionary<PacketID, Action<DreamConnection, IPacket>>();

        public readonly IPAddress Address;
        public readonly int Port;

        internal void RegisterPacketCallback<PacketClass>(PacketID packetID, Action<DreamConnection, PacketClass> packetCallback) where PacketClass : IPacket, new() {
            if (_packetIDToCallback.ContainsKey(packetID)) throw new Exception("Packet ID '" + packetID + "' already has a callback");

            if (packetCallback != null) {
                _packetIDToCallback[packetID] = (DreamConnection connection, IPacket packet) => {
                    packetCallback.Invoke(connection, (PacketClass)packet);
                };
            } else {
                throw new ArgumentNullException("packetCallback");
            }
        }

        public DreamConnection GetConnectionFromCKey(string cKey) {
            foreach (DreamConnection connection in Connections) {
                if (connection.CKey == cKey) return connection;
            }

            return null;
        }

        public DreamConnection GetConnectionFromMob(DreamObject mobObject) {
            foreach (DreamConnection connection in Connections) {
                if (connection.MobDreamObject == mobObject) return connection;
            }

            return null;
        }

        public DreamConnection GetConnectionFromClient(DreamObject clientObject) {
            foreach (DreamConnection connection in Connections) {
                if (connection.ClientDreamObject == clientObject) return connection;
            }

            return null;
        }
    }
}
