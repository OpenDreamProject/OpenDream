using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using DreamValueTypeFlag = OpenDreamRuntime.DreamValue.DreamValueTypeFlag;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenDreamRuntime.Procs.Native;

internal static class DreamProcNativeSavefile {
    [DreamProc("ExportText")]
    [DreamProcParameter("path", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("file", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamResource)]
    public static DreamValue NativeProc_ExportText(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var savefile = (DreamObjectSavefile)src!;
        var path = bundle.GetArgument(0, "path");
        var file = bundle.GetArgument(1, "file");

        string oldPath = savefile.CurrentPath;
        if (!path.IsNull && path.TryGetValueAsString(out var pathStr)) { //invalid path values are just ignored in BYOND
            savefile.CurrentPath = pathStr;
        }

        var result = new StringBuilder();
        ExportTextInternal(savefile, result);

        savefile.CurrentPath = oldPath; //restore current directory after a query
        if (!file.IsNull) {
            if (file.TryGetValueAsString(out var fileStr)) {
                File.WriteAllText(fileStr, result.ToString());
            } else if (file.TryGetValueAsDreamResource(out var fileResource)) {
                fileResource.Output(new DreamValue(result.ToString()));
            } else {
                throw new ArgumentException($"Invalid file value {file}");
            }
        }

        return new DreamValue(result.ToString());
    }

    private static void ExportTextInternal(DreamObjectSavefile savefile, StringBuilder result, int indent = int.MinValue) {
        var value = savefile.CurrentDir;
        var key = savefile.CurrentPath.Split('/').Last();
        if (indent == int.MinValue) {
            key = ".";
            indent = 0; //either way, set indent to 0 so we know we're not at the start anymore
        }

        switch (value) {
            case DreamObjectSavefile.SfDreamPrimitive primitiveValue:
                if (primitiveValue.Value.IsNull) {
                    result.Append($"{new string('\t', indent)}{key} = null\n");
                } else switch (primitiveValue.Value.Type) {
                    case DreamValue.DreamValueType.String:
                        result.Append($"{new string('\t', indent)}{key} = \"{primitiveValue.Value.MustGetValueAsString()}\"\n");
                        break;
                    case DreamValue.DreamValueType.Float:
                        result.Append($"{new string('\t', indent)}{key} = {primitiveValue.Value.MustGetValueAsFloat()}\n");
                        break;
                }

                break;
            case DreamObjectSavefile.SfDreamFileValue fileValue:
                result.Append($"{new string('\t', indent)}{key} = \nfiledata(\"");
                result.Append($"name={fileValue.Name};");
                result.Append($"ext={fileValue.Ext};");
                result.Append($"length={fileValue.Length};");
                result.Append($"crc32=0x{fileValue.Crc32:x8};");
                result.Append($"encoding=base64\",{{\"\n{fileValue.Data}\n\"}}");
                result.Append(")\n");
                break;
            case DreamObjectSavefile.SfDreamObjectPathValue objectValue:
                result.Append($"{new string('\t', indent)}{key} = object(\"{objectValue.Path}\")\n");
                break;
            case DreamObjectSavefile.SfDreamType typeValue:
                result.Append($"{new string('\t', indent)}{key} = {typeValue.TypePath}\n");
                break;
            case DreamObjectSavefile.SfDreamListValue listValue:
                result.Append($"{new string('\t', indent)}{key} = {ExportTextInternalListFormat(listValue)}\n");
                break;
            case DreamObjectSavefile.SfDreamDir:
                if (key == ".")
                    result.Append("\n");
                else
                    result.Append($"{new string('\t', indent)}{key}\n");
                break;
            default:
                throw new NotImplementedException($"Unhandled type {key} = {value} in ExportText()");
        }

        if (string.IsNullOrEmpty(key) || key == ".")
            indent = -1; //don't indent the subdirs of directly accessed keys or root dir

        foreach (string subKey in savefile.CurrentDir.Keys) {
            savefile.CurrentPath = subKey;
            ExportTextInternal(savefile, result, indent + 1);
            savefile.CurrentPath = "../";
        }
    }

    private static string ExportTextInternalListFormat(DreamObjectSavefile.SfDreamJsonValue listEntry) {
        switch (listEntry) {
            case DreamObjectSavefile.SfDreamPrimitive primitiveValue:
                if (primitiveValue.Value.IsNull)
                    return "null";

                switch (primitiveValue.Value.Type) {
                    case DreamValue.DreamValueType.String:
                        return $"\"{primitiveValue.Value.MustGetValueAsString()}\"";
                    case DreamValue.DreamValueType.Float:
                        return $"{primitiveValue.Value.MustGetValueAsFloat()}";
                }

                throw new NotImplementedException($"Unhandled list entry type {listEntry} in ExportTextInternalListFormat()");
            case DreamObjectSavefile.SfDreamObjectPathValue objectValue:
                return $"object(\"{objectValue.Path}\")";
            case DreamObjectSavefile.SfDreamType typeValue:
                return $"{typeValue.TypePath}";
            case DreamObjectSavefile.SfDreamListValue listValue:
                string result = "list(";

                for (int i=0; i<listValue.AssocKeys.Count; i++) {
                    if (listValue.AssocData != null && listValue.AssocData[i] != null)
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
