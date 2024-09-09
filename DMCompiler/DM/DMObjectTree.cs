using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;
using DMCompiler.Compiler.DM.AST;
using DMCompiler.Json;

namespace DMCompiler.DM;

internal static class DMObjectTree {
    public static readonly List<DMObject> AllObjects = new();
    public static readonly List<DMProc> AllProcs = new();

    //TODO: These don't belong in the object tree
    public static readonly List<DMVariable> Globals = new();
    public static readonly Dictionary<string, int> GlobalProcs = new();
    /// <summary>
    /// Used to keep track of when we see a /proc/foo() or whatever, so that duplicates or missing definitions can be discovered,
    /// even as GlobalProcs keeps clobbering old global proc overrides/definitions.
    /// </summary>
    public static readonly HashSet<string> SeenGlobalProcDefinition = new();
    public static readonly List<string> StringTable = new();
    public static DMProc GlobalInitProc = default!; // Initialized by Reset() (called in the static initializer)
    public static readonly HashSet<string> Resources = new();

    public static DMObject Root => GetDMObject(DreamPath.Root)!;

    private static readonly Dictionary<string, int> StringToStringId = new();
    private static readonly List<(int GlobalId, DMExpression Value)> _globalInitAssigns = new();

    private static readonly Dictionary<DreamPath, int> _pathToTypeId = new();
    private static int _dmObjectIdCounter;
    private static int _dmProcIdCounter;

    static DMObjectTree() {
        Reset();
    }

    /// <summary>
    /// A thousand curses upon you if you add a new member to this thing without deleting it here.
    /// </summary>
    public static void Reset() {
        AllObjects.Clear();
        AllProcs.Clear();

        Globals.Clear();
        GlobalProcs.Clear();
        SeenGlobalProcDefinition.Clear();
        StringTable.Clear();
        StringToStringId.Clear();
        Resources.Clear();

        _globalInitAssigns.Clear();
        _pathToTypeId.Clear();
        _dmObjectIdCounter = 0;
        _dmProcIdCounter = 0;
        GlobalInitProc = new(-1, Root, null);
    }

    public static int AddString(string value) {
        if (!StringToStringId.TryGetValue(value, out var stringId)) {
            stringId = StringTable.Count;

            StringTable.Add(value);
            StringToStringId.Add(value, stringId);
        }

        return stringId;
    }

    public static DMProc CreateDMProc(DMObject dmObject, DMASTProcDefinition? astDefinition) {
        DMProc dmProc = new DMProc(_dmProcIdCounter++, dmObject, astDefinition);
        AllProcs.Add(dmProc);

        return dmProc;
    }

    /// <summary>
    /// Returns the "New()" DMProc for a given object type ID
    /// </summary>
    /// <returns></returns>
    public static DMProc GetNewProc(int id) {
        var obj = AllObjects[id];
        var targetProc = obj!.GetProcs("New")[0];
        return AllProcs[targetProc];
    }

    public static DMObject? GetDMObject(DreamPath path, bool createIfNonexistent = true) {
        if (_pathToTypeId.TryGetValue(path, out int typeId)) {
            return AllObjects[typeId];
        }
        if (!createIfNonexistent) return null;

        DMObject? parent = null;
        if (path.Elements.Length > 1) {
            parent = GetDMObject(path.FromElements(0, -2)); // Create all parent classes as dummies, if we're being dummy-created too
        } else if (path.Elements.Length == 1) {
            switch (path.LastElement) {
                case "client":
                case "datum":
                case "list":
                case "savefile":
                case "world":
                    parent = GetDMObject(DreamPath.Root);
                    break;
                default:
                    parent = GetDMObject(DMCompiler.Settings.NoStandard ? DreamPath.Root : DreamPath.Datum);
                    break;
            }
        }

        if (path != DreamPath.Root && parent == null) // Parent SHOULD NOT be null here! (unless we're root lol)
            throw new Exception($"Type {path} did not have a parent");

        DMObject dmObject = new DMObject(_dmObjectIdCounter++, path, parent);
        AllObjects.Add(dmObject);
        _pathToTypeId[path] = dmObject.Id;
        return dmObject;
    }

