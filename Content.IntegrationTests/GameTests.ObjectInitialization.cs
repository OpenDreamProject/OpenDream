using System;
using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;

namespace Content.IntegrationTests;

public sealed partial class GameTests {
    private static void ObjectInitializationReleasesOwnedValues(DreamManager manager, DreamObjectTree tree, DreamRefManager refs) {
        Assert.That(tree.TryGetGlobalProc("object_initialization_new_probe", out var newProbe), Is.True);
        var payload = tree.CreateObject(tree.Datum);
        var instance = tree.CreateObject(tree.Datum);
        var usr = tree.CreateObject(tree.Datum);
        var ids = new[] { payload.RefId, instance.RefId, usr.RefId };
        // Read the actual internal frame pool without widening production visibility.
        var initStateType = typeof(DreamThread).Assembly.GetType("OpenDreamRuntime.Procs.InitDreamObjectState", throwOnError: true)!;
        var initPool = (System.Collections.ICollection)initStateType.GetField("Pool",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!.GetValue(null)!;
        static DreamValue[] Arguments(int count, DreamObject owner) {
            var values = new DreamValue[count];
            Array.Fill(values, DreamValue.Null);
            values[0] = new DreamValue(owner);
            return values;
        }
        try {
            for (var iteration = 0; iteration < 1000; iteration++) {
                var accepted = instance.InitProc(new DreamThread("accepted object initialization"), usr,
                    new DreamProcArguments(Arguments(256, payload)));
                try {
                    // Existing GetArguments uses inherited ArgumentCount, which Initialize
                    // does not set. Observe the actual copy and private count used by Resume;
                    // this ownership fixture does not repair or mask that separate API defect.
                    var copied = (DreamValue[])initStateType.GetField("_arguments",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(accepted)!;
                    var copiedCount = (int)initStateType.GetField("_argumentCount",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(accepted)!;
                    Assert.That(copied.Length, Is.EqualTo(256));
                    Assert.That(copiedCount, Is.EqualTo(256));
                    Assert.That(copied[0].MustGetValueAsDreamObject(), Is.SameAs(payload));
                    for (var index = 1; index < copiedCount; index++)
                        Assert.That(copied[index].IsNull, Is.True);
                    Assert.That(payload.RefCount, Is.EqualTo(2));
                    Assert.That(instance.RefCount, Is.EqualTo(2));
                    Assert.That(usr.RefCount, Is.EqualTo(2));
                } finally {
                    accepted.Dispose();
                }
                Assert.That(payload.RefCount, Is.EqualTo(1));
                Assert.That(instance.RefCount, Is.EqualTo(1));
                Assert.That(usr.RefCount, Is.EqualTo(1));
                var poolBefore = initPool.Count;
                var error = Assert.Throws<ArgumentException>(() => instance.InitProc(
                    new DreamThread("rejected object initialization"), usr,
                    new DreamProcArguments(Arguments(257, payload))));
                Assert.That(error!.ParamName, Is.EqualTo("destination"));
                Assert.That(error.Message, Is.EqualTo("Destination is too short. (Parameter 'destination')"));
                // Order makes both direct omission controls stop at this exact assertion.
                Assert.That(payload.RefCount, Is.EqualTo(1), "Rejected object initialization releases input owner");
                Assert.That(instance.RefCount, Is.EqualTo(1), "Rejected object initialization acquires no instance owner");
                Assert.That(usr.RefCount, Is.EqualTo(1), "Rejected object initialization acquires no usr owner");
                Assert.That(initPool.Count, Is.EqualTo(poolBefore));
                foreach (var count in new[] { 256, 257 }) {
                    var bucketBefore = refs.GetCountOf(RefType.DreamObjectDatum);
                    using (var result = DreamThread.Run(newProbe!, manager.WorldInstance, null,
                        new DreamValue(payload), new DreamValue(count))) {
                        Assert.That(result.MustGetValueAsFloat(), Is.EqualTo(1), "Actual new opcode accepts256/catches257 exact exception");
                    }
                    var retainedCreatorCount = refs.GetCountOf(RefType.DreamObjectDatum) - bucketBefore;
                    Assert.That(retainedCreatorCount, Is.EqualTo(0), "Rejected new opcode retains no creator owner");
                    Assert.That(payload.RefCount, Is.EqualTo(1));
                }
                Assert.That(payload.Deleted || instance.Deleted || usr.Deleted, Is.False);
            }
            Assert.That(manager.LastDMException, Is.Null);
        } finally {
            payload.DecRef(); instance.DecRef(); usr.DecRef();
        }
        Assert.That(payload.Deleted && instance.Deleted && usr.Deleted, Is.True);
        foreach (var id in ids) {
            using var located = refs.LocateRef(id);
            Assert.That(located.IsNull, Is.True, "Object initialization caller leaves actual reference bucket");
        }
    }
}
