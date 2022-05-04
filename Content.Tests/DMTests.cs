using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;
using Robust.Shared.Asynchronous;
using Robust.Shared.IoC;

namespace Content.Tests
{
    [TestFixture]
    public sealed class DMTests : ContentUnitTest
    {
        private IDreamManager _dreamMan;
        private ITaskManager _taskManager;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _taskManager = IoCManager.Resolve<ITaskManager>();
            _taskManager.Initialize();
            _dreamMan = IoCManager.Resolve<IDreamManager>();
            _dreamMan.Initialize(SetupCompileDm.CompiledProject);
        }

        [Test]
        public void SyncReturn()
        {
            var prev = _dreamMan.DMExceptionCount;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            var result = DreamThread.Run(async (state) => {
                return new DreamValue(1337);
            });
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

            Assert.That(result, Is.EqualTo(new DreamValue(1337)));
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        [Test]
        public void SyncReturnInAsync() {
            var prev = _dreamMan.DMExceptionCount;
            var sync_result = DreamThread.Run(async(state) => {
                state.Result = new DreamValue(420);
                await Task.Yield();
                return new DreamValue(1337);
            });

            Assert.That(sync_result, Is.EqualTo(new DreamValue(420)));
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        [Test]
        public void SyncCall() {
            var prev = _dreamMan.DMExceptionCount;
            var sync_result = DreamThread.Run(async(state) =>
            {
                var root = _dreamMan.WorldInstance;
                var proc = root.GetProc("sync_test");
                return await state.Call(proc, null, null, new DreamProcArguments(null));
            });

            Assert.That(sync_result, Is.EqualTo(new DreamValue(1992)));
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        [Test]
        public void Error() {
            var prev = _dreamMan.DMExceptionCount;

            var sync_result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("error_test");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(sync_result, Is.EqualTo(new DreamValue(1)));
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev + 1));
        }

        [Test]
        public void SyncImage() {
            var prev = _dreamMan.DMExceptionCount;

            var sync_result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("image_test");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            var obj = sync_result.GetValueAsDreamObject();
            Assert.IsNotNull(obj);

            var imageDefinition = _dreamMan.ObjectTree.GetObjectDefinition(DreamPath.Image);
            Assert.That(obj.ObjectDefinition, Is.EqualTo(imageDefinition));
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        /*[Test, Timeout(10000)]
        public void AsyncCall() {

            var result = DreamValue.Null;

            DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("async_test");
                result = await state.Call(proc, world, null, new DreamProcArguments(null));
                state.Runtime.Shutdown = true;
                return DreamValue.Null;
            });

            runtime.Run();

            Assert.AreEqual(new DreamValue(1337), result);
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }*/

        [Test]
        public void CrashPropagation() {
            var prev = _dreamMan.DMExceptionCount;

            var sync_result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("crash_test");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(sync_result, Is.EqualTo(new DreamValue(1)));
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev + 1));
        }

