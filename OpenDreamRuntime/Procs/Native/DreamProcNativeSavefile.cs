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

        if(!path.IsNull && path.TryGetValueAsString(out var pathStr)) { //invalid path values are just ignored in BYOND
            savefile.ChangeDirectory(pathStr);
        }

        string result = "";
        foreach (var (key, value) in savefile.CurrentDir) {
            if(value.IsNull)
                result += $"{key} = null\n";
            else {
                switch(value.Type) {
                    case DreamValue.DreamValueType.String:
                        result += $"{key} = \"{value.MustGetValueAsString()}\"\n";
                        break;
                    case DreamValue.DreamValueType.Float:
                        result += $"{key} = {value.MustGetValueAsFloat()}\n";
                        break;
                    case DreamValue.DreamValueType.DreamResource:
                        result += $"{key} = {Convert.ToBase64String(value.MustGetValueAsDreamResource().ResourceData!)}\n";
                        break;
                    case DreamValue.DreamValueType.DreamObject:
                        if (value.TryGetValueAsDreamObject<DreamObjectIcon>(out _) && bundle.ResourceManager.TryLoadIcon(value, out var icon))
                            result += $"{key} = {Convert.ToBase64String(icon.ResourceData!)}\n";
                        else
                            result += $"{key} = {value.MustGetValueAsDreamObject()}\n";
                        break;
                    default:
                        throw new NotImplementedException($"Unhandled type {key} = {value.Stringify()} in ExportText()");
                }
            }

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
