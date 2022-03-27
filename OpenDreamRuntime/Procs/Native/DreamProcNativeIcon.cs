using System;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;

namespace OpenDreamRuntime.Procs.Native {
    static class DreamProcNativeIcon {
        [DreamProc("Width")]
        public static DreamValue NativeProc_Width(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamMetaObjectIcon.DreamIcon dreamIcon = DreamMetaObjectIcon.ObjectToDreamIcon[instance];

            return new DreamValue(dreamIcon.Description.Width);
        }

        [DreamProc("Height")]
        public static DreamValue NativeProc_Height(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamMetaObjectIcon.DreamIcon dreamIcon = DreamMetaObjectIcon.ObjectToDreamIcon[instance];

            return new DreamValue(dreamIcon.Description.Height);
        }


    }
}
