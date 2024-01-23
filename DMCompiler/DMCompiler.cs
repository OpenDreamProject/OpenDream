using DMCompiler.Bytecode;
using DMCompiler.Compiler.DM;
using DMCompiler.Compiler.DMM;
using DMCompiler.Compiler.DMPreprocessor;
using DMCompiler.DM;
using DMCompiler.DM.Visitors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DMCompiler.DM.Optimizer;
using DMCompiler.Compiler;
using DMCompiler.Json;

namespace DMCompiler;

//TODO: Make this not a static class
public static class DMCompiler {
    public static string StandardLibraryDirectory = "";
    public static string MainDirectory = "";
    public static int ErrorCount;
    public static int WarningCount;
    public static DMCompilerSettings Settings;
    public static IReadOnlyList<string> ResourceDirectories => _resourceDirectories;

    private static readonly DMCompilerConfiguration Config = new();
    private static readonly List<string> _resourceDirectories = new();
    private static DateTime _compileStartTime;

    public static bool Compile(DMCompilerSettings settings) {
        ErrorCount = 0;
        WarningCount = 0;
        Settings = settings;
        if (Settings.Files == null) return false;
        Config.Reset();
        _resourceDirectories.Clear();

        //TODO: Only use InvariantCulture where necessary instead of it being the default
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        _compileStartTime = DateTime.Now;

#if DEBUG
        ForcedWarning("This compiler was compiled in the Debug .NET configuration. This will impact compile speed.");
#endif

        if (settings.SuppressUnimplementedWarnings) {
            ForcedWarning("Unimplemented proc & var warnings are currently suppressed");
        }

        DMPreprocessor preprocessor = Preprocess(settings.Files, settings.MacroDefines);
        bool successfulCompile = preprocessor is not null && Compile(preprocessor);

        if (successfulCompile) {
            //Output file is the first file with the extension changed to .json
            string outputFile = Path.ChangeExtension(settings.Files[0], "json");
            List<DreamMapJson> maps = ConvertMaps(preprocessor.IncludedMaps);

            if (ErrorCount > 0) {
                successfulCompile = false;
            } else {
                var output = SaveJson(maps, preprocessor.IncludedInterface, outputFile);
                if (ErrorCount > 0) {
                    successfulCompile = false;
                } else {
                    Console.WriteLine($"Compilation succeeded with {WarningCount} warnings");
                    Console.WriteLine(output);
                }
            }
        }

        if (!successfulCompile) {
            Console.WriteLine($"Compilation failed with {ErrorCount} errors and {WarningCount} warnings");
        }

        TimeSpan duration = DateTime.Now - _compileStartTime;
        Console.WriteLine($"Total time: {duration:mm\\:ss}");

        return successfulCompile;
    }

    public static void AddResourceDirectory(string dir) {
        dir = dir.Replace('\\', Path.DirectorySeparatorChar);

        _resourceDirectories.Add(dir);
    }

    private static DMPreprocessor? Preprocess(List<string> files, Dictionary<string, string>? macroDefines) {
        DMPreprocessor? Build() {
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
            MainDirectory = Path.GetDirectoryName(files[0]) ?? string.Empty;
            string compilerDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            string dmStandardDirectory = Path.Join(compilerDirectory, "DMStandard");
            StandardLibraryDirectory = dmStandardDirectory;
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
            StringBuilder result = new();
            foreach (Token t in Build()) {
                result.Append(t.Text);
            }

            string outputDir = Path.GetDirectoryName(Settings.Files[0]);
            string outputPath = Path.Combine(outputDir, "preprocessor_dump.dm");

            File.WriteAllText(outputPath, result.ToString());
            Console.WriteLine($"Preprocessor output dumped to {outputPath}");
        }

        return Build();
    }

