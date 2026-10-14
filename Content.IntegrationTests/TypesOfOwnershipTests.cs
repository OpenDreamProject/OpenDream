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

[TestFixture]
[NonParallelizable]
[Explicit("Requires a separate test host because BYOND API initialization is process-global")]
public sealed class TypesOfOwnershipTests : ContentIntegrationTest {
    private const int Iterations = 1000;
    private static readonly RefType[] Types = [RefType.DreamObjectList, RefType.DreamObjectDatum, RefType.DreamObjectAssocList];

    [Test]
    public async Task RejectedTypesOfReleasesCreator() {
        var originalDirectory = Directory.GetCurrentDirectory();
        var temporary = Path.Combine(Path.GetTempPath(), "opendream-typesof-ownership-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try {
            var fixture = Path.Combine(temporary, "fixture.dm");
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DMProject", "Fixtures", "typesof-ownership.dm"), fixture);
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
            Assert.That(server.IsAlive, Is.True);
            await server.WaitAssertion(() => {
                var manager = server.ResolveDependency<DreamManager>();
                var tree = server.ResolveDependency<DreamObjectTree>();
                var refs = server.ResolveDependency<DreamRefManager>();
                Assert.That(manager.LastDMException, Is.Null, "TYPESOF_BOOT_NO_RUNTIME");
                Assert.That(tree.TryGetGlobalProc("typesof_reject_caught", out var caught), Is.True);
                Assert.That(tree.TryGetGlobalProc("typesof_reject_unwind", out var unwind), Is.True);
                Assert.That(tree.TryGetGlobalProc("typesof_success_return", out var returned), Is.True);
                Assert.That(tree.TryGetGlobalProc("typesof_success_drop", out var dropped), Is.True);
                Assert.That(tree.TryGetGlobalProc("typesof_empty_return", out var empty), Is.True);
                var baseline = Snapshot(refs);
                try {
                    Assert.Multiple(() => {
                        Success(manager, tree, refs, returned!, dropped!, empty!);
                        foreach (var route in new[] { "caught", "unwind" })
                            foreach (var kind in new[] { "type", "proc", "verb" })
                                foreach (var position in new[] { "first", "middle", "last" })
                                    Reject(manager, tree, refs, route == "caught" ? caught! : unwind!, dropped!, route, kind, position);
                        AssertBuckets(refs, baseline, "TYPESOF_FINAL");
                        Assert.That(manager.LastDMException, Is.Null, "TYPESOF_NO_UNEXPECTED_RUNTIME");
                    });
                } finally {
                    CleanupNewObjects(refs, baseline);
                }
                TestContext.Out.WriteLine("TYPESOF_COMPLETE: 18 rejection cases x 1000 compiled calls; exact new list identities, owners, contents, cleanup and success controls");
            });
        } finally {
            Directory.SetCurrentDirectory(originalDirectory);
            Directory.Delete(temporary, recursive: true);
        }
    }

    private sealed record Held(DreamObject Payload, DreamList Container, DreamObject Child);

    private static Held MakeHeld(DreamObjectTree tree) {
        var payload = tree.CreateObject(tree.GetTreeEntry("/datum/typesof_owner_probe"));
        var container = tree.CreateList();
        var child = tree.CreateObject(tree.Datum);
        container.AddValue(new DreamValue(child));
        child.DecRef(); // Exactly the container owns the child; C# holds only a borrowed identity.
        return new(payload, container, child); // Two explicit creator owners remain with the caller.
    }

    private static Dictionary<RefType, HashSet<DreamObject>> Snapshot(DreamRefManager refs) =>
        Types.ToDictionary(type => type, type => refs.EnumerateType(type).ToHashSet());

    private static void CleanupNewObjects(DreamRefManager refs, Dictionary<RefType, HashSet<DreamObject>> baseline) {
        foreach (var type in Types)
            foreach (var value in refs.EnumerateType(type).Where(value => !baseline[type].Contains(value)).ToArray())
                if (!value.Deleted) value.Delete();
    }

    private static void AssertBuckets(DreamRefManager refs, Dictionary<RefType, HashSet<DreamObject>> expected, string id) {
        foreach (var type in Types) {
            Assert.That(refs.GetCountOf(type), Is.EqualTo(expected[type].Count), $"{id}_BUCKET_{type}");
            Assert.That(refs.EnumerateType(type).ToHashSet().SetEquals(expected[type]), Is.True, $"{id}_IDENTITIES_{type}");
        }
    }

    private static bool HeldCorrect(Held held) => !held.Payload.Deleted && !held.Container.Deleted && !held.Child.Deleted
        && held.Payload.RefCount == 1 && held.Container.RefCount == 1 && held.Child.RefCount == 1;

