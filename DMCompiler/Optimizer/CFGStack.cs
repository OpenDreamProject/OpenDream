using System;
using System.Collections.Generic;
using System.Text;
using DMCompiler.Bytecode;
using OpenDreamShared.Json;

namespace DMCompiler.DM.Optimizer;

public class CFGBasicBlock {
    public static List<CFGBasicBlock> Blocks = new();
    private static int nextId;
    public List<IAnnotatedBytecode> Bytecode = new();
    public int id = -1;
    public List<CFGBasicBlock> Predecessors = new();
    public List<CFGBasicBlock> Successors = new();

    public CFGBasicBlock() {
        id = nextId;
        nextId++;
    }
}

public class CFGStackCodeConverter {
    private readonly Dictionary<string, string> labelAliases = new();

    private readonly Dictionary<string, CFGBasicBlock> labels = new();

    private readonly Stack<CFGBasicBlock> tryBlocks = new();

    private CFGBasicBlock currentBlock;
    private CFGBasicBlock lastBlock = null;
    private string lastLabelName = "";
    private bool lastWasLabel;

    public static List<CFGBasicBlock> Convert(List<IAnnotatedBytecode> input, string errorPath) {
        var cfgStackCodeConverter = new CFGStackCodeConverter();
        return cfgStackCodeConverter.ConvertInternal(input, errorPath);
    }

    private List<CFGBasicBlock> ConvertInternal(List<IAnnotatedBytecode> input, string fileName) {
        var basicBlocks = SplitBasicBlocks(input);

        // Remove empty blocks and update label table
        var changed = true;
        while (changed) {
            changed = false;
            for (var i = 0; i < basicBlocks.Count; i++)
                if (basicBlocks[i].Bytecode.Count == 0) {
                    changed = true;
                    // First search the label table for labels that point to this block
                    foreach (var label in labels)
                        if (label.Value == basicBlocks[i])
                            labels[label.Key] = basicBlocks[i + 1];

                    basicBlocks.RemoveAt(i);
                    i--;
                }
        }

        // Naive connect blocks in order
        for (var i = 0; i < basicBlocks.Count - 1; i++) {
            basicBlocks[i].Successors.Add(basicBlocks[i + 1]);
            basicBlocks[i + 1].Predecessors.Add(basicBlocks[i]);
        }

        // Resolve jumps
        ResolveJumps(basicBlocks);

        // Merge blocks that only have one direct predecessor and direct successor
        changed = true;
        var removed = 0;
        var beginId = basicBlocks[0].id;
        while (changed) {
            changed = false;
            for (var i = 0; i < basicBlocks.Count;)
                if (basicBlocks[i].Predecessors.Count == 1 && basicBlocks[i].Successors.Count == 1) {
                    var predecessor = basicBlocks[i].Predecessors[0];
                    var successor = basicBlocks[i].Successors[0];
                    if (successor != basicBlocks[i + 1] || predecessor.Successors.Count > 1 ||
                        successor.Predecessors.Count > 1) {
                        i++;
                        continue;
                    }

                    if (i != 0 && predecessor != basicBlocks[i - 1]) {
                        i++;
                        continue;
                    }

                    changed = true;
                    removed++;
                    predecessor.Successors.Remove(basicBlocks[i]);
                    predecessor.Successors.Add(successor);
                    successor.Predecessors.Remove(basicBlocks[i]);
                    successor.Predecessors.Add(predecessor);
                    predecessor.Bytecode.AddRange(basicBlocks[i].Bytecode);
                    basicBlocks.RemoveAt(i);
                    i--;
                    if (i < 0) i = 0;
                } else if (basicBlocks[i].Predecessors.Count == 0) // No predecessors? Root for proc, check if mergeable
                {
                    if (basicBlocks[i].Successors.Count == 1 && basicBlocks[i].Successors[0].Predecessors.Count == 1) {
                        var id = basicBlocks[i].id;
                        var successor = basicBlocks[i].Successors[0];
                        changed = true;
                        removed++;
                        successor.Predecessors.Remove(basicBlocks[i]);
                        successor.Bytecode.InsertRange(0, basicBlocks[i].Bytecode);
                        basicBlocks.RemoveAt(i);
                        i--;
                        if (i < 0) i = 0;
                    } else {
                        i++;
                    }
                } else {
                    i++;
                }
        }

        // Renumber blocks
        for (var i = 0; i < basicBlocks.Count; i++) basicBlocks[i].id = beginId + i;

        // Remove all labels not at the start of a block and all labels at the start of a block if the block only has one contiguous predecessor
        foreach (var block in basicBlocks)
            for (var i = 0; i < block.Bytecode.Count; i++)
                if (block.Bytecode[i] is AnnotatedBytecodeLabel label) {
                    if (i != 0) {
                        block.Bytecode.RemoveAt(i);
                        i--;
                    } else if (block.Predecessors.Count == 1 && block.Predecessors[0].id == block.id - 1) {
                        block.Bytecode.RemoveAt(i);
                        i--;
                    }
                }

        // Should do a pass to calculate dominators and expand blocks
        return basicBlocks;
    }

