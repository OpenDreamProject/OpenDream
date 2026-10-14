using System;
using DMCompiler.Bytecode;
using DMCompiler.Json;
using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;

namespace Content.IntegrationTests;

public sealed partial class GameTests {
    private static float RunRangeOwnershipOpcode(DreamObjectTree tree, DreamValue value, DreamValue start, DreamValue end) {
        // Dispatch the real production opcode through the public frame API.
        // DMOpcodeHandlers itself is internal and is not called from this assembly.
        var proc = (DMProc)tree.LoadProcJson(-1, new ProcDefinitionJson {
            OwningTypeId = tree.Datum.Id, Name = "range_ownership_fixture",
            SourceInfo = [], MaxStackSize = 3,
            Bytecode = [(byte)DreamProcOpcode.IsInRange, (byte)DreamProcOpcode.Return]
        });
        if (!DMProcState.Pool.TryPop(out var frame)) frame = new DMProcState();
        frame.Initialize(proc, new DreamThread("range_ownership_fixture"), 3, null, null, default);
        try {
            frame.Push(value);
            frame.Push(start);
            frame.Push(end);
            Assert.That(frame.Resume(), Is.EqualTo(ProcStatus.Returned));
            Assert.That(frame.DebugStack().Length, Is.Zero);
            var result = frame.Result;
            return result.MustGetValueAsFloat();
        } finally {
            frame.Dispose();
        }
    }

    private static void RangeOperandsOwnExactlyOneCaller(DreamObjectTree tree, DreamRefManager refs, bool list, int role) {
        DreamObject owner = list ? tree.CreateList() : tree.CreateObject(tree.Datum);
        var id = owner.RefId;
        try {
            for (var iteration = 0; iteration < 1000; iteration++) {
                var values = new[] { new DreamValue(0), new DreamValue(0), new DreamValue(1) };
                for (var position = 0; position < values.Length; position++)
                    if (role == 3 || role == position) values[position] = new DreamValue(owner);
                Assert.That(RunRangeOwnershipOpcode(tree, values[0], values[1], values[2]), Is.EqualTo(1),
                    "Non-numeric range operands retain existing zero coercion");
            }
            var kind = list ? "LIST" : "DATUM";
            var slot = role == 3 ? "ALIAS" : role.ToString();
            Assert.That(owner.RefCount, Is.EqualTo(1), $"RANGE_{kind}_{slot}_OWNER_ASSERT_1000");
            if (owner.RefCount != 1) return;
            owner.DecRef();
            Assert.That(owner.Deleted, Is.True, "RANGE_FINAL_DELETE_ASSERT");
            if (!owner.Deleted) return;
            using var located = refs.LocateRef(id);
            Assert.That(located.IsNull, Is.True, "RANGE_FINAL_BUCKET_ASSERT");
        } finally {
            // Contain deliberately faulty controls after preserving their assertions.
            if (!owner.Deleted) owner.Delete();
        }
    }

    private static void InRangeOperandsReleaseOwners(DreamManager manager, DreamObjectTree tree, DreamRefManager refs) {
        var datums = refs.GetCountOf(RefType.DreamObjectDatum);
        var lists = refs.GetCountOf(RefType.DreamObjectList);
        Assert.Multiple(() => {
            foreach (var list in new[] { false, true })
                for (var role = 0; role < 4; role++) RangeOperandsOwnExactlyOneCaller(tree, refs, list, role);
        });
        Assert.That(refs.GetCountOf(RefType.DreamObjectDatum), Is.EqualTo(datums));
        Assert.That(refs.GetCountOf(RefType.DreamObjectList), Is.EqualTo(lists));
        Assert.That(tree.TryGetGlobalProc("range_ownership_compiled_probe", out var compiled), Is.True);
        var cases = new[] {
            (new DreamValue(1), new DreamValue(1), new DreamValue(2), 1f),
            (new DreamValue(2), new DreamValue(1), new DreamValue(2), 1f),
            (new DreamValue(0), new DreamValue(1), new DreamValue(2), 0f),
            (new DreamValue(1), new DreamValue(2), new DreamValue(0), 0f),
            (new DreamValue(-1), new DreamValue(-2), new DreamValue(0), 1f),
            (DreamValue.Null, new DreamValue(0), new DreamValue(1), 1f),
            (new DreamValue("text"), new DreamValue(0), new DreamValue(1), 1f),
            (new DreamValue(0), new DreamValue("text"), new DreamValue(1), 1f),
            (new DreamValue(0), new DreamValue(0), DreamValue.Null, 1f),
            (new DreamValue(float.NaN), new DreamValue(0), new DreamValue(1), 0f),
            (new DreamValue(float.PositiveInfinity), new DreamValue(float.NegativeInfinity), new DreamValue(float.PositiveInfinity), 1f)
        };
        foreach (var (value, start, end, expected) in cases) {
            Assert.That(RunRangeOwnershipOpcode(tree, value, start, end), Is.EqualTo(expected));
            using var result = DreamThread.Run(compiled!, manager.WorldInstance, null, value, start, end);
            Assert.That(result.MustGetValueAsFloat(), Is.EqualTo(expected), "Compiled DM and direct opcode parity");
        }
        Assert.That(manager.LastDMException, Is.Null);
        TestContext.Out.WriteLine("RANGE_OWNERSHIP_COMPLETE: 1000 datum/list operand roles and aliases, scalar/compiler parity, final buckets");
    }
}
