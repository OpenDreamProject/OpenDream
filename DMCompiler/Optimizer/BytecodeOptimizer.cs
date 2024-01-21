using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DMCompiler.Bytecode;
using OpenDreamShared.Json;

namespace DMCompiler.DM.Optimizer;

public class BytecodeOptimizer {
    private static int id;

    public static Dictionary<(DreamProcOpcode, DreamProcOpcode), int> opcodeTestPairsForPeephole2Len = new();

    public static Dictionary<(DreamProcOpcode, DreamProcOpcode, DreamProcOpcode), int> opcodeTestPairsForPeephole3Len = new();

    public static Dictionary<(DreamProcOpcode, DreamProcOpcode, DreamProcOpcode, DreamProcOpcode), int> opcodeTestPairsForPeephole4Len = new();

    public static Dictionary<(DreamProcOpcode, DreamProcOpcode, DreamProcOpcode, DreamProcOpcode, DreamProcOpcode), int> opcodeTestPairsForPeephole5Len = new();


    public int StackDepth { get; private set; }

    public List<IAnnotatedBytecode> Optimize(List<IAnnotatedBytecode> input, string errorPath, out int stackDepth) {
        if (input.Count == 0) {
            stackDepth = 0;
            return input;

        }
        RemoveUnreferencedLabels(input);
        JoinAndForwardLabels(input);
        RemoveUnreferencedLabels(input);
        PeepholeOptimizer.RunPeephole(input);
        GenerateOpcodeTestPairsForPeephole(input);
        stackDepth = RecalculateStackDepth(input);
        return input;
    }

    private static int RecalculateStackDepth(List<IAnnotatedBytecode> input) {
        int stackDepth = 0;
        int maxStackDepth = 1; // Guard against some higgs-buggsons
        for (int i = 0; i < input.Count; i++) {
            if (input[i] is AnnotatedBytecodeInstruction instruction) {
                stackDepth += instruction.StackSizeDelta;
                if (stackDepth > maxStackDepth) {
                    maxStackDepth = stackDepth;
                }
            }
        }
        return maxStackDepth;
    }


    private static void RemoveUnreferencedLabels(List<IAnnotatedBytecode> input) {
        Dictionary<string, int> labelReferences = new();
        List<IAnnotatedBytecode> output = new();
        int removed = 0;
        for (int i = 0; i < input.Count; i++) {
            if (input[i] is AnnotatedBytecodeLabel label) {
                labelReferences.TryAdd(label.LabelName, 0);
                output.Add(label);
            } else if (input[i] is AnnotatedBytecodeInstruction instruction) {
                output.Add(instruction);
                if (TryGetLabelName(instruction, out string? labelName)) {
                    if (labelReferences.ContainsKey(labelName)) {
                        labelReferences[labelName]++;
                    } else {
                        labelReferences.Add(labelName, 1);
                    }
                }
            } else {
                output.Add(input[i]);
            }
        }
        for (int i = 0; i < output.Count; i++) {
            if (output[i] is AnnotatedBytecodeLabel label) {
                if (labelReferences[label.LabelName] == 0) {
                    output.RemoveAt(i);
                    i--;
                    removed++;
                }
            }
        }
        input.Clear();
        input.AddRange(output);
    }

    private static void JoinAndForwardLabels(List<IAnnotatedBytecode> input) {
        Dictionary<string, string> labelAliases = new();
        List<IAnnotatedBytecode> output = new();
        for (int i = 0; i < input.Count; i++) {
            if (input[i] is AnnotatedBytecodeLabel label)
            {
                string finalLabelName = label.LabelName;
                List<string> previousLabelNames = new();
                while (i < input.Count - 1 && input[i + 1] is AnnotatedBytecodeLabel nextLabel) {
                    previousLabelNames.Add(nextLabel.LabelName);
                    i++;
                }
                foreach (string previousLabelName in previousLabelNames) {
                    labelAliases.Add(previousLabelName, finalLabelName);
                }
            }
        }
        for (int i = 0; i < input.Count; i++) {
            if (input[i] is AnnotatedBytecodeLabel label) {
                output.Add(label);
            } else if (input[i] is AnnotatedBytecodeInstruction instruction) {
                    if (TryGetLabelName(instruction, out string? labelName)) {
                        if (labelAliases.ContainsKey(labelName)) {
                            List<IAnnotatedBytecode> args = instruction.GetArgs();
                            for (int j = 0; j < args.Count; j++) {
                                if (args[j] is AnnotatedBytecodeLabel argLabel) {
                                    args[j] = new AnnotatedBytecodeLabel(labelAliases[argLabel.LabelName], argLabel.Location);
                                }
                            }
                            output.Add(new AnnotatedBytecodeInstruction(instruction, args));
                        } else {
                            output.Add(instruction);
                        }
                    } else {
                        output.Add(instruction);
                    }
                } else {
                    output.Add(input[i]);
                }
            }
            input.Clear();
            input.AddRange(output);
        }


