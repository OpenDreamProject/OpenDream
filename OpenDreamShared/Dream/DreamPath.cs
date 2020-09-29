using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenDreamShared.Dream {
    struct DreamPath {
        public static DreamPath Root = new DreamPath("/");
        public static DreamPath List = new DreamPath("/list");
        public static DreamPath Sound = new DreamPath("/sound");
        public static DreamPath Image = new DreamPath("/image");
        public static DreamPath MutableAppearance = new DreamPath("/mutable_appearance");
        public static DreamPath World = new DreamPath("/world");
        public static DreamPath Client = new DreamPath("/client");
        public static DreamPath Datum = new DreamPath("/datum");
        public static DreamPath Atom = new DreamPath("/atom");
        public static DreamPath Area = new DreamPath("/area");
        public static DreamPath Turf = new DreamPath("/turf");
        public static DreamPath Movable = new DreamPath("/atom/movable");
        public static DreamPath Obj = new DreamPath("/obj");
        public static DreamPath Mob = new DreamPath("/mob");

        public enum PathType {
            Absolute,
            Relative,
            Other
        }

        public string LastElement {
            get => Elements.Last();
        }

        public string[] Elements;
        public PathType Type;

        public string PathString {
            get {
                string pathString = null;

                if (Type == PathType.Absolute) pathString = "/";
                else if (Type == PathType.Relative) pathString = ".";

                if (Elements.Length > 0) {
                    pathString += Elements.Aggregate((string first, string second) => {
                        return first + "/" + second;
                    });
                }

                if (!pathString.EndsWith("/")) pathString += "/";
                return pathString;
            }
        }

        public DreamPath(string path) {
            Elements = null;
            Type = PathType.Absolute;

            SetFromString(path);
        }

        public void SetFromString(string rawPath) {
            char pathTypeChar = rawPath[0];

            if (pathTypeChar == '/') {
                Type = PathType.Absolute;
                rawPath = rawPath.Substring(1);
            } else if (pathTypeChar == '.') {
                Type = PathType.Relative;
                rawPath = rawPath.Substring(1);
            } else {
                Type = PathType.Other;
            }

            Elements = rawPath.Split("/");
            Normalize();
        }

        public bool IsDescendantOf(DreamPath path) {
            return PathString.StartsWith(path.PathString);
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

        public DreamPath FromElements(int elementStart, int elementEnd = -1) {
            List<string> elements = new List<string>();

            if (elementEnd == -1) elementEnd = Elements.Length;
            for (int i = elementStart; i < elementEnd; i++) {
                elements.Add(Elements[i]);
            }

            string rawPath = elements.Aggregate((string second, string first) => {
                return first + "/" + second;
            });

            if (!rawPath.StartsWith("/")) rawPath = "/" + rawPath;
            return new DreamPath(rawPath);
        }

        public override string ToString() {
            return PathString;
        }

        public override bool Equals(object obj) {
            if (obj is DreamPath) {
                return PathString == ((DreamPath)obj).PathString;
            } else {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode() {
            return PathString.GetHashCode();
        }

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
