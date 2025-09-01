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
    public readonly HashSet<WarningCode> UniqueEmissions = new();
    public DMCompilerSettings Settings;
    public IReadOnlyCollection<string> ResourceDirectories => _resourceDirectories;

    internal readonly DMCodeTree DMCodeTree;
    internal readonly DMObjectTree DMObjectTree;
    internal readonly DMProc GlobalInitProc;
    internal readonly BytecodeOptimizer BytecodeOptimizer;

    private readonly Dictionary<WarningCode, ErrorLevel> _errorConfig;
    private readonly HashSet<string> _resourceDirectories = new();
    private string? _codeDirectory;
    private DateTime _compileStartTime;
    private int _errorCount;
    private int _warningCount;

    public DMCompiler() {
        DMCodeTree = new(this);
        DMObjectTree = new(this);
        GlobalInitProc = new(this, -1, DMObjectTree.Root, null);
        BytecodeOptimizer = new BytecodeOptimizer(this);
        _errorConfig = new Dictionary<WarningCode, ErrorLevel>(CompilerEmission.DefaultErrorConfig);
    }

    public bool Compile(DMCompilerSettings settings) {
        if (_compileStartTime != default)
            throw new Exception("Create a new DMCompiler to compile again");

        UniqueEmissions.Clear();
        Settings = settings;
        _resourceDirectories.Clear();
        _errorCount = 0;
        _warningCount = 0;
        _compileStartTime = DateTime.Now;

        //TODO: Only use InvariantCulture where necessary instead of it being the default
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

#if DEBUG
        ForcedWarning("This compiler was compiled in the Debug .NET configuration. This will impact compile speed.");
#endif

        if (settings.SuppressUnimplementedWarnings)
            Emit(WarningCode.UnimplementedAccess, Location.Internal,
                "Unimplemented proc & var warnings are currently suppressed");

        if (settings.NoOpts)
            ForcedWarning("Compiler optimizations (const folding, peephole opts, etc.) are disabled via the \"--no-opts\" arg. This results in slower code execution and is not representative of OpenDream performance.");

        var preprocessor = Preprocess(this, settings.Files, settings.MacroDefines);
        var successfulCompile = false;
        if (preprocessor is not null && Compile(preprocessor)) {
            //Output file is the first file with the extension changed to .json
            string outputFile = Path.ChangeExtension(settings.Files[0], "json");
            List<DreamMapJson> maps = ConvertMaps(this, preprocessor.IncludedMaps);

            if (_errorCount == 0) {
                var output = SaveJson(maps, preprocessor.IncludedInterface, outputFile);

                if (_errorCount == 0) {
                    successfulCompile = true;
                    Console.WriteLine($"Compilation succeeded with {_warningCount} warnings");
                    Console.WriteLine(output);
                }
            }
        }

        if (!successfulCompile) {
            Console.WriteLine($"Compilation failed with {_errorCount} errors and {_warningCount} warnings");
        }

        TimeSpan duration = DateTime.Now - _compileStartTime;
        Console.WriteLine($"Total time: {duration:mm\\:ss}");

        return successfulCompile;
    }

    public void AddResourceDirectory(string dir, Location loc) {
        dir = dir.Replace('\\', Path.DirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(dir))
                dir = Path.GetFullPath(".");
        if (!Directory.Exists(dir)) {
            Emit(WarningCode.InvalidFileDirDefine, loc,
                $"Folder \"{Path.GetRelativePath(_codeDirectory ?? ".", dir)}\" does not exist");
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
                compiler.AddResourceDirectory(includeDir, Location.Internal);
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

            return preproc;
        }

        if (Settings.DumpPreprocessor) {
            //Preprocessing is done twice because the output is used up when dumping it
            var preproc = Build();
            if (preproc != null) {
                var result = new StringBuilder();
                foreach (Token t in preproc) {
                    result.Append(t.Text);
                }

                string outputDir = Path.GetDirectoryName(Settings.Files[0]);
                string outputPath = Path.Combine(outputDir, "preprocessor_dump.dm");

                File.WriteAllText(outputPath, result.ToString());
                Console.WriteLine($"Preprocessor output dumped to {outputPath}");
            }
        }

        return Build();
    }

    private bool Compile(IEnumerable<Token> preprocessedTokens) {
        DMLexer dmLexer = new DMLexer("<unknown>", preprocessedTokens);
        DMParser dmParser = new DMParser(this, dmLexer);

        VerbosePrint("Parsing");
        DMASTFile astFile = dmParser.File();

        DMASTFolder astSimplifier = new DMASTFolder();
        VerbosePrint("Constant folding");
        astSimplifier.FoldAst(astFile);

        DMCodeTreeBuilder dmCodeTreeBuilder = new(this);
        dmCodeTreeBuilder.BuildCodeTree(astFile);

        return _errorCount == 0;
    }

    /// <summary> Emits the given warning, according to its ErrorLevel as set in our config. </summary>
    /// <returns> True if the warning was an error, false if not.</returns>
    public bool Emit(WarningCode code, Location loc, string message) {
        if (!_errorConfig.TryGetValue(code, out var level)) {
            ForcedError(loc, $"Unknown warning code \"{code}\". Is it being used before the preprocessor has set it? Emission: \"{message}\"");
            return true;
        }

        var emission = new CompilerEmission(level, code, loc, message);
        switch (emission.Level) {
            case ErrorLevel.Disabled:
                return false;
            case ErrorLevel.Notice:
                if (!Settings.NoticesEnabled)
                    return false;
                break;
            case ErrorLevel.Warning:
                ++_warningCount;
                break;
            case ErrorLevel.Error:
                ++_errorCount;
                break;
        }

        UniqueEmissions.Add(emission.Code);
        Console.WriteLine(emission);
        return level == ErrorLevel.Error;
    }

    /// <summary>
    /// To be used when the compiler MUST ALWAYS give an error. <br/>
    /// Completely ignores the warning configuration. Use wisely!
    /// </summary>
    public void ForcedError(Location loc, string message) {
        Console.WriteLine(new CompilerEmission(ErrorLevel.Error, loc, message).ToString());
        _errorCount++;
    }

    /// <summary>
    /// To be used when the compiler MUST ALWAYS give a warning. <br/>
    /// Completely ignores the warning configuration. Use wisely!
    /// </summary>
    public void ForcedWarning(string message) {
        Console.WriteLine(new CompilerEmission(ErrorLevel.Warning, Location.Internal, message).ToString());
        _warningCount++;
    }

    /// <inheritdoc cref="ForcedWarning(string)"/>
    public void ForcedWarning(Location loc, string message) {
        Console.WriteLine(new CompilerEmission(ErrorLevel.Warning, loc, message).ToString());
        _warningCount++;
    }

    public void UnimplementedWarning(Location loc, string message) {
        if (Settings.SuppressUnimplementedWarnings)
            return;

        Emit(WarningCode.UnimplementedAccess, loc, message);
    }

    public void UnsupportedWarning(Location loc, string message) {
        if (Settings.SuppressUnsupportedAccessWarnings)
            return;

        Emit(WarningCode.UnsupportedAccess, loc, message);
    }

    public void VerbosePrint(string message) {
        if (!Settings.Verbose) return;

        TimeSpan duration = DateTime.Now - _compileStartTime;
        Console.WriteLine($"{duration:mm\\:ss\\.fffffff}: {message}");
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

    private string SaveJson(List<DreamMapJson> maps, string? interfaceFile, string outputFile) {
        if (!string.IsNullOrWhiteSpace(interfaceFile) &&
                Path.GetDirectoryName(Path.GetFullPath(outputFile)) is { } interfaceDirectory) {
            interfaceFile = Path.GetRelativePath(interfaceDirectory, interfaceFile);
            DMObjectTree.Resources.Add(interfaceFile); // Ensure the DMF is included in the list of resources
        } else {
            interfaceFile = string.Empty;
        }

        var optionalErrors = new Dictionary<WarningCode, ErrorLevel>();
        foreach (var (code, level) in _errorConfig) {
            if (((int)code) is >= 4000 and <= 4999) {
                optionalErrors.Add(code, level);
            }
        }

        var jsonRep = DMObjectTree.CreateJsonRepresentation();
        var compiledDream = new DreamCompiledJson {
            Metadata = new DreamCompiledJsonMetadata { Version = OpcodeVerifier.GetOpcodesHash() },
            Strings = DMObjectTree.StringTable,
            Resources = DMObjectTree.Resources.ToArray(),
            Maps = maps,
            Interface = interfaceFile,
            Types = jsonRep.Item1,
            Procs = jsonRep.Item2,
            OptionalErrors = optionalErrors,
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
                    ForcedError(global.Value?.Location ?? Location.Unknown, $"Failed to serialize global {global.Name}");

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
        if (_errorCount == 0) {
            using var outputFileHandle = File.Create(outputFile);

            try {
                JsonSerializer.Serialize(outputFileHandle, compiledDream,
                    new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault });

                return $"Saved to {outputFile}";
            } catch (Exception e) {
                Console.WriteLine($"Failed to save to {outputFile}: {e.Message}");
            }
        }

        return string.Empty;
    }

    public void SetPragma(WarningCode code, ErrorLevel level) {
        _errorConfig[code] = level;
    }
}

public struct DMCompilerSettings {
    public required List<string> Files;
    public bool SuppressUnimplementedWarnings = false;
    public bool SuppressUnsupportedAccessWarnings = false;
    public bool NoticesEnabled = false;
    public bool DumpPreprocessor = false;
    public bool NoStandard = false;
    public bool Verbose = false;
    public bool PrintCodeTree = false;
    public Dictionary<string, string>? MacroDefines = null;

    /// <summary> The value of the DM_VERSION macro </summary>
    public int DMVersion = 516;

    /// <summary> The value of the DM_BUILD macro </summary>
    public int DMBuild = 1655;

    /// <summary> Typechecking won't fail if the RHS type is "as anything" to ease migration, thus only emitting for explicit mismatches (e.g. "num" and "text") </summary>
    public bool SkipAnythingTypecheck = false;

    /// <summary> Disables compiler optimizations such as const-folding and peephole opts </summary>
    public bool NoOpts = false;

    public DMCompilerSettings() {
    }
}
