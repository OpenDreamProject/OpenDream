using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DMCompiler.Optimizer.CFG;
using OpenDreamShared.Json;

namespace DMCompiler.DM.Optimizer;

public class BytecodeOptimizer {
    private static int id;

    public List<IAnnotatedBytecode> Optimize(List<IAnnotatedBytecode> input, string errorPath) {
        if (input.Count == 0) return input;

        var cfgCode = CFGStackCodeConverter.Convert(input, errorPath);
        var newCode = CFGStackCodeConverter.ConvertBack(cfgCode);
        return newCode;
    }

    private static void DumpCode(List<IAnnotatedBytecode> input, string name)
    {
        // Temp debugging dump
        var directory = Directory.GetCurrentDirectory();
        Directory.CreateDirectory(directory);
        string file = Path.Combine(directory, name);
        StringBuilder sb = new StringBuilder();
        AnnotatedBytecodePrinter.Print(input, new List<SourceInfoJson>(), sb);
        File.WriteAllText(file, sb.ToString());
    }

    public int GetMaxStackSize() {
        throw new NotImplementedException();
    }
}
