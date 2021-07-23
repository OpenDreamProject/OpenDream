namespace Content.Shared.Compiler {
    public interface ASTVisitor {
        public void HandleCompileErrorException(CompileErrorException exception);
    }
}
