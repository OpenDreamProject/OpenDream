#nullable enable annotations
using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenDreamShared;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs;

namespace Content.IntegrationTests;

public sealed partial class GameTests {
    private static void AssocListRemovalReleasesOwners(DreamManager manager, DreamObjectTree tree, DreamRefManager refs) {
        var datums = refs.GetCountOf(RefType.DreamObjectDatum);
        var lists = refs.GetCountOf(RefType.DreamObjectList);
        var alists = refs.GetCountOf(RefType.DreamObjectAssocList);
        Assert.That(tree.TryGetGlobalProc("assoc_list_remove_owned_route", out var compiled), Is.True);
        Assert.Multiple(() => {
            foreach (var list in new[] { false, true })
                foreach (var keyKind in new[] { "string", "datum", "same" })
                    AssocRemovalDirect(tree, refs, list, keyKind);
            foreach (var keyIsSelf in new[] { false, true })
                AssocRemovalSelf(tree, refs, keyIsSelf);
            AssocRemovalNoOpAndOrder(tree, refs);
            foreach (var list in new[] { false, true })
                foreach (var route in new[] { "remove", "remove-all", "remove-op", "subtract", "mask" })
                    AssocRemovalCompiled(manager, tree, refs, compiled!, list, route);
        });
        Assert.That(refs.GetCountOf(RefType.DreamObjectDatum), Is.EqualTo(datums));
        Assert.That(refs.GetCountOf(RefType.DreamObjectList), Is.EqualTo(lists));
        Assert.That(refs.GetCountOf(RefType.DreamObjectAssocList), Is.EqualTo(alists));
        Assert.That(manager.LastDMException, Is.Null);
        TestContext.Out.WriteLine("ASSOC_REMOVE_COMPLETE: public API aliases, no-op/order, five compiled routes and final reference buckets");
    }

    private static DreamObject AssocRemovalPayload(DreamObjectTree tree, bool list, out DreamObject? child) {
        child = null;
        if (!list) return tree.CreateObject(tree.Datum);
        var payload = tree.CreateList();
        child = tree.CreateObject(tree.Datum);
        payload.AddValue(new DreamValue(child));
        child.DecRef(); // The contained list is the child's sole owner.
        return payload;
    }

    private static void AssocRemovalFinalDelete(DreamObject payload, DreamObject? child, DreamRefManager refs) {
        var id = payload.RefId;
        payload.DecRef();
        Assert.That(payload.Deleted, Is.True, "ASSOC_REMOVE_FINAL_DELETE");
        using var located = refs.LocateRef(id);
        Assert.That(located.IsNull, Is.True, "ASSOC_REMOVE_FINAL_BUCKET");
        if (child == null) return;
        Assert.That(child.Deleted, Is.True, "ASSOC_REMOVE_CONTAINED_DELETE");
        using var childLocated = refs.LocateRef(child.RefId);
        Assert.That(childLocated.IsNull, Is.True, "ASSOC_REMOVE_CONTAINED_BUCKET");
    }

    private static void AssocRemovalDirect(DreamObjectTree tree, DreamRefManager refs, bool list, string keyKind) {
        var table = tree.CreateAssocList();
        var payload = AssocRemovalPayload(tree, list, out var child);
        var separateKey = keyKind == "datum" ? tree.CreateObject(tree.Datum) : null;
        var key = keyKind == "same" ? new DreamValue(payload) :
            separateKey != null ? new DreamValue(separateKey) : new DreamValue("remove");
        try {
            for (var iteration = 0; iteration < 1000; iteration++) {
                table.SetValue(key, new DreamValue(payload));
                table.RemoveValue(key);
                Assert.That(table.GetLength(), Is.Zero);
                Assert.That(table.ContainsKey(key), Is.False);
                if (separateKey != null) Assert.That(separateKey.RefCount, Is.EqualTo(1));
            }
            Assert.That(payload.RefCount, Is.EqualTo(1), $"ASSOC_REMOVE_DIRECT_{list}_{keyKind}_OWNER_1000");
            if (payload.RefCount == 1) AssocRemovalFinalDelete(payload, child, refs);
        } finally {
            table.DecRef();
            separateKey?.DecRef();
            // Preserve negative-control failures, then isolate leaked objects from later checks.
            if (!payload.Deleted) payload.Delete();
        }
    }

    private static void AssocRemovalSelf(DreamObjectTree tree, DreamRefManager refs, bool keyIsSelf) {
        var table = tree.CreateAssocList();
        var key = keyIsSelf ? new DreamValue(table) : new DreamValue("self");
        try {
            for (var iteration = 0; iteration < 1000; iteration++) {
                table.SetValue(key, new DreamValue(table));
                table.RemoveValue(key);
                Assert.That(table.GetLength(), Is.Zero);
                Assert.That(table.Deleted, Is.False, "Caller survives removing self owners");
            }
            Assert.That(table.RefCount, Is.EqualTo(1), $"ASSOC_REMOVE_SELF_{keyIsSelf}_OWNER_1000");
            if (table.RefCount == 1) AssocRemovalFinalDelete(table, null, refs);
        } finally {
            if (!table.Deleted) table.Delete();
        }
    }

