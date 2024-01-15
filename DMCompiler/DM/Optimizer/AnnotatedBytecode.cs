using System;
using System.Collections.Generic;
using DMCompiler.Bytecode;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream;

namespace DMCompiler.DM.Optimizer;


    internal interface IAnnotatedBytecode {
        public void AddArg(IAnnotatedBytecode arg);
    }

    internal class AnnotatedBytecodeInstruction : IAnnotatedBytecode {
        public DreamProcOpcode Opcode;
        public Location Location;
        private List<IAnnotatedBytecode> _args = new();
        public int StackSizeDelta;

        public AnnotatedBytecodeInstruction(DreamProcOpcode opcode, int stackSizeDelta, Location location) {
            Opcode = opcode;
            StackSizeDelta = stackSizeDelta;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            _args.Add(arg);
        }

        public List<IAnnotatedBytecode> GetArgs() {
            return _args;
        }
    }

    internal class AnnotatedBytecodeInteger : IAnnotatedBytecode {
        public int Value;
        public Location Location;

        public AnnotatedBytecodeInteger(int value, Location location) {
            Value = value;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to an integer");
        }
    }

    internal class AnnotatedBytecodeFloat : IAnnotatedBytecode {
        public float Value;
        public Location Location;

        public AnnotatedBytecodeFloat(float value, Location location) {
            Value = value;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a float");
        }
    }

    internal class AnnotatedBytecodeString : IAnnotatedBytecode {
        public string Value;
        public Location Location;

        public AnnotatedBytecodeString(string value, Location location) {
            Value = value;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a string");
        }
    }

    internal class AnnotatedBytecodeArgumentType : IAnnotatedBytecode {
        public DMCallArgumentsType Value;
        public Location Location;

        public AnnotatedBytecodeArgumentType(DMCallArgumentsType value, Location location) {
            Value = value;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to an argument type");
        }
    }

    internal class AnnotatedBytecodeType : IAnnotatedBytecode {
        public DMValueType Value;
        public Location Location;

        public AnnotatedBytecodeType(DMValueType value, Location location) {
            Value = value;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a type");
        }
    }

    internal class AnnotatedBytecodeTypeID : IAnnotatedBytecode {
        public int TypeID;
        public DreamPath? Path;
        public Location Location;

        public AnnotatedBytecodeTypeID(int typeID, DreamPath? path, Location location) {
            TypeID = typeID;
            Path = path;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a type");
        }
    }

    internal class AnnotatedBytecodeProcID : IAnnotatedBytecode {
        public int ProcID;
        public DreamPath? Path;
        public Location Location;

        public AnnotatedBytecodeProcID(int procID, DreamPath? path, Location location) {
            ProcID = procID;
            Path = path;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a type");
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
    }

    internal class AnnotatedBytecodeResource : IAnnotatedBytecode {
        public string Value;
        public Location Location;

        public AnnotatedBytecodeResource(string value, Location location) {
            Value = value;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a resource");
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
    }


    internal class AnnotatedBytecodeFilter : IAnnotatedBytecode {
        public int FilterTypeId;
        public DreamPath FilterPath;
        public Location Location;

        public AnnotatedBytecodeFilter(int filterTypeId, DreamPath filterPath, Location location) {
            FilterTypeId = filterTypeId;
            FilterPath = filterPath;
            Location = location;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a filter");
        }
    }

    internal class AnnotatedBytecodeReference : IAnnotatedBytecode {
        public DMReference.Type RefType;
        public int Index;
        public string Name;
        public Location Location;

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
    }
