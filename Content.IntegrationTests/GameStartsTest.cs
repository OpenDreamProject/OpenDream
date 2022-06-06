using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.IntegrationTests {
    [TestFixture]
    public sealed class GameStartsTest : ContentIntegrationTest {
        /// <summary>
        ///     A simple test that starts a server and client and makes sure they connected successfully.
        /// </summary>
        /// <remarks>RobustToolbox has its own integration test for this. This is an example test, delete it sometime.</remarks>
        [Test]
        public async Task ServerClientConnectionTest() {
            var (client, server) = await StartConnectedServerClientPair();

            await client.WaitAssertion(() => {
                Assert.That(IoCManager.Resolve<IClientNetManager>().IsConnected);
            });

            await server.WaitAssertion(() => {
                Assert.That(IoCManager.Resolve<IPlayerManager>().PlayerCount, Is.EqualTo(1));
            });
        }
    }
}
