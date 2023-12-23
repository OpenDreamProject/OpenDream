using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;

namespace OpenDreamRuntime.Procs.Native;

internal static class DreamProcNativeDatum {
    [DreamProc("Write")]
    [DreamProcParameter("F", Type = DreamValue.DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_Write(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var probablySavefile = bundle.GetArgument(0, "F");
        if (!probablySavefile.TryGetValueAsDreamObject(out DreamObjectSavefile? savefile) && savefile == null)
            return DreamValue.Null; // error out bad path or something

        // TODO Reimplmement
        // foreach (var key in src!.GetVariableNames()) {
        //     if (!src.IsSaved(key)) continue;
        //     var result = src.GetVariable(key);
        //
        //     // skip if initial var is same
        //     if (src.ObjectDefinition.TryGetVariable(key, out var val) && val == result) continue;
        //     savefile.SetSavefileValue(key, result);
        // }
        return DreamValue.Null;
    }
}
