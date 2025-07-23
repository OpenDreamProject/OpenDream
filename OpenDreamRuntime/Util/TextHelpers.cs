using System.Text;

namespace OpenDreamRuntime.Util;

public static class TextHelpers {
    // Returns a slice of a rune array as a string.
    public static string RuneSubstring(Rune[] runes, int start, int end) {
        if (start < 0) throw new ArgumentOutOfRangeException("Start position is less than zero");
        if (end < 0 || end > runes.Length) throw new ArgumentOutOfRangeException("End position is not within array size");

        if (end == 0) {
            end = runes.Length;
        }

        var stringBuilder = new StringBuilder();
        for (int i = start; i < end; i++) {
            stringBuilder.Append(runes[i].ToString());
        }
        return stringBuilder.ToString();
    }
}
