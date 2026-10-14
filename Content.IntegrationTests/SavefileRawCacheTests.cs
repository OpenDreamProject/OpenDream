#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenDreamShared;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Resources;

namespace Content.IntegrationTests;

[TestFixture, NonParallelizable]
[Explicit("Fresh process and temporary disk storage required")]
public sealed class SavefileRawCacheTests : ContentIntegrationTest {
    private const int Size = 65536;
    private const int Paths = 16;
    private static string Mode => Environment.GetEnvironmentVariable("RAW_CACHE_MODE") ?? "positive";
    private static bool Retains => Mode is "baseline" or "omit-release";
    private static readonly FieldInfo Backing = typeof(DreamResource).GetField("_resourceDataBacking", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static byte[]? Bytes(DreamResource resource) => (byte[]?)Backing.GetValue(resource);
    private static int DirectoryCount => ((IDictionary)typeof(DreamObjectSavefile).GetField("SavefileDirectories", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!).Count;
    private static int DirtyCount => (int)typeof(DreamObjectSavefile).GetField("SavefilesToFlush", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!.GetType().GetProperty("Count")!.GetValue(typeof(DreamObjectSavefile).GetField("SavefilesToFlush", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null))!;

    [Test]
    public async Task DiskReadLifetimeAndIdentity() {
        var oldDirectory = Directory.GetCurrentDirectory();
        var temporary = Path.Combine(Path.GetTempPath(), "opendream-raw-cache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try {
            var fixture = Path.Combine(temporary, "fixture.dm");
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DMProject", "Fixtures", "savefile-raw-cache.dm"), fixture);
            var compiler = new DMCompiler.DMCompiler();
            Assert.That(compiler.Compile(new() { Files = [fixture], StoreMessages = true }), Is.True,
                "RAW_CACHE_COMPILE: " + string.Join("\n", compiler.CompilerMessages));
            var options = new ServerContentIntegrationOption();
            options.CVarOverrides[OpenDreamCVars.JsonPath.Name] = Path.ChangeExtension(fixture, "json");
            options.CVarOverrides[OpenDreamCVars.TopicPort.Name] = "0";
            var server = StartServer(options);
            await server.WaitIdleAsync();
            await server.WaitRunTicks(1);
            await server.WaitAssertion(() => Check(server.ResolveDependency<DreamManager>(),
                server.ResolveDependency<DreamObjectTree>(), server.ResolveDependency<DreamResourceManager>(),
                server.ResolveDependency<DreamRefManager>(), temporary));
        } finally {
            Directory.SetCurrentDirectory(oldDirectory);
            Directory.Delete(temporary, recursive: true);
        }
    }

    private static void Check(DreamManager manager, DreamObjectTree tree, DreamResourceManager resources, DreamRefManager refs, string root) {
        Assert.That(new[] { "positive", "baseline", "omit-release", "drop-inmemory" }, Does.Contain(Mode));
        var originalOpen = DreamObjectSavefile.Savefiles.Count;
        var originalDirectories = DirectoryCount;
        var originalDirty = DirtyCount;
        var bucket = refs.GetCountOf(RefType.DreamObjectSavefile);
        var datums = refs.GetCountOf(RefType.DreamObjectDatum);
        var lists = refs.GetCountOf(RefType.DreamObjectList);
        foreach (var name in new[] { "raw_cache_seed", "raw_cache_open", "raw_cache_read", "raw_cache_reject", "raw_cache_deleted", "raw_cache_file", "raw_cache_text" })
            Assert.That(tree.TryGetGlobalProc(name, out _), Is.True);
        DreamValue Run(string name, params DreamValue[] arguments) {
            tree.TryGetGlobalProc(name, out var proc);
            return DreamThread.Run(proc!, manager.WorldInstance, null, arguments);
        }
        void Seed(string path) { using var result = Run("raw_cache_seed", new DreamValue(path)); Assert.That(result.MustGetValueAsFloat(), Is.EqualTo(17)); }
        DreamValue Open(string path) => Run("raw_cache_open", new DreamValue(path));
        void Read(DreamValue file) { using var result = Run("raw_cache_read", file); Assert.That(result.MustGetValueAsFloat(), Is.EqualTo(73), "RAW_CACHE_VALUE"); }
        void Clean(string id) {
            Assert.That(manager.LastDMException, Is.Null, id + "_NO_RUNTIME");
            Assert.That(DreamObjectSavefile.Savefiles.Count, Is.EqualTo(originalOpen), id + "_OPEN");
            Assert.That(DirectoryCount, Is.EqualTo(originalDirectories), id + "_DIRECTORIES");
            Assert.That(DirtyCount, Is.EqualTo(originalDirty), id + "_DIRTY");
            Assert.That(refs.GetCountOf(RefType.DreamObjectSavefile), Is.EqualTo(bucket), id + "_SAVEFILE_BUCKET");
            Assert.That(refs.GetCountOf(RefType.DreamObjectDatum), Is.EqualTo(datums), id + "_DATUM_BUCKET");
            Assert.That(refs.GetCountOf(RefType.DreamObjectList), Is.EqualTo(lists), id + "_LIST_BUCKET");
        }
        void Raw(string id, DreamResource[] items, long expected) {
            var actual = items.Sum(item => (long)(Bytes(item)?.Length ?? 0));
            TestContext.Out.WriteLine($"RAW_CACHE_ROW {id} bytes={actual} expected={expected} paths={items.Length}");
            Assert.That(actual, Is.EqualTo(expected), "RAW_CACHE_BYTES_" + id);
        }

        Seed("valid.sav");
        var valid = resources.LoadResource("valid.sav");
        var validLength = new FileInfo(Path.Combine(root, "valid.sav")).Length;
        using (var owner = Open("valid.sav")) {
            Read(owner);
            Raw("valid-open", [valid], Retains ? validLength : 0);
            using (var second = Open("valid.sav")) {
                Read(second);
                Assert.That(second.MustGetValueAsDreamObject(), Is.Not.SameAs(owner.MustGetValueAsDreamObject()));
                Assert.That(DirectoryCount, Is.EqualTo(originalDirectories + 1), "RAW_CACHE_SHARED_ROOT");
            }
            Read(owner);
            Assert.That(DirectoryCount, Is.EqualTo(originalDirectories + 1), "RAW_CACHE_REMAINING_OWNER");
        }
        Raw("valid-close", [valid], 0); Clean("VALID");
        using (var roundtrip = Open("valid.sav")) Read(roundtrip);
        Clean("ROUNDTRIP");

        var malformed = Enumerable.Repeat((byte)' ', Size).ToArray(); malformed[0] = (byte)'!';
        var failed = new DreamResource[Paths];
        for (var i = 0; i < Paths; i++) {
            var path = $"bad-{i}.sav";
            File.WriteAllBytes(Path.Combine(root, path), malformed);
            using var result = Run("raw_cache_reject", new DreamValue(path));
            Assert.That(result.MustGetValueAsFloat(), Is.EqualTo(17), "RAW_CACHE_CAUGHT_PARSE");
            Assert.That(File.ReadAllBytes(Path.Combine(root, path)), Is.EqualTo(malformed), "RAW_CACHE_INPUT_UNCHANGED");
            failed[i] = resources.LoadResource(path);
            Clean("MALFORMED");
        }
        Raw("malformed-unique", failed, Retains ? Paths * Size : 0);
        for (var i = 0; i < Paths; i++) { using var result = Run("raw_cache_reject", new DreamValue("bad-0.sav")); Assert.That(result.MustGetValueAsFloat(), Is.EqualTo(17)); }
        Raw("malformed-repeat", [failed[0]], Retains ? Size : 0); Clean("REPEAT");

        Seed("deleted.sav");
        var deleted = resources.LoadResource("deleted.sav");
        var deletedLength = new FileInfo(Path.Combine(root, "deleted.sav")).Length;
        using (var result = Run("raw_cache_deleted", new DreamValue("deleted.sav"))) Assert.That(result.MustGetValueAsFloat(), Is.EqualTo(73));
        Assert.That(File.Exists(Path.Combine(root, "deleted.sav")), Is.False, "RAW_CACHE_NO_RECREATE");
        Raw("deleted-close", [deleted], Retains ? deletedLength : 0); Clean("DELETED");

        // A genuine production serialization error after Clear, without fake exception adapters.
        Seed("cycle.sav");
        var cycleResource = resources.LoadResource("cycle.sav");
        var cycleLength = new FileInfo(Path.Combine(root, "cycle.sav")).Length;
        var cycleOwner = Open("cycle.sav");
        var cycle = (DreamObjectSavefile)cycleOwner.MustGetValueAsDreamObject()!;
        cycle.CurrentDir["cycle"] = cycle.CurrentDir;
        Assert.Throws<JsonException>(() => cycleOwner.Dispose(), "RAW_CACHE_REAL_SERIALIZE_FAILURE");
        Assert.That(cycle.Deleted, Is.True);
        Assert.That(new FileInfo(Path.Combine(root, "cycle.sav")).Length, Is.Zero, "RAW_CACHE_EXISTING_CLEAR_BEHAVIOR");
        Raw("serialize-failure", [cycleResource], Retains ? cycleLength : 0); Clean("SERIALIZE");

        // Deny the actual append using a held FileShare.None handle after truncation.
        // The direct resource test exercises the same Output failure site; no claim about host permissions.
        Seed("append.sav");
        var appendResource = resources.LoadResource("append.sav");
        var appendLength = new FileInfo(Path.Combine(root, "append.sav")).Length;
        using (var owner = Open("append.sav")) {
            appendResource.Clear();
            using (var lockFile = new FileStream(Path.Combine(root, "append.sav"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Assert.Throws<IOException>(() => appendResource.Output(new DreamValue("blocked")), "RAW_CACHE_REAL_APPEND_FAILURE");
            Raw("append-failure", [appendResource], Retains ? appendLength : 0);
        }
        Clean("APPEND");

        Seed("shared.sav");
        var shared = resources.LoadResource("shared.sav");
        var id = shared.Id;
        var preload = shared.ResourceData!;
        using (var owner = Open("shared.sav")) {
            Read(owner);
            Assert.That(resources.LoadResource("shared.sav"), Is.SameAs(shared));
            Assert.That(shared.Id, Is.EqualTo(id));
            Assert.That(preload.Length, Is.GreaterThan(0), "RAW_CACHE_PREHELD_BYTES_SURVIVE");
            File.WriteAllText(Path.Combine(root, "shared.sav"), "external rewrite\r\n");
            using var text = Run("raw_cache_text", new DreamValue("shared.sav"));
            Assert.That(text.MustGetValueAsString(), Is.EqualTo(Retains ? System.Text.Encoding.UTF8.GetString(preload).Replace("\r\n", "\n") : "external rewrite\n"), "RAW_CACHE_SHARED_FRESH_READ");
            using var file = Run("raw_cache_file", new DreamValue("shared.sav"));
            Assert.That(file.MustGetValueAsDreamResource(), Is.SameAs(shared));
            Read(owner); // The parsed shared directory remains the authoritative open tree.
        }
        Clean("SHARED");
        var embeddedBytes = new byte[] { 1, 2, 3 };
        var embedded = resources.CreateResource(embeddedBytes);
        var release = typeof(DreamResource).GetMethod("ReleaseFileDataCache", BindingFlags.Instance | BindingFlags.NonPublic)!;
        release.Invoke(embedded, null);
        Assert.That(embedded.ResourceData, Is.SameAs(embeddedBytes), "RAW_CACHE_EMBEDDED_UNCHANGED");
        var console = resources.LoadResource("");
        release.Invoke(console, null);
        Assert.That(console.ReadAsString(), Is.Null, "RAW_CACHE_CONSOLE_UNCHANGED");
        TestContext.Out.WriteLine($"RAW_CACHE_COMPLETE mode={Mode} unique={Paths} size={Size} buckets=clean identity=preserved");
    }
}
