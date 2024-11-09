using DMCompiler.Bytecode;
using DMCompiler.Compiler;
using DMCompiler.DM;

namespace DMCompiler.Optimizer;

/*
 * Provides a wrapper about BinaryWriter that stores information about the bytecode
 * for optimization and debugging.
 */
internal class AnnotatedByteCodeWriter(DMCompiler compiler) {
    public DMCompiler Compiler = compiler;
    private readonly List<IAnnotatedBytecode>
        _annotatedBytecode = new(250); // 1/6th of max size for bytecode in tgstation

    private readonly List<(long Position, string LabelName)> _unresolvedLabelsInAnnotatedBytecode = new();
    private int _currentStackSize;
    private Location _location;
    private int _maxStackSize;
    private bool _negativeStackSizeError;
    private Stack<OpcodeArgType> _requiredArgs = new();
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
        if (_requiredArgs.Count > 0) {
            Compiler.ForcedError(location, "Expected argument");
        }

        var metadata = OpcodeMetadataCache.GetMetadata(opcode);
        // Goal here is to maintain correspondence between the raw bytecode and the annotated bytecode such that
        // the annotated bytecode can be used to generate the raw bytecode again.
        _annotatedBytecode.Add(new AnnotatedBytecodeInstruction(Compiler, opcode, metadata.StackDelta, location));

        ResizeStack(metadata.StackDelta);

        var requiredArgs = new Stack<OpcodeArgType>(metadata.RequiredArgs.Count);

        // Reverse the order and push to stack
        for (int i = metadata.RequiredArgs.Count - 1; i >= 0; i--) {
            requiredArgs.Push(metadata.RequiredArgs[i]);
        }

