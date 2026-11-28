using System.Runtime.InteropServices;
using JetBrains.Annotations;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

[Virtual]
public class DantomList : DreamEagerList {
    public override bool IsAssociative => _associativeValues is { Count: > 0 };

    private Dictionary<DreamValue, DreamValue>? _associativeValues;

    public DantomList(DreamObjectDefinition listDef, int size) : base(listDef, size) {
    }

    /// <summary>
    /// Create a new DreamList using an existing list of values (does not copy them)
    /// </summary>
    public DantomList(DreamObjectDefinition listDef, List<DreamValue> values, Dictionary<DreamValue, DreamValue>? associativeValues) : base(listDef, values) {
        _associativeValues = associativeValues;

        if (_associativeValues != null) {
            foreach (var assocValue in _associativeValues.Values) {
                assocValue.IncRef();
            }
        }

        UpdateTracyContentsMemory();
    }

    protected override void HandleDeletion() {
        if (_associativeValues != null)
            foreach (var assocValue in _associativeValues.Values)
                assocValue.DecRef();

        base.HandleDeletion();
    }

    public override DantomList ToDantomList(int start = 1, int end = 0) {
        if (start == 0) ++start; //start being 0 and start being 1 are equivalent

        if (end > Elements.Count + 1 || start > Elements.Count + 1) throw new DMException("list index out of bounds");
        if (end == 0) end = Elements.Count + 1;
        if (end <= start)
            return new DantomList(ObjectDefinition, 0);

        List<DreamValue> copyValues = Elements.GetRange(start - 1, end - start);

        Dictionary<DreamValue, DreamValue>? associativeValues = null;
        if (_associativeValues != null) {
            associativeValues = new(end - start);
            foreach (var key in copyValues) {
                if (_associativeValues.TryGetValue(key, out var value))
                    associativeValues[key] = value;
            }
        }

        return new DantomList(ObjectDefinition, copyValues, associativeValues);
    }

    public override IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumerateLoosePairs() {
        foreach (var value in Elements) {
            if (_associativeValues?.TryGetValue(value, out var associativeValue) is true) {
                yield return new(value, associativeValue);
            } else {
                yield return new(value, DreamValue.Null);
            }
        }
    }

    public Dictionary<DreamValue, DreamValue> CopyPairs() {
        if (_associativeValues is null)
            return new();

        return new(_associativeValues);
    }

    public override Dictionary<DreamValue, DreamValue> EnumerateTightPairs() {
        return _associativeValues ??= new Dictionary<DreamValue, DreamValue>();
    }

    [MustDisposeResource]
    public override DreamValue GetElement(DreamValue key) {
        if (key.TryGetValueAsInteger(out int keyInteger)) {
            var value = Elements[keyInteger - 1]; //1-indexed

            value.IncRef();
            return value;
        }

        if (_associativeValues?.TryGetValue(key, out DreamValue assocValue) is true) {
            assocValue.IncRef();
            return assocValue;
        }

        return DreamValue.Null;
    }

    public override bool TryGetElementByKey(DreamValue key, [MustDisposeResource] out DreamValue value) {
        if (_associativeValues is null) {
            value = default;
            return false;
        }

        if (_associativeValues.TryGetValue(key, out var associatedValue)) {
            value = associatedValue;
            value.IncRef();
            return true;
        }

        value = default;
        return false;
    }

    public override void Set(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (key.TryGetValueAsInteger(out int keyInteger)) {
            if (allowGrowth && keyInteger == Elements.Count + 1) {
                value.IncRef();
                Elements.Add(value);
            } else {
                var oldValue = Elements[keyInteger - 1];

                value.IncRef();
                oldValue.DecRef();
                Elements[keyInteger - 1] = value;
            }
        } else {
            if (!ContainsKey(key)) {
                Elements.Add(key);
                key.IncRef();
            }

            _associativeValues ??= new Dictionary<DreamValue, DreamValue>(1);
            if (_associativeValues.TryGetValue(key, out var oldValue))
                oldValue.DecRef();

            _associativeValues[key] = value;
            value.IncRef();
        }

        UpdateTracyContentsMemory();
    }

    public override void Remove(DreamValue value) {
        int valueIndex = Elements.LastIndexOf(value);

        if (valueIndex != -1) {
            if (_associativeValues?.TryGetValue(value, out var associatedValue) is true)  {
                associatedValue.DecRef();
                _associativeValues.Remove(value);
            }

            Elements.RemoveAt(valueIndex);
            value.DecRef();
        }

        UpdateTracyContentsMemory();
    }

    public override bool ElementHasAssociation(DreamValue value) {
        return _associativeValues != null && _associativeValues.ContainsKey(value);
    }

    public override void Cut(int start = 1, int end = 0) {
        start = start switch {
            < 0 => throw new ArgumentOutOfRangeException(nameof(start), start, "Parameter start is less than zero."),
            0 => 1,
            _ => start
        };
        if (end == 0 || end > (Elements.Count + 1)) end = Elements.Count + 1;

        if (_associativeValues != null) {
            for (int i = start; i < end; i++) {
                var key = Elements[i - 1];

                if (_associativeValues.Remove(key, out var assocValue)) {
                    assocValue.DecRef();
                }
            }
        }

        if (end > start) {
            for (int i = start; i < end; i++) {
                Elements[i - 1].DecRef();
            }

            Elements.RemoveRange(start - 1, end - start);
        }

        UpdateTracyContentsMemory();
    }

    public override void Swap(int index1, int index2) {
        using var first = GetElement(new DreamValue(index1));
        using var second = GetElement(new DreamValue(index2));

        Set(new DreamValue(index1), second);
        Set(new DreamValue(index2), first);
    }

    #if TOOLS
    protected override int TracyContentsCount => Elements.Count + (_associativeValues?.Count ?? 0);
    #endif

    #region Operators

    public override DreamValue OperatorAdd(DreamValue b, DMProcState state) {
        DantomList listCopy = ToDantomList();

        if (b.TryGetValueAsDreamList(out var bList)) {
            foreach (DreamValue key in bList.Enumerate()) {
                if (bList.TryGetElementByKey(key, out var assocValue)) {
                    using (assocValue) {
                        listCopy.Set(key, assocValue);
                    }
                } else {
                    listCopy.Add(key);
                }
            }
        } else {
            listCopy.Add(b);
        }

        return new DreamValue(listCopy);
    }

    public override DreamValue OperatorAppend(DreamValue b) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            var keys = bList.CopyKeys();
            var valueCount = keys.Count; // Some lists return a reference to their internal values list which could change with each loop
            for (int i = 0; i < valueCount; i++) {
                var key = keys[i];

                Add(key); // Always add the value

                if (bList.TryGetElementByKey(key, out var assocValue)) { // Ensure the associated value is correct
                    using (assocValue) {
                        _associativeValues ??= new();

                        ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(_associativeValues, key, out _);
                        assocValue.IncRef();
                        slot.DecRef();
                        slot = assocValue;
                    }
                }
            }
        } else {
            Add(b);
        }

        IncRef();
        return new DreamValue(this);
    }

    public override DreamValue OperatorCombine(DreamValue b, DMProcState state) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            foreach (DreamValue key in bList.Enumerate()) {
                if (ContainsKey(key))
                    continue;

                if (bList.TryGetElementByKey(key, out var assocValue)) {
                    using (assocValue) {
                        Set(key, assocValue);
                    }
                } else {
                    Add(key);
                }
            }
        } else if (!ContainsKey(b)) {
            Add(b);
        }

        IncRef();
        return new DreamValue(this);
    }

    #endregion Operators
}
