using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DMCompiler.Bytecode;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;

namespace DMCompiler.DM.Optimizer {
    public interface IAnnotatedBytecode {
        public void AddArg(IAnnotatedBytecode arg);
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

        public void AddArg(IAnnotatedBytecode arg) {
            _args.Add(arg);
        }

        public List<IAnnotatedBytecode> GetArgs() {
            return _args;
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
    }

    internal class AnnotatedBytecodeReference : IAnnotatedBytecode {
        public int Index;
        public Location Location;
        public string Name;
        public DMReference.Type RefType;

        public AnnotatedBytecodeReference(DMReference.Type refType, int index, string name, Location location) {
            RefType = refType;
            Index = index;
            Name = name;
            Location = location;
        }

        public AnnotatedBytecodeReference(DMReference.Type refType, string name, Location location) {
            RefType = refType;
            Location = location;
            Name = name;
        }

        public void AddArg(IAnnotatedBytecode arg) {
            DMCompiler.ForcedError(Location, "Cannot add args to a reference");
        }
    }

    internal class AnnotatedBytecodePrinter {
        private static string oldFile = "";
        private static List<string> oldFileContents = new();

        private static int max_opcode_length = -1;
        private static readonly List<string> indent_cache = new();

        public static void Print(IReadOnlyList<IAnnotatedBytecode> annotatedBytecode, List<SourceInfoJson> sourceInfo,
            StringBuilder output, int indent = 1) {
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
            if (currentFile != "" && currentFile != oldFile) {
                if (!File.Exists(currentFile)) {
                    currentFile = Path.Join(DMCompiler.StandardLibraryDirectory, currentFile);
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

            if (annotatedBytecode.Count == 0) {
                output.Append("\t pass\n");
                return;
            }

            List<string> localScopeVariables = new();
            foreach (IAnnotatedBytecode annotatedBytecodeItem in annotatedBytecode) {
                switch (annotatedBytecodeItem) {
                    case AnnotatedBytecodeInstruction annotatedBytecodeInstruction:
                        if (annotatedBytecodeInstruction.Location.Line != currentLine) {
                            currentLine = (annotatedBytecodeInstruction.Location.Line) ?? -1;
                            if (sourceInfoByLine.ContainsKey(currentLine) && currentFile != "" &&
                                currentLine - 1 < currentFileContents.Count) {
                                output.Append(' ', indent * 4).Append(" // ")
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
                        }

                        break;
                    default:
                        throw new Exception(
                            "Only labels and instructions allowed at top-level, others should be args to instructions");
                }
            }
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
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeString)!, sb);
                    sb.Append(", ");
                    Print((annotatedBytecode.GetArgs()[1] as AnnotatedBytecodeFormatCount)!, sb);
                    break;
                case DreamProcOpcode.PushString:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeString)!, sb);
                    break;
                case DreamProcOpcode.PushResource:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeResource)!, sb);
                    break;
                case DreamProcOpcode.DereferenceField:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeString)!, sb);
                    break;
                case DreamProcOpcode.DereferenceCall:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeString)!, sb);
                    sb.Append(", ");
                    Print((annotatedBytecode.GetArgs()[1] as AnnotatedBytecodeArgumentType)!, sb);
                    sb.Append(", ");
                    Print((annotatedBytecode.GetArgs()[2] as AnnotatedBytecodeStackDelta)!, sb);
                    break;
                case DreamProcOpcode.Prompt:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeType)!, sb);
                    break;

                case DreamProcOpcode.PushFloat:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeFloat)!, sb);
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
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeReference)!, sb);
                    break;

                case DreamProcOpcode.Input:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeReference)!, sb);
                    sb.Append(", ");
                    Print((annotatedBytecode.GetArgs()[1] as AnnotatedBytecodeReference)!, sb);
                    break;

                case DreamProcOpcode.CallStatement:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeArgumentType)!, sb);
                    sb.Append(", ");
                    Print((annotatedBytecode.GetArgs()[1] as AnnotatedBytecodeStackDelta)!, sb);
                    break;
                case DreamProcOpcode.CreateObject:
                case DreamProcOpcode.Gradient:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeArgumentType)!, sb);
                    sb.Append(", ");
                    Print((annotatedBytecode.GetArgs()[1] as AnnotatedBytecodeStackDelta)!, sb);
                    break;
                case DreamProcOpcode.Call:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeReference)!, sb);
                    sb.Append(", ");
                    Print((annotatedBytecode.GetArgs()[1] as AnnotatedBytecodeArgumentType)!, sb);
                    sb.Append(", ");
                    Print((annotatedBytecode.GetArgs()[2] as AnnotatedBytecodeStackDelta)!, sb);
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
                case DreamProcOpcode.JumpIfNullNoPop:
                case DreamProcOpcode.TryNoValue:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeLabel)!, sb);
                    break;
                case DreamProcOpcode.CreateList:
                case DreamProcOpcode.CreateAssociativeList:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeListSize)!, sb);
                    break;
                case DreamProcOpcode.CreateFilteredListEnumerator:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeFilter)!, sb);
                    break;
                case DreamProcOpcode.PickWeighted:
                case DreamProcOpcode.PickUnweighted:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodePickCount)!, sb);
                    break;
                case DreamProcOpcode.PushType:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeTypeID)!, sb);
                    break;
                case DreamProcOpcode.PushProc:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeProcID)!, sb);
                    break;
                case DreamProcOpcode.MassConcatenation:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeConcatCount)!, sb);
                    break;

                case DreamProcOpcode.JumpIfNullDereference:
                case DreamProcOpcode.JumpIfTrueReference:
                case DreamProcOpcode.JumpIfFalseReference:
                case DreamProcOpcode.Enumerate:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeReference)!, sb);
                    sb.Append(", ");
                    Print((annotatedBytecode.GetArgs()[1] as AnnotatedBytecodeLabel)!, sb);
                    break;

                case DreamProcOpcode.Try:
                    sb.Append($"{opcode}{less_offset} ");
                    Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeLabel)!, sb);
                    sb.Append(", ");
                    Print((annotatedBytecode.GetArgs()[1] as AnnotatedBytecodeReference)!, sb);
                    break;

                default:
                    if (annotatedBytecode.GetArgs().Count > 0) {
                        throw new Exception($"UH OH YOU FORGOT TO ADD A CASE FOR {opcode}");
                    }

                    sb.Append($"{opcode}{less_offset}");

                    return;
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

        private static void Print(AnnotatedBytecodeReference annotatedBytecode, StringBuilder sb) {
            sb.Append(annotatedBytecode.RefType.ToString()).Append(" ").Append(annotatedBytecode.Index.ToString())
                .Append(" -> ").Append(annotatedBytecode.Name);
        }
    }
}
