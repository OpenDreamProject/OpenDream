using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using DreamValueTypeFlag = OpenDreamRuntime.DreamValue.DreamValueTypeFlag;

namespace OpenDreamRuntime.Procs.Native;

internal static class DreamProcNativeDatabase {
    [DreamProc("Close")]
    public static DreamValue NativeProc_Close(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var database = (DreamObjectDatabase)src!;

        database.Close();

        return DreamValue.Null;
    }

    [DreamProc("Error")]
    public static DreamValue NativeProc_Error(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var database = (DreamObjectDatabase)src!;

        return new DreamValue(database.GetErrorCode() ?? 0);
    }

    [DreamProc("ErrorMsg")]
    public static DreamValue NativeProc_ErrorMsg(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var database = (DreamObjectDatabase)src!;

        var message = database.GetErrorMessage();
        return message == null ? DreamValue.Null : new DreamValue(message);
    }

    [DreamProc("Open")]
    [DreamProcParameter("filename", Type = DreamValueTypeFlag.String)]
    public static DreamValue NativeProc_Open(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var database = (DreamObjectDatabase)src!;

        DreamValue fileValue = bundle.GetArgument(0, "filename");

        if (fileValue.TryGetValueAsString(out var filename)) {
            database.Open(filename);
        }

        return DreamValue.Null;
    }
}
