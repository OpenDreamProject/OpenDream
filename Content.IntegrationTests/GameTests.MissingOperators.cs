using System;
using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;

namespace Content.IntegrationTests;

public sealed partial class GameTests {
    private static void SupportedOperatorFallbacksReleaseOwners(DreamManager manager, DreamObjectTree tree, DreamRefManager refs) {
        foreach (var probe in new[] { "operator_divide_fallback_probe", "operator_multiply_fallback_probe", "operator_add_defined_probe", "operator_divide_exact_probe", "operator_multiply_exact_probe", "operator_index_defined_probe", "operator_index_assign_defined_probe" }) {
            Assert.That(tree.TryGetGlobalProc(probe, out var proc), Is.True);
            var payload = tree.CreateObject(tree.Datum);
            var id = payload.RefId;
            try {
                for (var iteration = 0; iteration < 1000; iteration++) {
                    using (var result = DreamThread.Run(proc!, manager.WorldInstance, null, new DreamValue(payload))) {
                        if (probe is "operator_divide_exact_probe" or "operator_multiply_exact_probe")
                            Assert.That(result.MustGetValueAsFloat(), Is.EqualTo(73), "Defined assignment overload takes precedence over fallback");
                        else {
                            Assert.That(result.MustGetValueAsDreamObject(), Is.SameAs(payload), "Defined overload/fallback returns the actual caller payload");
                            Assert.That(payload.Deleted, Is.False, "Returned payload remains alive before result disposal");
                        }
                    }
                    Assert.That(payload.RefCount, Is.EqualTo(1), $"Supported operator releases payload owner: {probe}");
                    Assert.That(payload.Deleted, Is.False, "Supported operator preserves caller owner after result disposal");
                }
            } finally {
                payload.DecRef();
            }
            Assert.That(payload.Deleted, Is.True);
            using var located = refs.LocateRef(id);
            Assert.That(located.IsNull, Is.True, "Supported operator caller leaves actual reference bucket");
        }
        Assert.That(manager.LastDMException, Is.Null);
        TestContext.Progress.WriteLine("Supported operator ownership: 1000 division/multiplication fallbacks and defined precedence/index branches passed");
    }

    // Hook into the existing connected NoRuntimesTest's owning server thread.
    private static void MissingOperatorArgumentsReleaseOwners(DreamManager manager, DreamObjectTree tree, DreamRefManager refs) {
        var instance = tree.CreateObject(tree.Datum);
        var payload = tree.CreateObject(tree.Datum);
        var index = tree.CreateObject(tree.Datum);
        var value = tree.CreateObject(tree.Datum);
        var ids = new[] { instance.RefId, payload.RefId, index.RefId, value.RefId };
        try {
            foreach (var name in new[] { "operator+", "operator-", "operator*", "operator*=", "operator/", "operator/=", "operator|", "operator[]", "operator[]=" })
                Assert.That(instance.TryGetProc(name, out _), Is.False);
            var b = new DreamValue(payload);
            var cases = new (string Name, Action Invoke, string Message)[] {
                ("add", () => { using var result = instance.OperatorAdd(b, null!); }, $"Addition cannot be done between {instance} and {b}"),
                ("subtract", () => { using var result = instance.OperatorSubtract(b, null!); }, $"Subtraction cannot be done between {instance} and {b}"),
                ("multiply", () => { using var result = instance.OperatorMultiply(b, null!); }, $"Multiplication cannot be done between {instance} and {b}"),
                ("multiply-ref", () => { using var result = instance.OperatorMultiplyRef(b, null!); }, $"Multiplication cannot be done between {instance} and {b}"),
                ("divide", () => { using var result = instance.OperatorDivide(b, null!); }, $"Division cannot be done between {instance} and {b}"),
                ("divide-ref", () => { using var result = instance.OperatorDivideRef(b, null!); }, $"Division cannot be done between {instance} and {b}"),
                ("or", () => { using var result = instance.OperatorOr(b, null!); }, $"Cannot or {instance} and {b}"),
                ("index", () => { using var result = instance.OperatorIndex(new DreamValue(index), null!); }, $"Cannot index {instance} with {new DreamValue(index)}"),
                ("index-assign", () => instance.OperatorIndexAssign(new DreamValue(index), null!, new DreamValue(value)), $"Cannot assign {new DreamValue(value)} to index {new DreamValue(index)} of {instance}"),
            };
            for (var iteration = 0; iteration < 1000; iteration++) {
                foreach (var test in cases) {
                    var error = Assert.Throws<InvalidOperationException>(() => test.Invoke());
                    Assert.That(error!.Message, Is.EqualTo(test.Message), "Missing overload preserves exact fallback exception");
                    Assert.That(payload.RefCount, Is.EqualTo(1), $"Missing operator releases owned payload: {test.Name}");
                    Assert.That(index.RefCount, Is.EqualTo(1), "Missing index operator releases index owner");
                    Assert.That(value.RefCount, Is.EqualTo(1), "Missing index assignment releases value owner");
                    Assert.That(instance.RefCount, Is.EqualTo(1));
                    Assert.That(payload.Deleted || index.Deleted || value.Deleted || instance.Deleted, Is.False);
                }
            }
            Assert.That(manager.LastDMException, Is.Null);
            TestContext.Progress.WriteLine("Missing operator ownership: 1000 direct missing-overload iterations passed");
        } finally {
            instance.DecRef(); payload.DecRef(); index.DecRef(); value.DecRef();
        }
        Assert.That(instance.Deleted && payload.Deleted && index.Deleted && value.Deleted, Is.True);
        foreach (var id in ids) {
            using var located = refs.LocateRef(id);
            Assert.That(located.IsNull, Is.True, "Missing operator caller leaves actual reference bucket");
        }
    }
}
