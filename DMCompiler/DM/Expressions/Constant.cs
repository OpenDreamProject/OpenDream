using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using DMCompiler.Compiler;
using DMCompiler.Json;

namespace DMCompiler.DM.Expressions;

internal abstract class Constant(Location location) : DMExpression(location) {
    public sealed override bool TryAsConstant(out Constant constant) {
        constant = this;
        return true;
    }

    public abstract bool IsTruthy();
}

// null
internal sealed class Null(Location location) : Constant(location) {
    public override DMComplexValueType ValType => DMValueType.Null;

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        proc.PushNull();
    }

    public override bool IsTruthy() => false;

    public override bool TryAsJsonRepresentation(out object? json) {
        json = null;
        return true;
    }
}

// 4.0, -4.0
internal sealed class Number : Constant {
    public float Value { get; }

    public override DMComplexValueType ValType => DMValueType.Num;

    public Number(Location location, int value) : base(location) {
        Value = value;
    }

    public Number(Location location, float value) : base(location) {
        Value = value;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        proc.PushFloat(Value);
    }

    public override bool IsTruthy() => Value != 0;

    public override bool TryAsJsonRepresentation(out object? json) {
        // Positive/Negative infinity cannot be represented in JSON and need a special value
        if (float.IsPositiveInfinity(Value)) {
            json = new Dictionary<string, JsonVariableType>() {
                {"type", JsonVariableType.PositiveInfinity}
            };
        } else if (float.IsNegativeInfinity(Value)) {
            json = new Dictionary<string, JsonVariableType>() {
                {"type", JsonVariableType.NegativeInfinity}
            };
        } else {
            json = Value;
        }

        return true;
    }
}

// "abc"
internal sealed class String(Location location, string value) : Constant(location) {
    public string Value { get; } = value;

    public override DMComplexValueType ValType => DMValueType.Text;

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        proc.PushString(Value);
    }

    public override bool IsTruthy() => Value.Length != 0;

    public override bool TryAsJsonRepresentation(out object? json) {
        json = Value;
        return true;
    }
}

// '[resource_path]'
// Where resource_path is one of:
//   - path relative to project root (.dme file location)
//   - path relative to current .dm source file location
//
// Note: built .json file depends on resource files, so they should be moving with it
// TODO: cache resources to a single .rsc file, as BYOND does
internal sealed class Resource : Constant {
    private static readonly EnumerationOptions SearchOptions = new() {
        MatchCasing = MatchCasing.CaseInsensitive
    };

    private readonly string _filePath;
    private bool _isAmbiguous;
    public override DMComplexValueType ValType { get; }

    public Resource(Location location, string filePath) : base(location) {
        // Treat backslashes as forward slashes on Linux
        // Also remove "." and ".." from the directory path
        filePath = System.IO.Path.GetRelativePath(".", filePath.Replace('\\', '/'));

        var outputDir = System.IO.Path.GetDirectoryName(DMCompiler.Settings.Files?[0]) ?? "/";
        if (string.IsNullOrEmpty(outputDir))
            outputDir = "./";

        string? finalFilePath = null;

        var fileName = System.IO.Path.GetFileName(filePath);
        var fileDir = System.IO.Path.GetDirectoryName(filePath) ?? string.Empty;

        // Search every defined FILE_DIR
        foreach (string resourceDir in DMCompiler.ResourceDirectories) {
            var directory = FindDirectory(resourceDir, fileDir);

            if (directory != null) {
                // Perform a case-insensitive search for the file
                finalFilePath = FindFile(directory, fileName);

                if (finalFilePath != null)
                    break;
            }
        }

        // Search relative to the source file if it wasn't in one of the FILE_DIRs
        if (finalFilePath == null) {
            var sourceDir = System.IO.Path.Combine(outputDir, System.IO.Path.GetDirectoryName(Location.SourceFile) ?? string.Empty);
            var directory = FindDirectory(sourceDir, fileDir);

            if (directory != null)
                finalFilePath = FindFile(directory, fileName);
        }

        if (finalFilePath != null) {
            _filePath = System.IO.Path.GetRelativePath(outputDir, finalFilePath);

            if (_isAmbiguous) {
                DMCompiler.Emit(WarningCode.AmbiguousResourcePath, Location,
                    $"Resource {filePath} has multiple case-insensitive matches, using {_filePath}");
            }
        } else {
            DMCompiler.Emit(WarningCode.ItemDoesntExist, Location, $"Cannot find file '{filePath}'");
            _filePath = filePath;
        }

        // Path operations give backslashes on Windows, so do this again
        // Compile-time resources always use forward slashes
        _filePath = _filePath.Replace('\\', '/');

        DMObjectTree.Resources.Add(_filePath);

        ValType = System.IO.Path.GetExtension(fileName) switch {
            ".dmi" => DMValueType.Icon,
            ".png" => DMValueType.Icon,
            ".bmp" => DMValueType.Icon,
            ".gif" => DMValueType.Icon,
            ".ogg" => DMValueType.Sound,
            ".wav" => DMValueType.Sound,
            ".mid" => DMValueType.Sound,
            _ => DMValueType.File
        };
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        proc.PushResource(_filePath);
    }

