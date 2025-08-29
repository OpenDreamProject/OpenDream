using System;
using System.IO;
using NUnit.Framework;

// This it outside of any namespace so it affects the whole assembly.
[SetUpFixture]
// ReSharper disable once CheckNamespace
public sealed class SetupCompileDm {
    public const string TestProject = "DMProject";
    public const string TestDirectory = "Tests";
    public const string BaseEnvironment = "environment.dme";
    public const string Environment = "test_environment.dme";

    public static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string BaseDmEnvironment = Path.Join(BaseDirectory, TestProject, BaseEnvironment);
    public static readonly string DmEnvironment = Path.Join(BaseDirectory, TestProject, Environment);
    public static readonly string TestFilesDirectory = Path.Join(BaseDirectory, TestProject, TestDirectory);
    public static readonly string CompiledProject = Path.ChangeExtension(DmEnvironment, "json");

    [OneTimeSetUp]
    public void Compile() {
        DMCompiler.DMCompiler compiler = new();
        // open the DME and append include statements for each test
        var environmentText = File.ReadAllText(BaseDmEnvironment);
        foreach (var file in Directory.EnumerateFiles(TestFilesDirectory, "*.dm")) {
            environmentText += $"\n#include \"{file}\"";
        }
        File.WriteAllText(DmEnvironment, environmentText);

        bool successfulCompile = compiler.Compile(new() {
            Files = new() { DmEnvironment }
        });
        Assert.That(successfulCompile && File.Exists(CompiledProject), "Failed to compile DM test project!");
    }

    [OneTimeTearDown]
    public void Cleanup() {
        if (!File.Exists(CompiledProject))
            return;

        if (File.Exists(DmEnvironment))
            File.Delete(DmEnvironment);

        File.Delete(CompiledProject);
    }
}
