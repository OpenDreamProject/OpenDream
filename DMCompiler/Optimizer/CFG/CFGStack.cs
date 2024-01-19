using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DMCompiler.Bytecode;
using DMCompiler.DM.Optimizer;
using OpenDreamShared.Compiler;
using OpenDreamShared.Json;

namespace DMCompiler.Optimizer.CFG;

public class CFGBasicBlock {
    public Location Location;
    private static int nextId;
    public List<IAnnotatedBytecode> Bytecode;
    public string LabelName = "";
    public int id = -1;
    public List<CFGBasicBlock> Predecessors = new();
    public List<CFGBasicBlock> Successors = new();

    public CFGBasicBlock? DirectSuccessor;
    public CFGBasicBlock? AlternateSuccessor;

    public enum FlowExitType {
        Fallthrough,
        Return,
        Throw,
        Jump,
        ConditionalJump,
    }

    public FlowExitType ExitType = FlowExitType.Fallthrough;

    public string DirectLabelName = "";
    public string AlternateLabelName = "";


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
    private readonly Dictionary<string, int> labelReferences = new();

    public static List<CFGBasicBlock> Convert(List<IAnnotatedBytecode> input, string errorPath) {
        var cfgStackCodeConverter = new CFGStackCodeConverter();
        return cfgStackCodeConverter.GenerateCFG(input, errorPath);
    }


    private List<CFGBasicBlock> GenerateCFG(List<IAnnotatedBytecode> input, string fileName) {
        // Generate the basic blocks
        var basicBlocks = SplitBasicBlocks(null, 16, input, null);
        int changed = 1;
        int labelChanged = 1;
        int totalChanged = -1;
        bool first = true;
        bool ucePass = false;
        int pass = 0;
        DumpCFGToDebugDir(basicBlocks, "preopt");
        // Run the CFG optimizer until it stops changing
        while (true) {
            totalChanged += changed;
            changed = 0;
            labelChanged = 0;
            // Build the basic CFG
            RemoveEmptyBlocksAndUpdateLabels(basicBlocks, ref changed);

            ConnectBlocksInOrder(basicBlocks);

            // Resolve jumps and merge blocks
            ResolveJumps(basicBlocks, ref labelChanged);

            RenumberBlocks(basicBlocks);

            JumpForwarding(basicBlocks, ref changed);

            NormalizeTree(basicBlocks, ref changed);

            RenumberBlocks(basicBlocks);

            bool requireRebuildFromUnrefLabels = false;
            bool requireRebuild = false;
            RemoveUnreferencedLabels(basicBlocks, out requireRebuildFromUnrefLabels, ref labelChanged);

            labels.Clear();
            labelReferences.Clear();
            ClearConnectedBlocks(basicBlocks);
            RebuildLabelTable(basicBlocks, out requireRebuild);
            if (requireRebuild || requireRebuildFromUnrefLabels) {
                basicBlocks = SplitBasicBlocks(basicBlocks.Count, basicBlocks.Max(x => x.Bytecode.Count),
                    basicBlocks.SelectMany(x => x.Bytecode).ToList(), basicBlocks);
            }

            if (changed == 0 && labelChanged == 0 && !requireRebuild && !requireRebuildFromUnrefLabels) {
                ResolveJumps(basicBlocks, ref labelChanged);

                RenumberBlocks(basicBlocks);

                RemoveUnreferencedLabels(basicBlocks, out requireRebuildFromUnrefLabels, ref labelChanged);


                break;
            }
            pass++;

        }
        if (totalChanged > 10) {
            DumpCFGToDebugDir(basicBlocks, "prehoist");
        } else {
            RemoveDumpCFGToDebugDir("preopt");
        }
        HoistLabels(basicBlocks);
        if (totalChanged > 10) {
            DumpCFGToDebugDir(basicBlocks, "postopt");
        } else {
            RemoveDumpCFGToDebugDir("preopt");
            RemoveDumpCFGToDebugDir("prehoist");
        }

        return basicBlocks;
    }


