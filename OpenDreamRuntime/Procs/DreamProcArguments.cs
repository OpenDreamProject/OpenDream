using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs;

[MustDisposeResource]
public readonly ref struct DreamProcArguments : IDisposable {
    public int Count => Values.Length;

    public readonly ReadOnlySpan<DreamValue> Values;

    public DreamProcArguments() {
        Values = Array.Empty<DreamValue>();
    }

    public DreamProcArguments(ReadOnlySpan<DreamValue> values) {
        Values = values;
        foreach (var value in Values)
            value.IncRef();
    }

    public DreamProcArguments(params DreamValue[] values) {
        Values = values;
        foreach (var value in Values)
            value.IncRef();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DreamValue GetArgument(int argumentPosition) {
        if (Count > argumentPosition) {
            return Values[argumentPosition];
        }

        return DreamValue.Null;
    }

    public override string ToString() {
        var strBuilder = new StringBuilder((Count * 2 - 1) + 4);

        strBuilder.Append("<Arguments ");
        strBuilder.Append(Count);
        strBuilder.Append(">(");
        for (int i = 0; i < Count; i++) {
            strBuilder.Append(Values[i]);
            if (i != Count - 1)
                strBuilder.Append(", ");
        }

        strBuilder.Append(')');
        return strBuilder.ToString();
    }

    public void Dispose() {
        foreach (var value in Values)
            value.Dispose();
    }
}