    private void ResolveJumps(List<CFGBasicBlock> basicBlocks) {
        foreach (var block in basicBlocks)
            for (var index = 0; index < block.Bytecode.Count; index++) {
                var bytecode = block.Bytecode[index];
                // Only need to handle jumps, labels are already handled
                if (bytecode is AnnotatedBytecodeInstruction instruction)
                    switch (instruction.Opcode) {
                        // Conditional jumps w/ label in arg 0, preserve naive fallthrough
                        case DreamProcOpcode.SwitchCase:
                        case DreamProcOpcode.SwitchCaseRange:
                        case DreamProcOpcode.JumpIfFalse:
                        case DreamProcOpcode.JumpIfTrue:
                        case DreamProcOpcode.BooleanAnd:
                        case DreamProcOpcode.BooleanOr:
                        case DreamProcOpcode.JumpIfNull:
                        case DreamProcOpcode.JumpIfNullNoPop:
                        case DreamProcOpcode.EnumerateNoAssign:
                        case DreamProcOpcode.Spawn: {
                            if (index != block.Bytecode.Count - 1)
                                throw new Exception("Conditional jump is not the last instruction in the block");
                            var alternate = (instruction.GetArgs()[0] as AnnotatedBytecodeLabel)!.LabelName;
                            if (!labels.ContainsKey(alternate)) {
                                if (labelAliases.ContainsKey(alternate))
                                    alternate = labelAliases[alternate];
                                else
                                    throw new Exception("Label " + alternate + " does not exist");
                            }

                            block.Successors.Add(labels[alternate]);
                            labels[alternate].Predecessors.Add(block);
                            break;
                        }
                        // Conditional jumps w/ label in arg 1, preserve naive fallthrough
                        case DreamProcOpcode.Enumerate:
                        case DreamProcOpcode.JumpIfFalseReference:
                        case DreamProcOpcode.JumpIfTrueReference: {
                            if (index != block.Bytecode.Count - 1)
                                throw new Exception("Conditional jump is not the last instruction in the block");
                            var defaultLabel = (instruction.GetArgs()[1] as AnnotatedBytecodeLabel)!.LabelName;
                            if (!labels.ContainsKey(defaultLabel)) {
                                if (labelAliases.ContainsKey(defaultLabel))
                                    defaultLabel = labelAliases[defaultLabel];
                                else
                                    throw new Exception("Label " + defaultLabel + " does not exist");
                            }

                            block.Successors.Add(labels[defaultLabel]);
                            labels[defaultLabel].Predecessors.Add(block);
                            break;
                        }
                        // Unconditional jumps w/ label in arg 0, do not preserve naive fallthrough
                        case DreamProcOpcode.Jump: {
                            var defaultLabel = (instruction.GetArgs()[0] as AnnotatedBytecodeLabel)!.LabelName;
                            if (!labels.ContainsKey(defaultLabel)) {
                                if (labelAliases.ContainsKey(defaultLabel))
                                    defaultLabel = labelAliases[defaultLabel];
                                else
                                    throw new Exception("Label " + defaultLabel + " does not exist");
                            }

                            block.Successors[0].Predecessors.Remove(block);
                            block.Successors.Clear();
                            block.Successors.Add(labels[defaultLabel]);
                            labels[defaultLabel].Predecessors.Add(block);
                            break;
                        }
                        // Return, do not preserve naive fallthrough
                        case DreamProcOpcode.Return: {
                            // If return is in the last block, we don't need to do anything
                            if (index == block.Bytecode.Count - 1) break;
                            block.Successors[0].Predecessors.Remove(block);
                            block.Successors.Clear();
                            break;
                        }

                        // Throw creates either a direct successor of the catch segment or acts as a leaf
                        case DreamProcOpcode.Throw: {
                            // If throw is in the last block, we don't need to do anything
                            if (index == block.Bytecode.Count - 1) break;

                            block.Successors[0].Predecessors.Remove(block);
                            block.Successors.Clear();
                            if (tryBlocks.Count == 0) break;

                            var catchBlock = tryBlocks.Peek();
                            block.Successors.Add(catchBlock);
                            catchBlock.Predecessors.Add(block);
                            break;
                        }

                        // Try does not change the control flow, but does add its referenced label to the try stack
                        case DreamProcOpcode.TryNoValue:
                        case DreamProcOpcode.Try: {
                            var catchLabel = (instruction.GetArgs()[0] as AnnotatedBytecodeLabel)!.LabelName;
                            if (!labels.ContainsKey(catchLabel)) {
                                if (labelAliases.ContainsKey(catchLabel))
                                    catchLabel = labelAliases[catchLabel];
                                else
                                    throw new Exception("Label " + catchLabel + " does not exist");
                            }

                            tryBlocks.Push(labels[catchLabel]);
                            break;
                        }
                        // EndTry does not change the control flow, but does pop the try stack
                        case DreamProcOpcode.EndTry: {
                            tryBlocks.Pop();
                            break;
                        }

                        // Default, verify we haven't missed a conditional jump
                        default: {
                            var opcodeMetadata = OpcodeMetadataCache.GetMetadata(instruction.Opcode);
                            if (opcodeMetadata.SplitsBasicBlock)
                                throw new Exception("Control flow splitting opcode " + instruction.Opcode +
                                                    " is not handled");
                            break;
                        }
                    }
            }
    }

