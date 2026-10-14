#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenDreamShared;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs;

namespace Content.IntegrationTests;

[TestFixture, NonParallelizable]
[Explicit("Separate process required because BYOND API initialization is process-global")]
public sealed class NativeMatrixOwnershipTests : ContentIntegrationTest {
    private const int Iterations = 100;
    private static string Mode => Environment.GetEnvironmentVariable("MATRIX_OWNERSHIP_MODE") ?? "positive";
    private static bool CreatorMissing => Mode is "baseline" or "omit-creator";
    private static bool ArgumentsMissing => Mode is "baseline" or "omit-arguments";

    private async Task Host(string name, Action<DreamManager, DreamObjectTree, DreamRefManager> assertions) {
        var oldDirectory = Directory.GetCurrentDirectory();
        var temporary = Path.Combine(Path.GetTempPath(), "opendream-matrix-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try {
            var dm = Path.Combine(temporary, "fixture.dm");
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DMProject", "Fixtures", name + ".dm"), dm);
            var compiler = new DMCompiler.DMCompiler();
            var compiled = compiler.Compile(new() { Files = [dm], StoreMessages = true });
            Assert.That(compiled && File.Exists(Path.ChangeExtension(dm, "json")), Is.True,
                "MATRIX_COMPILE: " + string.Join("\n", compiler.CompilerMessages));
            var options = new ServerContentIntegrationOption();
            options.CVarOverrides[OpenDreamCVars.JsonPath.Name] = Path.ChangeExtension(dm, "json");
            options.CVarOverrides[OpenDreamCVars.TopicPort.Name] = (ushort)0;
            var server = StartServer(options);
            await server.WaitIdleAsync();
            await server.WaitRunTicks(1);
            Assert.That(server.IsAlive, Is.True);
            await server.WaitAssertion(() => {
                var manager = server.ResolveDependency<DreamManager>();
                Assert.That(manager.LastDMException, Is.Null, "MATRIX_BOOT_RUNTIME");
                assertions(manager, server.ResolveDependency<DreamObjectTree>(), server.ResolveDependency<DreamRefManager>());
                Assert.That(manager.LastDMException, Is.Null, "MATRIX_UNEXPECTED_RUNTIME");
            });
        } finally {
            Directory.SetCurrentDirectory(oldDirectory);
            Directory.Delete(temporary, recursive: true);
        }
    }

    [Test]
    public Task FastRejectionsAndTransfers() => Host("native-matrix-fast", (manager, tree, refs) => {
        Assert.That(new[] { "positive", "baseline", "omit-creator", "omit-arguments" }, Does.Contain(Mode));
        Assert.That(tree.Matrix.ObjectDefinition.NoConstructors, Is.True, "MATRIX_FAST_PRECONDITION");
        Assert.That(tree.Datum.ObjectDefinition.NoConstructors, Is.True, "GENERIC_FAST_PRECONDITION");
        Assert.That(tree.TryGetGlobalProc("matrix_reject", out var reject), Is.True);
        var original = refs.EnumerateType(RefType.DreamObjectDatum).ToHashSet();
        try {
            foreach (var kind in new[] { "null", "string", "datum", "list" }) {
                for (var position = 0; position <= 6; position++) {
                    var before = refs.EnumerateType(RefType.DreamObjectDatum).OfType<DreamObjectMatrix>().ToHashSet();
                    DreamObject? held = kind switch { "datum" => tree.CreateObject(tree.Datum), "list" => tree.CreateList(), _ => null };
                    var child = kind == "list" ? tree.CreateObject(tree.Datum) : null;
                    if (held is DreamList list && child is not null) { list.AddValue(new DreamValue(child)); child.DecRef(); }
                    var bad = held is null ? (kind == "string" ? new DreamValue("bad") : DreamValue.Null) : new DreamValue(held);
                    for (var i = 0; i < Iterations; i++) {
                        using var result = DreamThread.Run(reject!, manager.WorldInstance, null, bad, new DreamValue(position));
                        Assert.That(result.MustGetValueAsFloat(), Is.EqualTo(1), "MATRIX_CAUGHT_REJECTION");
                        Assert.That(manager.LastDMException, Is.Null, "MATRIX_CAUGHT_NOT_GLOBAL");
                    }
                    var extra = refs.EnumerateType(RefType.DreamObjectDatum).OfType<DreamObjectMatrix>().Where(value => !before.Contains(value)).ToArray();
                    var expectedMatrices = CreatorMissing ? Iterations : 0;
                    var copies = position == 0 ? 6 : 1;
                    var expectedOwner = held is null ? 0 : 1 + (ArgumentsMissing ? copies * Iterations : 0);
                    TestContext.Out.WriteLine($"MATRIX_CASE {kind} {position} matrices={extra.Length} expected={expectedMatrices} owner={held?.RefCount ?? 0} expectedOwner={expectedOwner}");
                    Assert.That(extra.Length, Is.EqualTo(expectedMatrices), "MATRIX_CREATOR_BUCKET");
                    Assert.That(extra.All(value => value.RefCount == 1 && !value.Deleted), Is.True, "MATRIX_BASELINE_CREATOR_SIGNATURE");
                    if (held is not null) {
                        Assert.That(held.RefCount, Is.EqualTo(expectedOwner), "MATRIX_ARGUMENT_OWNER");
                        held.DecRef();
                        Assert.That(held.Deleted, Is.EqualTo(!ArgumentsMissing), "MATRIX_ARGUMENT_FINAL_DELETE");
                        if (child is not null) Assert.That(child.Deleted, Is.EqualTo(!ArgumentsMissing), "MATRIX_CHILD_FINAL_DELETE");
                        if (!held.Deleted) held.Delete(); // Only after the real retention assertions.
                    }
                    foreach (var value in extra) value.Delete(); // Retire only observed control leaks.
                }
            }
            foreach (var list in new[] { false, true }) {
                var payload = list ? tree.CreateList() : tree.CreateObject(tree.Datum);
                var target = new RejectingDatum(tree.Datum.ObjectDefinition);
                try {
                    for (var i = 0; i < Iterations; i++) {
                        Assert.That(() => target.InitSpawn(new DreamProcArguments(new DreamValue(payload), new DreamValue(payload))),
                            Throws.TypeOf<ArgumentException>().With.Message.EqualTo("MATRIX_GENERIC_REJECTION"));
                    }
                    var expected = 1 + (ArgumentsMissing ? 2 * Iterations : 0);
                    TestContext.Out.WriteLine($"MATRIX_GENERIC {(list ? "list" : "datum")} owner={payload.RefCount} expected={expected}");
                    Assert.That(payload.RefCount, Is.EqualTo(expected), "GENERIC_ARGUMENT_OWNER");
                    Assert.That(target.RefCount, Is.EqualTo(1), "GENERIC_CALLER_OWNS_TARGET");
                    payload.DecRef();
                    Assert.That(payload.Deleted, Is.EqualTo(!ArgumentsMissing), "GENERIC_FINAL_DELETE");
                } finally { if (!payload.Deleted) payload.Delete(); target.DecRef(); }
            }
            Assert.That(tree.TryGetGlobalProc("matrix_success", out var success), Is.True);
            var count = refs.EnumerateType(RefType.DreamObjectDatum).OfType<DreamObjectMatrix>().Count();
            using (var result = DreamThread.Run(success!, manager.WorldInstance, null))
                Assert.That(result.MustGetValueAsFloat(), Is.EqualTo(1), "MATRIX_SUCCESS_FIELDS_AND_TRANSFER");
            Assert.That(refs.EnumerateType(RefType.DreamObjectDatum).OfType<DreamObjectMatrix>().Count(), Is.EqualTo(count), "MATRIX_SUCCESS_FINAL_BUCKET");
            TestContext.Out.WriteLine($"MATRIX_FAST_COMPLETE mode={Mode} cases=28 iterations=100 generic=2 success=1");
        } finally {
            // Failure isolation only; no successful observation depends on cleanup.
            foreach (var value in refs.EnumerateType(RefType.DreamObjectDatum).Where(value => !original.Contains(value)).ToArray()) value.Delete();
        }
    });

