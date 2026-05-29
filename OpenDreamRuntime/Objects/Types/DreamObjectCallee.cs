using DMCompiler.Bytecode;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectCallee(DreamObjectDefinition objectDefinition) : DreamObject(objectDefinition) {
    public ProcState? ProcState;
    public long ProcStateId; // Used to ensure the proc state hasn't been reused for another proc
    private DreamObjectCallee? _caller; // cached, we'll build the call stack only if we actually need to

    public static DreamObjectCallee FromDMProcState(DMProcState procState) {
        var proc = procState.Proc;
        var callee = proc.ObjectTree.CreateObject<DreamObjectCallee>(proc.ObjectTree.Callee);
        callee.ProcState = procState;
        callee.ProcStateId = procState.Id;
        return callee;
    }

    protected override void HandleDeletion() {
        _caller?.DecRef();
        _caller = null;
        base.HandleDeletion();
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        // TODO: This ProcState check doesn't match byond behavior?
        if (ProcState == null || ProcState.Id != ProcStateId)
            throw new Exception("This callee has expired");

        switch (varName) {
            case "proc":
                value = ProcState.Proc != null ? new(ProcState.Proc) : DreamValue.Null;
                return true;
            case "args":
                value = new(new ProcArgsList(ObjectTree.List.ObjectDefinition, ProcState));
                return true;
            case "caller":
                if(_caller is null)
                    SetCaller();
                value = new DreamValue(_caller);
                return true;
            case "name":
                value = ProcState.Proc?.VerbName != null ? new(ProcState.Proc.VerbName) : DreamValue.Null;
                return true;
            case "desc":
                value = ProcState.Proc?.VerbDesc != null ? new(ProcState.Proc.VerbDesc) : DreamValue.Null;
                return true;
            case "category":
                value = ProcState.Proc?.VerbCategory != null ? new(ProcState.Proc.VerbCategory) : DreamValue.Null;
                return true;
            case "file":
                value = ProcState.Proc is DMProc procFile
                    ? new DreamValue(procFile.GetSourceAtOffset(0).Source)
                    : DreamValue.Null;
                return true;
            case "line":
                value = ProcState.Proc is DMProc procLine
                    ? new DreamValue(procLine.GetSourceAtOffset(0).Line)
                    : DreamValue.Null;
                return true;
            case "src":
                ProcState.Instance?.IncRef();
                value = new(ProcState.Instance);
                return true;
            case "usr":
                ProcState.Usr?.IncRef();
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
        int ourIndex = ProcState.Thread.StackDepth - ProcState.Depth;
        ProcState? callerProcState = ProcState.Thread.PeekStack(ourIndex + 1);
        if(callerProcState is not DMProcState dmProcState) { // avert your eyes
            if(callerProcState is not InitDreamObjectState) return;
            callerProcState = ProcState.Thread.PeekStack(ourIndex + 2);
            if(callerProcState is not DMProcState realDmProcState) return;
            dmProcState = realDmProcState;
        }


        _caller = FromDMProcState(dmProcState);
        _caller.IncRef();
        _caller.SetCaller();
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
