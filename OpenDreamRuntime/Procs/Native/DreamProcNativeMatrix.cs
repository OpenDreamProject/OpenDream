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

        [DreamProc("Scale")]
        public static DreamValue NativeProc_Scale(DreamObject src, DreamObject usr, DreamProcArguments arguments) {
            float horizontalScale;
            float verticalScale;
            arguments.GetArgument(0, "x").TryGetValueAsFloat(out horizontalScale);
            if (arguments.ArgumentCount == 2)
                arguments.GetArgument(1, "y").TryGetValueAsFloat(out verticalScale);
            else
                verticalScale = horizontalScale;
            DreamMetaObjectMatrix.ScaleMatrix(src, horizontalScale, verticalScale);
            return new DreamValue(src);
        }
    }
}
