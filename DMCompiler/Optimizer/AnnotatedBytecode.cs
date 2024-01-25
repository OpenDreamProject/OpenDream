using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DMCompiler.Bytecode;
using DMCompiler.Json;

namespace DMCompiler.DM.Optimizer {
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

    internal class AnnotatedBytecodePrinter {
        private static string oldFile = "";
        private static List<string> oldFileContents = new();

        private static int max_opcode_length = -1;
        private static readonly List<string> indent_cache = new();

        private static DMProc _input;

        private static List<string> localScopeVariables = new();
        private static List<string> localScopeVariablesNoRemove = new();

        public static void Print(IReadOnlyList<IAnnotatedBytecode> annotatedBytecode, List<SourceInfoJson> sourceInfo,
            StreamWriter outp, DMProc input, int indent = 1) {
            _input = input;
            int currentLine = 0;

            Dictionary<int, SourceInfoJson> sourceInfoByLine = new();

            string currentFile = "";

            foreach (SourceInfoJson sourceInfoItem in sourceInfo) {
                if (sourceInfoItem.File != null) {
                    currentFile = DMObjectTree.StringTable[sourceInfoItem.File.Value];
                }

                sourceInfoByLine.TryAdd(sourceInfoItem.Line, sourceInfoItem);
            }

            List<string> currentFileContents = new();
            string fileNameRelative = "";
            if (currentFile != "" && currentFile != oldFile) {
                fileNameRelative = currentFile;
                if (!File.Exists(currentFile)) {
                    var newFile = Path.Join(DMCompiler.StandardLibraryDirectory, currentFile);
                    if (File.Exists(newFile)) {
                        currentFile = newFile;
                    } else {
                        currentFile = Path.Join(DMCompiler.MainDirectory, currentFile);
                    }
                }

                if (!File.Exists(currentFile)) {
                    currentFile = "";
                } else {
                    currentFileContents = File.ReadLines(currentFile).ToList();
                }
            }

            if (currentFile != oldFile) {
                oldFile = currentFile;
                oldFileContents = currentFileContents;
            } else {
                currentFileContents = oldFileContents;
            }

            StringBuilder output = new();

            if (annotatedBytecode.Count == 0) {
                output.Append("\t pass\n");
                return;
            }

            List<int> hideSetLines = new();

            sourceInfoByLine = sourceInfoByLine.Where(x => x.Key >= 0 && x.Key < currentFileContents.Count && (currentFileContents[x.Key].Trim() != "" || currentFileContents[x.Key - 1].Trim() != "")).ToDictionary(x => x.Key, x => x.Value);

            if(sourceInfoByLine.Count == 0 && annotatedBytecode.Count > 0) {
                output.Append("// No source info available. Likely function stub with code for initializing default values\n");
                if(annotatedBytecode[0].GetLocation().Line != null && currentFileContents.Count > annotatedBytecode[0].GetLocation().Line - 1 && currentFileContents[(Index)(annotatedBytecode[0].GetLocation().Line - 1)].Trim() != "") {
                    output.Append("// First line of code: ").Append(currentFileContents[(Index)(annotatedBytecode[0].GetLocation().Line - 1)].Trim()).Append("\n");
                }
            }

            foreach (IAnnotatedBytecode annotatedBytecodeItem in annotatedBytecode) {
                switch (annotatedBytecodeItem) {
                    case AnnotatedBytecodeInstruction annotatedBytecodeInstruction:
                        if (annotatedBytecodeInstruction.Location.Line != currentLine) {
                            currentLine = (annotatedBytecodeInstruction.Location.Line) ?? -1;
                            if (sourceInfoByLine.ContainsKey(currentLine) && currentFile != "" &&
                                currentLine - 1 < currentFileContents.Count && !hideSetLines.Contains(currentLine)) {
                                hideSetLines.Add(currentLine);
                                output.Append(' ', indent * 4).Append(" // ").Append(fileNameRelative).Append("@").Append(currentLine).Append(": ")
                                    .Append(currentFileContents[currentLine - 1].Trim()).Append("\n");
                            }
                        }

                        // For now all this does is keep labels flush with the left side
                        // Later may do something different e.g basic blocks
                        output.Append(' ', indent * 4);
                        Print(annotatedBytecodeInstruction, output);
                        output.Append("\n");
                        break;
                    case AnnotatedBytecodeLabel label:
                        output.Append(label.LabelName).Append(":").Append("\n");
                        break;
                    case AnnotatedBytecodeVariable variable:
                        if (variable.Exitingscope) {
                            output.Append(' ', indent * 4).Append("//Variable")
                                .Append(localScopeVariables.Count == 1 ? " " : "s ");
                            for (var i = 0; i < localScopeVariables.Count; i++) {
                                output.Append(localScopeVariables[i]);
                                if (i < localScopeVariables.Count - 2)
                                    output.Append(", ");
                                else if (i == localScopeVariables.Count - 2)
                                    output.Append(" and ");
                            }

                            var havehas = localScopeVariables.Count == 1 ? "has" : "have";
                            output.Append($" {havehas} exited scope\n");
                            if (localScopeVariables.Count - variable.Exit >= 0)
                                localScopeVariables.RemoveRange(localScopeVariables.Count - variable.Exit,
                                    variable.Exit);
                        } else {
                            output.Append(' ', indent * 4).Append("// Variable ").Append(variable.Name)
                                .Append(" has entered scope\n");
                            localScopeVariables.Add(variable.Name);
                            localScopeVariablesNoRemove.Add(variable.Name);
                        }

                        break;
                    default:
                        throw new Exception(
                            "Only labels and instructions allowed at top-level, others should be args to instructions");
                }
            }

            localScopeVariables.Clear();
            localScopeVariablesNoRemove.Clear();

            outp.Write(output.ToString());
        }

