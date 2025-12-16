using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace OpenDreamRuntime.Procs;

public readonly ref struct DreamProcArguments {
    public int Count => Values.Length;

    public ReadOnlySpan<DreamValue> Values { get; init; }

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
}
