using System;

namespace OpenDreamShared.Resources
{
    /// <summary>
    /// An abstract, shared model of how we store Dream Resources (such as the console, loaded DMIs, etc)
    /// </summary>
    public abstract class AbstractResource
    {
        public string ResourcePath;
        public byte[] ResourceData { get; protected set; }

        protected AbstractResource(string resourcePath, byte[]? resourceData = null)
        {
            ResourcePath = resourcePath;
            ResourceData = resourceData ?? Array.Empty<byte>();
        }
    }
}
