namespace DMCompiler {
    interface ASTNode<VisitorType> {
        public object Visit(VisitorType visitor);
    }
}
