using OpenDreamShared.Dream;
using System.Collections.Generic;

namespace DMCompiler.DM {
    struct DMList {
        public object[] Values;

        public DMList(object[] values) {
            Values = values;
        }
    }

    struct DMResource {
        public string ResourcePath;

        public DMResource(string resourcePath) {
            ResourcePath = resourcePath;
        }
    }
    
    //Used in object variable definitions
    struct DMNewInstance {
        public DreamPath Path;

        public DMNewInstance(DreamPath path) {
            Path = path;
        }
    }
}
