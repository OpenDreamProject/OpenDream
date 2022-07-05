using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectTurf : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        private readonly IDreamMapManager _dreamMapManager = IoCManager.Resolve<IDreamMapManager>();

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

            if (varName == "loc") {
                if (value.TryGetValueAsDreamObjectOfType(DreamPath.Turf, out DreamObject replacedTurf)) {
                    //Transfer all the old turf's contents
                    DreamList contents = replacedTurf.GetVariable("contents").GetValueAsDreamList();
                    foreach (DreamValue child in contents.GetValues()) {
                        child.GetValueAsDreamObjectOfType(DreamPath.Movable).SetVariable("loc", new DreamValue(dreamObject));
                    }

                    int x = replacedTurf.GetVariable("x").GetValueAsInteger();
                    int y = replacedTurf.GetVariable("y").GetValueAsInteger();
                    int z = replacedTurf.GetVariable("z").GetValueAsInteger();
                    _dreamMapManager.SetTurf(x, y, z, dreamObject);
                }
            }
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) {
            if (varName == "loc") {
                int x = dreamObject.GetVariable("x").GetValueAsInteger();
                int y = dreamObject.GetVariable("y").GetValueAsInteger();
                int z = dreamObject.GetVariable("z").GetValueAsInteger();

                return new DreamValue(_dreamMapManager.GetAreaAt(x, y, z));
            } else {
                return ParentType.OnVariableGet(dreamObject, varName, value);
            }
        }
    }
}
