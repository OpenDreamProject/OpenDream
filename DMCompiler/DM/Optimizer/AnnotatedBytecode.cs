using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DMCompiler.Bytecode;
using DMCompiler.Compiler.DMPreprocessor;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using Robust.Shared.Utility;

namespace DMCompiler.DM.Optimizer {

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
        public int ID;
        public Location Location;

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
        public int Size;
        public Location Location;

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
        public string Value;
        public int ResourceID;
        public Location Location;

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
        public static string Print(IReadOnlyList<IAnnotatedBytecode> annotatedBytecode, List<SourceInfoJson> sourceInfo)
        {
            StringBuilder output = new StringBuilder();
            int indent = 1;

            int currentLine = 0;

            Dictionary<int, SourceInfoJson> sourceInfoByLine = new();

            string currentFile = "";

            foreach (SourceInfoJson sourceInfoItem in sourceInfo)
            {
                if (sourceInfoItem.File != null)
                {
                    currentFile = DMObjectTree.StringTable[sourceInfoItem.File.Value];
                }
                sourceInfoByLine.TryAdd(sourceInfoItem.Line, sourceInfoItem);
            }

            List<string> currentFileContents = new();
            if (currentFile != "" && currentFile != oldFile)
            {
                if (!File.Exists(currentFile))
                {
                    currentFile = Path.Join(DMCompiler.StandardLibraryDirectory, currentFile);
                }

                if (!File.Exists(currentFile))
                {
                    currentFile = "";
                }
                else
                {
                    currentFileContents = File.ReadLines(currentFile).ToList();
                }
            }
            if (currentFile != oldFile)
            {
                oldFile = currentFile;
                oldFileContents = currentFileContents;
            } else {
                currentFileContents = oldFileContents;
            }
            foreach (IAnnotatedBytecode annotatedBytecodeItem in annotatedBytecode)
            {
                switch (annotatedBytecodeItem)
                {
                    case AnnotatedBytecodeInstruction annotatedBytecodeInstruction:
                        if (annotatedBytecodeInstruction.Location.Line != currentLine)
                        {
                            currentLine = (annotatedBytecodeInstruction.Location.Line) ?? -1;
                            if (sourceInfoByLine.ContainsKey(currentLine) && currentFile != "" && currentLine - 1 < currentFileContents.Count)
                            {
                                output.Append(" // ").Append(currentFileContents[currentLine - 1].Trim()).Append("\n");
                            }
                        }
                        // For now all this does is keep labels flush with the left side
                        // Later may do something different e.g basic blocks
                        output.Append(' ', indent * 4).Append(Print(annotatedBytecodeInstruction)).Append("\n");
                        break;
                    case AnnotatedBytecodeLabel label:
                        output.Append(label.LabelName).Append(":").Append("\n");
                        break;
                    default:
                        throw new Exception("Only labels and instructions allowed at top-level, others should be args to instructions");
                }
            }

            return output.ToString();
        }

        private static int max_opcode_length = -1;

