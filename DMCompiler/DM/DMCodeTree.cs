using System.Diagnostics.CodeAnalysis;
using DMCompiler.Compiler;
using DMCompiler.DM.Builders;

namespace DMCompiler.DM;

/// <summary>
/// A representation of all the types, procs, and vars defined in the tree.<br/>
/// Important in the role of defining everything & initializing statics in the correct order.
/// </summary>
// TODO: "/var" vs "var" has a different init order (same for procs)
// TODO: Path elements like /static and /global are grouped together
internal static partial class DMCodeTree {
    private interface INode;

    private class TypeNode(string name) : INode {
        public readonly List<INode> Children = new();

        private readonly string _name = name;

        public bool TryGetChild(string name, [NotNullWhen(true)] out TypeNode? child) {
            foreach (var ourChild in Children) {
                if (ourChild is not TypeNode typeNode)
                    continue;

                if (typeNode._name == name) {
                    child = typeNode;
                    return true;
                }
            }

            child = null;
            return false;
        }

        public override string ToString() {
            return _name;
        }
    }

    private class ObjectNode(string name, DreamPath type) : TypeNode(name) {
        private bool _defined;
        private ProcsNode? _procs;

        public void DefineType() {
            if (_defined)
                return;

            DMObject? explicitParent = null;
            if (ParentTypes.TryGetValue(type, out var parentType) &&
                !DMObjectTree.TryGetDMObject(parentType, out explicitParent))
                return; // Parent type isn't ready yet

            _defined = true;
            WaitingNodes.Remove(this);

            var dmObject = DMObjectTree.GetOrCreateDMObject(type);
            if (explicitParent != null) {
                dmObject.Parent = explicitParent;
                ParentTypes.Remove(type);
            }

            if (NewProcs.Remove(type, out var newProcNode))
                newProcNode.DefineProc();
        }

        public ProcsNode AddProcsNode() {
            if (_procs is null) {
                _procs = new();
                Children.Add(_procs);
            }

            return _procs;
        }
    }

    public static DMProc GlobalInitProc = default!;

    private static readonly HashSet<INode> WaitingNodes = new();
    private static readonly Dictionary<DreamPath, DreamPath> ParentTypes = new();
    private static readonly Dictionary<DreamPath, ProcNode> NewProcs = new();

    private static ObjectNode _root = default!;
    private static ObjectNode? _dmStandardRoot;
    private static int _currentPass;

    public static void Reset() {
        // Yep, not _dmStandardRoot
        // They get switched in FinishDMStandard()
        _root = new("/ (DMStandard)", DreamPath.Root);

        GlobalInitProc = new DMProc(-1, DMObjectTree.Root, null);
        _dmStandardRoot = null;
        _currentPass = 0;
        WaitingNodes.Clear();
        ParentTypes.Clear();
        NewProcs.Clear();

        DMObjectTree.Reset();
    }

    public static void DefineEverything() {
        if (_dmStandardRoot == null)
            FinishDMStandard();

        static void Pass(ObjectNode root) {
            foreach (var node in TraverseNodes(root)) {
                if (node is ObjectNode objectNode) {
                    objectNode.DefineType();
                } else if (node is ProcNode procNode) {
                    procNode.DefineProc();
                } else if (node is VarNode varNode) {
                    varNode.TryDefineVar();
                }
            }
        }

        // Pass 0
        DMExpressionBuilder.ScopeOperatorEnabled = false;
        Pass(_root);
        Pass(_dmStandardRoot!);

        int lastCount;
        do {
            _currentPass++;
            lastCount = WaitingNodes.Count;

            Pass(_root);
            Pass(_dmStandardRoot!);
        } while (WaitingNodes.Count < lastCount && WaitingNodes.Count > 0);

        // Scope operator pass
        DMExpressionBuilder.ScopeOperatorEnabled = true;
        Pass(_root);
        Pass(_dmStandardRoot!);

        // If there exists vars that didn't successfully compile, emit their errors
        foreach (var node in WaitingNodes) {
            if (node is not VarNode varNode) // TODO: If a type or proc fails?
                continue;
            if (varNode.LastError == null)
                continue;

            DMCompiler.Emit(WarningCode.ItemDoesntExist, varNode.LastError.Location,
                varNode.LastError.Message);
        }

        GlobalInitProc.ResolveLabels();
    }

    public static void FinishDMStandard() {
        _dmStandardRoot = _root;
        _root = new("/", DreamPath.Root);
    }

    public static void AddType(DreamPath type) {
        GetDMObjectNode(type); // Add it to our tree
    }

    public static DreamPath? UpwardSearch(DMObject start, DreamPath search) {
        var currentPath = start.Path;

        search.Type = DreamPath.PathType.Relative;

        while (true) {
            TypeNode node = GetDMObjectNode(currentPath);

            for (int i = 0; i < search.Elements.Length; i++) {
                var element = search.Elements[i];
                if (element == "verb")
                    element = "proc"; // TODO: Separate proc and verb on the code tree

                if (!node.TryGetChild(element, out var child))
                    break;

                if (i == search.Elements.Length - 1)
                    return currentPath.AddToPath(search.PathString);

                node = child;
            }

            if (currentPath == DreamPath.Root)
                return null;
            currentPath = currentPath.FromElements(0, -2);
        }
    }

    public static void Print() {
        PrintNode(_root);
        if (_dmStandardRoot != null)
            PrintNode(_dmStandardRoot);
    }

    private static void PrintNode(INode node, int level = 0) {
        if (node is TypeNode typeNode) {
            Console.Write(new string('\t', level));
            Console.WriteLine(typeNode);

            foreach (var child in typeNode.Children) {
                PrintNode(child, level + 1);
            }
        } else {
            Console.Write(new string('\t', level));
            Console.WriteLine(node);
        }
    }

    private static ObjectNode GetDMObjectNode(DreamPath path) {
        var node = _root;

        for (int i = 0; i < path.Elements.Length; i++) {
            var element = path.Elements[i];
            if (!node.TryGetChild(element, out var childNode)) {
                var creating = path.FromElements(0, i + 1);

                DMCompiler.VerbosePrint($"Adding {creating} to the code tree");
                childNode = new ObjectNode(element, creating);
                node.Children.Add(childNode);
                WaitingNodes.Add(childNode);
            }

            if (childNode is not ObjectNode objectNode)
                break;

            node = objectNode;
        }

        return node;
    }

    private static IEnumerable<INode> TraverseNodes(TypeNode from) {
        yield return from;

        foreach (var child in from.Children) {
            yield return child;

            if (child is TypeNode typeNode) {
                using var children = TraverseNodes(typeNode).GetEnumerator();
                while (children.MoveNext())
                    yield return children.Current;
            }
        }
    }
}
