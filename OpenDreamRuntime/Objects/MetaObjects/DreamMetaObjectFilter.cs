using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectFilter : IDreamMetaObject {
        public bool ShouldCallNew => false;
        public IDreamMetaObject? ParentType { get; set; }

        private DreamList filter_data;

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            
        }

        public void OnObjectDeleted(DreamObject dreamObject) {
                       
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) {
            if(filter_data.ContainsKey(new DreamValue(varName)))
                return filter_data.GetValue(new DreamValue(varName));
            else
                throw new Exception("{varName}: undefined var");
        }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            //recompile shader
        }
    }
}