using System.Diagnostics.CodeAnalysis;
using DMCompiler.Compiler;
using DMCompiler.Compiler.DM.AST;
using DMCompiler.Json;

namespace DMCompiler.DM;

internal class DMObjectTree(DMCompiler compiler) {
    public readonly List<DMObject> AllObjects = new();
    public readonly List<DMProc> AllProcs = new();

    //TODO: These don't belong in the object tree
    public readonly List<DMVariable> Globals = new();
    public readonly Dictionary<string, int> GlobalProcs = new();
    public readonly List<string> StringTable = new();
    public readonly HashSet<string> Resources = new();

    public DMObject Root => GetOrCreateDMObject(DreamPath.Root);

    private readonly Dictionary<string, int> _stringToStringId = new();
    private readonly Dictionary<DreamPath, int> _pathToTypeId = new();
    private int _dmObjectIdCounter;
    private int _dmProcIdCounter;

    public int AddString(string value) {
        if (!_stringToStringId.TryGetValue(value, out var stringId)) {
            stringId = StringTable.Count;

            StringTable.Add(value);
            _stringToStringId.Add(value, stringId);
        }

        return stringId;
    }

    public DMProc CreateDMProc(DMObject dmObject, DMASTProcDefinition? astDefinition) {
        DMProc dmProc = new DMProc(compiler, _dmProcIdCounter++, dmObject, astDefinition);
        AllProcs.Add(dmProc);

        return dmProc;
    }

    /// <summary>
    /// Returns the "New()" DMProc for a given object type ID
    /// </summary>
    /// <returns></returns>
    public DMProc? GetNewProc(int id) {
        var obj = AllObjects[id];
        var procs = obj.GetProcs("New");

        if (procs != null)
            return AllProcs[procs[0]];
        else
            return null;
    }

    public DMObject GetOrCreateDMObject(DreamPath path) {
        if (TryGetDMObject(path, out var dmObject))
            return dmObject;

        DMObject? parent = null;
        if (path.Elements.Length > 1) {
            parent = GetOrCreateDMObject(path.FromElements(0, -2)); // Create all parent classes as dummies, if we're being dummy-created too
        } else if (path.Elements.Length == 1) {
            switch (path.LastElement) {
                case "client":
                case "datum":
                case "list":
                case "vector":
                case "savefile":
                case "world":
                    parent = GetOrCreateDMObject(DreamPath.Root);
                    break;
                default:
                    parent = GetOrCreateDMObject(compiler.Settings.NoStandard ? DreamPath.Root : DreamPath.Datum);
                    break;
            }
        }

        if (path != DreamPath.Root && parent == null) // Parent SHOULD NOT be null here! (unless we're root lol)
            throw new Exception($"Type {path} did not have a parent");

        dmObject = new DMObject(compiler, _dmObjectIdCounter++, path, parent);
        AllObjects.Add(dmObject);
        _pathToTypeId[path] = dmObject.Id;
        return dmObject;
    }

    public bool TryGetDMObject(DreamPath path, [NotNullWhen(true)] out DMObject? dmObject) {
        if (_pathToTypeId.TryGetValue(path, out int typeId)) {
            dmObject = AllObjects[typeId];
            return true;
        }

        dmObject = null;
        return false;
    }

    public bool TryGetGlobalProc(string name, [NotNullWhen(true)] out DMProc? proc) {
        if (!GlobalProcs.TryGetValue(name, out var id)) {
            proc = null;
            return false;
        }

        proc = AllProcs[id];
        return true;
    }

    /// <returns>True if the path exists, false if not. Keep in mind though that we may just have not found this object path yet while walking in ObjectBuilder.</returns>
    public bool TryGetTypeId(DreamPath path, out int typeId) {
        return _pathToTypeId.TryGetValue(path, out typeId);
    }

    // TODO: This is all so snowflake and needs redone
    public DreamPath? UpwardSearch(DreamPath path, DreamPath search) {
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

    public int CreateGlobal(out DMVariable global, DreamPath? type, string name, bool isConst, bool isFinal, DMComplexValueType? valType) {
        int id = Globals.Count;

        global = new DMVariable(type, name, true, isConst, isFinal, false, valType);
        Globals.Add(global);
        return id;
    }

    public void AddGlobalProc(DMProc proc) {
        if (GlobalProcs.ContainsKey(proc.Name)) {
            compiler.Emit(WarningCode.DuplicateProcDefinition, proc.Location, $"Global proc {proc.Name} is already defined");
            return;
        }

        GlobalProcs[proc.Name] = proc.Id;
    }

    public (DreamTypeJson[], ProcDefinitionJson[]) CreateJsonRepresentation() {
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
