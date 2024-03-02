using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using OpenDreamRuntime.Procs.DebugAdapter.Protocol;

namespace OpenDreamRuntime.Procs.DebugAdapter;

public sealed class DebugAdapterClient {
    public delegate void OnRequestHandler(DebugAdapterClient client, Request req);

    public event OnRequestHandler? OnRequest;

    public bool Connected => _client.Connected;

    private readonly TcpClient _client;
    private readonly NetworkStream _netStream;
    private readonly StreamReader _netReader;
    private readonly StreamWriter _netWriter;
    private readonly ISawmill _sawmill;

    private int _seqCounter;

    public DebugAdapterClient(TcpClient client) {
        _client = client;
        _netStream = _client.GetStream();
        _netReader = new StreamReader(_netStream, Encoding.ASCII);
        _netWriter = new StreamWriter(_netStream, Encoding.ASCII);

        _sawmill = Logger.GetSawmill("opendream.debug_adapter");
        _sawmill.Info($"Accepted debug adapter client {_client.Client.RemoteEndPoint?.Serialize()}");
    }

    public void HandleMessages() {
        // `_netStream.DataAvailable` goes to false as soon as there is one Read call.
        // `_client` and `_netReader` each keep buffers and we have to loop until they are all drained.
        try {
            _netReader.BaseStream.ReadTimeout = 1; //1ms is lowest possible value
            while (_client.Connected && (_netStream.DataAvailable || _client.Available > 0) &&  ReadRequest() is { } message) {
                _sawmill.Log(LogLevel.Verbose, $"Parsed {message}");
                _seqCounter = message.Seq + 1;
                switch (message) {
                    case Request req:
                        OnRequest?.Invoke(this, req);
                        break;
                }
            }
        } catch (IOException) {} //ignore timeouts
    }

    public void Close() {
        _client.Close();
    }

    public void SendMessage<T>(T message) where T : ProtocolMessage {
        message.Seq = _seqCounter++;

        string body = JsonSerializer.Serialize(message);
        string header = $"Content-Length: {body.Length}\r\n\r\n";

        _sawmill.Log(LogLevel.Verbose, $"Sending {body.Length} {body}");
        _netWriter.BaseStream.Write(Encoding.ASCII.GetBytes(header));
        _netWriter.BaseStream.Write(Encoding.UTF8.GetBytes(body));
    }

    public void SendMessage(IEvent evt) => SendMessage(evt.ToEvent());

    private ProtocolMessage? ReadRequest() {
        ProtocolHeader? header = ReadProtocolMessageHeader();
        if (header == null)
            return null;

        string? body = ReadProtocolMessageBody(header.ContentLength);
        if (body == null)
            return null;

        using var messageJson = JsonSerializer.Deserialize<JsonDocument>(body);
        if (messageJson == null) {
            _sawmill.Error($"Failed to deserialize message");
            return null;
        }

        try {
            var message = messageJson.Deserialize<ProtocolMessage>();
            if (message == null) {
                _sawmill.Error($"Failed to deserialize message");
                return null;
            }

            switch (message.Type) {
                case "request": return Request.DeserializeRequest(messageJson);
                default:
                    _sawmill.Error($"Unrecognized \"type\" field: {message.Type}");
                    return null;
            }
        } catch (Exception e) {
            _sawmill.Error($"Exception while deserializing message: {e.Message}\n{e.StackTrace}");
            return null;
        }
    }

    private sealed class ProtocolHeader {
        public int ContentLength;
    }

    private ProtocolHeader? ReadProtocolMessageHeader() {
        int contentLength = -1;

        string? headerLine;
        while ((headerLine = _netReader.ReadLine()) != String.Empty && headerLine != null) {
            string[] split = headerLine.Split(": ");
            string field = split[0];
            string value = split[1];

            if (field == "Content-Length") {
                if (!int.TryParse(value, out contentLength)) {
                    _sawmill.Error($"Invalid Content-Length field: {value}");
                    return null;
                }
            }
        }

        if (headerLine == null) {
            _sawmill.Error("Reached the end of the stream while reading the header");
            return null;
        }

        if (contentLength == -1) {
            _sawmill.Error("Header did not contain a \"Content-Length\" field");
            return null;
        }

        return new ProtocolHeader {
            ContentLength = contentLength
        };
    }

    private string? ReadProtocolMessageBody(int contentLength) {
        char[] buffer = new char[contentLength];
        int read = _netReader.Read(buffer, 0, contentLength);
        if (read != contentLength) {
            _sawmill.Error($"Expected to read {contentLength} bytes but got {read} instead");
            return null;
        }

        string content = new String(buffer);
        _sawmill.Log(LogLevel.Verbose, $"Received {contentLength} {content}");

        return content;
    }
}
