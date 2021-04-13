using System;

namespace OpenDreamClient.Resources.ResourceTypes {
    class ResourceSound : Resource {
        public ResourceSound(string resourcePath, byte[] data) : base(resourcePath, data) {
            if (!resourcePath.EndsWith(".ogg")) {
                throw new Exception("Only *.ogg audio files are supported");
            }
        }
    }
}
