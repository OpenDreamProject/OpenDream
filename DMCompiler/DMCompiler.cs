using DMCompiler.Compiler.DM;
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
using DMCompiler.Compiler;
using Robust.Shared.Utility;

namespace DMCompiler {
    //TODO: Make this not a static class
    public static class DMCompiler {
        public static int ErrorCount = 0;
        public static int WarningCount = 0;
        public static DMCompilerSettings Settings;

        private static DMCompilerConfiguration Config;
        private static DateTime _compileStartTime;

        public static bool Compile(DMCompilerSettings settings) {
            ErrorCount = 0;
            WarningCount = 0;
            Settings = settings;
            if (Settings.Files == null) return false;
            Config = new();

            //TODO: Only use InvariantCulture where necessary instead of it being the default
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            _compileStartTime = DateTime.Now;

            if (settings.SuppressUnimplementedWarnings) {
                ForcedWarning("Unimplemented proc & var warnings are currently suppressed");
            }
            if(OpenDreamShared.Dream.Procs.OpcodeVerifier.AreOpcodesInvalid())
            {
                ForcedError("Some opcodes have the same byte value! Output assembly may be corrupted.");
            }

            DMPreprocessor preprocessor = Preprocess(settings.Files, settings.MacroDefines);
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
        private static DMPreprocessor Preprocess(List<string> files, Dictionary<string, string> macroDefines) {
            DMPreprocessor build() {
                DMPreprocessor preproc = new DMPreprocessor(true);
                if (macroDefines != null) {
                    foreach (var (key, value) in macroDefines) {
                        preproc.DefineMacro(key, value);
                    }
                }
                DefineFatalErrors();

                // NB: IncludeFile pushes newly seen files to a stack, so push
                // them in reverse order to process them in forward order.
                for (var i = files.Count - 1; i >= 0; i--) {
                    string includeDir = Path.GetDirectoryName(files[i]);
                    string fileName = Path.GetFileName(files[i]);

                    preproc.IncludeFile(includeDir, fileName);
                }
                string compilerDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                string dmStandardDirectory = Path.Join(compilerDirectory, "DMStandard");
                // Push DMStandard to the top of the stack, prioritizing it.
                if (!Settings.NoStandard) {
                    preproc.IncludeFile(dmStandardDirectory, "_Standard.dm");
                }
                // Push the pragma config file to the tippy-top of the stack, super-duper prioritizing it, since it governs some compiler behaviour.
                string pragmaName;
                string pragmaDirectory;
                if(Settings.PragmaFileOverride is not null) {
                    pragmaDirectory = Path.GetDirectoryName(Settings.PragmaFileOverride);
                    pragmaName = Path.GetFileName(Settings.PragmaFileOverride);
                } else {
                    pragmaDirectory = dmStandardDirectory;
                    pragmaName = "DefaultPragmaConfig.dm";
                }
                if(!File.Exists(Path.Join(pragmaDirectory,pragmaName))) {
                    ForcedError($"Configuration file '{pragmaName}' not found.");
                    return null;
                }
                preproc.IncludeFile(pragmaDirectory,pragmaName);
                return preproc;
            }

            if (Settings.DumpPreprocessor) {
                //Preprocessing is done twice because the output is used up when dumping it
                DMPreprocessor dumpPreproc = build();

                StringBuilder result = new();
                foreach (Token t in dumpPreproc) {
                    result.Append(t.Text);
                }

                string outputDir = Path.GetDirectoryName(Settings.Files[0]);
                string outputPath = Path.Combine(outputDir, "preprocessor_dump.dm");

                File.WriteAllText(outputPath, result.ToString());
                Console.WriteLine($"Preprocessor output dumped to {outputPath}");
            }
            return build();
        }

        private static bool Compile(IEnumerable<Token> preprocessedTokens) {
            DMLexer dmLexer = new DMLexer(null, preprocessedTokens);
            DMParser dmParser = new DMParser(dmLexer, !Settings.SuppressUnimplementedWarnings);

            VerbosePrint("Parsing");
            DMASTFile astFile = dmParser.File();

            foreach (CompilerEmission warning in dmParser.Emissions) {
                Emit(warning);
            }

            if (astFile is null) {
                VerbosePrint("Parsing failed, exiting compilation");
                return false;
            }

            DMASTSimplifier astSimplifier = new DMASTSimplifier();
            VerbosePrint("Constant folding");
            astSimplifier.SimplifyAST(astFile);

            DMObjectBuilder.BuildObjectTree(astFile);

            return ErrorCount == 0;
        }

        public static void Emit(CompilerEmission emission) {
            switch (emission.Level) {
                case ErrorLevel.Disabled:
                    return;
                case ErrorLevel.Notice:
                    if (!Settings.NoticesEnabled)
                        return;
                    break;
                case ErrorLevel.Warning:
                    ++WarningCount;
                    break;
                case ErrorLevel.Error:
                    ++ErrorCount;
                    break;
            }
            Console.WriteLine(emission);
            return;
        }

        /// <summary> Emits the given warning, according to its ErrorLevel as set in our config. </summary>
        public static void Emit(WarningCode code, Location loc, string message) {
            ErrorLevel level = Config.errorConfig[code];
            Emit(new CompilerEmission(level, code, loc, message));
        }

        /// <summary>
        /// To be used when the compiler MUST ALWAYS give an error. <br/>
        /// Completely ignores the warning configuration. Use wisely!
        /// </summary>
        public static void ForcedError(string message) {
            ForcedError(Location.Internal, message);
        }

        /// <inheritdoc cref="ForcedError(string)"/>
        public static void ForcedError(Location loc, string message) {
            Console.WriteLine(new CompilerEmission(ErrorLevel.Error, loc, message));
            ErrorCount++;
        }

        /// <summary>
        /// To be used when the compiler MUST ALWAYS give a warning. <br/>
        /// Completely ignores the warning configuration. Use wisely!
        /// </summary>
        public static void ForcedWarning(string message) {
            Console.WriteLine(new CompilerEmission(ErrorLevel.Warning, Location.Internal, message));
            WarningCount++;
        }

        /// <inheritdoc cref="ForcedWarning(string)"/>
        public static void ForcedWarning(Location loc, string message) {
            Console.WriteLine(new CompilerEmission(ErrorLevel.Warning, loc, message));
            WarningCount++;
        }

        public static void UnimplementedWarning(Location loc, string message) {
            if (Settings.SuppressUnimplementedWarnings)
                return;

            ForcedWarning(loc, message);
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

                if (parser.Emissions.Count > 0) {
                    foreach (CompilerEmission error in parser.Emissions) {
                        Emit(error);
                    }

                    continue;
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
            compiledDream.Interface = string.IsNullOrEmpty(interfaceFile) ? "" : Path.GetRelativePath(Path.GetDirectoryName(Path.GetFullPath(outputFile)), interfaceFile);
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
                        ForcedError(global.Value.Location, $"Failed to serialize global {global.Name}");

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

        public static void DefineFatalErrors() {
            foreach (WarningCode code in Enum.GetValues<WarningCode>()) {
                if((int)code < 1_000) {
                    Config.errorConfig[code] = ErrorLevel.Error;
                }
            }
        }

        /// <summary>
        /// This method also enforces the rule that all emissions with codes less than 1000 are mandatory errors.
        /// </summary>
        public static void CheckAllPragmasWereSet() {
            foreach(WarningCode code in Enum.GetValues<WarningCode>()) {
                if (!Config.errorConfig.ContainsKey(code)) {
                    ForcedWarning($"Warning #{(int)code:d4} '{code.ToString()}' was never declared as error, warning, notice, or disabled.");
                    Config.errorConfig.Add(code, ErrorLevel.Disabled);
                }
            }
        }

        public static void SetPragma(WarningCode code, ErrorLevel level) {
            Config.errorConfig[code] = level;
        }

        public static ErrorLevel CodeToLevel(WarningCode code) {
            bool didFind = Config.errorConfig.TryGetValue(code, out var ret);
            DebugTools.Assert(didFind);
            return ret;
        }
    }

    public struct DMCompilerSettings {
        public List<string> Files;
        public bool SuppressUnimplementedWarnings;
        public bool NoticesEnabled;
        public bool DumpPreprocessor;
        public bool NoStandard;
        public bool Verbose;
        public Dictionary<string, string> MacroDefines;
        /// <summary> A user-provided pragma config file, if one was provided. </summary>
        public string? PragmaFileOverride;
    }

    class DMCompilerConfiguration {
        public Dictionary<WarningCode, ErrorLevel> errorConfig;
        public DMCompilerConfiguration() {
            errorConfig = new(Enum.GetValues<WarningCode>().Length);
        }
    }
}
