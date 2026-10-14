#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;

namespace Content.IntegrationTests;

public sealed partial class GameTests {
    private static class AssocConstructionOwnershipFixture {
        private const int Iterations = 1000;
        private static readonly RefType[] Types = [RefType.DreamObjectList, RefType.DreamObjectDatum, RefType.DreamObjectAssocList];
        public static void Run(DreamManager manager, DreamObjectTree tree, DreamRefManager refs) {
            Assert.That(tree.TryGetGlobalProc("assoc_construct_reject_caught", out var caught), Is.True);
            Assert.That(tree.TryGetGlobalProc("assoc_construct_reject_unwind", out var uncaught), Is.True);
            Assert.That(tree.TryGetGlobalProc("assoc_construct_reuse", out var reuse), Is.True);
            Assert.That(tree.TryGetGlobalProc("assoc_construct_success", out var success), Is.True);
            var baseline = Snapshot(refs);
            var previousError = manager.LastDMException;
            try {
                Assert.Multiple(() => {
                    Assert.That(tree.TryGetGlobalProc("assoc_construct_release_timing", out var timing), Is.True);
                    ReleaseTiming(manager, refs, timing!);
                    foreach (var list in new[] { false, true }) {
                        foreach (var alias in new[] { false, true }) {
                            foreach (var route in new[] { "mixed", "null", "repeat", "numeric", "empty" })
                                Success(manager, tree, refs, success!, route, list, alias);
                            foreach (var route in new[] { "caught", "uncaught" })
                                foreach (var bad in new[] { 0, 99 })
                                    foreach (var position in new[] { "first", "middle", "last" })
                                        Reject(manager, tree, refs, route == "caught" ? caught! : uncaught!, reuse!,
                                            route, bad, position, list, alias);
                        }
                    }
                    foreach (var type in Types)
                        Assert.That(refs.GetCountOf(type), Is.EqualTo(baseline[type].Count), $"ASSOC_CONSTRUCT_FINAL_{type}");
                    Assert.That(manager.LastDMException, Is.Null, "ASSOC_CONSTRUCT_NO_UNEXPECTED_RUNTIME");
                });
            } finally {
                // Only deliberately injected runtimes are acknowledged by Reject.
                manager.LastDMException = previousError;
                CleanupNewObjects(refs, baseline);
            }
            TestContext.Out.WriteLine("ASSOC_CONSTRUCT_COMPLETE: 48 rejection cases x 1000 real compiled constructions, 20 success controls, caller owners and reference buckets");
        }

        private sealed record Held(DreamObject Key, DreamObject P, DreamObject Q, DreamObject R,
            DreamObject[] Owners, DreamObject[] Children);

        private static DreamObject Payload(DreamObjectTree tree, bool list, List<DreamObject> children) {
            if (!list) return tree.CreateObject(tree.Datum);
            var payload = tree.CreateList();
            var child = tree.CreateObject(tree.Datum);
            payload.AddValue(new DreamValue(child));
            child.DecRef(); // Exactly the contained list owns this child.
            children.Add(child);
            return payload; // Its initial creator owner belongs to the C# caller.
        }

        private static Held MakeHeld(DreamObjectTree tree, bool list, bool alias) {
            var children = new List<DreamObject>();
            if (alias) {
                var same = Payload(tree, list, children);
                return new(same, same, same, same, [same], children.ToArray());
            }
            var key = tree.CreateObject(tree.Datum);
            var p = Payload(tree, list, children);
            var q = Payload(tree, list, children);
            var r = Payload(tree, list, children);
            return new(key, p, q, r, [key, p, q, r], children.ToArray());
        }

        private static Dictionary<RefType, HashSet<DreamObject>> Snapshot(DreamRefManager refs) =>
            Types.ToDictionary(type => type, type => refs.EnumerateType(type).ToHashSet());

        private static void CleanupNewObjects(DreamRefManager refs, Dictionary<RefType, HashSet<DreamObject>> baseline) {
            // Observe/assert before cleanup. Retire only objects allocated in this case, even on the faulty control.
            // Lists first release their legitimate contained owners; snapshots avoid mutating enumerators.
            foreach (var type in Types)
                foreach (var value in refs.EnumerateType(type).Where(value => !baseline[type].Contains(value)).ToArray())
                    if (!value.Deleted) value.Delete();
        }

        private static bool CountsMatch(DreamRefManager refs, Dictionary<RefType, HashSet<DreamObject>> expected) =>
            Types.All(type => refs.GetCountOf(type) == expected[type].Count);

        private static void AssertBuckets(DreamRefManager refs, Dictionary<RefType, HashSet<DreamObject>> expected, string id) {
            foreach (var type in Types)
                Assert.That(refs.GetCountOf(type), Is.EqualTo(expected[type].Count), $"{id}_{type}");
        }