        private static void Print(AnnotatedBytecodeInstruction annotatedBytecode, StringBuilder sb) {
            DreamProcOpcode opcode = annotatedBytecode.Opcode;
            if (max_opcode_length == -1) max_opcode_length = Enum.GetNames(typeof(DreamProcOpcode)).Max(x => x.Length);
            // If the length of the opcode name is less than the max opcode length, add spaces to the end of the opcode name
            // Also cache the indent strings for each opcode length
            if (indent_cache.Count == 0)
                for (var i = 0; i <= max_opcode_length; i++)
                    indent_cache.Add(new string(' ', max_opcode_length - i) + "\t");
            var less_offset = indent_cache[opcode.ToString().Length];
            switch (opcode) {
                case DreamProcOpcode.FormatString:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    sb.Append(", ");
                    Print(annotatedBytecode.GetArgs()[1], sb);
                    break;
                case DreamProcOpcode.PushString:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    break;
                case DreamProcOpcode.PushResource:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    break;
                case DreamProcOpcode.DereferenceField:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    break;
                case DreamProcOpcode.DereferenceCall:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    sb.Append(", ");
                    Print(annotatedBytecode.GetArgs()[1], sb);
                    sb.Append(", ");
                    Print(annotatedBytecode.GetArgs()[2], sb);
                    break;
                case DreamProcOpcode.Prompt:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    break;

                case DreamProcOpcode.PushFloat:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    break;

                case DreamProcOpcode.Assign:
                case DreamProcOpcode.AssignInto:
                case DreamProcOpcode.Append:
                case DreamProcOpcode.Remove:
                case DreamProcOpcode.Combine:
                case DreamProcOpcode.Increment:
                case DreamProcOpcode.Decrement:
                case DreamProcOpcode.Mask:
                case DreamProcOpcode.MultiplyReference:
                case DreamProcOpcode.DivideReference:
                case DreamProcOpcode.BitXorReference:
                case DreamProcOpcode.ModulusReference:
                case DreamProcOpcode.ModulusModulusReference:
                case DreamProcOpcode.BitShiftLeftReference:
                case DreamProcOpcode.BitShiftRightReference:
                case DreamProcOpcode.OutputReference:
                case DreamProcOpcode.PushReferenceValue:
                case DreamProcOpcode.PopReference:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    break;

                case DreamProcOpcode.Input:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    sb.Append(", ");
                    Print(annotatedBytecode.GetArgs()[1], sb);
                    break;

                case DreamProcOpcode.CallStatement:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    sb.Append(", ");
                    Print(annotatedBytecode.GetArgs()[1], sb);
                    break;
                case DreamProcOpcode.CreateObject:
                case DreamProcOpcode.Gradient:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    sb.Append(", ");
                    Print(annotatedBytecode.GetArgs()[1], sb);
                    break;
                case DreamProcOpcode.Call:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    sb.Append(", ");
                    Print(annotatedBytecode.GetArgs()[1], sb);
                    sb.Append(", ");
                    Print(annotatedBytecode.GetArgs()[2], sb);
                    break;

                case DreamProcOpcode.EnumerateNoAssign:
                case DreamProcOpcode.Spawn:
                case DreamProcOpcode.BooleanOr:
                case DreamProcOpcode.BooleanAnd:
                case DreamProcOpcode.SwitchCase:
                case DreamProcOpcode.SwitchCaseRange:
                case DreamProcOpcode.Jump:
                case DreamProcOpcode.JumpIfFalse:
                case DreamProcOpcode.JumpIfTrue:
                case DreamProcOpcode.JumpIfNull:
                case DreamProcOpcode.JumpIfNotNull:
                case DreamProcOpcode.JumpIfNullNoPop:
                case DreamProcOpcode.TryNoValue:
                case DreamProcOpcode.NullRef:
                case DreamProcOpcode.CreateList:
                case DreamProcOpcode.CreateAssociativeList:
                case DreamProcOpcode.CreateFilteredListEnumerator:
                case DreamProcOpcode.PickWeighted:
                case DreamProcOpcode.PickUnweighted:
                case DreamProcOpcode.PushType:
                case DreamProcOpcode.IsTypeDirect:
                case DreamProcOpcode.PushProc:
                case DreamProcOpcode.MassConcatenation:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    break;

                case DreamProcOpcode.JumpIfNullDereference:
                case DreamProcOpcode.JumpIfTrueReference:
                case DreamProcOpcode.JumpIfFalseReference:
                case DreamProcOpcode.Enumerate:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    sb.Append(", ");
                    Print(annotatedBytecode.GetArgs()[1], sb);
                    break;

                case DreamProcOpcode.Try:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    sb.Append(", ");
                    Print(annotatedBytecode.GetArgs()[1], sb);
                    break;
                // Peephole optimizations
                case DreamProcOpcode.PushRefandJumpIfNotNull:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    sb.Append(", ");
                    Print(annotatedBytecode.GetArgs()[1], sb);
                    break;
                case DreamProcOpcode.AssignPop:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    break;
                case DreamProcOpcode.PushRefAndDereferenceField:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    sb.Append(", ");
                    Print(annotatedBytecode.GetArgs()[1], sb);
                    break;
                case DreamProcOpcode.PushNFloats:
                case DreamProcOpcode.PushNResources:
                case DreamProcOpcode.PushNRefs:
                case DreamProcOpcode.PushNStrings:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    sb.Append("  ->   ");
                    for (var i = 1; i < annotatedBytecode.GetArgs().Count; i++) {
                        Print(annotatedBytecode.GetArgs()[i], sb);
                        if (i < annotatedBytecode.GetArgs().Count - 1) sb.Append(", ");
                    }

                    break;
                case DreamProcOpcode.PushStringFloat:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    sb.Append(", ");
                    Print(annotatedBytecode.GetArgs()[1], sb);
                    break;
                case DreamProcOpcode.JumpIfReferenceFalse:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    sb.Append(", ");
                    Print(annotatedBytecode.GetArgs()[1], sb);
                    break;
                case DreamProcOpcode.SwitchOnFloat:
                case DreamProcOpcode.SwitchOnString:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    sb.Append(", ");
                    Print(annotatedBytecode.GetArgs()[1], sb);
                    break;
                case DreamProcOpcode.PushNOfStringFloats: {
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    sb.Append("  ->   ");
                    for (var i = 1; i < annotatedBytecode.GetArgs().Count; i++) {
                        Print(annotatedBytecode.GetArgs()[i], sb);
                        sb.Append(", ");
                        Print(annotatedBytecode.GetArgs()[i + 1], sb);
                        if (i < annotatedBytecode.GetArgs().Count - 2) sb.Append(", ");
                        i++;
                    }

                    break;
                }

                case DreamProcOpcode.CreateListNFloats:
                case DreamProcOpcode.CreateListNStrings:
                case DreamProcOpcode.CreateListNRefs:
                case DreamProcOpcode.CreateListNResources:
                    sb.Append($"{opcode}{less_offset} ");
                    Print(annotatedBytecode.GetArgs()[0], sb);
                    sb.Append("  ->   ");
                    for (var i = 1; i < annotatedBytecode.GetArgs().Count; i++) {
                        Print(annotatedBytecode.GetArgs()[i], sb);
                        if (i < annotatedBytecode.GetArgs().Count - 1) sb.Append(", ");
                    }

                    break;
                default:
                    if (annotatedBytecode.GetArgs().Count > 0) {
                        throw new Exception($"UH OH YOU FORGOT TO ADD A CASE FOR {opcode}");
                    }

                    sb.Append($"{opcode}{less_offset}");

                    return;
            }
        }

