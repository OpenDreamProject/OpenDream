namespace Content.Compiler {
    public interface ASTVisitor {
        public void HandleCompileErrorException(CompileErrorException exception);
    }
}