    private List<CFGBasicBlock> SplitBasicBlocks(List<IAnnotatedBytecode> input) {
        var root = new CFGBasicBlock();
        currentBlock = root;
        // Generate naive basic blocks without connecting them, spliting on all labels and conditional jumps
        var basicBlocks = new List<CFGBasicBlock>();
        basicBlocks.Add(root);
        foreach (var bytecode in input)
            if (bytecode is AnnotatedBytecodeInstruction instruction) {
                lastWasLabel = false;
                currentBlock.Bytecode.Add(instruction);
                var opcodeMetadata = OpcodeMetadataCache.GetMetadata(instruction.Opcode);
                if (opcodeMetadata.SplitsBasicBlock) {
                    currentBlock = new CFGBasicBlock();
                    basicBlocks.Add(currentBlock);
                }
            } else if (bytecode is AnnotatedBytecodeLabel label) {
                if (labels.ContainsKey(label.LabelName)) throw new Exception("Duplicate label " + label.LabelName);

                if (lastWasLabel) {
                    // Do not split on repeated labels
                    currentBlock.Bytecode.Add(label);
                    labelAliases.Add(label.LabelName, lastLabelName);
                    continue;
                }

                currentBlock = new CFGBasicBlock();
                basicBlocks.Add(currentBlock);
                currentBlock.Bytecode.Add(label);
                labels.Add(label.LabelName, currentBlock);
                lastWasLabel = true;
                lastLabelName = label.LabelName;
            } else if (bytecode is AnnotatedBytecodeVariable localVariable) {
                lastWasLabel = false;
                currentBlock.Bytecode.Add(localVariable);
            } else {
                throw new Exception("Invalid bytecode type " + bytecode.GetType());
            }

        return basicBlocks;
    }

    public static string DumpCFGTable(List<CFGBasicBlock> blocks) {
        var sb = new StringBuilder();

        foreach (var block in blocks) {
            sb.Append("Block " + block.id + ":\n");
            AnnotatedBytecodePrinter.Print(block.Bytecode, new List<SourceInfoJson>(), sb, 2);
            sb.Append("Successors: ");
            if (block.Successors.Count == 0) sb.Append("None");
            foreach (var successor in block.Successors) sb.Append(successor.id + " ");
            sb.Append("\n");
            sb.Append("Predecessors: ");
            if (block.Predecessors.Count == 0) sb.Append("None");
            foreach (var predecessor in block.Predecessors) sb.Append(predecessor.id + " ");
            sb.Append("\n\n");
        }

        return sb.ToString();
    }
}
