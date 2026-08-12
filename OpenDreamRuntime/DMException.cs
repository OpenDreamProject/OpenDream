using System;

namespace OpenDreamRuntime;

/// <summary>
/// Represents an error caused by invalid DM code execution.
/// Unlike regular <see cref="Exception"/> instances, this exception is handled
/// as a runtime error originating from DM code.
/// </summary>
public sealed class DMException : Exception {
    public DMException(string message) : base(message) { }
}
