using System;
using System.Collections.Generic;
using System.IO;
using DMCompiler.Bytecode;
using OpenDreamShared.Compiler;

namespace DMCompiler.DM.Optimizer;

internal class AnnotatedBytecodeSerializer {
    private BinaryWriter _bytecodeWriter;
    private Dictionary<string, int> _labels = new();
    private List<(long Position, string LabelName)> _unresolvedLabels = new();
    public MemoryStream Bytecode = new();

    public AnnotatedBytecodeSerializer() {
        _bytecodeWriter = new BinaryWriter(Bytecode);
    }


    public byte[]? Serialize(List<IAnnotatedBytecode> annotatedBytecode) {
        foreach (IAnnotatedBytecode bytecodeChunk in annotatedBytecode) {
            if (bytecodeChunk is AnnotatedBytecodeInstruction instruction) {
                SerializeInstruction(instruction);
            } else if (bytecodeChunk is AnnotatedBytecodeLabel label) {
                SerializeLabel(label);
            } else {
                return null;
            }
        }

        ResolveLabels();
        return Bytecode.ToArray();
    }

    private void SerializeInstruction(AnnotatedBytecodeInstruction instruction) {
        _bytecodeWriter.Write((byte)instruction.Opcode);
        var opcodeMetadata = OpcodeMetadataCache.GetMetadata(instruction.Opcode);
        if (opcodeMetadata.RequiredArgs.Count != instruction.GetArgs().Count) {
            throw new System.Exception("Invalid number of arguments for opcode " + instruction.Opcode);
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
                int fieldID = DMObjectTree.AddString(reference.Name);
                _bytecodeWriter.Write(fieldID);
                break;

            case DMReference.Type.SrcProc:
            case DMReference.Type.SrcField:
                fieldID = DMObjectTree.AddString(reference.Name);
                _bytecodeWriter.Write(fieldID);
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
}
