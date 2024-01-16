using System;
using System.Collections.Generic;
using DMCompiler.DM.Optimizer.SSAInstructions;

namespace DMCompiler.DM.Optimizer;

public class SSAConvert {
    public static List<SSAObject> Convert(CFGBasicBlock input) {
        var ssaConvert = new SSAConvert();
        return ssaConvert.ConvertInternal(input);
    }

    public static List<IAnnotatedBytecode> ConvertBack(List<SSAObject> ssaInstructions) {
        var ssaConvert = new SSAConvert();
        return ssaConvert.ConvertBackInternal(ssaInstructions);
    }

    private List<SSAObject> ConvertInternal(CFGBasicBlock input) {
        throw new NotImplementedException();
    }

    private List<IAnnotatedBytecode> ConvertBackInternal(List<SSAObject> ssaInstructions) {
        throw new NotImplementedException();
    }
}
