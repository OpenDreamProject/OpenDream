using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using DMCompiler.DM;
using DMCompiler.DM.Visitors;
using OpenDreamShared.Compiler;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Compiler.DMPreprocessor;
using OpenDreamShared.Json;

namespace DMCompiler {
    class Program {
        public static List<string> IncludedMaps = new();
        public static string IncludedInterface = null;

        static void Main(string[] args) {
            if (!VerifyArguments(args)) return;

            DMPreprocessor preprocessor = Preprocess(args);
            if (Compile(preprocessor.GetResult())) {
                //Output file is the first file with the extension changed to .json
                string outputFile = Path.ChangeExtension(args[0], "json");

                SaveJson(preprocessor.IncludedMaps, preprocessor.IncludedInterface, outputFile);
            }
        }

        private static bool VerifyArguments(string[] args) {
            if (args.Length < 1) {
                Console.WriteLine("At least one DME or DM file must be provided as an argument");

                return false;
            }

            foreach (string arg in args) {
                string extension = Path.GetExtension(arg);

                if (extension != ".dme" && extension != ".dm") {
                    Console.WriteLine(arg + " is not a valid DME or DM file");

                    return false;
                }
            }

            return true;
        }

        private static DMPreprocessor Preprocess(string[] files) {
            DMPreprocessor preprocessor = new DMPreprocessor(true);

            string compilerDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string dmStandardDirectory = Path.Combine(compilerDirectory, "DMStandard");
            preprocessor.IncludeFile(dmStandardDirectory, "_Standard.dm");

            foreach (string file in files) {
                string directoryPath = Path.GetDirectoryName(file);
                string fileName = Path.GetFileName(file);

                preprocessor.IncludeFile(directoryPath, fileName);
            }

            return preprocessor;
        }

        private static bool Compile(List<Token> preprocessedTokens) {
            DMLexer dmLexer = new DMLexer(null, preprocessedTokens);
            DMParser dmParser = new DMParser(dmLexer);
            DMASTFile astFile = dmParser.File();

            if (dmParser.Errors.Count > 0) {
                foreach (CompilerError error in dmParser.Errors) {
                    Console.WriteLine(error);
                }

                return false;
            }

            DMASTSimplifier astSimplifier = new DMASTSimplifier();
            astSimplifier.SimplifyAST(astFile);

            DMVisitorObjectBuilder dmObjectBuilder = new DMVisitorObjectBuilder();
            dmObjectBuilder.BuildObjectTree(astFile);

            return true;
        }

        private static void SaveJson(List<string> maps, string interfaceFile, string outputFile) {
            DreamCompiledJson compiledDream = new DreamCompiledJson();
            compiledDream.Strings = DMObjectTree.StringTable;
            compiledDream.Maps = maps;
            compiledDream.Interface = interfaceFile;
            compiledDream.RootObject = DMObjectTree.CreateJsonRepresentation();
            if (DMObjectTree.GlobalInitProc != null) compiledDream.GlobalInitProc = DMObjectTree.GlobalInitProc.GetJsonRepresentation();

            string json = JsonSerializer.Serialize(compiledDream, new JsonSerializerOptions() {
                IgnoreNullValues = true
            });

            File.WriteAllText(outputFile, json);
            Console.WriteLine("Saved to " + outputFile);
        }
    }
}
