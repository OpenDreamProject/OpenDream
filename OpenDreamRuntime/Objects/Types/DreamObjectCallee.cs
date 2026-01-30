using DMCompiler.Bytecode;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectCallee : DreamObject {
    public DMProcState? ProcState;
    public long ProcStateId; // Used to ensure the proc state hasn't been reused for another proc
    private DreamObjectCallee? _caller; // Caching caller prevents issues with returning incorrect info when the proc ends or calls another proc

    public DreamObjectCallee(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        SetCaller(); // we need to cache _caller recursively
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        // TODO: This ProcState check doesn't match byond behavior?
        if (ProcState == null || ProcState.Id != ProcStateId)
            throw new Exception("This callee has expired");

        switch (varName) {
            case "proc":
                value = new(ProcState.Proc);
                return true;
            case "args":
                value = new(new ProcArgsList(ObjectTree.List.ObjectDefinition, ProcState));
                return true;
            case "caller":
                SetCaller(); // sometimes ProcState is null in the constructor?
                value = new DreamValue(_caller);
                return true;
            case "name":
                value = new(ProcState.Proc.VerbName);
                return true;
            case "desc":
                value = ProcState.Proc.VerbDesc != null ? new(ProcState.Proc.VerbDesc) : DreamValue.Null;
                return true;
            case "category":
                value = ProcState.Proc.VerbCategory != null ? new(ProcState.Proc.VerbCategory) : DreamValue.Null;
                return true;
            case "file":
                value = new(ProcState.Proc.GetSourceAtOffset(0).Source);
                return true;
            case "line":
                value = new(ProcState.Proc.GetSourceAtOffset(0).Line);
                return true;
            case "src":
                value = new(ProcState.Instance);
                return true;
            case "usr":
                value = new(ProcState.Usr);
                return true;
            case "type":
                value = new(ObjectDefinition.TreeEntry);
                return true;

            default:
                // Do not call base.TryGetVar(), those base vars do not exist here
                value = DreamValue.Null;
                return false;
        }
    }

    /// <summary>
    /// Sets <see cref="_caller"/> if it hasn't been already and <see cref="ProcState"/> is not null
    /// </summary>
    private void SetCaller() {
        if (ProcState is null || _caller is not null) return;

        var caller = ObjectTree.CreateObject<DreamObjectCallee>(ObjectTree.Callee);
        var peekStack = ProcState.Thread.PeekStack(1);
        caller.ProcState = (DMProcState)peekStack;
        caller.ProcStateId = peekStack.Id;
        _caller = caller;
    }

    protected override void SetVar(string varName, DreamValue value) {
        throw new Exception($"Cannot set var {varName} on /callee");
    }

    public override string GetDisplayName(StringFormatEncoder.FormatSuffix? suffix = null) {
        if (ProcState == null || ProcState.Id != ProcStateId)
            return string.Empty;

        return $"proc<{ProcState.Proc},0>"; // TODO: Call depth
    }
}
