using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Resources;
using DreamValueTypeFlag = OpenDreamRuntime.DreamValue.DreamValueTypeFlag;
using System.IO;
using System.Linq;
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
            savefile.CurrentPath = pathStr;
        }


        string result = ExportTextInternal(savefile);
        if(!file.IsNull){
            if(file.TryGetValueAsString(out var fileStr)) {
                File.WriteAllText(fileStr, result);
            } else if(file.TryGetValueAsDreamResource(out var fileResource)) {
                fileResource.Output(new DreamValue(result));
            } else {
                throw new ArgumentException($"Invalid file value {file}");
            }
        }
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
            savefile.CurrentPath = pathStr;
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
                    savefile.CurrentPath = string.Join("/", directoryStack.Reverse());
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
        var value = savefile.CurrentDir;
        var oldPath = savefile.CurrentPath;
        var key = savefile.CurrentPath.Split('/').Last();
        switch(value) {
            case DreamObjectSavefile.DreamPrimitive primitiveValue:
                if(primitiveValue.Value.IsNull)
                    result += $"{new string('\t', indent)}{key} = null\n";
                else switch(primitiveValue.Value.Type){
                    case DreamValue.DreamValueType.String:
                        result += $"{new string('\t', indent)}{key} = \"{primitiveValue.Value.MustGetValueAsString()}\"\n";
                        break;
                    case DreamValue.DreamValueType.Float:
                        result += $"{new string('\t', indent)}{key} = {primitiveValue.Value.MustGetValueAsFloat()}\n";
                        break;
                }
                break;
            case DreamObjectSavefile.DreamFileValue fileValue:
                result += $"{new string('\t', indent)}{key} = \nfiledata(\"";
                result += $"name={fileValue.Name};";
                result += $"ext={fileValue.Ext};";
                result += $"length={fileValue.Length};";
                result += $"crc32={fileValue.Crc32};"; //TODO crc32
                result += $"encoding=base64\",{{\"{fileValue.Data}\"}}";
                //result += $"encoding=base64\",{{\"{Convert.ToBase64String(fileValue.Data)}\"}}";
                result += ")\n";
                break;
            case DreamObjectSavefile.DreamObjectValue objectValue:
                throw new NotImplementedException($"ExportText() can't do objects yet TODO");
            case DreamObjectSavefile.DreamJsonValue jsonValue:
                result += $"{new string('\t', indent)}{key}\n";
                break;
            default:
                throw new NotImplementedException($"Unhandled type {key} = {value} in ExportText()");
        }

        foreach (string subkey in savefile.CurrentDir.Keys) {
            savefile.CurrentPath = subkey;
            result += ExportTextInternal(savefile, indent + 1);
            savefile.CurrentPath = "../";
        }
        savefile.CurrentPath = oldPath;


        return result;
    }

    [DreamProc("Flush")]
    public static DreamValue NativeProc_Flush(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var savefile = (DreamObjectSavefile)src!;

        savefile.Flush();
        return DreamValue.Null;
    }
}
