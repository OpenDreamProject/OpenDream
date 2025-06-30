﻿using DMCompiler.Bytecode;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectCallee(DreamObjectDefinition objectDefinition) : DreamObject(objectDefinition) {
    public DMProcState? ProcState;
    public long ProcStateId; // Used to ensure the proc state hasn't been reused for another proc

    protected override bool TryGetVar(string varName, out DreamValue value) {
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
                // TODO
                value = DreamValue.Null;
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

    protected override void SetVar(string varName, DreamValue value) {
        throw new Exception($"Cannot set var {varName} on /callee");
    }

    public override string GetDisplayName(StringFormatEncoder.FormatSuffix? suffix = null) {
        if (ProcState == null || ProcState.Id != ProcStateId)
            return string.Empty;

        return $"proc<{ProcState.Proc},0>"; // TODO: Call depth
    }
}
