using System;
using System.Collections.Generic;
using System.IO;

namespace DMCompiler {
    class Program {
        static void Main(string[] args) {
            if (!TryParseArguments(args, out DMCompilerSettings settings)) return;

            if (!DMCompiler.Compile(settings)) {
                //Compile errors, exit with an error code
                Environment.Exit(1);
            }
        }

        private static bool TryParseArguments(string[] args, out DMCompilerSettings settings) {
            settings = new();
            settings.Files = new List<string>();

            foreach (string arg in args) {
                switch (arg) {
                    case "--suppress-unimplemented": settings.SuppressUnimplementedWarnings = true; break;
                    case "--dump-preprocessor": settings.DumpPreprocessor = true; break;
                    case "--no-standard": settings.NoStandard = true; break;
                    case "--verbose": settings.Verbose = true; break;
                    default: {
                        string extension = Path.GetExtension(arg);

                        if (!String.IsNullOrEmpty(extension) && (extension == ".dme" || extension == ".dm")) {
                            settings.Files.Add(arg);
                            Console.WriteLine($"Compiling {Path.GetFileName(arg)}");
                        } else {
                            Console.WriteLine($"Invalid arg '{arg}'");
                            return false;
                        }

                        break;
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
