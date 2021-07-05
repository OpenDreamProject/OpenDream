using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenDreamShared.Dream {
    public struct DreamPath {
        public static readonly DreamPath Root = new DreamPath("/");
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

        public enum PathType {
            Absolute,
            Relative,
            DownwardSearch,
            UpwardSearch
        }

        public string LastElement {
            get => Elements.Last();
        }

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

                StringBuilder pathStringBuilder = new StringBuilder();
                if (Type == PathType.Absolute) pathStringBuilder.Append("/");
                else if (Type == PathType.DownwardSearch) pathStringBuilder.Append(":");
                else if (Type == PathType.UpwardSearch) pathStringBuilder.Append(".");

                for (int i = 0; i < Elements.Length; i++) {
                    pathStringBuilder.Append(Elements[i]);
                    if (i < Elements.Length - 1) pathStringBuilder.Append("/");
                }

                _pathString = pathStringBuilder.ToString();
                return _pathString;
            }
        }

        public PathType Type;

        private string[] _elements;
        private string _pathString;

        public DreamPath(string path) {
            Type = PathType.Absolute;
            _elements = null;
            _pathString = null;

            SetFromString(path);
        }

        public DreamPath(PathType type, string[] elements) {
            Type = type;
            _elements = elements;
            _pathString = null;

            Normalize();
        }

        public void SetFromString(string rawPath) {
            char pathTypeChar = rawPath[0];

            if (pathTypeChar == '/') {
                Type = PathType.Absolute;
                rawPath = rawPath.Substring(1);
            } else if (pathTypeChar == ':') {
                Type = PathType.DownwardSearch;
                rawPath = rawPath.Substring(1);
            } else if (pathTypeChar == '.') {
                Type = PathType.UpwardSearch;
                rawPath = rawPath.Substring(1);
            } else {
                Type = PathType.Relative;
            }

            Elements = rawPath.Split("/");
            Normalize();
        }

        public bool IsDescendantOf(DreamPath path) {
            if (path.Elements.Length > Elements.Length) return false;

            for (int i = 0; i < path.Elements.Length; i++) {
                if (Elements[i] != path.Elements[i]) return false;
            }

            return true;
        }

        public DreamPath AddToPath(string path) {
            string rawPath = PathString;

            if (!rawPath.EndsWith("/") && !path.StartsWith("/")) {
                path = "/" + path;
            }

            return new DreamPath(rawPath + path);
        }

        public int FindElement(string element) {
            for (int i = 0; i < Elements.Length; i++) {
                if (Elements[i] == element) return i;
            }

            return -1;
        }

        public string[] GetElements(int elementStart, int elementEnd = -1) {
            List<string> elements = new List<string>();

            if (elementEnd < 0) elementEnd = Elements.Length + elementEnd + 1;
            for (int i = elementStart; i < elementEnd; i++) {
                elements.Add(Elements[i]);
            }

            return elements.ToArray();
        }

        public DreamPath FromElements(int elementStart, int elementEnd = -1) {
            string[] elements = GetElements(elementStart, elementEnd);
            string rawPath = String.Empty;

            if (elements.Length >= 1) {
                rawPath = elements.Aggregate((string first, string second) => {
                    return first + "/" + second;
                });
            }

            if (!rawPath.StartsWith("/")) rawPath = "/" + rawPath;
            return new DreamPath(rawPath);
        }

        public DreamPath RemoveElement(int elementIndex) {
            if (elementIndex < 0) elementIndex = Elements.Length + elementIndex + 1;

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

        public override bool Equals(object obj) => obj is DreamPath other && Equals(other);

        public bool Equals(DreamPath other) {
            if (other.Elements.Length != Elements.Length) return false;

            for (int i = 0; i < Elements.Length; i++) {
                if (Elements[i] != other.Elements[i]) return false;
            }

            return true;
        }

        public override int GetHashCode() {
            return PathString.GetHashCode();
        }

        public static bool operator ==(DreamPath lhs, DreamPath rhs) => lhs.Equals(rhs);

        public static bool operator !=(DreamPath lhs, DreamPath rhs) => !(lhs == rhs);

        private void Normalize() {
            Stack<string> elements = new Stack<string>();

            foreach (string element in Elements) {
                if (element == "..") {
                    elements.Pop();
                } else if (element != "") {
                    elements.Push(element);
                }
            }

            Elements = elements.Reverse().ToArray();
        }
    }
}