    private void NormalizeTree(List<CFGBasicBlock> basicBlocks, ref int changed) {
        // Remove blocks with no predecessors
        for (var i = 1; i < basicBlocks.Count; i++) {
            if (basicBlocks[i].Predecessors.Count == 0) {
                changed++;
                basicBlocks.RemoveAt(i);
                i--;
            }
        }
        // If the last block in the tree is a label with no bytecode, add a return
        if (basicBlocks.Count > 0) {
            var lastBlock = basicBlocks[basicBlocks.Count - 1];
            int j = 0;
            if (lastBlock.Successors.Count != 0) return;
            while (j < lastBlock.Bytecode.Count && lastBlock.Bytecode[j] is AnnotatedBytecodeLabel) j++;
            if (j >= lastBlock.Bytecode.Count) {
                lastBlock.Bytecode.Add(new AnnotatedBytecodeInstruction(DreamProcOpcode.Return, 0, lastBlock.Location));
            }
        }
    }

    private void RemoveUnreferencedLabels(List<CFGBasicBlock> basicBlocks, out bool requireRebuild, ref int changed) {
        requireRebuild = false;
        foreach (var block in basicBlocks) {
            for (var index = 0; index < block.Bytecode.Count; index++) {
                var bytecode = block.Bytecode[index];
                if (bytecode is AnnotatedBytecodeLabel label) {
                    if (labelReferences[label.LabelName] == 0) {
                        requireRebuild = true;
                        block.Bytecode.RemoveAt(index);
                        changed++;
                        index--;
                    }
                }
            }
        }
    }

    static int UID = 0;

    private void RebuildLabelTable(List<CFGBasicBlock> basicBlocks, out bool requireRebuild) {
        requireRebuild = false;
        foreach (var block in basicBlocks) {
            for (var index = 0; index < block.Bytecode.Count; index++) {
                var bytecode = block.Bytecode[index];
                if (bytecode is AnnotatedBytecodeLabel label) {
                    if (index != 0) {
                        requireRebuild = true;
                    }

                    if (labels.ContainsKey(label.LabelName)) throw new Exception("Duplicate label " + label.LabelName);
                    labels.Add(label.LabelName, block);
                    labelReferences.Add(label.LabelName, 0);
                }
            }
        }
    }

    private void ClearConnectedBlocks(List<CFGBasicBlock> basicBlocks) {
        foreach (var block in basicBlocks) {
            block.Successors.Clear();
            block.Predecessors.Clear();
        }
    }


