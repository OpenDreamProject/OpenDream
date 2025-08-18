using System.Runtime.CompilerServices;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;
using DMCompiler.DM;

namespace DMCompiler.Optimizer;

/*
 * Provides a wrapper about BinaryWriter that stores information about the bytecode
 * for optimization and debugging.
 */
internal class AnnotatedByteCodeWriter(DMCompiler compiler) {
    private readonly List<IAnnotatedBytecode>
        _annotatedBytecode = new(250); // 1/6th of max size for bytecode in tgstation

    private readonly List<(long Position, string LabelName)> _unresolvedLabelsInAnnotatedBytecode = new();
    private int _currentStackSize;
    private Location _location;
    private int _maxStackSize;
    private bool _negativeStackSizeError;
    private int _requiredArgIdx;
    private OpcodeMetadata? _currentMetadata;
    private Dictionary<string, long> _labels = new();

    public long Position => _annotatedBytecode.Count;

    public List<IAnnotatedBytecode> GetAnnotatedBytecode() {
        return _annotatedBytecode;
    }

    /// <summary>
    /// Writes an opcode to the stream
    /// </summary>
    /// <param name="opcode">The opcode to write</param>
    /// <param name="location">The location of the opcode in the source code</param>
    public void WriteOpcode(DreamProcOpcode opcode, Location location) {
        _location = location;
        if (_currentMetadata is not null) {
            if (_requiredArgIdx < _currentMetadata.Value.RequiredArgs.Count - 1) {
                compiler.ForcedError(location, "Expected argument");
            } else if (_requiredArgIdx > _currentMetadata.Value.RequiredArgs.Count) {
                compiler.ForcedError(location, "Unexpected argument count. Opcode arguments have likely been mishandled internally");
            }
        }

        var metadata = OpcodeMetadataCache.GetMetadata(opcode);
        _requiredArgIdx = 0;
        _currentMetadata = metadata;

        // Goal here is to maintain correspondence between the raw bytecode and the annotated bytecode such that
        // the annotated bytecode can be used to generate the raw bytecode again.
        _annotatedBytecode.Add(new AnnotatedBytecodeInstruction(opcode, metadata.StackDelta, location));

        ResizeStack(metadata.StackDelta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateArgument(Location location, OpcodeArgType argType) {
        if (_currentMetadata!.Value.RequiredArgs.Count == 0 || _requiredArgIdx >= _currentMetadata.Value.RequiredArgs.Count || _currentMetadata.Value.RequiredArgs[_requiredArgIdx] != argType) {
            compiler.ForcedError(location, $"Expected {argType.ToString()} argument");
        }

        _requiredArgIdx += 1;
    }

    /// <summary>
    /// Writes a float to the stream
    /// </summary>
    /// <param name="val">The integer to write</param>
    /// <param name="location">The location of the integer in the source code</param>
    public void WriteFloat(float val, Location location) {
        _location = location;
        ValidateArgument(location, OpcodeArgType.Float);
        _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodeFloat(val, location));
    }

    /// <summary>
    /// Writes argument classification to the stream
    /// </summary>
    /// <param name="argType">The argument type to write</param>
    /// <param name="location">The location of the integer in the source code</param>
    public void WriteArgumentType(DMCallArgumentsType argType, Location location) {
        _location = location;
        ValidateArgument(location, OpcodeArgType.ArgType);
        _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodeArgumentType(argType, location));
    }

