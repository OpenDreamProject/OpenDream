namespace OpenDreamShared.Compiler {
    public interface ASTNode<VisitorType> where VisitorType:ASTVisitor {
        public void Visit(VisitorType visitor);
    }
}
