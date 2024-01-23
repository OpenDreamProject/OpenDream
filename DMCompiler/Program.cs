using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DMCompiler;

internal struct Argument {
    /// <summary> The text we found that's in the '--whatever' format. May be null if no such text was present.</summary>
    public string? Name;
    /// <summary> The value, either set in a '--whatever=whoever' format or just left by itself anonymously. May be null.</summary>
    public string? Value;
}

internal static class Program {
    private static void Main(string[] args) {
        if (!TryParseArguments(args, out DMCompilerSettings settings)) {
            Environment.Exit(1);
            return;
        }

        if (!DMCompiler.Compile(settings)) {
            //Compile errors, exit with an error code
            Environment.Exit(1);
        }
    }

    /// <summary> Helper for TryParseArguments(), to turn the arg array into something better-parsed.</summary>
    private static IEnumerable<Argument> StringArrayToArguments(string[] args) {
        List<Argument> retArgs = new(args.Length);
        for(var i = 0; i < args.Length;i+=1) {
            var firstString = args[i];
            if(string.IsNullOrWhiteSpace(firstString)) // Is this possible? I don't even know. (IsNullOrWhiteSpace also checks if the string is empty, btw)
                continue;
            if(!firstString.StartsWith("--")) { // If it's a value-only argument
                retArgs.Add(new Argument { Value = firstString });
                continue;
            }
            firstString = firstString.TrimStart('-');
            var split = firstString.Split('=');
            if(split.Length == 1) { // If it's a name-only argument
                if(firstString == "define" && i + 1 < args.Length) { // Weird snowflaking to make our define syntax work
                    i+=1;
                    if(!args[i].StartsWith("--")) { // To make the error make a schmidge more sense
                        retArgs.Add(new Argument {Name = firstString, Value = args[i] });
                    }
                }
                retArgs.Add(new Argument { Name = firstString });
                continue;
            }
            retArgs.Add(new Argument { Name = split[0], Value = split[1] });
        }

        return retArgs.AsEnumerable();
    }

    private static bool HasValidDMExtension(string filename) {
        var extension = Path.GetExtension(filename);
        return !string.IsNullOrEmpty(extension) && (extension == ".dme" || extension == ".dm");
    }

    private static void PrintHelp() {
        Console.WriteLine("DM Compiler for OpenDream");
        Console.WriteLine("For more information please visit https://github.com/OpenDreamProject/OpenDream/wiki");
        Console.WriteLine("Usage: ./DMCompiler [options] [file].dme\n");
        Console.WriteLine("Options and arguments:");
        Console.WriteLine("--help                    : Show this help");
        Console.WriteLine("--version [VER].[BUILD]   : Used to set the DM_VERSION and DM_BUILD macros");
        Console.WriteLine("--skip-bad-args           : Skip arguments the compiler doesn't recognize");
        Console.WriteLine("--suppress-unimplemented  : Do not warn about unimplemented proc and var uses");
        Console.WriteLine("--dump-preprocessor       : This saves the result of preprocessing (#include, #if, defines, etc) in a file called preprocessor_dump.dm beside the given DME file.");
        Console.WriteLine("--dump-bytecode           : This saves the result of the bytecode generation in a file called [file].dmc beside the given DME file.");
        Console.WriteLine("--no-standard             : This disables objects and procs that are usually built-into every DM program by not including DMStandard.dm.");
        Console.WriteLine("--define [KEY=VAL]        : Add extra defines to the compilation");
        Console.WriteLine("--verbose                 : Show verbose output during compile");
        Console.WriteLine("--notices-enabled         : Show notice output during compile");
        Console.WriteLine("--pragma-config [file].dm : Configure the error/warning/notice/ignore level of compiler messages");
    }

    private static bool TryParseArguments(string[] args, out DMCompilerSettings settings) {
        settings = new DMCompilerSettings {
            Files = new List<string>()
        };

        var skipBad = args.Contains("--skip-bad-args");

        foreach (Argument arg in StringArrayToArguments(args)) {
            switch (arg.Name) {
                case "help":
                    PrintHelp();
                    return false;
                case "suppress-unimplemented": settings.SuppressUnimplementedWarnings = true; break;
                case "dump-preprocessor": settings.DumpPreprocessor = true; break;
                case "dump-bytecode": settings.DumpBytecode = true; break;
                case "no-standard": settings.NoStandard = true; break;
                case "verbose": settings.Verbose = true; break;
                case "skip-bad-args": break;
                case "define":
                    var parts = arg.Value?.Split('=', 2); // Only split on the first = in case of stuff like "--define AAA=0==1"
                    if (parts is { Length: 0 }) {
                        Console.WriteLine("Compiler arg 'define' requires macro identifier for definition directive");
                        return false;
                    }
                    settings.MacroDefines ??= new Dictionary<string, string>();
                    settings.MacroDefines[parts[0]] = parts.Length > 1 ? parts[1] : "";
                    break;
                case "wall":
                case "notices-enabled":
                    settings.NoticesEnabled = true;
                    break;
                case "pragma-config": {
                    if(arg.Value is null || !HasValidDMExtension(arg.Value)) {
                        if(skipBad) {
                            DMCompiler.ForcedWarning($"Compiler arg 'pragma-config' requires filename of valid DM file, skipping");
                            continue;
                        }
                        Console.WriteLine("Compiler arg 'pragma-config' requires filename of valid DM file");
                        return false;
                    }
                    settings.PragmaFileOverride = arg.Value;
                    break;
                }
                case "version": {
                    if(arg.Value is null) {
                        if(skipBad) {
                            DMCompiler.ForcedWarning("Compiler arg 'version' requires a full BYOND build (e.g. --version=514.1584), skipping");
                            continue;
                        }
                        Console.WriteLine("Compiler arg 'version' requires a full BYOND build (e.g. --version=514.1584)");
                        return false;
                    }

                    var split = arg.Value.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length != 2 || !int.TryParse(split[0], out _) || !int.TryParse(split[1], out _)) { // We want to make sure that they *are* ints but the preprocessor takes strings
                        if(skipBad) {
                            DMCompiler.ForcedWarning("Compiler arg 'version' requires a full BYOND build (e.g. --version=514.1584), skipping");
                            continue;
                        }
                        Console.WriteLine("Compiler arg 'version' requires a full BYOND build (e.g. --version=514.1584)");
                        return false;
                    }

                    settings.DMVersion = split[0];
                    settings.DMBuild = split[1];
                    break;
                }
                case null: { // Value-only argument
                    if (arg.Value is null) // A completely empty argument? This should be a bug.
                        continue;
                    if (HasValidDMExtension(arg.Value)) {
                        settings.Files.Add(arg.Value);
                        break;
                    }
                    if (skipBad) {
                        DMCompiler.ForcedWarning($"Invalid compiler arg '{arg.Value}', skipping");
                    } else {
                        Console.WriteLine($"Invalid arg '{arg}'");
                        return false;
                    }

                    break;
                }
                default: {
                    if (skipBad) {
                        DMCompiler.ForcedWarning($"Unknown compiler arg '{arg.Name}', skipping");
                        break;
                    }

                    Console.WriteLine($"Unknown arg '{arg}'");
                    return false;
                }
            }
        }

        if (settings.Files.Count == 0) {
            PrintHelp();
            return false;
        }

        foreach(var file in settings.Files) {
            Console.WriteLine($"Compiling {Path.GetFileName(file)} on {settings.DMVersion}.{settings.DMBuild}");
        }

        return true;
    }
}
