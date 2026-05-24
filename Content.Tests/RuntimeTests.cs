using System;
using System.IO;
using System.Linq;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;
using DMCompiler.Json;
using NUnit.Framework;

namespace Content.Tests;

[TestFixture]
public sealed class RuntimeTests {
    private const string SpecialTestsDirectory = "SpecialTests";

    /// <summary>
    /// Validates that the opcodes in DreamProcOpcode are all unique, such that none resolve to the same byte.
    /// </summary>
    [Test]
    public void EnsureOpcodesUnique() {
        Assert.That(Enum.GetValues<DreamProcOpcode>(), Is.Unique);
    }

    /// <summary>
    /// Ensures that every pragma code has a default set
    /// </summary>
    [Test]
    public void EnsurePragmaDefaults() {
        foreach (WarningCode code in Enum.GetValues<WarningCode>()) {
            Assert.That(CompilerEmission.DefaultErrorConfig.ContainsKey(code),
                $"Warning #{(int)code:d4} '{code.ToString()}' was never declared as error, warning, notice, or disabled.");

            if ((int)code < 1000)
                Assert.That(CompilerEmission.DefaultErrorConfig[code] == ErrorLevel.Error,
                    $"Warning #{(int)code:d4} '{code.ToString()}' is in the range 0-999 and so must be an error.");
        }
    }

    /// <summary>
    /// The MaxVariableCount of a proc
    /// should reflect the max amount of variables that COULD be allocated at once.
    /// </summary>
    [Test]
    public void EnsureLocalCountIsValid() {
        const string testPath = "Procs/local_count.dme";
        const string testIdentifier = "Test___";

        var compiler = new DMCompiler.DMCompiler();
        compiler.Compile(new() {
            Files = [Path.Join(Directory.GetCurrentDirectory(), SpecialTestsDirectory, testPath)]
        }, out DreamCompiledJson? compiledDream);
        Assert.That(compiledDream, Is.Not.Null, "Environment failed to compile");

        var tests = compiledDream.Procs
            .Where((proc) => proc.Name.StartsWith(testIdentifier))
            .ToDictionary((proc) => proc.Name[testIdentifier.Length..]); // strip the identifier out

        using (Assert.EnterMultipleScope()) {
            Assert.That(tests["ProcWithNothing"].MaxVariableId, Is.Zero);
            Assert.That(tests["ProcWithNoScope"].MaxVariableId, Is.EqualTo(5));
            Assert.That(tests["ProcWithOnlyScope"].MaxVariableId, Is.EqualTo(5));
            Assert.That(tests["ProcWithFullOuterScope"].MaxVariableId, Is.EqualTo(3));
            Assert.That(tests["ProcWithFullInnerScope"].MaxVariableId, Is.EqualTo(4));
        }
    }
}