        private static string Print(AnnotatedBytecodeInstruction annotatedBytecode) {
            DreamProcOpcode opcode = annotatedBytecode.Opcode;
            if (max_opcode_length == -1) {
                max_opcode_length = Enum.GetNames(typeof(DreamProcOpcode)).Max(x => x.Length);
            }
            // If the length of the opcode name is less than the max opcode length, add spaces to the end of the opcode name
            string less_offset = new string(' ', max_opcode_length - opcode.ToString().Length) + "\t";
            switch (opcode) {
                case DreamProcOpcode.FormatString:
                    // FormatString takes an unformatted string and the interpolation count to pop off the stack
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeString)!)}, {Print((annotatedBytecode.GetArgs()[1] as AnnotatedBytecodeFormatCount)!)}";
                case DreamProcOpcode.PushString:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeString)!)}";
                case DreamProcOpcode.PushResource:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeResource)!)}";
                case DreamProcOpcode.DereferenceField:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeString)!)}";
                case DreamProcOpcode.DereferenceCall:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeString)!)}, {Print((annotatedBytecode.GetArgs()[1] as AnnotatedBytecodeArgumentType)!)}, {Print((annotatedBytecode.GetArgs()[2] as AnnotatedBytecodeStackDelta)!)}";
                case DreamProcOpcode.Prompt:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeType)!)}";

                case DreamProcOpcode.PushFloat:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeFloat)!)}";

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
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeReference)!)}";

                case DreamProcOpcode.Input:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeReference)!)}, {Print((annotatedBytecode.GetArgs()[1] as AnnotatedBytecodeReference)!)}";

                case DreamProcOpcode.CallStatement:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeArgumentType)!)}, {Print((annotatedBytecode.GetArgs()[1] as AnnotatedBytecodeStackDelta)!)}";
                case DreamProcOpcode.CreateObject:
                case DreamProcOpcode.Gradient:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeArgumentType)!)}, {Print((annotatedBytecode.GetArgs()[1] as AnnotatedBytecodeStackDelta)!)}";
                case DreamProcOpcode.Call:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeReference)!)}, {Print((annotatedBytecode.GetArgs()[1] as AnnotatedBytecodeArgumentType)!)}, {Print((annotatedBytecode.GetArgs()[2] as AnnotatedBytecodeStackDelta)!)}";

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
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeLabel)!)}";
                case DreamProcOpcode.CreateList:
                case DreamProcOpcode.CreateAssociativeList:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeListSize)!)}";
                case DreamProcOpcode.CreateFilteredListEnumerator:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeFilter)!)}";
                case DreamProcOpcode.PickWeighted:
                case DreamProcOpcode.PickUnweighted:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodePickCount)!)}";
                case DreamProcOpcode.PushType:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeTypeID)!)}";
                case DreamProcOpcode.PushProc:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeProcID)!)}";
                case DreamProcOpcode.MassConcatenation:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeConcatCount)!)}";

                case DreamProcOpcode.JumpIfNullDereference:
                case DreamProcOpcode.JumpIfTrueReference:
                case DreamProcOpcode.JumpIfFalseReference:
                case DreamProcOpcode.Enumerate:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeReference)!)}, {Print((annotatedBytecode.GetArgs()[1] as AnnotatedBytecodeLabel)!)}";

                case DreamProcOpcode.Try:
                    return $"{opcode}{less_offset} {Print((annotatedBytecode.GetArgs()[0] as AnnotatedBytecodeLabel)!)}, {Print((annotatedBytecode.GetArgs()[1] as AnnotatedBytecodeReference)!)}";

                default:
                    if (annotatedBytecode.GetArgs().Count > 0) {
                        throw new Exception($"UH OH YOU FORGOT TO ADD A CASE FOR {opcode}");
                    }
                    return opcode.ToString();
            }

            throw new Exception($"Unknown opcode {opcode}");
        }

        private static string Print(AnnotatedBytecodeInteger annotatedBytecode) {
            return annotatedBytecode.Value.ToString();
        }

        private static string Print(AnnotatedBytecodeFloat annotatedBytecode) {
            return annotatedBytecode.Value.ToString();
        }

        private static string Print(AnnotatedBytecodeString annotatedBytecode) {
            return "\"" + StringFormatEncoder.PrettyPrint(annotatedBytecode.Value) + "\"";
        }

        private static string Print(AnnotatedBytecodeArgumentType annotatedBytecode) {
            return annotatedBytecode.Value.ToString();
        }

        private static string Print(AnnotatedBytecodeType annotatedBytecode) {
            return annotatedBytecode.Value.ToString();
        }

        private static string Print(AnnotatedBytecodeTypeID annotatedBytecode) {
            // Print the type and the path if it's not null
            return annotatedBytecode.TypeID.ToString() +
                   (annotatedBytecode.Path is not null ? " -> " + annotatedBytecode.Path.ToString() : "");
        }

        private static string Print(AnnotatedBytecodeProcID annotatedBytecode) {
            // Print the proc and the path if it's not null
            return annotatedBytecode.ProcID.ToString() +
                   (annotatedBytecode.Path is not null ? " -> " + annotatedBytecode.Path.ToString() : "");
        }

        private static string Print(AnnotatedBytecodeFormatCount annotatedBytecode) {
            return $"FormatCount {annotatedBytecode.Count}";
        }

        private static string Print(AnnotatedBytecodePickCount annotatedBytecode) {
            return $"PickCount {annotatedBytecode.Count}";
        }

        private static string Print(AnnotatedBytecodeConcatCount annotatedBytecode) {
            return $"ConcatCount {annotatedBytecode.Count}";
        }

        private static string Print(AnnotatedBytecodeStackDelta annotatedBytecode) {
            return $"StackDelta {annotatedBytecode.Delta}";
        }

        private static string Print(AnnotatedBytecodeListSize annotatedBytecode) {
            return $"ListSize {annotatedBytecode.Size}";
        }

        private static string Print(AnnotatedBytecodeResource annotatedBytecode) {
            return annotatedBytecode.Value;
        }

        private static string Print(AnnotatedBytecodeLabel annotatedBytecode) {
            return annotatedBytecode.LabelName;
        }

        private static string Print(AnnotatedBytecodeFilter annotatedBytecode) {
            return annotatedBytecode.FilterTypeId.ToString() + " -> " + annotatedBytecode.FilterPath.ToString();
        }

        private static string Print(AnnotatedBytecodeReference annotatedBytecode) {
            return annotatedBytecode.RefType.ToString() + " " + annotatedBytecode.Index.ToString() + " -> " + annotatedBytecode.Name;
        }

    }
}