    private static void AssocRemovalNoOpAndOrder(DreamObjectTree tree, DreamRefManager refs) {
        var table = tree.CreateAssocList();
        var payload = tree.CreateObject(tree.Datum);
        try {
            table.SetValue(new DreamValue("first"), new DreamValue(payload));
            table.SetValue(new DreamValue("middle"), new DreamValue(2));
            table.SetValue(new DreamValue("last"), new DreamValue(3));
            for (var iteration = 0; iteration < 1000; iteration++) {
                table.RemoveValue(new DreamValue(payload)); // Absent borrowed object key.
                Assert.That(payload.RefCount, Is.EqualTo(2));
                Assert.That(table.GetValues(), Is.EqualTo(new[] {
                    new DreamValue("first"), new DreamValue("middle"), new DreamValue("last") }));
            }
            table.RemoveValue(new DreamValue("middle"));
            Assert.That(table.GetValues(), Is.EqualTo(new[] { new DreamValue("first"), new DreamValue("last") }));
            using (var first = table.GetValue(new DreamValue("first")))
                Assert.That(first.MustGetValueAsDreamObject(), Is.SameAs(payload));
            using (var last = table.GetValue(new DreamValue("last")))
                Assert.That(last.MustGetValueAsFloat(), Is.EqualTo(3));
            table.Cut();
            Assert.That(payload.RefCount, Is.EqualTo(1));
            AssocRemovalFinalDelete(payload, null, refs);
            // Existing self-list operator branches bypass RemoveValue. Keep their behavior.
            table.SetValue(new DreamValue("scalar"), new DreamValue(1));
            using (var masked = table.OperatorMask(new DreamValue(table)))
                Assert.That(masked.MustGetValueAsDreamObject(), Is.SameAs(table));
            Assert.That(table.GetLength(), Is.EqualTo(1));
            using (var subtracted = table.OperatorSubtract(new DreamValue(table), null!))
                Assert.That(subtracted.MustGetValueAsIDreamList().GetLength(), Is.Zero);
            Assert.That(table.GetLength(), Is.EqualTo(1));
            using (var removed = table.OperatorRemove(new DreamValue(table)))
                Assert.That(removed.MustGetValueAsDreamObject(), Is.SameAs(table));
            Assert.That(table.GetLength(), Is.Zero);
            Assert.That(table.RefCount, Is.EqualTo(1));
            AssocRemovalFinalDelete(table, null, refs);
        } finally {
            if (!table.Deleted) table.Delete();
            if (!payload.Deleted) payload.Delete();
        }
    }

    private static void AssocRemovalCompiled(DreamManager manager, DreamObjectTree tree, DreamRefManager refs,
            DreamProc proc, bool list, string route) {
        var table = tree.CreateAssocList();
        var payload = AssocRemovalPayload(tree, list, out var child);
        try {
            for (var iteration = 0; iteration < 1000; iteration++) {
                table.SetValue(new DreamValue("remove"), new DreamValue(payload));
                table.SetValue(new DreamValue("keep"), new DreamValue(7));
                using (var result = DreamThread.Run(proc, manager.WorldInstance, null,
                        new DreamValue(table), new DreamValue("remove"), new DreamValue(route))) {
                    if (route is "remove" or "remove-all") {
                        Assert.That(result.MustGetValueAsFloat(), Is.EqualTo(1));
                    } else {
                        var returned = result.MustGetValueAsIDreamList();
                        Assert.That(returned.GetValues(), Is.EqualTo(new[] { new DreamValue("keep") }));
                        using var kept = returned.GetValue(new DreamValue("keep"));
                        Assert.That(kept.MustGetValueAsFloat(), Is.EqualTo(7));
                        if (route == "subtract")
                            Assert.That(result.MustGetValueAsDreamObject(), Is.Not.SameAs(table));
                        else
                            Assert.That(result.MustGetValueAsDreamObject(), Is.SameAs(table));
                    }
                    Assert.That(table.ContainsKey(new DreamValue("remove")), Is.EqualTo(route == "subtract"));
                }
                table.Cut(); // Subtraction leaves the original's legitimate owner intact.
                Assert.That(table.RefCount, Is.EqualTo(1));
                Assert.That(manager.LastDMException, Is.Null);
            }
            Assert.That(payload.RefCount, Is.EqualTo(1), $"ASSOC_REMOVE_COMPILED_{list}_{route}_OWNER_1000");
            if (payload.RefCount == 1) AssocRemovalFinalDelete(payload, child, refs);
        } finally {
            table.DecRef();
            if (!payload.Deleted) payload.Delete();
        }
    }
}
