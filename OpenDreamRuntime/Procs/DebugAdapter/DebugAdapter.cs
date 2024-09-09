using System.Net.Sockets;
using OpenDreamRuntime.Procs.DebugAdapter.Protocol;

namespace OpenDreamRuntime.Procs.DebugAdapter;

public sealed class DebugAdapter {
    public delegate void OnClientConnectedHandler(DebugAdapterClient client);

    public event OnClientConnectedHandler? OnClientConnected;

    private readonly List<DebugAdapterClient> _clients = new();

    public bool AnyClientsConnected() => _clients.Count > 0;

    public void ConnectOut(string? host = null, int? port = null) {
        var client = new DebugAdapterClient(new TcpClient(host ?? "127.0.0.1", port ?? 25567));
        _clients.Add(client);
        OnClientConnected?.Invoke(client);
    }

    public void HandleMessages() {
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

    public void SendAll(IEvent evt) => SendAll(evt.ToEvent());
}
