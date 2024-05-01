using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;
using DMCompiler.Json;

namespace DMCompiler.DM.Optimizer;

internal class AnnotatedBytecodeSerializer {
    private readonly List<LocalVariableJson> _localVariables = new();
    private BinaryWriter _bytecodeWriter;
    private Dictionary<string, int> _labels = new();
    private List<(long Position, string LabelName)> _unresolvedLabels = new();
    private int lastFileID = -1;
    public MemoryStream Bytecode = new();

    private Location? location;

    public List<SourceInfoJson> SourceInfo = new();

    public AnnotatedBytecodeSerializer() {
        _bytecodeWriter = new BinaryWriter(Bytecode);
    }


    public byte[]? Serialize(List<IAnnotatedBytecode> annotatedBytecode) {
        foreach (IAnnotatedBytecode bytecodeChunk in annotatedBytecode) {
            if (bytecodeChunk is AnnotatedBytecodeInstruction instruction) {
                SerializeInstruction(instruction);
            } else if (bytecodeChunk is AnnotatedBytecodeLabel label) {
                SerializeLabel(label);
            } else if (bytecodeChunk is AnnotatedBytecodeVariable localVariable) {
                if (localVariable.Exitingscope)
                    _localVariables.Add(new LocalVariableJson {
                        Offset = (int)Bytecode.Position,
                        Remove = localVariable.Exit
                    });
                else
                    _localVariables.Add(new LocalVariableJson {
                        Offset = (int)Bytecode.Position,
                        Add = localVariable.Name
                    });
            } else {
                return null;
            }
        }

        ResolveLabels();
        // Sort and remove duplicates
        SourceInfo = SourceInfo.GroupBy(x => x.Line, (key, group) => group.First()).OrderBy(x => x.Offset).ToList();
        return Bytecode.ToArray();
    }


