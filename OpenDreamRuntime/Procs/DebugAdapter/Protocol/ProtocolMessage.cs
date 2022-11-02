﻿using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[Virtual]
public class ProtocolMessage {
    [JsonPropertyName("seq")] public int Seq { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; }

    public ProtocolMessage() { }

    protected ProtocolMessage(string type) {
        Type = type;
    }
}