    private static bool Compile(IEnumerable<Token> preprocessedTokens) {
        DMLexer dmLexer = new DMLexer(null, preprocessedTokens);
        DMParser dmParser = new DMParser(dmLexer);

        VerbosePrint("Parsing");
        DMASTFile astFile = dmParser.File();

        foreach (CompilerEmission warning in dmParser.Emissions) {
            Emit(warning);
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
    }

    /// <summary> Emits the given warning, according to its ErrorLevel as set in our config. </summary>
    /// <returns> True if the warning was an error, false if not.</returns>
    public static bool Emit(WarningCode code, Location loc, string message) {
        ErrorLevel level = Config.ErrorConfig[code];
        Emit(new CompilerEmission(level, code, loc, message));
        return level == ErrorLevel.Error;
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
        Console.WriteLine(new CompilerEmission(ErrorLevel.Error, loc, message).ToString());
        ErrorCount++;
    }

    /// <summary>
    /// To be used when the compiler MUST ALWAYS give a warning. <br/>
    /// Completely ignores the warning configuration. Use wisely!
    /// </summary>
    public static void ForcedWarning(string message) {
        Console.WriteLine(new CompilerEmission(ErrorLevel.Warning, Location.Internal, message).ToString());
        WarningCount++;
    }

    /// <inheritdoc cref="ForcedWarning(string)"/>
    public static void ForcedWarning(Location loc, string message) {
        Console.WriteLine(new CompilerEmission(ErrorLevel.Warning, loc, message).ToString());
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
        int zOffset = 0;

        foreach (string mapPath in mapPaths) {
            VerbosePrint($"Converting map {mapPath}");

            DMPreprocessor preprocessor = new DMPreprocessor(false);
            preprocessor.PreprocessFile(Path.GetDirectoryName(mapPath), Path.GetFileName(mapPath));

            DMLexer lexer = new DMLexer(mapPath, preprocessor);
            DMMParser parser = new DMMParser(lexer, zOffset);
            DreamMapJson map = parser.ParseMap();

            bool hadErrors = false;
            if (parser.Emissions.Count > 0) {
                foreach (CompilerEmission error in parser.Emissions) {
                    if (error.Level == ErrorLevel.Error)
                        hadErrors = true;

                    Emit(error);
                }
            }

            zOffset = Math.Max(zOffset + 1, map.MaxZ);
            if (!hadErrors)
                maps.Add(map);
        }

        return maps;
    }

    private static string SaveJson(List<DreamMapJson> maps, string interfaceFile, string outputFile) {
        if (Settings.DumpBytecode) {
            var bytecodeDumpFile = Path.ChangeExtension(outputFile, "dmc");
        }

        var jsonRep = DMObjectTree.CreateJsonRepresentation();
        DreamCompiledJson compiledDream = new DreamCompiledJson {
            Metadata = new DreamCompiledJsonMetadata { Version = OpcodeVerifier.GetOpcodesHash() },
            Strings = DMObjectTree.StringTable,
            Resources = DMObjectTree.Resources.ToArray(),
            Maps = maps,
            Interface = string.IsNullOrEmpty(interfaceFile)
                ? ""
                : Path.GetRelativePath(Path.GetDirectoryName(Path.GetFullPath(outputFile)), interfaceFile),
            Types = jsonRep.Item1,
            Procs = jsonRep.Item2
        };

        if (DMObjectTree.GlobalInitProc.AnnotatedBytecode.GetLength() > 0)
            compiledDream.GlobalInitProc = DMObjectTree.GlobalInitProc.GetJsonRepresentation();

        if (DMObjectTree.Globals.Count > 0) {
            GlobalListJson globalListJson = new GlobalListJson();
            globalListJson.GlobalCount = DMObjectTree.Globals.Count;
            globalListJson.Names = new List<string>(globalListJson.GlobalCount);

            // Approximate capacity (4/285 in tgstation, ~3%)
            globalListJson.Globals = new Dictionary<int, object>((int) (DMObjectTree.Globals.Count * 0.03));

            for (int i = 0; i < DMObjectTree.Globals.Count; i++) {
                DMVariable global = DMObjectTree.Globals[i];
                globalListJson.Names.Add(global.Name);

                if (!global.TryAsJsonRepresentation(out var globalJson))
                    ForcedError(global.Value.Location, $"Failed to serialize global {global.Name}");

                if (globalJson != null) {
                    globalListJson.Globals.Add(i, globalJson);
                }
            }
            compiledDream.Globals = globalListJson;
        }

        if (DMObjectTree.GlobalProcs.Count > 0) {
            compiledDream.GlobalProcs = DMObjectTree.GlobalProcs.Values.ToArray();
        }


        if (Settings.DumpBytecode) {
            var bytecodeDumpFile = Path.ChangeExtension(outputFile, "dmc");
            using var bytecodeDumpHandle = File.Create(bytecodeDumpFile);
            using var bytecodeDumpWriter = new StreamWriter(bytecodeDumpHandle);
            DMObjectTree.GlobalInitProc.Dump(bytecodeDumpWriter);
            foreach (var proc in DMObjectTree.AllProcs) {
                proc.Dump(bytecodeDumpWriter);
            }
        }

        // Successful serialization
        if (ErrorCount == 0) {
            using var outputFileHandle = File.Create(outputFile);

            try {
                JsonSerializer.Serialize(outputFileHandle, compiledDream,
                    new JsonSerializerOptions() {DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault});

                return $"Saved to {outputFile}";
            } catch (Exception e) {
                Console.WriteLine($"Failed to save to {outputFile}: {e.Message}");
            }
        }

        return string.Empty;
    }

    public static void DefineFatalErrors() {
        foreach (WarningCode code in Enum.GetValues<WarningCode>()) {
            if((int)code < 1_000) {
                Config.ErrorConfig[code] = ErrorLevel.Error;
            }
        }
    }

    /// <summary>
    /// This method also enforces the rule that all emissions with codes less than 1000 are mandatory errors.
    /// </summary>
    public static void CheckAllPragmasWereSet() {
        foreach(WarningCode code in Enum.GetValues<WarningCode>()) {
            if (!Config.ErrorConfig.ContainsKey(code)) {
                ForcedWarning($"Warning #{(int)code:d4} '{code.ToString()}' was never declared as error, warning, notice, or disabled.");
                Config.ErrorConfig.Add(code, ErrorLevel.Disabled);
            }
        }
    }

    public static void SetPragma(WarningCode code, ErrorLevel level) {
        Config.ErrorConfig[code] = level;
    }

    public static ErrorLevel CodeToLevel(WarningCode code) {
        if (!Config.ErrorConfig.TryGetValue(code, out var ret))
            throw new Exception($"Failed to find error level for code {code}");

        return ret;
    }
}

public struct DMCompilerSettings {
    public List<string>? Files = null;
    public bool SuppressUnimplementedWarnings = false;
    public bool NoticesEnabled = false;
    public bool DumpPreprocessor = false;
    public bool NoStandard = false;
    public bool Verbose = false;
    public bool DumpBytecode = false;
    public Dictionary<string, string>? MacroDefines = null;
    /// <summary> A user-provided pragma config file, if one was provided. </summary>
    public string? PragmaFileOverride = null;

    // These are the default DM_VERSION and DM_BUILD values. They're strings because that's what the preprocessor expects (seriously)
    public string DMVersion = "514";
    public string DMBuild = "1584";

    public DMCompilerSettings() {
    }
}

internal class DMCompilerConfiguration {
    public readonly Dictionary<WarningCode, ErrorLevel> ErrorConfig = new(Enum.GetValues<WarningCode>().Length);

    public void Reset() {
        ErrorConfig.Clear();
    }
}