    private sealed class RejectingDatum(DreamObjectDefinition definition) : DreamObject(definition) {
        public override void Initialize(DreamProcArguments args) {
            if (args.Count != 2) throw new InvalidOperationException("wrong generic argument count");
            throw new ArgumentException("MATRIX_GENERIC_REJECTION");
        }
    }

    [Test]
    public Task ConstructorModePreservesRuntimeDelivery() => Host("native-matrix-constructors", (manager, tree, refs) => {
        Assert.That(tree.Matrix.ObjectDefinition.NoConstructors, Is.False, "MATRIX_CONSTRUCTOR_PRECONDITION");
        Assert.That(tree.TryGetGlobalProc("matrix_constructor_valid", out var valid), Is.True);
        Assert.That(tree.TryGetGlobalProc("matrix_constructor_invalid", out var invalid), Is.True);
        Assert.That(tree.TryGetGlobalProc("matrix_constructor_counts", out var counts), Is.True);
        var observed = new List<Exception>();
        manager.OnException += Observe;
        void Observe(object? sender, Exception failure) => observed.Add(failure);
        try {
            var before = refs.EnumerateType(RefType.DreamObjectDatum).OfType<DreamObjectMatrix>().Count();
            using (var result = DreamThread.Run(valid!, manager.WorldInstance, null)) {
                var value = result.MustGetValueAsDreamObject() as DreamObjectMatrix;
                Assert.That(value, Is.Not.Null);
                Assert.That(value!.Deleted, Is.False);
                Assert.That(value.A, Is.EqualTo(1));
                Assert.That(value.F, Is.EqualTo(6));
            }
            Assert.That(observed, Is.Empty);
            using (var result = DreamThread.Run(invalid!, manager.WorldInstance, null)) {
                var value = result.MustGetValueAsDreamObject() as DreamObjectMatrix;
                Assert.That(value, Is.Not.Null, "MATRIX_EXISTING_PARTIAL_RETURN");
                Assert.That(value!.Deleted, Is.False);
                Assert.That(value.RefCount, Is.EqualTo(1));
            }
            Assert.That(observed.Count, Is.EqualTo(1), "MATRIX_EXPECTED_RUNTIME_COUNT");
            Assert.That(observed[0], Is.TypeOf<ArgumentException>());
            Assert.That(observed[0].Message, Is.EqualTo("Invalid arguments used to create matrix null 2 3 4 5 6"));
            Assert.That(manager.LastDMException, Is.SameAs(observed[0]));
            using (var result = DreamThread.Run(counts!, manager.WorldInstance, null))
                Assert.That(result.MustGetValueAsFloat(), Is.EqualTo(11), "MATRIX_NEW_ONCE_ERROR_ONCE");
            manager.LastDMException = null; // Only the exact observed deliberate runtime is acknowledged.
            Assert.That(refs.EnumerateType(RefType.DreamObjectDatum).OfType<DreamObjectMatrix>().Count(), Is.EqualTo(before));
            TestContext.Out.WriteLine("MATRIX_CONSTRUCTORS_COMPLETE new=1 error=1 partial=1");
        } finally { manager.OnException -= Observe; }
    });
}
