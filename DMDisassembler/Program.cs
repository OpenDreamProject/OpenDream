using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using DMCompiler.Bytecode;
using DMCompiler.Json;
using JetBrains.Annotations;

namespace DMDisassembler;

internal class Program {
    public static string JsonFile = string.Empty;
    public static DreamCompiledJson CompiledJson;
    public static DMProc GlobalInitProc = null;
    public static List<DMProc> Procs = null;
    public static Dictionary<string, DMType> AllTypes = null;
    public static List<DMType> TypesById = null;

    private static readonly string NoTypeSelectedMessage = "No type is selected";

    private static DMType _selectedType = null;

    static void Main(string[] args) {
        if (args.Length == 0 || Path.GetExtension(args[0]) != ".json") {
            Console.WriteLine("The json output of DMCompiler must be provided as an argument");
            Environment.Exit(1);
        }

        JsonFile = args[0];

        string compiledJsonText = File.ReadAllText(args[0]);

        CompiledJson = JsonSerializer.Deserialize<DreamCompiledJson>(compiledJsonText);
        if (CompiledJson.GlobalInitProc != null) GlobalInitProc = new DMProc(CompiledJson.GlobalInitProc);
        LoadAllProcs();
        LoadAllTypes();

        if (args.Length == 2) {
            if (args[1] == "crash-on-test") {
                // crash-on-test is a special mode used by CI to
                // verify that an entire codebase can be disassembled without errors
                int errors = TestAll();

                if (errors > 0) {
                    Console.WriteLine($"Detected {errors} errors. Exiting.");
                    Environment.Exit(1);
                } else {
                    Console.WriteLine("No errors detected. Exiting cleanly.");
                    Environment.Exit(0);
                }
            } else if (args[1] == "dump-all") {
                DumpAll();
                Environment.Exit(0);
            }
        }

        Console.WriteLine("DM Disassembler for OpenDream. Enter a command or \"help\" for more information.");

        bool acceptingCommands = true;
        while (acceptingCommands) {
            if (_selectedType != null) {
                Console.Write(_selectedType.Path);
            }

            Console.Write("> ");

            string input = Console.ReadLine();
            if (input == null) {
                // EOF
                break;
            }

            string[] split = input.Split(" ");
            string command = split[0].ToLower();

            switch (command) {
                case "quit":
                case "exit":
                case "q": acceptingCommands = false; break;
                case "search": Search(split); break;
                case "sel":
                case "select": Select(split); break;
                case "list": List(split); break;
                case "d":
                case "decompile": Decompile(split); break;
                case "stats": Stats(GetArg()); break;
                case "dump-types": DumpTypes(); break;
                case "test-all": TestAll(); break;
                case "dump-all": DumpAll(); break;
                case "help": {
                    PrintHelp(GetArg());
                    break;
                }
                default: Console.WriteLine($"Invalid command \"{command}\""); break;
            }

            [CanBeNull]
            string GetArg() {
                if (split.Length > 2) {
                    Console.WriteLine($"Command \"{command}\" takes 0 or 1 arguments. Ignoring extra arguments.");
                }

                return split.Length > 1 ? split[1] : null;
            }
        }
    }

    private static void PrintHelp([CanBeNull] string command) {
        if (string.IsNullOrEmpty(command)) {
            AllCommands();
            return;
        }

        command = command.ToLower();

        switch (command) {
            case "stats": {
                Console.WriteLine("Prints various statistics. Usage: stats [type]");
                Console.WriteLine("Options for [type]:");
                Console.WriteLine("procs-by-type         : Prints the number of proc declarations (not overrides) on each type in descending order");
                Console.WriteLine("subtypes-by-type      : Prints the number of direct-descendant subtypes on each type in descending order");
                Console.WriteLine("opcode-count          : Prints the number of occurrences for each opcode in descending order");
                break;
            }
            default: {
                Console.WriteLine($"No additional help for \"{command}\"");
                AllCommands();
                break;
            }
        }

        void AllCommands() {
            Console.WriteLine("DM Disassembler for OpenDream");
            Console.WriteLine("Commands and arguments:");
            Console.WriteLine("help [command]            : Show additional help for [command] if applicable");
            Console.WriteLine("exit|quit|q               : Exits the disassembler");
            Console.WriteLine("search type|proc [name]   : Search for a particular typepath or a proc on a selected type");
            Console.WriteLine("select|sel                : Select a typepath to run further commands on");
            Console.WriteLine("list procs|globals        : List all globals, or all procs on a selected type");
            Console.WriteLine("decompile|d [name]        : Decompiles the proc on the selected type");
            Console.WriteLine("stats [type]              : Prints various stats about the game. Use \"help stats\" for more info");
            Console.WriteLine("dump-types                : Writes a list of every type to a file");
            Console.WriteLine("dump-all                  : Decompiles every proc and writes the output to a file");
            Console.WriteLine("test-all                  : Tries to decompile every single proc to check for issues with this disassembler; not for production use");
        }
    }

