using DMCompiler.Compiler;
using DMCompiler.Compiler.DM.AST;

namespace DMCompiler.DM;

internal partial class DMCodeTree {
    private class ProcsNode() : TypeNode("proc");

    private class ProcNode(DreamPath owner, DMASTProcDefinition procDef) : TypeNode(procDef.Name) {
        private string ProcName => procDef.Name;
        private bool IsOverride => procDef.IsOverride;

        private bool _defined;

        public void DefineProc(DMCompiler compiler) {
            if (_defined)
                return;
            if (!compiler.DMObjectTree.TryGetDMObject(owner, out var dmObject))
                return;

            _defined = true;
            WaitingNodes.Remove(this);

            bool hasProc = dmObject.HasProc(ProcName);
            if (hasProc && !IsOverride && !dmObject.OwnsProc(ProcName) && !procDef.Location.InDMStandard) {
                compiler.Emit(WarningCode.DuplicateProcDefinition, procDef.Location,
                    $"Type {owner} already inherits a proc named \"{ProcName}\" and cannot redefine it");
                return; // TODO: Maybe fallthrough since this error is a little pedantic?
            }

            DMProc proc = compiler.DMObjectTree.CreateDMProc(dmObject, procDef);

            if (dmObject == compiler.DMObjectTree.Root) { // Doesn't belong to a type, this is a global proc
                if(IsOverride) {
                    compiler.Emit(WarningCode.InvalidOverride, procDef.Location,
                        $"Global procs cannot be overridden - '{ProcName}' override will be ignored");
                    //Continue processing the proc anyhoo, just don't add it.
                } else {
                    compiler.VerbosePrint($"Adding global proc {procDef.Name}() on pass {_currentPass}");
                    compiler.DMObjectTree.AddGlobalProc(proc);
                }
            } else {
                compiler.VerbosePrint($"Adding proc {procDef.Name}() to {dmObject.Path} on pass {_currentPass}");
                dmObject.AddProc(proc, forceFirst: procDef.Location.InDMStandard);
            }

            foreach (var varDecl in GetVarDeclarations()) {
                if (!varDecl.IsGlobal)
                    continue;

                var procGlobalNode = new ProcGlobalVarNode(owner, proc, varDecl);
                Children.Add(procGlobalNode);
                WaitingNodes.Add(procGlobalNode);
            }

            if (proc.IsVerb) {
                dmObject.AddVerb(proc);
            }
        }

        // TODO: Remove this entirely
        private IEnumerable<DMASTProcStatementVarDeclaration> GetVarDeclarations() {
            var statements = new Queue<DMASTProcStatement>(procDef.Body?.Statements ?? []);

            static void AddBody(Queue<DMASTProcStatement> queue, DMASTProcBlockInner? block) {
                if (block is null)
                    return;

                foreach (var stmt in block.Statements)
                    queue.Enqueue(stmt);
            }

            while (statements.TryDequeue(out var stmt)) {
                switch (stmt) {
                    // TODO multiple var definitions.
                    case DMASTProcStatementVarDeclaration ps: yield return ps; break;

                    case DMASTProcStatementSpawn ps: AddBody(statements, ps.Body); break;
                    case DMASTProcStatementFor ps: AddBody(statements, ps.Body); break;
                    case DMASTProcStatementWhile ps: AddBody(statements, ps.Body); break;
                    case DMASTProcStatementDoWhile ps: AddBody(statements, ps.Body); break;
                    case DMASTProcStatementInfLoop ps: AddBody(statements, ps.Body); break;
                    case DMASTProcStatementIf ps:
                        AddBody(statements, ps.Body);
                        AddBody(statements, ps.ElseBody);
                        break;
                    case DMASTProcStatementTryCatch ps:
                        AddBody(statements, ps.TryBody);
                        AddBody(statements, ps.CatchBody);
                        break;
                    // TODO Good luck if you declare a static var inside a switch
                    case DMASTProcStatementSwitch ps: {
                        foreach (var swCase in ps.Cases) {
                            AddBody(statements, swCase.Body);
                        }

                        break;
                    }
                }
            }
        }

        public override string ToString() {
            return ProcName + "()";
        }
    }

    public void AddProc(DreamPath owner, DMASTProcDefinition procDef) {
        var node = GetDMObjectNode(owner);
        var procNode = new ProcNode(owner, procDef);

        if (procDef is { Name: "New", IsOverride: false })
            NewProcs[owner] = procNode; // We need to be ready to define New() as soon as the type is created

        node.AddProcsNode().Children.Add(procNode);
        WaitingNodes.Add(procNode);
    }
}
