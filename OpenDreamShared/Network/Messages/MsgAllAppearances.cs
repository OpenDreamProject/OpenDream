using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Lidgren.Network;
using OpenDreamShared.Dream;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace OpenDreamShared.Network.Messages;

public sealed class MsgAllAppearances(Dictionary<uint, ImmutableAppearance> allAppearances) : NetMessage {
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;
    public Dictionary<uint, ImmutableAppearance> AllAppearances = allAppearances;

    public MsgAllAppearances() : this(new()) { }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) {
        var compressedData = new MemoryStream(buffer.Data, buffer.PositionInBytes, buffer.LengthBytes - buffer.PositionInBytes);
        using var decompressStream = new DeflateStream(compressedData, CompressionMode.Decompress);
        var decompressedData = decompressStream.CopyToArray();
        var decompressed = new NetBuffer {
            Data = decompressedData,
            LengthBytes = decompressedData.Length,
            Position = 0
        };

        var count = decompressed.ReadInt32();
        AllAppearances = new(count);

        for (int i = 0; i < count; i++) {
            var appearance = new ImmutableAppearance(decompressed, serializer);
            AllAppearances.Add(appearance.MustGetId(), appearance);
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        var beforeCompress = new NetBuffer();
        beforeCompress.Write(AllAppearances.Count);
        foreach (var pair in AllAppearances) {
            pair.Value.WriteToBuffer(beforeCompress, serializer);
        }

        var compressBound = ZStd.CompressBound(beforeCompress.LengthBytes);
        var compressedData = new MemoryStream(compressBound);
        using var compressStream = new DeflateStream(compressedData, CompressionMode.Compress);

        compressStream.Write(beforeCompress.Data, 0, beforeCompress.LengthBytes);
        compressStream.Flush();
        buffer.Write(compressedData.GetBuffer(), 0, (int)compressedData.Position);
    }
}
