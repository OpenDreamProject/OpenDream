using System;
using DMCompiler.Json;
using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;

namespace Content.IntegrationTests;

public sealed partial class GameTests {
    private static DMProcState IndexReadFrame(DreamObjectTree tree) {
        var proc = (DMProc)tree.LoadProcJson(-1, new ProcDefinitionJson {
            OwningTypeId = tree.Datum.Id, Name = "index_read_ownership_fixture",
            SourceInfo = [], MaxStackSize = 2
        });
        if (!DMProcState.Pool.TryPop(out var state)) state = new DMProcState();
        state.Initialize(proc, new DreamThread("index_read_ownership_fixture"), 2, null, null, default);
        return state;
    }

    private static void IndexReadFinalBucket(DreamObject owner, DreamRefManager refs) {
        var id = owner.RefId;
        owner.DecRef();
        Assert.That(owner.Deleted, Is.True, "INDEX_READ_FINAL_DELETE_ASSERT");
        if (!owner.Deleted) return;
        // LocateRef wraps without acquiring; inspect only after deletion.
        using var located = refs.LocateRef(id);
        Assert.That(located.IsNull, Is.True, "INDEX_READ_FINAL_BUCKET_ASSERT");
    }

    private static void IndexReadMissingOwners1000(DreamObjectTree tree, DreamRefManager refs, bool missingOperator) {
        DreamObject owner = missingOperator ? tree.CreateObject(tree.Datum) : tree.CreateList();
        var indexOwner = missingOperator ? tree.CreateObject(tree.Datum) : null;
        var index = indexOwner is null ? new DreamValue(0) : new DreamValue(indexOwner);
        if (missingOperator) Assert.That(owner.TryGetProc("operator[]", out _), Is.False);
        try {
            for (var i = 0; i < 1000; i++) {
                var frame = IndexReadFrame(tree);
                try {
                    frame.Push(new DreamValue(owner));
                    frame.Push(index);
                    if (missingOperator) {
                        var error = Assert.Throws<InvalidOperationException>(() => {
                            using var result = frame.GetReferenceValue(DreamReference.ListIndex);
                        });
                        Assert.That(error!.Message, Is.EqualTo($"Cannot index {owner} with {index}"));
                    } else {
                        var error = Assert.Throws<ArgumentOutOfRangeException>(() => {
                            using var result = frame.GetReferenceValue(DreamReference.ListIndex);
                        });
                        Assert.That(error!.ParamName, Is.EqualTo("index"));
                    }
                    Assert.That(frame.DebugStack().Length, Is.Zero);
                } finally {
                    frame.Dispose();
                }
            }
            Assert.That(owner.RefCount, Is.EqualTo(1), missingOperator ? "INDEX_READ_DATUM_OWNER_ASSERT_1000" : "INDEX_READ_LIST_OWNER_ASSERT_1000");
            if (indexOwner is not null)
                Assert.That(indexOwner.RefCount, Is.EqualTo(1), "INDEX_READ_INDEX_OWNER_ASSERT_1000");
            if (owner.RefCount != 1 || (indexOwner is not null && indexOwner.RefCount != 1)) return;
            IndexReadFinalBucket(owner, refs);
            if (indexOwner is not null) IndexReadFinalBucket(indexOwner, refs);
        } finally {
            // Contain baseline-only retained objects without hiding the failed assertions.
            if (!owner.Deleted) owner.Delete();
            if (indexOwner is not null && !indexOwner.Deleted) indexOwner.Delete();
        }
    }

