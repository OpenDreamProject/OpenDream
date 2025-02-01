using DMCompiler.Bytecode;
using DMCompiler.Compiler.DM;
using DMCompiler.Compiler.DMM;
using DMCompiler.Compiler.DMPreprocessor;
using DMCompiler.DM;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DMCompiler.Compiler;
using DMCompiler.Compiler.DM.AST;
using DMCompiler.DM.Builders;
using DMCompiler.Json;
using DMCompiler.Optimizer;

namespace DMCompiler;

public class DMCompiler {
    public int ErrorCount;
    public int WarningCount;
    public readonly HashSet<WarningCode> UniqueEmissions = new();
    public DMCompilerSettings Settings;
    public IReadOnlyList<string> ResourceDirectories => _resourceDirectories;

    private readonly DMCompilerConfiguration Config = new();
    private readonly List<string> _resourceDirectories = new();
    private string _codeDirectory;
    private DateTime _compileStartTime;

    internal readonly DMCodeTree DMCodeTree;
    internal readonly DMObjectTree DMObjectTree;
    internal readonly DMProc GlobalInitProc;
    internal readonly BytecodeOptimizer BytecodeOptimizer;

    public DMCompiler() {
        DMCodeTree = new(this);
        DMObjectTree = new(this);
        GlobalInitProc = new(this, -1, DMObjectTree.Root, null);
        BytecodeOptimizer = new BytecodeOptimizer(this);
    }

