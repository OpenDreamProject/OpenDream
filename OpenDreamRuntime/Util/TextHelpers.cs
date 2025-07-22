using System.Text;

namespace OpenDreamRuntime.Util;

public static class TextHelpers {
    // Like string.Substring(), but takes an array of runes instead.
    public static string RuneSubstring(Rune[] runes, int start, int end) {
        if (start < 0) throw new ArgumentOutOfRangeException("Start position is less than zero");
        if (end < 0 || end > runes.Length) throw new ArgumentOutOfRangeException("End position is not within array size");

        var stringBuilder = new StringBuilder();
        for (int i = start; i < end; i++) {
            stringBuilder.Append(runes[i].ToString());
        }
        return stringBuilder.ToString();
    }
}
