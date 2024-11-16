using System.IO;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;
using DMCompiler.Json;

namespace DMCompiler.Optimizer;

internal class AnnotatedBytecodeSerializer(DMCompiler compiler) {
    private readonly List<LocalVariableJson> _localVariables = new();
    private BinaryWriter? _bytecodeWriter;
    private Dictionary<string, int> _labels = new();
    private List<(long Position, string LabelName)> _unresolvedLabels = new();
    private int _lastFileId = -1;
    public MemoryStream Bytecode = new();

    private Location? _location;

    public List<SourceInfoJson> SourceInfo = new();

    public byte[]? Serialize(List<IAnnotatedBytecode> annotatedBytecode) {
        _bytecodeWriter ??= new BinaryWriter(Bytecode);
        foreach (IAnnotatedBytecode bytecodeChunk in annotatedBytecode) {
            if (bytecodeChunk is AnnotatedBytecodeInstruction instruction) {
                SerializeInstruction(instruction);
            } else if (bytecodeChunk is AnnotatedBytecodeLabel label) {
                SerializeLabel(label);
            } else if (bytecodeChunk is AnnotatedBytecodeVariable localVariable) {
                if (localVariable.ExitingScope)
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

        // Remove duplicates
        var uniqueSourceInfo = new HashSet<SourceInfoJson>(SourceInfo, new LineComparer());
        // Go back to a list and sort
        var sortedSourceInfo = new List<SourceInfoJson>(uniqueSourceInfo);
        sortedSourceInfo.Sort((x, y) => x.Offset.CompareTo(y.Offset));
        // Assign the sorted list back to SourceInfo
        SourceInfo = sortedSourceInfo;

        return Bytecode.ToArray();
    }

    private void SerializeInstruction(AnnotatedBytecodeInstruction instruction) {
        _bytecodeWriter ??= new BinaryWriter(Bytecode);
        if (instruction.Location.Line != null && (_location == null || instruction.Location.Line != _location?.Line)) {
            int sourceFileId = compiler.DMObjectTree.AddString(instruction.Location.SourceFile);
            if (_lastFileId != sourceFileId) {
                _lastFileId = sourceFileId;
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

            _location = instruction.Location;
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
                case AnnotatedBytecodeProcId annotatedBytecodeProcId:
                    _bytecodeWriter.Write(annotatedBytecodeProcId.ProcId);
                    break;
                case AnnotatedBytecodeEnumeratorId annotatedBytecodeEnumeratorId:
                    _bytecodeWriter.Write(annotatedBytecodeEnumeratorId.EnumeratorId);
                    break;
                case AnnotatedBytecodeReference annotatedBytecodeReference:
                    WriteReference(annotatedBytecodeReference);
                    break;
                case AnnotatedBytecodeResource annotatedBytecodeResource:
                    _bytecodeWriter.Write(annotatedBytecodeResource.ResourceId);
                    break;
                case AnnotatedBytecodeStackDelta annotatedBytecodeStackDelta:
                    _bytecodeWriter.Write(annotatedBytecodeStackDelta.Delta);
                    break;
                case AnnotatedBytecodeString annotatedBytecodeString:
                    _bytecodeWriter.Write(annotatedBytecodeString.Id);
                    break;
                case AnnotatedBytecodeType annotatedBytecodeType:
                    _bytecodeWriter.Write((int)annotatedBytecodeType.Value);
                    break;
                case AnnotatedBytecodeTypeId annotatedBytecodeTypeId:
                    _bytecodeWriter.Write(annotatedBytecodeTypeId.TypeId);
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
        _bytecodeWriter ??= new BinaryWriter(Bytecode);
        foreach ((long position, string labelName) in _unresolvedLabels) {
            if (_labels.TryGetValue(labelName, out int labelPosition)) {
                _bytecodeWriter.Seek((int)position, SeekOrigin.Begin);
                _bytecodeWriter.Write((int)labelPosition);
            } else {
                compiler.Emit(WarningCode.BadLabel, Location.Internal,
                    "Label \"" + labelName + "\" could not be resolved");
            }
        }

        _unresolvedLabels.Clear();
        _bytecodeWriter.Seek(0, SeekOrigin.End);
    }

    public void WriteReference(AnnotatedBytecodeReference reference) {
        _bytecodeWriter ??= new BinaryWriter(Bytecode);
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
            case DMReference.Type.World:
            case DMReference.Type.Usr:
            case DMReference.Type.Invalid:
                break;

            default:
                compiler.ForcedError(_location ?? Location.Unknown, $"Encountered unknown reference type {reference.RefType}");
                break;
        }
    }

    public List<LocalVariableJson> GetLocalVariablesJson() {
        return _localVariables;
    }
}