        [Test]
        public void StackOverflow() {
            var prev = _dreamMan.DMExceptionCount;

            var sync_result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("stack_overflow_test");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(sync_result, Is.EqualTo(new DreamValue(1)));
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev + 1));
        }

        /*[Test, Timeout(10000)]
        public void WaitFor() {

            DreamValue result_1 = DreamValue.Null;
            DreamValue result_2 = DreamValue.Null;

            DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc_1 = world.GetProc("waitfor_1_a");
                result_1 = await state.Call(proc_1, world, null, new DreamProcArguments(null));

                var proc_2 = world.GetProc("waitfor_2_a");
                result_2 = await state.Call(proc_2, world, null, new DreamProcArguments(null));

                state.Runtime.Shutdown = true;
                return DreamValue.Null;
            });

            runtime.Run();

            Assert.AreEqual(new DreamValue(3), result_1);
            Assert.AreEqual(new DreamValue(2), result_2);
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }*/


        [Test]
        public void Default() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("default_test");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            var obj = result.GetValueAsDreamObjectOfType(DreamPath.Datum);
            Assert.IsNotNull(obj);
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        [Test]
        public void ValueInList() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("value_in_list");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.AreEqual(new DreamValue(1), result);
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }


        [Test]
        public void CallTest()
        {
            var prev = _dreamMan.DMExceptionCount;
            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("call_test");
                var res= await state.Call(proc, world, null, new DreamProcArguments(null));
                return res;
            });

            //var result = DreamThread.Run(_dreamMan.WorldInstance.GetProc("call_test"), _dreamMan.WorldInstance, null,
            //    new DreamProcArguments(null));

            Assert.That(result, Is.EqualTo(new DreamValue(13)));
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        [Test]
        public void SuperCallTest() {
            var prev = _dreamMan.DMExceptionCount;
            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("super_call");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(result, Is.EqualTo(new DreamValue(127)));
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        [Test]
        public void ConditionalAccessTest() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("conditional_access_test");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(result, Is.EqualTo(new DreamValue(1)));
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        [Test]
        public void DrConditionalAccessErrorTest()
        {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("conditional_access_test_error");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev + 1));
        }

        //TODO Failing test
        /*[Test]
        public void ConditionalCallTest() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("conditional_call_test");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(result, Is.EqualTo(DreamValue.Null));
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }*/

        [Test]
        public void ConditionalCallErrorTest()
        {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("conditional_call_test_error");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev + 1));
        }

        [Test]
        public void ConditionalMutateTest() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("conditional_mutate");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(result, Is.EqualTo(new DreamValue(4)));
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

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
        public void ClampValueTest() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async (state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("clamp_value");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
            Assert.That(result, Is.EqualTo(new DreamValue(1)));
        }

        [Test]
        public void Md5Test() {
            var prev = _dreamMan.DMExceptionCount;

            var result = DreamThread.Run(async (state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("md5_test");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
            Assert.That(result, Is.EqualTo(new DreamValue("c74318b61a3024520c466f828c043c79")));
        }

        [Test]
        public void ForLoopsTest()
        {
            var prev = _dreamMan.DMExceptionCount;
            var result = DreamThread.Run(async state =>
            {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("for_loops_test");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
            var resultList = result.GetValueAsDreamList();
            foreach(var value in resultList.GetValues())
            {
                Assert.That(value.GetValueAsInteger(), Is.EqualTo(3));
            }
        }

        [Test]
        public void MatrixOperationsTest()
        {
            var prev = _dreamMan.DMExceptionCount;
            DreamThread.Run(async state =>
            {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("matrix_operations_test");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        [Test]
        public void UnicodeProcsTest()
        {
            var prev = _dreamMan.DMExceptionCount;
            DreamThread.Run(async state =>
            {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("unicode_procs_test");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        //TODO Failing test
        /*[TestCase("Hello, World!", ", ", -1, 1)]
        [TestCase("Hello, World!", ", ", 3, 3)]
        [TestCase("Hello, World!", ", ", 7, 0)]
        [TestCase("Hello, World!", ", ", 14, 0)]
        [TestCase("Hello, World!", ", ", 0, 0)]
        public void NonspantextTest(string haystack, string needles, int start, int valueResult)
        {
            var prev = _dreamMan.DMExceptionCount;

            var haystackDreamValue = new DreamValue(haystack);
            var needlesDreamValue = new DreamValue(needles);
            var startDreamValue = new DreamValue(start);
            var valueResultDreamValue = new DreamValue(valueResult);
            var listDreamValue = new List<DreamValue>() { haystackDreamValue, needlesDreamValue, startDreamValue };
            var result = DreamThread.Run(async state =>
            {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("nonspantext");
                return await state.Call(proc, world, null, new DreamProcArguments(listDreamValue));
            });
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
            Assert.That(result, Is.EqualTo(valueResultDreamValue));
        }*/

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

        [Test]
        public void AssertPass() {
            var prev = _dreamMan.DMExceptionCount;

            var sync_result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("assert_test_pass");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(sync_result, Is.EqualTo(new DreamValue(1)));
            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev));
        }

        [Test]
        public void AssertFail() {
            var prev = _dreamMan.DMExceptionCount;

            var sync_result = DreamThread.Run(async(state) => {
                var world = _dreamMan.WorldInstance;
                var proc = world.GetProc("assert_test_fail");
                return await state.Call(proc, world, null, new DreamProcArguments(null));
            });

            Assert.That(_dreamMan.DMExceptionCount, Is.EqualTo(prev + 1));
        }

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
