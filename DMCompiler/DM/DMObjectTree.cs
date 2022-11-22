using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DMCompiler.Compiler.DM;
using JetBrains.Annotations;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream.Procs;
using Robust.Shared.Utility;

namespace DMCompiler.DM {
    static class DMObjectTree {
        public static List<DMObject> AllObjects = new();
        public static List<DMProc> AllProcs = new();

        //TODO: These don't belong in the object tree
        public static List<DMVariable> Globals = new();
        public static Dictionary<string, int> GlobalProcs = new();
        /// <summary>
        /// Used to keep track of when we see a /proc/foo() or whatever, so that duplicates or missing definitions can be discovered,
        /// even as GlobalProcs keeps clobbering old global proc overrides/definitions.
        /// </summary>
        public static HashSet<string> SeenGlobalProcDefinition = new();
        public static List<string> StringTable = new();
        public static Dictionary<string, int> StringToStringID = new();
        public static DMProc GlobalInitProc;
        public static DMObject Root => GetDMObject(DreamPath.Root);

        private static Dictionary<DreamPath, List<(int GlobalId, DMExpression Value)>> _globalInitAssigns = new();

        private static Dictionary<DreamPath, int> _pathToTypeId = new();
        private static int _dmObjectIdCounter = 0;
        private static int _dmProcIdCounter = 0;

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
            StringToStringID.Clear();

            _globalInitAssigns.Clear();
            _pathToTypeId.Clear();
            _dmObjectIdCounter = 0;
            _dmProcIdCounter = 0;
            GlobalInitProc = new(-1, GetDMObject(DreamPath.Root), null);
        }

        public static DMProc CreateDMProc(DMObject dmObject, [CanBeNull] DMASTProcDefinition astDefinition)
        {
            DMProc dmProc = new DMProc(_dmProcIdCounter++, dmObject, astDefinition);
            AllProcs.Add(dmProc);

            return dmProc;
        }

        public static DMObject GetDMObject(DreamPath path, bool createIfNonexistent = true) {
            if (_pathToTypeId.TryGetValue(path, out int typeId)) {
                return AllObjects[typeId];
            }
            if (!createIfNonexistent) return null;

            DMObject parent = null;
            if (path.Elements.Length > 1) {
                parent = GetDMObject(path.FromElements(0, -2), true); // Create all parent classes as dummies, if we're being dummy-created too
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

            DebugTools.Assert(path == DreamPath.Root || parent != null); // Parent SHOULD NOT be null here! (unless we're root lol)

            DMObject dmObject = new DMObject(_dmObjectIdCounter++, path, parent);
            AllObjects.Add(dmObject);
            _pathToTypeId[path] = dmObject.Id;
            return dmObject;
        }

        public static bool TryGetGlobalProc(string name, [NotNullWhen(true)] [CanBeNull] out DMProc proc) {
            proc = null;
            return GlobalProcs.TryGetValue(name, out var id) && AllProcs.TryGetValue(id, out proc);
        }

        /// <returns>True if the path exists, false if not. Keep in mind though that we may just have not found this object path yet while walking in ObjectBuilder.</returns>
        public static bool TryGetTypeId(DreamPath path, out int typeId) {
            return _pathToTypeId.TryGetValue(path, out typeId);
        }

        // TODO: This is all so snowflake and needs redone
        public static DreamPath? UpwardSearch(DreamPath path, DreamPath search) {
            bool requireProcElement = search.Type == DreamPath.PathType.Absolute;
            string searchingProcName = null;

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
                    break;
                }

                if (currentPath == DreamPath.Root) {
                    break; // Nothing found
                }

                currentPath = currentPath.AddToPath("..");
            }

            return null;
        }

        public static int CreateGlobal(out DMVariable global, DreamPath? type, string name, bool isConst, DMValueType valType = DMValueType.Anything) {
            int id = Globals.Count;

            global = new DMVariable(type, name, true, isConst, valType);
            Globals.Add(global);
            return id;
        }

        public static void AddGlobalProc(string name, int id) {
            GlobalProcs[name] = id; // Said in this way so it clobbers previous definitions of this global proc (the ..() stuff doesn't work with glob procs)
        }

        public static void AddGlobalInitAssign(DMObject owningType, int globalId, DMExpression value) {
            if (!_globalInitAssigns.TryGetValue(owningType.Path, out var list)) {
                list = new List<(int GlobalId, DMExpression Value)>();

                _globalInitAssigns.Add(owningType.Path, list);
            }

            list.Add( (globalId, value) );
        }

        public static void CreateGlobalInitProc() {
            if (_globalInitAssigns.Count == 0) return;

            DMObject root = GetDMObject(DreamPath.Root);
            foreach (var globals in _globalInitAssigns.Values) {
                foreach (var assign in globals) {
                    try {
                        if (assign.Value.Location.Line is int line) {
                            GlobalInitProc.DebugSource(assign.Value.Location.SourceFile);
                            GlobalInitProc.DebugLine(line);
                        }
                        assign.Value.EmitPushValue(root, GlobalInitProc);
                        GlobalInitProc.Assign(DMReference.CreateGlobal(assign.GlobalId));
                    } catch (CompileErrorException e) {
                        DMCompiler.Emit(e.Error);
                    }
                }
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
}
