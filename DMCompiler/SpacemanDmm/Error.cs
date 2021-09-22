#nullable enable

namespace DMCompiler.SpacemanDmm
{
    // Contains C# equivalents of SpacemanDMM's Error types in error.rs

    public struct Location
    {
        public ushort File;
        public ushort Column;
        public uint Line;
    }

    public enum Severity : byte
    {
        Error = 1,
        Warning = 2,
        Info = 3,
        Hint = 4
    }

    public enum Component : byte
    {
        Unspecified,
        DreamChecker
    }

    public struct DiagnosticNote
    {
        public Location Location;
        public string Description;
    }

    public sealed class DMError
    {
        public Location Location;
        public Severity Severity;
        public Component Component;
        public string Description { get; init; }
        public DiagnosticNote[] Notes { get; init; }
        public string? ErrorType;
    }
}
