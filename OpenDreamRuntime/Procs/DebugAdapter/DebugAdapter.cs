using System.Net;
using System.Net.Sockets;
using OpenDreamRuntime.Procs.DebugAdapter.Protocol;

namespace OpenDreamRuntime.Procs.DebugAdapter;

public sealed class DebugAdapter {
    public delegate void OnClientConnectedHandler(DebugAdapterClient client);

    public event OnClientConnectedHandler? OnClientConnected;

    private TcpListener? _listener;
    private readonly List<DebugAdapterClient> _clients = new();

    public DebugAdapter() {
    }

    public void StartListening(string? host = null, int? port = null) {
        if (!IPAddress.TryParse(host, out IPAddress? hostAddress)) {
            hostAddress = IPAddress.Any;
        }
        _listener = new TcpListener(hostAddress, port ?? 25567);
        _listener.Start();
    }

    public void ConnectOut(string? host = null, int? port = null) {
        var client = new DebugAdapterClient(new TcpClient(host ?? "127.0.0.1", port ?? 25567));
        _clients.Add(client);
        OnClientConnected?.Invoke(client);
    }

    public void HandleMessages() {
        if (_listener != null) {
            while (_listener.Pending()) {
                var client = new DebugAdapterClient(_listener.AcceptTcpClient());

                _clients.Add(client);
                OnClientConnected?.Invoke(client);
            }
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

    public void SendAll(IEvent evt) => SendAll(evt.ToEvent());
}
