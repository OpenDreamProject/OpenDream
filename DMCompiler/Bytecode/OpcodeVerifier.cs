using System.Security.Cryptography;
using System.Text;

namespace DMCompiler.Bytecode;

// Dummy class-as-namespace because C# just kinda be like this
public static class OpcodeVerifier {
    /// <summary>
    /// Calculates a hash of all the <c>DreamProcOpcode</c>s and their bytecode metadata for warning on incompatibilities.
    /// </summary>
    /// <returns>A MD5 hash string</returns>
    public static string GetOpcodesHash() {
        Array allOpcodes = Enum.GetValues(typeof(DreamProcOpcode));
        List<byte> opcodesBytes = new List<byte>();

        foreach (var value in allOpcodes) {
            byte[] nameBytes = Encoding.ASCII.GetBytes(value.ToString()!);
            opcodesBytes.AddRange(nameBytes);
            opcodesBytes.Add((byte)value);

            var metadata = OpcodeMetadataCache.GetMetadata((DreamProcOpcode)value);
            opcodesBytes.AddRange(BitConverter.GetBytes(metadata.StackDelta));
            opcodesBytes.Add((byte)(metadata.VariableArgs ? 1 : 0));
            AddArgTypes(opcodesBytes, metadata.RequiredArgs);
            AddArgTypes(opcodesBytes, metadata.RepeatedArgs);
        }

        byte[] hashBytes = MD5.HashData(opcodesBytes.ToArray());
        return BitConverter.ToString(hashBytes).Replace("-", "");
    }

    private static void AddArgTypes(List<byte> opcodesBytes, OpcodeArgType[] argTypes) {
        opcodesBytes.AddRange(BitConverter.GetBytes(argTypes.Length));

        foreach (OpcodeArgType argType in argTypes) {
            opcodesBytes.Add((byte)argType);
        }
    }
}