    private static void AssertHeld(Held held, string id) {
        Assert.That(new[] { held.Payload.RefCount, held.Container.RefCount, held.Child.RefCount }, Is.EqualTo(new[] { 1, 1, 1 }), $"{id}_OWNERS");
        Assert.That(held.Payload.Deleted || held.Container.Deleted || held.Child.Deleted, Is.False, $"{id}_ALIVE");
        Assert.That(held.Container.GetLength(), Is.EqualTo(1), $"{id}_CONTAINED_LENGTH");
        using var contained = held.Container.GetValue(new DreamValue(1));
        Assert.That(contained.MustGetValueAsDreamObject(), Is.SameAs(held.Child), $"{id}_CONTAINED_IDENTITY");
    }

    private static void FinalRelease(Held held, DreamRefManager refs, string id) {
        var all = new[] { held.Payload, held.Container, held.Child };
        var ids = all.Select(value => value.RefId).ToArray();
        held.Payload.DecRef();
        held.Container.DecRef();
        for (var i = 0; i < all.Length; i++) {
            Assert.That(all[i].Deleted, Is.True, $"{id}_FINAL_DELETE_{i}");
            using var located = refs.LocateRef(ids[i]);
            Assert.That(located.IsNull, Is.True, $"{id}_FINAL_LOCATE_{i}");
        }
    }

    private static bool ContentsCorrect(DreamList list, DreamObjectTree tree, int groups) {
        var root = tree.GetTreeEntry("/datum/typesof_owner_probe");
        var subtype = tree.GetTreeEntry("/datum/typesof_owner_probe/subtype");
        var values = list.EnumerateValues().ToArray(); // Borrowed type values need no owner release.
        return values.Length == groups * 2 && list.EnumerateAssocValues().All(pair => pair.Value.IsNull)
            && values.Select((value, index) => value.TryGetValueAsType(out var type)
                && type == (index % 2 == 0 ? root : subtype)).All(correct => correct);
    }

    private static void Reject(DreamManager manager, DreamObjectTree tree, DreamRefManager refs, DreamProc proc,
            DreamProc dropped, string route, string kind, string position) {
        var id = $"TYPESOF_REJECT_{route}_{kind}_{position}";
        var baseline = Snapshot(refs);
        var held = MakeHeld(tree);
        var expected = Snapshot(refs);
        var suffix = kind == "type" ? "" : "/" + kind;
        var missing = "/__typesof_ownership_missing" + suffix;
        Assert.That(tree.TryGetTreeEntry("/__typesof_ownership_missing", out _), Is.False, $"{id}_MISSING_PRECONDITION");
        var positionNumber = position == "first" ? 1 : position == "middle" ? 2 : 3;
        var groups = positionNumber == 1 ? 0 : positionNumber == 2 ? 1 : 3;
        var outcomes = true;
        var reuse = true;
        try {
            for (var iteration = 0; iteration < Iterations; iteration++) {
                using (var result = DreamThread.Run(proc, manager.WorldInstance, null,
                        new DreamValue(held.Payload), new DreamValue(held.Container), new DreamValue(missing), new DreamValue(positionNumber)))
                    outcomes &= result.TryGetValueAsInteger(out var number) && number == 17 && manager.LastDMException == null && HeldCorrect(held);
                using (var result = DreamThread.Run(dropped, manager.WorldInstance, null,
                        new DreamValue(held.Payload), new DreamValue(held.Container)))
                    reuse &= result.TryGetValueAsInteger(out var number) && number == 17 && manager.LastDMException == null && HeldCorrect(held);
            }
            var liveLists = refs.EnumerateType(RefType.DreamObjectList).ToHashSet();
            var fresh = liveLists.Except(expected[RefType.DreamObjectList]).ToArray();
            var refOne = fresh.Count(value => value.RefCount == 1);
            var content = fresh.Count(value => value is DreamList list && ContentsCorrect(list, tree, groups));
            TestContext.Out.WriteLine($"{id}: owners=[{held.Payload.RefCount},{held.Container.RefCount},{held.Child.RefCount}]; new={fresh.Length}; ref1={refOne}; content={content}; lists={liveLists.Count} expected={expected[RefType.DreamObjectList].Count}; datums={refs.GetCountOf(RefType.DreamObjectDatum)} expected={expected[RefType.DreamObjectDatum].Count}");
            Assert.That(outcomes, Is.True, $"{id}_EXPECTED_REJECTIONS_1000");
            Assert.That(reuse, Is.True, $"{id}_STATE_REUSE_1000");
            AssertHeld(held, id);
            Assert.That(refOne, Is.EqualTo(fresh.Length), $"{id}_NEW_REFS_EXACT_ONE");
            Assert.That(content, Is.EqualTo(fresh.Length), $"{id}_NEW_TYPE_ONLY_CONTENTS");
            Assert.That(expected[RefType.DreamObjectList].IsSubsetOf(liveLists), Is.True, $"{id}_EXISTING_LIST_IDENTITIES");
            // Exactly one intended failing assertion per baseline case. Other invariants must pass.
            Assert.That(fresh.Length, Is.EqualTo(0), $"{id}_UNRETURNED_LISTS_1000");
            foreach (var type in Types.Where(type => type != RefType.DreamObjectList)) {
                Assert.That(refs.GetCountOf(type), Is.EqualTo(expected[type].Count), $"{id}_BUCKET_{type}");
                Assert.That(refs.EnumerateType(type).ToHashSet().SetEquals(expected[type]), Is.True, $"{id}_IDENTITIES_{type}");
            }
        } finally {
            // Preserve caller-owned identities. First observe every baseline leak, then retire exactly new members.
            CleanupNewObjects(refs, expected);
            if (HeldCorrect(held)) FinalRelease(held, refs, id);
            CleanupNewObjects(refs, baseline);
        }
        AssertBuckets(refs, baseline, id + "_CLEAN");
    }

