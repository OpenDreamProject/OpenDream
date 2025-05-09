using System.Diagnostics.CodeAnalysis;

namespace DMCompiler.Optimizer;

public class BytecodeOptimizer(DMCompiler compiler) {
    private readonly PeepholeOptimizer _peepholeOptimizer = new(compiler);

    internal void Optimize(List<IAnnotatedBytecode> input) {
        if (input.Count == 0)
            return;

        RemoveUnreferencedLabels(input);
        JoinAndForwardLabels(input);
        RemoveUnreferencedLabels(input);
        RemoveImmediateJumps(input);
        RemoveUnreferencedLabels(input);

        _peepholeOptimizer.RunPeephole(input);

        // Run label optimizations again due to possibly removed jumps in peephole optimizers
        RemoveUnreferencedLabels(input);
        JoinAndForwardLabels(input);
        RemoveUnreferencedLabels(input);
        RemoveImmediateJumps(input);
        RemoveUnreferencedLabels(input);
    }

    private void RemoveUnreferencedLabels(List<IAnnotatedBytecode> input) {
        Dictionary<string, int>? labelReferences = null;
        var labelCount = 0;
        for (int i = 0; i < input.Count; i++) {
            switch (input[i])
            {
                case AnnotatedBytecodeLabel label:
                    labelReferences ??= new Dictionary<string, int>();
                    labelReferences.TryAdd(label.LabelName, 0);
                    labelCount += 1;
                    break;
                case AnnotatedBytecodeInstruction instruction: {
                    if (TryGetLabelName(instruction, out string? labelName)) {
                        labelReferences ??= new Dictionary<string, int>();
                        if (!labelReferences.TryAdd(labelName, 1)) {
                            labelReferences[labelName] += 1;
                        }
                    }

                    break;
                }
            }
        }

        if (labelReferences == null) return;

        for (int i = 0; i < input.Count; i++) {
            if (input[i] is AnnotatedBytecodeLabel label) {
                if (labelReferences[label.LabelName] == 0) {
                    input.RemoveAt(i);
                    i -= 1;
                }
                
                labelCount -= 1;
                if (labelCount <= 0) break;
            }
        }
    }

    /**
     * <summary>Removes jumps for which the next element is the jump's destination</summary>
     */
    private void RemoveImmediateJumps(List<IAnnotatedBytecode> input) {
        for (int i = input.Count - 2; i >= 0; i--) {
            if (input[i] is AnnotatedBytecodeInstruction { Opcode: Bytecode.DreamProcOpcode.Jump } instruction) {
                if (input[i + 1] is AnnotatedBytecodeLabel followingLabel) {
                    AnnotatedBytecodeLabel jumpLabelName = instruction.GetArg<AnnotatedBytecodeLabel>(0);
                    if (jumpLabelName.LabelName == followingLabel.LabelName) {
                        input.RemoveAt(i);
                    }
                }
            }
        }
    }

    private void JoinAndForwardLabels(List<IAnnotatedBytecode> input) {
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

    private bool TryGetLabelName(AnnotatedBytecodeInstruction instruction, [NotNullWhen(true)] out string? labelName) {
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
