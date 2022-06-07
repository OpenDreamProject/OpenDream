using System;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System.Collections.Generic;
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
        public static List<string> StringTable = new();
        public static Dictionary<string, int> StringToStringID = new();
        public static DMProc GlobalInitProc;

        private static Dictionary<DreamPath, List<(int GlobalId, DMExpression Value)>> _globalInitAssigns = new();

        private static Dictionary<DreamPath, int> _pathToTypeId = new();
        private static int _dmObjectIdCounter = 0;
        private static int _dmProcIdCounter = 0;

        static DMObjectTree() {
            Reset();
            GlobalInitProc = new(-1, GetDMObject(DreamPath.Root), null);
        }

        public static void Reset() {
            AllObjects.Clear();
            AllProcs.Clear();
            GlobalProcs.Clear();
            _pathToTypeId.Clear();
            _dmObjectIdCounter = 0;
            _dmProcIdCounter = 0;
            GetDMObject(DreamPath.Root);
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
            } else {
                if (!createIfNonexistent) return null;

                DMObject parent = null;
                if (path.Elements.Length > 1) {
                    parent = GetDMObject(path.FromElements(0, -2), true);
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

                DMObject dmObject = new DMObject(_dmObjectIdCounter++, path, parent);
                AllObjects.Add(dmObject);
                _pathToTypeId[path] = dmObject.Id;
                return dmObject;
            }
        }

        public static bool TryGetGlobalProc(string name, [CanBeNull] out DMProc proc)
        {
            proc = null;
            return GlobalProcs.TryGetValue(name, out var id) && AllProcs.TryGetValue(id, out proc);
        }

        public static bool TryGetTypeId(DreamPath path, out int typeId) {
            return _pathToTypeId.TryGetValue(path, out typeId);
        }

        public static DreamPath? UpwardSearch(DreamPath path, DreamPath search) {
            bool requireProcElement = search.Type == DreamPath.PathType.Absolute;

            DreamPath searchObjectPath;

            int procElement = search.FindElement("proc");
            if (procElement == -1) procElement = search.FindElement("verb");
            if (procElement != -1) {
                searchObjectPath = search.FromElements(0, procElement);
                searchObjectPath.Type = DreamPath.PathType.Relative; // FromElements makes an absolute path
            } else {
                if (requireProcElement) return null;

                searchObjectPath = search;
            }

            int foundTypeId;
            DreamPath currentPath = path;
            while (!_pathToTypeId.TryGetValue(currentPath.Combine(searchObjectPath), out foundTypeId)) {
                if (currentPath == DreamPath.Root) break;

                currentPath = currentPath.AddToPath("..");
            }

            DMObject found = AllObjects[foundTypeId];

            //We're searching for a proc
            if (procElement != -1) {
                DreamPath procPath = search.FromElements(procElement + 1);
                if (procPath.Elements.Length != 1 || procPath.LastElement is null) return null;

                if (found.HasProc(procPath.LastElement)) {
                    return new DreamPath(found.Path.PathString + "/proc" + procPath);
                } else {
                    return null;
                }
            } else { //We're searching for an object
                return found?.Path;
            }
        }

        public static int CreateGlobal(out DMVariable global, DreamPath? type, string name, bool isConst) {
            int id = Globals.Count;

            global = new DMVariable(type, name, true, isConst);
            Globals.Add(global);
            return id;
        }

        public static void AddGlobalProc(string name, int id) {
            GlobalProcs.Add(name, id);
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
                        assign.Value.EmitPushValue(root, GlobalInitProc);
                        GlobalInitProc.Assign(DMReference.CreateGlobal(assign.GlobalId));
                    } catch (CompileErrorException e) {
                        DMCompiler.Error(e.Error);
                    }
                }
            }
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
