using OpenDreamShared.Compiler;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace DMCompiler.DM.Expressions {
    abstract class Constant : DMExpression {
        public Constant(Location location) : base(location) { }

        public sealed override bool TryAsConstant(out Constant constant) {
            constant = this;
            return true;
        }

        public abstract bool IsTruthy();

        #region Unary Operations
        public Constant Not() {
            return new Number(Location, IsTruthy() ? 0 : 1);
        }

        public virtual Constant Negate() {
            throw new CompileErrorException(Location, $"const operation \"-{this}\" is invalid");
        }

        public virtual Constant BinaryNot() {
            throw new CompileErrorException(Location, $"const operation \"~{this}\" is invalid");
        }
        #endregion

        #region Binary Operations

        public virtual Constant Add(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} + {rhs}\" is invalid");
        }

        public virtual Constant Subtract(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} - {rhs}\" is invalid");
        }

        public virtual Constant Multiply(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} * {rhs}\" is invalid");
        }

        public virtual Constant Divide(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} / {rhs}\" is invalid");
        }

        public virtual Constant Modulo(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} % {rhs}\" is invalid");
        }

        public virtual Constant ModuloModulo(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} % {rhs}\" is invalid");
        }

        public virtual Constant Power(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} ** {rhs}\" is invalid");
        }

        public virtual Constant LeftShift(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} << {rhs}\" is invalid");
        }

        public virtual Constant RightShift(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} >> {rhs}\" is invalid");
        }

        public virtual Constant BinaryAnd(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} & {rhs}\" is invalid");
        }

        public virtual Constant BinaryXor(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} ^ {rhs}\" is invalid");
        }

        public virtual Constant BinaryOr(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} | {rhs}\" is invalid");
        }

        public virtual Constant GreaterThan(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} > {rhs}\" is invalid");
        }

        public virtual Constant GreaterThanOrEqual(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} >= {rhs}\" is invalid");
        }

        public virtual Constant LessThan(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} < {rhs}\" is invalid");
        }

        public virtual Constant LessThanOrEqual(Constant rhs) {
            throw new CompileErrorException(Location, $"const operation \"{this} <= {rhs}\" is invalid");
        }

        public virtual Constant Sin() {
            throw new CompileErrorException(Location, $"const operation \"sin({this})\" is invalid");
        }

        public virtual Constant Cos() {
            throw new CompileErrorException(Location, $"const operation \"cos({this})\" is invalid");
        }

        public virtual Constant Tan() {
            throw new CompileErrorException(Location, $"const operation \"tan({this})\" is invalid");
        }

        public virtual Constant Arcsin() {
            throw new CompileErrorException(Location, $"const operation \"arcsin({this})\" is invalid");
        }

        public virtual Constant Arccos() {
            throw new CompileErrorException(Location, $"const operation \"arccos({this})\" is invalid");
        }

        public virtual Constant Arctan() {
            throw new CompileErrorException(Location, $"const operation \"arctan({this})\" is invalid");
        }

        public virtual Constant Arctan2(Constant yConst) {
            throw new CompileErrorException(Location, $"const operation \"arctan({this}, {yConst})\" is invalid");
        }

        public virtual Constant Sqrt() {
            throw new CompileErrorException(Location, $"const operation \"sqrt({this})\" is invalid");
        }

        public virtual Constant Log(Constant? baseVal) {
            if (baseVal == null) {
                throw new CompileErrorException(Location, $"const operation \"log({this})\" is invalid");
            }
            throw new CompileErrorException(Location, $"const operation \"log({baseVal}, {this})\" is invalid");
        }

        public virtual Constant Abs() {
            throw new CompileErrorException(Location, $"const operation \"abs({this})\" is invalid");
        }
        #endregion
    }

    // null
    sealed class Null : Constant {
        public Null(Location location) : base(location) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushNull();
        }

        public override bool IsTruthy() => false;

        public override bool TryAsJsonRepresentation(out object? json) {
            json = null;
            return true;
        }

        public override Constant GreaterThan(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.GreaterThan(rhs);
            }
            return new Number(Location, (0 > rhsNum.Value) ? 1 : 0);
        }

        public override Constant GreaterThanOrEqual(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.GreaterThanOrEqual(rhs);
            }
            return new Number(Location, (0 >= rhsNum.Value) ? 1 : 0);
        }

        public override Constant LessThan(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.LessThan(rhs);
            }
            return new Number(Location, (0 < rhsNum.Value) ? 1 : 0);
        }

        public override Constant LessThanOrEqual(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.LessThanOrEqual(rhs);
            }
            return new Number(Location, (0 <= rhsNum.Value) ? 1 : 0);
        }
    }

    // 4.0, -4.0
    sealed class Number : Constant {
        public float Value { get; }

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

        public override Constant Negate() {
            return new Number(Location, -Value);
        }

        public override Constant BinaryNot() {
            return new Number(Location, ~(int)Value);
        }

        public override Constant Add(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, Value + rhsNum.Value);
        }

        public override Constant Subtract(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, Value - rhsNum.Value);
        }

        public override Constant Multiply(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, Value * rhsNum.Value);
        }

        public override Constant Divide(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, Value / rhsNum.Value);
        }

        public override Constant Modulo(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, Value % rhsNum.Value);
        }

        public override Constant ModuloModulo(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.ModuloModulo(rhs);
            }

            // BYOND docs say that A %% B is equivalent to B * fract(A/B)
            var fraction = Value / rhsNum.Value;
            fraction -= MathF.Truncate(fraction);
            return new Number(Location, fraction * rhsNum.Value);
        }

        public override Constant Power(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, MathF.Pow(Value, rhsNum.Value));
        }

        public override Constant LeftShift(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, ((int)Value) << ((int)rhsNum.Value));
        }

        public override Constant RightShift(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, ((int)Value) >> ((int)rhsNum.Value));
        }


        public override Constant BinaryAnd(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, ((int)Value) & ((int)rhsNum.Value));
        }


        public override Constant BinaryXor(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, ((int)Value) ^ ((int)rhsNum.Value));
        }


        public override Constant BinaryOr(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.Add(rhs);
            }

            return new Number(Location, ((int)Value) | ((int)rhsNum.Value));
        }

        public override Constant GreaterThan(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.GreaterThan(rhs);
            }
            return new Number(Location, (Value > rhsNum.Value) ? 1 : 0);
        }

        public override Constant GreaterThanOrEqual(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.GreaterThanOrEqual(rhs);
            }
            return new Number(Location, (Value >= rhsNum.Value) ? 1 : 0);
        }

        public override Constant LessThan(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.LessThan(rhs);
            }
            return new Number(Location, (Value < rhsNum.Value) ? 1 : 0);
        }

        public override Constant LessThanOrEqual(Constant rhs) {
            if (rhs is not Number rhsNum) {
                return base.LessThanOrEqual(rhs);
            }
            return new Number(Location, (Value <= rhsNum.Value) ? 1 : 0);
        }

        public override Constant Sin() {
            return new Number(Location, MathF.Sin(Value / 180 * MathF.PI));
        }

        public override Constant Cos() {
            return new Number(Location, MathF.Cos(Value / 180 * MathF.PI));
        }

        public override Constant Tan() {
            return new Number(Location, MathF.Tan(Value / 180 * MathF.PI));
        }

        public override Constant Arcsin() {
            if (Value < -1 || Value > 1) {
                throw new CompileErrorException(Location, $"const operation \"arcsin({this})\" is invalid (out of range)");
            }
            return new Number(Location, MathF.Asin(Value) / MathF.PI * 180);
        }

        public override Constant Arccos() {
            if (Value < -1 || Value > 1) {
                throw new CompileErrorException(Location, $"const operation \"arccos({this})\" is invalid (out of range)");
            }
            return new Number(Location, MathF.Acos(Value) / MathF.PI * 180);
        }

        public override Constant Arctan() {
            return new Number(Location, MathF.Atan(Value) / MathF.PI * 180);
        }

        public override Constant Arctan2(Constant yConst) {
            if (yConst is not Number yNum) {
                throw new CompileErrorException(Location, $"const operation \"arctan2({this}, {yConst})\" is invalid");
            }
            return new Number(Location, MathF.Atan2(yNum.Value, Value) / MathF.PI * 180);
        }

        public override Constant Sqrt() {
            if (Value < 0) {
                throw new CompileErrorException(Location, $"const operation \"sqrt({this})\" is invalid (negative)");
            }
            return new Number(Location, MathF.Sqrt(Value));
        }

        public override Constant Log(Constant? baseVal) {
            if (Value <= 0) {
                throw new CompileErrorException(Location, $"const operation \"log({this})\" is invalid (non-positive)");
            }
            if (baseVal == null) {
                return new Number(Location, MathF.Log(Value));
            }
            if (baseVal is not Number baseNum) {
                throw new CompileErrorException(Location, $"const operation \"log({this}, {baseVal})\" is invalid");
            }
            if (baseNum.Value <= 0) {
                throw new CompileErrorException(Location, $"const operation \"log({this}, {baseVal})\" is invalid (non-positive base)");
            }
            return new Number(Location, MathF.Log(Value, baseNum.Value));
        }

        public override Constant Abs() {
            return new Number(Location, MathF.Abs(Value));
        }
    }

    // "abc"
    sealed class String : Constant {
        public string Value { get; }

        public String(Location location, string value) : base(location) {
            Value = value;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            proc.PushString(Value);
        }

        public override bool IsTruthy() => Value.Length != 0;

        public override bool TryAsJsonRepresentation(out object? json) {
            json = Value;
            return true;
        }

        public override Constant Add(Constant rhs) {
            if (rhs is not String rhsString) {
                return base.Add(rhs);
            }

            return new String(Location, Value + rhsString.Value);
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

        public Resource(Location location, string filePath) : base(location) {
            // Treat backslashes as forward slashes on Linux
            filePath = filePath.Replace('\\', '/');

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
    sealed class Path : Constant {
        public DreamPath Value { get; }

        /// <summary>
        /// The DMObject this expression resides in. Used for path searches.
        /// </summary>
        private readonly DMObject _dmObject;

        private enum PathType {
            TypeReference,
            ProcReference,
            ProcStub,
            VerbStub
        }

        public Path(Location location, DMObject dmObject, DreamPath value) : base(location) {
            Value = value;
            _dmObject = dmObject;
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

        public override string GetNameof(DMObject dmObject, DMProc proc) {
            return Value.LastElement;
        }

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

        private bool TryResolvePath([NotNullWhen(true)] out (PathType Type, int Id)? pathInfo) {
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
}
