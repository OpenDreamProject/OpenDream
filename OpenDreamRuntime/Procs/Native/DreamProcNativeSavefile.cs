using OpenDreamRuntime.Objects.Types;
using DreamValueType = OpenDreamRuntime.DreamValue.DreamValueType;

namespace OpenDreamRuntime.Procs.Native;

internal static class DreamProcNativeSavefile {
    [DreamProc("ExportText")]
    [DreamProcParameter("path", Type = DreamValueType.String)]
    [DreamProcParameter("file", Type = DreamValueType.String | DreamValueType.DreamResource)]
    public static DreamValue NativeProc_ExportText(NativeProc.State state) {
        // Implementing this correctly is a fair amount of effort, and the only use of it I'm aware of is icon2base64()
        // So this implements it just enough to get that working

        var savefile = (DreamObjectSavefile)state.Src!;
        DreamValue path = state.GetArgument(0, "path");
        DreamValue file = state.GetArgument(1, "file");

        if (!path.TryGetValueAsString(out var pathStr) || file != DreamValue.Null) {
            throw new NotImplementedException("General support for ExportText() is not implemented");
        }

        // Treat pathStr as the name of a value in the current dir, as that's how icon2base64() uses it
        if (!savefile.CurrentDir.TryGetValue(pathStr, out var exportValue)) {
            throw new NotImplementedException("General support for ExportText() is not implemented");
        }

        if (!state.ResourceManager.TryLoadIcon(exportValue, out var icon)) {
            throw new NotImplementedException("General support for ExportText() is not implemented");
        }

        var base64 = Convert.ToBase64String(icon.ResourceData);
        var exportedText = $"{{\"\n{base64}\n\"}})";
        return new DreamValue(exportedText);
    }

    [DreamProc("Flush")]
    public static DreamValue NativeProc_Flush(NativeProc.State state) {
        var savefile = (DreamObjectSavefile)state.Src!;

        savefile.Flush();
        return DreamValue.Null;
    }
}