        _requiredArgs = requiredArgs;
    }

    /// <summary>
    /// Writes a float to the stream
    /// </summary>
    /// <param name="val">The integer to write</param>
    /// <param name="location">The location of the integer in the source code</param>
    public void WriteFloat(float val, Location location) {
        _location = location;
        if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.Float) {
            Compiler.ForcedError(location, "Expected floating argument");
        }

        _requiredArgs.Pop();
        _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeFloat(Compiler, val, location));
    }

    /// <summary>
    /// Writes argument classification to the stream
    /// </summary>
    /// <param name="argType">The argument type to write</param>
    /// <param name="location">The location of the integer in the source code</param>
    public void WriteArgumentType(DMCallArgumentsType argType, Location location) {
        _location = location;
        if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.ArgType) {
            Compiler.ForcedError(location, "Expected argument type argument");
        }

        _requiredArgs.Pop();
        _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeArgumentType(Compiler, argType, location));
    }

    /// <summary>
    /// Write a stack delta to the stream
    /// </summary>
    /// <param name="delta">The stack delta to write</param>
    /// <param name="location">The location of the integer in the source code</param>
    public void WriteStackDelta(int delta, Location location) {
        _location = location;
        if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.StackDelta) {
            Compiler.ForcedError(location, "Expected stack delta argument");
        }

        _requiredArgs.Pop();
        _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeStackDelta(Compiler, delta, location));
    }

    /// <summary>
    /// Write a type to the stream
    /// </summary>
    /// <param name="type">The type to write</param>
    /// <param name="location">The location of the type in the source code</param>
    public void WriteType(DMValueType type, Location location) {
        if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.TypeId) {
            Compiler.ForcedError(location, "Expected type argument");
        }

        _requiredArgs.Pop();

        _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeType(Compiler, type, location));
    }

    /// <summary>
    /// Writes a string to the stream and stores it in the string table
    /// </summary>
    /// <param name="value">The string to write</param>
    /// <param name="location">The location of the string in the source code</param>
    public void WriteString(string value, Location location) {
        _location = location;
        if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.String) {
            Compiler.ForcedError(location, "Expected string argument");
        }

        _requiredArgs.Pop();
        int stringId = DMObjectTree.AddString(value);
        _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeString(Compiler, stringId, location));
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

        if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.FilterId) {
            Compiler.ForcedError(location, "Expected filter argument");
        }

        _requiredArgs.Pop();

        _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeFilter(Compiler, filterTypeId, filterPath, location));
    }

    /// <summary>
    /// Write a list size, restricted to non-negative integers
    /// </summary>
    /// <param name="value">The size of the list</param>
    /// <param name="location">The location of the list in the source code</param>
    public void WriteListSize(int value, Location location) {
        _location = location;
        if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.ListSize) {
            Compiler.ForcedError(location, "Expected list size argument");
        }

        if (value < 0) {
            Compiler.ForcedError(location, "List size cannot be negative");
        }

        _requiredArgs.Pop();
        _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeListSize(Compiler, value, location));
    }

    /// <summary>
    /// Writes a label to the stream
    /// </summary>
    /// <param name="s">The label to write</param>
    /// <param name="location">The location of the label in the source code</param>
    public void WriteLabel(string s, Location location) {
        _location = location;
        if (_requiredArgs.Count == 0 || _requiredArgs.Pop() != OpcodeArgType.Label) {
            Compiler.ForcedError(location, "Expected label argument");
        }

        _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeLabel(Compiler, s, location));
        _unresolvedLabelsInAnnotatedBytecode.Add((_annotatedBytecode.Count - 1, s));
    }

    public void ResolveCodeLabelReferences(Stack<DMProc.CodeLabelReference> pendingLabelReferences) {
        while (pendingLabelReferences.Count > 0) {
            DMProc.CodeLabelReference reference = pendingLabelReferences.Pop();
            DMProc.CodeLabel? label = GetCodeLabel(reference.Identifier, reference.Scope);

            // Failed to find the label in the given context
            if (label == null) {
                Compiler.Emit(
                    WarningCode.ItemDoesntExist,
                    reference.Location,
                    $"Label \"{reference.Identifier}\" unreachable from scope or does not exist"
                );
                // Not cleaning away the placeholder will emit another compiler error
                // let's not do that
                _unresolvedLabelsInAnnotatedBytecode.RemoveAt(
                    _unresolvedLabelsInAnnotatedBytecode.FindIndex(((long Position, string LabelName) o) =>
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
            Compiler.ForcedError(_location, "Negative stack size");
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
        if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.Resource) {
            Compiler.ForcedError(location, "Expected resource argument");
        }

        _requiredArgs.Pop();
        int stringId = DMObjectTree.AddString(value);
        _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeResource(Compiler, stringId, location));
    }

    public void WriteTypeId(int typeId, Location location) {
        _location = location;
        if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.TypeId) {
            Compiler.ForcedError(location, "Expected TypeID argument");
        }

        _requiredArgs.Pop();
        _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeTypeId(Compiler, typeId, location));
    }

    public void WriteProcId(int procId, Location location) {
        _location = location;
        if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.ProcId) {
            Compiler.ForcedError(location, "Expected ProcID argument");
        }

        _requiredArgs.Pop();
        _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeProcId(Compiler, procId, location));
    }

    public void WriteEnumeratorId(int enumeratorId, Location location) {
        _location = location;
        if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.EnumeratorId) {
            Compiler.ForcedError(location, "Expected EnumeratorID argument");
        }

        _requiredArgs.Pop();
        _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeEnumeratorId(Compiler, enumeratorId, location));
    }

    public void WriteFormatCount(int formatCount, Location location) {
        _location = location;
        if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.FormatCount) {
            Compiler.ForcedError(location, "Expected format count argument");
        }

        _requiredArgs.Pop();
        _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeFormatCount(Compiler, formatCount, location));
    }

    public void WritePickCount(int count, Location location) {
        _location = location;
        if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.PickCount) {
            Compiler.ForcedError(location, "Expected pick count argument");
        }

        _requiredArgs.Pop();
        _annotatedBytecode[^1].AddArg(new AnnotatedBytecodePickCount(Compiler, count, location));
    }

    public void WriteConcatCount(int count, Location location) {
        _location = location;
        if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.ConcatCount) {
            Compiler.ForcedError(location, "Expected concat count argument");
        }

        _requiredArgs.Pop();
        _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeConcatCount(Compiler, count, location));
    }

    public void WriteReference(DMReference reference, Location location, bool affectStack = true) {
        _location = location;
        if (_requiredArgs.Count == 0 || _requiredArgs.Pop() != OpcodeArgType.Reference) {
            Compiler.ForcedError(location, "Expected reference argument");
        }

        switch (reference.RefType) {
            case DMReference.Type.Argument:
            case DMReference.Type.Local:
                _annotatedBytecode[^1]
                    .AddArg(new AnnotatedBytecodeReference(Compiler, reference.RefType, reference.Index, location));
                break;

            case DMReference.Type.Global:
            case DMReference.Type.GlobalProc:
                _annotatedBytecode[^1]
                    .AddArg(new AnnotatedBytecodeReference(Compiler, reference.RefType, reference.Index, location));
                break;

            case DMReference.Type.Field:
                int fieldId = DMObjectTree.AddString(reference.Name);
                _annotatedBytecode[^1]
                    .AddArg(new AnnotatedBytecodeReference(Compiler, reference.RefType, fieldId, location));
                ResizeStack(affectStack ? -1 : 0);
                break;

            case DMReference.Type.SrcProc:
            case DMReference.Type.SrcField:
                fieldId = DMObjectTree.AddString(reference.Name);
                _annotatedBytecode[^1]
                    .AddArg(new AnnotatedBytecodeReference(Compiler, reference.RefType, fieldId, location));
                break;

            case DMReference.Type.ListIndex:
                _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeReference(Compiler, reference.RefType, location));
                ResizeStack(affectStack ? -2 : 0);
                break;

            case DMReference.Type.SuperProc:
            case DMReference.Type.Src:
            case DMReference.Type.Self:
            case DMReference.Type.Args:
            case DMReference.Type.Usr:
            case DMReference.Type.Invalid:
                _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeReference(Compiler, reference.RefType, location));
                break;

            default:
                Compiler.ForcedError(_location, $"Encountered unknown reference type {reference.RefType}");
                break;
        }
    }

    public int GetLength() {
        return _annotatedBytecode.Count;
    }

    public void AddLabel(string name) {
        _labels.Add(name, _annotatedBytecode.Count);
        _annotatedBytecode.Add(new AnnotatedBytecodeLabel(Compiler, name, _location));
    }

    public void AddLabel(string name, int position) {
        _labels.Add(name, position);
        _annotatedBytecode.Insert(position, new AnnotatedBytecodeLabel(Compiler, name, _location));
    }

    public bool LabelExists(string name) {
        return _labels.ContainsKey(name);
    }

    public Dictionary<string, long> GetLabels() {
        return _labels;
    }

    public void WriteLocalVariable(string name, Location writerLocation) {
        _annotatedBytecode.Add(new AnnotatedBytecodeVariable(Compiler, name, writerLocation));
    }

    public void WriteLocalVariableDealloc(int amount, Location writerLocation) {
        _annotatedBytecode.Add(new AnnotatedBytecodeVariable(Compiler, amount, writerLocation));
    }
}
