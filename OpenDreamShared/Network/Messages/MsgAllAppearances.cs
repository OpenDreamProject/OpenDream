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
        using var compressed = new MemoryStream(buffer.Data, buffer.PositionInBytes, buffer.LengthBytes - buffer.PositionInBytes);
        var decompressed = DecompressAppearances(compressed);
        var count = decompressed.ReadInt32();
        AllAppearances = new(count);

        for (int i = 0; i < count; i++) {
            var appearance = new ImmutableAppearance(decompressed, serializer);
            AllAppearances.Add(appearance.MustGetId(), appearance);
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) {
        using var compressed = CompressAppearances(AllAppearances.Values, AllAppearances.Count, serializer);

        buffer.Write(compressed.GetBuffer(), 0, (int)compressed.Position);
    }

    public static NetBuffer DecompressAppearances(MemoryStream data) {
        using var decompressStream = new DeflateStream(data, CompressionMode.Decompress);
        var decompressedData = decompressStream.CopyToArray();

        return new NetBuffer {
            Data = decompressedData,
            LengthBytes = decompressedData.Length,
            Position = 0
        };
    }

    public static MemoryStream CompressAppearances(IEnumerable<ImmutableAppearance> appearances, int count, IRobustSerializer serializer) {
        var beforeCompress = new NetBuffer();
        beforeCompress.Write(count);
        foreach (var appearance in appearances) {
            appearance.WriteToBuffer(beforeCompress, serializer);
        }

        var compressBound = ZStd.CompressBound(beforeCompress.LengthBytes);
        var compressedData = new MemoryStream(compressBound);
        using var compressStream = new DeflateStream(compressedData, CompressionMode.Compress);

        compressStream.Write(beforeCompress.Data, 0, beforeCompress.LengthBytes);
        compressStream.Flush();
        var buffer = new MemoryStream();
        buffer.Write(compressedData.GetBuffer(), 0, (int)compressedData.Position);
        return buffer;
    }
}
