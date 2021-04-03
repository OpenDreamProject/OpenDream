using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;

namespace DMCompiler.DM {
    static class DMObjectTree {
        public static Dictionary<DreamPath, DMObject> AllObjects = new();

        private static uint _dmObjectIdCounter = 0;

        static DMObjectTree() {
            Clear();
        }

        public static void Clear() {
            AllObjects.Clear();
            GetDMObject(DreamPath.Root);
        }

        public static DMObject GetDMObject(DreamPath path) {
            if (path.IsDescendantOf(DreamPath.List)) path = DreamPath.List;

            DMObject dmObject;

            if (!AllObjects.TryGetValue(path, out dmObject)) {
                DMObject parent = null;
                if (path.Elements.Length > 0) {
                    parent = GetDMObject(path.FromElements(0, -2));
                }

                dmObject = new DMObject(_dmObjectIdCounter++, path, parent);
                AllObjects.Add(path, dmObject);
            }

            return dmObject;
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