        private static void Print(IAnnotatedBytecode annotatedBytecode, StringBuilder sb) {
            switch (annotatedBytecode) {
                case AnnotatedBytecodeInstruction annotatedBytecodeInstruction:
                    Print(annotatedBytecodeInstruction, sb);
                    break;
                case AnnotatedBytecodeVariable annotatedBytecodeVariable:
                    Print(annotatedBytecodeVariable, sb);
                    break;
                case AnnotatedBytecodeInteger annotatedBytecodeInteger:
                    Print(annotatedBytecodeInteger, sb);
                    break;
                case AnnotatedBytecodeFloat annotatedBytecodeFloat:
                    Print(annotatedBytecodeFloat, sb);
                    break;
                case AnnotatedBytecodeString annotatedBytecodeString:
                    Print(annotatedBytecodeString, sb);
                    break;
                case AnnotatedBytecodeArgumentType annotatedBytecodeArgumentType:
                    Print(annotatedBytecodeArgumentType, sb);
                    break;
                case AnnotatedBytecodeType annotatedBytecodeType:
                    Print(annotatedBytecodeType, sb);
                    break;
                case AnnotatedBytecodeTypeID annotatedBytecodeTypeID:
                    Print(annotatedBytecodeTypeID, sb);
                    break;
                case AnnotatedBytecodeProcID annotatedBytecodeProcID:
                    Print(annotatedBytecodeProcID, sb);
                    break;
                case AnnotatedBytecodeFormatCount annotatedBytecodeFormatCount:
                    Print(annotatedBytecodeFormatCount, sb);
                    break;
                case AnnotatedBytecodePickCount annotatedBytecodePickCount:
                    Print(annotatedBytecodePickCount, sb);
                    break;
                case AnnotatedBytecodeConcatCount annotatedBytecodeConcatCount:
                    Print(annotatedBytecodeConcatCount, sb);
                    break;
                case AnnotatedBytecodeStackDelta annotatedBytecodeStackDelta:
                    Print(annotatedBytecodeStackDelta, sb);
                    break;
                case AnnotatedBytecodeListSize annotatedBytecodeListSize:
                    Print(annotatedBytecodeListSize, sb);
                    break;
                case AnnotatedBytecodeResource annotatedBytecodeResource:
                    Print(annotatedBytecodeResource, sb);
                    break;
                case AnnotatedBytecodeLabel annotatedBytecodeLabel:
                    Print(annotatedBytecodeLabel, sb);
                    break;
                case AnnotatedBytecodeFilter annotatedBytecodeFilter:
                    Print(annotatedBytecodeFilter, sb);
                    break;
                case AnnotatedBytecodeReference @ref:
                    Print(@ref, sb);
                    break;
                default:
                    throw new Exception("Unknown annotated bytecode type");
            }
        }

