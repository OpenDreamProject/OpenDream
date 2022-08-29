using System.IO;
using System.Text;

namespace OpenDreamRuntime.Resources {

    /// <summary>
    /// This is distinct from <see cref="OpenDreamShared.Resources.AbstractResource"/> insofar that, since we are the runtime, <br/>
    /// we have direct file access to what this Resource is based off of, provided it is written to disk. <br/>
    /// As such, this virtual class has several file I/O methods that AbstractResource cannot define nor implement.
    /// </summary>
    [Virtual]
    public class DreamResource : OpenDreamShared.Resources.AbstractResource {
        private string _filePath;
        public new byte[] ResourceData { // This getter shadows the ResourceData in the base class so we can do this lazy eval thing
            get {
                if (base.ResourceData == null) {
                    if(Exists())
                        return base.ResourceData = File.ReadAllBytes(_filePath);
                    return Array.Empty<byte>();
                }

                return base.ResourceData;
            }
        }

        public DreamResource(string filePath, string resourcePath) : base(resourcePath) {
            _filePath = filePath;
            ResourcePath = resourcePath;
        }

        public bool Exists() {
            return File.Exists(_filePath);
        }

        public virtual string? ReadAsString() {
            if (!File.Exists(_filePath)) return null;

            string resourceString = Encoding.ASCII.GetString(ResourceData);

            resourceString = resourceString.Replace("\r\n", "\n");
            return resourceString;
        }

        public virtual void Clear() {
            CreateDirectory();
            File.WriteAllText(_filePath, String.Empty);
        }

        public virtual void Output(DreamValue value) {
            if (value.TryGetValueAsString(out string text)) {
                string filePath = Path.Combine(IoCManager.Resolve<DreamResourceManager>().RootPath, ResourcePath);

                CreateDirectory();
                File.AppendAllText(filePath, text + "\r\n");
                base.ResourceData = null; // Invalidate this data
            } else {
                throw new Exception("Invalid output operation on '" + ResourcePath + "'");
            }
        }

        private void CreateDirectory() {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
        }
    }
}
