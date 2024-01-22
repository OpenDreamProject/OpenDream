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

        string oldPath = savefile.CurrentPath;
        if(!path.IsNull && path.TryGetValueAsString(out var pathStr)) { //invalid path values are just ignored in BYOND
            savefile.CurrentPath = pathStr;
        }

        string result = ExportTextInternal(savefile);

        savefile.CurrentPath = oldPath; //restore current directory after a query
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
            var indentCount = line.TakeWhile(char.IsWhiteSpace).Count();
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


    private static string ExportTextInternal(DreamObjectSavefile savefile, int indent = int.MinValue) {
        string result = "";
        var value = savefile.CurrentDir;
        var key = savefile.CurrentPath.Split('/').Last();
        if(indent == int.MinValue){
            if(!string.IsNullOrEmpty(key)) //if this is is the start and not root dir, use . = value syntax
                key = ".";
            indent = 0; //either way, set indent to 0 so we know we're not at the start anymore
        }
        switch(value) {
            case DreamObjectSavefile.SFDreamPrimitive primitiveValue:
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
            case DreamObjectSavefile.SFDreamFileValue fileValue:
                result += $"{new string('\t', indent)}{key} = \nfiledata(\"";
                result += $"name={fileValue.Name};";
                result += $"ext={fileValue.Ext};";
                result += $"length={fileValue.Length};";
                result += $"crc32={fileValue.Crc32};"; //TODO crc32
                result += $"encoding=base64\",{{\"{fileValue.Data}\"}}";
                //result += $"encoding=base64\",{{\"{Convert.ToBase64String(fileValue.Data)}\"}}";
                result += ")\n";
                break;
            case DreamObjectSavefile.SFDreamObjectPathValue objectValue:
                result += $"{new string('\t', indent)}{key} = object(\"{objectValue.Path}\")\n";
                break;
            case DreamObjectSavefile.SFDreamType typeValue:
                result += $"{new string('\t', indent)}{key} = {typeValue.TypePath}\n";
                break;
            case DreamObjectSavefile.SFDreamListValue listValue:
                result += $"{new string('\t', indent)}{key} = {ExportTextInternalListFormat(listValue)}\n";
                break;
            case DreamObjectSavefile.SFDreamDir jsonValue:
                if(indent==0){ //root dir has a minor difference in formatting
                    indent--;
                    result += "\n";
                } else
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
        return result;
    }

    private static string ExportTextInternalListFormat(DreamObjectSavefile.SFDreamJsonValue listEntry){
        switch(listEntry) {
            case DreamObjectSavefile.SFDreamPrimitive primitiveValue:
                if(primitiveValue.Value.IsNull)
                    return "null";
                else switch(primitiveValue.Value.Type){
                    case DreamValue.DreamValueType.String:
                        return $"\"{primitiveValue.Value.MustGetValueAsString()}\"";
                    case DreamValue.DreamValueType.Float:
                        return $"{primitiveValue.Value.MustGetValueAsFloat()}";
                }
                throw new NotImplementedException($"Unhandled list entry type {listEntry} in ExportTextInternalListFormat()");
            case DreamObjectSavefile.SFDreamObjectPathValue objectValue:
                return $"object(\"{objectValue.Path}\")";
            case DreamObjectSavefile.SFDreamType typeValue:
                return $"{typeValue.TypePath}";
            case DreamObjectSavefile.SFDreamListValue listValue:
                string result = "list(";

                    for(int i=0; i<listValue.AssocKeys.Count; i++){
                        if(listValue.AssocData != null && listValue.AssocData[i] != null)
                            result += ExportTextInternalListFormat(listValue.AssocKeys[i])+" = "+ExportTextInternalListFormat(listValue.AssocData[i]!);
                        else
                            result += ExportTextInternalListFormat(listValue.AssocKeys[i]);
                        result += ",";
                    }
                result = result.TrimEnd(',');
                result += ")";
                return result;
            default:
                throw new NotImplementedException($"Unhandled list entry type {listEntry} in ExportTextInternalListFormat()");
        }

    }

    [DreamProc("Flush")]
    public static DreamValue NativeProc_Flush(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var savefile = (DreamObjectSavefile)src!;

        savefile.Flush();
        return DreamValue.Null;
    }
}
