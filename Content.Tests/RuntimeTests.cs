using System;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;
using NUnit.Framework;

namespace Content.Tests;

[TestFixture]
public sealed class RuntimeTests {
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
}
