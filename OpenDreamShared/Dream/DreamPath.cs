using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace OpenDreamShared.Dream {
    public struct DreamPath {
        public static readonly DreamPath Root = new DreamPath("/");
        public static readonly DreamPath Exception = new DreamPath("/exception");
        public static readonly DreamPath List = new DreamPath("/list");
        public static readonly DreamPath Regex = new DreamPath("/regex");
        public static readonly DreamPath Savefile = new DreamPath("/savefile");
        public static readonly DreamPath Sound = new DreamPath("/sound");
        public static readonly DreamPath Image = new DreamPath("/image");
        public static readonly DreamPath Icon = new DreamPath("/icon");
        public static readonly DreamPath MutableAppearance = new DreamPath("/mutable_appearance");
        public static readonly DreamPath World = new DreamPath("/world");
        public static readonly DreamPath Client = new DreamPath("/client");
        public static readonly DreamPath Datum = new DreamPath("/datum");
        public static readonly DreamPath Matrix = new DreamPath("/matrix");
        public static readonly DreamPath Atom = new DreamPath("/atom");
        public static readonly DreamPath Area = new DreamPath("/area");
        public static readonly DreamPath Turf = new DreamPath("/turf");
        public static readonly DreamPath Movable = new DreamPath("/atom/movable");
        public static readonly DreamPath Obj = new DreamPath("/obj");
        public static readonly DreamPath Mob = new DreamPath("/mob");
        public static readonly DreamPath Filter = new DreamPath("/dm_filter");

        public enum PathType {
            Absolute,
            Relative,

            //TODO: These really shouldn't be here
            DownwardSearch,
            UpwardSearch
        }

        [JsonIgnore]
        public string? LastElement {
            get => Elements.Length > 0 ? Elements.Last() : null;
        }

        [JsonIgnore]
        public string[] Elements {
            get => _elements;
            set {
                _elements = value;
                _pathString = null;
            }
        }

        public string PathString {
            get {
                if (_pathString != null) return _pathString;

                _pathString = Type switch {
                    PathType.Absolute => "/",
                    PathType.DownwardSearch => ":",
                    PathType.UpwardSearch => ".",
                    _ => string.Empty
                };

                // Elements is usually small enough for this to be faster than StringBuilder
                _pathString += string.Join("/", Elements);

                return _pathString;
            }
            set => SetFromString(value);
        }

        public PathType Type;

        private string[] _elements;
        private string? _pathString;

        public DreamPath(string path) {
            Type = PathType.Absolute;
            _elements = Array.Empty<string>(); // Set in SetFromString()
            _pathString = null;

            SetFromString(path);
        }

        public DreamPath(PathType type, string[] elements) {
            Type = type;
            _elements = elements;
            _pathString = null;

            Normalize(true);
        }

        public void SetFromString(string rawPath) {
            char pathTypeChar = rawPath[0];
            string[] tempElements = rawPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            bool skipFirstChar = false;

            switch (pathTypeChar) {
                case '/':
                    Type = PathType.Absolute;
                    // No need to skip the first char, as it will end up as an empty entry in tempElements
                    break;
                case ':':
                    Type = PathType.DownwardSearch;
                    skipFirstChar = true;
                    break;
                case '.':
                    Type = PathType.UpwardSearch;
                    skipFirstChar = true;
                    break;
                default:
                    Type = PathType.Relative;
                    break;
            }

            if (skipFirstChar) {
                // Skip the '/', ':' or '.' if needed
                tempElements[0] = tempElements[0][1..];
            }

            Elements = tempElements;
            Normalize(false);
        }

        /// <summary>
        /// Checks if the DreamPath is a descendant of another. NOTE: For type inheritance, use IsSubtypeOf()
        /// </summary>
        /// <param name="path">Path to compare to.</param>
        public bool IsDescendantOf(DreamPath path) {
            if (path.Elements.Length > Elements.Length) return false;

            for (int i = 0; i < path.Elements.Length; i++) {
                if (Elements[i] != path.Elements[i]) return false;
            }

            return true;
        }

        public DreamPath AddToPath(string path) {
            string rawPath = PathString;

            if (!rawPath.EndsWith('/') && !path.StartsWith('/')) {
                path = '/' + path;
            }

            return new DreamPath(rawPath + path);
        }

        public int FindElement(string element) {
            return Array.IndexOf(Elements, element);
        }

        public string[] GetElements(int elementStart, int elementEnd = -1) {
            if (elementEnd < 0) elementEnd = Elements.Length + elementEnd + 1;

            string[] elements = new string[elementEnd - elementStart];
            Array.Copy(Elements, elementStart, elements, 0, elements.Length);

            return elements;
        }

        public DreamPath FromElements(int elementStart, int elementEnd = -1) {
            string[] elements = GetElements(elementStart, elementEnd);
            string rawPath = String.Empty;

            if (elements.Length >= 1) {
                rawPath = elements.Aggregate((string first, string second) => {
                    return first + "/" + second;
                });
            }

            rawPath = "/" + rawPath;
            return new DreamPath(rawPath);
        }

        public DreamPath RemoveElement(int elementIndex) {
            if (elementIndex < 0) elementIndex += Elements.Length;

            List<string> elements = new List<string>();
            elements.AddRange(GetElements(0, elementIndex));
            elements.AddRange(GetElements(Math.Min(elementIndex + 1, Elements.Length), -1));
            return new DreamPath(Type, elements.ToArray());
        }

        public DreamPath Combine(DreamPath path) {
            switch (path.Type) {
                case PathType.Relative: return new DreamPath(PathString + "/" + path.PathString);
                case PathType.Absolute: return path;
                default: return new DreamPath(PathString + path.PathString);
            }
        }

        public override string ToString() {
            return PathString;
        }

        public override bool Equals(object? obj) => obj is DreamPath other && Equals(other);

        public bool Equals(DreamPath other) {
            if (other.Elements.Length != Elements.Length) return false;

            for (int i = 0; i < Elements.Length; i++) {
                if (Elements[i] != other.Elements[i]) return false;
            }

            return true;
        }

        public override int GetHashCode() {
            int hashCode = 0;
            for (int i = 0; i < Elements.Length; i++) {
                hashCode += Elements[i].GetHashCode();
            }

            return hashCode;
        }

        public static bool operator ==(DreamPath lhs, DreamPath rhs) => lhs.Equals(rhs);

        public static bool operator !=(DreamPath lhs, DreamPath rhs) => !(lhs == rhs);

        private void Normalize(bool canHaveEmptyEntries) {
            if (canHaveEmptyEntries && _elements.Contains("")) {
                // Slow path :(
                _elements = _elements.Where(el => !string.IsNullOrEmpty(el)).ToArray();
            }

            var writeIdx = Array.IndexOf(_elements, "..");
            if (writeIdx == -1) return;

            for (var i = writeIdx; i < _elements.Length; i++) {
                var elem = _elements[i];
                if (elem == "..") {
                    writeIdx -= 1;
                } else {
                    _elements[writeIdx] = elem;
                    writeIdx += 1;
                }
            }

            Elements = _elements[..writeIdx];
        }
    }
}
