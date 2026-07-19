using System.Linq;
using JetBrains.Annotations;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

// TODO: An arglist given to New() can be used to initialize an alist with values
public sealed class DreamAssocList(DreamObjectDefinition aListDef, int size) : BaseDreamList(aListDef) {
    public override bool IsAssociative => true;

    private readonly Dictionary<DreamValue, DreamValue> _values = new(size);

    public DreamAssocList(DreamObjectDefinition listDef, Dictionary<DreamValue, DreamValue>? values) : this(listDef, values?.Count ?? 0) {
        if (values != null) {
            _values = values;
        }
    }

    protected override void HandleDeletion() {
        foreach (var pair in _values) {
            pair.Key.DecRef();
            pair.Value.DecRef();
        }

        base.HandleDeletion();
    }

    public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (_values.TryGetValue(key, out var oldValue))
            oldValue.DecRef();
        else
            key.IncRef();

        _values[key] = value;
        value.IncRef();
    }

    public override DreamValue GetValue(DreamValue key) {
        if (!_values.TryGetValue(key, out var value))
            return DreamValue.Null;

        value.IncRef();
        return value;
    }

    public override bool ContainsKey(DreamValue key) {
        return _values.ContainsKey(key);
    }

    public override bool ContainsValue(DreamValue value) {
        return _values.ContainsKey(value); // not a mistake
    }

    public override IEnumerable<DreamValue> EnumerateValues() {
        return _values.Keys; // The keys, counter-intuitively
    }

    public override int GetLength() {
        return _values.Count;
    }

    public override void Cut(int start = 1, int end = 0) {
        if (start != 1 && start != 0) {
            throw new Exception($"Cut() was called with non-default start value of {start}.");
        }

        if (end != 0) {
            throw new Exception($"Cut() was called with non-default end value of {end}.");
        }

        foreach (var value in _values) {
            value.Key.DecRef();
            value.Value.DecRef();
        }

        _values.Clear();
    }

    public override Dictionary<DreamValue, DreamValue> GetAssociativeValues() {
        return _values;
    }

    public override void RemoveValue(DreamValue value) {
        if(_values.Remove(value))
            value.DecRef();
    }

    public override IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumerateAssocValues() {
        return _values;
    }

    public override void AddValue(DreamValue value) {
        if (ContainsValue(value)) {
            return; // calling Add("c") on alist("c" = 5) does not change anything
        }

        _values[value] = DreamValue.Null;
        value.IncRef();
    }

    public override BaseDreamList CreateCopy(int start = 1, int end = 0) {
        if (start != 1 || end != 0) {
            throw new Exception("list index out of bounds");
        }

        var copyValues = new Dictionary<DreamValue, DreamValue>(_values);
        foreach (var value in _values) {
            value.Key.IncRef();
            value.Value.IncRef();
        }

        return new DreamAssocList(ObjectDefinition, copyValues);
    }

    public override int FindValue(DreamValue value, int start = 1, int end = 0) {
        // Unlike list.Find(), alist.Find() doesn't pay attention to start and end, and returns a boolean 0/1 instead of the position of the found object
        if (ContainsValue(value)) {
            return 1;
        }

        return 0;
    }

    public override void Resize(int size) {
        // alists specifically will always either runtime or get set to 0 and cut
        if(size != 0)
            throw new InvalidOperationException("length of strict associative list can only be set to 0");
        else
            Cut();
    }

    public override DreamValue OperatorAdd(DreamValue b, DMProcState state) {
        var listCopy = (DreamAssocList)CreateCopy();

        if (b.TryGetValueAsBaseDreamList(out var bList)) {
            foreach (var pair in bList.EnumerateAssocValues()) {
                if (listCopy.ContainsKey(pair.Key)) {
                    continue;
                }

                listCopy.SetValue(pair.Key, pair.Value);
            }
        } else {
            listCopy.AddValue(b);
        }

        return new DreamValue(listCopy);
    }

    public override DreamValue OperatorSubtract(DreamValue b, DMProcState state) {
        DreamAssocList listCopy;

        if (b.TryGetValueAsBaseDreamList(out var bList)) {
            if (bList.Equals(this)) {
                return new DreamValue(ObjectTree.CreateAssocList());
            }

            listCopy = (DreamAssocList)CreateCopy();
            foreach (DreamValue value in bList.EnumerateValues()) {
                listCopy.RemoveValue(value);
            }
        } else {
            listCopy = (DreamAssocList)CreateCopy();
            listCopy.RemoveValue(b);
        }

        return new DreamValue(listCopy);
    }

    public override DreamValue OperatorOr(DreamValue b, DMProcState state) {
        return OperatorAdd(b, state);
    }

    public override DreamValue OperatorCombine(DreamValue b) {
        return OperatorAppend(b);
    }

    public override DreamValue OperatorAppend(DreamValue b) {
        if (b.TryGetValueAsBaseDreamList(out var bList)) {
            foreach (var pair in bList.EnumerateAssocValues()) {
                if (ContainsKey(pair.Key)) {
                    continue;
                }

                SetValue(pair.Key, pair.Value);
            }
        } else {
            AddValue(b);
        }

        IncRef();
        return new(this);
    }

    public override DreamValue OperatorRemove(DreamValue b) {
        if (b.TryGetValueAsBaseDreamList(out var bList)) {
            if (bList.Equals(this)) {
                Cut();
            } else {
                foreach (var value in bList.EnumerateValues()) {
                    RemoveValue(value);
                }
            }
        } else {
            RemoveValue(b);
        }

        IncRef();
        return new(this);
    }

    public override DreamValue OperatorMask(DreamValue b) {
        if (b.TryGetValueAsBaseDreamList(out var bList)) {
            if (!bList.Equals(this)) {
                foreach (var value in CopyToArray()) {
                    if (!bList.ContainsValue(value)) {
                        RemoveValue(value);
                    }
                }
            }
        } else {
            if (!ContainsKey(b)) {
                Cut();
            } else {
                using var item = GetValue(b);
                Cut();
                SetValue(b, item);
            }
        }

        IncRef();
        return new(this);
    }

    public override DreamValue OperatorEquivalent(DreamValue b) {
        if (!b.TryGetValueAsBaseDreamList(out var secondList)) {
            return DreamValue.False;
        }

        if (secondList.Equals(this)) {
            return DreamValue.True;
        }

        if (GetLength() != secondList.GetLength()) {
            return DreamValue.False;
        }

        foreach (var pair in secondList.EnumerateAssocValues()) {
            if (!ContainsKey(pair.Key)) {
                return DreamValue.False;
            }

            using var temp = GetValue(pair.Key);
            if (!temp.Equals(pair.Value)) {
                return DreamValue.False;
            }
        }

        return DreamValue.True;
    }
}


