using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using System.IO;

namespace Content.Server.Dream {
    public class DreamResource {
        public string ResourcePath { get => _resourcePath.ToString(); }
        public byte[] ResourceData {
            get {
                if (_resourceData == null && _resourceManager.TryContentFileRead(_resourcePath, out Stream stream)) {
                    _resourceData = stream.CopyToArray();
                }

                return _resourceData;
            }
        }

        private IResourceManager _resourceManager = IoCManager.Resolve<IResourceManager>();
        private byte[] _resourceData = null;
        private ResourcePath _resourcePath = null;

        public DreamResource(string resourcePath) {
            if (resourcePath != null) {
                _resourcePath = new ResourcePath("/Game") / new ResourcePath(resourcePath);
            }
        }

        public bool Exists() {
            return _resourceManager.ContentFileExists(_resourcePath);
        }

        public virtual string ReadAsString() {
            if (!Exists()) return null;

            string text = _resourceManager.ContentFileReadAllText(_resourcePath);
            text = text.Replace("\r\n", "\n");
            return text;
        }

        public virtual void Clear() {
            CreateDirectory();
            //File.WriteAllText(_filePath, String.Empty);
        }

        public virtual void Output(DreamValue value) {
            /*if (value.TryGetValueAsString(out string text)) {
                string filePath = Path.Combine(Runtime.ResourceManager.RootPath, ResourcePath);

                CreateDirectory();
                File.AppendAllText(filePath, text + "\r\n");
                _resourceData = null;
            } else {
                throw new Exception("Invalid output operation on '" + ResourcePath + "'");
            }*/
        }

        private void CreateDirectory() {
            //Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
        }
    }

    //A special resource that outputs to the console
    //world.log defaults to this
    class ConsoleOutputResource : DreamResource {
        public ConsoleOutputResource() : base(null) { }

        public override string ReadAsString() {
            return null;
        }

        public override void Output(DreamValue value) {
            Logger.Info(value.Stringify());
        }
    }
}
