namespace DMCompiler.Compiler {
    interface ASTNode<VisitorType> {
        public void Visit(VisitorType visitor);
    }
}
