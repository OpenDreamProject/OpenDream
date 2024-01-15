using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OpenDreamShared.Json;

namespace DMDisassembler;

internal class Program {
    public static DreamCompiledJson CompiledJson;
    public static DMProc GlobalInitProc = null;
    public static List<DMProc> Procs = null;
    public static Dictionary<string, DMType> AllTypes = null;

    private static readonly string NoTypeSelectedMessage = "No type is selected";

    private static DMType _selectedType = null;

    static void Main(string[] args) {
        if (args.Length == 0 || Path.GetExtension(args[0]) != ".json") {
            Console.WriteLine("The json output of DMCompiler must be provided as an argument");

            return;
        }

        string compiledJsonText = File.ReadAllText(args[0]);

        CompiledJson = JsonSerializer.Deserialize<DreamCompiledJson>(compiledJsonText);
        if (CompiledJson.GlobalInitProc != null) GlobalInitProc = new DMProc(CompiledJson.GlobalInitProc);
        LoadAllProcs();
        LoadAllTypes();

        if(args.Length > 1) {
            string command = args[1].ToLower();
            switch (command) {
                case "search": Search(args); break;
                case "sel":
                case "select": Select(args); break;
                case "list": List(args); break;
                case "d":
                case "decompile": Decompile(args); break;
                case "dump-all": DumpAll(); break;
                case "test-all": TestAll(); break;
                default: Console.WriteLine("Invalid command \"" + command + "\""); break;
            }
            return;
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
                case "q": acceptingCommands = false; break;
                case "search": Search(split); break;
                case "sel":
                case "select": Select(split); break;
                case "list": List(split); break;
                case "d":
                case "decompile": Decompile(split); break;
                case "dump-all": DumpAll(); break;
                case "test-all": TestAll(); break;
                default: Console.WriteLine("Invalid command \"" + command + "\""); break;
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

    private static void TestAll() {
        int errored = 0, all = 0;
        foreach (DMProc proc in Procs) {
            string value = proc.Decompile();
            if (proc.exception != null) {
                Console.WriteLine("Error disassembling " + proc.Name);
                Console.WriteLine(value);
                ++errored;
            }
            ++all;
        }
        Console.WriteLine($"Errors in {errored}/{all} procs");
    }

    private static void DumpAll() {
        foreach (DMProc proc in Procs) {
            string value = proc.Decompile();
            if (proc.exception != null) {
                Console.WriteLine("Error disassembling " + proc.Name);
                Console.WriteLine(value);
            }
        }
    }
}
