using OpenDreamRuntime.Procs;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamRuntime.Objects.MetaObjects {
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
