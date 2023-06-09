using System.IO;

namespace OpenDreamClient.Resources.ResourceTypes;

public abstract class DreamResource {
    public readonly int Id;

    protected readonly byte[] Data;

    protected DreamResource(int id, byte[] data) {
        Id = id;
        Data = data;
    }

    public void WriteTo(Stream stream) {
        stream.Write(Data);
    }
}
