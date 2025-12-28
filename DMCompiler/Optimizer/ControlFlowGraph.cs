using System.Linq;
using System.Text;
using DMCompiler.Bytecode;
using DMCompiler.DM;

namespace DMCompiler.Optimizer;

internal class ControlFlowGraph(DMCompiler compiler, DMProc proc) {
    // The first AnnotatedBytecodeLabel or AnnotatedBytecodeInstruction in each block; blocks are separated by labels and terminators
    List<int> BlockLeaderIndices = new(){0}; // start of instructions (idx 0) is a leader
    Dictionary<IAnnotatedBytecode, BasicBlock> StartBytecodeToBlock = new(); // The first instruction/label of the block
    List<BasicBlock> Blocks = new();
    private DMProc Proc = proc;

    public void Build(List<IAnnotatedBytecode> bytecode) {
        if(compiler.Settings.NoOpts) // Optimizations are disabled
            return;
        BuildBlocks(bytecode);
        if (Blocks.Count == 0) return; // empty proc declaration

        WireEdges(bytecode);
        PruneUnreachable();
    }

    // TODO: It would be nice to print the arguments properly for all opcodes; need to pass CFG info to DMDisassembler somehow
    public void Dump() {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Dumping control flow graph...");
        foreach (var b in Blocks) {
            sb.AppendLine($"B{b.Id} [{b.StartIndex}..{b.EndIndexExclusive}):");
            foreach (var ins in b.Instructions) {
                if(ins is AnnotatedBytecodeInstruction opcode)
                    if(IsConditionalBranch(opcode.Opcode) || opcode.Opcode == DreamProcOpcode.Jump)
                        sb.AppendLine($"  {opcode.Opcode} {opcode.GetArg<AnnotatedBytecodeLabel>(0).LabelName}");
                    else
                        sb.AppendLine($"  {opcode.Opcode}");
                if(ins is AnnotatedBytecodeLabel label)
                    sb.AppendLine($"  {label.LabelName}");
            }

            sb.Append("  predecessors: ").AppendLine(string.Join(", ", b.Predecessors.Select(s => $"B{s.Id}")));
            sb.Append("  successors: ").AppendLine(string.Join(", ", b.Successors.Select(s => $"B{s.Id}")));
            sb.AppendLine();
        }

        var result = sb.ToString();
        Console.WriteLine(result);
    }

    private void BuildBlocks(List<IAnnotatedBytecode> bytecode) {
        // TODO: Validate switch() behavior
        // TODO: Proper try/catch exception handler shenanigans

        if(Proc.Name == "push" && Proc.Location.Line == 102)
            Console.WriteLine("b");

        if (bytecode.Count == 0)
            return; // empty proc

        for (int i = 0; i < bytecode.Count; i++) {
            switch (bytecode[i]) {
                case AnnotatedBytecodeLabel:
                    // Labels always start a new block
                    if (!BlockLeaderIndices.Contains(i))
                        BlockLeaderIndices.Add(i);
                    break;

                case AnnotatedBytecodeInstruction inst:
                    bool endsBlock =
                        IsTerminator(inst.Opcode) ||
                        IsConditionalBranch(inst.Opcode);

                    if (endsBlock) {
                        int nextIdx = i + 1;
                        if (nextIdx < bytecode.Count) {
                            // next instruction (or next label) starts a new block
                            if (!BlockLeaderIndices.Contains(nextIdx))
                                BlockLeaderIndices.Add(nextIdx);
                        }
                    }

                    break;
            }
        }

        for (int bi = 0; bi < BlockLeaderIndices.Count; bi++) {
            int start = BlockLeaderIndices[bi];
            int end = (bi + 1 < BlockLeaderIndices.Count)
                ? BlockLeaderIndices[bi + 1]
                : bytecode.Count;

            var block = new BasicBlock {
                Id = bi,
                StartIndex = start,
                EndIndexExclusive = end
            };

            for (int j = start; j < end; j++) {
                block.Instructions.Add(bytecode[j]);
            }

            // The last actual instruction in this block might be a branch/jump/return/etc.
            for (int j = end - 1; j >= start; j--) {
                if (bytecode[j] is AnnotatedBytecodeInstruction lastIns) {
                    // treat conditional branches as legal terminators too
                    if (IsTerminator(lastIns.Opcode) || IsConditionalBranch(lastIns.Opcode)) {
                        block.Terminator = lastIns;
                    }

                    break;
                }
            }

            Blocks.Add(block);
            StartBytecodeToBlock[bytecode[start]] = block;
        }
    }