    public override bool IsTruthy() => true;

    public override bool TryAsJsonRepresentation(out object? json) {
        json = new Dictionary<string, object>() {
            { "type", JsonVariableType.Resource },
            { "resourcePath", _filePath }
        };

        return true;
    }

    /// <summary>
    /// Performs a recursive case-insensitive for a directory.<br/>
    /// Marks the resource as ambiguous if multiple are found.
    /// </summary>
    /// <param name="directory">Directory to search in (case-sensitive)</param>
    /// <param name="searching">Directory to search for (case-insensitive)</param>
    /// <returns>The found directory, null if none</returns>
    private string? FindDirectory(string directory, string searching) {
        var searchingDirectories = searching.Split('/', StringSplitOptions.RemoveEmptyEntries);

        foreach (var searchingDirectory in searchingDirectories) {
            string[] directories = Directory.GetDirectories(directory, searchingDirectory, SearchOptions);

            if (directories.Length == 0)
                return null;
            else if (directories.Length > 1)
                _isAmbiguous = true;

            directory = directories[0];
        }

        return directory;
    }

    /// <summary>
    /// Performs a case-insensitive search for a file inside a directory.<br/>
    /// Marks the resource as ambiguous if multiple are found.
    /// </summary>
    /// <param name="directory">Directory to search in (case-sensitive)</param>
    /// <param name="searching">File to search for (case-insensitive)</param>
    /// <returns>The found file, null if none</returns>
    private string? FindFile(string directory, string searching) {
        var files = Directory.GetFiles(directory, searching, SearchOptions);

        // GetFiles() can't find "..ogg" on Linux for some reason, so try a direct check for the file
        if (files.Length == 0) {
            string combined = System.IO.Path.Combine(directory, searching);

            return File.Exists(combined) ? combined : null;
        } else if (files.Length > 1) {
            _isAmbiguous = true;
        }

        return files[0];
    }
}

// /a/b/c
// no, this can't be called "Path" because of CS0542
internal sealed class ConstantPath(Location location, DMObject dmObject, DreamPath value) : Constant(location) {
    public DreamPath Value { get; } = value;

    /// <summary>
    /// The DMObject this expression resides in. Used for path searches.
    /// </summary>
    private readonly DMObject _dmObject = dmObject;

    public override DreamPath? Path => Value;
    public override DMComplexValueType ValType => new DMComplexValueType(DMValueType.Path, Value);

    public enum PathType {
        TypeReference,
        ProcReference,
        ProcStub,
        VerbStub
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        if (!TryResolvePath(out var pathInfo)) {
            proc.PushNull();
            return;
        }

        switch (pathInfo.Value.Type) {
            case PathType.TypeReference:
                proc.PushType(pathInfo.Value.Id);
                break;
            case PathType.ProcReference:
                proc.PushProc(pathInfo.Value.Id);
                break;
            case PathType.ProcStub:
            case PathType.VerbStub:
                var type = DMObjectTree.AllObjects[pathInfo.Value.Id].Path.PathString;

                // /datum/proc and /datum/verb just compile down to strings lmao
                proc.PushString($"{type}/{(pathInfo.Value.Type == PathType.ProcStub ? "proc" : "verb")}");
                break;
            default:
                DMCompiler.ForcedError(Location, $"Invalid PathType {pathInfo.Value.Type}");
                break;
        }
    }