        private static void Print(AnnotatedBytecodeInteger annotatedBytecode, StringBuilder sb) {
            sb.Append(annotatedBytecode.Value.ToString());
        }

        private static void Print(AnnotatedBytecodeFloat annotatedBytecode, StringBuilder sb) {
            sb.Append(annotatedBytecode.Value.ToString());
        }

        private static void Print(AnnotatedBytecodeString annotatedBytecode, StringBuilder sb) {
            sb.Append("\"").Append(StringFormatEncoder.PrettyPrint(annotatedBytecode.Value)).Append("\"");
        }

        private static void Print(AnnotatedBytecodeArgumentType annotatedBytecode, StringBuilder sb) {
            sb.Append(annotatedBytecode.Value.ToString());
        }

        private static void Print(AnnotatedBytecodeType annotatedBytecode, StringBuilder sb) {
            sb.Append(annotatedBytecode.Value.ToString());
        }

        private static void Print(AnnotatedBytecodeTypeID annotatedBytecode, StringBuilder sb) {
            // Print the type and the path if it's not null
            sb.Append(annotatedBytecode.TypeID.ToString());
            if (annotatedBytecode.Path is not null) sb.Append(" -> ").Append(annotatedBytecode.Path.ToString());
        }

        private static void Print(AnnotatedBytecodeProcID annotatedBytecode, StringBuilder sb) {
            // Print the proc and the path if it's not null
            sb.Append(annotatedBytecode.ProcID.ToString());
            if (annotatedBytecode.Path is not null) sb.Append(" -> ").Append(annotatedBytecode.Path.ToString());
        }

