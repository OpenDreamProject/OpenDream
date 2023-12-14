using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Resources;
using DreamValueTypeFlag = OpenDreamRuntime.DreamValue.DreamValueTypeFlag;
using System.IO;
using YamlDotNet.Core.Tokens;

namespace OpenDreamRuntime.Procs.Native;

internal static class DreamProcNativeSavefile {
    [DreamProc("ExportText")]
    [DreamProcParameter("path", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("file", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamResource)]
    public static DreamValue NativeProc_ExportText(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {

        var savefile = (DreamObjectSavefile)src!;
        DreamValue path = bundle.GetArgument(0, "path");
        DreamValue file = bundle.GetArgument(1, "file");

        if(!path.IsNull && path.TryGetValueAsString(out var pathStr)) { //invalid path values are just ignored in BYOND
            savefile.ChangeDirectory(pathStr);
        }

        string result = ExportTextInternal(savefile);
        //TODO if file is not null, write to file
        return new DreamValue(result);
    }


    private static string ExportTextInternal(DreamObjectSavefile savefile, int indent = 0) {
        string result = "";

        foreach (string key in savefile.GetCurrentDirKeys()) {
            var value = savefile.CurrentDir[key]; //TODO handle savefile directories
            //if value is a savefile directory, recurse
            //result += ExportTextInternal(savefile, indent + 1);
            if(value.IsNull)
                result += $"{new string('\t', indent)}{key} = null\n";
            else {
                switch(value.Type) {
                    case DreamValue.DreamValueType.String:
                        result += $"{new string('\t', indent)}{key} = \"{value.MustGetValueAsString()}\"\n";
                        break;
                    case DreamValue.DreamValueType.Float:
                        result += $"{new string('\t', indent)}{key} = {value.MustGetValueAsFloat()}\n";
                        break;
                    case DreamValue.DreamValueType.DreamResource: //TODO this should probably be implemented in SaveFile.Read instead
                        DreamResource dreamResource = value.MustGetValueAsDreamResource();
                        result += $"{new string('\t', indent)}{key} = \nfiledata(\"";
                        result += $"name={dreamResource.ResourcePath};";
                        result += $"ext={Path.GetExtension(dreamResource.ResourcePath)};";
                        result += $"length={dreamResource.ResourceData!.Length};";
                        result += $"crc32=0x00000000;"; //TODO crc32
                        result += $"encoding=base64\",{{\"{Convert.ToBase64String(dreamResource.ResourceData!)}\"}}";
                        result += ")\n";
                        break;
                    default:
                        throw new NotImplementedException($"Unhandled type {key} = {value.Stringify()} in ExportText()");
                }
            }
        }
        return result;
    }

    [DreamProc("Flush")]
    public static DreamValue NativeProc_Flush(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var savefile = (DreamObjectSavefile)src!;

        savefile.Flush();
        return DreamValue.Null;
    }
}