    /// <summary>
    /// Explicit unconditional jump/throw/return termination opcodes
    /// </summary>
    private bool IsTerminator(DreamProcOpcode op) => op switch {
        DreamProcOpcode.Return or DreamProcOpcode.ReturnReferenceValue or DreamProcOpcode.ReturnFloat
            or DreamProcOpcode.Throw
            or DreamProcOpcode.Jump => true,
        _ => false
    };

    // TODO: Consider moving this to a bool on OpcodeMetadata?
    private bool IsConditionalBranch(DreamProcOpcode op) => op switch {
        DreamProcOpcode.JumpIfFalse
            or DreamProcOpcode.BooleanAnd
            or DreamProcOpcode.BooleanOr
            or DreamProcOpcode.JumpIfNull
            or DreamProcOpcode.JumpIfNullNoPop
            or DreamProcOpcode.JumpIfTrueReference
            or DreamProcOpcode.JumpIfFalseReference
            or DreamProcOpcode.JumpIfReferenceFalse
            or DreamProcOpcode.Enumerate
            or DreamProcOpcode.EnumerateAssoc
            or DreamProcOpcode.EnumerateNoAssign
            or DreamProcOpcode.SwitchCase
            or DreamProcOpcode.SwitchCaseRange
            or DreamProcOpcode.SwitchOnFloat
            or DreamProcOpcode.SwitchOnString => true,
        _ => false
    };

    private bool IsEnumeratorSetup(DreamProcOpcode op) => op switch {
        DreamProcOpcode.CreateFilteredListEnumerator
            or DreamProcOpcode.CreateListEnumerator
            or DreamProcOpcode.CreateRangeEnumerator
            or DreamProcOpcode.CreateTypeEnumerator
            => true,
        _ => false
    };

    /// <summary>
    /// Build successor/predecessor edges for all blocks
    /// Assumes:
    ///   - block.Terminator is the last control-flow-relevant instruction
    ///   - IsTerminator(op) -> no fallthrough
    ///   - IsConditionalBranch(op) -> has an arg label and does fallthrough
    ///   - non-branching last instruction -> implicit fallthrough only
    /// </summary>
    private void WireEdges(List<IAnnotatedBytecode> bytecode) {
        for (int bi = 0; bi < Blocks.Count; bi++) {
            var block = Blocks[bi];

            var lastInstr = block.Terminator ?? GetLastInstruction(block);
            if (lastInstr == null)
                continue;

            var op = lastInstr.Opcode;

            // Track whether we've added a "thread continuation" edge that should
            // replace normal fallthrough for the parent thread
            bool suppressNormalFallthrough = false;

            // 1. Conditional branches and Jump
            if (IsConditionalBranch(op) || op == DreamProcOpcode.Jump) {
                var labelObj = lastInstr.GetArg<AnnotatedBytecodeLabel>(0);

                if (StartBytecodeToBlock.TryGetValue(labelObj, out var targetBlock)) {
                    block.AddEdge(targetBlock, Proc);
                } else {
                    Console.WriteLine(
                        $"[CFG] Missing label resolution in {Proc.Name}: {op} -> {labelObj.LabelName}"
                    );
                }
            }

            // 2. Enumerator-setup (CreateFilteredListEnumerator / etc)
            //    After setup, control transfers into the loop header block (next block)
            if (IsEnumeratorSetup(op)) {
                if (bi + 1 < Blocks.Count) {
                    var headerBlock = Blocks[bi + 1];
                    block.AddEdge(headerBlock, Proc);
                    // Enumerator setup does NOT kill normal fallthrough; we still
                    // run AddFallthroughEdge below
                }
            }

            // 3. Spawn handling
            // walk all instructions in this block looking for Spawn
            // Semantics:
            //   Spawn <label L>
            //     - code after Spawn (still in this block) runs in the new thread,
            //       returns on its own
            //     - the original thread resumes at label L
            // So CFG-wise for reachability:
            //   - this block has an edge to block(label L)
            //   - and we should NOT treat normal sequential fallthrough from this block
            //     as the parent's continuation, because parent doesn't run the
            //     "spawn body" inline
            // Note: we *don't* need an edge to the "spawn body" because that body
            // is physically in the same BasicBlock already, so it's inherently marked
            // reachable as part of this block
            foreach (var instr in block.Instructions) {
                if (instr is AnnotatedBytecodeInstruction abi &&
                    abi.Opcode == DreamProcOpcode.Spawn)
                {
                    var contLabel = abi.GetArg<AnnotatedBytecodeLabel>(0);

                    if (StartBytecodeToBlock.TryGetValue(contLabel, out var contBlock)) {
                        block.AddEdge(contBlock, Proc);
                        suppressNormalFallthrough = true;
                    } else {
                        Console.WriteLine(
                            $"[CFG] Missing spawn continuation target in {Proc.Name}: {contLabel.LabelName}"
                        );
                    }
                }
            }

            // 4. Normal fallthrough
            // add fallthrough only if:
            //   - the last opcode is not a hard terminator
            //   - and Spawn did NOT tell us "parent resumes via label instead"
            // the suppressNormalFallthrough flag protects us in any case where Spawn is seen in a non-terminating block
            if (!IsTerminator(op) && !suppressNormalFallthrough) {
                AddFallthroughEdge(block, bytecode);
            }
        }
    }



