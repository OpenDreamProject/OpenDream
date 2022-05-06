using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Procs;

namespace Content.Tests
{
    [TestFixture]
    public partial class DMTests
    {
        [Test]
        public void ListIndexMutateTest()
        {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("list_index_mutate");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(result, Is.EqualTo(new DreamValue(30)));
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        // TODO Failed test
        /*[Test]
        public void ListNullArg1() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("ListNullArg1_Proc");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev + 1));
        }*/

        // TODO Failed test - DM compiler error. Fixed in PR #659
        /*[Test]
        public void ListNullArg2() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("ListNullArg2_Proc");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev + 1));
        }*/

        [Test]
        public void ListNullObj1() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("ListNullObj1_proc");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        // TODO Failed test - DM compiler error. Fixed in PR #659
        /*[Test]
        public void ListNullObj2() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("ListNullObj2_proc");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }*/

        [Test]
        public void ListNullProc1() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("ListNullProc1_proc");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        // TODO Failed test - DM compiler error. Fixed in PR #659
        /*[Test]
        public void ListNullProc2() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("ListNullProc2_proc");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }*/
    }
}
