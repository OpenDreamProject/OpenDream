using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using DMCompiler.Compiler.DMM;
using DMCompiler.DM;
using DMCompiler.DM.Visitors;
using DMCompiler.SpacemanDmm;
using OpenDreamShared.Compiler;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Compiler.DMPreprocessor;
using OpenDreamShared.Json;

namespace DMCompiler {
    class Program {
        public static int _errorCount = 0;

        static int Main(string[] args)
        {
            if (ParseCommandLineArgs(args, out var parsedArgs) is { } exitCode)
                return exitCode;

            var file = parsedArgs.CompileFile;
            var sw = Stopwatch.StartNew();
            string interfaceFile;
            List<string> mapFiles = new();
            if (parsedArgs.OldParser)
            {
                DMPreprocessor preprocessor = Preprocess(file);
                if (parsedArgs.DumpPreprocesor)
                {
                    StringBuilder result = new();
                    foreach (Token t in preprocessor.GetResult()) {
                        result.Append(t.Text);
                    }

                    string output = Path.Join(
                        Path.GetDirectoryName(file) ?? AppDomain.CurrentDomain.BaseDirectory,
                        "preprocessor_dump.dm");

                    File.WriteAllText(output, result.ToString());
                    Console.WriteLine($"Preprocessor output dumped to {output}");
                }

                Compile(preprocessor.GetResult());

                interfaceFile = preprocessor.IncludedInterface;
                mapFiles.AddRange(preprocessor.IncludedMaps);
            }
            else
            {
                var result = ParseResult.Parse(GetFileList(file));

                var files = result.GetFileList();
                var diagnostics = result.GetDiagnostics();
                foreach (var diag in diagnostics)
                {
                    var loc = $"{files[diag.Location.File-1]}:{diag.Location.Line}:{diag.Location.Column}";
                    Console.WriteLine($"{diag.Severity}: {diag.Description}");
                    Console.WriteLine($"   @ {loc}");
                }

                AstConverter.ConvertTypesToObjectTree(result);

                var specialFiles = result.GetSpecialFiles();
                interfaceFile = specialFiles.Skins[0];
                mapFiles.AddRange(specialFiles.Maps);
            }

            // Parser-independent compile tasks.
            foreach (var dmObject in DMObjectTree.AllObjects.Values) {
                dmObject.CompileProcs();
            }

            DMObjectTree.CreateGlobalInitProc();

            var successfulCompile = _errorCount == 0;
            if (successfulCompile) {
                //Output file is the first file with the extension changed to .json
                var outputFile = Path.ChangeExtension(file, "json");
                var maps = ConvertMaps(mapFiles);

                SaveJson(maps, interfaceFile, outputFile);
            }

            var duration = sw.Elapsed;
            Console.WriteLine($"Total time: {duration:mm\\:ss}");

            if (!successfulCompile) {
                Console.WriteLine($"Compilation failed with {_errorCount} errors");

                //Compile errors, exit with an error code
                return 1;
            }

            return 0;
        }

        /// <returns>Null -> success, value -> return code to exit with</returns>
        private static int? ParseCommandLineArgs(IEnumerable<string> args, out CommandLineArgs parsed)
        {
            parsed = null;
            var oldParser = false;
            var dumpPreprocesor = false;
            string compileFile = null;

            // ReSharper disable once GenericEnumeratorNotDisposed
            var enumerator = args.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var cur = enumerator.Current;
                if (cur == "--help")
                {
                    PrintHelp();
                }
                else if (cur == "--old-parser")
                {
                    oldParser = true;
                }
                else if (cur == "--dump-preprocessor")
                {
                    dumpPreprocesor = true;
                }
                else
                {
                    if (compileFile != null)
                    {
                        Console.WriteLine($"Cannot specify multiple files to be compiled.");
                        return 1;
                    }

                    // File to compile.
                    var extension = Path.GetExtension(cur);
                    if (extension is not (".dm" or ".dme"))
                    {
                        Console.WriteLine($"'{cur}' is not a valid DME or DM file, aborting");
                        return 1;
                    }

                    compileFile = cur;
                }
            }

