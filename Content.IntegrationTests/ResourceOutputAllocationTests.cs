#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Resources;
using OpenDreamShared;
using OpenDreamShared.Dream;

namespace Content.IntegrationTests;

[TestFixture, NonParallelizable, Explicit("Isolated resource output and savefile regression")]
public sealed class ResourceOutputAllocationTests : ContentIntegrationTest {
    private sealed class OutputOverride() : DreamResource(1, null, null) {
        public int Calls;
        public override void Output(DreamValue value) => Calls++;
    }

    [Test]
    public void BytesErrorsCache() {
        var temp = Path.Combine(Path.GetTempPath(), "output-contract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try {
            var path = Path.Combine(temp, "nested", "output.txt");
            var resource = new DreamResource(1, path, path);
            var values = new[] { "", "plain", "already\r\n", "é😀\uFF00tail", "\0\t\n", new string('x', 4096), new string('x', 4097), new string('x', 8193) + "😀" };
            var expected = new StringBuilder();
            foreach (var text in values) {
                var previous = resource.ResourceData;
                resource.Output(new DreamValue(text));
                expected.Append(StringFormatDecoder.RemoveFormatting(text)).Append("\r\n");
                Assert.That(resource.ResourceData, Is.EqualTo(new UTF8Encoding(false, true).GetBytes(expected.ToString())), "OUTPUT_BYTES_CRLF");
                if (previous != null) Assert.That(resource.ResourceData, Is.Not.SameAs(previous), "success cache invalidation");
                using var exclusive = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            resource.Output(DreamValue.Null);
            expected.Append("\r\n");
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(new UTF8Encoding(false, true).GetBytes(expected.ToString())), "null is empty line");
            var cached = resource.ResourceData;
            Assert.Throws<DMException>(() => resource.Output(new DreamValue(3)));
            Assert.That(resource.ResourceData, Is.SameAs(cached), "invalid value keeps cache");
            foreach (var chars in new[] { 0, 4097 }) {
                Assert.Throws<EncoderFallbackException>(() => resource.Output(new DreamValue(new string('x', chars) + "invalid\uD800")));
                Assert.That(resource.ResourceData, Is.SameAs(cached), "encoding failure keeps cache");
                using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            }
            var missingPath = new DreamResource(1, null, null);
            Assert.Throws<Exception>(() => missingPath.Output(new DreamValue("x")));
            var overrideResource = new OutputOverride();
            DreamResource dispatch = overrideResource;
            dispatch.Output(new DreamValue("x"));
            Assert.That(overrideResource.Calls, Is.EqualTo(1));
            // A failed append must not invalidate the cached bytes.
            File.Delete(path);
            Directory.CreateDirectory(path);
            foreach (var chars in new[] { 1, 4097 }) {
                Assert.Throws<UnauthorizedAccessException>(() => resource.Output(new DreamValue(new string('x', chars))));
                Assert.That(resource.ResourceData, Is.SameAs(cached));
            }
            Directory.Delete(path);
        } finally { Directory.Delete(temp, recursive: true); }
    }

    [Test]
    public void AllocationWorkloads() {
        var path = Path.Combine(Path.GetTempPath(), "output-allocation-" + Guid.NewGuid().ToString("N"));
        try {
            var resource = new DreamResource(1, path, path);
            long largestBytesPerCall = 0;
            long worstSmallExcess = long.MinValue;
            foreach (var chars in new[] { 16, 1024, 4096, 4097, 1048576, 8388608 }) {
                var text = new string('x', chars);
                var value = new DreamValue(text);
                for (var warmup = 0; warmup < 3; warmup++) { resource.Clear(); resource.Output(value); }
                long allocated = 0, ticks = 0;
                var gc = Enumerable.Range(0, 3).Select(GC.CollectionCount).ToArray();
                const int count = 8;
                for (var i = 0; i < count; i++) {
                    resource.Clear();
                    var startBytes = GC.GetAllocatedBytesForCurrentThread();
                    var start = Stopwatch.GetTimestamp();
                    resource.Output(value);
                    ticks += Stopwatch.GetTimestamp() - start;
                    allocated += GC.GetAllocatedBytesForCurrentThread() - startBytes;
                }
                var collections = Enumerable.Range(0, 3).Select(i => GC.CollectionCount(i) - gc[i]).ToArray();
                Assert.That(new FileInfo(path).Length, Is.GreaterThanOrEqualTo(chars));
                TestContext.Out.WriteLine("OUTPUT_ALLOCATION_METRIC " + JsonSerializer.Serialize(new {
                    chars, count, allocated, seconds = ticks / (double)Stopwatch.Frequency, collections,
                    runtime = Environment.Version.ToString(), os = Environment.OSVersion.ToString()
                }));
                if (chars <= 4096) {
                    worstSmallExcess = Math.Max(worstSmallExcess, allocated / count - (2 * chars + 1024));
                }
                if (chars == 8388608) largestBytesPerCall = allocated / count;
            }
            Assert.That(worstSmallExcess, Is.LessThan(0), "OUTPUT_SMALL_ALLOCATION");
            Assert.That(largestBytesPerCall, Is.LessThan(8388608), "OUTPUT_ALLOCATION_SCALING");
        } finally { File.Delete(path); }
    }

    [Test]
    public async Task SavefileRoundTrip() {
        var originalDirectory = Directory.GetCurrentDirectory();
        var temporary = Path.Combine(Path.GetTempPath(), "output-savefile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try {
            var fixture = Path.Combine(temporary, "fixture.dm");
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DMProject", "Fixtures", "resource-output-allocation.dm"), fixture);
            var compiler = new DMCompiler.DMCompiler();
            Assert.That(compiler.Compile(new() { Files = [fixture], StoreMessages = true }), Is.True,
                string.Join("\n", compiler.CompilerMessages));
            var options = new ServerContentIntegrationOption();
            options.CVarOverrides[OpenDreamCVars.JsonPath.Name] = Path.ChangeExtension(fixture, "json");
            options.CVarOverrides[OpenDreamCVars.TopicPort.Name] = "0";
            var server = StartServer(options);
            await server.WaitIdleAsync();
            await server.WaitRunTicks(1);
            await server.WaitAssertion(() => {
                var manager = server.ResolveDependency<DreamManager>();
                var tree = server.ResolveDependency<DreamObjectTree>();
                Assert.That(tree.TryGetGlobalProc("output_savefile_roundtrip", out var proc), Is.True);
                using var result = DreamThread.Run(proc!, manager.WorldInstance, null);
                Assert.That(result.MustGetValueAsFloat(), Is.EqualTo(1));
                Assert.That(manager.LastDMException, Is.Null);
                var bytes = File.ReadAllBytes("output-roundtrip.sav");
                Assert.That(bytes[^2..], Is.EqualTo(new byte[] { 13, 10 }), "OUTPUT_BYTES_CRLF");
                Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 239, 187, 191 }));
                using var parsed = JsonDocument.Parse(bytes);
                Assert.That(Encoding.UTF8.GetString(bytes), Does.Contain("\\uFF00"));
            });
        } finally {
            Directory.SetCurrentDirectory(originalDirectory);
            Directory.Delete(temporary, recursive: true);
        }
    }
}
