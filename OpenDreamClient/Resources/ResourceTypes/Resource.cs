namespace OpenDreamClient.Resources.ResourceTypes {
    class Resource {
        public string ResourcePath;
        public byte[] Data;

        public Resource(string resourcePath, byte[] data) {
            ResourcePath = resourcePath;
            Data = data;
        }
    }
}
