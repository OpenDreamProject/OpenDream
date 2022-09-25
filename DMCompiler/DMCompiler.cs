﻿using DMCompiler.Compiler.DM;
using DMCompiler.Compiler.DMM;
using DMCompiler.Compiler.DMPreprocessor;
using DMCompiler.DM;
using DMCompiler.DM.Visitors;
using OpenDreamShared.Compiler;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace DMCompiler {
    //TODO: Make this not a static class
    public static class DMCompiler {
        public static int ErrorCount = 0;
        public static int WarningCount = 0;
        public static DMCompilerSettings Settings;

        private static DateTime _compileStartTime;

        public static bool Compile(DMCompilerSettings settings) {
            ErrorCount = 0;
            WarningCount = 0;
            Settings = settings;
            if (Settings.Files == null) return false;

            //TODO: Only use InvariantCulture where necessary instead of it being the default
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            _compileStartTime = DateTime.Now;

            if (settings.SuppressUnimplementedWarnings) {
                Warning(new CompilerWarning(Location.Internal, "Unimplemented proc & var warnings are currently suppressed"));
            }
            if(OpenDreamShared.Dream.Procs.OpcodeVerifier.AreOpcodesInvalid())
            {
                Error(new CompilerError(Location.Internal, "Some opcodes have the same byte value! Output assembly may be corrupted."));
            }

            DMPreprocessor preprocessor = Preprocess(settings.Files);
            bool successfulCompile = preprocessor is not null && Compile(preprocessor);

            if (successfulCompile)
            {
                //Output file is the first file with the extension changed to .json
                string outputFile = Path.ChangeExtension(settings.Files[0], "json");
                List<DreamMapJson> maps = ConvertMaps(preprocessor.IncludedMaps);

                if (ErrorCount > 0)
                {
                    successfulCompile = false;
                }
                else
                {
                    var output = SaveJson(maps, preprocessor.IncludedInterface, outputFile);
                    if (ErrorCount > 0)
                    {
                        successfulCompile = false;
                    }
                    else
                    {
                        Console.WriteLine($"Compilation succeeded with {WarningCount} warnings");
                        Console.WriteLine(output);
                    }
                }
            }

            if (!successfulCompile) {
                Console.WriteLine($"Compilation failed with {ErrorCount} errors and {WarningCount} warnings");
            }

            TimeSpan duration = DateTime.Now - _compileStartTime;
            Console.WriteLine($"Total time: {duration.ToString(@"mm\:ss")}");

            return successfulCompile;
        }

        [CanBeNull]
        private static DMPreprocessor Preprocess(List<string> files) {
            if (!Settings.NoStandard) {
                string compilerDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string dmStandard = Path.Join(compilerDirectory ?? string.Empty, "DMStandard", "_Standard.dm");

                if (!File.Exists(dmStandard))
                {
                    Error(new CompilerError(Location.Internal, "DMStandard not found."));
                    return null;
                }

                files.Add(dmStandard);
            }

            if (Settings.DumpPreprocessor) {
                //Preprocessing is done twice because the output is used up when dumping it
                DMPreprocessor dumpPreproc = new DMPreprocessor(true);
                dumpPreproc.IncludeFiles(files);

                StringBuilder result = new();
                foreach (Token t in dumpPreproc) {
                    result.Append(t.Text);
                }

                string outputDir = Path.GetDirectoryName(Settings.Files[0]);
                string outputPath = Path.Combine(outputDir, "preprocessor_dump.dm");

                File.WriteAllText(outputPath, result.ToString());
                Console.WriteLine($"Preprocessor output dumped to {outputPath}");
            }

            DMPreprocessor preproc = new DMPreprocessor(true);
            preproc.IncludeFiles(files);
            return preproc;
        }

        private static bool Compile(IEnumerable<Token> preprocessedTokens) {
            DMLexer dmLexer = new DMLexer(null, preprocessedTokens);
            DMParser dmParser = new DMParser(dmLexer, !Settings.SuppressUnimplementedWarnings);

            VerbosePrint("Parsing");
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
            }

            if (astFile == null) return false;

            DMASTSimplifier astSimplifier = new DMASTSimplifier();
            VerbosePrint("Constant folding");
            astSimplifier.SimplifyAST(astFile);

            DMObjectBuilder.BuildObjectTree(astFile);

            if (ErrorCount > 0) {
                return false;
            }

            return true;
        }

        public static void Error(CompilerError error) {
            Console.WriteLine(error);
            ErrorCount++;
        }

        public static void Error(Location loc, string message) {
            Error(new CompilerError(loc, message));
        }

        public static void Warning(CompilerWarning warning) {
            Console.WriteLine(warning);
            WarningCount++;
        }

        public static void Warning(Location loc, string message) {
            Warning(new CompilerWarning(loc, message));
        }

        public static void UnimplementedWarning(Location loc, string message) {
            if (Settings.SuppressUnimplementedWarnings)
                return;

            Warning(new CompilerWarning(loc, message));
        }

        public static void VerbosePrint(string message) {
            if (!Settings.Verbose) return;

            TimeSpan duration = DateTime.Now - _compileStartTime;
            Console.WriteLine($"{duration.ToString(@"mm\:ss\.fffffff")}: {message}");
        }

        private static List<DreamMapJson> ConvertMaps(List<string> mapPaths) {
            List<DreamMapJson> maps = new();

            foreach (string mapPath in mapPaths) {
                VerbosePrint($"Converting map {mapPath}");

                DMPreprocessor preprocessor = new DMPreprocessor(false);
                preprocessor.PreprocessFile(Path.GetDirectoryName(mapPath), Path.GetFileName(mapPath));

                DMLexer lexer = new DMLexer(mapPath, preprocessor);
                DMMParser parser = new DMMParser(lexer);
                DreamMapJson map = parser.ParseMap();

                if (parser.Errors.Count > 0) {
                    foreach (CompilerError error in parser.Errors) {
                        Error(error);
                    }

                    continue;
                }

                if (parser.Warnings.Count > 0) {
                    foreach (CompilerWarning warning in parser.Warnings) {
                        Warning(warning);
                    }
                }

                maps.Add(map);
            }

            return maps;
        }

        private static string SaveJson(List<DreamMapJson> maps, string interfaceFile, string outputFile)
        {
            DreamCompiledJson compiledDream = new DreamCompiledJson();
            compiledDream.Strings = DMObjectTree.StringTable;
            compiledDream.Maps = maps;
            compiledDream.Interface = interfaceFile;
            var jsonRep = DMObjectTree.CreateJsonRepresentation();
            compiledDream.Types = jsonRep.Item1;
            compiledDream.Procs = jsonRep.Item2;
            if (DMObjectTree.GlobalInitProc.Bytecode.Length > 0) compiledDream.GlobalInitProc = DMObjectTree.GlobalInitProc.GetJsonRepresentation();

            if (DMObjectTree.Globals.Count > 0) {
                GlobalListJson globalListJson = new GlobalListJson();
                globalListJson.GlobalCount = DMObjectTree.Globals.Count;

                // Approximate capacity (4/285 in tgstation, ~3%)
                globalListJson.Globals = new Dictionary<int, object>((int) (DMObjectTree.Globals.Count * 0.03));

                for (int i = 0; i < DMObjectTree.Globals.Count; i++) {
                    DMVariable global = DMObjectTree.Globals[i];
                    if (!global.TryAsJsonRepresentation(out var globalJson))
                        DMCompiler.Error(new CompilerError(global.Value.Location, $"Failed to serialize global {global.Name}"));

                    if (globalJson != null) {
                        globalListJson.Globals.Add(i, globalJson);
                    }
                }
                compiledDream.Globals = globalListJson;
            }

            if (DMObjectTree.GlobalProcs.Count > 0)
            {
                compiledDream.GlobalProcs = DMObjectTree.GlobalProcs.Values.ToList();
            }

            string json = JsonSerializer.Serialize(compiledDream, new JsonSerializerOptions() {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            });

            // Successful serialization
            if (ErrorCount == 0)
            {
                File.WriteAllText(outputFile, json);
                return "Saved to " + outputFile;
            }
            return string.Empty;
        }
    }

    public struct DMCompilerSettings {
        public List<string> Files;
        public bool SuppressUnimplementedWarnings;
        public bool DumpPreprocessor;
        public bool NoStandard;
        public bool Verbose;
    }
}
