using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
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

        #if TOOLS
        TracyMemoryId = Profiler.BeginMemoryZone(1, "/list instance");
        UpdateTracyContentsMemory();
        #endif
    }

    protected override void HandleDeletion() {

        if (_associativeValues != null)
            foreach (var assocValue in _associativeValues.Values)
                assocValue.DecRef();

        base.HandleDeletion();
    }

    [Obsolete("Consider ToDantomList() instead.")]
    public override DantomList ToCopy(int start = 1, int end = 0) {
        return ToDantomList(start, end);
    }

    public override DantomList ToDantomList(int start = 1, int end = 0) {
        if (start == 0) ++start; //start being 0 and start being 1 are equivalent

        if (end > _elements.Count + 1 || start > _elements.Count + 1) throw new DMException("list index out of bounds");
        if (end == 0) end = _elements.Count + 1;
        if (end <= start)
            return new DantomList(ObjectDefinition, 0);

        List<DreamValue> copyValues = _elements.GetRange(start - 1, end - start);

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
        foreach (var value in _elements) {
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
            var value = _elements[keyInteger - 1]; //1-indexed

            value.IncRef();
            return value;
        }

        if (_associativeValues?.TryGetValue(key, out DreamValue assocValue) is true) {
            assocValue.IncRef();
            return assocValue;
        }

        return DreamValue.Null;
    }

    public override bool TryGetElementByKey(DreamValue key, [MustDisposeResource][NotNullWhen(true)] out DreamValue? value) {
        if (_associativeValues is null) {
            value = null;
            return false;
        }

        if (_associativeValues.TryGetValue(key, out var _value)) {
            value = _value;
            value.Value.IncRef();
            return true;
        }

        value = null;
        return false;
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (key.TryGetValueAsInteger(out int keyInteger)) {
            if (allowGrowth && keyInteger == _elements.Count + 1) {
                value.IncRef();
                _elements.Add(value);
            } else {
                var oldValue = _elements[keyInteger - 1];

                value.IncRef();
                oldValue.DecRef();
                _elements[keyInteger - 1] = value;
            }
        } else {
            if (!ContainsKey(key)) {
                _elements.Add(key);
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

    public override void RemoveKey(DreamValue value) {
        int valueIndex = _elements.LastIndexOf(value);

        if (valueIndex != -1) {
            if (_associativeValues?.TryGetValue(value, out var associatedValue) is true)  {
                associatedValue.DecRef();
                _associativeValues.Remove(value);
            }

            _elements.RemoveAt(valueIndex);
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
        if (end == 0 || end > (_elements.Count + 1)) end = _elements.Count + 1;

        if (_associativeValues != null) {
            for (int i = start; i < end; i++) {
                var key = _elements[i - 1];

                if (_associativeValues.Remove(key, out var assocValue)) {
                    assocValue.DecRef();
                }
            }
        }

        if (end > start) {
            for (int i = start; i < end; i++) {
                _elements[i - 1].DecRef();
            }

            _elements.RemoveRange(start - 1, end - start);
        }

        UpdateTracyContentsMemory();
    }

    public override void Swap(int index1, int index2) {
        using var first = GetElement(new DreamValue(index1));
        using var second = GetElement(new DreamValue(index2));

        SetKeyValuePair(new DreamValue(index1), second);
        SetKeyValuePair(new DreamValue(index2), first);
    }

    protected override void UpdateTracyContentsMemory() {
        #if TOOLS
        var valueCount = _elements.Count + (_associativeValues?.Count ?? 0);
        var totalSize = Unsafe.SizeOf<DantomList>() + valueCount * Unsafe.SizeOf<DreamValue>();

        _tracyContentsMemoryId?.ReleaseMemory();
        _tracyContentsMemoryId = Profiler.BeginMemoryZone((ulong)totalSize, "/list contents");
        #endif
    }


    #region Operators

    public override DreamValue OperatorAdd(DreamValue b, DMProcState state) {
        DantomList listCopy = ToDantomList();

        if (b.TryGetValueAsDreamList(out var bList)) {
            foreach (DreamValue key in bList.EnumerateKeys()) {
                if (bList.TryGetElementByKey(key, out var _assocValue)) {
                    using var assocValue = _assocValue.Value;
                    listCopy.SetKeyValuePair(key, assocValue);
                } else {
                    listCopy.AddKey(key);
                }
            }
        } else {
            listCopy.AddKey(b);
        }

        return new DreamValue(listCopy);
    }

    public override DreamValue OperatorAppend(DreamValue b) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            var keys = bList.CopyKeys();
            var valueCount = keys.Count; // Some lists return a reference to their internal values list which could change with each loop
            for (int i = 0; i < valueCount; i++) {
                var key = keys[i];

                AddKey(key); // Always add the value

                if (bList.TryGetElementByKey(key, out var _assocValue)) { // Ensure the associated value is correct
                    _associativeValues ??= new();
                    using var assocValue = _assocValue.Value;

                    ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(_associativeValues, key, out _);
                    assocValue.IncRef();
                    slot.DecRef();
                    slot = assocValue;
                }
            }
        } else {
            AddKey(b);
        }

        IncRef();
        return new DreamValue(this);
    }

    public override DreamValue OperatorCombine(DreamValue b) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            foreach (DreamValue key in bList.EnumerateKeys()) {
                if (ContainsKey(key))
                    continue;

                if (bList.TryGetElementByKey(key, out var _assocValue)) {
                    using var assocValue = _assocValue;
                    SetKeyValuePair(key, assocValue.Value);

                } else {
                    AddKey(key);
                }
            }
        } else if (!ContainsKey(b)) {
            AddKey(b);
        }

        IncRef();
        return new DreamValue(this);
    }
    #endregion Operators
}
