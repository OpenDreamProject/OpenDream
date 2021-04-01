using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DMCompiler.DM;
using DMCompiler.DM.Visitors;
using DMCompiler.Preprocessor;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Json;

namespace DMCompiler {
    class Program {
        public static List<string> StringTable = new();
        public static Dictionary<string, int> StringToStringID = new();
        public static DMProc GlobalInitProc = new();

        static void Main(string[] args) {
            if (args.Length < 2) {
                Console.WriteLine("Three arguments are required:");
                Console.WriteLine("\tInclude Path");
                Console.WriteLine("\t\tPath to the folder containing the code");
                Console.WriteLine("\tDME File");
                Console.WriteLine("\t\tPath to the DME file to be compiled");
                Console.WriteLine("\tOutput File");
                Console.WriteLine("\t\tPath to the output file");

                return;
            }

            DMPreprocessor preprocessor = new DMPreprocessor();
            preprocessor.IncludeFile("DMStandard", "_Standard.dm");
            preprocessor.IncludeFile(args[0], args[1]);

            string source = preprocessor.GetResult();
            DMLexer dmLexer = new DMLexer(source);
            DMParser dmParser = new DMParser(dmLexer);
            DMASTFile astFile = dmParser.File();
            
            DMASTSimplifier astSimplifier = new DMASTSimplifier();
            astSimplifier.SimplifyAST(astFile);

            DMVisitorObjectBuilder dmObjectBuilder = new DMVisitorObjectBuilder();
            DMObjectTree objectTree = dmObjectBuilder.BuildObjectTree(astFile);

            DreamCompiledJson compiledDream = new DreamCompiledJson();
            compiledDream.Strings = StringTable;
            compiledDream.RootObject = objectTree.CreateJsonRepresentation();
            if (GlobalInitProc.Bytecode.Length > 0) compiledDream.GlobalInitProc = GlobalInitProc.GetJsonRepresentation();

            string json = JsonSerializer.Serialize(compiledDream, new JsonSerializerOptions() {
                IgnoreNullValues = true
            });
            
            File.WriteAllText(args[2], json);
        }
    }
}
