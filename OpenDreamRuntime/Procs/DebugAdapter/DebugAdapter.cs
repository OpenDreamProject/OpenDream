using System.Net;
using System.Net.Sockets;
using OpenDreamRuntime.Procs.DebugAdapter.Protocol;

namespace OpenDreamRuntime.Procs.DebugAdapter;

public sealed class DebugAdapter {
    public delegate void OnClientConnectedHandler(DebugAdapterClient client);

    public event OnClientConnectedHandler? OnClientConnected;

    private readonly TcpListener _listener;
    private readonly List<DebugAdapterClient> _clients = new();

    public DebugAdapter(string? host = null, int? port = null) {
        if (!IPAddress.TryParse(host, out IPAddress? hostAddress)) {
            hostAddress = IPAddress.Any;
        }

        _listener = new TcpListener(hostAddress, port ?? 25567);
    }

    public void StartListening() {
        _listener.Start();
    }

    public void HandleMessages() {
        while (_listener.Pending()) {
            var client = new DebugAdapterClient(_listener.AcceptTcpClient());

            _clients.Add(client);
            OnClientConnected?.Invoke(client);
        }

        for (int i = 0; i < _clients.Count; i++) {
            DebugAdapterClient client = _clients[i];

            if (client.Connected) {
                client.HandleMessages();
            } else {
                _clients.RemoveAt(i--);
            }
        }
    }

    public void Shutdown() {
        foreach (DebugAdapterClient client in _clients) {
            client.Close();
        }

        _clients.Clear();
    }

    public void SendAll<T>(T message) where T : ProtocolMessage {
        foreach (DebugAdapterClient client in _clients) {
            client.SendMessage(message);
        }
    }
}
