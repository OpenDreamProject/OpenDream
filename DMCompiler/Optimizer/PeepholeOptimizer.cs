using System.Runtime.CompilerServices;
using DMCompiler.Bytecode;

namespace DMCompiler.Optimizer;

/// <summary>
/// A single peephole optimization (e.g. const fold an operator)
/// </summary>
internal interface IOptimization {
    public OptPass OptimizationPass { get; }
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
/// The list of peephole optimizer passes in the order that they should run
/// </summary>
internal enum OptPass : byte {
    PeepholeOptimization = 1,   // First-pass peephole optimizations (e.g. const folding)
    BytecodeCompactor = 2,      // Next-pass bytecode compacting (e.g. PushNFloats and other PushN opcodes)
    ListCompactor = 3           // Final-pass list compacting (e.g. PushNFloats & CreateList -> CreateListNFloats)
}

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class PeepholeOptimizer {
    private class OptimizationTreeEntry {
        public IOptimization? Optimization;
        public Dictionary<DreamProcOpcode, OptimizationTreeEntry>? Children;
    }

    /// <summary>
    /// Trees matching chains of opcodes to peephole optimizations
    /// </summary>
    /// <remarks>This is instantiated when the optimizer runs</remarks>
    private static Dictionary<DreamProcOpcode, OptimizationTreeEntry> _optimizationTrees = null!;

    /// Setup <see cref="_optimizationTrees"/> for the current <see cref="OptPass"/>
    private static void GetOptimizations(OptPass optPass) {
        _optimizationTrees = new();

        var possibleTypes = typeof(IOptimization).Assembly.GetTypes();
        var optimizationTypes = new List<Type>();

        foreach (var type in possibleTypes) {
            if (typeof(IOptimization).IsAssignableFrom(type) && type is { IsClass: true, IsAbstract: false }) {
                optimizationTypes.Add(type);
            }
        }

        foreach (var optType in optimizationTypes) {
            var opt = (IOptimization)(Activator.CreateInstance(optType)!);
            if(opt.OptimizationPass != optPass)
                continue;

            var opcodes = opt.GetOpcodes();
            if (opcodes.Length < 2) {
                DMCompiler.ForcedError(Location.Internal, $"Peephole optimization {optType} must have at least 2 opcodes");
                continue;
            }

            if (!_optimizationTrees.TryGetValue(opcodes[0], out var treeEntry)) {
                treeEntry = new() {
                    Children = new()
                };

                _optimizationTrees.Add(opcodes[0], treeEntry);
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
        var passes = (OptPass[])Enum.GetValues(typeof(OptPass));
        foreach (var optPass in passes) {
            GetOptimizations(optPass);
            RunPass(input);
        }
    }

    private static void RunPass(List<IAnnotatedBytecode> input) {
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
                _optimizationTrees.TryGetValue(opcode, out currentOpt);
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
