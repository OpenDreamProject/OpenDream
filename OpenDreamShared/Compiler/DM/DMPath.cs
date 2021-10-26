
using System.Collections.Generic;
using OpenDreamShared.Dream;

namespace OpenDreamShared.Compiler.DM {

    public class VarDeclInfo {
        public DreamPath? TypePath;
        public string VarName;
        public bool IsGlobal;
        public bool IsConst;
    }

    public class ProcVarDeclInfo : VarDeclInfo {
        public ProcVarDeclInfo(DreamPath path) {
            string[] elements = path.Elements;
            var readIdx = 0;
            List<string> currentPath = new();
            if (elements[readIdx] == "var") {
                readIdx++;
            }
            while (readIdx < elements.Length - 1) {
                var elem = elements[readIdx];
                if (elem == "static" || elem == "global") {
                    IsGlobal = true;
                }
                else if (elem == "const") {
                    IsConst = true;
                }
                else {
                    currentPath.Add(elem);
                }
                readIdx += 1;
            }
            if (currentPath.Count > 0) {
                TypePath = new DreamPath(DreamPath.PathType.Absolute, currentPath.ToArray());
            }
            else {
                TypePath = null;
            }
            VarName = elements[elements.Length - 1];
        }
    }

    public class ObjVarDeclInfo : VarDeclInfo {
        public DreamPath ObjectPath;
        public bool IsTmp;
        public bool IsToplevel;

        public ObjVarDeclInfo(DreamPath path) {
            string[] elements = path.Elements;
            var readIdx = 0;
            List<string> currentPath = new();
            while (readIdx < elements.Length && elements[readIdx] != "var") {
                currentPath.Add(elements[readIdx]);
                readIdx += 1;
            }
            ObjectPath = new DreamPath(path.Type, currentPath.ToArray());
            if (ObjectPath.Elements.Length == 0) {
                IsToplevel = true;
            }
            currentPath.Clear();
            readIdx += 1;
            while (readIdx < elements.Length - 1) {
                var elem = elements[readIdx];
                if (elem == "static" || elem == "global") {
                    IsGlobal = true;
                }
                else if (elem == "const") {
                    IsConst = true;
                }
                else if (elem == "tmp") {
                    IsTmp = true;
                }
                else {
                    currentPath.Add(elem);
                }
                readIdx += 1;
            }
            if (currentPath.Count > 0) {
                TypePath = new DreamPath(DreamPath.PathType.Absolute, currentPath.ToArray());
            }
            else {
                TypePath = null;
            }
            VarName = elements[elements.Length - 1];
        }

    }
}