    public bool Compile(DMCompilerSettings settings) {
        if (_compileStartTime != default)
            throw new Exception("Create a new DMCompiler to compile again");

        ErrorCount = 0;
        WarningCount = 0;
        UniqueEmissions.Clear();
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

        DMPreprocessor preprocessor = Preprocess(this, settings.Files, settings.MacroDefines);
        bool successfulCompile = preprocessor is not null && Compile(preprocessor);

        if (successfulCompile) {
            //Output file is the first file with the extension changed to .json
            string outputFile = Path.ChangeExtension(settings.Files[0], "json");
            List<DreamMapJson> maps = ConvertMaps(this, preprocessor.IncludedMaps);

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

    public void AddResourceDirectory(string dir, Location loc) {
        dir = dir.Replace('\\', Path.DirectorySeparatorChar);
        if (!Directory.Exists(dir)) {
            Emit(WarningCode.InvalidFileDirDefine, loc,
                $"Folder \"{Path.GetRelativePath(_codeDirectory, dir)}\" does not exist");
            return;
        }

        _resourceDirectories.Add(dir);
    }

    private DMPreprocessor? Preprocess(DMCompiler compiler, List<string> files, Dictionary<string, string>? macroDefines) {
        DMPreprocessor? Build() {
            DMPreprocessor preproc = new DMPreprocessor(compiler, true);
            if (macroDefines != null) {
                foreach (var (key, value) in macroDefines) {
                    preproc.DefineMacro(key, value);
                }
            }

            DefineFatalErrors();

            // NB: IncludeFile pushes newly seen files to a stack, so push
            // them in reverse order to process them in forward order.
            for (var i = files.Count - 1; i >= 0; i--) {
                if (!File.Exists(files[i])) {
                    Console.WriteLine($"'{files[i]}' does not exist");
                    return null;
                }

                string includeDir = Path.GetDirectoryName(files[i]);
                string fileName = Path.GetFileName(files[i]);

                preproc.IncludeFile(includeDir, fileName, false);
            }

            // Adds the root of the DM project to FILE_DIR
            _codeDirectory = Path.GetDirectoryName(files[0]) ?? "";
            if (string.IsNullOrWhiteSpace(_codeDirectory))
                _codeDirectory = Path.GetFullPath(".");
            compiler.AddResourceDirectory(_codeDirectory, Location.Internal);

            string compilerDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            string dmStandardDirectory = Path.Join(compilerDirectory, "DMStandard");

            // Push DMStandard to the top of the stack, prioritizing it.
            if (!Settings.NoStandard) {
                preproc.IncludeFile(dmStandardDirectory, "_Standard.dm", true);
            }

            // Push the pragma config file to the tippy-top of the stack, super-duper prioritizing it, since it governs some compiler behaviour.
            string pragmaName;
            string pragmaDirectory;
            if (Settings.PragmaFileOverride is not null) {
                pragmaDirectory = Path.GetDirectoryName(Settings.PragmaFileOverride);
                pragmaName = Path.GetFileName(Settings.PragmaFileOverride);
            } else {
                pragmaDirectory = dmStandardDirectory;
                pragmaName = "DefaultPragmaConfig.dm";
            }

            if (!File.Exists(Path.Join(pragmaDirectory, pragmaName))) {
                ForcedError($"Configuration file '{pragmaName}' not found.");
                return null;
            }

            preproc.IncludeFile(pragmaDirectory, pragmaName, true);
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

    private bool Compile(IEnumerable<Token> preprocessedTokens) {
        DMLexer dmLexer = new DMLexer(null, preprocessedTokens);
        DMParser dmParser = new DMParser(this, dmLexer);

        VerbosePrint("Parsing");
        DMASTFile astFile = dmParser.File();

        DMASTFolder astSimplifier = new DMASTFolder();
        VerbosePrint("Constant folding");
        astSimplifier.FoldAst(astFile);

        DMCodeTreeBuilder dmCodeTreeBuilder = new(this);
        dmCodeTreeBuilder.BuildCodeTree(astFile);

        return ErrorCount == 0;
    }

    public void Emit(CompilerEmission emission) {
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

        UniqueEmissions.Add(emission.Code);
        Console.WriteLine(emission);
    }

    /// <summary> Emits the given warning, according to its ErrorLevel as set in our config. </summary>
    /// <returns> True if the warning was an error, false if not.</returns>
    public bool Emit(WarningCode code, Location loc, string message) {
        ErrorLevel level = Config.ErrorConfig[code];
        Emit(new CompilerEmission(level, code, loc, message));
        return level == ErrorLevel.Error;
    }

    /// <summary>
    /// To be used when the compiler MUST ALWAYS give an error. <br/>
    /// Completely ignores the warning configuration. Use wisely!
    /// </summary>
    public void ForcedError(string message) {
        ForcedError(Location.Internal, message);
    }

    /// <inheritdoc cref="ForcedError(string)"/>
    public void ForcedError(Location loc, string message) {
        Console.WriteLine(new CompilerEmission(ErrorLevel.Error, loc, message).ToString());
        ErrorCount++;
    }

    /// <summary>
    /// To be used when the compiler MUST ALWAYS give a warning. <br/>
    /// Completely ignores the warning configuration. Use wisely!
    /// </summary>
    public void ForcedWarning(string message) {
        Console.WriteLine(new CompilerEmission(ErrorLevel.Warning, Location.Internal, message).ToString());
        WarningCount++;
    }

    /// <inheritdoc cref="ForcedWarning(string)"/>
    public void ForcedWarning(Location loc, string message) {
        Console.WriteLine(new CompilerEmission(ErrorLevel.Warning, loc, message).ToString());
        WarningCount++;
    }

    public void UnimplementedWarning(Location loc, string message) {
        if (Settings.SuppressUnimplementedWarnings)
            return;

        Emit(WarningCode.UnimplementedAccess, loc, message);
    }

    public void VerbosePrint(string message) {
        if (!Settings.Verbose) return;

        TimeSpan duration = DateTime.Now - _compileStartTime;
        Console.WriteLine($"{duration.ToString(@"mm\:ss\.fffffff")}: {message}");
    }

    private List<DreamMapJson> ConvertMaps(DMCompiler compiler, List<string> mapPaths) {
        List<DreamMapJson> maps = new();
        int zOffset = 0;

        foreach (string mapPath in mapPaths) {
            VerbosePrint($"Converting map {mapPath}");

            DMPreprocessor preprocessor = new DMPreprocessor(compiler, false);
            preprocessor.PreprocessFile(Path.GetDirectoryName(mapPath), Path.GetFileName(mapPath), false);

            DMLexer lexer = new DMLexer(mapPath, preprocessor);
            DMMParser parser = new DMMParser(this, lexer, zOffset);
            DreamMapJson map = parser.ParseMap();

            zOffset = Math.Max(zOffset + 1, map.MaxZ);
            maps.Add(map);
        }

        return maps;
    }

    private string SaveJson(List<DreamMapJson> maps, string interfaceFile, string outputFile) {
        var jsonRep = DMObjectTree.CreateJsonRepresentation();
        var compiledDream = new DreamCompiledJson {
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

        if (GlobalInitProc.AnnotatedBytecode.GetLength() > 0)
            compiledDream.GlobalInitProc = GlobalInitProc.GetJsonRepresentation();

        if (DMObjectTree.Globals.Count > 0) {
            GlobalListJson globalListJson = new GlobalListJson {
                GlobalCount = DMObjectTree.Globals.Count,
                Names = new(),
                Globals = new()
            };

            globalListJson.Names.EnsureCapacity(globalListJson.GlobalCount);

            // Approximate capacity (4/285 in tgstation, ~3%)
            globalListJson.Globals.EnsureCapacity((int)(DMObjectTree.Globals.Count * 0.03));

            for (int i = 0; i < DMObjectTree.Globals.Count; i++) {
                DMVariable global = DMObjectTree.Globals[i];
                globalListJson.Names.Add(global.Name);

                if (!global.TryAsJsonRepresentation(this, out var globalJson))
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

        // Successful serialization
        if (ErrorCount == 0) {
            using var outputFileHandle = File.Create(outputFile);

            try {
                JsonSerializer.Serialize(outputFileHandle, compiledDream,
                    new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault });

                return $"Saved to {outputFile}";
            } catch (Exception e) {
                Console.WriteLine($"Failed to save to {outputFile}: {e.Message}");
            }
        }

        return string.Empty;
    }

    public void DefineFatalErrors() {
        foreach (WarningCode code in Enum.GetValues<WarningCode>()) {
            if ((int)code < 1_000) {
                Config.ErrorConfig[code] = ErrorLevel.Error;
            }
        }
    }

    /// <summary>
    /// This method also enforces the rule that all emissions with codes less than 1000 are mandatory errors.
    /// </summary>
    public void CheckAllPragmasWereSet() {
        foreach (WarningCode code in Enum.GetValues<WarningCode>()) {
            if (!Config.ErrorConfig.ContainsKey(code)) {
                ForcedWarning($"Warning #{(int)code:d4} '{code.ToString()}' was never declared as error, warning, notice, or disabled.");
                Config.ErrorConfig.Add(code, ErrorLevel.Disabled);
            }
        }
    }

    public void SetPragma(WarningCode code, ErrorLevel level) {
        Config.ErrorConfig[code] = level;
    }

    public ErrorLevel CodeToLevel(WarningCode code) {
        if (!Config.ErrorConfig.TryGetValue(code, out var ret))
            throw new Exception($"Failed to find error level for code {code}");

        return ret;
    }
}

public struct DMCompilerSettings {
    public List<string>? Files = null;
    public bool SuppressUnimplementedWarnings = false;
    /// <summary> Typechecking won't fail if the RHS type is "as anything" to ease migration, thus only emitting for explicit mismatches (e.g. "num" and "text") </summary>
    public bool SkipAnythingTypecheck = false;
    public bool NoticesEnabled = false;
    public bool DumpPreprocessor = false;
    public bool NoStandard = false;
    public bool Verbose = false;
    public bool PrintCodeTree = false;
    public Dictionary<string, string>? MacroDefines = null;
    /// <summary> A user-provided pragma config file, if one was provided. </summary>
    public string? PragmaFileOverride = null;

    // These are the default DM_VERSION and DM_BUILD values. They're strings because that's what the preprocessor expects (seriously)
    public string DMVersion = "515";
    public string DMBuild = "1633";

    public DMCompilerSettings() {
    }
}

internal class DMCompilerConfiguration {
    public readonly Dictionary<WarningCode, ErrorLevel> ErrorConfig = new(Enum.GetValues<WarningCode>().Length);

    public void Reset() {
        ErrorConfig.Clear();
    }
}
