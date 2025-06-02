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
            throw new Exception($"No value with the key {key}");

        return value;
    }

    public bool ContainsKey(DreamValue key) {
        return _values.ContainsKey(key);
    }

    public IEnumerable<DreamValue> EnumerateValues() {
        return _values.Keys; // The keys, counter-intuitively
    }
}
