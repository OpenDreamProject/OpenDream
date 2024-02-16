using System.Runtime.CompilerServices;

namespace OpenDreamRuntime.Procs;

public readonly ref struct DreamProcArguments {
    public int Count => Values.Length;

    public readonly ReadOnlySpan<DreamValue> Values;

    public DreamProcArguments() {
        Values = Array.Empty<DreamValue>();
    }

    public DreamProcArguments(ReadOnlySpan<DreamValue> values) {
        Values = values;
    }

    public DreamProcArguments(params DreamValue[] values) {
        Values = values;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DreamValue GetArgument(int argumentPosition) {
        if (Count > argumentPosition) {
            return Values[argumentPosition];
        }

        return DreamValue.Null;
    }

    public override string ToString() {
        return $"<Arguments {Count}>";
    }
}
