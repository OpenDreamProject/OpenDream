using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using OpenDreamShared.Compiler;

namespace DMCompiler {

    struct Argument
    {
        /// <summary> The text we found that's in the '--whatever' format. May be null if no such text was present.</summary>
        public string? Name;
        /// <summary> The value, either set in a '--whatever=whoever' format or just left by itself anonymously. May be null.</summary>
        public string? Value;
    }



    class Program {
        static void Main(string[] args) {
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
                string firstString = args[i];
                if(firstString.Length == 0) // Is this possible? I don't even know
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
            string extension = Path.GetExtension(filename);
            return !String.IsNullOrEmpty(extension) && (extension == ".dme" || extension == ".dm");
        }

        private static bool TryParseArguments(string[] args, out DMCompilerSettings settings) {
            settings = new();
            settings.Files = new List<string>();


            bool skipBad = args.Contains("--skip-bad-args");

            foreach (Argument arg in StringArrayToArguments(args)) {
                switch (arg.Name) {
                    case "suppress-unimplemented": settings.SuppressUnimplementedWarnings = true; break;
                    case "dump-preprocessor": settings.DumpPreprocessor = true; break;
                    case "no-standard": settings.NoStandard = true; break;
                    case "verbose": settings.Verbose = true; break;
                    case "skip-bad-args": break;
                    case "define":
                        string[] parts = arg.Value.Split('=');
                        if(parts.Length == 0) {
                            Console.WriteLine("Compiler arg 'define' requires macro identifier for definition directive");
                            return false;
                        }
                        (settings.MacroDefines ??= new())[parts[0]] = parts.Length > 1 ? parts[1] : "";
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
                    case null: { // Value-only argument
                        if (arg.Value is null) // A completely empty argument? This should be a bug.
                            continue;
                        if (HasValidDMExtension(arg.Value)) {
                            settings.Files.Add(arg.Value);
                            Console.WriteLine($"Compiling {Path.GetFileName(arg.Value)}");
                            break;
                        }
                        if(skipBad) {
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
                        } else {
                            Console.WriteLine($"Unknown arg '{arg}'");
                            return false;
                        }
                    }
                }
            }

            if (settings.Files.Count == 0)
            {
                Console.WriteLine("At least one DME or DM file must be provided as an argument");
                return false;
            }

            return true;
        }
    }
}