    public static bool TryGetGlobalProc(string name, [NotNullWhen(true)] out DMProc? proc) {
        if (!GlobalProcs.TryGetValue(name, out var id)) {
            proc = null;
            return false;
        }

        proc = AllProcs[id];
        return true;
    }

    /// <returns>True if the path exists, false if not. Keep in mind though that we may just have not found this object path yet while walking in ObjectBuilder.</returns>
    public static bool TryGetTypeId(DreamPath path, out int typeId) {
        return _pathToTypeId.TryGetValue(path, out typeId);
    }

    // TODO: This is all so snowflake and needs redone
    public static DreamPath? UpwardSearch(DreamPath path, DreamPath search) {
        bool requireProcElement = search.Type == DreamPath.PathType.Absolute;
        string? searchingProcName = null;

        int procElement = path.FindElement("proc");
        if (procElement == -1) procElement = path.FindElement("verb");
        if (procElement != -1) {
            searchingProcName = search.LastElement;
            path = path.RemoveElement(procElement);
            search = search.FromElements(0, -2);
            search.Type = DreamPath.PathType.Relative;
        }

        procElement = search.FindElement("proc");
        if (procElement == -1) procElement = search.FindElement("verb");
        if (procElement != -1) {
            searchingProcName = search.LastElement;
            search = search.FromElements(0, procElement);
            search.Type = DreamPath.PathType.Relative;
        }

        if (searchingProcName == null && requireProcElement)
            return null;

        DreamPath currentPath = path;
        while (true) {
            bool foundType = _pathToTypeId.TryGetValue(currentPath.Combine(search), out var foundTypeId);

            // We're searching for a proc
            if (searchingProcName != null && foundType) {
                DMObject type = AllObjects[foundTypeId];

                if (type.HasProc(searchingProcName)) {
                    return new DreamPath(type.Path.PathString + "/proc/" + searchingProcName);
                } else if (foundTypeId == Root.Id && GlobalProcs.ContainsKey(searchingProcName)) {
                    return new DreamPath("/proc/" + searchingProcName);
                }
            } else if (foundType) { // We're searching for a type
                return currentPath.Combine(search);
            }

            if (currentPath == DreamPath.Root) {
                break; // Nothing found
            }

            currentPath = currentPath.AddToPath("..");
        }

        return null;
    }

    public static int CreateGlobal(out DMVariable global, DreamPath? type, string name, bool isConst, DMComplexValueType valType) {
        int id = Globals.Count;

        global = new DMVariable(type, name, true, isConst, false, valType);
        Globals.Add(global);
        return id;
    }

    public static void AddGlobalProc(DMProc proc) {
        // Said in this way so it clobbers previous definitions of this global proc (the ..() stuff doesn't work with glob procs)
        GlobalProcs[proc.Name] = proc.Id;
    }

    public static void AddGlobalInitAssign(int globalId, DMExpression value) {
        _globalInitAssigns.Add( (globalId, value) );
    }

    public static void CreateGlobalInitProc() {
        if (_globalInitAssigns.Count == 0) return;

        foreach (var assign in _globalInitAssigns) {
            GlobalInitProc.DebugSource(assign.Value.Location);

            assign.Value.EmitPushValue(Root, GlobalInitProc);
            GlobalInitProc.Assign(DMReference.CreateGlobal(assign.GlobalId));
        }

        GlobalInitProc.ResolveLabels();
    }

    public static (DreamTypeJson[], ProcDefinitionJson[]) CreateJsonRepresentation() {
        DreamTypeJson[] types = new DreamTypeJson[AllObjects.Count];
        ProcDefinitionJson[] procs = new ProcDefinitionJson[AllProcs.Count];

        foreach (DMObject dmObject in AllObjects) {
            types[dmObject.Id] = dmObject.CreateJsonRepresentation();
        }

        foreach (DMProc dmProc in AllProcs) {
            procs[dmProc.Id] = dmProc.GetJsonRepresentation();
        }

        return (types, procs);
    }
}
