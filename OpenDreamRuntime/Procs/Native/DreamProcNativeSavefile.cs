using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;

namespace OpenDreamRuntime.Procs.Native {
    static class DreamProcNativeSavefile {
        [DreamProc("Flush")]
        public static DreamValue NativeProc_Flush(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamMetaObjectSavefile.Savefile savefile = DreamMetaObjectSavefile.ObjectToSavefile[instance];

            savefile.Flush();
            return DreamValue.Null;
        }
    }
}
