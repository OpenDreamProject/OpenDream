using NUnit.Framework;
using OpenDreamShared.Net.Packets;
using OpenDreamVM;
using OpenDreamVM.Objects;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

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
		[Test]
		public void SyncReturn()
		{
			var runtime = new DreamRuntime(new TestServer(), "E:\\OpenDream\\TestGame\\environment.json");

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
			var runtime = new DreamRuntime(new TestServer(), "E:\\OpenDream\\TestGame\\environment.json");

			var sync_result = DreamThread.Run(runtime, async(state) => {
				state.Result = new DreamValue(420);
				await Task.Yield();
				return new DreamValue(1337);
			});

			Assert.AreEqual(sync_result, new DreamValue(420));
		}
	}
}