using System.Threading.Tasks;
using NUnit.Framework;
using OpenDreamRuntime;

namespace Content.IntegrationTests;
[TestFixture]
public sealed class GameTests : ContentIntegrationTest {
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
    }
}