    private AnnotatedBytecodeInstruction? GetLastInstruction(BasicBlock block)
    {
        for (int i = block.Instructions.Count - 1; i >= 0; i--) {
            if (block.Instructions[i] is AnnotatedBytecodeInstruction abi) {
                return abi;
            }
        }

        return null;
    }

    // Add the fallthrough edge: from this block to the block that starts exactly at EndIndexExclusive (the next instruction after this block)
    private void AddFallthroughEdge(
        BasicBlock from,
        List<IAnnotatedBytecode> bytecode)
    {
        int nextInstrIndex = from.EndIndexExclusive;
        if (nextInstrIndex >= bytecode.Count)
            return;

        var leaderKey = bytecode[nextInstrIndex];
        if (!StartBytecodeToBlock.TryGetValue(leaderKey, out var fallBlock))
            return;

        if (fallBlock != from)
            from.AddEdge(fallBlock, Proc);
    }

    private void PruneUnreachable() {
        if (Blocks.Count == 0)
            return;

        // Reachability from entry
        var seen = new HashSet<BasicBlock>();
        var stack = new Stack<BasicBlock>();
        stack.Push(Blocks[0]);

        while (stack.Count > 0) {
            var block = stack.Pop();
            if (!seen.Add(block)) continue;
            foreach (var succ in block.Successors) {
                stack.Push(succ);
            }
        }

        // Drop unreachable blocks from Blocks
        var deadBlocks = Blocks.Where(b => !seen.Contains(b)).ToList();
        foreach (var deadBlock in deadBlocks) {
            if(deadBlock.Instructions.Count > 2)
                compiler.ForcedWarning(deadBlock.Instructions[0].GetLocation(), "Dead code lol");
            Blocks.Remove(deadBlock);
        }

        // Trim edges that still point to removed blocks
        foreach (var block in Blocks) {
            block.Successors.RemoveAll(s => !seen.Contains(s));
            block.Predecessors.RemoveAll(p => !seen.Contains(p));
        }

        // Repair symmetry between Successors and Predecessors
        foreach (var block in Blocks) {
            foreach (var succ in block.Successors) {
                if (!succ.Predecessors.Contains(block))
                    succ.Predecessors.Add(block);
            }

            foreach (var pred in block.Predecessors) {
                if (!pred.Successors.Contains(block))
                    pred.Successors.Add(block);
            }
        }
    }
}

/// <summary>
/// Represents a continuous region of bytecode with a single entry and no internal control flow splits. Ends with a single terminator opcode
/// </summary>
internal sealed class BasicBlock
{
    /// <summary>
    /// Sequential ID for debugging and CFG ordering
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Inclusive start index in the proc’s instructions
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// Exclusive end index in the proc’s instructions
    /// </summary>
    public int EndIndexExclusive { get; set; }

    /// <summary>
    /// All bytecode instructions in the block
    /// </summary>
    public List<IAnnotatedBytecode> Instructions { get; } = new();

    /// <summary>
    /// The terminator instruction (must be the final instruction (if present))
    /// </summary>
    public AnnotatedBytecodeInstruction? Terminator { get; set; }

    /// <summary>
    /// Forward control-flow edges: All possible successor blocks
    /// </summary>
    public List<BasicBlock> Successors { get; } = new();

    /// <summary>
    /// Reverse control-flow edges: All blocks that can reach this one
    /// </summary>
    public List<BasicBlock> Predecessors { get; } = new();

    /// <summary>
    /// Adds a bidirectional edge to another block
    /// </summary>
    public void AddEdge(BasicBlock successor, DMProc proc)
    {
        if (proc.Name == "foo") {
            Console.WriteLine("q");
        }

        if (!Successors.Contains(successor))
            Successors.Add(successor);
        if (!successor.Predecessors.Contains(this))
            successor.Predecessors.Add(this);
    }

    public override string ToString()
    {
        return $"Block {Id} [{StartIndex}..{EndIndexExclusive})";
    }
}

