using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DMCompiler.Compiler;
using NUnit.Framework;
using OpenDreamRuntime;
using OpenDreamRuntime.Objects;
using Robust.Shared.Asynchronous;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Tests;

[TestFixture]
public sealed partial class DMTests : ContentUnitTest {
    private const string TestProject = "DMProject";
    private const string InitializeEnvironment = "./environment.dme";
    private const string TestsDirectory = "Tests";

    [Dependency] private readonly DreamManager _dreamMan = default!;
    [Dependency] private readonly DreamObjectTree _objectTree = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;

    [Flags]
    public enum DMTestFlags {
        NoError = 0,        // Should run without errors
        Ignore = 1,         // Ignore entirely
        CompileError = 2,   // Should fail to compile
        RuntimeError = 4,   // Should throw an exception at runtime
        ReturnTrue = 8,     // Should return TRUE
        NoReturn = 16,      // Shouldn't return (aka stopped by a stack-overflow or runtimes)
    }

    private void OnException(object? sender, Exception exception) => TestContext.Out.WriteLine(exception);

    [OneTimeSetUp]
    public void OneTimeSetup() {
        var dmCompiler = new DMCompiler.DMCompiler();
        IoCManager.InjectDependencies(this);
        _taskManager.Initialize();
        Compile(dmCompiler, InitializeEnvironment);
        _dreamMan.PreInitialize(Path.ChangeExtension(InitializeEnvironment, "json"));
        _dreamMan.OnException += OnException;
    }

    private string? Compile(DMCompiler.DMCompiler compiler, string sourceFile) {
        bool successfulCompile = compiler.Compile(new() {
            Files = [sourceFile]
        });

        return successfulCompile ? Path.ChangeExtension(sourceFile, "json") : null;
    }

    private static void Cleanup(string? compiledFile) {
        if (!File.Exists(compiledFile))
            return;

        File.Delete(compiledFile);
    }

    [Test, TestCaseSource(nameof(GetTests))]
    public void TestFiles(string sourceFile, DMTestFlags testFlags, int errorCode) {
        string initialDirectory = Directory.GetCurrentDirectory();
        TestContext.Out.WriteLine($"--- TEST {sourceFile} | Flags: {testFlags}");
        try {
            var dmCompiler = new DMCompiler.DMCompiler();
            var compiledFile = Compile(dmCompiler, Path.Join(initialDirectory, TestsDirectory, sourceFile));
            if (testFlags.HasFlag(DMTestFlags.CompileError)) {
                Assert.That(errorCode == -1, Is.False, "Expected an error code");
                Assert.That(dmCompiler.UniqueEmissions.Contains((WarningCode)errorCode), Is.True, $"Expected error code \"{errorCode}\" was not found");
                Assert.That(compiledFile, Is.Null, "Expected an error during DM compilation");

                Cleanup(compiledFile);
                TestContext.Out.WriteLine($"--- PASS {sourceFile}");
                return;
            }

            Assert.That(compiledFile is not null && File.Exists(compiledFile), "Failed to compile DM source file");
            Assert.That(_dreamMan.LoadJson(compiledFile), $"Failed to load {compiledFile}");
            _dreamMan.LastDMException = null; // Nuke any exception from a prior test
            _dreamMan.StartWorld();

            (bool successfulRun, DreamValue? returned, Exception? exception) = RunTest();

            if (testFlags.HasFlag(DMTestFlags.NoReturn)) {
                Assert.That(returned.HasValue, Is.False, "proc returned unexpectedly");
            } else {
                Assert.That(returned.HasValue, "proc did not return (did it hit an exception?)");
            }

            if (testFlags.HasFlag(DMTestFlags.RuntimeError)) {
                Assert.That(successfulRun, Is.False, "A DM runtime exception was expected");
            } else {
                if (exception != null)
                    Assert.That(successfulRun, $"A DM runtime exception was thrown: \"{exception}\"");
                else
                    Assert.That(successfulRun, "A DM runtime exception was thrown, and its message could not be recovered!");
            }

            if (testFlags.HasFlag(DMTestFlags.ReturnTrue)) {
                Assert.That(returned?.IsTruthy(), Is.True, "Test was expected to return TRUE");
            }

            // Due to how softdels work we need to make sure we clean up /now/.
            GC.Collect();
            _dreamMan.Update();
            Cleanup(compiledFile);
            TestContext.Out.WriteLine($"--- PASS {sourceFile}");
        } finally {
            // Restore the original CurrentDirectory, since loading a compiled JSON changes it.
            Directory.SetCurrentDirectory(initialDirectory);
        }
    }

    private (bool Success, DreamValue? Returned, Exception? except) RunTest() {
        DreamValue? retValue = null;
        Task<DreamValue> callTask = null!;

        DreamThread.Run("RunTest", async (state) => {
            if (_objectTree.TryGetGlobalProc("RunTest", out DreamProc? proc)) {
                callTask = state.Call(proc, null, null);
                retValue = await callTask;
                return DreamValue.Null;
            } else {
                Assert.Fail("No global proc named RunTest");
                return DreamValue.Null;
            }
        });

        var watch = new Stopwatch();
        watch.Start();

        // Tick until our inner call has finished
        while (!callTask.IsCompleted) {
            _dreamMan.Update();
            _taskManager.ProcessPendingTasks();

            if (watch.Elapsed.TotalMilliseconds > 500) {
                Assert.Fail("Test timed out");
            }
        }

        bool retSuccess = _dreamMan.LastDMException == null; // Works because "null == null" is true in this language.
        return (retSuccess, retValue, _dreamMan.LastDMException);
    }

    private static IEnumerable<object[]> GetTests()
    {
        Directory.SetCurrentDirectory(TestProject);

        foreach (string sourceFile in Directory.GetFiles(TestsDirectory, "*.dm", SearchOption.AllDirectories)) {
            string sourceFile2 = sourceFile[$"{TestsDirectory}/".Length..];
            DMTestFlags testFlags = GetDMTestFlags(sourceFile, out var errorCode);
            if (testFlags.HasFlag(DMTestFlags.Ignore))
                continue;

            yield return new object[] {
                sourceFile2,
                testFlags,
                errorCode
            };
        }
    }

    private static DMTestFlags GetDMTestFlags(string sourceFile, out int errorCode) {
        DMTestFlags testFlags = DMTestFlags.NoError;
        errorCode = -1; // If it's null GetTests() fusses about a NRE

        using StreamReader reader = new StreamReader(sourceFile);
        string? firstLine = reader.ReadLine();
        if (firstLine == null)
            return testFlags;
        if (firstLine.Contains("IGNORE", StringComparison.InvariantCulture))
            testFlags |= DMTestFlags.Ignore;
        if (firstLine.Contains("COMPILE ERROR", StringComparison.InvariantCulture)) {
            testFlags |= DMTestFlags.CompileError;

            Match match = ErrorCodeRegex().Match(firstLine);  // "OD" followed by exactly 4 numbers
            if (match.Success) {
                errorCode = int.Parse(match.Groups[1].Value);
            }
        }

        if (firstLine.Contains("RUNTIME ERROR", StringComparison.InvariantCulture))
            testFlags |= DMTestFlags.RuntimeError;
        if (firstLine.Contains("RETURN TRUE", StringComparison.InvariantCulture))
            testFlags |= DMTestFlags.ReturnTrue;
        if (firstLine.Contains("NO RETURN", StringComparison.InvariantCulture))
            testFlags |= DMTestFlags.NoReturn;

        return testFlags;
    }

    [GeneratedRegex(@"OD([0-9]{4})")]
    private static partial Regex ErrorCodeRegex();

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
