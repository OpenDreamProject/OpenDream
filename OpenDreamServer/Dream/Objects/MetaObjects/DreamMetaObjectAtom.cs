using OpenDreamServer.Dream.Procs;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectAtom : DreamMetaObjectDatum {
        public static Dictionary<DreamObject, UInt16> AtomIDs = new Dictionary<DreamObject, UInt16>();
        public static Dictionary<UInt16, DreamObject> AtomIDToAtom = new Dictionary<UInt16, DreamObject>();

        private static UInt16 _atomIDCounter = 0;
        private static object _atomListsLock = new object();

        public override void  OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            lock (_atomListsLock) {
                UInt16 atomID = _atomIDCounter++;

                DreamMetaObjectAtom.AtomIDs.Add(dreamObject, atomID);
                DreamMetaObjectAtom.AtomIDToAtom.Add(atomID, dreamObject);
            }

            Program.DreamStateManager.AddAtomCreation(dreamObject);

            if (creationArguments.ArgumentCount >= 1) {
                DreamObject loc = creationArguments.GetArgument(0, "loc").GetValueAsDreamObject();
                if (loc != null && loc.IsSubtypeOf(DreamPath.Atom)) {
                    dreamObject.SetVariable("loc", new DreamValue(loc)); //loc is set before /New() is ever called
                }
            } else {
                creationArguments.OrderedArguments.Add(new DreamValue((DreamObject)null)); //First argument is loc, which is null
            }

            DreamObject worldContents = Program.WorldInstance.GetVariable("contents").GetValueAsDreamObjectOfType(DreamPath.List);
            worldContents.CallProc("Add", new DreamProcArguments(new List<DreamValue>() { new DreamValue(dreamObject) }));

            base.OnObjectCreated(dreamObject, creationArguments);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            Program.DreamStateManager.AddAtomDeletion(dreamObject);

            lock (_atomListsLock) {
                DreamMetaObjectAtom.AtomIDToAtom.Remove(DreamMetaObjectAtom.AtomIDs[dreamObject]);
                DreamMetaObjectAtom.AtomIDs.Remove(dreamObject);
            }

            if (Program.WorldInstance.GetVariable("contents").TryGetValueAsDreamObjectOfType(DreamPath.List, out DreamObject worldContents)) {
                worldContents.CallProc("Remove", new DreamProcArguments(new List<DreamValue>() { new DreamValue(dreamObject) }));
            }

            base.OnObjectDeleted(dreamObject);
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            if (variableName == "icon_state") {
                Program.DreamStateManager.AddAtomIconStateDelta(dreamObject);
            }
        }
    }
}
