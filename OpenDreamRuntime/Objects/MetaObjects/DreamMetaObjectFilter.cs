using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;


namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectFilter : IDreamMetaObject {
        public bool ShouldCallNew => false;
        public IDreamMetaObject? ParentType { get; set; }
        public static Dictionary<DreamObject, DreamObject> _FilterToDreamObject = new Dictionary<DreamObject, DreamObject>();

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            ParentType?.OnObjectCreated(dreamObject, creationArguments);
        }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            DreamObject holderAtom;
            if(!_FilterToDreamObject.TryGetValue(dreamObject, out holderAtom))
                return; //we don't need to do any further behaviour if this filter isn't attached
            DreamList filterList;
            holderAtom.GetVariable("filters").TryGetValueAsDreamList(out filterList);
            holderAtom.SetVariable("filters", new DreamValue(filterList)); //basically just trigger the DreamMetaObjectAtom's filter handling code again, which will update the appearance
        }

        public void OnObjectDeleted(DreamObject dreamObject)
        {
            ParentType?.OnObjectDeleted(dreamObject);
            _FilterToDreamObject.Remove(dreamObject);
        }
    }
}
