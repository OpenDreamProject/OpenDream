namespace OpenDreamShared.Compiler {
    public interface ASTNode<VisitorType> {
        public void Visit(VisitorType visitor);
    }
}