        private static bool TryGetLabelName(AnnotatedBytecodeInstruction instruction, out string? labelName) {
            object? result = instruction.GetArgs().Where(arg => arg is AnnotatedBytecodeLabel).FirstOrDefault();
            if (result == null) {
                labelName = null;
                return false;
            }
            labelName = ((AnnotatedBytecodeLabel)result).LabelName;
            return true;
        }

        private static void GenerateOpcodeTestPairsForPeephole(List<IAnnotatedBytecode> input) {
            for (int i = 0; i < input.Count - 1;) {
                if (i < input.Count - 3 && input[i] is AnnotatedBytecodeInstruction instruction1 && input[i + 1] is AnnotatedBytecodeInstruction instruction2 && input[i + 2] is AnnotatedBytecodeInstruction instruction3) {
                    opcodeTestPairsForPeephole3Len.TryAdd((instruction1.Opcode, instruction2.Opcode, instruction3.Opcode), 0);
                    opcodeTestPairsForPeephole3Len[(instruction1.Opcode, instruction2.Opcode, instruction3.Opcode)]++;
                }
                i += 1;
            }

            for (int i = 0; i < input.Count - 1;) {
                if (i < input.Count - 2 && input[i] is AnnotatedBytecodeInstruction instruction1 && input[i + 1] is AnnotatedBytecodeInstruction instruction2) {
                    opcodeTestPairsForPeephole2Len.TryAdd((instruction1.Opcode, instruction2.Opcode), 0);
                    opcodeTestPairsForPeephole2Len[(instruction1.Opcode, instruction2.Opcode)]++;
                }
                i += 1;
            }

            for (int i = 0; i < input.Count - 1;) {
                if (i < input.Count - 4 && input[i] is AnnotatedBytecodeInstruction instruction1 && input[i + 1] is AnnotatedBytecodeInstruction instruction2 && input[i + 2] is AnnotatedBytecodeInstruction instruction3 && input[i + 3] is AnnotatedBytecodeInstruction instruction4) {
                    opcodeTestPairsForPeephole4Len.TryAdd((instruction1.Opcode, instruction2.Opcode, instruction3.Opcode, instruction4.Opcode), 0);
                    opcodeTestPairsForPeephole4Len[(instruction1.Opcode, instruction2.Opcode, instruction3.Opcode, instruction4.Opcode)]++;
                }
                i += 1;
            }

            for (int i = 0; i < input.Count - 1;) {
                if (i < input.Count - 5 && input[i] is AnnotatedBytecodeInstruction instruction1 && input[i + 1] is AnnotatedBytecodeInstruction instruction2 && input[i + 2] is AnnotatedBytecodeInstruction instruction3 && input[i + 3] is AnnotatedBytecodeInstruction instruction4 && input[i + 4] is AnnotatedBytecodeInstruction instruction5) {
                    opcodeTestPairsForPeephole5Len.TryAdd((instruction1.Opcode, instruction2.Opcode, instruction3.Opcode, instruction4.Opcode, instruction5.Opcode), 0);
                    opcodeTestPairsForPeephole5Len[(instruction1.Opcode, instruction2.Opcode, instruction3.Opcode, instruction4.Opcode, instruction5.Opcode)]++;
                }
                i += 1;
            }
        }
    }
