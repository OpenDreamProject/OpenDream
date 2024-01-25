﻿using System;
using DMCompiler.Bytecode;
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
}
