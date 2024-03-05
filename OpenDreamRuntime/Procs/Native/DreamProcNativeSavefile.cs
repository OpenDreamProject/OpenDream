using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
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

    private static string ExportTextInternal(DreamObjectSavefile savefile, int indent = int.MinValue) {
        string result = "";
        var value = savefile.CurrentDir;
        var key = savefile.CurrentPath.Split('/').Last();
        if(indent == int.MinValue){
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
                result += $"crc32=0x{fileValue.Crc32:x8};";
                result += $"encoding=base64\",{{\"\n{fileValue.Data}\n\"}}";
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
            case DreamObjectSavefile.SFDreamDir:
                if(key==".")
                    result += "\n";
                else
                    result += $"{new string('\t', indent)}{key}\n";
                break;
            default:
                throw new NotImplementedException($"Unhandled type {key} = {value} in ExportText()");
        }

        if(string.IsNullOrEmpty(key) || key==".")
            indent = -1; //don't indent the subdirs of directly accessed keys or root dir

        foreach (string subKey in savefile.CurrentDir.Keys) {
            savefile.CurrentPath = subKey;
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

    [DreamProc("ImportText")]
    [DreamProcParameter("path", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("source", Type = DreamValueTypeFlag.String | DreamValueTypeFlag.DreamResource)]
    public static DreamValue NativeProc_ImportText(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var savefile = (DreamObjectSavefile)src!;
        DreamValue path = bundle.GetArgument(0, "path");
        DreamValue source = bundle.GetArgument(1, "source");

        string oldPath = savefile.CurrentPath;
        if(!path.IsNull && path.TryGetValueAsString(out var pathStr)) { //invalid path values are just ignored in BYOND
            savefile.CurrentPath = pathStr;
        }

        int result = ImportTextInternal(savefile, source, bundle);

        savefile.CurrentPath = oldPath; //restore current directory after a query
        return new DreamValue(result);
    }

    private static int ImportTextInternal(DreamObjectSavefile savefile, DreamValue source, NativeProc.Bundle bundle) {
        if(source.IsNull) {
            return 0;
        }

        if(source.TryGetValueAsString(out var sourceStr)) {
            return ImportTextParseLine(sourceStr, savefile, bundle);
        } else if(source.TryGetValueAsDreamResource(out var fileResource)) {
            if(fileResource.ResourceData == null) {
                return 0;
            }

            string fileString = fileResource.ReadAsString()!;
            string[] fileStrings = fileString.Split("\n");
            bool returnedFalse = false;
            foreach(string fileLine in fileStrings) {
                int returnValue = ImportTextParseLine(fileLine, savefile, bundle);
                if(returnValue == 0) {
                    returnedFalse = true;
                }
            }
            if(returnedFalse) {
                return 0;
            } else {
                return 1;
            }
        }

        return 0;
    }

    private static int ImportTextParseLine(string lineToParse, DreamObjectSavefile savefile, NativeProc.Bundle bundle) {
        // ; is used to split individual savefile entries in ImportText()
        string[] savefileImports = lineToParse.Split(";");
        foreach(string savefileEntry in savefileImports) {
            // We cannot split by " = " or similar because we cannot guarantee there will be only one = or that there is consistent spacing
            string[] spaceSplit = savefileEntry.Split(" ");
            bool foundEquals = false;
            string foundValue = "";
            string foundIndex = "";
            for(int i = 0; i < spaceSplit.Length; i++) {
                if(i == 0) {
                    foundIndex = spaceSplit[i];
                    continue;
                }
                string individualWord = spaceSplit[i];
                if(individualWord == "=") {
                    // "entry = ="
                    if(foundEquals) {
                        bundle.DreamManager.WriteWorldLog($"failed to parse savefile text at line 0 (reading 'end of file'): expecting ;");
                    }
                    foundEquals = true;
                    continue;
                }
                // "entry = a b"
                if(spaceSplit[i - 1] != "=") {
                    bundle.DreamManager.WriteWorldLog($"failed to parse savefile text at line 0 (reading 'end of file'): expecting ;");
                } else {
                    foundValue = individualWord;
                }
            }

            // "entry ="
            if(foundEquals && (foundValue == "")) {
                bundle.DreamManager.WriteWorldLog($"failed to parse savefile text at line 0 (reading 'end of file'): expecting ;");
            }

            if(!foundEquals && (foundValue == "")) {
                //todo: sort out the difference between "key = null" and "key"
                savefile.SetSavefileValue(foundIndex, DreamValue.Null);
            }

            if((foundValue != "") && (foundIndex != "")) {
                bool canConvert = float.TryParse(foundValue, out float possibleNumber);
                // DM implicitly converts values to numbers if possible
                if (canConvert) {
                    DreamValue value = new(possibleNumber);
                    savefile.SetSavefileValue(foundIndex, value);
                } else {
                    DreamValue value = new(foundValue);
                    savefile.SetSavefileValue(foundIndex, value);
                }
            }

        }
        return 1;
    }

    [DreamProc("Flush")]
    public static DreamValue NativeProc_Flush(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var savefile = (DreamObjectSavefile)src!;

        savefile.Flush();
        return DreamValue.Null;
    }
}
