using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.MetaObjects {
    class DreamMetaObjectImage : DreamMetaObjectRoot {
        public DreamMetaObjectImage(DreamRuntime runtime)
            : base(runtime)
        {}

        public override bool ShouldCallNew => true;

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);
        }
    }
}
