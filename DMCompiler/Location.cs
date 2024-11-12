using System.IO;
using System.Text;

namespace DMCompiler;

public readonly struct Location(string filePath, int? line, int? column) {
    /// <summary>
    /// For when DM location information can't be determined.
    /// </summary>
    public static readonly Location Unknown = new();

    /// <summary>
    /// For when internal OpenDream warnings/errors are raised or something internal needs to be passed a location.
    /// </summary>
    public static readonly Location Internal = new("<internal>", null, null);

    public string SourceFile { get; } = filePath;
    public int? Line { get; } = line;
    public int? Column { get; } = column;

    public override string ToString() {
        var builder = new StringBuilder((SourceFile is null) ? "<unknown>" : Path.GetRelativePath(".", SourceFile));

        if (Line is not null) {
            builder.Append(":" + Line);

            if (Column is not null) {
                builder.Append(":" + Column);
            }
        }

        return builder.ToString();
    }
}
