using OpenDreamVM.Procs;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamVM.Objects.MetaObjects {
    class DreamMetaObjectImage : DreamMetaObjectRoot {
        public DreamMetaObjectImage(DreamRuntime runtime)
            : base(runtime)
        {}

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            dreamObject.SpawnProc("New", creationArguments);
        }
    }
}
