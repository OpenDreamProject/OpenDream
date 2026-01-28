using System.Linq;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

// TODO: An arglist given to New() can be used to initialize an alist with values
public sealed class DreamAssocList(DreamObjectDefinition aListDef, int size) : DreamObject(aListDef), IDreamList {
    public bool IsAssociative => true;

    private readonly Dictionary<DreamValue, DreamValue> _values = new(size);

    public DreamAssocList(DreamObjectDefinition listDef, Dictionary<DreamValue, DreamValue>? values) : this(listDef, values?.Count ?? 0) {
        if (values != null) {
            _values = values;
        }
    }

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
        if (start != 1 && start != 0) {
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

    public IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumerateAssocValues() {
        return _values;
    }

    public DreamValue[] CopyToArray() {
        var array = new DreamValue[_values.Count];

        _values.Keys.CopyTo(array, 0);
        return array;
    }

    public Dictionary<DreamValue, DreamValue> CopyAssocValues() {
        return new(_values);
    }

    public void AddValue(DreamValue value) {
        if (ContainsValue(value)) {
            return; // calling Add("c") on alist("c" = 5) does not change anything
        }

        _values[value] = DreamValue.Null;
    }

    public IDreamList CreateCopy(int start = 1, int end = 0) {
        if (start != 1 || end != 0) {
            throw new Exception("list index out of bounds");
        }

        Dictionary<DreamValue, DreamValue> copyValues = new(_values);
        return new DreamAssocList(ObjectDefinition, copyValues);
    }

    public int FindValue(DreamValue value, int start = 1, int end = 0) {
        // Unlike list.Find(), alist.Find() doesn't pay attention to start and end, and returns a boolean 0/1 instead of the position of the found object
        if (ContainsValue(value)) {
            return 1;
        }

        return 0;
    }

    public void Insert(int index, DreamValue value) {
        throw new Exception("insert not allowed for this list");
    }

    public void Swap(int index1, int index2) {
        throw new Exception("swap not allowed for this list");
    }

    public bool ContainsValue(DreamValue value) {
        return _values.ContainsKey(value);
    }

    public override DreamValue OperatorAppend(DreamValue b) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            foreach (var value in bList.EnumerateValues()) {
                AddValue(value);
            }
        } else {
            AddValue(b);
        }

        return new(this);
    }

}
