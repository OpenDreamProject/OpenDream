using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;


namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectFilter : IDreamMetaObject {
        public bool ShouldCallNew => false;
        public IDreamMetaObject? ParentType { get; set; }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            ParentType?.OnObjectCreated(dreamObject, creationArguments);
        }

      /*  public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            //recompile shader
            if(dreamObject.HasVariable(varName))
            {

            }
        }*/
    }
}