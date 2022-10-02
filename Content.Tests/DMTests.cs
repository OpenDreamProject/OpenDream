using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using Robust.Shared.Asynchronous;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Tests
{
    [TestFixture]
    public sealed partial class DMTests : ContentUnitTest
    {
        public const string TestProject = "DMProject";
        public const string InitializeEnvironment = "./environment.dme";

        private IDreamManager _dreamMan;
        private ITaskManager _taskManager;

        [Flags]
        public enum DMTestFlags {
            NoError = 0,        // Should run without errors
            Ignore = 1,         // Ignore entirely
            CompileError = 2,   // Should fail to compile
            RuntimeError = 4,   // Should throw an exception at runtime
            ReturnTrue = 8      // Should return TRUE
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _taskManager = IoCManager.Resolve<ITaskManager>();
            _taskManager.Initialize();
            IComponentFactory componentFactory = IoCManager.Resolve<IComponentFactory>();
            componentFactory.RegisterClass<DMISpriteComponent>();
            componentFactory.GenerateNetIds();
            _dreamMan = IoCManager.Resolve<IDreamManager>();
            Compile(InitializeEnvironment);
            _dreamMan.Initialize(Path.ChangeExtension(InitializeEnvironment, "json"));
        }

        public string Compile(string sourceFile) {
            bool successfulCompile = DMCompiler.DMCompiler.Compile(new() {
                Files = new() { sourceFile },
                Silent = true
            });

            return successfulCompile ? Path.ChangeExtension(sourceFile, "json") : null;
        }

        public void Cleanup(string compiledFile) {
            if (!File.Exists(compiledFile))
                return;

            File.Delete(compiledFile);
        }

        [Test, TestCaseSource(nameof(GetTests))]
        public void TestFiles(string sourceFile, DMTestFlags testFlags)
        {
            string initialDirectory = Directory.GetCurrentDirectory();
            try {
                string compiledFile = Compile(Path.Join(initialDirectory, "Tests", sourceFile));
                if (testFlags.HasFlag(DMTestFlags.CompileError)) {
                    Assert.IsNull(compiledFile, $"Expected an error during DM compilation of '{sourceFile}'");
                    Cleanup(compiledFile);
                    return;
                }

                Assert.IsTrue(compiledFile is not null && File.Exists(compiledFile), $"Failed to compile DM source file '{sourceFile}'");
                Assert.IsTrue(_dreamMan.LoadJson(compiledFile), $"Failed to load {compiledFile}");

                (bool successfulRun, DreamValue returned, Exception? exception) = RunTest();
                if (testFlags.HasFlag(DMTestFlags.RuntimeError)) {
                    Assert.IsFalse(successfulRun, $"A DM runtime exception was expected in '{sourceFile}'");
                } else {
                    if (exception != null)
                        Assert.IsTrue(successfulRun, $"A DM runtime exception was thrown in '{sourceFile}': \"{exception.Message}\"");
                    else
                        Assert.IsTrue(successfulRun, $"A DM runtime exception was thrown in '{sourceFile}', and its message could not be recovered!");
                }

                if (testFlags.HasFlag(DMTestFlags.ReturnTrue)) {
                    returned.TryGetValueAsInteger(out int returnInt);
                    Assert.IsTrue(returnInt != 0, $"Test '{sourceFile}' was expected to return TRUE");
                }

                Cleanup(compiledFile);
            } finally {
                // Restore the original CurrentDirectory, since loading a compiled JSON changes it.
                Directory.SetCurrentDirectory(initialDirectory);
            }
        }

        [Test, TestCaseSource(nameof(GetBrokenTests))]
        public void TestBrokenFiles(string sourceFile, DMTestFlags testFlags)
        {
            string initialDirectory = Directory.GetCurrentDirectory();
            try {
                string compiledFile = Compile(Path.Join(initialDirectory, "Broken Tests", sourceFile));
                if (testFlags.HasFlag(DMTestFlags.CompileError)) {
                    Assert.IsNotNull(compiledFile, $"Broken test '{sourceFile}' appears to be fixed, please move it to 'Tests'");
                    Cleanup(compiledFile);
                    return;
                }

                if (compiledFile is null || !File.Exists(compiledFile) || !_dreamMan.LoadJson(compiledFile)) {
                    Cleanup(compiledFile);
                    return;
                }

                (bool successfulRun, DreamValue returned, Exception? exception) = RunTest();
                if (testFlags.HasFlag(DMTestFlags.RuntimeError)) {
                    Assert.IsTrue(successfulRun, $"Broken test '{sourceFile}' appears to be fixed, please move it to 'Tests'");
                } else {
                    if (exception != null)
                        Assert.IsFalse(successfulRun, $"Broken test '{sourceFile}' appears to be fixed, please move it to 'Tests'");
                    else
                        Assert.IsFalse(successfulRun, $"Broken test '{sourceFile}' appears to be fixed, please move it to 'Tests'");
                }

                if (testFlags.HasFlag(DMTestFlags.ReturnTrue)) {
                    returned.TryGetValueAsInteger(out int returnInt);
                    Assert.IsTrue(returnInt == 0, $"Broken test '{sourceFile}' appears to be fixed, please move it to 'Tests'");
                }

                Cleanup(compiledFile);
            } finally {
                // Restore the original CurrentDirectory, since loading a compiled JSON changes it.
                Directory.SetCurrentDirectory(initialDirectory);
            }
        }

        private (bool Success, DreamValue Returned, Exception? except) RunTest() {
            var prev = _dreamMan.LastDMException;

            var result = DreamThread.Run(async (state) => {
                if (_dreamMan.ObjectTree.TryGetGlobalProc("RunTest", out DreamProc proc)) {
                    return await state.Call(proc, null, null, new DreamProcArguments(null));
                } else {
                    Assert.Fail($"No global proc named RunTest");
                    return DreamValue.Null;
                }
            });
            bool retSuccess = _dreamMan.LastDMException == prev; // Works because "null == null" is true in this language.
            if (retSuccess)
                return (retSuccess, result, null);
            else
                return (false, result, _dreamMan.LastDMException);
        }

        private static IEnumerable<object[]> GetTests()
        {
            if (!Directory.GetCurrentDirectory().EndsWith(TestProject)) {
                Directory.SetCurrentDirectory(TestProject);
            }

            foreach (string sourceFile in Directory.GetFiles("Tests", "*.dm", SearchOption.AllDirectories)) {
                string sourceFile2 = sourceFile.Substring("Tests/".Length);
                DMTestFlags testFlags = GetDMTestFlags(sourceFile);
                if (testFlags.HasFlag(DMTestFlags.Ignore))
                    continue;

                yield return new object[] {
                    sourceFile2,
                    testFlags
                };
            }
        }

        private static IEnumerable<object[]> GetBrokenTests() {
            if (!Directory.GetCurrentDirectory().EndsWith(TestProject)) {
                Directory.SetCurrentDirectory(TestProject);
            }

            foreach (string sourceFile in Directory.GetFiles("Broken Tests", "*.dm", SearchOption.AllDirectories)) {
                string sourceFile2 = sourceFile.Substring("Broken Tests/".Length);
                DMTestFlags testFlags = GetDMTestFlags(sourceFile);
                if (testFlags.HasFlag(DMTestFlags.Ignore))
                    continue;

                yield return new object[] {
                    sourceFile2,
                    testFlags
                };
            }
        }

        private static DMTestFlags GetDMTestFlags(string sourceFile) {
            DMTestFlags testFlags = DMTestFlags.NoError;

            using (StreamReader reader = new StreamReader(sourceFile)) {
                string firstLine = reader.ReadLine();

                if (firstLine.Contains("IGNORE", StringComparison.InvariantCulture))
                    testFlags |= DMTestFlags.Ignore;
                if (firstLine.Contains("COMPILE ERROR", StringComparison.InvariantCulture))
                    testFlags |= DMTestFlags.CompileError;
                if (firstLine.Contains("RUNTIME ERROR", StringComparison.InvariantCulture))
                    testFlags |= DMTestFlags.RuntimeError;
                if (firstLine.Contains("RETURN TRUE", StringComparison.InvariantCulture))
                    testFlags |= DMTestFlags.ReturnTrue;
            }

            return testFlags;
        }

        // TODO Convert the below async tests

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
    }
}
