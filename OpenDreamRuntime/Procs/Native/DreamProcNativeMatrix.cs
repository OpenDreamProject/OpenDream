using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDreamRuntime.Procs.Native {
    internal static class DreamProcNativeMatrix {
        [DreamProc("Invert")]
        public static DreamValue NativeProc_Invert(DreamObject src, DreamObject usr, DreamProcArguments arguments) {
            if (!DreamMetaObjectMatrix.TryInvert(src)) {
                throw new ArgumentException("Matrix does not have a valid inversion for Invert()");
            }
            return new DreamValue(src);
        }
    }
}
