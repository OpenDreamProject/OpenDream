using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Procs;

namespace Content.Tests
{
    [TestFixture]
    public partial class DMTests
    {
        //TODO Failing test.
        /*[Test]
        public void DerefTest1() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("DerefTest1_proc");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }*/

        //TODO Failing test.
        /*[Test]
        public void DerefTest2() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("DerefTest2_proc");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }*/

        [Test]
        public void DerefTest3() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("DerefTest3_proc");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }
    }
}
