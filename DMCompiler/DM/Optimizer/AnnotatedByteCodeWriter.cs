using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DMCompiler.Bytecode;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream;

namespace DMCompiler.DM.Optimizer {

/*
 * Provides a wrapper about BinaryWriter that stores information about the bytecode
 * for optimization and debugging.
 */
    internal class AnnotatedByteCodeWriter : BinaryWriter {
        private Location _location = new();
        private readonly List<(long Position, string LabelName)> _unresolvedLabels = new();
        private readonly List<(long Position, string LabelName)> _unresolvedLabelsInAnnotatedBytecode = new();
        private Stack<OpcodeArgType> _requiredArgs = new();
        private readonly List<IAnnotatedBytecode> _annotatedBytecode = new();
        private int _maxStackSize;
        private int _currentStackSize;
        private bool _negativeStackSizeError;

        public AnnotatedByteCodeWriter(MemoryStream bytecode) : base(bytecode) {
        }

        public AnnotatedByteCodeWriter() {
            OutStream = new MemoryStream();
        }


        /// <summary>
        /// Writes an opcode to the stream
        /// </summary>
        /// <param name="opcode">The opcode to write</param>
        /// <param name="location">The location of the opcode in the source code</param>
        public void WriteOpcode(DreamProcOpcode opcode, Location location) {
            _location = location;
            if (_requiredArgs.Count > 0) {
                DMCompiler.ForcedError(location, "Expected argument");
            }
            Write((byte)opcode);
            var metadata = OpcodeMetadataCache.GetMetadata(opcode);
            // Goal here is to maintain correspondence between the raw bytecode and the annotated bytecode such that
            // the annotated bytecode can be used to generate the raw bytecode again.
            _annotatedBytecode.Add(new AnnotatedBytecodeInstruction(opcode, metadata.StackDelta, location));


            ResizeStack(metadata.StackDelta);

            _requiredArgs = new Stack<OpcodeArgType>(metadata.RequiredArgs.AsEnumerable().Reverse());
        }

