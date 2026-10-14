using System.Threading.Tasks;
using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;

namespace Content.IntegrationTests;
[TestFixture]
public sealed partial class GameTests : ContentIntegrationTest {

    private static void CompletedSpawnsReuseStates(DreamManager manager, DreamObjectTree tree) {
        Assert.That(tree.TryGetGlobalProc("spawn_pool_reuse_probe", out var proc), Is.True);
        var payload = tree.CreateObject(tree.Datum);
        var originalReferences = payload.RefCount;
        var initialPoolCount = DMProcState.Pool.Count;
        try {
            for (var batch = 0; batch < 10; batch++) {
                using var result = DreamThread.Run(proc!, manager.WorldInstance, payload,
                    new DreamValue(1000), new DreamValue(payload));
                Assert.That(result.MustGetValueAsFloat(), Is.EqualTo(1000));
                Assert.That(payload.RefCount, Is.EqualTo(originalReferences),
                    "Completed spawn copies must release arguments, locals and return values");
                Assert.That(DMProcState.Pool.Count, Is.LessThanOrEqualTo(initialPoolCount + 2),
                    $"Completed spawn batch {batch} must reuse states instead of permanently growing the pool");
            }
            foreach (var state in DMProcState.Pool) {
                Assert.That(state.Thread, Is.Null);
                Assert.That(state.Instance, Is.Null);
                Assert.That(state.Usr, Is.Null);
                Assert.That(state.Proc, Is.Null);
                Assert.That(state.ArgumentCount, Is.Zero);
                Assert.That(state.ProgramCounter, Is.Zero);
                Assert.That(state.Result, Is.EqualTo(DreamValue.Null));
                Assert.That(state.WaitFor, Is.True);
                Assert.That(state.Enumerators, Is.All.Null);
            }
            TestContext.Progress.WriteLine($"Spawn pool: {initialPoolCount} initial, {DMProcState.Pool.Count} after 10000 completed spawns");
        } finally {
            payload.DecRef();
        }
    }

    /// <summary>
    /// Tests to make sure the IntegrationTest project does not runtime.
    /// </summary>
    [Test]
    public async Task NoRuntimesTest() {
        var (client, server) = await StartConnectedServerClientPair();
        await RunTicksSync(client, server, 1000);
        Assert.That(server.IsAlive);
        var manager = server.ResolveDependency<DreamManager>();
        if(manager.LastDMException is not null) {
            Assert.Fail($"Runtime occurred on server boot: {manager.LastDMException}");
        }
        // Reuse the existing server: BYOND API initialization is process-global.
        await server.WaitAssertion(() => {
            var tree = server.ResolveDependency<DreamObjectTree>();
            CompletedSpawnsReuseStates(manager, tree);
            CancelledThreadsReleaseOwnedFrames(manager, tree);
            var refs = server.ResolveDependency<DreamRefManager>();
            ObjectInitializationReleasesOwnedValues(manager, tree, refs);
            MissingOperatorArgumentsReleaseOwners(manager, tree, refs);
            SupportedOperatorFallbacksReleaseOwners(manager, tree, refs);
            ExternalCallRejectionsReleaseArguments(manager, tree, refs);
            NumericListAssignmentsReleaseOwners(manager, tree, refs);
            FieldReferencesReleaseOwners(tree, refs);
        });
    }
}
