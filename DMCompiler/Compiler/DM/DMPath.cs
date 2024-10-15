namespace DMCompiler.Compiler.DM {
    public abstract class VarDeclInfo {
        public DreamPath? TypePath;
        public string VarName = string.Empty;

        ///<summary>Marks whether the variable is /global/ or /static/. (These are seemingly interchangeable keywords in DM and so are under this same boolean)</summary>
        public bool IsStatic;

        public bool IsConst;
        public bool IsList;
    }

    public sealed class ProcVarDeclInfo : VarDeclInfo
    {
        public ProcVarDeclInfo(DreamPath path)
        {
            string[] elements = path.Elements;
            var readIdx = 0;
            List<string> currentPath = new();
            if (elements[readIdx] == "var")
            {
                readIdx++;
            }
            while (readIdx < elements.Length - 1)
            {
                var elem = elements[readIdx];
                if (elem == "static" || elem == "global")
                {
                    IsStatic = true;
                }
                else if (elem == "const")
                {
                    IsConst = true;
                }
                else if (elem == "list")
                {
                    IsList = true;
                }
                else
                {
                    currentPath.Add(elem);
                }
                readIdx += 1;
            }
            if (currentPath.Count > 0)
            {
                TypePath = new DreamPath(DreamPath.PathType.Absolute, currentPath.ToArray());
            }
            else
            {
                TypePath = null;
            }
            VarName = elements[elements.Length - 1];
        }
    }

    public sealed class ObjVarDeclInfo : VarDeclInfo
    {
        public DreamPath ObjectPath;
        public bool IsTmp;

        public ObjVarDeclInfo(DreamPath path)
        {
            string[] elements = path.Elements;
            var readIdx = 0;
            List<string> currentPath = new();
            while (readIdx < elements.Length && elements[readIdx] != "var")
            {
                currentPath.Add(elements[readIdx]);
                readIdx += 1;
            }
            ObjectPath = new DreamPath(path.Type, currentPath.ToArray());
            if (ObjectPath.Elements.Length == 0) // Variables declared in the root scope are inherently static.
            {
                IsStatic = true;
            }
            currentPath.Clear();
            readIdx += 1;
            while (readIdx < elements.Length - 1)
            {
                var elem = elements[readIdx];
                if (elem == "static" || elem == "global")
                {
                    IsStatic = true;
                }
                else if (elem == "const")
                {
                    IsConst = true;
                }
                else if (elem == "list")
                {
                    IsList = true;
                }
                else if (elem == "tmp")
                {
                    IsTmp = true;
                }
                else
                {
                    currentPath.Add(elem);
                }
                readIdx += 1;
            }
            if (currentPath.Count > 0)
            {
                TypePath = new DreamPath(DreamPath.PathType.Absolute, currentPath.ToArray());
            }
            else
            {
                TypePath = null;
            }
            VarName = elements[elements.Length - 1];
        }
    }

    public sealed class ProcParameterDeclInfo : VarDeclInfo {
        public ProcParameterDeclInfo(DreamPath path) {
            string[] elements = path.Elements;
            var readIdx = 0;
            List<string> currentPath = new();
            if (elements[readIdx] == "var") {
                readIdx++;
            }
            while (readIdx < elements.Length - 1) {
                var elem = elements[readIdx];
                if (elem == "static" || elem == "global") {
                    //No effect
                } else if (elem == "const") {
                    //TODO: Parameters can be constant
                    //If they are they can't be assigned to but still cannot be used in const-only contexts (such as switch cases)
                } else if (elem == "list") {
                    IsList = true;
                } else {
                    currentPath.Add(elem);
                }
                readIdx += 1;
            }
            if (currentPath.Count > 0) {
                TypePath = new DreamPath(DreamPath.PathType.Absolute, currentPath.ToArray());
            } else {
                TypePath = null;
            }
            VarName = elements[elements.Length - 1];
        }
    }
}
