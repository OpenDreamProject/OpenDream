#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenDreamShared;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Resources;

namespace Content.IntegrationTests;

[TestFixture, NonParallelizable]
[Explicit("Requires an isolated integration host")]
public sealed class SavefileResourceBytesTests : ContentIntegrationTest {
    [Test]
    public async Task ResourceIdentityAndSharedBytes() {
        var originalDirectory = Directory.GetCurrentDirectory();
        var temporary = Path.Combine(Path.GetTempPath(), "opendream-resource-bytes-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try {
            var fixture = Path.Combine(temporary, "fixture.dm");
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DMProject", "Fixtures", "savefile-resource-bytes.dm"), fixture);
            var payload = Enumerable.Range(0, 65536).Select(i => (byte)(i % 251)).ToArray();
            File.WriteAllBytes(Path.Combine(temporary, "payload.bin"), payload);
            var compiler = new DMCompiler.DMCompiler();
            var compiled = compiler.Compile(new() { Files = [fixture], StoreMessages = true });
            var json = Path.ChangeExtension(fixture, "json");
            Assert.That(compiled && File.Exists(json), Is.True,
                "Isolated DM fixture compilation failed: " + string.Join("\n", compiler.CompilerMessages));
            var options = new ServerContentIntegrationOption();
            options.CVarOverrides[OpenDreamCVars.JsonPath.Name] = json;
            options.CVarOverrides[OpenDreamCVars.TopicPort.Name] = (ushort)0;
            var server = StartServer(options);
            await server.WaitIdleAsync();
            await server.WaitRunTicks(1);
            await server.WaitAssertion(() => CheckResources(server.ResolveDependency<DreamManager>(),
                server.ResolveDependency<DreamObjectTree>(), server.ResolveDependency<DreamResourceManager>(),
                server.ResolveDependency<DreamRefManager>(), payload));
        } finally {
            Directory.SetCurrentDirectory(originalDirectory);
            Directory.Delete(temporary, recursive: true);
        }
    }

    private static T Field<T>(object value, string name) =>
        (T)value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!;

