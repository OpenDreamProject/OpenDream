using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Resources;

namespace Content.IntegrationTests;

public sealed partial class GameTests {
    private sealed class ExpectedCancellationLog : DreamResource {
        public int Count;

        public ExpectedCancellationLog() : base(-1, null, null) {}

        public override void Output(DreamValue value) {
            Assert.That(value.MustGetValueAsString(), Does.Contain("stack depth limit reached"));
            Count++;
        }
    }

    private static void CancelledThreadsReleaseOwnedFrames(DreamManager manager, DreamObjectTree tree) {
        Assert.That(tree.TryGetGlobalProc("cancellation_depth_probe", out var proc), Is.True);
        var payload = tree.CreateObject(tree.Datum);
        var originalReferences = payload.RefCount;
        using var originalLog = manager.WorldInstance.GetVariable("log");
        var originalException = manager.LastDMException;
        var expectedLog = new ExpectedCancellationLog();
        manager.WorldInstance.SetVariable("log", new DreamValue(expectedLog));
        try {
            for (var iteration = 0; iteration < 5; iteration++) {
                using (var result = DreamThread.Run(proc!, manager.WorldInstance, payload, new DreamValue(payload))) {
                    Assert.That(result.MustGetValueAsDreamObject(), Is.SameAs(payload));
                    Assert.That(payload.RefCount, Is.EqualTo(originalReferences + 1),
                        "Stack cancellation must release accepted and rejected frames while returning one owned result");
                }
                Assert.That(payload.RefCount, Is.EqualTo(originalReferences),
                    "Disposing the cancelled result must release its remaining ownership");
                Assert.That(payload.Deleted, Is.False);
                Assert.That(manager.LastDMException?.Message, Does.Contain("stack depth limit reached"));
            }
            Assert.That(expectedLog.Count, Is.EqualTo(5));

            Assert.That(tree.TryGetGlobalProc("cancellation_operator_probe", out var operatorProc), Is.True);
            var recursiveOperator = tree.CreateObject(tree.GetTreeEntry("/datum/cancellation_operator"));
            try {
                using var result = DreamThread.Run(operatorProc!, manager.WorldInstance, payload,
                    new DreamValue(recursiveOperator), new DreamValue(payload));
                Assert.That(result.Type, Is.EqualTo(default(DreamValue).Type));
                Assert.That(payload.RefCount, Is.EqualTo(originalReferences),
                    "Reentrant cancellation must release its discarded owned result and all proc frames");
                Assert.That(recursiveOperator.RefCount, Is.EqualTo(1));
                Assert.That(expectedLog.Count, Is.EqualTo(6));
            } finally {
                recursiveOperator.DecRef();
            }
        } finally {
            manager.WorldInstance.SetVariable("log", originalLog);
            manager.LastDMException = originalException;
            payload.DecRef();
        }
    }
}
