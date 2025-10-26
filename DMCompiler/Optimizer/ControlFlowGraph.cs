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
                    if(opcode.Opcode == DreamProcOpcode.Jump)
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
        if (bytecode.Count == 0) return; // empty proc declaration // TODO: We can probably use this case to optimize caller procs
        for (var index = 0; index < bytecode.Count; index++) {
            switch (bytecode[index])
            {
                case AnnotatedBytecodeLabel:
                    BlockLeaderIndices.Add(index);
                    break;
                case AnnotatedBytecodeInstruction inst:
                    if(index + 1 < bytecode.Count && IsTerminator(inst.Opcode) && bytecode[index + 1] is not AnnotatedBytecodeLabel) // labels are handled by the above case & also don't include the final instruction
                        BlockLeaderIndices.Add(index + 1);
                    break;
            }

            // TODO: Better handling for "catch" blocks / proper exception handling table
            // TODO: switch() statements aren't properly split into blocks yet
        }

        for (int idx = 0; idx < BlockLeaderIndices.Count; idx++) {
            var start = BlockLeaderIndices[idx];
            var end = (idx + 1 < BlockLeaderIndices.Count) ? BlockLeaderIndices[idx + 1] : bytecode.Count;

            var block = new BasicBlock() {Id = idx, StartIndex = start, EndIndexExclusive = end};
            for (int i = start; i < end; i++)
                block.Instructions.Add(bytecode[i]);
            if (block.Instructions.Count > 0 && block.Instructions[^1] is AnnotatedBytecodeInstruction ins && IsTerminator(ins.Opcode))
                block.Terminator = ins;

            if(block.Instructions.Count == 0)
                Console.WriteLine("e");

            Blocks.Add(block);
            if(bytecode[start] is AnnotatedBytecodeLabel label && label.LabelName == "label3")
                Console.WriteLine("e");
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

    private void WireEdges(List<IAnnotatedBytecode> bytecode) {
        for (var idx = 0; idx < Blocks.Count; idx++) {
            var block = Blocks[idx];
            if (block.Instructions.Count == 0) { // empty block, fall through to next
                if(idx + 1 < Blocks.Count)
                    block.AddEdge(Blocks[idx + 1], Proc);
            }

            // TODO: switch() handling

            // terminator handling
            if (block.Terminator != null) {
                if (block.Terminator.Opcode == DreamProcOpcode.Jump) { // Only unconditional jump has successors
                    var jumpLabel = block.Terminator.GetArg<AnnotatedBytecodeLabel>(0);
                    if (StartBytecodeToBlock.TryGetValue(jumpLabel, out var edgeBlock))
                        block.AddEdge(edgeBlock, Proc);
                }

                // other terminators just don't have successors; continue
                continue;
            }

            if(block.Instructions.Count == 0) continue; // TODO: This shouldn't be happening/necessary

            // conditional branch and fallthrough
            if (block.Instructions[^1] is AnnotatedBytecodeInstruction inst && IsConditionalBranch(inst.Opcode)) {
                var metadata = OpcodeMetadataCache.GetMetadata(inst.Opcode);
                var targetLabelIdx = metadata.GetLabelIndex();
                if (targetLabelIdx is not null) {
                    var targetLabel = bytecode[targetLabelIdx.Value];
                    var targetBlock = StartBytecodeToBlock[targetLabel];
                    block.AddEdge(targetBlock, Proc);
                }

                // implied fallthrough to next instruction
                var offset = 1;
                while (bytecode[idx + offset] is not AnnotatedBytecodeInstruction) {
                    offset++;
                    if(idx + offset >= Blocks.Count) break;
                }

                if (idx + offset < Blocks.Count) block.AddEdge(Blocks[idx + offset], Proc);
                continue;
            }

            // pure fallthrough
            if (idx + 1 < Blocks.Count) block.AddEdge(Blocks[idx + 1], Proc);
        }
    }

    private void PruneUnreachable() {
        var seen = new HashSet<BasicBlock>();
        var stack = new Stack<BasicBlock>();
        stack.Push(Blocks[0]);
        while (stack.Count > 0) {
            var block = stack.Pop();
            if (!seen.Add(block)) continue;
            foreach (var successor in block.Successors) stack.Push(successor);
        }

        if(Proc.Name == "foo")
            Console.WriteLine("Done");

        Blocks.RemoveAll(b => !seen.Contains(b));

        // Fix succs/preds after prune
        foreach (var block in Blocks) {
            block.Successors.RemoveAll(s => !seen.Contains(s));
            block.Predecessors.RemoveAll(p => !seen.Contains(p));
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
    /// True if this block is reachable from the proc start after CFG construction
    /// </summary>
    public bool Reachable { get; set; } = true;

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

