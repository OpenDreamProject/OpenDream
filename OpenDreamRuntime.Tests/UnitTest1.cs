using NUnit.Framework;
using OpenDreamShared.Net.Packets;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace OpenDreamServer.Tests
{
	class TestConnection : DreamConnection {
		public TestConnection(DreamRuntime runtime)
			: base(runtime)
		{}

		public override byte[] ReadPacketData()
		{
			throw new System.NotImplementedException();
		}

		public override void SendPacket(IPacket packet)
		{
			throw new System.NotImplementedException();
		}
	}

	class TestServer : DreamServer
	{
		public TestServer() {

		}

        public override event DreamConnectionReadyEventHandler DreamConnectionRequest;

        public override void Start(DreamRuntime runtime)
		{

		}

		public override void Process()
		{
			
		}
	}

	public class Tests {
		[SetUp]
		public void Compile() {
			// Terrible platform-specific way to build our test dependencies
			var info = new ProcessStartInfo {
				FileName = "DMCompiler.exe",
				Arguments = "DMProject\\environment.dme",
				RedirectStandardInput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			};

			var process = new Process { StartInfo = info };
			process.Start();
			process.WaitForExit();

			Assert.AreEqual(process.ExitCode, 0);
		}

		[Test]
		public void SyncReturn()
		{
			var x = Directory.GetCurrentDirectory();
			var runtime = new DreamRuntime(new TestServer(), "DMProject\\environment.json");

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
			var result = DreamThread.Run(runtime, async (state) => {
				return new DreamValue(1337);
			});
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

			Assert.AreEqual(result, new DreamValue(1337));
		}

		[Test]
		public void SyncReturnInAsync()
		{
			var runtime = new DreamRuntime(new TestServer(), "DMProject\\environment.json");

			var sync_result = DreamThread.Run(runtime, async(state) => {
				state.Result = new DreamValue(420);
				await Task.Yield();
				return new DreamValue(1337);
			});

			Assert.AreEqual(sync_result, new DreamValue(420));
		}
	}
}