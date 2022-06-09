using System;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Internal;

// This is outside of any namespace so it affects the whole assembly.
[SetUpFixture]
// ReSharper disable once CheckNamespace
public sealed class SetupCompileDm {
    public const string TestProject = "DMProject";
    public const string Environment = "environment.dme";

    public static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string DmEnvironment = Path.Join(BaseDirectory, TestProject, Environment);
    public static readonly string CompiledProject = Path.ChangeExtension(DmEnvironment, "json");

    [OneTimeSetUp]
    public void Compile() {

        // TODO Auto-generate the DME based on the files in DMProject/Shared and DMProject/Tests

        bool successfulCompile = DMCompiler.DMCompiler.Compile(new() {
            Files = new() { DmEnvironment }
        });

        Assert.That(successfulCompile, Is.True);
        Assert.That(File.Exists(CompiledProject), Is.True, "Failed to compile DM test project!");
    }

    [OneTimeTearDown]
    public void Cleanup() {
        if (!File.Exists(CompiledProject))
            return;

        File.Delete(CompiledProject);
    }
}

