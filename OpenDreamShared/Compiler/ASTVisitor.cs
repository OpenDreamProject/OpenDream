namespace OpenDreamShared.Compiler {
    public interface ASTVisitor {
        public void HandleCompileErrorException(CompileErrorException exception);
    }
}
