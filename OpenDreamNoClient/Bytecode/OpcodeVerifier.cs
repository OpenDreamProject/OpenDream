using OpenDreamShared.Common.Bytecode;
using System.Security.Cryptography;
using System.Text;

namespace OpenDreamNoClient.Bytecode;

// Dummy class-as-namespace because C# just kinda be like this
public class OpcodeVerifier: IOpcodeVerifier {
    /// <summary>
    /// Calculates a hash of all the <c>DreamProcOpcode</c>s for warning on incompatibilities.
    /// </summary>
    /// <returns>A MD5 hash string</returns>
    public string GetOpcodesHash() {
        var allOpcodes = Enum.GetValues(typeof(DreamProcOpcode));
        List<byte> opcodesBytes = new List<byte>();

        foreach (var value in allOpcodes) {
            byte[] nameBytes = Encoding.ASCII.GetBytes(value.ToString()!);
            opcodesBytes.AddRange(nameBytes);
            opcodesBytes.Add((byte)value);
        }

        byte[] hashBytes = MD5.HashData(opcodesBytes.ToArray());
        return BitConverter.ToString(hashBytes).Replace("-", "");
    }
}
