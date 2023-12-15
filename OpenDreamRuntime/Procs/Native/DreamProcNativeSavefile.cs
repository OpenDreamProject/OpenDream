using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Resources;
using DreamValueTypeFlag = OpenDreamRuntime.DreamValue.DreamValueTypeFlag;
using System.IO;
using System.Linq;

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

    [DreamProc("ImportText")]
    [DreamProcParameter("path", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("source", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamResource)]
    public static DreamValue NativeProc_ImportText(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {

        var savefile = (DreamObjectSavefile)src!;
        DreamValue path = bundle.GetArgument(0, "path");
        DreamValue source = bundle.GetArgument(1, "source");

        if(!path.IsNull && path.TryGetValueAsString(out var pathStr)) { //invalid path values are just ignored in BYOND
            savefile.ChangeDirectory(pathStr);
        }
        //if source is a file, read text from file and parse
        //else if source is a string, parse string
        //savefile.OperatorOutput(new DreamValue(source));
        string sourceStr = "";
        if (source.TryGetValueAsDreamResource(out var sourceResource)) {
            sourceStr = sourceResource.ReadAsString() ?? "";
        } else if (source.TryGetValueAsString(out var sourceString)) {
            sourceStr = sourceString;
        } else {
            throw new ArgumentException($"Invalid source value {source}");
        }

        var lines = sourceStr.Split('\n');
        var directoryStack = new Stack<string>();
        foreach (var line in lines) {
            var indentCount = line.TakeWhile(Char.IsWhiteSpace).Count();
            while (directoryStack.Count > indentCount) {
                directoryStack.Pop();
            }
            var keyValue = line.Trim().Split(new[] { " = " }, StringSplitOptions.None);
            if (keyValue.Length == 2) {
                var key = keyValue[0].Trim();
                var value = keyValue[1].Trim();
                if (value.StartsWith("object(")) {
                    directoryStack.Push(key);
                    savefile.ChangeDirectory(string.Join("/", directoryStack.Reverse()));
                } else {
                    savefile.OperatorIndexAssign(new DreamValue(key), new DreamValue(value));
                }
            } else {
                throw new ArgumentException($"Invalid line {line}");
            }
        }

        return DreamValue.Null;
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