        private static void FinalRelease(Held held, DreamRefManager refs, string id) {
            var all = held.Owners.Concat(held.Children).ToArray();
            var ids = all.Select(value => value.RefId).ToArray();
            foreach (var value in held.Owners) value.DecRef();
            for (var i = 0; i < all.Length; i++) {
                Assert.That(all[i].Deleted, Is.True, $"{id}_FINAL_DELETE_{i}");
                using var located = refs.LocateRef(ids[i]);
                Assert.That(located.IsNull, Is.True, $"{id}_FINAL_LOCATE_{i}");
            }
        }

        private static void Reject(DreamManager manager, DreamObjectTree tree, DreamRefManager refs, DreamProc proc,
                DreamProc reuse, string route, int bad, string position, bool list, bool alias) {
            var id = $"ASSOC_CONSTRUCT_REJECT_{route}_{bad}_{position}_{(list ? "list" : "datum")}_{(alias ? "alias" : "distinct")}";
            var baseline = Snapshot(refs);
            var held = MakeHeld(tree, list, alias);
            var expectedBuckets = Snapshot(refs); // Includes exactly the explicit caller-owned payloads/children.
            var outcomesValid = true;
            var reuseValid = true;
            try {
                for (var iteration = 0; iteration < Iterations; iteration++) {
                    using (var result = DreamThread.Run(proc, manager.WorldInstance, null,
                            new DreamValue(bad), new DreamValue(position), new DreamValue(held.Key),
                            new DreamValue(held.P), new DreamValue(held.Q), new DreamValue(held.R))) {
                        outcomesValid &= result.TryGetValueAsInteger(out var number) && number == 17 && manager.LastDMException == null;
                    }
                    // Both paths catch deliberately rejected keys inside DM; global diagnostics must stay clear.
                    using (var result = DreamThread.Run(reuse, manager.WorldInstance, null, new DreamValue(held.P)))
                        reuseValid &= result.MustGetValueAsDreamObject() == held.P && manager.LastDMException == null;
                }
                var counts = held.Owners.Select(value => value.RefCount).ToArray();
                var expectedOwners = Enumerable.Repeat(1, held.Owners.Length).ToArray();
                TestContext.Out.WriteLine($"{id}: owners=[{string.Join(",", counts)}]; lists={refs.GetCountOf(RefType.DreamObjectList)} expected={expectedBuckets[RefType.DreamObjectList].Count}; datums={refs.GetCountOf(RefType.DreamObjectDatum)} expected={expectedBuckets[RefType.DreamObjectDatum].Count}");
                // One aggregated owner assertion per rejecting case, not 1000 cascading failures.
                Assert.That(counts, Is.EqualTo(expectedOwners), $"{id}_OWNERS_1000");
                Assert.That(outcomesValid, Is.True, $"{id}_EXPECTED_REJECTIONS_1000");
                Assert.That(reuseValid, Is.True, $"{id}_STATE_REUSE_1000");
                AssertBuckets(refs, expectedBuckets, $"{id}_LIVE_BUCKET");
                foreach (var child in held.Children)
                    Assert.That(child.RefCount == 1 && !child.Deleted, Is.True, $"{id}_CONTAINED_OWNER");
                if (counts.SequenceEqual(expectedOwners) && CountsMatch(refs, expectedBuckets))
                    FinalRelease(held, refs, id);
            } finally {
                manager.LastDMException = null;
                CleanupNewObjects(refs, baseline);
            }
            AssertBuckets(refs, baseline, $"{id}_CLEAN_BUCKET");
        }

