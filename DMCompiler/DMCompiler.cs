using DMCompiler.Compiler.DMM;
using DMCompiler.DM;
using DMCompiler.DM.Visitors;
using OpenDreamShared.Compiler;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Compiler.DMPreprocessor;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DMCompiler {
    //TODO: Make this not a static class
    public static class DMCompiler {
        public static int ErrorCount = 0;
        public static DMCompilerSettings Settings;

        public static bool Compile(DMCompilerSettings settings) {
            Settings = settings;
            if (Settings.Files == null) return false;

            //TODO: Only use InvariantCulture where necessary instead of it being the default
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            DateTime startTime = DateTime.Now;

            if (settings.SuppressUnimplementedWarnings) {
                Warning(new CompilerWarning(null, "Unimplemented proc & var warnings are currently suppressed"));
            }

            DMPreprocessor preprocessor = Preprocess(settings.Files);
            if (settings.DumpPreprocessor) {
                StringBuilder result = new();
                foreach (Token t in preprocessor.GetResult()) {
                    result.Append(t.Text);
                }

                string output = Path.Join(Path.GetDirectoryName(settings.Files?[0]) ?? AppDomain.CurrentDomain.BaseDirectory, "preprocessor_dump.dm");
                File.WriteAllText(output, result.ToString());
                Console.WriteLine($"Preprocessor output dumped to {output}");
            }

            bool successfulCompile = Compile(preprocessor.GetResult());

            if (successfulCompile) {
                //Output file is the first file with the extension changed to .json
                string outputFile = Path.ChangeExtension(settings.Files[0], "json");
                List<DreamMapJson> maps = ConvertMaps(preprocessor.IncludedMaps);

                SaveJson(maps, preprocessor.IncludedInterface, outputFile);
            } else {
                Console.WriteLine($"Compilation failed with {ErrorCount} errors");
            }

            TimeSpan duration = DateTime.Now - startTime;
            Console.WriteLine($"Total time: {duration.ToString(@"mm\:ss")}");

            return successfulCompile;
        }

        private static DMPreprocessor Preprocess(List<string> files) {
            DMPreprocessor preprocessor = new DMPreprocessor(true, !Settings.SuppressUnimplementedWarnings);

            if (!Settings.NoStandard) {
                string compilerDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string dmStandardDirectory = Path.Combine(compilerDirectory ?? string.Empty, "DMStandard");
                preprocessor.IncludeFile(dmStandardDirectory, "_Standard.dm");
            }

            foreach (string file in files) {
                string directoryPath = Path.GetDirectoryName(file);
                string fileName = Path.GetFileName(file);

                preprocessor.IncludeFile(directoryPath, fileName);
            }

            return preprocessor;
        }

        private static bool Compile(List<Token> preprocessedTokens) {
            DMLexer dmLexer = new DMLexer(null, preprocessedTokens);
            DMParser dmParser = new DMParser(dmLexer, !Settings.SuppressUnimplementedWarnings);
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

                return false;
            }

            if (astFile == null) return false;

            DMASTSimplifier astSimplifier = new DMASTSimplifier();
            astSimplifier.SimplifyAST(astFile);

            DMObjectBuilder dmObjectBuilder = new DMObjectBuilder();
            dmObjectBuilder.BuildObjectTree(astFile);

            if (ErrorCount > 0) {
                return false;
            }

            return true;
        }

        public static void Error(CompilerError error) {
            Console.WriteLine(error);
            ErrorCount++;
        }

        public static void Warning(CompilerWarning warning) {
            Console.WriteLine(warning);
        }

        private static List<DreamMapJson> ConvertMaps(List<string> mapPaths) {
            List<DreamMapJson> maps = new();

            foreach (string mapPath in mapPaths) {
                DMPreprocessor preprocessor = new DMPreprocessor(false, !Settings.SuppressUnimplementedWarnings);
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

            if (DMObjectTree.Globals.Count > 0) {
                compiledDream.Globals = new List<object>();

                foreach (DMVariable global in DMObjectTree.Globals) {
                    compiledDream.Globals.Add(global.ToJsonRepresentation());
                }
            }

            string json = JsonSerializer.Serialize(compiledDream, new JsonSerializerOptions() {
                IgnoreNullValues = true
            });

            File.WriteAllText(outputFile, json);
            Console.WriteLine("Saved to " + outputFile);
        }
    }

    public struct DMCompilerSettings {
        public List<string> Files;
        public bool SuppressUnimplementedWarnings;
        public bool DumpPreprocessor;
        public bool NoStandard;
    }
}