    private static void Success(DreamManager manager, DreamObjectTree tree, DreamRefManager refs,
            DreamProc returned, DreamProc dropped, DreamProc empty) {
        var baseline = Snapshot(refs);
        var held = MakeHeld(tree);
        var expected = Snapshot(refs);
        try {
            foreach (var isEmpty in new[] { false, true }) {
                DreamList? target = null;
                uint refId = 0;
                using (var result = isEmpty ? DreamThread.Run(empty, manager.WorldInstance, null)
                        : DreamThread.Run(returned, manager.WorldInstance, null, new DreamValue(held.Payload), new DreamValue(held.Container))) {
                    Assert.That(result.TryGetValueAsDreamList(out target), Is.True, "TYPESOF_SUCCESS_RETURN_KIND");
                    if (target == null) throw new InvalidOperationException("Expected a returned ordinary list");
                    refId = target.RefId;
                    Assert.That(target.RefCount, Is.EqualTo(1), "TYPESOF_SUCCESS_RETURN_OWNER");
                    Assert.That(ContentsCorrect(target, tree, isEmpty ? 0 : 3), Is.True, "TYPESOF_SUCCESS_CONTENTS");
                    AssertHeld(held, "TYPESOF_SUCCESS");
                    var live = Snapshot(refs);
                    Assert.That(live[RefType.DreamObjectList].SetEquals(expected[RefType.DreamObjectList].Append(target)), Is.True, "TYPESOF_SUCCESS_ONE_LIST_IDENTITY");
                    foreach (var type in Types.Where(type => type != RefType.DreamObjectList))
                        Assert.That(live[type].SetEquals(expected[type]), Is.True, $"TYPESOF_SUCCESS_{type}");
                }
                Assert.That(target!.Deleted, Is.True, "TYPESOF_SUCCESS_RETURN_DELETE");
                using (var located = refs.LocateRef(refId))
                    Assert.That(located.IsNull, Is.True, "TYPESOF_SUCCESS_RETURN_LOCATE");
                AssertBuckets(refs, expected, "TYPESOF_SUCCESS_RELEASE");
            }
            using (var result = DreamThread.Run(dropped, manager.WorldInstance, null, new DreamValue(held.Payload), new DreamValue(held.Container)))
                Assert.That(result.MustGetValueAsInteger(), Is.EqualTo(17), "TYPESOF_SUCCESS_DROP_RESULT");
            AssertHeld(held, "TYPESOF_SUCCESS_DROP");
            AssertBuckets(refs, expected, "TYPESOF_SUCCESS_DROP");
            Assert.That(manager.LastDMException, Is.Null, "TYPESOF_SUCCESS_NO_RUNTIME");
            TestContext.Out.WriteLine("TYPESOF_SUCCESS: returned=6; empty=0; drop=17; released=1; owners=[1,1,1]; contents=type-only");
        } finally {
            CleanupNewObjects(refs, expected);
            if (HeldCorrect(held)) FinalRelease(held, refs, "TYPESOF_SUCCESS");
            CleanupNewObjects(refs, baseline);
        }
        AssertBuckets(refs, baseline, "TYPESOF_SUCCESS_CLEAN");
    }
}
