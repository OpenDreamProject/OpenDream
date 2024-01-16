namespace DMCompiler.DM.Optimizer.SSAInstructions;

public interface SSAObject {
    public SSAObjectType GetObjectType();
}

public enum SSAObjectType {
    Instruction,
    Variable,
    Label,
    Phi,
    Jump,
    Return
}
