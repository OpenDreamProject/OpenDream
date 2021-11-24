using System;
using System.IO;
using System.Text;

namespace OpenDreamRuntime.Resources {
    public class DreamResource {
        public DreamRuntime Runtime { get; }

        public string ResourcePath;
        public byte[] ResourceData {
            get {
                if (_resourceData == null && File.Exists(_filePath)) {
                    _resourceData = File.ReadAllBytes(_filePath);
                }

                return _resourceData;
            }
        }

        private string _filePath;
        private byte[] _resourceData = null;

        public DreamResource(DreamRuntime runtime, string filePath, string resourcePath) {
            Runtime = runtime;
            _filePath = filePath;
            ResourcePath = resourcePath;
        }

        public virtual string ReadAsString() {
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
                string filePath = Path.Combine(Runtime.ResourceManager.RootPath, ResourcePath);

                CreateDirectory();
                File.AppendAllText(filePath, text + "\r\n");
                _resourceData = null;
            } else {
                throw new Exception("Invalid output operation on '" + ResourcePath + "'");
            }
        }

        private void CreateDirectory() {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
        }
    }
}
