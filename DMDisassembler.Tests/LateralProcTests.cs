using System.Diagnostics;
using NUnit.Framework;

namespace DMDisassembler.Tests;

[TestFixture]
public sealed class LateralProcTests {
    [Test]
    public async Task DecompileDisplaysEveryDefinitionInSourceOrder() {
        string sourceFile = Path.Combine(AppContext.BaseDirectory, "lateral_procs.dm");
        string jsonFile = Path.ChangeExtension(sourceFile, ".json");

        try {
            var compiler = new global::DMCompiler.DMCompiler();
            Assert.That(compiler.Compile(new() {
                Files = [sourceFile],
                NoStandard = true
            }), Is.True);

            string disassembler = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "..", "DMDisassembler", "DMDisassembler.dll"));
            Assert.That(File.Exists(disassembler), Is.True, $"Could not find {disassembler}");

            var startInfo = new ProcessStartInfo {
                FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
                WorkingDirectory = AppContext.BaseDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add(disassembler);
            startInfo.ArgumentList.Add(jsonFile);

            using var process = new Process { StartInfo = startInfo };
            Assert.That(process.Start(), Is.True);

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            await process.StandardInput.WriteLineAsync("select /datum/disassembler_test");
            await process.StandardInput.WriteLineAsync("decompile target");
            await process.StandardInput.WriteLineAsync("quit");
            process.StandardInput.Close();

            try {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await process.WaitForExitAsync(timeout.Token);
            } finally {
                if (!process.HasExited) {
                    process.Kill(entireProcessTree: true);
                }
            }

            string output = await outputTask;
            string error = await errorTask;
            Assert.That(process.ExitCode, Is.Zero, error);

            const string notice = "Notice: Found 3 definitions of target(); decompiling all in source order.";
            string[] definitions = output.Split(Environment.NewLine)
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("ReturnFloat", StringComparison.Ordinal))
                .ToArray();

            Assert.That(output, Does.Contain(notice));
            Assert.That(definitions, Is.EqualTo(new[] {
                "ReturnFloat 1",
                "ReturnFloat 2",
                "ReturnFloat 3"
            }), output);
        } finally {
            File.Delete(jsonFile);
        }
    }
}
