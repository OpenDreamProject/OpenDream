using System.Linq;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

// TODO: An arglist given to New() can be used to initialize an alist with values
public sealed class DreamAssocList(DreamObjectDefinition aListDef, int size) : DreamObject(aListDef), IDreamList {
    public bool IsAssociative => true;

    private readonly Dictionary<DreamValue, DreamValue> _values = new(size);

    public void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
        _values[key] = value;
    }

    public DreamValue GetValue(DreamValue key) {
        if (!_values.TryGetValue(key, out var value))
            return DreamValue.Null;

        return value;
    }

    public bool ContainsKey(DreamValue key) {
        return _values.ContainsKey(key);
    }

    public override DreamValue OperatorIndex(DreamValue index, DMProcState state) {
        return GetValue(index);
    }

    public IEnumerable<DreamValue> EnumerateValues() {
        return _values.Keys; // The keys, counter-intuitively
    }

    public int GetLength() {
        return _values.Count;
    }

    public void Cut(int start = 1, int end = 0) {
        if (start != 1) {
            throw new Exception($"Cut() was called with non-default start value of {start}.");
        }
        if (end != 0) {
            throw new Exception($"Cut() was called with non-default end value of {end}.");
        }
        _values.Clear();
    }

    public List<DreamValue> GetValues() {
        return _values.Keys.ToList();
    }

    public Dictionary<DreamValue, DreamValue> GetAssociativeValues() {
        return _values;
    }

    public void RemoveValue(DreamValue value) {
        _values.Remove(value);
    }
}
