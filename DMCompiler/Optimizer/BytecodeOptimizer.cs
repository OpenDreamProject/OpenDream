using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DMCompiler.Bytecode;

namespace DMCompiler.DM.Optimizer;

public class BytecodeOptimizer {
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
            if (input[i] is AnnotatedBytecodeLabel label) {
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
                                args[j] = new AnnotatedBytecodeLabel(labelAliases[argLabel.LabelName],
                                    argLabel.Location);
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
}
