using System.Runtime.CompilerServices;
using DMCompiler.Bytecode;

namespace DMCompiler.Optimizer;

/// <summary>
/// Every interface that inherits IOptimization can be executed as a separate peephole optimizer pass
/// </summary>
internal interface IOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes();
    public void Apply(List<IAnnotatedBytecode> input, int index);

    public bool CheckPreconditions(List<IAnnotatedBytecode> input, int index) {
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AnnotatedBytecodeInstruction GetInstructionAndValue(IAnnotatedBytecode input, out float value, int argIndex = 0) {
        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input);
        value = firstInstruction.GetArg<AnnotatedBytecodeFloat>(argIndex).Value;
        return firstInstruction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReplaceInstructions(List<IAnnotatedBytecode> input, int index, int replacedOpcodes, AnnotatedBytecodeInstruction replacement) {
        input.RemoveRange(index, replacedOpcodes);
        input.Insert(index, replacement);
    }
}

/// <summary>
/// First-pass peephole optimizations (e.g. const folding)
/// </summary>
internal interface IPeepholeOptimization : IOptimization;

/// <summary>
/// Next-pass bytecode compacting (e.g. PushNFloats and other PushN opcodes)
/// </summary>
internal interface IBytecodeCompactor : IOptimization;

/// <summary>
/// Final-pass list compacting (e.g. PushNFloats & CreateList -> CreateListNFloats)
/// </summary>
internal interface IListCompactor : IOptimization;

internal sealed class PeepholeOptimizer<T> where T : class, IOptimization {
    private class OptimizationTreeEntry {
        public T? Optimization;
        public Dictionary<DreamProcOpcode, OptimizationTreeEntry>? Children;
    }

    /// <summary>
    /// Trees matching chains of opcodes to peephole optimizations
    /// </summary>
    private static readonly Dictionary<DreamProcOpcode, OptimizationTreeEntry> OptimizationTrees = new();

    /// Setup <see cref="OptimizationTrees"/>
    static PeepholeOptimizer() {
        var possibleTypes = typeof(T).Assembly.GetTypes();
        var optimizationTypes = new List<Type>();

        foreach (var type in possibleTypes) {
            if (typeof(T).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract) {
                optimizationTypes.Add(type);
            }
        }

        foreach (var optType in optimizationTypes) {
            var opt = (T)(Activator.CreateInstance(optType)!);
            var opcodes = opt.GetOpcodes();
            if (opcodes.Length < 2) {
                DMCompiler.ForcedError(Location.Internal, $"Peephole optimization {optType} must have at least 2 opcodes");
                continue;
            }

            if (!OptimizationTrees.TryGetValue(opcodes[0], out var treeEntry)) {
                treeEntry = new() {
                    Children = new()
                };

                OptimizationTrees.Add(opcodes[0], treeEntry);
            }

            for (int i = 1; i < opcodes.Length; i++) {
                if (treeEntry.Children == null || !treeEntry.Children.TryGetValue(opcodes[i], out var child)) {
                    child = new();

                    treeEntry.Children ??= new(1);
                    treeEntry.Children.Add(opcodes[i], child);
                }

                treeEntry = child;
            }

            // Final child in this path, assign the optimization to this entry
            treeEntry.Optimization = opt;
        }
    }

    public static void RunOptimizations(List<IAnnotatedBytecode> input) {
        OptimizationTreeEntry? currentOpt = null;
        int optSize = 0;

        int AttemptCurrentOpt(int i) {
            if (currentOpt == null)
                return 0;

            int offset;

            if (currentOpt.Optimization?.CheckPreconditions(input, i - optSize) is true) {
                currentOpt.Optimization.Apply(input, i - optSize);
                offset = (optSize + 2); // Run over the new opcodes for potential further optimization
            } else {
                // This chain of opcodes did not lead to a valid optimization.
                // Start again from the opcode after the first.
                offset = optSize;
            }

            currentOpt = null;
            return offset;
        }

        for (int i = 0; i < input.Count; i++) {
            var bytecode = input[i];
            if (bytecode is not AnnotatedBytecodeInstruction instruction) {
                i -= AttemptCurrentOpt(i);
                i = Math.Max(i, 0);
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

            i -= AttemptCurrentOpt(i);
            i = Math.Max(i, 0);
        }

        AttemptCurrentOpt(input.Count);
    }
}