    private static void CheckResources(DreamManager manager, DreamObjectTree tree,
        DreamResourceManager resources, DreamRefManager refs, byte[] payload) {
        Assert.That(manager.LastDMException, Is.Null);
        var listCount = refs.GetCountOf(RefType.DreamObjectList);
        var datumCount = refs.GetCountOf(RefType.DreamObjectDatum);
        var openCount = OpenDreamRuntime.Objects.Types.DreamObjectSavefile.Savefiles.Count;
        Assert.That(tree.TryGetGlobalProc("savefile_resource_reads", out var reads), Is.True);
        Assert.That(tree.TryGetGlobalProc("savefile_resource_identity", out var identity), Is.True);
        Assert.That(tree.TryGetGlobalProc("savefile_resource_drop", out var drop), Is.True);
        var provider = Field<DreamAczProvider>(resources, "_aczProvider");
        var downloads = Field<Dictionary<int, byte[]>>(provider, "_extraResources");
        var startDownloadCount = downloads.Count;
        var decoded = new List<DreamResource>();
        for (var pass = 0; pass < 2; pass++) {
            using var result = DreamThread.Run(reads!, manager.WorldInstance, null,
                new DreamValue("payload.bin"), new DreamValue(1000));
            var values = result.MustGetValueAsIDreamList();
            Assert.That(values.GetLength(), Is.EqualTo(1000));
            decoded.AddRange(values.GetValues().Select(v => v.MustGetValueAsDreamResource()));
        }
        using (var result = DreamThread.Run(drop!, manager.WorldInstance, null,
                   new DreamValue("payload.bin")))
            Assert.That(result.MustGetValueAsFloat(), Is.EqualTo(17));
        // The dropped third pass must still retain its distinct IDs, but reuse bytes.
        var generated = downloads.Where(p => p.Key >= decoded[0].Id).Select(p => p.Value).ToArray();
        Assert.That(downloads.Count - startDownloadCount, Is.EqualTo(3000));
        Assert.That(generated.Length, Is.EqualTo(3000));
        Assert.That(decoded.Select(r => r.Id).Distinct().Count(), Is.EqualTo(2000));
        Assert.That(decoded.All(r => r.GetType() == typeof(DreamResource) && r.ResourcePath == null), Is.True);
        Assert.That(decoded.All(r => r.ResourceData!.SequenceEqual(payload)), Is.True);
        foreach (var resource in decoded) {
            Assert.That(resources.TryLoadResource(resource.Id, out var found), Is.True);
            Assert.That(found, Is.SameAs(resource));
            Assert.That(downloads[resource.Id], Is.SameAs(resource.ResourceData));
        }
        using (var result = DreamThread.Run(identity!, manager.WorldInstance, null,
                   new DreamValue(decoded[0]), new DreamValue(decoded[1]))) {
            var values = result.MustGetValueAsIDreamList().GetValues();
            // DM == compares resource paths; two null paths already compare equal.
            Assert.That(values[0].MustGetValueAsFloat(), Is.EqualTo(1));
            Assert.That(new DreamValue(decoded[0]).Equals(new DreamValue(decoded[1])), Is.False);
            Assert.That(values[1], Is.Not.EqualTo(values[2]));
            Assert.That(values[3].MustGetValueAsFloat(), Is.EqualTo(1));
            Assert.That(values[4].MustGetValueAsFloat(), Is.EqualTo(1));
        }
        var firstFile = resources.LoadResource("payload.bin");
        var secondFile = resources.LoadResource("payload.bin");
        Assert.That(secondFile, Is.SameAs(firstFile));
        var direct1 = resources.CreateResource([1, 2, 3]);
        var direct2 = resources.CreateResource([1, 2, 3]);
        Assert.That(direct1, Is.Not.SameAs(direct2));
        Assert.That(direct1.ResourceData, Is.Not.SameAs(direct2.ResourceData));
        Assert.That(refs.GetCountOf(RefType.DreamObjectList), Is.EqualTo(listCount));
        Assert.That(refs.GetCountOf(RefType.DreamObjectDatum), Is.EqualTo(datumCount));
        Assert.That(OpenDreamRuntime.Objects.Types.DreamObjectSavefile.Savefiles.Count, Is.EqualTo(openCount));
        Assert.That(manager.LastDMException, Is.Null);
        var arrays = generated.Distinct(ReferenceEqualityComparer.Instance).Count();
        TestContext.Out.WriteLine($"RESOURCE_BYTES_OBSERVATION: reads=3000 arrays={arrays} bytes={arrays * payload.Length} ids=3000");
        // Clearing/reloading one pathless handle must not alter another handle or old downloads.
        decoded[0].Clear();
        Assert.That(decoded[0].ResourceData!.SequenceEqual(payload), Is.True);
        var oldDownload = downloads[decoded[0].Id];
        decoded[0].ReloadFromDisk();
        Assert.That(decoded[0].ResourceData, Is.Null);
        Assert.That(decoded[1].ResourceData!.SequenceEqual(payload), Is.True);
        Assert.That(downloads[decoded[0].Id], Is.SameAs(oldDownload));
        Assert.That(oldDownload.SequenceEqual(payload), Is.True);
        // Reflection keeps this same fixture compilable against the historical baseline.
        var method = typeof(DreamResourceManager).GetMethod("CreateResourceFromSavefile", BindingFlags.Instance | BindingFlags.NonPublic);
        var collisionCorrect = true;
        if (method != null) {
            var buckets = Field<Dictionary<string, List<byte[]>>>(resources, "_savefileResourceData");
            byte[] wanted = [9, 8, 7], unequal = [9, 8, 6];
            buckets.Add(Convert.ToHexString(SHA256.HashData(wanted)), new List<byte[]> { unequal });
            var resource = (DreamResource)method.Invoke(resources, [wanted])!;
            collisionCorrect = resource.ResourceData!.SequenceEqual(wanted);
            Assert.That(resource.GetType(), Is.EqualTo(typeof(DreamResource)));
            Assert.That(resource.Id, Is.Not.EqualTo(decoded[0].Id));
            Assert.That(resource.ResourceData, Is.Not.SameAs(decoded[1].ResourceData));
            Assert.That(downloads[resource.Id], Is.SameAs(resource.ResourceData));
            Assert.That(resources.TryLoadResource(decoded[0].Id, out var earlier), Is.True);
            Assert.That(earlier, Is.SameAs(decoded[0]));
            var empty1 = (DreamResource)method.Invoke(resources, [Array.Empty<byte>()])!;
            var empty2 = (DreamResource)method.Invoke(resources, [new byte[0]])!;
            Assert.That(empty1.Id, Is.Not.EqualTo(empty2.Id));
            Assert.That(empty1.ResourceData, Is.SameAs(empty2.ResourceData));
        }
        TestContext.Out.WriteLine("RESOURCE_BYTES_COLLISION: " + (method == null ? "historical" : collisionCorrect ? "checked" : "mismatch"));
        TestContext.Out.WriteLine("RESOURCE_BYTES_COMPLETE: identities, lookups, downloads, contents and cleanup checked");
        Assert.Multiple(() => {
            Assert.That(arrays, Is.EqualTo(1), "RESOURCE_BYTES_SHARED");
            Assert.That(collisionCorrect, Is.True, "RESOURCE_BYTES_COLLISION_SAFE");
        });
    }
}