    /// <summary>
    /// Write a stack delta to the stream
    /// </summary>
    /// <param name="delta">The stack delta to write</param>
    /// <param name="location">The location of the integer in the source code</param>
    public void WriteStackDelta(int delta, Location location) {
        _location = location;
        ValidateArgument(location, OpcodeArgType.StackDelta);
        _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodeStackDelta(delta, location));
    }

    /// <summary>
    /// Write a type to the stream
    /// </summary>
    /// <param name="type">The type to write</param>
    /// <param name="location">The location of the type in the source code</param>
    public void WriteType(DMValueType type, Location location) {
        ValidateArgument(location, OpcodeArgType.TypeId);

        _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodeType(type, location));
    }

    /// <summary>
    /// Writes a string to the stream and stores it in the string table
    /// </summary>
    /// <param name="value">The string to write</param>
    /// <param name="location">The location of the string in the source code</param>
    public void WriteString(string value, Location location) {
        _location = location;
        ValidateArgument(location, OpcodeArgType.String);
        int stringId = compiler.DMObjectTree.AddString(value);
        _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodeString(stringId, location));
    }

    /// <summary>
    /// Write a filter. Filters are stored as reference IDs in the raw bytecode, which refer
    /// to a string in the string table containing the datum path of the filter.
    /// </summary>
    /// <param name="filterTypeId">The type ID of the filter</param>
    /// <param name="filterPath">The datum path of the filter</param>
    /// <param name="location">The location of the filter in the source code</param>
    ///
    public void WriteFilterId(int filterTypeId, DreamPath filterPath, Location location) {
        _location = location;

        ValidateArgument(location, OpcodeArgType.FilterId);

        _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodeFilter(filterTypeId, filterPath, location));
    }

    /// <summary>
    /// Write a list size, restricted to non-negative integers
    /// </summary>
    /// <param name="value">The size of the list</param>
    /// <param name="location">The location of the list in the source code</param>
    public void WriteListSize(int value, Location location) {
        _location = location;
        ValidateArgument(location, OpcodeArgType.ListSize);

        if (value < 0) {
            compiler.ForcedError(location, "List size cannot be negative");
        }

        _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodeListSize(value, location));
    }

    /// <summary>
    /// Writes a label to the stream
    /// </summary>
    /// <param name="s">The label to write</param>
    /// <param name="location">The location of the label in the source code</param>
    public void WriteLabel(string s, Location location) {
        _location = location;
        ValidateArgument(location, OpcodeArgType.Label);

        _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodeLabel(s, location));
        _unresolvedLabelsInAnnotatedBytecode.Add((_annotatedBytecode.Count - 1, s));
    }

    public void ResolveCodeLabelReferences(Stack<DMProc.CodeLabelReference> pendingLabelReferences) {
        while (pendingLabelReferences.Count > 0) {
            DMProc.CodeLabelReference reference = pendingLabelReferences.Pop();
            DMProc.CodeLabel? label = GetCodeLabel(reference.Identifier, reference.Scope);

            // Failed to find the label in the given context
            if (label == null) {
                compiler.Emit(
                    WarningCode.ItemDoesntExist,
                    reference.Location,
                    $"Label \"{reference.Identifier}\" unreachable from scope or does not exist"
                );
                // Not cleaning away the placeholder will emit another compiler error
                // let's not do that
                _unresolvedLabelsInAnnotatedBytecode.RemoveAt(
                    _unresolvedLabelsInAnnotatedBytecode.FindIndex(o =>
                        o.LabelName == reference.Placeholder)
                );
                continue;
            }

            // Found it.
            AddLabel(reference.Placeholder, (int)label.AnnotatedByteOffset);

            // I was thinking about going through to replace all the placeholders
            // with the actual label.LabelName, but it means I need to modify
            // _unresolvedLabels, being a list of tuple objects. Fuck that noise
        }

        // TODO: Implement "unused label" like in BYOND DM, use label.ReferencedCount to figure out
        // foreach (CodeLabel codeLabel in CodeLabels) {
        //  ...
        // }
    }

    internal DMProc.CodeLabel? GetCodeLabel(string name, DMProc.DMProcScope? scope) {
        while (scope != null) {
            if (scope.LocalCodeLabels.TryGetValue(name, out var localCodeLabel))
                return localCodeLabel;

            scope = scope.ParentScope;
        }

        return null;
    }

    /// <summary>
    /// Tracks the maximum possible stack size of the proc
    /// </summary>
    /// <param name="sizeDelta">The net change in stack size caused by an operation</param>
    public void ResizeStack(int sizeDelta) {
        _currentStackSize += sizeDelta;
        _maxStackSize = Math.Max(_currentStackSize, _maxStackSize);
        if (_currentStackSize < 0 && !_negativeStackSizeError) {
            _negativeStackSizeError = true;
            compiler.ForcedError(_location, "Negative stack size");
        }
    }

    /// <summary>
    /// Gets the maximum possible stack size of the proc
    /// </summary>
    public int GetMaxStackSize() {
        return _maxStackSize;
    }

    public void WriteResource(string value, Location location) {
        _location = location;
        ValidateArgument(location, OpcodeArgType.Resource);
        int stringId = compiler.DMObjectTree.AddString(value);
        _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodeResource(stringId, location));
    }

    public void WriteTypeId(int typeId, Location location) {
        _location = location;
        ValidateArgument(location, OpcodeArgType.TypeId);
        _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodeTypeId(typeId, location));
    }

    public void WriteProcId(int procId, Location location) {
        _location = location;
        ValidateArgument(location, OpcodeArgType.ProcId);
        _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodeProcId(procId, location));
    }

    public void WriteEnumeratorId(int enumeratorId, Location location) {
        _location = location;
        ValidateArgument(location, OpcodeArgType.EnumeratorId);
        _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodeEnumeratorId(enumeratorId, location));
    }

    public void WriteFormatCount(int formatCount, Location location) {
        _location = location;
        ValidateArgument(location, OpcodeArgType.FormatCount);
        _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodeFormatCount(formatCount, location));
    }

    public void WritePickCount(int count, Location location) {
        _location = location;
        ValidateArgument(location, OpcodeArgType.PickCount);
        _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodePickCount(count, location));
    }

    public void WriteConcatCount(int count, Location location) {
        _location = location;
        ValidateArgument(location, OpcodeArgType.ConcatCount);
        _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodeConcatCount(count, location));
    }

    public void WriteReference(DMReference reference, Location location, bool affectStack = true) {
        _location = location;
        ValidateArgument(location, OpcodeArgType.Reference);

        switch (reference.RefType) {
            case DMReference.Type.Argument:
            case DMReference.Type.Local:
                _annotatedBytecode[^1]
                    .AddArg(compiler, new AnnotatedBytecodeReference(reference.RefType, reference.Index, location));
                break;

            case DMReference.Type.Global:
            case DMReference.Type.GlobalProc:
                _annotatedBytecode[^1]
                    .AddArg(compiler, new AnnotatedBytecodeReference(reference.RefType, reference.Index, location));
                break;

            case DMReference.Type.Field:
                int fieldId = compiler.DMObjectTree.AddString(reference.Name);
                _annotatedBytecode[^1]
                    .AddArg(compiler, new AnnotatedBytecodeReference(reference.RefType, fieldId, location));
                ResizeStack(affectStack ? -1 : 0);
                break;

            case DMReference.Type.SrcProc:
            case DMReference.Type.SrcField:
                fieldId = compiler.DMObjectTree.AddString(reference.Name);
                _annotatedBytecode[^1]
                    .AddArg(compiler, new AnnotatedBytecodeReference(reference.RefType, fieldId, location));
                break;

            case DMReference.Type.ListIndex:
                _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodeReference(reference.RefType, location));
                ResizeStack(affectStack ? -2 : 0);
                break;

            case DMReference.Type.SuperProc:
            case DMReference.Type.Src:
            case DMReference.Type.Self:
            case DMReference.Type.Args:
            case DMReference.Type.World:
            case DMReference.Type.Usr:
            case DMReference.Type.Callee:
            case DMReference.Type.Caller:
            case DMReference.Type.Invalid:
                _annotatedBytecode[^1].AddArg(compiler, new AnnotatedBytecodeReference(reference.RefType, location));
                break;

            default:
                compiler.ForcedError(_location, $"Encountered unknown reference type {reference.RefType}");
                break;
        }
    }

    public int GetLength() {
        return _annotatedBytecode.Count;
    }

    public void AddLabel(string name) {
        _labels.Add(name, _annotatedBytecode.Count);
        _annotatedBytecode.Add(new AnnotatedBytecodeLabel(name, _location));
    }

    public void AddLabel(string name, int position) {
        _labels.Add(name, position);
        _annotatedBytecode.Insert(position, new AnnotatedBytecodeLabel(name, _location));
    }

    public bool LabelExists(string name) {
        return _labels.ContainsKey(name);
    }

    public Dictionary<string, long> GetLabels() {
        return _labels;
    }

    public void WriteLocalVariable(string name, Location writerLocation) {
        _annotatedBytecode.Add(new AnnotatedBytecodeVariable(name, writerLocation));
    }

    public void WriteLocalVariableDealloc(int amount, Location writerLocation) {
        _annotatedBytecode.Add(new AnnotatedBytecodeVariable(amount, writerLocation));
    }
}
