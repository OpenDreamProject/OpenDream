using System;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Internal;

// This it outside of any namespace so it affects the whole assembly.
[SetUpFixture]
// ReSharper disable once CheckNamespace
public class SetupCompileDm {
    public const string TestProject = "DMProject";
    public const string Environment = "environment.dme";

    public static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string DmEnvironment = Path.Join(BaseDirectory, TestProject, Environment);
    public static readonly string CompiledProject = Path.ChangeExtension(DmEnvironment, "json");

    [OneTimeSetUp]
    public void Compile() {
        // TODO: Make this more sane.
        DMCompiler.Program.Main(new []{ DmEnvironment });

        if (!File.Exists(CompiledProject)) {
            Console.WriteLine("Failed to compile DM test project!");
            System.Environment.Exit(1);
        }
    }

    [OneTimeTearDown]
    public void Cleanup() {
        if (!File.Exists(CompiledProject))
            return;

        File.Delete(CompiledProject);
    }
}
