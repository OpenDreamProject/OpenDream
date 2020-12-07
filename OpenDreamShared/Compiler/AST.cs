namespace OpenDreamShared.Compiler {
    interface ASTNode<VisitorType> {
        public void Visit(VisitorType visitor);
    }
}