        /// <summary>
        /// Writes an integer to the stream
        /// </summary>
        /// <param name="val">The integer to write</param>
        /// <param name="location">The location of the integer in the source code</param>
        public void WriteInt(int val, Location location) {
            _location = location;
            if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.Int) {
                DMCompiler.ForcedError(location, "Expected integer argument");
            }
            _requiredArgs.Pop();
            _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeInteger(val, location));
            Write(val);
        }

        /// <summary>
        /// Writes a float to the stream
        /// </summary>
        /// <param name="val">The integer to write</param>
        /// <param name="location">The location of the integer in the source code</param>
        public void WriteFloat(float val, Location location) {
            _location = location;
            if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.Float) {
                DMCompiler.ForcedError(location, "Expected floating argument");
            }
            _requiredArgs.Pop();
            _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeFloat(val, location));
            Write(val);
        }

        /// <summary>
        /// Writes argument classification to the stream
        /// </summary>
        /// <param name="argtype">The argument type to write</param>
        /// <param name="location">The location of the integer in the source code</param>
        public void WriteArgumentType(DMCallArgumentsType argtype, Location location) {
            _location = location;
            if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.ArgType) {
                DMCompiler.ForcedError(location, "Expected argument type argument");
            }
            _requiredArgs.Pop();
            _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeArgumentType(argtype, location));
            Write((byte)argtype);
        }

        /// <summary>
        /// Write a stack delta to the stream
        /// </summary>
        /// <param name="delta">The stack delta to write</param>
        /// <param name="location">The location of the integer in the source code</param>
        public void WriteStackDelta(int delta, Location location) {
            _location = location;
            if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.StackDelta) {
                DMCompiler.ForcedError(location, "Expected stack delta argument");
            }
            _requiredArgs.Pop();
            _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeInteger(delta, location));
            Write(delta);
        }

        /// <summary>
        /// Write a type to the stream
        /// </summary>
        /// <param name="type">The type to write</param>
        /// <param name="location">The location of the type in the source code</param>
        public void WriteType(DMValueType type, Location location) {
            if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.TypeId) {
                DMCompiler.ForcedError(location, "Expected type argument");
            }

            _requiredArgs.Pop();

            _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeType(type, location));
            Write((int)type);

        }


        /// <summary>
        /// Writes a string to the stream and stores it in the string table
        /// </summary>
        /// <param name="value">The string to write</param>
        /// <param name="location">The location of the string in the source code</param>
        public void WriteString(string value, Location location) {
            _location = location;
            if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.String) {
                DMCompiler.ForcedError(location, "Expected string argument");
            }
            _requiredArgs.Pop();
            _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeString(value, location));
            int stringId = DMObjectTree.AddString(value);

            Write(stringId);
        }

        /// <summary>
        /// Write a filter. Filters are stored as reference IDs in the raw bytecode, which refer
        /// to a string in the string table containing the datum path of the filter.
        /// </summary>
        /// <param name="filterTypeId">The type ID of the filter</param>
        /// <param name="filterPath">The datum path of the filter</param>
        /// <param name="location">The location of the filter in the source code</param>
        ///
        public void WriteFilterID(int filterTypeId, DreamPath filterPath, Location location) {
            _location = location;

            if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.FilterId) {
                DMCompiler.ForcedError(location, "Expected filter argument");
            }

            _requiredArgs.Pop();

            _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeFilter(filterTypeId, filterPath, location));
            Write(filterTypeId);
        }

        /// <summary>
        /// Write a list size, restricted to non-negative integers
        /// </summary>
        /// <param name="value">The size of the list</param>
        /// <param name="location">The location of the list in the source code</param>
        public void WriteListSize(int value, Location location) {
            _location = location;
            if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.ListSize) {
                DMCompiler.ForcedError(location, "Expected list size argument");
            }
            if (value < 0) {
                DMCompiler.ForcedError(location, "List size cannot be negative");
            }
            _requiredArgs.Pop();
            _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeInteger(value, location));
            Write(value);
        }

        /// <summary>
        /// Writes a label to the stream
        /// </summary>
        /// <param name="s">The label to write</param>
        /// <param name="location">The location of the label in the source code</param>
        public void WriteLabel(string s, Location location) {
            _location = location;
            if (_requiredArgs.Count == 0 || _requiredArgs.Pop() != OpcodeArgType.Label) {
                DMCompiler.ForcedError(location, "Expected label argument");
            }
            _unresolvedLabels.Add((OutStream.Position, s));
            _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeLabel(s, location));
            _unresolvedLabelsInAnnotatedBytecode.Add((_annotatedBytecode.Count - 1, s));
            Write(0);
        }

        public void UpdateLabel(int unresolvedLabelPosition, long newPos) {
            OutStream.Seek(unresolvedLabelPosition, SeekOrigin.Begin);
            Write((int)newPos);
            OutStream.Seek(0, SeekOrigin.End);
        }


        public void UpdateLabelInAnnotatedBytecode(string labelName) {
            // For the annotated bytecode, labels are explicitly stored as a string and not as a position
            // So we just need to push the position of the label onto the code
            _annotatedBytecode.Add(new AnnotatedBytecodeLabel(labelName, _location));
        }

        public void ResolveCodeLabelReferences(Stack<DMProc.CodeLabelReference> pendingLabelReferences, ref Dictionary<string, long> labels) {
            while (pendingLabelReferences.Count > 0) {
                DMProc.CodeLabelReference reference = pendingLabelReferences.Pop();
                DMProc.CodeLabel? label = GetCodeLabel(reference.Identifier, reference.Scope);

                // Failed to find the label in the given context
                if (label == null) {
                    DMCompiler.Emit(
                        WarningCode.ItemDoesntExist,
                        reference.Location,
                        $"Label \"{reference.Identifier}\" unreachable from scope or does not exist"
                    );
                    // Not cleaning away the placeholder will emit another compiler error
                    // let's not do that
                    _unresolvedLabels.RemoveAt(
                        _unresolvedLabels.FindIndex(((long Position, string LabelName) o) =>
                            o.LabelName == reference.Placeholder)
                    );
                    continue;
                }

                // Found it.
                labels.Add(reference.Placeholder, label.ByteOffset);
                label.ReferencedCount += 1;

                // I was thinking about going through to replace all the placeholers
                // with the actual label.LabelName, but it means I need to modify
                // _unresolvedLabels, being a list of tuple objects. Fuck that noise
            }

            // TODO: Implement "unused label" like in BYOND DM, use label.ReferencedCount to figure out
            // foreach (CodeLabel codeLabel in CodeLabels) {
            //  ...
            // }
        }

        public void ResolveLabels(Stack<DMProc.CodeLabelReference> pendingLabelReferences, ref Dictionary<string, long> labels) {
            ResolveCodeLabelReferences(pendingLabelReferences, ref labels);
            foreach ((long Position, string LabelName) unresolvedLabel in _unresolvedLabels) {
                if (labels.TryGetValue(unresolvedLabel.LabelName, out long labelPosition)) {
                    UpdateLabel((int)unresolvedLabel.Position, labelPosition);
                    UpdateLabelInAnnotatedBytecode(unresolvedLabel.LabelName);
                } else {
                    DMCompiler.Emit(WarningCode.BadLabel, _location,
                        "Label \"" + unresolvedLabel.LabelName + "\" could not be resolved");
                }
            }

            _unresolvedLabels.Clear();
            Seek(0, SeekOrigin.End);
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
                DMCompiler.ForcedError(_location, "Negative stack size");
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
                DMCompiler.ForcedError(location, "Expected resource argument");

            }
            _requiredArgs.Pop();
            _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeResource(value, location));
            int stringId = DMObjectTree.AddString(value);

            Write(stringId);
        }

        public void WriteTypeId(int typeId, DreamPath? path, Location location)
        {
            _location = location;
            if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.TypeId) {
                DMCompiler.ForcedError(location, "Expected TypeID argument");
            }
            _requiredArgs.Pop();
            _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeTypeID(typeId, path, location));
            Write(typeId);
        }


        public void WriteProcId(int procId, DreamPath? path, Location location) {
            _location = location;
            if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.ProcId) {
                DMCompiler.ForcedError(location, "Expected ProcID argument");
            }
            _requiredArgs.Pop();
            _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeProcID(procId, path, location));
            Write(procId);
        }

        public void WriteFormatCount(int formatCount, Location location) {
            _location = location;
            if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.FormatCount) {
                DMCompiler.ForcedError(location, "Expected format count argument");
            }
            _requiredArgs.Pop();
            _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeFormatCount(formatCount, location));
            Write(formatCount);

        }

        public void WritePickCount(int count, Location location) {
            _location = location;
            if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.PickCount) {
                DMCompiler.ForcedError(location, "Expected pick count argument");
            }
            _requiredArgs.Pop();
            _annotatedBytecode[^1].AddArg(new AnnotatedBytecodePickCount(count, location));
            Write(count);
        }

        public void WriteConcatCount(int count, Location location) {
            _location = location;
            if (_requiredArgs.Count == 0 || _requiredArgs.Peek() != OpcodeArgType.ConcatCount) {
                DMCompiler.ForcedError(location, "Expected concat count argument");
            }
            _requiredArgs.Pop();
            _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeConcatCount(count, location));
            Write(count);
        }



        public void WriteReference(DMReference reference, Location location, bool affectStack = true) {
            _location = location;
            if (_requiredArgs.Count == 0 || _requiredArgs.Pop() != OpcodeArgType.Reference) {
                DMCompiler.ForcedError(location, "Expected reference argument");
            }
            Write((byte)reference.RefType);
            switch (reference.RefType) {
                case DMReference.Type.Argument:
                case DMReference.Type.Local:
                    Write((byte)reference.Index);
                    _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeReference(reference.RefType, reference.Index, location));
                    break;


                case DMReference.Type.Global:
                case DMReference.Type.GlobalProc:
                    Write(reference.Index);
                    _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeReference(reference.RefType, reference.Index, location));
                    break;


                case DMReference.Type.Field:
                    int fieldID = DMObjectTree.AddString(reference.Name);
                    Write(fieldID);
                    _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeReference(reference.RefType, fieldID, location));
                    ResizeStack(affectStack ? -1 : 0);
                    break;

                case DMReference.Type.SrcProc:
                case DMReference.Type.SrcField:
                    fieldID = DMObjectTree.AddString(reference.Name);
                    Write(fieldID);
                    _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeReference(reference.RefType, fieldID, location));
                    break;

                case DMReference.Type.ListIndex:
                    _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeReference(reference.RefType, location));
                    ResizeStack(affectStack ? -2 : 0);
                    break;

                case DMReference.Type.SuperProc:
                case DMReference.Type.Src:
                case DMReference.Type.Self:
                case DMReference.Type.Args:
                case DMReference.Type.Usr:
                    _annotatedBytecode[^1].AddArg(new AnnotatedBytecodeReference(reference.RefType, location));
                    break;

                default:
                    throw new CompileAbortException(location, $"Invalid reference type {reference.RefType}");
            }
        }
    }
}