    private List<CFGBasicBlock> SplitBasicBlocks(int? size, int? largestbytecode, List<IAnnotatedBytecode> input, List<CFGBasicBlock>? old) {
        labels.Clear();
        labelReferences.Clear();
        var root = old?[0] ?? new CFGBasicBlock();
        int firstId = 0;
        if (root.Bytecode == null) {
            root.Bytecode = new List<IAnnotatedBytecode>(largestbytecode ?? input.Count * 1/4);
        }
        root.Bytecode.Clear();
        root.Successors.Clear();
        root.Predecessors.Clear();
        currentBlock = root;
        currentBlock.Bytecode.EnsureCapacity(largestbytecode ?? input.Count * 1/4);
        // Generate naive basic blocks without connecting them, spliting on all labels and conditional jumps
        var basicBlocks = new List<CFGBasicBlock>(size ?? input.Count * 1/4);
        basicBlocks.Add(root);
        foreach (var bytecode in input)
            if (bytecode is AnnotatedBytecodeInstruction instruction) {
                lastWasLabel = false;
                currentBlock.Bytecode.Add(instruction);
                var opcodeMetadata = OpcodeMetadataCache.GetMetadata(instruction.Opcode);
                if (opcodeMetadata.SplitsBasicBlock) {
                    if (firstId + basicBlocks.Count < old?.Count) {
                        currentBlock = old[firstId + basicBlocks.Count];
                    } else {
                        currentBlock = new CFGBasicBlock();
                    }
                    if (currentBlock.Bytecode == null) {
                        currentBlock.Bytecode = new List<IAnnotatedBytecode>(largestbytecode ?? input.Count * 1/4);
                    }
                    currentBlock.Bytecode.Clear();
                    currentBlock.Successors.Clear();
                    currentBlock.Predecessors.Clear();
                    currentBlock.Bytecode.EnsureCapacity(largestbytecode ?? input.Count * 1/4);
                    basicBlocks.Add(currentBlock);
                }
            } else if (bytecode is AnnotatedBytecodeLabel label) {
                if (labels.ContainsKey(label.LabelName)) throw new Exception("Duplicate label " + label.LabelName);

                if (lastWasLabel) {
                    // Do not split on repeated labels
                    labelAliases.TryAdd(label.LabelName, lastLabelName);
                    continue;
                }

                if (firstId + basicBlocks.Count < old?.Count) {
                    currentBlock = old[firstId + basicBlocks.Count];
                } else {
                    currentBlock = new CFGBasicBlock();
                }
                if (currentBlock.Bytecode == null) {
                    currentBlock.Bytecode = new List<IAnnotatedBytecode>(largestbytecode ?? input.Count * 1/4);
                }
                currentBlock.Bytecode.Clear();
                currentBlock.Successors.Clear();
                currentBlock.Predecessors.Clear();
                currentBlock.Bytecode.EnsureCapacity(largestbytecode ?? input.Count * 1/4);
                basicBlocks.Add(currentBlock);
                currentBlock.Bytecode.Add(label);
                labels.Add(label.LabelName, currentBlock);
                labelReferences.Add(label.LabelName, 0);
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

    private void RemoveEmptyBlocksAndUpdateLabels(List<CFGBasicBlock> basicBlocks, ref int changed) {
        int oldChanged = 0;
        do {
            oldChanged = changed;
            for (var i = 0; i < basicBlocks.Count; i++)
                if (basicBlocks[i].Bytecode.Count == 0) {
                    changed++;
                    UpdateLabelTable(basicBlocks, i);
                    basicBlocks.RemoveAt(i);
                    i--;
                }
        }
        while (oldChanged != changed);
    }

    private void UpdateLabelTable(List<CFGBasicBlock> basicBlocks, int index) {
        foreach (var label in labels)
            if (label.Value == basicBlocks[index] && index != basicBlocks.Count - 1)
                labels[label.Key] = basicBlocks[index + 1];
    }

    private void ConnectBlocksInOrder(List<CFGBasicBlock> basicBlocks) {
        for (var i = 0; i < basicBlocks.Count - 1; i++) {
            basicBlocks[i].Successors.Add(basicBlocks[i + 1]);
            basicBlocks[i + 1].Predecessors.Add(basicBlocks[i]);
        }
    }

    private void HoistLabels(List<CFGBasicBlock> basicBlocks) {
        Dictionary<string, int> finalLabels = new();
        int id = 0;
        // Hoist labels from the list into a field
        foreach (var block in basicBlocks) {
            block.id = id++;
            if (block.Bytecode.Count == 0) throw new Exception("Empty block");
            if ((block.Bytecode[0] is AnnotatedBytecodeLabel)) {
                var name = (block.Bytecode[0] as AnnotatedBytecodeLabel)!.LabelName;
                block.Location = (block.Bytecode[0] as AnnotatedBytecodeLabel)!.Location;
                block.Bytecode.RemoveAt(0);
                block.LabelName = name;
                finalLabels.Add(name, block.id);
            }

            bool foundExit = false;
            for (var index = 0; index < block.Bytecode.Count; index++) {
                var bytecode = block.Bytecode[index];
                if (bytecode is AnnotatedBytecodeLabel label) {
                    throw new Exception("Label in non-first position");
                }
            }
        }

        // Disambiguate jumps
        for (var blockIndex = 0; blockIndex < basicBlocks.Count; blockIndex++) {
            var block = basicBlocks[blockIndex];
            bool foundExit = false;
            for (var index = 0; index < block.Bytecode.Count; index++) {
                var bytecode = block.Bytecode[index];
                if (bytecode is AnnotatedBytecodeInstruction instruction) {
                    if (IsJump(instruction) && !IsUnconditionalJump(instruction)) {
                        if (foundExit) throw new Exception("Multiple exits in block");
                        foundExit = true;
                        if (!TryGetLabelFromJump(instruction, out var label, out var loc))
                            throw new Exception("Jump without label");
                        if (finalLabels.ContainsKey(label)) {
                            var target = finalLabels[label];
                            block.ExitType = CFGBasicBlock.FlowExitType.ConditionalJump;
                            block.DirectSuccessor = basicBlocks[blockIndex + 1];
                            block.AlternateLabelName = label;
                            block.AlternateSuccessor = basicBlocks[target];
                        } else {
                            throw new Exception("Jump to non-existent label");
                        }
                    } else if (IsUnconditionalJump(instruction)) {
                        if (foundExit) throw new Exception("Multiple exits in block");
                        foundExit = true;
                        if (!TryGetLabelFromJump(instruction, out var label, out var loc))
                            throw new Exception("Jump without label");
                        if (finalLabels.ContainsKey(label)) {
                            var target = finalLabels[label];
                            block.ExitType = CFGBasicBlock.FlowExitType.Jump;
                            block.DirectSuccessor = basicBlocks[target];
                            block.DirectLabelName = label;
                        }
                        else {
                            throw new Exception("Jump to non-existent label");
                        }
                    } else if (instruction.Opcode == DreamProcOpcode.Return) {
                        if (foundExit) throw new Exception("Multiple exits in block");
                        foundExit = true;
                        block.ExitType = CFGBasicBlock.FlowExitType.Return;
                    } else if (instruction.Opcode == DreamProcOpcode.Throw) {
                        if (foundExit) throw new Exception("Multiple exits in block");
                        foundExit = true;
                        block.ExitType = CFGBasicBlock.FlowExitType.Throw;
                    }
                }
            }
            if (!foundExit) {
                block.ExitType = CFGBasicBlock.FlowExitType.Fallthrough;
                if (blockIndex == basicBlocks.Count - 1) {
                    block.ExitType = CFGBasicBlock.FlowExitType.Return;
                    break;
                }
                block.DirectSuccessor = basicBlocks[blockIndex + 1];
            }

            if (block.ExitType == CFGBasicBlock.FlowExitType.Fallthrough) {
                if (block.DirectSuccessor == null) {
                    throw new Exception("Block has more than 1 successor");
                }
            }
        }

        return;
    }

    public static List<IAnnotatedBytecode> ConvertBack(List<CFGBasicBlock> blocks) {
        var output = new List<IAnnotatedBytecode>();
        foreach (var block in blocks) {
            if (block.LabelName != "") output.Add(new AnnotatedBytecodeLabel(block.LabelName, block.Location));
            output.AddRange(block.Bytecode);
        }

        return output;
    }

    private void ResolveJumps(List<CFGBasicBlock> basicBlocks, ref int changed) {
        foreach (var block in basicBlocks)
            for (var index = 0; index < block.Bytecode.Count; index++) {
                var bytecode = block.Bytecode[index];
                if (bytecode is AnnotatedBytecodeInstruction instruction)
                    switch (instruction.Opcode) {
                        // Handle conditional jumps
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
                            HandleConditionalJump(block, index, instruction, ref changed);
                            break;

                        // Handle conditional jumps with label in arg 1
                        case DreamProcOpcode.Enumerate:
                        case DreamProcOpcode.JumpIfFalseReference:
                        case DreamProcOpcode.JumpIfTrueReference:
                            HandleConditionalJumpWithLabelArg1(block, index, instruction, ref changed);
                            break;

                        // Handle unconditional jumps
                        case DreamProcOpcode.Jump:
                            HandleUnconditionalJump(block, index, instruction, ref changed);
                            break;

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

                        case DreamProcOpcode.Call:
                        case DreamProcOpcode.DereferenceCall:
                        case DreamProcOpcode.CallStatement: {
                            // Since we're not doing interprocedural analysis, we can't know if a call throws, so we have to assume it does and
                            // treat it like a throw, with the caveat that it can also continue execution
                            if (index == block.Bytecode.Count - 1) break;

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
                            labelReferences[catchLabel]++;
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

    private bool TryGetLabelFromJump(AnnotatedBytecodeInstruction instruction, out string label, out int loc) {
        label = "";
        loc = -1; // Force exception in calling function if they try to index into args
        if (instruction.GetArgs().Count == 0) return false;

        switch (instruction.Opcode) {
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
                label = GetLabelName(instruction.GetArgs()[0]);
                loc = 0;
                return true;
            case DreamProcOpcode.Enumerate:
            case DreamProcOpcode.JumpIfFalseReference:
            case DreamProcOpcode.JumpIfTrueReference:
                label = GetLabelName(instruction.GetArgs()[1]);
                loc = 1;
                return true;
            case DreamProcOpcode.Jump:
                label = GetLabelName(instruction.GetArgs()[0]);
                loc = 0;
                return true;
            default:
                return false;
        }
    }

    private bool IsJump(AnnotatedBytecodeInstruction instruction) {
        switch (instruction.Opcode) {
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
            case DreamProcOpcode.Enumerate:
            case DreamProcOpcode.JumpIfFalseReference:
            case DreamProcOpcode.JumpIfTrueReference:
            case DreamProcOpcode.Jump:
                return true;
            default:
                return false;
        }
    }

    private bool IsUnconditionalJump(AnnotatedBytecodeInstruction instruction) {
        switch (instruction.Opcode) {
            case DreamProcOpcode.Jump:
                return true;
            default:
                return false;
        }
    }


    private void HandleConditionalJump(CFGBasicBlock block, int index, AnnotatedBytecodeInstruction instruction, ref int changed) {
        if (index != block.Bytecode.Count - 1)
            throw new Exception("Conditional jump is not the last instruction in the block");

        var alternate = GetLabelName(instruction.GetArgs()[0]);
        var successor = GetLabelPointedBlock(alternate);
        block.Successors.Add(successor);
        successor.Predecessors.Add(block);

        var claimedLabel = (instruction.GetArgs()[0] as AnnotatedBytecodeLabel)!.LabelName;
        if (claimedLabel != alternate) {
            // Replace the jump label to resolve aliases
            var args = instruction.GetArgs();
            args[0] = new AnnotatedBytecodeLabel(alternate, instruction.Location);
            changed++;
            block.Bytecode[index] = new AnnotatedBytecodeInstruction(instruction, args);
        }
    }

    private void HandleConditionalJumpWithLabelArg1(CFGBasicBlock block, int index,
        AnnotatedBytecodeInstruction instruction, ref int changed) {
        if (index != block.Bytecode.Count - 1)
            throw new Exception("Conditional jump is not the last instruction in the block");

        var defaultLabel = GetLabelName(instruction.GetArgs()[1]);
        var successor = GetLabelPointedBlock(defaultLabel);
        block.Successors.Add(successor);
        successor.Predecessors.Add(block);

        var claimedLabel = (instruction.GetArgs()[1] as AnnotatedBytecodeLabel)!.LabelName;
        if (claimedLabel != defaultLabel) {
            // Replace the jump label to resolve aliases
            var args = instruction.GetArgs();
            args[1] = new AnnotatedBytecodeLabel(defaultLabel, instruction.Location);
            changed++;
            block.Bytecode[index] = new AnnotatedBytecodeInstruction(instruction, args);
        }
    }

    private void HandleUnconditionalJump(CFGBasicBlock block, int index, AnnotatedBytecodeInstruction instruction, ref int changed) {
        var defaultLabel = GetLabelName(instruction.GetArgs()[0]);
        var successor = GetLabelPointedBlock(defaultLabel);
        if (block.Successors.Count > 0) {
            var directSuccessor = block.Successors[0];
            directSuccessor.Predecessors.Remove(block);
        }
        block.Successors.Clear();
        block.Successors.Add(successor);
        successor.Predecessors.Add(block);

        var claimedLabel = (instruction.GetArgs()[0] as AnnotatedBytecodeLabel)!.LabelName;
        if (claimedLabel != defaultLabel) {
            // Replace the jump label to resolve aliases
            var args = instruction.GetArgs();
            args[0] = new AnnotatedBytecodeLabel(defaultLabel, instruction.Location);
            changed++;
            block.Bytecode[index] = new AnnotatedBytecodeInstruction(instruction, args);
        }
    }

    private string GetLabelName(object arg) {
        var label = (arg as AnnotatedBytecodeLabel)?.LabelName;
        if (labelAliases.ContainsKey(label)) {
            label = labelAliases[label];
            labelReferences[label]++;
            return label;
        }
        else if (labels.ContainsKey(label)) {
            labelReferences[label]++;
            return label;
        }
        else throw new Exception("Label " + label + " does not exist");
    }

    private CFGBasicBlock GetLabelPointedBlock(string label) {
        if (labelAliases.ContainsKey(label)) label = labelAliases[label];

        if (!labels.ContainsKey(label)) throw new Exception("Label " + label + " does not exist");
        labelReferences[label]++;
        return labels[label];
    }

    private void RenumberBlocks(List<CFGBasicBlock> basicBlocks) {
        var beginId = basicBlocks[0].id;
        for (var i = 0; i < basicBlocks.Count; i++) basicBlocks[i].id = beginId + i;
    }

    /*
     * When a conditional or unconditional jump points to a block consisting of a single unconditional jump preceded by
     * any number of labels, we can forward the jump to the target of the unconditional jump.
     */
    private void JumpForwarding(List<CFGBasicBlock> basicBlocks, ref int changed) {
        foreach (var block in basicBlocks)
            for (var i = 0; i < block.Bytecode.Count; i++)
                if (block.Bytecode[i] is AnnotatedBytecodeInstruction instruction && IsJump(instruction)) {
                    string label = "";
                    int loc = -1;
                    if (!TryGetLabelFromJump(instruction, out label, out loc)) continue;

                    var target = GetLabelPointedBlock(label);
                    // Make a copy of the bytecode, step through it discarding labels until an instruction is found, then check if it is an unconditional jump
                    var targetBytecode = new List<IAnnotatedBytecode>(target.Bytecode);
                    var j = 0;
                    while (j < targetBytecode.Count && targetBytecode[j] is AnnotatedBytecodeLabel) j++;
                    if (j >= targetBytecode.Count) continue;
                    if (!(targetBytecode[j] is AnnotatedBytecodeInstruction targetInstruction)) continue;
                    if ((!(IsUnconditionalJump(targetInstruction))) && (!(targetInstruction.Opcode == DreamProcOpcode.Return))) continue;
                    var targetLabel = "";
                    if (!TryGetLabelFromJump(targetInstruction, out targetLabel, out _)) continue;

                    // We have found a jump target that is a single unconditional jump preceded by any number of labels
                    // Forward the jump
                    changed++;
                    var args = instruction.GetArgs();
                    args[loc] = new AnnotatedBytecodeLabel(targetLabel, instruction.Location);
                    block.Bytecode[i] = new AnnotatedBytecodeInstruction(instruction, args);
                }
    }

    private static void DumpCFGToFile(List<CFGBasicBlock> blocks, string fileName) {
        var sb = new StringBuilder();
        int firstId = blocks[0].id;
        foreach (var block in blocks) {
            sb.Append("Block " + (block.id - firstId) + ":\n");
            if (block.LabelName != "") sb.Append(block.LabelName + ":\n");
            AnnotatedBytecodePrinter.Print(block.Bytecode, new List<SourceInfoJson>(), sb, 2);
            sb.Append("Successors: ");
            if (block.Successors.Count == 0) sb.Append("None");
            foreach (var successor in block.Successors) sb.Append((successor.id - firstId) + " ");
            sb.Append("\n");
            sb.Append("Predecessors: ");
            if (block.Predecessors.Count == 0) sb.Append("None");
            foreach (var predecessor in block.Predecessors) sb.Append((predecessor.id - firstId) + " ");
            if (block.ExitType == CFGBasicBlock.FlowExitType.Return) {
                sb.Append("\n");
                sb.Append("Return");
            } else if (block.ExitType == CFGBasicBlock.FlowExitType.Throw) {
                sb.Append("\n");
                sb.Append("Throw");
            } else if (block.ExitType == CFGBasicBlock.FlowExitType.Jump) {
                sb.Append("\n");
                sb.Append("Direct Successor: " + (block.DirectSuccessor!.id - firstId) + "\n");
                sb.Append("Direct Label: " + block.DirectLabelName + "\n");
            }
            else if (block.ExitType == CFGBasicBlock.FlowExitType.ConditionalJump) {
                sb.Append("\n");
                sb.Append("Direct Successor: " + (block.DirectSuccessor!.id - firstId) + "\n");
                sb.Append("Direct Label: " + block.DirectLabelName + "\n");
                sb.Append("Alternate Successor: " + (block.AlternateSuccessor!.id - firstId) + "\n");
                sb.Append("Alternate Label: " + block.AlternateLabelName + "\n");
            }
            sb.Append("\n\n");
        }

        File.WriteAllText(fileName, sb.ToString());
    }

    public static void DumpCFGToDebugDir(List<CFGBasicBlock> blocks, string name) {
        Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/cfg");
        name = name.Replace("/", "_");
        DumpCFGToFile(blocks, Directory.GetCurrentDirectory() + "/cfg/" + name);
        var insts = ConvertBack(blocks);
        var sb = new StringBuilder();
        AnnotatedBytecodePrinter.Print(insts, new List<SourceInfoJson>(), sb);
        File.WriteAllText(Directory.GetCurrentDirectory() + "/cfg/" + name + "_insts", sb.ToString());
    }

    private void RemoveDumpCFGToDebugDir(string name) {
        name = name.Replace("/", "_");
        File.Delete(Directory.GetCurrentDirectory() + "/cfg/" + name);
        File.Delete(Directory.GetCurrentDirectory() + "/cfg/" + name + "_insts");
    }

}