    private static void IndexReadBorrowedAndValidParity(DreamObjectTree tree, DreamRefManager refs) {
        foreach (var peek in new[] { false, true }) {
            var list = tree.CreateList();
            var value = tree.CreateObject(tree.Datum);
            var key = tree.CreateObject(tree.Datum);
            try {
                list.AddValue(new DreamValue(value));
                list.SetValue(new DreamValue(key), new DreamValue(value));
                foreach (var index in new[] { new DreamValue(1), new DreamValue(key) }) {
                    var frame = IndexReadFrame(tree);
                    try {
                        frame.Push(new DreamValue(list));
                        frame.Push(index);
                        using (var result = frame.GetReferenceValue(DreamReference.ListIndex, peek)) {
                            Assert.That(result.MustGetValueAsDreamObject(), Is.SameAs(value));
                            Assert.That(value.RefCount, Is.EqualTo(4)); // caller + two list slots + result
                        }
                        Assert.That(value.RefCount, Is.EqualTo(3));
                        Assert.That(list.RefCount, Is.EqualTo(peek ? 2 : 1));
                        Assert.That(key.RefCount, Is.EqualTo(peek && index.Type != DreamValue.DreamValueType.Float ? 3 : 2));
                        Assert.That(frame.DebugStack().Length, Is.EqualTo(peek ? 2 : 0));
                    } finally {
                        frame.Dispose();
                    }
                    Assert.That(list.RefCount, Is.EqualTo(1));
                    Assert.That(key.RefCount, Is.EqualTo(2));
                }
                list.DecRef();
                Assert.That(list.Deleted, Is.True);
                IndexReadFinalBucket(value, refs);
                IndexReadFinalBucket(key, refs);
                using var located = refs.LocateRef(list.RefId);
                Assert.That(located.IsNull, Is.True);
            } finally {
                if (!list.Deleted) list.Delete();
                if (!value.Deleted) value.Delete();
                if (!key.Deleted) key.Delete();
            }
        }
        // On throwing borrowed reads, both stack operands remain owned by the frame.
        foreach (var missingOperator in new[] { false, true }) {
            DreamObject owner = missingOperator ? tree.CreateObject(tree.Datum) : tree.CreateList();
            var indexOwner = tree.CreateObject(tree.Datum);
            var index = missingOperator ? new DreamValue(indexOwner) : new DreamValue(0);
            try {
                var frame = IndexReadFrame(tree);
                try {
                    frame.Push(new DreamValue(owner));
                    frame.Push(index);
                    if (missingOperator) Assert.Throws<InvalidOperationException>(() => {
                        using var result = frame.GetReferenceValue(DreamReference.ListIndex, peek: true);
                    });
                    else Assert.Throws<ArgumentOutOfRangeException>(() => {
                        using var result = frame.GetReferenceValue(DreamReference.ListIndex, peek: true);
                    });
                    Assert.That(owner.RefCount, Is.EqualTo(2));
                    Assert.That(indexOwner.RefCount, Is.EqualTo(missingOperator ? 2 : 1));
                    Assert.That(frame.DebugStack().Length, Is.EqualTo(2));
                } finally {
                    frame.Dispose();
                }
                Assert.That(owner.RefCount, Is.EqualTo(1));
                Assert.That(indexOwner.RefCount, Is.EqualTo(1));
                IndexReadFinalBucket(owner, refs);
                IndexReadFinalBucket(indexOwner, refs);
            } finally {
                if (!owner.Deleted) owner.Delete();
                if (!indexOwner.Deleted) indexOwner.Delete();
            }
        }
    }

    private static void IndexReferenceReadsReleaseOwners(DreamObjectTree tree, DreamRefManager refs) {
        var datums = refs.GetCountOf(RefType.DreamObjectDatum);
        var lists = refs.GetCountOf(RefType.DreamObjectList);
        Assert.Multiple(() => {
            IndexReadMissingOwners1000(tree, refs, false);
            IndexReadMissingOwners1000(tree, refs, true);
            IndexReadBorrowedAndValidParity(tree, refs);
        });
        Assert.That(refs.GetCountOf(RefType.DreamObjectDatum), Is.EqualTo(datums));
        Assert.That(refs.GetCountOf(RefType.DreamObjectList), Is.EqualTo(lists));
        TestContext.Out.WriteLine("INDEX_READ_OWNERSHIP_COMPLETE: 1000 list and operator errors, valid/peek ownership, final buckets");
    }
}
