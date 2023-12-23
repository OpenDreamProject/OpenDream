using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using DreamValueTypeFlag = OpenDreamRuntime.DreamValue.DreamValueTypeFlag;

namespace OpenDreamRuntime.Procs.Native;

internal static class DreamProcNativeSavefile {
    [DreamProc("ExportText")]
    [DreamProcParameter("path", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("file", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamResource)]
    public static DreamValue NativeProc_ExportText(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        // Implementing this correctly is a fair amount of effort, and the only use of it I'm aware of is icon2base64()
        // So this implements it just enough to get that working

        var savefile = (DreamObjectSavefile)src!;
        DreamValue path = bundle.GetArgument(0, "path");
        DreamValue file = bundle.GetArgument(1, "file");

        if (!path.TryGetValueAsString(out var pathStr) || !file.IsNull) {
            throw new NotImplementedException("General support for ExportText() is not implemented");
        }

        // Treat pathStr as the name of a value in the current dir, as that's how icon2base64() uses it
        if (!savefile.CurrentDir.TryGetValue(pathStr, out var exportValue) || exportValue is not DreamObjectSavefile.DreamFileValue fileData) {
            throw new NotImplementedException("General support for ExportText() is not implemented");
        }

        if (!bundle.ResourceManager.TryLoadIcon(savefile.RealizeJsonValue(fileData), out var icon)) {
            throw new NotImplementedException("General support for ExportText() is not implemented");
        }

        var base64 = Convert.ToBase64String(icon.ResourceData);
        var exportedText = $"{{\"\n{base64}\n\"}})";
        return new DreamValue(exportedText);
    }

    [DreamProc("Flush")]
    public static DreamValue NativeProc_Flush(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var savefile = (DreamObjectSavefile)src!;

        savefile.Flush();
        return DreamValue.Null;
    }
}
