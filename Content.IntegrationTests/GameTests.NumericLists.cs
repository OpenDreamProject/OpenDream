using System;
using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;

namespace Content.IntegrationTests;

public sealed partial class GameTests {
    private static void NumericListAssignmentsReleaseOwners(DreamManager manager, DreamObjectTree tree, DreamRefManager refs) {
        var datumBefore = refs.GetCountOf(RefType.DreamObjectDatum);
        var listBefore = refs.GetCountOf(RefType.DreamObjectList);
        var payload = tree.CreateObject(tree.Datum);
        var previous = tree.CreateObject(tree.Datum);
        var list = tree.CreateList();
        var ids = new[] { payload.RefId, previous.RefId, list.RefId };
        try {
            list.AddValue(new DreamValue(previous));
            for (var iteration = 0; iteration < 1000; iteration++) {
                foreach (var index in new[] { 0, -1, 3 }) {
                    foreach (var growth in new[] { false, true }) {
                        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
                            list.SetValue(new DreamValue(index), new DreamValue(payload), growth));
                        Assert.That(error!.ParamName, Is.EqualTo("index"));
                        Assert.That(payload.RefCount, Is.EqualTo(1), "Invalid numeric list assignment releases incoming owner");
                        Assert.That(previous.RefCount, Is.EqualTo(2));
                        Assert.That(list.GetLength(), Is.EqualTo(1));
                        using var unchanged = list.GetValue(new DreamValue(1));
                        Assert.That(unchanged.MustGetValueAsDreamObject(), Is.SameAs(previous));
                    }
                }
            }
            for (var iteration = 0; iteration < 1000; iteration++) {
                list.SetValue(new DreamValue(1), new DreamValue(payload));
                Assert.That(payload.RefCount, Is.EqualTo(2), "Numeric replacement retains incoming owner");
                Assert.That(previous.RefCount, Is.EqualTo(1));
                // Borrowed incoming value: list owner must survive self-replacement.
                list.SetValue(new DreamValue(1), new DreamValue(payload));
                Assert.That(payload.RefCount, Is.EqualTo(2));
                using (var replaced = list.GetValue(new DreamValue(1)))
                    Assert.That(replaced.MustGetValueAsDreamObject(), Is.SameAs(payload));
                list.SetValue(new DreamValue(1), new DreamValue(previous));
                Assert.That(payload.RefCount, Is.EqualTo(1));
                Assert.That(previous.RefCount, Is.EqualTo(2));
            }
            list.SetValue(new DreamValue(2), new DreamValue(payload), allowGrowth: true);
            Assert.That(list.GetLength(), Is.EqualTo(2));
            Assert.That(payload.RefCount, Is.EqualTo(2));
            using (var appended = list.GetValue(new DreamValue(2)))
                Assert.That(appended.MustGetValueAsDreamObject(), Is.SameAs(payload));
            var soleList = tree.CreateList();
            var sole = tree.CreateObject(tree.Datum);
            var soleId = sole.RefId;
            var soleListId = soleList.RefId;
            try {
                soleList.AddValue(new DreamValue(sole));
                sole.DecRef(); // Now exactly the list owns this datum.
                Assert.That(sole.RefCount, Is.EqualTo(1));
                soleList.SetValue(new DreamValue(1), new DreamValue(sole));
                Assert.That(sole.RefCount, Is.EqualTo(1));
                Assert.That(sole.Deleted, Is.False, "Sole list owner survives self-replacement");
            } finally {
                soleList.DecRef();
            }
            Assert.That(sole.Deleted && soleList.Deleted, Is.True);
            foreach (var id in new[] { soleId, soleListId }) {
                using var located = refs.LocateRef(id);
                Assert.That(located.IsNull, Is.True);
            }
            Assert.That(manager.LastDMException, Is.Null);
        } finally {
            list.DecRef(); payload.DecRef(); previous.DecRef();
        }
        Assert.That(payload.Deleted && previous.Deleted && list.Deleted, Is.True);
        foreach (var id in ids) {
            using var located = refs.LocateRef(id);
            Assert.That(located.IsNull, Is.True, "Numeric list caller leaves actual reference bucket");
        }
        Assert.That(refs.GetCountOf(RefType.DreamObjectDatum), Is.EqualTo(datumBefore));
        Assert.That(refs.GetCountOf(RefType.DreamObjectList), Is.EqualTo(listBefore));
        TestContext.Progress.WriteLine("Numeric list ownership: 1000 invalid-index and replacement iterations plus append/sole-owner self-assignment passed");
    }
}
