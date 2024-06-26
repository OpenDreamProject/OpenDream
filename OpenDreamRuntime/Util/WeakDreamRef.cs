using System.Runtime.CompilerServices;
using OpenDreamRuntime.Objects;

namespace OpenDreamRuntime.Util;

public sealed class WeakDreamRef {
    private readonly WeakReference<DreamObject> _weakReference;

    public WeakDreamRef(DreamObject obj) {
        _weakReference = new(obj);
    }

    public DreamObject? Target {
        get {
            _weakReference.TryGetTarget(out var t);
            return t;
        }
    }

    public override bool Equals(object? obj) {
        return Target?.Equals(obj) ?? false;
    }

    public override int GetHashCode() {
        throw new NotSupportedException("WeakDreamRef cannot be used as a hashmap key or similar.");
    }
}
