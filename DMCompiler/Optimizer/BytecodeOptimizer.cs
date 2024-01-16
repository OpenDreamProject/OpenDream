using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenDreamShared.Json;

namespace DMCompiler.DM.Optimizer;

public class BytecodeOptimizer {
    private static int id;

    public List<IAnnotatedBytecode> Optimize(List<IAnnotatedBytecode> input, string errorPath) {
        if (input.Count == 0) return input;
        {
            // Temp debugging dump
            var directory = Directory.GetCurrentDirectory();
            string name = "precfg";
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, name);
            StringBuilder sb = new StringBuilder();
            AnnotatedBytecodePrinter.Print(input, new List<SourceInfoJson>(), sb);
            File.WriteAllText(file, sb.ToString());
        }

        var cfgCode = CFGStackCodeConverter.Convert(input, errorPath);
        cfgCode = RunCFGPasses(cfgCode);
        //List<SSAObject> ssaInstructions = new List<SSAObject>();
        //ssaInstructions = SSAConvert.Convert(root);
        var newCode = cfgCode.SelectMany(x => x.Bytecode).ToList();
        // Dump new and old code for comparison

        {
            // Temp debugging dump
            var directory = Directory.GetCurrentDirectory();
            string name = "postcfg";
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, name);
            StringBuilder sb = new StringBuilder();
            AnnotatedBytecodePrinter.Print(newCode, new List<SourceInfoJson>(), sb);
            File.WriteAllText(file, sb.ToString());
        }
        return newCode;
    }

    public int GetMaxStackSize() {
        throw new NotImplementedException();
    }

    private List<CFGBasicBlock> RunCFGPasses(List<CFGBasicBlock> input) {
        var output = input;
        output = CFGPasses.RemoveUnreachableBlocks(output);
        CFGStackCodeConverter.DumpCFGToDebugDir(output, $"{id}-preremovejumps");
        int didChange;
        do {
            output = CFGPasses.RemoveUnnecessaryJumps(output, out didChange);
        } while (didChange > 0);

        CFGStackCodeConverter.DumpCFGToDebugDir(output, $"{id}-postremovejumps");
        if (didChange > 0) {
            id++;
            return output;
        }

        return output;
    }
}
