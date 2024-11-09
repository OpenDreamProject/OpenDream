using DMCompiler.Bytecode;
using DMCompiler.DM;

namespace DMCompiler.Optimizer;

internal interface IAnnotatedBytecode {
    public DMCompiler Compiler { get; }
    public void AddArg(IAnnotatedBytecode arg);
    void SetLocation(IAnnotatedBytecode location);
    void SetLocation(Location location);
    public Location GetLocation();
}

internal sealed class AnnotatedBytecodeInstruction : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; }
    public Location Location;
    public DreamProcOpcode Opcode;
    public int StackSizeDelta;

    private readonly List<IAnnotatedBytecode> _args = new();
    private Location? _location;

    public AnnotatedBytecodeInstruction(DMCompiler compiler, DreamProcOpcode opcode, int stackSizeDelta, Location location) {
        Compiler = compiler;
        Opcode = opcode;
        StackSizeDelta = stackSizeDelta;
        Location = location;
    }

    // Given an existing instruction, create a new instruction with the same opcode and stack delta, but with new args
    public AnnotatedBytecodeInstruction(AnnotatedBytecodeInstruction instruction, List<IAnnotatedBytecode> args) {
        Compiler = instruction.Compiler;
        Opcode = instruction.Opcode;
        StackSizeDelta = instruction.StackSizeDelta;
        Location = instruction.Location;
        _args = args;
    }

    // Look up the stack delta for the opcode and create a new instruction with that stack delta and args
    public AnnotatedBytecodeInstruction(DMCompiler compiler, DreamProcOpcode op, List<IAnnotatedBytecode> args) {
        Compiler = compiler;
        Opcode = op;
        OpcodeMetadata metadata = OpcodeMetadataCache.GetMetadata(op);
        StackSizeDelta = metadata.StackDelta;
        Location = new Location("Internal", null, null);
        ValidateArgs(metadata, args);
        _args = args;
    }

    public AnnotatedBytecodeInstruction(DMCompiler compiler, DreamProcOpcode opcode, int stackSizeDelta, List<IAnnotatedBytecode> args) {
        Compiler = compiler;
        Opcode = opcode;
        StackSizeDelta = stackSizeDelta;
        Location = new Location("Internal", null, null);
        ValidateArgs(OpcodeMetadataCache.GetMetadata(opcode), args);
        _args = args;
    }

    private void ValidateArgs(OpcodeMetadata metadata, List<IAnnotatedBytecode> args) {
        if (metadata.VariableArgs) {
            if (args[0] is not AnnotatedBytecodeInteger) {
                throw new Exception("Variable arg instructions must have a sizing operand (integer) as their first arg");
            }

            return;
        }

        if (metadata.RequiredArgs.Count != args.Count) {
            throw new Exception($"Expected {metadata.RequiredArgs.Count} args, got {args.Count}");
        }

        for (int i = 0; i < metadata.RequiredArgs.Count; i++) {
            if (!MatchArgs(metadata.RequiredArgs[i], args[i])) {
                throw new Exception($"Expected arg {i} to be {metadata.RequiredArgs[i]}, got {args[i].GetType()}");
            }
        }
    }

    private bool MatchArgs(OpcodeArgType requiredArg, IAnnotatedBytecode arg) {
        switch (requiredArg) {
            case OpcodeArgType.ArgType:
                return arg is AnnotatedBytecodeArgumentType;
            case OpcodeArgType.StackDelta:
                return arg is AnnotatedBytecodeStackDelta;
            case OpcodeArgType.Resource:
                return arg is AnnotatedBytecodeResource;
            case OpcodeArgType.TypeId:
                return arg is AnnotatedBytecodeTypeId;
            case OpcodeArgType.ProcId:
                return arg is AnnotatedBytecodeProcId;
            case OpcodeArgType.FilterId:
                return arg is AnnotatedBytecodeFilter;
            case OpcodeArgType.ListSize:
                return arg is AnnotatedBytecodeListSize;
            case OpcodeArgType.Int:
                return arg is AnnotatedBytecodeInteger;
            case OpcodeArgType.Label:
                return arg is AnnotatedBytecodeLabel;
            case OpcodeArgType.Float:
                return arg is AnnotatedBytecodeFloat;
            case OpcodeArgType.String:
                return arg is AnnotatedBytecodeString;
            case OpcodeArgType.Reference:
                return arg is AnnotatedBytecodeReference;
            case OpcodeArgType.FormatCount:
                return arg is AnnotatedBytecodeFormatCount;
            case OpcodeArgType.PickCount:
                return arg is AnnotatedBytecodePickCount;
            case OpcodeArgType.ConcatCount:
                return arg is AnnotatedBytecodeConcatCount;
            default:
                throw new ArgumentOutOfRangeException(nameof(requiredArg), requiredArg, null);
        }
    }

    public void AddArg(IAnnotatedBytecode arg) {
        _args.Add(arg);
    }

    public List<IAnnotatedBytecode> GetArgs() {
        return _args;
    }

    public IAnnotatedBytecode GetArg(int index) {
        return _args[index];
    }

    public T GetArg<T>(int index) where T : IAnnotatedBytecode {
        return (T)GetArg(index);
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        if (_location != null) return;
        _location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        if (_location != null) return;
        _location = loc;
    }

    public Location GetLocation() {
        return _location ?? Location;
    }
}

