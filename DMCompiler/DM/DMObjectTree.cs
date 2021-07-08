using DMCompiler.DM.Visitors;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;

namespace DMCompiler.DM {
    static class DMObjectTree {
        public static Dictionary<DreamPath, DMObject> AllObjects = new();
        public static List<string> StringTable = new();
        public static Dictionary<string, int> StringToStringID = new();
        public static DMProc GlobalInitProc = null;

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

        public static void AddGlobalInitProcAssign(Expressions.Assignment assign) {
            _globalInitProcAssigns.Add(assign);
        }

        public static void CreateGlobalInitProc() {
            if (_globalInitProcAssigns.Count == 0) return;

            GlobalInitProc = new DMProc(null);

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
    }
}
