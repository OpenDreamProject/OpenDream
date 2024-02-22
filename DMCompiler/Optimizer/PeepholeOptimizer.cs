using System;
using System.Collections.Generic;
using System.Linq;
using DMCompiler.Bytecode;

namespace DMCompiler.DM.Optimizer;

internal interface IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes();
    public void Apply(List<IAnnotatedBytecode> input, int index);

    public bool CheckPreconditions(List<IAnnotatedBytecode> input, int index) {
        return true;
    }
}

internal sealed class PeepholeOptimizer {
    private class OptimizationTreeEntry {
        public IPeepholeOptimization? Optimization;
        public Dictionary<DreamProcOpcode, OptimizationTreeEntry>? Children;
    }

    /// <summary>
    /// Trees matching chains of opcodes to peephole optimizations
    /// </summary>
    private static readonly Dictionary<DreamProcOpcode, OptimizationTreeEntry> OptimizationTrees = new();

    /// Setup <see cref="OptimizationTrees"/>
    static PeepholeOptimizer() {
        var optimizationTypes =
            typeof(PeepholeOptimizer).Assembly.GetTypes().Where(x => typeof(IPeepholeOptimization).IsAssignableFrom(x));

        foreach (var optType in optimizationTypes) {
            if (optType.IsInterface || optType.IsAbstract)
                continue;

            var opt = (IPeepholeOptimization)Activator.CreateInstance(optType)!;
            var opcodes = opt.GetOpcodes();
            if (opcodes.Length < 2)
                throw new Exception($"Peephole optimization {optType} must have at least 2 opcodes");

            if (!OptimizationTrees.TryGetValue(opcodes[0], out var treeEntry)) {
                treeEntry = new() {
                    Children = new()
                };

                OptimizationTrees.Add(opcodes[0], treeEntry);
            }

            for (int i = 1; i < opcodes.Length; i++) {
                if (treeEntry.Children == null || !treeEntry.Children.TryGetValue(opcodes[i], out var child)) {
                    child = new();

                    treeEntry.Children ??= new();
                    treeEntry.Children.Add(opcodes[i], child);
                }

                treeEntry = child;
            }

            // Final child in this path, assign the optimization to this entry
            treeEntry.Optimization = opt;
        }
    }

    public static void RunPeephole(List<IAnnotatedBytecode> input) {
        OptimizationTreeEntry? currentOpt = null;
        int optSize = 0;

        for (int i = 0; i < input.Count; i++) {
            var bytecode = input[i];
            if (bytecode is not AnnotatedBytecodeInstruction instruction) {
                currentOpt = null;
                continue;
            }

            var opcode = instruction.Opcode;

            if (currentOpt == null) {
                optSize = 1;
                OptimizationTrees.TryGetValue(opcode, out currentOpt);
                continue;
            }

            if (currentOpt.Children?.TryGetValue(opcode, out var childOpt) is true) {
                optSize++;
                currentOpt = childOpt;
                continue;
            }

            if (currentOpt.Optimization?.CheckPreconditions(input, i - optSize) is true) {
                currentOpt.Optimization.Apply(input, i - optSize);
                i -= (optSize + 1); // Run over the new opcodes for potential further optimization
            } else {
                // This chain of opcodes did not lead to a valid optimization.
                // Start again from the opcode after the first.
                i -= optSize;
            }

            currentOpt = null;
        }
    }
}
