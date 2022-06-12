using System.IO;
using System.Text;

namespace OpenDreamRuntime.Resources {
    [Virtual]
    public class DreamResource {
        [Dependency] private readonly DreamResourceManager _rscMan = default!;

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

        public DreamResource(string filePath, string resourcePath) {
            _filePath = filePath;
            ResourcePath = resourcePath;
        }

        public virtual string ReadAsString(bool ignoreTrustLevel = false)
        {
            if (!ignoreTrustLevel && !_rscMan.SufficientTrustLevel(_filePath)) return null;

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
                string filePath = Path.Combine(_rscMan.RootPath, ResourcePath);

                //TODO Is this necessary?
                if (!_rscMan.SufficientTrustLevel(filePath))
                {
                    return;
                }

                CreateDirectory();
                File.AppendAllText(filePath, text + "\r\n");
                _resourceData = null;
            } else {
                throw new Exception("Invalid output operation on '" + ResourcePath + "'");
            }
        }

        private void CreateDirectory() {
            //TODO Is this necessary?
            var path = Path.GetDirectoryName(_filePath);
            if (!_rscMan.SufficientTrustLevel(path))
            {
                return;
            }
            Directory.CreateDirectory(path);
        }
    }
}
