using OpenDreamRuntime.Procs.DebugAdapter;

namespace OpenDreamRuntime.Resources;

/// <summary>
/// A special resource that outputs to the console
/// <c>world.log</c> defaults to this
/// </summary>
sealed class ConsoleOutputResource : DreamResource {
    public ConsoleOutputResource() : base(0, null, null) { }

    public override string ReadAsString() {
        return null;
    }

    public void WriteConsole(LogLevel logLevel, string sawmill, string message) {
        IoCManager.Resolve<IDreamDebugManager>().HandleOutput(logLevel, message);
        Logger.GetSawmill(sawmill).Log(logLevel, message);
    }

    public override void Output(DreamValue value) {
        WriteConsole(LogLevel.Info, "world.log", value.Stringify());
    }
}
