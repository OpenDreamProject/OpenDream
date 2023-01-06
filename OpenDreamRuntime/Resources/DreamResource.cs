using System.IO;
using System.Text;

namespace OpenDreamRuntime.Resources {
    [Virtual]
    public class DreamResource {
        public readonly string? ResourcePath;
        public readonly int Id;
        public byte[]? ResourceData {
            get {
                if (_resourceData == null && File.Exists(_filePath)) {
                    _resourceData = File.ReadAllBytes(_filePath);
                }

                return _resourceData;
            }
        }

        private readonly string? _filePath;
        private byte[]? _resourceData = null;

        public DreamResource(int id, string? filePath, string? resourcePath) {
            Id = id;
            ResourcePath = resourcePath;
            _filePath = filePath;
        }

        public DreamResource(int id, byte[] data) {
            Id = id;
            _resourceData = data;
        }

        public virtual string? ReadAsString() {
            if (ResourceData == null) return null;

            string resourceString = Encoding.ASCII.GetString(ResourceData);

            resourceString = resourceString.Replace("\r\n", "\n");
            return resourceString;
        }

        public void Clear() {
            CreateDirectory();
            File.WriteAllText(_filePath, String.Empty);
        }

        public virtual void Output(DreamValue value) {
            if (value.TryGetValueAsString(out string? text)) {
                string filePath = Path.Combine(IoCManager.Resolve<DreamResourceManager>().RootPath, ResourcePath);

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

        public override string ToString() {
            return $"'{ResourcePath}'";
        }
    }
}
