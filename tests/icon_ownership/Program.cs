using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Resources;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;

sealed class CountingAllocator : MemoryAllocator {
    private readonly MemoryAllocator _inner = MemoryAllocator.Create();
    public int OutstandingOwners;
    public long OutstandingBytes;
    protected override int GetBufferCapacityInBytes() => 4 * 1024 * 1024;
    public override IMemoryOwner<T> Allocate<T>(int length, AllocationOptions options = AllocationOptions.None) {
        var owner = _inner.Allocate<T>(length, options);
        OutstandingOwners++;
        OutstandingBytes += (long)length * Unsafe.SizeOf<T>();
        return new TrackedOwner<T>(this, owner, (long)length * Unsafe.SizeOf<T>());
    }
    private sealed class TrackedOwner<T>(CountingAllocator allocator, IMemoryOwner<T> owner, long bytes) : IMemoryOwner<T> where T : struct {
        private bool _disposed;
        public Memory<T> Memory => owner.Memory;
        public void Dispose() {
            if (_disposed) return;
            _disposed = true;
            owner.Dispose();
            allocator.OutstandingOwners--;
            allocator.OutstandingBytes -= bytes;
        }
    }
}

static class Program {
    private static readonly FieldInfo TextureField = typeof(IconResource).GetField("_texture", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly List<object> Held = new();
    private static readonly List<object> Samples = new();
    private static int _assertions;
    static void Check(bool condition, string message) {
        _assertions++;
        if (!condition) throw new Exception("REGRESSION: " + message);
    }
    static Image<Rgba32> NewTexture(Configuration config, byte red = 7) {
        var image = new Image<Rgba32>(config, 32, 32);
        image[0, 0] = new Rgba32(red, 23, 41, 255);
        Held.Add(image); // Keep discarded images alive so collection cannot hide omissions.
        return image;
    }
    static byte[] Encode(Image<Rgba32> image) {
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }
    static bool Usable(Image<Rgba32> image) {
        try { _ = image[0, 0]; return true; }
        catch (ObjectDisposedException) { return false; }
    }
    static void EqualLive() {
        var allocator = new CountingAllocator();
        var config = Configuration.Default.Clone();
        config.MemoryAllocator = allocator;
        var manager = new DreamResourceManager();
        Held.Add(manager);
        var texture = NewTexture(config);
        var data = Encode(texture);
        var dmi = new DMIParser.ParsedDMIDescription();
        var cached = manager.CreateIconResource(data, texture, dmi);
        var owners = allocator.OutstandingOwners;
        var bytes = allocator.OutstandingBytes;
        var discarded = new List<Image<Rgba32>>();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (var batch = 0; batch < 10; batch++) {
            for (var i = 0; i < 100; i++) {
                var incoming = NewTexture(config);
                discarded.Add(incoming);
                Check(ReferenceEquals(cached, manager.CreateIconResource(data, incoming, dmi)), "duplicate must return cached resource");
            }
            Samples.Add(new { operations = (batch + 1) * 100, allocator.OutstandingOwners, allocator.OutstandingBytes });
        }
        watch.Stop();
        Check(allocator.OutstandingOwners == owners && allocator.OutstandingBytes == bytes,
              "equal-live incoming owners must be released immediately");
        Check(discarded.All(image => !Usable(image)), "all discarded textures must be disposed");
        Check(manager.CacheCount == 1 && manager.PublishedCount == 1, "equal-live cache/provider must stay bounded");
        Check(ReferenceEquals(cached.Texture, texture) && Usable(texture), "original cached texture must remain usable");
        Check(ReferenceEquals(cached, manager.CreateIconResource(data, texture, dmi)), "same-instance alias returns cached resource");
        Check(Usable(texture) && allocator.OutstandingOwners == owners, "cached texture alias must not be disposed");
        Console.WriteLine($"PASS 1000 equal-live duplicates; retained bytes {bytes}; elapsed milliseconds {watch.Elapsed.TotalMilliseconds:F3}");
    }
    static void ByteOnly() {
        var allocator = new CountingAllocator();
        var config = Configuration.Default.Clone();
        config.MemoryAllocator = allocator;
        var manager = new DreamResourceManager();
        Held.Add(manager);
        var incoming = NewTexture(config);
        var data = Encode(incoming);
        var cached = manager.CreateIconResource(data);
        Check(TextureField.GetValue(cached) is null, "byte-only seed must be lazy");
        Check(ReferenceEquals(cached, manager.CreateIconResource(data, incoming, new())), "byte-only duplicate uses cached resource");
        Check(!Usable(incoming) && allocator.OutstandingOwners == 0, "byte-only duplicate must release incoming texture");
        Check(TextureField.GetValue(cached) is null, "duplicate ownership check must not decode lazy cached texture");
        Check(manager.CacheCount == 1 && manager.PublishedCount == 1, "byte-only duplicate cache must stay bounded");
    }
    static void Adoption() {
        var allocator = new CountingAllocator();
        var config = Configuration.Default.Clone();
        config.MemoryAllocator = allocator;
        var manager = new DreamResourceManager();
        Held.Add(manager);
        var first = NewTexture(config);
        var firstData = Encode(first);
        var cached = manager.CreateIconResource(firstData, first, new());
        var second = NewTexture(config, 8);
        var secondData = Encode(second);
        manager.SeedCollision(secondData, cached);
        var unique = manager.CreateIconResource(secondData, second, new());
        Check(!ReferenceEquals(unique, cached) && ReferenceEquals(unique.Texture, second), "unequal collision bytes must adopt unique image");
        Check(Usable(first) && Usable(second), "unique textures must remain usable");
        Check(manager.CacheCount == 2 && manager.PublishedCount == 2, "unique/collision entries publish once");
        Check(allocator.OutstandingOwners == 2, "adopted image owners remain cached");
        // The resource is cached before provider publication; a later provider failure must not dispose it.
        var third = NewTexture(config, 9);
        manager.FailProvider();
        try { manager.CreateIconResource(Encode(third), third, new()); throw new Exception("Expected provider failure"); }
        catch (InjectedProviderException) { }
        Check(manager.CacheCount == 3 && manager.PublishedCount == 2 && Usable(third), "post-adoption failure must preserve cached texture");
        Check(manager.Cached.Any(resource => ReferenceEquals(TextureField.GetValue(resource), third)), "failed publication still owns cached image");
    }
    public static int Main() {
        try {
            EqualLive();
            ByteOnly();
            Adoption();
            Console.WriteLine($"PASS {_assertions} generated icon ownership assertions");
            Console.WriteLine(JsonSerializer.Serialize(Samples));
            GC.KeepAlive(Held); // No collection or finalizer evidence is used.
            return 0;
        } catch (Exception exception) {
            Console.Error.WriteLine(exception.Message);
            GC.KeepAlive(Held);
            return 1;
        } finally {
            // Dispose owned fixture images only after the assertions, including deliberately broken controls.
            foreach (var image in Held.OfType<Image<Rgba32>>()) image.Dispose();
            foreach (var manager in Held.OfType<DreamResourceManager>())
                foreach (var resource in manager.Cached)
                    if (TextureField.GetValue(resource) is Image<Rgba32> texture) texture.Dispose();
        }
    }
}
