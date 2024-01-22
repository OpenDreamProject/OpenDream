using System.Collections.Generic;
using System.Linq;
using DMCompiler.Bytecode;

namespace DMCompiler.DM.Optimizer {
    internal interface PeepholeOptimization {
        public int GetLength();
        public List<DreamProcOpcode> GetOpcodes();
        public void Apply(List<IAnnotatedBytecode> input, int index);

        public bool CheckPreconditions(List<IAnnotatedBytecode> input, int index) {
            return true;
        }
        bool IsDisabled() {
            return false;
        }
    }

    class PeepholeOptimizer {
        private static List<PeepholeOptimization> optimizations = typeof(PeepholeOptimizer).Assembly.GetTypes()
            .Where(x => typeof(PeepholeOptimization).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
            .Select(x => (PeepholeOptimization)System.Activator.CreateInstance(x)).Where(x => !x.IsDisabled()).ToList();

        public static void RunPeephole(List<IAnnotatedBytecode> input) {
            int changes = 1;
            while (changes > 0) {
                changes = 0;
                // Pass 1: Find all 5-byte sequences
                for (int i = 0; i < input.Count; i++) {
                    if (i > input.Count - 5) {
                        break;
                    }

                    if (input.GetRange(i, 5).TrueForAll(x => x is AnnotatedBytecodeInstruction)) {
                        var slice5 = input.GetRange(i, 5).Select(x => ((AnnotatedBytecodeInstruction)x).Opcode)
                            .ToList();
                        foreach (var opt in optimizations) {
                            if (opt.GetLength() == 5 && opt.GetOpcodes().SequenceEqual(slice5) &&
                                opt.CheckPreconditions(input, i)) {
                                var startpoint = input.GetRange(i, 5).Where(x => x.GetLocation().Line != null ).FirstOrDefault() ?? input[i];
                                changes++;
                                opt.Apply(input, i);
                                input[i].SetLocation(startpoint);
                            }
                        }
                    }
                }

                // Pass 2: Find all 4-byte sequences
                for (int i = 0; i < input.Count; i++) {
                    if (i > input.Count - 4) {
                        break;
                    }

                    if (input.GetRange(i, 4).TrueForAll(x => x is AnnotatedBytecodeInstruction)) {
                        var slice4 = input.GetRange(i, 4).Select(x => ((AnnotatedBytecodeInstruction)x).Opcode)
                            .ToList();
                        foreach (var opt in optimizations) {
                            if (opt.GetLength() == 4 && opt.GetOpcodes().SequenceEqual(slice4) &&
                                opt.CheckPreconditions(input, i)) {
                                var startpoint = input.GetRange(i, 4).Where(x => x.GetLocation().Line != null).FirstOrDefault() ?? input[i];
                                changes++;
                                opt.Apply(input, i);
                                input[i].SetLocation(startpoint);
                            }
                        }
                    }
                }

                // Pass 3: Find all 3-byte sequences
                for (int i = 0; i < input.Count; i++) {
                    if (i > input.Count - 3) {
                        break;
                    }

                    if (input.GetRange(i, 3).TrueForAll(x => x is AnnotatedBytecodeInstruction)) {
                        var slice3 = input.GetRange(i, 3).Select(x => ((AnnotatedBytecodeInstruction)x).Opcode)
                            .ToList();
                        var slice2 = input.GetRange(i, 2).Select(x => ((AnnotatedBytecodeInstruction)x).Opcode)
                            .ToList();
                        foreach (var opt in optimizations) {
                            if (opt.GetLength() == 3 && opt.GetOpcodes().SequenceEqual(slice3) &&
                                opt.CheckPreconditions(input, i)) {
                                var startpoint = input.GetRange(i, 3).Where(x => x.GetLocation().Line != null).FirstOrDefault() ?? input[i];
                                changes++;
                                opt.Apply(input, i);
                                input[i].SetLocation(startpoint);
                            }
                        }
                    }
                }

                // Pass 4: Find all 2-byte sequences
                for (int i = 0; i < input.Count; i++) {
                    if (i > input.Count - 2) {
                        break;
                    }

                    if (input.GetRange(i, 2).TrueForAll(x => x is AnnotatedBytecodeInstruction)) {
                        var slice2 = input.GetRange(i, 2).Select(x => ((AnnotatedBytecodeInstruction)x).Opcode)
                            .ToList();
                        foreach (var opt in optimizations) {
                            if (opt.GetLength() == 2 && opt.GetOpcodes().SequenceEqual(slice2) &&
                                opt.CheckPreconditions(input, i)) {
                                changes++;
                                var startpoint = input.GetRange(i, 2).Where(x => x.GetLocation().Line != null).FirstOrDefault() ?? input[i];
                                opt.Apply(input, i);
                                input[i].SetLocation(startpoint);
                            }
                        }
                    }
                }
            }
        }
    }
}
