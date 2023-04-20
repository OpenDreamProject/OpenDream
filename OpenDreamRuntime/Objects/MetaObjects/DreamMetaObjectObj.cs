using OpenDreamRuntime.Procs;
using Robust.Shared.Utility;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectObj : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly IAtomManager _atomManager = default!;

        public DreamMetaObjectObj() {
            IoCManager.InjectDependencies(this);
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            _atomManager.Objects.Add(dreamObject);

            ParentType?.OnObjectCreated(dreamObject, creationArguments);
        }

        public void OnObjectDeleted(DreamObject dreamObject) {
            _atomManager.Objects.RemoveSwap(_atomManager.Objects.IndexOf(dreamObject));

            ParentType?.OnObjectDeleted(dreamObject);
        }
    }
}
