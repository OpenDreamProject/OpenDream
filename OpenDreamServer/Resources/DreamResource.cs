using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamServer.Resources {
    class DreamResource {
        public string ResourcePath;
        public byte[] ResourceData;

        public DreamResource(string path, byte[] data) {
            ResourcePath = path;
            ResourceData = data;
        }

        public string ReadAsString() {
            String resourceString = Encoding.ASCII.GetString(ResourceData);

            resourceString = resourceString.Replace("\r\n", "\n");
            return resourceString;
        }
    }
}
