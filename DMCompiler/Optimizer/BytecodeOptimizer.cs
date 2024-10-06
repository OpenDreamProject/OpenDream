using System.Diagnostics.CodeAnalysis;

namespace DMCompiler.Optimizer;

public class BytecodeOptimizer {
    public List<IAnnotatedBytecode> Optimize(List<IAnnotatedBytecode> input) {
        if (input.Count == 0) {
            return input;
        }

        RemoveUnreferencedLabels(input);
        JoinAndForwardLabels(input);
        RemoveUnreferencedLabels(input);
        PeepholeOptimizer.RunPeephole(input);
        return input;
    }

    private static void RemoveUnreferencedLabels(List<IAnnotatedBytecode> input) {
        Dictionary<string, int> labelReferences = new();
        for (int i = 0; i < input.Count; i++) {
            if (input[i] is AnnotatedBytecodeLabel label) {
                labelReferences.TryAdd(label.LabelName, 0);
            } else if (input[i] is AnnotatedBytecodeInstruction instruction) {
                if (TryGetLabelName(instruction, out string? labelName)) {
                    if (!labelReferences.TryAdd(labelName, 1)) {
                        labelReferences[labelName]++;
                    }
                }
            }
        }

        for (int i = 0; i < input.Count; i++) {
            if (input[i] is AnnotatedBytecodeLabel label) {
                if (labelReferences[label.LabelName] == 0) {
                    input.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    private static void JoinAndForwardLabels(List<IAnnotatedBytecode> input) {
        Dictionary<string, string> labelAliases = new();
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
            if (input[i] is AnnotatedBytecodeInstruction instruction) {
                if (TryGetLabelName(instruction, out string? labelName)) {
                    if (labelAliases.ContainsKey(labelName)) {
                        List<IAnnotatedBytecode> args = instruction.GetArgs();
                        for (int j = 0; j < args.Count; j++) {
                            if (args[j] is AnnotatedBytecodeLabel argLabel) {
                                args[j] = new AnnotatedBytecodeLabel(labelAliases[argLabel.LabelName],
                                    argLabel.Location);
                            }
                        }

                        input[i] = new AnnotatedBytecodeInstruction(instruction, args);
                    }
                }
            }
        }
    }

    private static bool TryGetLabelName(AnnotatedBytecodeInstruction instruction, [NotNullWhen(true)] out string? labelName) {
        foreach (var arg in instruction.GetArgs()) {
            if (arg is not AnnotatedBytecodeLabel label)
                continue;

            labelName = label.LabelName;
            return true;
        }

        labelName = null;
        return false;
    }
}
