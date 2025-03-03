using OpenDreamShared.Common.Bytecode;

namespace DMCompiler.Optimizer;

/// <summary>
/// Automatically builds a cache of the <see cref="OpcodeMetadata"/> attribute for each opcode
/// </summary>
public static class OpcodeMetadataCache {
    private static readonly OpcodeMetadata[] MetadataCache = new OpcodeMetadata[256];

    static OpcodeMetadataCache() {
        foreach (DreamProcOpcode opcode in Enum.GetValues(typeof(DreamProcOpcode))) {
            var field = typeof(DreamProcOpcode).GetField(opcode.ToString());
            var attribute = Attribute.GetCustomAttribute(field!, typeof(OpcodeMetadataAttribute));
            var metadataAttribute = (OpcodeMetadataAttribute?)attribute;
            MetadataCache[(byte)opcode] = metadataAttribute?.Metadata ?? new OpcodeMetadata();
            MetadataCache[(byte)opcode] = metadataAttribute?.Metadata ?? new OpcodeMetadata();
        }
    }

    public static OpcodeMetadata GetMetadata(DreamProcOpcode opcode) {
        return MetadataCache[(byte)opcode];
    }
}
