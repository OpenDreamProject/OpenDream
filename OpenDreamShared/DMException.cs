using System;

namespace OpenDreamShared;

public sealed class DMException : Exception {
    public DMException(string message) : base(message) { }
}