    private void SerializeInstruction(AnnotatedBytecodeInstruction instruction) {
        if (instruction.Location.Line != null && (location == null || instruction.Location.Line != location?.Line)) {
            int sourceFileId = DMObjectTree.AddString(instruction.Location.SourceFile);
            if (lastFileID != sourceFileId) {
                lastFileID = sourceFileId;
                SourceInfo.Add(new SourceInfoJson {
                    Offset = (int)Bytecode.Position,
                    File = sourceFileId,
                    Line = (int)instruction.Location.Line
                });
            } else {
                SourceInfo.Add(new SourceInfoJson {
                    Offset = (int)Bytecode.Position,
                    Line = (int)instruction.Location.Line
                });
            }

            location = instruction.Location;
        }

        _bytecodeWriter.Write((byte)instruction.Opcode);
        var opcodeMetadata = OpcodeMetadataCache.GetMetadata(instruction.Opcode);
        if (opcodeMetadata.RequiredArgs.Count != instruction.GetArgs().Count && !opcodeMetadata.VariableArgs) {
            throw new Exception("Invalid number of arguments for opcode " + instruction.Opcode);
        }

        var args = instruction.GetArgs();
        for (int i = 0; i < args.Count; i++) {
            switch (args[i]) {
                case AnnotatedBytecodeArgumentType annotatedBytecodeArgumentType:
                    _bytecodeWriter.Write((byte)annotatedBytecodeArgumentType.Value);
                    break;
                case AnnotatedBytecodeConcatCount annotatedBytecodeConcatCount:
                    _bytecodeWriter.Write(annotatedBytecodeConcatCount.Count);
                    break;
                case AnnotatedBytecodeFilter annotatedBytecodeFilter:
                    _bytecodeWriter.Write(annotatedBytecodeFilter.FilterTypeId);
                    break;
                case AnnotatedBytecodeFloat annotatedBytecodeFloat:
                    _bytecodeWriter.Write(annotatedBytecodeFloat.Value);
                    break;
                case AnnotatedBytecodeFormatCount annotatedBytecodeFormatCount:
                    _bytecodeWriter.Write(annotatedBytecodeFormatCount.Count);
                    break;
                case AnnotatedBytecodeInteger annotatedBytecodeInteger:
                    _bytecodeWriter.Write(annotatedBytecodeInteger.Value);
                    break;
                case AnnotatedBytecodeLabel annotatedBytecodeLabel:
                    _unresolvedLabels.Add((Bytecode.Position, annotatedBytecodeLabel.LabelName));
                    _bytecodeWriter.Write(0);
                    break;
                case AnnotatedBytecodeListSize annotatedBytecodeListSize:
                    _bytecodeWriter.Write(annotatedBytecodeListSize.Size);
                    break;
                case AnnotatedBytecodePickCount annotatedBytecodePickCount:
                    _bytecodeWriter.Write(annotatedBytecodePickCount.Count);
                    break;
                case AnnotatedBytecodeProcID annotatedBytecodeProcId:
                    _bytecodeWriter.Write(annotatedBytecodeProcId.ProcID);
                    break;
                case AnnotatedBytecodeReference annotatedBytecodeReference:
                    WriteReference(annotatedBytecodeReference);
                    break;
                case AnnotatedBytecodeResource annotatedBytecodeResource:
                    _bytecodeWriter.Write(annotatedBytecodeResource.ResourceID);
                    break;
                case AnnotatedBytecodeStackDelta annotatedBytecodeStackDelta:
                    _bytecodeWriter.Write(annotatedBytecodeStackDelta.Delta);
                    break;
                case AnnotatedBytecodeString annotatedBytecodeString:
                    _bytecodeWriter.Write(annotatedBytecodeString.ID);
                    break;
                case AnnotatedBytecodeType annotatedBytecodeType:
                    _bytecodeWriter.Write((int)annotatedBytecodeType.Value);
                    break;
                case AnnotatedBytecodeTypeID annotatedBytecodeTypeId:
                    _bytecodeWriter.Write(annotatedBytecodeTypeId.TypeID);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void SerializeLabel(AnnotatedBytecodeLabel label) {
        _labels.TryAdd(label.LabelName, (int)Bytecode.Position);
    }

    private void ResolveLabels() {
        foreach ((long Position, string LabelName) in _unresolvedLabels) {
            if (_labels.TryGetValue(LabelName, out int labelPosition)) {
                _bytecodeWriter.Seek((int)Position, SeekOrigin.Begin);
                _bytecodeWriter.Write((int)labelPosition);
            } else {
                DMCompiler.Emit(WarningCode.BadLabel, Location.Internal,
                    "Label \"" + LabelName + "\" could not be resolved");
            }
        }

        _unresolvedLabels.Clear();
        _bytecodeWriter.Seek(0, SeekOrigin.End);
    }

    public void WriteReference(AnnotatedBytecodeReference reference) {
        _bytecodeWriter.Write((byte)reference.RefType);
        switch (reference.RefType) {
            case DMReference.Type.Argument:
            case DMReference.Type.Local:
                _bytecodeWriter.Write((byte)reference.Index);
                break;


            case DMReference.Type.Global:
            case DMReference.Type.GlobalProc:
                _bytecodeWriter.Write(reference.Index);
                break;


            case DMReference.Type.Field:
                _bytecodeWriter.Write(reference.Index);
                break;

            case DMReference.Type.SrcProc:
            case DMReference.Type.SrcField:
                _bytecodeWriter.Write(reference.Index);
                break;

            case DMReference.Type.ListIndex:
            case DMReference.Type.SuperProc:
            case DMReference.Type.Src:
            case DMReference.Type.Self:
            case DMReference.Type.Args:
            case DMReference.Type.Usr:
                break;

            default:
                throw new CompileAbortException(Location.Internal, $"Invalid reference type {reference.RefType}");
        }
    }

    public List<LocalVariableJson> GetLocalVariablesJSON() {
        return _localVariables;
    }
}