internal sealed class AnnotatedBytecodeVariable : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; }
    public int Exit;
    public bool ExitingScope;
    public Location Location;
    public string? Name;

    public AnnotatedBytecodeVariable(DMCompiler compiler, string name, Location location) {
        Compiler = compiler;
        Name = name;
        Location = location;
        ExitingScope = false;
    }

    public AnnotatedBytecodeVariable(DMCompiler compiler, int popOff, Location location) {
        Compiler = compiler;
        ExitingScope = true;
        Exit = popOff;
        Location = location;
    }

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to a variable");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodeInteger(DMCompiler compiler, int value, Location location) : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public Location Location = location;
    public int Value = value;

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to an integer");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodeFloat(DMCompiler compiler, float value, Location location) : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public Location Location = location;
    public float Value = value;

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to a float");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodeString(DMCompiler compiler, int id, Location location) : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public int Id = id;
    public Location Location = location;

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to a string");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodeArgumentType(DMCompiler compiler, DMCallArgumentsType value, Location location) : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public Location Location = location;
    public DMCallArgumentsType Value = value;

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to an argument type");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodeType(DMCompiler compiler, DMValueType value, Location location) : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public Location Location = location;
    public DMValueType Value = value;

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to a type");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodeTypeId(DMCompiler compiler, int typeId, Location location) : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public Location Location = location;
    public int TypeId = typeId;

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to a type");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodeProcId(DMCompiler compiler, int procId, Location location) : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public Location Location = location;
    public int ProcId = procId;

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to a type");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodeEnumeratorId(DMCompiler compiler, int enumeratorId, Location location) : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public Location Location = location;
    public int EnumeratorId = enumeratorId;

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to a type");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodeFormatCount(DMCompiler compiler, int count, Location location)
    : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public int Count = count;
    public Location Location = location;

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to a format count");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodeStackDelta(DMCompiler compiler, int delta, Location location) : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public int Delta = delta;
    public Location Location = location;

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to a stack delta");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodeListSize(DMCompiler compiler, int size, Location location) : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public Location Location = location;
    public int Size = size;

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to a list size");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodePickCount(DMCompiler compiler, int count, Location location) : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public int Count = count;
    public Location Location = location;

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to a pick count");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodeConcatCount(DMCompiler compiler, int count, Location location) : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public int Count = count;
    public Location Location = location;

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to a concat count");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodeResource(DMCompiler compiler, int rid, Location location) : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public Location Location = location;
    public int ResourceId = rid;

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to a resource");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodeLabel(DMCompiler compiler, string labelName, Location location) : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public string LabelName = labelName;
    public Location Location = location;

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to a label");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodeFilter(DMCompiler compiler, int filterTypeId, DreamPath filterPath, Location location)
    : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public DreamPath FilterPath = filterPath;
    public int FilterTypeId = filterTypeId;
    public Location Location = location;

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to a filter");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}

internal sealed class AnnotatedBytecodeReference(DMCompiler compiler, DMReference.Type refType, int index, Location location)
    : IAnnotatedBytecode {
     public DMCompiler Compiler { get; set; } = compiler;
    public int Index = index;
    public Location Location = location;
    public DMReference.Type RefType = refType;

    public AnnotatedBytecodeReference(DMCompiler compiler, DMReference.Type refType, Location location) : this(compiler, refType, 0, location) {
    }

    public void AddArg(IAnnotatedBytecode arg) {
        Compiler.ForcedError(Location, "Cannot add args to a reference");
    }

    public void SetLocation(IAnnotatedBytecode loc) {
        Location = loc.GetLocation();
    }

    public void SetLocation(Location loc) {
        Location = loc;
    }

    public Location GetLocation() {
        return Location;
    }
}