    public override string? GetNameof(DMObject dmObject) => Value.LastElement;

    public override bool IsTruthy() => true;

    public override bool TryAsJsonRepresentation(out object? json) {
        if (!TryResolvePath(out var pathInfo)) {
            json = null;
            return false;
        }

        if (pathInfo.Value.Type is PathType.ProcStub or PathType.VerbStub) {
            var type = DMObjectTree.AllObjects[pathInfo.Value.Id].Path.PathString;

            json = $"{type}/{(pathInfo.Value.Type == PathType.ProcStub ? "proc" : "verb")}";
            return true;
        }

        JsonVariableType jsonType = pathInfo.Value.Type switch {
            PathType.TypeReference => JsonVariableType.Type,
            PathType.ProcReference => JsonVariableType.Proc,
            _ => throw new UnreachableException()
        };

        json = new Dictionary<string, object>() {
            { "type", jsonType },
            { "value", pathInfo.Value.Id }
        };

        return true;
    }

    public bool TryResolvePath([NotNullWhen(true)] out (PathType Type, int Id)? pathInfo) {
        DreamPath path = Value;

        // An upward search with no left-hand side
        if (Value.Type == DreamPath.PathType.UpwardSearch) {
            DreamPath? foundPath = DMObjectTree.UpwardSearch(_dmObject.Path, path);
            if (foundPath == null) {
                DMCompiler.Emit(WarningCode.ItemDoesntExist, Location, $"Could not find path {path}");

                pathInfo = null;
                return false;
            }

            path = foundPath.Value;
        }

        // /datum/proc and /datum/verb
        if (Value.LastElement is "proc" or "verb") {
            DreamPath typePath = Value.FromElements(0, -2);
            if (!DMObjectTree.TryGetTypeId(typePath, out var ownerId)) {
                DMCompiler.Emit(WarningCode.ItemDoesntExist, Location, $"Type {typePath} does not exist");

                pathInfo = null;
                return false;
            }

            pathInfo = Value.LastElement switch {
                "proc" => (PathType.ProcStub, ownerId),
                "verb" => (PathType.VerbStub, ownerId),
                _ => throw new InvalidOperationException($"Last element of {Value} is not \"proc\" or \"verb\"")
            };
            return true;
        }

        // /datum/proc/foo
        int procIndex = path.FindElement("proc");
        if (procIndex == -1) procIndex = path.FindElement("verb");
        if (procIndex != -1) {
            DreamPath withoutProcElement = path.RemoveElement(procIndex);
            DreamPath ownerPath = withoutProcElement.FromElements(0, -2);
            DMObject owner = DMObjectTree.GetDMObject(ownerPath, createIfNonexistent: false);
            string procName = path.LastElement;

            int? procId;
            if (owner == DMObjectTree.Root && DMObjectTree.TryGetGlobalProc(procName, out var globalProc)) {
                procId = globalProc.Id;
            } else {
                var procs = owner.GetProcs(procName);

                procId = procs?[^1];
            }

            if (procId == null) {
                DMCompiler.Emit(WarningCode.ItemDoesntExist, Location,
                    $"Type {ownerPath} does not have a proc named {procName}");

                pathInfo = null;
                return false;
            }

            pathInfo = (PathType.ProcReference, procId.Value);
            return true;
        }

        // Any other path
        if (DMObjectTree.TryGetTypeId(Value, out var typeId)) {
            pathInfo = (PathType.TypeReference, typeId);
            return true;
        } else {
            DMCompiler.Emit(WarningCode.ItemDoesntExist, Location, $"Type {Value} does not exist");

            pathInfo = null;
            return false;
        }
    }
}

// TODO: Use this instead of ConstantPath for procs
/// <summary>
/// A reference to a proc
/// </summary>
internal sealed class ConstantProcReference(Location location, DMProc referencedProc) : Constant(location) {
    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        proc.PushProc(referencedProc.Id);
    }

    public override string GetNameof(DMObject dmObject) => referencedProc.Name;

    public override bool IsTruthy() => true;

    public override bool TryAsJsonRepresentation(out object? json) {
        json = new Dictionary<string, object> {
            { "type", JsonVariableType.Proc },
            { "value", referencedProc.Id }
        };

        return true;
    }
}