            parsed = new CommandLineArgs(compileFile, oldParser, dumpPreprocesor);
            return null;
        }

        private static void PrintHelp()
        {
            Console.WriteLine(@"usage: DMCompiler [options] <source to compile>
Arguments:
    source to compile       The .dme environment or .dm source file to compile.

Options:
    --old-parser            Use old parser instead of SpacemanDMM.
");
        }

        private static string[] GetFileList(string environmentFile)
        {
            var compilerDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dmStandardDirectory = Path.Combine(compilerDirectory ?? string.Empty, "DMStandard");
            return new[] { Path.Combine(dmStandardDirectory, "_Standard.dm"), environmentFile };
        }

        private static DMPreprocessor Preprocess(string file) {
            DMPreprocessor preprocessor = new DMPreprocessor(true);

            string compilerDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string dmStandardDirectory = Path.Combine(compilerDirectory ?? string.Empty, "DMStandard");
            preprocessor.IncludeFile(dmStandardDirectory, "_Standard.dm");

            string directoryPath = Path.GetDirectoryName(file);
            string fileName = Path.GetFileName(file);

            preprocessor.IncludeFile(directoryPath, fileName);

            return preprocessor;
        }

        private static void Compile(List<Token> preprocessedTokens) {
            DMLexer dmLexer = new DMLexer(null, preprocessedTokens);
            DMParser dmParser = new DMParser(dmLexer);
            DMASTFile astFile = dmParser.File();

            if (dmParser.Warnings.Count > 0) {
                foreach (CompilerWarning warning in dmParser.Warnings) {
                    Warning(warning);
                }
            }

            if (dmParser.Errors.Count > 0) {
                foreach (CompilerError error in dmParser.Errors) {
                    Error(error);
                }

                return;
            }

            DMASTSimplifier astSimplifier = new DMASTSimplifier();
            astSimplifier.SimplifyAST(astFile);

            DMObjectBuilder dmObjectBuilder = new DMObjectBuilder();
            dmObjectBuilder.BuildObjectTree(astFile);
        }

        public static void Error(CompilerError error) {
            Console.WriteLine(error);
            _errorCount++;
        }

        public static void Warning(CompilerWarning warning) {
            Console.WriteLine(warning);
        }

        private static List<DreamMapJson> ConvertMaps(List<string> mapPaths) {
            List<DreamMapJson> maps = new();

            foreach (string mapPath in mapPaths) {
                DMPreprocessor preprocessor = new DMPreprocessor(false);
                preprocessor.IncludeFile(String.Empty, mapPath);

                DMLexer lexer = new DMLexer(mapPath, preprocessor.GetResult());
                DMMParser parser = new DMMParser(lexer);
                DreamMapJson map = parser.ParseMap();

                if (parser.Errors.Count > 0) {
                    foreach (CompilerError error in parser.Errors) {
                        Error(error);
                    }

                    continue;
                }

                maps.Add(map);
            }

            return maps;
        }

        private static void SaveJson(List<DreamMapJson> maps, string interfaceFile, string outputFile) {
            DreamCompiledJson compiledDream = new DreamCompiledJson();
            compiledDream.Strings = DMObjectTree.StringTable;
            compiledDream.Maps = maps;
            compiledDream.Interface = interfaceFile;
            compiledDream.RootObject = DMObjectTree.CreateJsonRepresentation();
            if (DMObjectTree.GlobalInitProc.Bytecode.Length > 0) compiledDream.GlobalInitProc = DMObjectTree.GlobalInitProc.GetJsonRepresentation();

            string json = JsonSerializer.Serialize(compiledDream, new JsonSerializerOptions() {
                IgnoreNullValues = true
            });

            File.WriteAllText(outputFile, json);
            Console.WriteLine("Saved to " + outputFile);
        }

        private sealed record CommandLineArgs(
            string CompileFile,
            bool OldParser,
            bool DumpPreprocesor);
    }
}
