using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

// This it outside of any namespace so it affects the whole assembly.
[SetUpFixture]
// ReSharper disable once CheckNamespace
public sealed class SetupCompileDm {
    public const string TestProject = "DMProject";
    public const string Environment = "environment.dme";
    public const string TestFolder = "Tests";

    public static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string DmEnvironment = Path.Join(BaseDirectory, TestProject, Environment);
    public static readonly string TestFilesDirectory = Path.Join(BaseDirectory, TestProject, TestFolder);
    public static readonly string CompiledProject = Path.ChangeExtension(DmEnvironment, "json");

    [OneTimeSetUp]
    public void Compile() {
        DMCompiler.DMCompiler compiler = new();
        List<string> files = [DmEnvironment, .. Directory.EnumerateFiles(TestFilesDirectory, "*.dm")];
        bool successfulCompile = compiler.Compile(new() {
            Files = files,
            DumpPreprocessor = true
        });

        Assert.That(successfulCompile && File.Exists(CompiledProject), "Failed to compile DM test project!");
    }

    [OneTimeTearDown]
    public void Cleanup() {
        if (!File.Exists(CompiledProject))
            return;

        File.Delete(CompiledProject);
    }
}
