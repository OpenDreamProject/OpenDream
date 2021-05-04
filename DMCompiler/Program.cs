using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using DMCompiler.DM;
using DMCompiler.DM.Visitors;
using DMCompiler.Preprocessor;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Json;

namespace DMCompiler {
    class Program {
        public static List<string> IncludedMaps = new();
        public static string IncludedInterface = null;

        static void Main(string[] args) {
            if (!VerifyArguments(args)) return;

            string source = Preprocess(args);
            Compile(source);

            //Output file is the first file with the extension changed to .json
            string outputFile = Path.ChangeExtension(args[0], "json");
            SaveJson(outputFile);
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

        private static string Preprocess(string[] files) {
            DMPreprocessor preprocessor = new DMPreprocessor();

            string compilerDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string dmStandardDirectory = Path.Combine(compilerDirectory, "DMStandard");
            preprocessor.IncludeFile(dmStandardDirectory, "_Standard.dm");

            foreach (string file in files) {
                string directoryPath = Path.GetDirectoryName(file);
                string fileName = Path.GetFileName(file);

                preprocessor.IncludeFile(directoryPath, fileName);
            }

            return preprocessor.GetResult();
        }

        private static void Compile(string source) {
            DMLexer dmLexer = new DMLexer(source);
            DMParser dmParser = new DMParser(dmLexer);
            DMASTFile astFile = dmParser.File();

            DMASTSimplifier astSimplifier = new DMASTSimplifier();
            astSimplifier.SimplifyAST(astFile);

            DMVisitorObjectBuilder dmObjectBuilder = new DMVisitorObjectBuilder();
            dmObjectBuilder.BuildObjectTree(astFile);
        }

        private static void SaveJson(string outputFile) {
            DreamCompiledJson compiledDream = new DreamCompiledJson();
            compiledDream.Strings = DMObjectTree.StringTable;
            compiledDream.Maps = IncludedMaps;
            compiledDream.Interface = IncludedInterface;
            compiledDream.RootObject = DMObjectTree.CreateJsonRepresentation();
            if (DMObjectTree.GlobalInitProc != null) compiledDream.GlobalInitProc = DMObjectTree.GlobalInitProc.GetJsonRepresentation();

            string json = JsonSerializer.Serialize(compiledDream, new JsonSerializerOptions() {
                IgnoreNullValues = true
            });

            File.WriteAllText(outputFile, json);
        }
    }
}
