using OpenDreamShared.Dream;

namespace DMCompiler.DM {
    interface DMASTVisitor {
        public object VisitFile(DMASTFile file);
        public object VisitBlockInner(DMASTBlockInner file);
        public object VisitStatement(DMASTStatement statement);
        public object VisitPath(DMASTPath path);
        public object VisitPathElement(DMASTPathElement pathElement);
    }

    interface DMASTNode : ASTNode<DMASTVisitor> {
        
    }

    class DMASTFile : DMASTNode {
        public DMASTBlockInner BlockInner;

        public DMASTFile(DMASTBlockInner blockInner) {
            BlockInner = blockInner;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitFile(this);
        }
    }

    class DMASTBlockInner : DMASTNode {
        public DMASTStatement[] Statements;

        public DMASTBlockInner(DMASTStatement[] statements) {
            Statements = statements;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitBlockInner(this);
        }
    }

    class DMASTStatement : DMASTNode {
        public DMASTPath Path;

        public DMASTStatement(DMASTPath path) {
            Path = path;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitStatement(this);
        }
    }

    class DMASTPath : DMASTNode {
        public DreamPath.PathType PathType;
        public DMASTPathElement[] PathElements;

        public DMASTPath(DreamPath.PathType pathType, DMASTPathElement[] pathElements = null) {
            PathType = pathType;
            PathElements = pathElements;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitPath(this);
        }
    }

    class DMASTPathElement : DMASTNode {
        public string Element;

        public DMASTPathElement(string element) {
            Element = element;
        }

        public object Visit(DMASTVisitor visitor) {
            return visitor.VisitPathElement(this);
        }
    }
}
