using System;
using System.Collections.Generic;
using System.Linq;
using DMCompiler.Bytecode;

namespace DMCompiler.DM.Optimizer;

public class CFGPasses {
    // Step through the CFG list, remove any blocks which lack a predecessor and are not the first block
    public static List<CFGBasicBlock> RemoveUnreachableBlocks(List<CFGBasicBlock> output) {
        var removed = 0;
        for (var i = 1; i < output.Count; i++)
            if (output[i].Predecessors.Count == 0) {
                removed++;
                output.RemoveAt(i);
                i--;
            }

        if (removed > 0)
            Console.WriteLine($"Removed {removed} unreachable blocks");
        return output;
    }

    // Replace an unconditional jump into a block which immediately jumps to another block with a jump to the final block
    public static List<CFGBasicBlock> RemoveUnnecessaryJumps(List<CFGBasicBlock> output, out int didChange) {
        var removed = 0;
        for (var i = 0; i < output.Count; i++)
        for (var j = 0; j < output[i].Bytecode.Count; j++)
            if (IsJump(output[i].Bytecode[j])) {
                var target = ExtractJumpTarget(output[i].Bytecode[j]);
                var oldLocation = (output[i].Bytecode[j] as AnnotatedBytecodeInstruction)!.Location;
                if (!output[i].LabelMap.TryGetValue(target, out var targetIndex)) {
                    targetIndex = 0;
                    continue;
                }

                // Check for any number of labels at the target
                var targetBlock = GetBlockByID(output, targetIndex);
                var targetBytecode = targetBlock.Bytecode.ToList();
                while (targetBytecode.Count > 0 && targetBytecode[0] is AnnotatedBytecodeLabel)
                    targetBytecode.RemoveAt(0);

                // Check for an unconditional jump at the target
                if (targetBytecode.Count == 1 && IsUnconditionalJump(targetBytecode[0])) {
                    var actualTarget = ExtractJumpTarget(targetBytecode[0]);
                    output[i].Bytecode[j] = new AnnotatedBytecodeInstruction(DreamProcOpcode.Jump, 0, oldLocation);
                    output[i].Bytecode[j].AddArg(new AnnotatedBytecodeLabel(actualTarget, oldLocation));
                    removed++;
                }
            }

        if (removed > 0)
            Console.WriteLine($"Removed {removed} unnecessary jumps");
        didChange = removed;
        return output;
    }

    private static CFGBasicBlock GetBlockByID(List<CFGBasicBlock> blocks, int id) {
        foreach (var block in blocks)
            if (block.id == id)
                return block;
        throw new Exception("Block not found");
    }

    private static string ExtractJumpTarget(IAnnotatedBytecode bytecode) {
        if (bytecode is AnnotatedBytecodeInstruction instruction)
            switch (instruction.Opcode) {
                // label in arg 0
                case DreamProcOpcode.SwitchCase:
                case DreamProcOpcode.SwitchCaseRange:
                case DreamProcOpcode.JumpIfFalse:
                case DreamProcOpcode.JumpIfTrue:
                case DreamProcOpcode.BooleanAnd:
                case DreamProcOpcode.BooleanOr:
                case DreamProcOpcode.JumpIfNull:
                case DreamProcOpcode.JumpIfNullNoPop:
                case DreamProcOpcode.EnumerateNoAssign:
                case DreamProcOpcode.Spawn:
                    return (instruction.GetArgs()[0] as AnnotatedBytecodeLabel)!.LabelName;

                // label in arg 1
                case DreamProcOpcode.Enumerate:
                case DreamProcOpcode.JumpIfFalseReference:
                case DreamProcOpcode.JumpIfTrueReference:
                    return (instruction.GetArgs()[1] as AnnotatedBytecodeLabel)!.LabelName;

                // label in arg 0
                case DreamProcOpcode.Jump:
                    return (instruction.GetArgs()[0] as AnnotatedBytecodeLabel)!.LabelName;

                // No label
                default:
                    return "";
            }

        return "";
    }

    private static bool IsJump(IAnnotatedBytecode bytecode) {
        return bytecode is AnnotatedBytecodeInstruction instruction && instruction.Opcode is DreamProcOpcode.SwitchCase
            or
            DreamProcOpcode.SwitchCaseRange or
            DreamProcOpcode.JumpIfFalse or
            DreamProcOpcode.JumpIfTrue or
            DreamProcOpcode.BooleanAnd or
            DreamProcOpcode.BooleanOr or
            DreamProcOpcode.JumpIfNull or
            DreamProcOpcode.JumpIfNullNoPop or
            DreamProcOpcode.EnumerateNoAssign or
            DreamProcOpcode.Spawn or
            DreamProcOpcode.Enumerate or
            DreamProcOpcode.JumpIfFalseReference or
            DreamProcOpcode.JumpIfTrueReference or
            DreamProcOpcode.Jump;
    }

    private static bool IsUnconditionalJump(IAnnotatedBytecode bytecode) {
        return bytecode is AnnotatedBytecodeInstruction instruction && instruction.Opcode is DreamProcOpcode.Jump;
    }
}