        private static void Print(AnnotatedBytecodeFormatCount annotatedBytecode, StringBuilder sb) {
            sb.Append("FormatCount ").Append(annotatedBytecode.Count);
        }

        private static void Print(AnnotatedBytecodePickCount annotatedBytecode, StringBuilder sb) {
            sb.Append("PickCount ").Append(annotatedBytecode.Count);
        }

        private static void Print(AnnotatedBytecodeConcatCount annotatedBytecode, StringBuilder sb) {
            sb.Append("ConcatCount ").Append(annotatedBytecode.Count);
        }

        private static void Print(AnnotatedBytecodeStackDelta annotatedBytecode, StringBuilder sb) {
            sb.Append("StackDelta ").Append(annotatedBytecode.Delta);
        }

        private static void Print(AnnotatedBytecodeListSize annotatedBytecode, StringBuilder sb) {
            sb.Append("ListSize ").Append(annotatedBytecode.Size);
        }

        private static void Print(AnnotatedBytecodeResource annotatedBytecode, StringBuilder sb) {
            sb.Append(annotatedBytecode.Value);
        }

        private static void Print(AnnotatedBytecodeLabel annotatedBytecode, StringBuilder sb) {
            sb.Append(annotatedBytecode.LabelName);
        }

        private static void Print(AnnotatedBytecodeFilter annotatedBytecode, StringBuilder sb) {
            sb.Append(annotatedBytecode.FilterTypeId.ToString()).Append(" -> ")
                .Append(annotatedBytecode.FilterPath.ToString());
        }

        private static void Print(AnnotatedBytecodeReference @ref, StringBuilder sb) {
            switch (@ref.RefType) {
                case DMReference.Type.Src:
                case DMReference.Type.Self:
                case DMReference.Type.Usr:
                case DMReference.Type.Args:
                case DMReference.Type.SuperProc:
                case DMReference.Type.ListIndex:
                    sb.Append(@ref.RefType.ToString());
                    break;
                case DMReference.Type.Argument:
                    sb.Append("Param ").Append(_input.Parameters[@ref.Index]);
                    break;
                case DMReference.Type.Local:
                    sb.Append("Local ").Append(localScopeVariables[@ref.Index]);
                    break;
                case DMReference.Type.Global:
                    sb.Append("Global ").Append(DMObjectTree.Globals[@ref.Index].Name);
                    break;
                case DMReference.Type.GlobalProc:
                    sb.Append("GlobalProc ").Append(DMObjectTree.AllProcs[@ref.Index].Name);
                    break;
                case DMReference.Type.Field:
                    sb.Append("Field ").Append(DMObjectTree.StringTable[@ref.Index]);
                    break;
                case DMReference.Type.SrcField:
                    sb.Append("SrcField ").Append(DMObjectTree.StringTable[@ref.Index]);
                    break;
                case DMReference.Type.SrcProc:
                    sb.Append("SrcProc ").Append(DMObjectTree.StringTable[@ref.Index]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(@ref.RefType), @ref.RefType,
                        null);
            }
        }
    }
}
