using System.Collections.Generic;
using OpenDreamRuntime.Objects;

namespace OpenDreamRuntime.Procs.Native
{
    public static class DreamProcNativeMatrix
    {
        [DreamProc("Invert")]
        public static DreamValue NativeProc_Invert(DreamObject instance, DreamObject usr, DreamProcArguments arguments)
        {
            double a = instance.GetVariable("a").GetValueAsFloat();
            double b = instance.GetVariable("b").GetValueAsFloat();
            double c = instance.GetVariable("c").GetValueAsFloat();
            double d = instance.GetVariable("d").GetValueAsFloat();
            double e = instance.GetVariable("e").GetValueAsFloat();
            double f = instance.GetVariable("f").GetValueAsFloat();
            double determinant = a * e - d * b;

            instance.SetVariable("a", new DreamValue((float)e));
            instance.SetVariable("b", new DreamValue((float)-b));
            instance.SetVariable("c", new DreamValue((float)(b*f - e*c)));
            instance.SetVariable("d", new DreamValue((float)-d));
            instance.SetVariable("e", new DreamValue((float)a));
            instance.SetVariable("f", new DreamValue((float)(d*c - a*f)));

            return instance.GetProc("Scale")
                .Spawn(instance, new DreamProcArguments(new List<DreamValue> {new((float)(1/determinant))}));
        }
    }
}
