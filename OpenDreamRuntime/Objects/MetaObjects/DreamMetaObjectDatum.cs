using Robust.Shared.IoC;

namespace OpenDreamRuntime.Objects.MetaObjects {
    class DreamMetaObjectDatum : DreamMetaObjectRoot {
        public override bool ShouldCallNew => true;

        private IDreamManager _dreamManager = IoCManager.Resolve<IDreamManager>();

        public override void OnObjectDeleted(DreamObject dreamObject) {
            base.OnObjectDeleted(dreamObject);

            dreamObject.SpawnProc("Del");
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "type") {
                return new DreamValue(dreamObject.ObjectDefinition.Type);
            } else if (variableName == "parent_type") {
                return new DreamValue(_dreamManager.ObjectTree.GetTreeEntry(dreamObject.ObjectDefinition.Type).ParentEntry.ObjectDefinition.Type);
            } else if (variableName == "vars") {
                return new DreamValue(DreamListVars.Create(dreamObject));
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }
    }
}
