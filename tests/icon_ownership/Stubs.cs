using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OpenDreamShared.Resources {
    // DMI parsing is outside this ownership check. Lazy texture decoding remains real ImageSharp.
    public static class DMIParser {
        public sealed class ParsedDMIDescription { }
        public static ParsedDMIDescription ParseDMI(Stream stream) => new();
    }
}

namespace OpenDreamRuntime.Resources {
    public class DreamResource {
        public int Id { get; }
        public byte[]? ResourceData { get; set; }
        public DreamResource(int id, byte[] data) { Id = id; ResourceData = data; }
        public DreamResource(int id, string? filePath, string? resourcePath) { Id = id; }
    }

    public sealed class InjectedProviderException : Exception { }
    public sealed class ProviderAdapter {
        public readonly Dictionary<int, byte[]> Entries = new();
        public bool Fail;
        public void AddResource(int id, byte[] data) {
            if (Fail) throw new InjectedProviderException();
            Entries.Add(id, data);
        }
    }

    public sealed partial class DreamResourceManager {
        private readonly List<DreamResource> _resourceCache = new();
        private readonly Dictionary<string, IconResource> _md5ToGeneratedIcon = new();
        private readonly ProviderAdapter _aczProvider = new();
        public int CacheCount => _resourceCache.Count;
        public int PublishedCount => _aczProvider.Entries.Count;
        public IEnumerable<IconResource> Cached => _resourceCache.Cast<IconResource>();
        public void SeedCollision(byte[] incoming, IconResource previous) => _md5ToGeneratedIcon[CalculateMd5(incoming)] = previous;
        public void FailProvider() => _aczProvider.Fail = true;
    }
}
