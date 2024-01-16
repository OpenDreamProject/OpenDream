using System;
using System.Collections.Generic;
using System.Linq;

namespace DMCompiler.DM.Optimizer;

public class BytecodeOptimizer {
    public List<IAnnotatedBytecode> Optimize(List<IAnnotatedBytecode> input, string errorPath) {
        if (input.Count == 0) return input;
        /*{
            // Temp debugging dump
            string directory = "/home/reagan/OpenDreamCFG/";
            string name = "precfg";
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, name);
            StringBuilder sb = new StringBuilder();
            AnnotatedBytecodePrinter.Print(input, new List<SourceInfoJson>(), sb);
            File.WriteAllText(file, sb.ToString());
        }
        */
        var root = CFGStackCodeConverter.Convert(input, errorPath);
        //List<SSAObject> ssaInstructions = new List<SSAObject>();
        //ssaInstructions = SSAConvert.Convert(root);
        var newCode = root.SelectMany(x => x.Bytecode).ToList();
        // Dump new and old code for comparison
        /*
        {
            // Temp debugging dump
            string directory = "/home/reagan/OpenDreamCFG/";
            string name = "postcfg";
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, name);
            StringBuilder sb = new StringBuilder();
            AnnotatedBytecodePrinter.Print(newCode, new List<SourceInfoJson>(), sb);
            File.WriteAllText(file, sb.ToString());
        }*/
        return newCode;
    }

    public int GetMaxStackSize() {
        throw new NotImplementedException();
    }
}
