using DMCompiler.Bytecode;

namespace DMCompiler.Optimizer;

internal class ControlFlowGraph(DMCompiler compiler) {
    // The first AnnotatedBytecodeLabel or AnnotatedBytecodeInstruction in each block; blocks are separated by labels and terminators
    List<int> BlockLeaderIndices = new(){0}; // start of instructions (idx 0) is a leader

    Dictionary<int, BasicBlock> StartIndexToBlock = new(); // The first instruction/label of the block
    List<BasicBlock> Blocks = new();

    public void Build(List<IAnnotatedBytecode> bytecode) {
        BuildBlocks(bytecode);
        WireEdges(bytecode);
    }

    private void BuildBlocks(List<IAnnotatedBytecode> bytecode) {
        for (var index = 0; index < bytecode.Count - 1; index++) { // - 1 because anything at the end of bytecode can't be a leader
            switch (bytecode[index])
            {
                case AnnotatedBytecodeLabel:
                    BlockLeaderIndices.Add(index);
                    break;
                case AnnotatedBytecodeInstruction inst:
                    if(IsTerminator(inst.Opcode))
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

            Blocks.Add(block);
            StartIndexToBlock.Add(start, block);
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
                    block.AddEdge(Blocks[idx + 1]);
            }

            // TODO: switch() handling

            // terminator handling
            if (block.Terminator != null && block.Terminator.Opcode == DreamProcOpcode.Jump) { // Only unconditional jump has successors
                var jumpLabel = block.Terminator.GetArg<AnnotatedBytecodeLabel>(0);
                var edgeBlock = LabelToBlock(bytecode, jumpLabel);
                if(edgeBlock is not null) block.AddEdge(edgeBlock);
                continue;
            }

            // conditional branch and fallthrough
            if (block.Instructions[^1] is AnnotatedBytecodeInstruction inst && IsConditionalBranch(inst.Opcode)) {
                var metadata = OpcodeMetadataCache.GetMetadata(inst.Opcode);
                var targetLabel = metadata.GetLabel();
            }
        }
    }

    private BasicBlock? LabelToBlock(List<IAnnotatedBytecode> bytecode, AnnotatedBytecodeLabel label) {
        foreach (var index in BlockLeaderIndices) {
            if(bytecode[index] == label)
                return Blocks[index];
        }

        return null;
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
    public void AddEdge(BasicBlock successor)
    {
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

