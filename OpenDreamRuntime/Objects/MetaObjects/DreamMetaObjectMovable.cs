using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    class DreamMetaObjectMovable : DreamMetaObjectAtom {
        public DreamMetaObjectMovable(DreamRuntime runtime)
            : base(runtime)
        {}

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            DreamValue screenLocationValue = dreamObject.GetVariable("screen_loc");
            if (screenLocationValue.Value != null)UpdateScreenLocation(dreamObject, screenLocationValue);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            if (dreamObject.GetVariable("loc").TryGetValueAsDreamObjectOfType(DreamPath.Atom, out DreamObject loc)) {
                DreamList contents = loc.GetVariable("contents").GetValueAsDreamList();

                contents.RemoveValue(new DreamValue(dreamObject));
            }

            base.OnObjectDeleted(dreamObject);
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            if (variableName == "x" || variableName == "y" || variableName == "z") {
                int x = (variableName == "x") ? variableValue.GetValueAsInteger() : dreamObject.GetVariable("x").GetValueAsInteger();
                int y = (variableName == "y") ? variableValue.GetValueAsInteger() : dreamObject.GetVariable("y").GetValueAsInteger();
                int z = (variableName == "z") ? variableValue.GetValueAsInteger() : dreamObject.GetVariable("z").GetValueAsInteger();
                DreamObject newLocation = Runtime.Map.GetTurfAt(x, y, z);

                dreamObject.SetVariable("loc", new DreamValue(newLocation));
            } else if (variableName == "loc") {
                Runtime.StateManager.AddAtomLocationDelta(dreamObject, variableValue.GetValueAsDreamObject());

                if (oldVariableValue.Value != null) {
                    DreamObject oldLoc = oldVariableValue.GetValueAsDreamObjectOfType(DreamPath.Atom);
                    DreamList oldLocContents = oldLoc.GetVariable("contents").GetValueAsDreamList();

                    oldLocContents.RemoveValue(new DreamValue(dreamObject));
                }

                if (variableValue.Value != null) {
                    DreamObject newLoc = variableValue.GetValueAsDreamObjectOfType(DreamPath.Atom);
                    DreamList newLocContents = newLoc.GetVariable("contents").GetValueAsDreamList();

                    newLocContents.AddValue(new DreamValue(dreamObject));
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
            if (screenLocationValue.TryGetValueAsString(out string screenLocationString)) {
                screenLocation = new ScreenLocation(screenLocationString);
            } else {
                screenLocation = new ScreenLocation(0, 0, 0, 0);
            }

            Runtime.StateManager.AddAtomScreenLocationDelta(movable, screenLocation);
        }
    }
}
