using OpenDreamServer.Dream.Procs;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectSound : DreamMetaObjectRoot {
        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            dreamObject.CallProc("New", creationArguments);
        }
    }
}
