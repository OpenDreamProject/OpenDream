using OpenDreamShared.Common;

namespace DMCompiler.Compiler.DM;

internal abstract class VarDeclInfo {
    public DreamPath? TypePath;
    public string VarName;

    ///<summary>Marks whether the variable is /global/ or /static/. (These are seemingly interchangeable keywords in DM and so are under this same boolean)</summary>
    public bool IsStatic;

    public bool IsConst;
    public bool IsFinal;
    public bool IsList;
}

internal sealed class ProcVarDeclInfo : VarDeclInfo {
    public ProcVarDeclInfo(DreamPath path) {
        string[] elements = path.Elements;
        var readIdx = 0;
        List<string> currentPath = new();
        if (elements[readIdx] == "var") {
            readIdx++;
        }

        while (readIdx < elements.Length - 1) {
            var elem = elements[readIdx];
            switch (elem) {
                case "static":
                case "global":
                    IsStatic = true;
                    break;
                case "const":
                    IsConst = true;
                    break;
                case "final":
                    IsFinal = true;
                    break;
                case "list":
                    IsList = true;
                    break;
                default:
                    currentPath.Add(elem);
                    break;
            }

            readIdx += 1;
        }

        if (currentPath.Count > 0) {
            TypePath = new DreamPath(DreamPath.PathType.Absolute, currentPath.ToArray());
        } else {
            TypePath = null;
        }

        VarName = elements[^1];
    }
}

internal sealed class ObjVarDeclInfo : VarDeclInfo {
    public DreamPath ObjectPath;
    public readonly bool IsTmp;

    public ObjVarDeclInfo(DreamPath path) {
        string[] elements = path.Elements;
        var readIdx = 0;
        List<string> currentPath = new();
        while (readIdx < elements.Length && elements[readIdx] != "var") {
            currentPath.Add(elements[readIdx]);
            readIdx += 1;
        }

        ObjectPath = new DreamPath(path.Type, currentPath.ToArray());
        if (ObjectPath.Elements.Length == 0) { // Variables declared in the root scope are inherently static.
            IsStatic = true;
        }

        currentPath.Clear();
        readIdx += 1;
        while (readIdx < elements.Length - 1) {
            var elem = elements[readIdx];
            switch (elem) {
                case "static":
                case "global":
                    IsStatic = true;
                    break;
                case "const":
                    IsConst = true;
                    break;
                case "final":
                    IsFinal = true;
                    break;
                case "list":
                    IsList = true;
                    break;
                case "tmp":
                    IsTmp = true;
                    break;
                default:
                    currentPath.Add(elem);
                    break;
            }

            readIdx += 1;
        }

        if (currentPath.Count > 0) {
            TypePath = new DreamPath(DreamPath.PathType.Absolute, currentPath.ToArray());
        } else {
            TypePath = null;
        }

        VarName = elements[^1];
    }
}

internal sealed class ProcParameterDeclInfo : VarDeclInfo {
    public ProcParameterDeclInfo(DreamPath path) {
        string[] elements = path.Elements;
        var readIdx = 0;
        List<string> currentPath = new();
        if (elements[readIdx] == "var") {
            readIdx++;
        }

        while (readIdx < elements.Length - 1) {
            var elem = elements[readIdx];
            switch (elem) {
                case "static":
                case "global":
                    //No effect
                    break;
                case "const":
                    //TODO: Parameters can be constant
                    //If they are they can't be assigned to but still cannot be used in const-only contexts (such as switch cases)
                    break;
                case "final":
                    IsFinal = true;
                    break;
                case "list":
                    IsList = true;
                    break;
                default:
                    currentPath.Add(elem);
                    break;
            }

            readIdx += 1;
        }

        if (currentPath.Count > 0) {
            TypePath = new DreamPath(DreamPath.PathType.Absolute, currentPath.ToArray());
        } else {
            TypePath = null;
        }

        VarName = elements[^1];
    }
}
