using System.Diagnostics.CodeAnalysis;
using System.IO;
using DMCompiler.Compiler;
using OpenDreamShared.Common;
using OpenDreamShared.Common.DM;
using OpenDreamShared.Common.Json;

namespace DMCompiler.DM.Expressions;

internal abstract class Constant(Location location) : DMExpression(location) {
    public sealed override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        constant = this;
        return true;
    }

    public abstract bool IsTruthy();
}

// null
internal sealed class Null(Location location) : Constant(location) {
    public override DMComplexValueType ValType => DMValueType.Null;

    public override void EmitPushValue(ExpressionContext ctx) {
        ctx.Proc.PushNull();
    }

    public override bool IsTruthy() => false;

    public override bool TryAsJsonRepresentation(DMCompiler compiler, out object? json) {
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

    public override void EmitPushValue(ExpressionContext ctx) {
        ctx.Proc.PushFloat(Value);
    }

    public override bool IsTruthy() => Value != 0;

    public override bool TryAsJsonRepresentation(DMCompiler compiler, out object? json) {
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

    public override void EmitPushValue(ExpressionContext ctx) {
        ctx.Proc.PushString(Value);
    }

    public override bool IsTruthy() => Value.Length != 0;

    public override bool TryAsJsonRepresentation(DMCompiler compiler, out object? json) {
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

    public Resource(DMCompiler compiler, Location location, string filePath) : base(location) {
        // Treat backslashes as forward slashes on Linux
        // Also remove "." and ".." from the directory path
        filePath = System.IO.Path.GetRelativePath(".", filePath.Replace('\\', '/'));

        var outputDir = System.IO.Path.GetDirectoryName(compiler.Settings.Files?[0]) ?? "/";
        if (string.IsNullOrEmpty(outputDir))
            outputDir = "./";

        string? finalFilePath = null;

        var fileName = System.IO.Path.GetFileName(filePath);
        var fileDir = System.IO.Path.GetDirectoryName(filePath) ?? string.Empty;

        // Search every defined FILE_DIR
        foreach (string resourceDir in compiler.ResourceDirectories) {
            var directory = FindDirectory(resourceDir == string.Empty ? "./" : resourceDir, fileDir);

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
                compiler.Emit(WarningCode.AmbiguousResourcePath, Location,
                    $"Resource {filePath} has multiple case-insensitive matches, using {_filePath}");
            }
        } else {
            compiler.Emit(WarningCode.ItemDoesntExist, Location, $"Cannot find file '{filePath}'");
            _filePath = filePath;
        }

        // Path operations give backslashes on Windows, so do this again
        // Compile-time resources always use forward slashes
        _filePath = _filePath.Replace('\\', '/');

        compiler.DMObjectTree.Resources.Add(_filePath);
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        ctx.Proc.PushResource(_filePath);
    }

    public override bool IsTruthy() => true;

    public override bool TryAsJsonRepresentation(DMCompiler compiler, out object? json) {
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

internal interface IConstantPath {
    public DreamPath? Path { get; }
}

/// <summary>
/// A reference to a type
/// <code>/a/b/c</code>
/// </summary>
internal class ConstantTypeReference(Location location, DMObject dmObject) : Constant(location), IConstantPath {
    public DMObject Value { get; } = dmObject;

    public override DreamPath? Path => Value.Path;
    public override DMComplexValueType ValType => Value.Path;

    public override void EmitPushValue(ExpressionContext ctx) {
        ctx.Proc.PushType(Value.Id);
    }

    public override string? GetNameof(ExpressionContext ctx) => Value.Path.LastElement;

    public override bool IsTruthy() => true;

    public override bool TryAsJsonRepresentation(DMCompiler compiler, out object? json) {
        json = new Dictionary<string, object> {
            { "type", JsonVariableType.Type },
            { "value", Value.Id }
        };

        return true;
    }
}

/// <summary>
/// A reference to a proc
/// <code>/datum/proc/foo</code>
/// </summary>
internal sealed class ConstantProcReference(Location location, DreamPath path, DMProc referencedProc) : Constant(location), IConstantPath {
    public DMProc Value { get; } = referencedProc;

    public override DreamPath? Path => path;

    public override void EmitPushValue(ExpressionContext ctx) {
        ctx.Proc.PushProc(Value.Id);
    }

    public override string GetNameof(ExpressionContext ctx) => Value.Name;

    public override bool IsTruthy() => true;

    public override bool TryAsJsonRepresentation(DMCompiler compiler, out object? json) {
        json = new Dictionary<string, object> {
            { "type", JsonVariableType.Proc },
            { "value", Value.Id }
        };

        return true;
    }
}

/// <summary>
/// A generic reference to all of a type's procs or verbs
/// <code>/datum/proc</code>
/// </summary>
internal sealed class ConstantProcStub(Location location, DMObject onObject, bool isVerb) : Constant(location), IConstantPath {
    private readonly string _str =
        $"{(onObject.Path == DreamPath.Root ? string.Empty : onObject.Path.PathString)}/{(isVerb ? "verb" : "proc")}";

    public override DreamPath? Path => onObject.Path.AddToPath(isVerb ? "verb" : "proc");

    public override void EmitPushValue(ExpressionContext ctx) {
        // /datum/proc and /datum/verb just compile down to strings lmao
        ctx.Proc.PushString(_str);
    }

    public override bool IsTruthy() => true;

    public override bool TryAsJsonRepresentation(DMCompiler compiler, out object? json) {
        json = _str;
        return true;
    }
}
