using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Procs;

namespace Content.Tests
{
    [TestFixture]
    public partial class DMTests
    {
        [Test]
        public void ConstSwitch() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("ConstSwitch_1");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.AreEqual(new DreamValue(1), result);
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        [Test]
        public void ConstDivZero() {
            var prev = _dreamMan.DMExceptionCount;

            // Case 1
            var result1 = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("ConstZero1");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev + 1));

            //Case 2
            //TODO Failed test
            /*prev = _dreamMan.DMExceptionCount;
            var result2 = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("ConstZero2");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev + 1));*/
        }

        //TODO Failing test
        /*[Test]
        public void SwitchConstTest() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("switch_const");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(result, Is.EqualTo(new DreamValue(1)));
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(0));
        }*/

        [Test]
        public void ConstList() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("ConstList1");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.AreEqual(new DreamValue(5), result);
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        //TODO Failing test. DM code doesn't compile.
        /*[Test]
        public void ConstProc() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("ConstProc1");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.AreEqual(new DreamValue(1), result);
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }*/

        [Test]
        public void ConstInit() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("ConstInit1");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.AreEqual(new DreamValue(5), result);
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        //TODO Failing test. DM code doesn't compile.
        /*[Test]
        public void ConstSort() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("ConstSort1");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.AreEqual(new DreamValue(1), result);
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }*/
    }
}