        private static void Success(DreamManager manager, DreamObjectTree tree, DreamRefManager refs, DreamProc proc,
                string route, bool list, bool alias) {
            var id = $"ASSOC_CONSTRUCT_SUCCESS_{route}_{(list ? "list" : "datum")}_{(alias ? "alias" : "distinct")}";
            var baseline = Snapshot(refs);
            var held = MakeHeld(tree, list, alias);
            var expectedBuckets = Snapshot(refs);
            DreamObject? returned = null;
            uint returnedId = 0;
            try {
                using (var result = DreamThread.Run(proc, manager.WorldInstance, null, new DreamValue(route),
                        new DreamValue(held.Key), new DreamValue(held.P), new DreamValue(held.Q), new DreamValue(held.R))) {
                    var target = result.MustGetValueAsDreamList();
                    returned = target;
                    returnedId = target.RefId;
                    var length = route == "empty" ? 0 : route == "repeat" ? 1 : alias && (route == "mixed" || route == "null") ? 2 : 3;
                    Assert.That(target.GetLength(), Is.EqualTo(length), $"{id}_LENGTH");
                    if (route == "mixed" || route == "null") {
                        using (var first = target.GetValue(new DreamValue(1)))
                            Assert.That(first.MustGetValueAsDreamObject(), Is.SameAs(held.P), $"{id}_POSITIONAL");
                        using (var associated = target.GetValue(new DreamValue(held.Key)))
                            Assert.That(associated.MustGetValueAsDreamObject(), Is.SameAs(held.Q), $"{id}_ASSOCIATION");
                        using (var tail = target.GetValue(route == "mixed" ? new DreamValue("tail") : new DreamValue(length)))
                            Assert.That(tail.MustGetValueAsDreamObject(), Is.SameAs(held.R), $"{id}_TAIL");
                    } else if (route == "repeat") {
                        using var final = target.GetValue(new DreamValue(held.Key));
                        Assert.That(final.MustGetValueAsDreamObject(), Is.SameAs(held.R), $"{id}_REPLACED");
                    } else if (route == "numeric") {
                        var values = new[] { held.P, held.Q, held.R };
                        for (var i = 0; i < values.Length; i++) {
                            using var value = target.GetValue(new DreamValue(i + 1));
                            Assert.That(value.MustGetValueAsDreamObject(), Is.SameAs(values[i]), $"{id}_NUMERIC_{i}");
                        }
                    }
                    int[] owners = alias ? [route switch { "mixed" or "null" => 4, "repeat" => 3, "numeric" => 4, _ => 1 }] :
                        route switch { "mixed" or "null" => [2, 2, 2, 2], "repeat" => [2, 1, 1, 2], "numeric" => [1, 2, 2, 2], _ => [1, 1, 1, 1] };
                    Assert.That(held.Owners.Select(value => value.RefCount).ToArray(), Is.EqualTo(owners), $"{id}_LIVE_OWNERS");
                    Assert.That(target.RefCount, Is.EqualTo(1), $"{id}_RETURN_OWNER");
                    Assert.That(refs.GetCountOf(RefType.DreamObjectList), Is.EqualTo(expectedBuckets[RefType.DreamObjectList].Count + 1), $"{id}_LIVE_LIST_BUCKET");
                    Assert.That(refs.GetCountOf(RefType.DreamObjectDatum), Is.EqualTo(expectedBuckets[RefType.DreamObjectDatum].Count), $"{id}_LIVE_DATUM_BUCKET");
                }
                Assert.That(returned!.Deleted, Is.True, $"{id}_RETURN_DELETE");
                using (var retired = refs.LocateRef(returnedId))
                    Assert.That(retired.IsNull, Is.True, $"{id}_RETURN_LOCATE");
                Assert.That(held.Owners.Select(value => value.RefCount).ToArray(), Is.EqualTo(Enumerable.Repeat(1, held.Owners.Length).ToArray()), $"{id}_RELEASED_OWNERS");
                AssertBuckets(refs, expectedBuckets, $"{id}_RELEASED_BUCKET");
                Assert.That(manager.LastDMException, Is.Null, $"{id}_NO_RUNTIME");
                if (held.Owners.All(value => value.RefCount == 1) && CountsMatch(refs, expectedBuckets))
                    FinalRelease(held, refs, id);
            } finally {
                CleanupNewObjects(refs, baseline);
            }
            AssertBuckets(refs, baseline, $"{id}_CLEAN_BUCKET");
        }

        // Call inside the existing Assert.Multiple before the unchanged 48 rejection cases:
        // Assert.That(tree.TryGetGlobalProc("assoc_construct_release_timing", out var timing), Is.True);
        // ReleaseTiming(manager, refs, timing!);
        private static void ReleaseTiming(DreamManager manager, DreamRefManager refs, DreamProc proc) {
            foreach (var list in new[] { false, true }) {
                var baseline = Snapshot(refs);
                var id = list ? "ASSOC_TIMING_LIST" : "ASSOC_TIMING_DATUM";
                var correct = true;
                var last = "";
                try {
                    for (var iteration = 0; iteration < 1000; iteration++) {
                        using var result = DreamThread.Run(proc, manager.WorldInstance, null, new DreamValue(list ? 1 : 0));
                        last = result.MustGetValueAsString();
                        correct &= last == "BA" && manager.LastDMException == null;
                        correct &= CountsMatch(refs, baseline);
                    }
                    TestContext.Out.WriteLine($"{id}: trace={last}; expected=BA; iterations=1000");
                    Assert.That(correct, Is.True, $"{id}_PER_PAIR_ORDER_AND_BUCKETS_1000");
                    AssertBuckets(refs, baseline, id);
                } finally {
                    CleanupNewObjects(refs, baseline);
                }
            }
        }
    }
}
