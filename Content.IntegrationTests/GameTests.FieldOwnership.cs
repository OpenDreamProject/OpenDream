using System;
using DMCompiler.Json;
using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;

namespace Content.IntegrationTests;

public sealed partial class GameTests {
    private static DMProcState FieldFrame(DreamObjectTree tree) {
        var proc = (DMProc)tree.LoadProcJson(-1, new ProcDefinitionJson {
            OwningTypeId = tree.Datum.Id,
            Name = "field_ownership_fixture",
            SourceInfo = [],
            MaxStackSize = 1
        });
        if (!DMProcState.Pool.TryPop(out var state)) state = new DMProcState();
        state.Initialize(proc, new DreamThread("field_ownership_fixture"), 1, null, null, default);
        return state;
    }

    private static DreamReference FieldReference(DreamObjectTree tree, string name) {
        var id = tree.Strings.Count;
        tree.Strings.Add(name);
        return DreamReference.CreateField(id);
    }

    private static void FieldFinalBucketRemoval(DreamObject owner, DreamRefManager refs) {
        var refId = owner.RefId;
        owner.DecRef();
        Assert.That(owner.Deleted, Is.True, "FIELD_FINAL_DELETE_ASSERT");
        if (!owner.Deleted) return; // Do not release a borrowed lookup on failing controls.
        // LocateRef(uint) wraps the bucket value without IncRef; only use it after deletion.
        using var located = refs.LocateRef(refId);
        Assert.That(located.TryGetValueAsDreamObject(out var obj) && obj != null,
            Is.False, "FIELD_FINAL_BUCKET_ASSERT");
    }

    private static void FieldMissingOwners1000(DreamObjectTree tree, DreamRefManager refs, bool read) {
        var owner = new DreamObject(tree.Datum.ObjectDefinition);
        var reference = FieldReference(tree, "__field_ownership_missing_2e057__");
        Assert.That(owner.ObjectDefinition.Variables.ContainsKey("__field_ownership_missing_2e057__"), Is.False);
        try {
            for (var i = 0; i < 1000; i++) {
                var frame = FieldFrame(tree);
                try {
                    frame.Push(new DreamValue(owner));
                    if (read) {
                        Assert.Throws<DMException>(() => {
                            using var result = frame.GetReferenceValue(reference);
                        });
                    } else {
                        Assert.Throws<DMException>(() => frame.AssignReference(reference, DreamValue.Null));
                    }
                    Assert.That(frame.DebugStack().Length, Is.Zero);
                } finally { frame.Dispose(); }
            }
            Assert.That(owner.RefCount, Is.EqualTo(1), read ? "FIELD_READ_OWNER_ASSERT_1000" : "FIELD_WRITE_OWNER_ASSERT_1000");
            if (owner.RefCount != 1) return; // Contain failed controls without adding deletion assertions.
            FieldFinalBucketRemoval(owner, refs);
        } finally {
            if (!owner.Deleted) owner.Delete();
        }
    }

    private static void FieldSuccessAndPeekOwnership(DreamObjectTree tree, DreamRefManager refs) {
        var owner = new DreamObject(tree.Datum.ObjectDefinition);
        try {
            foreach (var peek in new[] { false, true }) {
                foreach (var read in new[] { false, true }) {
                    foreach (var mode in new[] { "success", "missing", "invalid-string" }) {
                        var reference = mode == "invalid-string"
                            ? DreamReference.CreateField(tree.Strings.Count)
                            : FieldReference(tree, mode == "success" ? "tag" : "__field_ownership_missing_2e057__");
                        var frame = FieldFrame(tree);
                        try {
                            frame.Push(new DreamValue(owner));
                            void Access() {
                                if (read) {
                                    using var result = frame.GetReferenceValue(reference, peek);
                                    Assert.That(result.TryGetValueAsString(out var tag) && tag == "fixture-tag", Is.True);
                                } else {
                                    frame.AssignReference(reference, new DreamValue("fixture-tag"), peek);
                                    using var result = owner.GetVariable("tag");
                                    Assert.That(result.TryGetValueAsString(out var tag) && tag == "fixture-tag", Is.True);
                                }
                            }
                            if (mode == "success") Access();
                            else if (mode == "missing") Assert.Throws<DMException>(() => Access());
                            else Assert.Throws<ArgumentOutOfRangeException>(() => Access());
                            Assert.That(frame.DebugStack().Length, Is.EqualTo(peek ? 1 : 0));
                            Assert.That(owner.RefCount, Is.EqualTo(peek ? 2 : 1), "FIELD_PEEK_OWNER_ASSERT");
                            if (owner.RefCount != (peek ? 2 : 1)) return;
                        } finally { frame.Dispose(); }
                        Assert.That(owner.RefCount, Is.EqualTo(1), "FIELD_CALLER_ONLY_ASSERT");
                    }
                }
            }
            FieldFinalBucketRemoval(owner, refs);
        } finally {
            if (!owner.Deleted) owner.Delete();
        }
    }
    private static void FieldReferencesReleaseOwners(DreamObjectTree tree, DreamRefManager refs) {
        Assert.Multiple(() => {
            FieldMissingOwners1000(tree, refs, false);
            FieldMissingOwners1000(tree, refs, true);
            FieldSuccessAndPeekOwnership(tree, refs);
        });
        TestContext.Out.WriteLine("FIELD_OWNERSHIP_COMPLETE: 1000 setter and getter errors, valid/peek ownership, final buckets");
    }
}
