using System.Text;

namespace OpenDreamShared.Compiler
{
    public readonly struct Location
    {
        public static readonly Location Unknown = new Location();

        public Location(string filePath, int? line, int? column) {
            SourceFile = filePath;
            Line = line;
            Column = column;
        }

        public readonly string SourceFile { get; }
        public readonly int? Line { get; }
        public readonly int? Column { get; }

        public override string ToString() {
            var builder = new StringBuilder(SourceFile ?? "<unknown>");

            if (Line is not null && Line is var line) {
                builder.Append(":" + line);

                if (Column is not null && Column is var column) {
                    builder.Append(":" + column);
                }
            }

            return builder.ToString();
        }
    }
}