    private static void Stats([CanBeNull] string statType) {
        if (string.IsNullOrEmpty(statType)) {
            PrintHelp("stats");
            return;
        }

        switch (statType) {
            case "procs-by-type": {
                ProcsByType();
                return;
            }
            case "subtypes-by-type": {
                SubtypesByType();
                return;
            }
            case "opcode-count": {
                OpcodeCount();
                return;
            }
            default: {
                Console.WriteLine($"Unknown stat \"{statType}\"");
                PrintHelp("stats");
                return;
            }
        }

        void ProcsByType() {
            Console.WriteLine("Counting all proc declarations (no overrides) by type. This may take a moment.");
            Dictionary<int, int> typeIdToProcCount = new Dictionary<int, int>();
            foreach (DMProc proc in Procs) {
                if(proc.IsOverride || proc.Name == "<init>") continue; // Don't count overrides or <init> procs
                if (typeIdToProcCount.TryGetValue(proc.OwningTypeId, out var count)) {
                    typeIdToProcCount[proc.OwningTypeId] = count + 1;
                } else {
                    typeIdToProcCount[proc.OwningTypeId] = 1;
                }
            }

            Console.WriteLine("Type: Proc Declarations");
            foreach (var pair in typeIdToProcCount.OrderByDescending(kvp => kvp.Value)) {
                var type = TypesById[pair.Key];
                if (pair.Key == 0) {
                    Console.WriteLine($"<global>: {pair.Value:n0}");
                } else {
                    Console.WriteLine($"{type.Path}: {pair.Value:n0}");
                }
            }
        }

        void SubtypesByType() {
            Console.WriteLine("Counting all subtypes by type. This may take a moment.");
            Dictionary<int, int> typeIdToSubtypeCount = new Dictionary<int, int>(TypesById.Count);

            foreach (DMType type in TypesById) {
                var parent = type.Json.Parent;
                if (parent is null) continue;

                if (typeIdToSubtypeCount.TryGetValue(parent.Value, out var count)) {
                    typeIdToSubtypeCount[parent.Value] = count + 1;
                } else {
                    typeIdToSubtypeCount[parent.Value] = 1;
                }
            }

            var outputFile = Path.ChangeExtension(JsonFile, ".txt")!;
            var name = Path.GetFileName(outputFile);
            outputFile = outputFile.Replace(name!, $"__od_subtypes-by-type_{name}");
            using StreamWriter writer = new StreamWriter(outputFile, append: false, encoding: Encoding.UTF8, bufferSize: 65536);

            writer.WriteLine("Type: Subtype Count");
            foreach (var pair in typeIdToSubtypeCount.OrderByDescending(kvp => kvp.Value)) {
                var type = TypesById[pair.Key];
                if (pair.Key == 0) {
                    writer.WriteLine($"<global>: {pair.Value:n0}");
                } else {
                    writer.WriteLine($"{type.Path}: {pair.Value:n0}");
                }
            }

            Console.WriteLine($"Successfully dumped subtypes-by-type to {outputFile}");
        }

        void OpcodeCount() {
            Console.WriteLine("Counting all opcode occurrences. This may take a moment.");
            Dictionary<string, int> opcodeToCount = new Dictionary<string, int>();

            // We need to fill the dict first in case there's any opcodes with 0 occurrences in the bytecode
            foreach (string opcodeName in Enum.GetNames(typeof(DreamProcOpcode))) {
                opcodeToCount.Add(opcodeName, 0);
            }

            foreach (DMProc proc in Procs) {
                var decompiledOpcodes = proc.GetDecompiledOpcodes(out _);
                foreach (var opcode in decompiledOpcodes) {
                    var name = opcode.Text.Split(' ')[0];
                    opcodeToCount[name] += 1;
                }
            }

            Console.WriteLine("Opcode: Count");
            foreach (var pair in opcodeToCount.OrderByDescending(kvp => kvp.Value)) {
                Console.WriteLine($"{pair.Key}: {pair.Value:n0}");
            }
        }
    }

    private static void Search(string[] args) {
        if (args.Length < 3) {
            Console.WriteLine("search type|proc [name]");

            return;
        }

        string type = args[1];
        string name = args[2];
        if (type == "type") {
            foreach (string typePath in AllTypes.Keys) {
                if (typePath.Contains(name)) Console.WriteLine(typePath);
            }
        } else if (type == "proc") {
            if (_selectedType == null) {
                Console.WriteLine(NoTypeSelectedMessage);

                return;
            }

            foreach (string procName in _selectedType.Procs.Keys) {
                if (procName.Contains(name)) Console.WriteLine(procName);
            }
        } else {
            Console.WriteLine("Invalid search type \"" + type + "\"");
        }
    }

    private static void Select(string[] args) {
        if (args.Length < 2) {
            Console.WriteLine("select [type]");

            return;
        }

        string type = args[1];
        if (AllTypes.TryGetValue(type, out DMType dmType)) {
            _selectedType = dmType;
        } else {
            Console.WriteLine("Invalid type \"" + type + "\"");
        }
    }

