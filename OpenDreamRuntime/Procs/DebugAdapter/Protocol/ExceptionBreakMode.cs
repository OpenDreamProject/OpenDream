using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public enum ExceptionBreakMode {
    Never,
    Always,
    Unhandled,
    UserUnhandled,
}
