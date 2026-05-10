using System.Diagnostics.CodeAnalysis;
using DMCompiler.Bytecode;

namespace DMCompiler.Optimizer;

public class BytecodeOptimizer(DMCompiler compiler) {
    private readonly PeepholeOptimizer _peepholeOptimizer = new(compiler);

    internal void Optimize(List<IAnnotatedBytecode> input) {
        if(compiler.Settings.NoOpts) // Optimizations are disabled
            return;

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
        for (int i = 0; i < input.Count; i++) {
            switch (input[i]) {
                case AnnotatedBytecodeLabel label:
                    labelReferences ??= new Dictionary<string, int>();
                    labelReferences.TryAdd(label.LabelName, 0);
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
        var labelCount = labelReferences.Count;

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

        ForwardLabelsAfterTerminalJump(input, labelAliases);

        for (int i = 0; i < input.Count; i++) {
            if (input[i] is AnnotatedBytecodeInstruction instruction) {
                if (TryGetLabelName(instruction, out string? labelName)) {
                    if (labelAliases.ContainsKey(labelName)) {
                        List<IAnnotatedBytecode> args = instruction.GetArgs();
                        for (int j = 0; j < args.Count; j++) {
                            if (args[j] is AnnotatedBytecodeLabel argLabel) {
                                args[j] = new AnnotatedBytecodeLabel(ResolveLabelAlias(labelAliases, argLabel.LabelName),
                                    argLabel.Location);
                            }
                        }

                        input[i] = new AnnotatedBytecodeInstruction(instruction, args);
                    }
                }
            }
        }
    }

    private void ForwardLabelsAfterTerminalJump(List<IAnnotatedBytecode> input, Dictionary<string, string> labelAliases) {
        for (int i = 0; i < input.Count; i++) {
            if (input[i] is not AnnotatedBytecodeInstruction instruction || !IsTerminalInstruction(instruction.Opcode))
                continue;

            var labels = new List<string>();
            for (int j = i + 1; j < input.Count; j++) {
                switch (input[j]) {
                    case AnnotatedBytecodeVariable:
                        continue;
                    case AnnotatedBytecodeLabel label:
                        labels.Add(label.LabelName);
                        continue;
                    case AnnotatedBytecodeInstruction { Opcode: DreamProcOpcode.Jump } jump when labels.Count > 0: {
                        string targetLabel = jump.GetArg<AnnotatedBytecodeLabel>(0).LabelName;
                        if (labels.Contains(targetLabel))
                            break;

                        foreach (string labelName in labels) {
                            labelAliases[labelName] = targetLabel;
                        }

                        break;
                    }
                }

                break;
            }
        }
    }

    // TODO: Once we have a CFG we'll likely be storing this info in opcode metadata and this hardcoded list can be removed
    private bool IsTerminalInstruction(DreamProcOpcode opcode) {
        return opcode is DreamProcOpcode.Return or DreamProcOpcode.ReturnReferenceValue or DreamProcOpcode.ReturnFloat or DreamProcOpcode.Throw;
    }

    private string ResolveLabelAlias(Dictionary<string, string> labelAliases, string labelName) {
        HashSet<string>? visited = null;
        while (labelAliases.TryGetValue(labelName, out string? alias) && alias != labelName) {
            visited ??= new();
            if (!visited.Add(labelName))
                break;

            labelName = alias;
        }

        return labelName;
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