    private static void List(string[] args) {
        if (args.Length < 2) {
            Console.WriteLine("list procs|globals");

            return;
        }

        string what = args[1];
        switch (what) {
            case "procs":
                if (_selectedType == null) {
                    Console.WriteLine(NoTypeSelectedMessage);
                    break;
                }

                foreach (string procName in _selectedType.Procs.Keys) {
                    Console.WriteLine(procName);
                }

                break;
            case "globals":
                if (CompiledJson.Globals == null) {
                    Console.WriteLine("There are no globals");
                    break;
                }

                for (int i = 0; i < CompiledJson.Globals.GlobalCount; i++) {
                    Console.Write(i);
                    Console.Write(": ");
                    Console.WriteLine(CompiledJson.Globals.Names[i]);
                }

                break;
        }
    }

    private static void Decompile(string[] args) {
        if (args.Length < 2) {
            Console.WriteLine("decompile [name]");

            return;
        }

        string name = args[1];
        if (name == "<global_init>" || (name == "<init>" && (_selectedType == null || _selectedType.Path == "/"))) {
            if (GlobalInitProc != null) {
                Console.WriteLine(GlobalInitProc.Decompile());
            } else {
                Console.WriteLine("There is no global init proc");
            }

            return;
        }

        if (_selectedType == null) {
            Console.WriteLine(NoTypeSelectedMessage);
            return;
        }

        if (name == "<init>") {
            if (_selectedType.InitProc != null) {
                Console.WriteLine(_selectedType.InitProc.Decompile());
            } else {
                Console.WriteLine("Selected type does not have an init proc");
            }
        } else if (_selectedType.Procs.TryGetValue(name, out DMProc proc)) {
            Console.WriteLine(proc.Decompile());
        } else {
            Console.WriteLine("No procs named \"" + name + "\"");
        }
    }

    private static void LoadAllProcs() {
        Procs = new List<DMProc>(CompiledJson.Procs.Length);

        foreach (ProcDefinitionJson procDef in CompiledJson.Procs) {
            Procs.Add(new DMProc(procDef));
        }
    }

    private static void LoadAllTypes() {
        AllTypes = new Dictionary<string, DMType>(CompiledJson.Types.Length);
        TypesById = new List<DMType>(CompiledJson.Types.Length);

        foreach (DreamTypeJson json in CompiledJson.Types) {
            var dmType = new DMType(json);
            AllTypes.Add(json.Path, dmType);
            TypesById.Add(dmType);
        }

        //Add global procs to the root type
        if (CompiledJson.GlobalProcs != null) {
            DMType globalType = AllTypes["/"];
            foreach (int procId in CompiledJson.GlobalProcs) {
                var proc = Procs[procId];

                globalType.Procs.Add(proc.Name, proc);
            }
        }
    }

    private static int TestAll() {
        int errored = 0, all = 0;
        foreach (DMProc proc in Procs) {
            string value = proc.Decompile();
            if (proc.exception != null) {
                Console.WriteLine("Error disassembling " + PrettyPrintPath(proc));
                Console.WriteLine(value);
                ++errored;
            }

            ++all;
        }

        Console.WriteLine($"Errors in {errored}/{all} procs");
        return errored;
    }

    private static void DumpTypes() {
        Console.WriteLine("Dumping all types. This may take a moment.");

        var outputFile = Path.ChangeExtension(JsonFile, ".txt")!;
        var name = Path.GetFileName(outputFile);
        outputFile = outputFile.Replace(name!, $"__od_types_{name}");
        using StreamWriter writer = new StreamWriter(outputFile, append: false, encoding: Encoding.UTF8, bufferSize: 65536);

        foreach (DMType type in TypesById) {
                writer.WriteLine(type.Path);
        }

        Console.WriteLine($"Successfully dumped {TypesById.Count:n0} types to {outputFile}");
    }

    private static void DumpAll() {
        Console.WriteLine("Dumping all procs. This may take a moment.");
        int errored = 0, all = 0;
        // ".dmd" for "dm disassembly"
        var outputFile = Path.ChangeExtension(JsonFile, ".dmd")!;
        using StreamWriter writer = new StreamWriter(outputFile, append: false, encoding: Encoding.UTF8, bufferSize: 65536);

        foreach (DMProc proc in Procs) {
            string value = proc.Decompile();
            if (proc.exception != null) {
                Console.WriteLine("Error disassembling " + PrettyPrintPath(proc));
                ++errored;
            } else {
                writer.WriteLine(PrettyPrintPath(proc) + ":");
                writer.WriteLine(value);
            }

            ++all;
        }

        var procCount = errored > 0 ? $"{(all - errored):n0}/{all:n0} ({errored:n0} failed procs)" : $"all {all:n0}";
        Console.WriteLine($"Successfully dumped {procCount} procs to {outputFile}");
    }

    private static string PrettyPrintPath(DMProc proc) {
        var path = CompiledJson.Types![proc.OwningTypeId].Path;
        var args = proc.GetArguments();

        if(args is null)
            return path + (path[^1] == '/' ? "" : "/") + (proc.IsOverride ? "" : "proc/") + proc.Name + "()";
        return path + (path[^1] == '/' ? "" : "/") + (proc.IsOverride ? "" : "proc/") + proc.Name + $"({string.Join(", ", args)})";
    }
}
