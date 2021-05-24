﻿using OpenDreamRuntime.Procs;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamRuntime.Objects.MetaObjects {
    class DreamMetaObjectSound : DreamMetaObjectRoot {
        public DreamMetaObjectSound(DreamRuntime runtime)
            : base(runtime)

        {}

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            dreamObject.SpawnProc("New", creationArguments);
        }
    }
}
