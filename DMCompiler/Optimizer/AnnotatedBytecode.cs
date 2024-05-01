using System;
using System.Collections.Generic;
using DMCompiler.Bytecode;
using DMCompiler.DM;

namespace DMCompiler.Optimizer {
    public interface IAnnotatedBytecode {
        public void AddArg(IAnnotatedBytecode arg);
        void SetLocation(IAnnotatedBytecode location);
        void SetLocation(Location location);
        public Location GetLocation();
    }

    internal class AnnotatedBytecodeInstruction : IAnnotatedBytecode {
        private List<IAnnotatedBytecode> _args = new();
        public Location Location;
        public DreamProcOpcode Opcode;
        public int StackSizeDelta;

        public AnnotatedBytecodeInstruction(DreamProcOpcode opcode, int stackSizeDelta, Location location) {
            Opcode = opcode;
            StackSizeDelta = stackSizeDelta;
            Location = location;
        }

        // Given an existing instruction, create a new instruction with the same opcode and stack delta, but with new args
        public AnnotatedBytecodeInstruction(AnnotatedBytecodeInstruction instruction, List<IAnnotatedBytecode> args) {
            Opcode = instruction.Opcode;
            StackSizeDelta = instruction.StackSizeDelta;
            Location = instruction.Location;
            _args = args;
        }

        // Look up the stack delta for the opcode and create a new instruction with that stack delta and args
        public AnnotatedBytecodeInstruction(DreamProcOpcode op, List<IAnnotatedBytecode> args) {
            Opcode = op;
            OpcodeMetadata metadata = OpcodeMetadataCache.GetMetadata(op);
            StackSizeDelta = metadata.StackDelta;
            Location = new Location("Internal", null, null);
            ValidateArgs(metadata, args);
            _args = args;
        }

        public AnnotatedBytecodeInstruction(DreamProcOpcode opcode, int stackSizeDelta, List<IAnnotatedBytecode> args) {
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
                if (!matchArgs(metadata.RequiredArgs[i], args[i])) {
                    throw new Exception($"Expected arg {i} to be {metadata.RequiredArgs[i]}, got {args[i].GetType()}");
                }
            }
        }

        private bool matchArgs(OpcodeArgType requiredArg, IAnnotatedBytecode arg) {
            switch (requiredArg) {

                case OpcodeArgType.ArgType:
                    return arg is AnnotatedBytecodeArgumentType;
                case OpcodeArgType.StackDelta:
                    return arg is AnnotatedBytecodeStackDelta;
                case OpcodeArgType.Resource:
                    return arg is AnnotatedBytecodeResource;
                case OpcodeArgType.TypeId:
                    return arg is AnnotatedBytecodeTypeID;
                case OpcodeArgType.ProcId:
                    return arg is AnnotatedBytecodeProcID;
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

        private Location? _location;
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

    internal class AnnotatedBytecodeVariable : IAnnotatedBytecode {
        public int Exit;
        public bool Exitingscope;
        public Location Location;
        public string Name;

        public AnnotatedBytecodeVariable(string name, Location location) {
            Name = name;
            Location = location;
            Exitingscope = false;
        }

        public AnnotatedBytecodeVariable(int popoff, Location location) {
            Exitingscope = true;
            Exit = popoff;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a variable");
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


    internal class AnnotatedBytecodeInteger : IAnnotatedBytecode {
        public Location Location;
        public int Value;

        public AnnotatedBytecodeInteger(int value, Location location) {
            Value = value;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to an integer");
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

    internal class AnnotatedBytecodeFloat : IAnnotatedBytecode {
        public Location Location;
        public float Value;

        public AnnotatedBytecodeFloat(float value, Location location) {
            Value = value;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a float");
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

    internal class AnnotatedBytecodeString : IAnnotatedBytecode {
        public int ID;
        public Location Location;
        public string Value;

        public AnnotatedBytecodeString(string value, int id, Location location) {
            Value = value;
            ID = id;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a string");
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

    internal class AnnotatedBytecodeArgumentType : IAnnotatedBytecode {
        public Location Location;
        public DMCallArgumentsType Value;

        public AnnotatedBytecodeArgumentType(DMCallArgumentsType value, Location location) {
            Value = value;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to an argument type");
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

    internal class AnnotatedBytecodeType : IAnnotatedBytecode {
        public Location Location;
        public DMValueType Value;

        public AnnotatedBytecodeType(DMValueType value, Location location) {
            Value = value;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a type");
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

    internal class AnnotatedBytecodeTypeID : IAnnotatedBytecode {
        public Location Location;
        public DreamPath? Path;
        public int TypeID;

        public AnnotatedBytecodeTypeID(int typeID, DreamPath? path, Location location) {
            TypeID = typeID;
            Path = path;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a type");
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

    internal class AnnotatedBytecodeProcID : IAnnotatedBytecode {
        public Location Location;
        public DreamPath? Path;
        public int ProcID;

        public AnnotatedBytecodeProcID(int procID, DreamPath? path, Location location) {
            ProcID = procID;
            Path = path;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a type");
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

    internal class AnnotatedBytecodeFormatCount : IAnnotatedBytecode {
        public int Count;
        public Location Location;

        public AnnotatedBytecodeFormatCount(int count, Location location) {
            Count = count;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a format count");
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

    internal class AnnotatedBytecodeStackDelta : IAnnotatedBytecode {
        public int Delta;
        public Location Location;

        public AnnotatedBytecodeStackDelta(int delta, Location location) {
            Delta = delta;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a stack delta");
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

    internal class AnnotatedBytecodeListSize : IAnnotatedBytecode {
        public Location Location;
        public int Size;

        public AnnotatedBytecodeListSize(int size, Location location) {
            Size = size;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a list size");
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

    internal class AnnotatedBytecodePickCount : IAnnotatedBytecode {
        public int Count;
        public Location Location;

        public AnnotatedBytecodePickCount(int count, Location location) {
            Count = count;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a pick count");
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

    internal class AnnotatedBytecodeConcatCount : IAnnotatedBytecode {
        public int Count;
        public Location Location;

        public AnnotatedBytecodeConcatCount(int count, Location location) {
            Count = count;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a concat count");
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

    internal class AnnotatedBytecodeResource : IAnnotatedBytecode {
        public Location Location;
        public int ResourceID;
        public string Value;

        public AnnotatedBytecodeResource(string value, int rid, Location location) {
            Value = value;
            ResourceID = rid;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a resource");
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

    internal class AnnotatedBytecodeLabel : IAnnotatedBytecode {
        public string LabelName;
        public Location Location;

        public AnnotatedBytecodeLabel(string labelName, Location location) {
            LabelName = labelName;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a label");
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


    internal class AnnotatedBytecodeFilter : IAnnotatedBytecode {
        public DreamPath FilterPath;
        public int FilterTypeId;
        public Location Location;

        public AnnotatedBytecodeFilter(int filterTypeId, DreamPath filterPath, Location location) {
            FilterTypeId = filterTypeId;
            FilterPath = filterPath;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a filter");
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

    internal class AnnotatedBytecodeReference : IAnnotatedBytecode {
        public int Index;
        public Location Location;
        public DMReference.Type RefType;

        public AnnotatedBytecodeReference(DMReference.Type refType, int index, Location location) {
            RefType = refType;
            Index = index;
            Location = location;
        }

        public AnnotatedBytecodeReference(DMReference.Type refType, Location location) {
            RefType = refType;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a reference");
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
}
