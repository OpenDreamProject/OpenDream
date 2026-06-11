using System.Diagnostics.CodeAnalysis;
using DMCompiler.Bytecode;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectCallee(DreamObjectDefinition objectDefinition) : DreamObject(objectDefinition) {
    public DMProcState? ProcState;
    public long ProcStateId; // Used to ensure the proc state hasn't been reused for another proc

    [MemberNotNullWhen(false, nameof(ProcState))]
    public bool Expired => ProcState == null || ProcState.Id != ProcStateId;

    public static DreamObjectCallee FromDMProcState(DMProcState procState) {
        var proc = procState.Proc;
        var callee = proc.ObjectTree.CreateObject<DreamObjectCallee>(proc.ObjectTree.Callee);
        callee.ProcState = procState;
        callee.ProcStateId = procState.Id;
        return callee;
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        // TODO: This ProcState check doesn't match byond behavior?
        if (Expired)
            throw new Exception("This callee has expired");

        switch (varName) {
            case "proc":
                value = new(ProcState.Proc);
                return true;
            case "args":
                value = new(new ProcArgsList(ObjectTree.List.ObjectDefinition, ProcState));
                return true;
            case "caller":
                var caller = ProcState.Caller;
                while(caller is not (DMProcState or null))
                    caller = caller.Caller;

                value = caller is DMProcState dmCaller ? new DreamValue(dmCaller.CalleeObject) : DreamValue.Null;
                value.IncRef();
                return true;
            case "name":
                value = new(ProcState.Proc.VerbName);
                return true;
            case "desc":
                value = ProcState.Proc.VerbDesc is not null
                    ? new(ProcState.Proc.VerbDesc)
                    : DreamValue.Null;
                return true;
            case "category":
                value = ProcState.Proc.VerbCategory is not null
                    ? new(ProcState.Proc.VerbCategory)
                    : DreamValue.Null;
                return true;
            case "file":
                value = new DreamValue(ProcState.Proc.GetSourceAtOffset(0).Source);
                return true;
            case "line":
                value = new DreamValue(ProcState.Proc.GetSourceAtOffset(0).Line);
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

    protected override void SetVar(string varName, DreamValue value) {
        throw new Exception($"Cannot set var {varName} on /callee");
    }

    public override string GetDisplayName(StringFormatEncoder.FormatSuffix? suffix = null) {
        if (Expired)
            return string.Empty;

        return $"proc<{ProcState.Proc},{ProcState.Id}>"; // ID isn't accurate but does that really matter?
    }
}
