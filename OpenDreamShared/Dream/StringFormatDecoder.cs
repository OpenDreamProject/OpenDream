using System.Text;

namespace OpenDreamShared.Dream;

public static class StringFormatDecoder {
    /// <summary>
    /// Refer to DMCompiler.Bytecode.StringFormatEncoder for documentation (can't cref cross-project)
    /// </summary>
    private static readonly ushort FormatPrefix = 0xFF00;

    private static readonly StringBuilder UnformattedString = new();

    /// <returns>A new version of the string, with all formatting characters removed.</returns>
    public static string RemoveFormatting(string input) {
        UnformattedString.Clear();
        UnformattedString.EnsureCapacity(input.Length); // Trying to keep it to one malloc here
        foreach(char c in input) {
            ushort bytes = c;
            if((bytes & FormatPrefix) != FormatPrefix)
                UnformattedString.Append(c);
        }

        return UnformattedString.ToString();
    }
}
