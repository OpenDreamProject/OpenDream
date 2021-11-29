namespace OpenDreamClient.Resources.ResourceTypes
{
    public abstract class DreamResource
    {
        public string ResourcePath;
        public byte[] Data;

        protected DreamResource(string resourcePath, byte[] data) {
            ResourcePath = resourcePath;
            Data = data;
        }
    }
}
