using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;

namespace Content.IntegrationTests;

public sealed partial class GameTests {
    // Actual compiled DM call statements, with rejection before NativeLibrary loading.
    private static void ExternalCallRejectionsReleaseArguments(DreamManager manager, DreamObjectTree tree, DreamRefManager refs) {
        foreach (var probe in new[] { "call_ext_string_rejection_probe", "call_ext_byond_rejection_probe" }) {
            Assert.That(tree.TryGetGlobalProc(probe, out var proc), Is.True);
            var payload = tree.CreateObject(tree.Datum);
            var id = payload.RefId;
            try {
                for (var iteration = 0; iteration < 1000; iteration++) {
                    using (var result = DreamThread.Run(proc!, manager.WorldInstance, null, new DreamValue(payload))) {
                        Assert.That(result.MustGetValueAsString(), Is.EqualTo("Stdcall calling convention is not supported in OpenDream"), "Actual DM catch preserves exact resolver rejection message");
                    }
                    Assert.That(payload.RefCount, Is.EqualTo(1), $"External call releases argument owner: {probe}");
                    Assert.That(payload.Deleted, Is.False, "Resolver rejection preserves caller payload");
                    Assert.That(manager.LastDMException, Is.Null, "Resolver rejection was caught by actual DM probe");
                }
            } finally {
                payload.DecRef();
            }
            Assert.That(payload.Deleted, Is.True);
            using var located = refs.LocateRef(id);
            Assert.That(located.IsNull, Is.True, "External call caller leaves actual reference bucket");
        }
        TestContext.Progress.WriteLine("External call ownership: 1000 caught string and BYOND ABI resolver rejections passed");
    }
}
