using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;
//using Robust.Client.Graphics;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectFilter : IDreamMetaObject {
        public bool ShouldCallNew => false;
        public IDreamMetaObject? ParentType { get; set; }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            //recompile shader
            if(dreamObject.HasVariable(varName))
            {
                
            }
        }
    }
}