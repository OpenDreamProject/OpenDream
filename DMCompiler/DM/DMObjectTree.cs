using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;

namespace DMCompiler.DM {
    class DMObjectTree {
        private Dictionary<DreamPath, DMObject> _allObjects = new();
        private uint _dmObjectIdCounter = 0;

        public DMObjectTree() {
            GetDMObject(DreamPath.Root);
        }

        public DMObject GetDMObject(DreamPath path) {
            DMObject dmObject;

            if (!_allObjects.TryGetValue(path, out dmObject)) {
                DreamPath? parentType = null;
                if (path.Elements.Length >= 2) {
                    parentType = path.FromElements(0, -2);
                    GetDMObject(parentType.Value); //Make sure the parent exists
                }

                dmObject = new DMObject(_dmObjectIdCounter++, path, parentType);
                _allObjects.Add(path, dmObject);
            }

            return dmObject;
        }

        public DreamObjectJson CreateJsonRepresentation() {
            Dictionary<DreamPath, DreamObjectJson> jsonObjects = new();
            Queue<(DMObject, DreamObjectJson)> unparentedObjects = new();

            foreach (KeyValuePair<DreamPath, DMObject> dmObject in _allObjects) {
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
                    if (unparentedObject.Item1.Parent != null && unparentedObject.Item1.Parent.Value.Equals(treeParentPath)) {
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
