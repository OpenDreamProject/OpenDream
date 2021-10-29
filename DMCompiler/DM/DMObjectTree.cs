using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Compiler;
using System;
using System.Collections.Generic;

namespace DMCompiler.DM {
    static class DMObjectTree {
        public static Dictionary<DreamPath, DMObject> AllObjects = new();
        public static List<string> StringTable = new();
        public static Dictionary<string, int> StringToStringID = new();
        public static DMProc GlobalInitProc = new DMProc(null);

        private static List<Expressions.Assignment> _globalInitProcAssigns = new();

        private static uint _dmObjectIdCounter = 0;

        static DMObjectTree() {
            Reset();
        }

        public static void Reset() {
            AllObjects.Clear();
            GetDMObject(DreamPath.Root);
        }

        public static DMObject GetDMObject(DreamPath path, bool createIfNonexistent = true) {
            if (path.IsDescendantOf(DreamPath.List)) path = DreamPath.List;

            DMObject dmObject;

            if (!AllObjects.TryGetValue(path, out dmObject)) {
                if (!createIfNonexistent) throw new Exception("Type " + path + " does not exist");

                DMObject parent = null;
                if (path.Elements.Length > 0) {
                    parent = GetDMObject(path.FromElements(0, -2), createIfNonexistent);
                }

                dmObject = new DMObject(_dmObjectIdCounter++, path, parent);
                AllObjects.Add(path, dmObject);
            }

            return dmObject;
        }

        public static DreamPath? UpwardSearch(DreamPath path, DreamPath search) {
            // I was unable to find any situation where searching for an absolute path worked
            if (search.Type == DreamPath.PathType.Absolute) return null;

            DreamPath searchObjectPath;

            int procElement = search.FindElement("proc");
            if (procElement == -1) procElement = search.FindElement("verb");
            if (procElement != -1) {
                searchObjectPath = search.FromElements(0, procElement);
                searchObjectPath.Type = DreamPath.PathType.Relative; // FromElements makes an absolute path
            } else {
                searchObjectPath = search;
            }

            DMObject found;
            DreamPath currentPath = path;
            while (!AllObjects.TryGetValue(currentPath.Combine(searchObjectPath), out found)) {
                if (currentPath == DreamPath.Root) break;

                currentPath = currentPath.AddToPath("..");
            }

            //We're searching for a proc
            if (procElement != -1) {
                DreamPath procPath = search.FromElements(procElement + 1);
                if (procPath.Elements.Length != 1) return null;

                if (found.HasProc(procPath.LastElement)) {
                    return new DreamPath(found.Path.PathString + "/proc" + procPath);
                } else {
                    return null;
                }
            } else { //We're searching for an object
                return found?.Path;
            }
        }

        public static void AddGlobalInitProcAssign(Expressions.Assignment assign) {
            _globalInitProcAssigns.Add(assign);
        }

        public static void CreateGlobalInitProc() {
            if (_globalInitProcAssigns.Count == 0) return;

            DMObject root = GetDMObject(DreamPath.Root);
            foreach (Expressions.Assignment assign in _globalInitProcAssigns) {
                assign.EmitPushValue(root, GlobalInitProc);
            }
        }

        public static DreamObjectJson CreateJsonRepresentation() {
            Dictionary<DreamPath, DreamObjectJson> jsonObjects = new();
            Queue<(DMObject, DreamObjectJson)> unparentedObjects = new();

            foreach (KeyValuePair<DreamPath, DMObject> dmObject in AllObjects) {
                DreamObjectJson jsonObject = dmObject.Value.CreateJsonRepresentation();

                jsonObjects.Add(dmObject.Key, jsonObject);
                if (!dmObject.Key.Equals(DreamPath.Root)) {
                    unparentedObjects.Enqueue((dmObject.Value, jsonObject));
                }
            }

            while (unparentedObjects.Count > 0) {
                (DMObject, DreamObjectJson) unparentedObject = unparentedObjects.Dequeue();
                DreamPath treeParentPath = unparentedObject.Item1.Path.FromElements(0, -2);

                if (jsonObjects.TryGetValue(treeParentPath, out DreamObjectJson treeParent)) {
                    if (treeParent.Children == null) treeParent.Children = new List<DreamObjectJson>();

                    treeParent.Children.Add(unparentedObject.Item2);
                    if (unparentedObject.Item1.Parent != null && unparentedObject.Item1.Parent?.Path.Equals(treeParentPath) == true) {
                        unparentedObject.Item2.Parent = null; //Parent type can be assumed
                    }
                } else {
                    throw new Exception("Invalid object path \"" + unparentedObject.Item1.Path + "\"");
                }
            }

            return jsonObjects[DreamPath.Root];
        }

        static public HashSet<string> const_procs = new() { "rgb", "matrix" };
        static public bool ConstProc(string s) {
            return const_procs.Contains(s);
        }

        static public bool CheckConst(DMASTExpression expr, DMObject obj, DMProc proc) {
            try {
                DMExpression.Constant(obj, proc, expr).ToConstant();
                return true;
            }
            catch (CompileErrorException) {
                return false;
            }
        }

        static public bool CheckConstExpression(DMASTExpression value, DMObject dm_object, DMProc dm_proc) {
            if (value is DMASTProcCall ast_proc && ast_proc.Callable is DMASTCallableProcIdentifier ast_callable && DMObjectTree.ConstProc(ast_callable.Identifier)) {
                bool all_const = true;
                foreach (var expr in ast_proc.Parameters) {
                    if (!DMObjectTree.CheckConst(expr.Value, dm_object, dm_proc)) {
                        all_const = false;
                    }
                }
                return all_const;
            }
            if (value is DMASTNewList newl) {
                bool all_const = true;
                foreach (var expr in newl.Parameters) {
                    if (!DMObjectTree.CheckConst(expr.Value, dm_object, dm_proc)) {
                        all_const = false;
                    }
                }
                return all_const;
            }
            return false;
        }
    }
}
