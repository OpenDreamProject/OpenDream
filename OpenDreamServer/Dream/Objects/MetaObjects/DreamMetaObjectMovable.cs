using OpenDreamServer.Dream.Procs;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectMovable : DreamMetaObjectAtom {
        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            DreamValue screenLocationValue = dreamObject.GetVariable("screen_loc");
            if (screenLocationValue.Value != null)UpdateScreenLocation(dreamObject, screenLocationValue);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            if (dreamObject.GetVariable("loc").TryGetValueAsDreamObjectOfType(DreamPath.Atom, out DreamObject loc)) {
                DreamList contents = DreamMetaObjectList.DreamLists[loc.GetVariable("contents").GetValueAsDreamObjectOfType(DreamPath.List)];
                contents.RemoveValue(new DreamValue(dreamObject));
            }

            base.OnObjectDeleted(dreamObject);
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            if (variableName == "x" || variableName == "y" || variableName == "z") {
                int x = (variableName == "x") ? variableValue.GetValueAsInteger() : dreamObject.GetVariable("x").GetValueAsInteger();
                int y = (variableName == "y") ? variableValue.GetValueAsInteger() : dreamObject.GetVariable("y").GetValueAsInteger();
                DreamObject newLocation = Program.DreamMap.GetTurfAt(x, y);

                dreamObject.SetVariable("loc", new DreamValue(newLocation));
            } else if (variableName == "loc") {
                Program.DreamStateManager.AddAtomLocationDelta(dreamObject, variableValue.GetValueAsDreamObject());

                if (oldVariableValue.Value != null) {
                    DreamObject oldLoc = oldVariableValue.GetValueAsDreamObjectOfType(DreamPath.Atom);
                    DreamObject oldLocContents = oldLoc.GetVariable("contents").GetValueAsDreamObjectOfType(DreamPath.List);

                    oldLocContents.CallProc("Remove", new DreamProcArguments(new List<DreamValue>() { new DreamValue(dreamObject) }));
                }

                if (variableValue.Value != null) {
                    DreamObject newLoc = variableValue.GetValueAsDreamObjectOfType(DreamPath.Atom);
                    DreamObject newLocContents = newLoc.GetVariable("contents").GetValueAsDreamObjectOfType(DreamPath.List);

                    newLocContents.CallProc("Add", new DreamProcArguments(new List<DreamValue>() { new DreamValue(dreamObject) }));
                }
            } else if (variableName == "screen_loc") {
                UpdateScreenLocation(dreamObject, variableValue);
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "x" || variableName == "y" || variableName == "z") {
                DreamObject location = dreamObject.GetVariable("loc").GetValueAsDreamObject();

                if (location != null) {
                    return location.GetVariable(variableName);
                } else {
                    return new DreamValue(0);
                }
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }

        private void UpdateScreenLocation(DreamObject movable, DreamValue screenLocationValue) {
            ScreenLocation screenLocation;
            if (screenLocationValue.Value != null) {
                string screenLocationString = screenLocationValue.GetValueAsString();
                screenLocation = new ScreenLocation(screenLocationString);
            } else {
                screenLocation = new ScreenLocation();
            }

            Program.DreamStateManager.AddAtomScreenLocationDelta(movable, screenLocation);
        }
    }
}
