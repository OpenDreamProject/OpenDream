using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using DMCompiler.Json;

namespace DMDisassembler;

internal class Program {
    public static string JsonFile = string.Empty;
    public static DreamCompiledJson CompiledJson;
    public static DMProc GlobalInitProc = null;
    public static List<DMProc> Procs = null;
    public static Dictionary<string, DMType> AllTypes = null;

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
                case "q": acceptingCommands = false; break;
                case "search": Search(split); break;
                case "sel":
                case "select": Select(split); break;
                case "list": List(split); break;
                case "d":
                case "decompile": Decompile(split); break;
                case "test-all": TestAll(); break;
                case "dump-all": DumpAll(); break;
                case "help": PrintHelp(); break;
                default: Console.WriteLine("Invalid command \"" + command + "\""); break;
            }
        }
    }

    private static void PrintHelp() {
        Console.WriteLine("DM Disassembler for OpenDream");
        Console.WriteLine("Commands and arguments:");
        Console.WriteLine("help                      : Show this help");
        Console.WriteLine("quit|q                    : Exits the disassembler");
        Console.WriteLine("search type|proc [name]   : Search for a particular typepath or a proc on a selected type");
        Console.WriteLine("select|sel                : Select a typepath to run further commands on");
        Console.WriteLine("list procs|globals        : List all globals, or all procs on a selected type");
        Console.WriteLine("decompile|d [name]        : Decompiles the proc on the selected type");
        Console.WriteLine("dump-all                  : Decompiles every proc and writes the output to a file");
        Console.WriteLine("test-all                  : Tries to decompile every single proc to check for issues with this disassembler; not for production use");
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

        foreach (DreamTypeJson json in CompiledJson.Types) {
            AllTypes.Add(json.Path, new DMType(json));
        }

        //Add global procs to the root type
        DMType globalType = AllTypes["/"];
        foreach (int procId in CompiledJson.GlobalProcs) {
            var proc = Procs[procId];

            globalType.Procs.Add(proc.Name, proc);
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

        var procCount = errored > 0 ? $"{all - errored}/{all} ({errored} failed procs)" : $"all {all}";
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
