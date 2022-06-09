﻿using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectArea : DreamMetaObjectRoot {
        private IDreamManager _dreamManager = IoCManager.Resolve<IDreamManager>();
        private IDreamMapManager _dreamMapManager = IoCManager.Resolve<IDreamMapManager>();

        public override bool ShouldCallNew => true;

        public DreamMetaObjectArea(DreamObjectDefinition definition) : base(definition){}

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            DreamList contents = DreamList.Create();

            contents.ValueAssigned += (DreamList list, DreamValue key, DreamValue value) => {
                if (value.TryGetValueAsDreamObjectOfType(DreamPath.Turf, out DreamObject turf)) {
                    int x = turf.GetVariable("x").GetValueAsInteger();
                    int y = turf.GetVariable("y").GetValueAsInteger();
                    int z = turf.GetVariable("z").GetValueAsInteger();

                    _dreamMapManager.SetArea(x, y, z, dreamObject);
                }
            };

            _dreamManager.AreaContents.Add(dreamObject, contents);

            ParentType.OnObjectCreated(dreamObject, creationArguments);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            _dreamManager.AreaContents.Remove(dreamObject);
            ParentType.OnObjectDeleted(dreamObject);
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue,
            DreamValue oldVariableValue)
        {
            ParentType.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "contents") {
                return new DreamValue(_dreamManager.AreaContents[dreamObject]);
            } else {
                return ParentType.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }
    }
}
