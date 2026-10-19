using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using OpenDreamRuntime.Procs;
namespace OpenDreamRuntime.Objects.Types;

[Virtual]
public class DreamList : DreamObject, IDreamList {
    private static readonly Stack<List<DreamValue>> ListPool = new();

    public virtual bool IsAssociative => _associativeValues is { Count: > 0 };

    private readonly List<DreamValue> _elements;
    private Dictionary<DreamValue, DreamValue>? _associativeValues;

    public DreamList(DreamObjectDefinition listDef, int size) : base(listDef) {
        if (size >= DreamManager.ListPoolThreshold && ListPool.TryPop(out var poppedValues)) {
            _elements = poppedValues;
            _elements.EnsureCapacity(size);
        } else {
            _elements = new List<DreamValue>(size);
        }
    }

    /// <summary>
    /// Create a new DreamList using an existing list of values (does not copy them)
    /// </summary>
    public DreamList(DreamObjectDefinition listDef, List<DreamValue> values, Dictionary<DreamValue, DreamValue>? associativeValues) : base(listDef) {
        _elements = values;
        _associativeValues = associativeValues;

        foreach (var value in _elements)
            value.IncRef();

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

    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);

        // Named arguments are ignored
        if (args.Count == 1 && args.GetArgument(0).TryGetValueAsInteger(out int size)) {
            Resize(size);
        } else if (args.Count > 1) {
            DreamList[] lists = [this];

            int dimensions = args.Count;
            for (int argIndex = 0; argIndex < dimensions; argIndex++) {
                DreamValue arg = args.GetArgument(argIndex);

                // BYOND coerces negative/non-num values to zero for multidimensional lists
                if (!arg.TryGetValueAsInteger(out size) || size < 0)
                    size = 0;

                DreamList[] newLists = new DreamList[size * lists.Length];

                for (int i = 0; i < lists.Length; i++) {
                    DreamList list = lists[i];

                    for (int j = 0; j < size; j++) {
                        if (argIndex < dimensions - 1) {
                            DreamList newList = ObjectTree.CreateList();

                            list.AddKey(new DreamValue(newList));
                            newLists[i * size + j] = newList;
                            newList.DecRef();
                        } else {
                            list.AddKey(DreamValue.Null);
                        }
                    }
                }

                lists = newLists;
            }
        }
    }

    // Hold on to large lists for reuse later
    protected override void HandleDeletion() {
        foreach (var value in _elements)
            value.DecRef();
        if (_associativeValues != null)
            foreach (var assocValue in _associativeValues.Values)
                assocValue.DecRef();

        if (_elements.Capacity > DreamManager.ListPoolThreshold && ListPool.Count < DreamManager.ListPoolSize) {
            _elements.Clear();
            ListPool.Push(_elements);
        }

        base.HandleDeletion();
    }

    public IDreamList CreateCopy(int start = 1, int end = 0) {
        if (start == 0) ++start; //start being 0 and start being 1 are equivalent

        var values = GetKeys();
        if (end > values.Count + 1 || start > values.Count + 1) throw new DMException("list index out of bounds");
        if (end == 0) end = values.Count + 1;
        if (end <= start)
            return new DreamList(ObjectDefinition, 0);

        List<DreamValue> copyValues = values.GetRange(start - 1, end - start);

        Dictionary<DreamValue, DreamValue>? associativeValues = null;
        if (_associativeValues != null) {
            associativeValues = new(end - start);
            foreach (var key in copyValues) {
                if (_associativeValues.TryGetValue(key, out var value))
                    associativeValues[key] = value;
            }
        }

        return new DreamList(ObjectDefinition, copyValues, associativeValues);
    }

    /// <summary>
    /// Returns a copy of the list of array values. Doesn't include the associative values indexable by some of these.
    /// </summary>
    [Obsolete("Deprecated. Use EnumerateValues() instead.")]
    public virtual List<DreamValue> GetKeys() {
        return [.._elements]; // Returns a copy for parity with DreamAssocList.
    }

    public virtual IEnumerable<DreamValue> EnumerateKeys() {
        return _elements;
    }

    public IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumeratePairs() {
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

    public Dictionary<DreamValue, DreamValue> GetPairs() {
        return _associativeValues ??= new Dictionary<DreamValue, DreamValue>();
    }

    [MustDisposeResource]
    public virtual DreamValue GetElement(DreamValue key) {
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

    public virtual bool TryGetAssocValue(DreamValue key, [MustDisposeResource][NotNullWhen(true)] out DreamValue? value) {
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

    public virtual void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
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

    public virtual void RemoveKey(DreamValue value) {
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

    public virtual void AddKey(DreamValue value) {
        _elements.Add(value);
        value.IncRef();
        UpdateTracyContentsMemory();
    }

    //Does not include associations
    public virtual bool ContainsKey(DreamValue value) {
        var count = _elements.Count;

        for (int i = 0; i < count; i++) {
            if (_elements[i].Equals(value))
                return true;
        }

        return false;
    }

    public virtual bool ElementHasAssociation(DreamValue value) {
        return _associativeValues != null && _associativeValues.ContainsKey(value);
    }

    public virtual int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        if (end == 0 || end > _elements.Count) end = _elements.Count + 1;

        for (int i = start; i < end; i++) {
            if (_elements[i - 1].Equals(value)) return i;
        }

        return 0;
    }

    public virtual void Cut(int start = 1, int end = 0) {
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

    public void Insert(int index, DreamValue value) {
        _elements.Insert(index - 1, value);
        value.IncRef();
        UpdateTracyContentsMemory();
    }

    public void Swap(int index1, int index2) {
        using var first = GetElement(new DreamValue(index1));
        using var second = GetElement(new DreamValue(index2));

        SetKeyValuePair(new DreamValue(index1), second);
        SetKeyValuePair(new DreamValue(index2), first);
    }

    public void Resize(int size) {
        if (size > _elements.Count) {
            _elements.EnsureCapacity(size);

            for (int i = _elements.Count; i < size; i++) {
                AddKey(DreamValue.Null);
            }
        } else {
            if (size < 0) {
                DreamManager.OptionalException<InvalidOperationException>(DMCompiler.Compiler.WarningCode.ListNegativeSizeException, "Setting a list size to a negative value is invalid");
                size = 0;
            }

            Cut(size + 1);
        }

        UpdateTracyContentsMemory();
    }

    public virtual int GetLength() {
        return _elements.Count;
    }

    public DreamList Union(IDreamList other) {
        DreamList newList = new DreamList(ObjectDefinition, _elements.Union(other.GetKeys()).ToList(), null);
        Dictionary<DreamValue, DreamValue> otherAssociations = other.GetPairs();
        foreach (DreamValue value in newList.EnumerateKeys()) {
            // Existing left pairs win, only values newly contributed by the right list use its association
            if (_associativeValues?.TryGetValue(value, out DreamValue leftAssociatedValue) is true)
                newList.SetKeyValuePair(value, leftAssociatedValue);
            else if (otherAssociations.TryGetValue(value, out DreamValue rightAssociatedValue))
                newList.SetKeyValuePair(value, rightAssociatedValue);
        }

        return newList;
    }

    public override string ToString() {
        string assoc = IsAssociative ? ", assoc" : "";
        return $"/list{{len={GetLength()}{assoc}}}";
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        if (varName == "len") {
            value = new(GetLength());
            return true;
        }

        // Note that invalid vars on /list will give null and not error in BYOND
        // We don't replicate that
        return base.TryGetVar(varName, out value);
    }

    protected override void SetVar(string varName, DreamValue value) {
        if (varName == "len") {
            value.TryGetValueAsInteger(out var newLen);

            Resize(newLen);
        } else {
            base.SetVar(varName, value);
        }
    }

    [Conditional("TOOLS")]
    private void UpdateTracyContentsMemory() {
        #if TOOLS
        var valueCount = _elements.Count + (_associativeValues?.Count ?? 0);
        var totalSize = Unsafe.SizeOf<DreamList>() + valueCount * Unsafe.SizeOf<DreamValue>();

        _tracyContentsMemoryId?.ReleaseMemory();
        _tracyContentsMemoryId = Profiler.BeginMemoryZone((ulong)totalSize, "/list contents");
        #endif
    }

    #region Operators

    public override DreamValue OperatorIndex(DreamValue index, DMProcState state) {
        return GetElement(index);
    }

    public override DreamValue OperatorAdd(DreamValue b, DMProcState state) {
        DreamList listCopy = (DreamList)CreateCopy();

        if (b.TryGetValueAsIDreamList(out var bList)) {
            foreach (DreamValue key in bList.EnumerateKeys()) {
                if (bList.TryGetAssocValue(key, out var _assocValue)) {
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

    public override DreamValue OperatorSubtract(DreamValue b, DMProcState state) {
        DreamList listCopy = (DreamList)CreateCopy();

        if (b.TryGetValueAsIDreamList(out var bList)) {
            foreach (DreamValue value in bList.EnumerateKeys()) {
                listCopy.RemoveKey(value);
            }
        } else {
            listCopy.RemoveKey(b);
        }

        return new DreamValue(listCopy);
    }

    public override DreamValue OperatorOr(DreamValue b, DMProcState state) {
        DreamList list;

        if (b.TryGetValueAsIDreamList(out var bList)) {  // List | List
            list = Union(bList);
        } else {                                        // List | x
            list = (DreamList)CreateCopy();
            list.AddKey(b);
        }

        return new DreamValue(list);
    }

    public override DreamValue OperatorAppend(DreamValue b) {
        if (b.TryGetValueAsIDreamList(out var bList)) {
            var keys = bList.GetKeys();
            var valueCount = keys.Count; // Some lists return a reference to their internal values list which could change with each loop
            for (int i = 0; i < valueCount; i++) {
                var key = keys[i];

                AddKey(key); // Always add the value

                if (bList.TryGetAssocValue(key, out var _assocValue)) { // Ensure the associated value is correct
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

    public override DreamValue OperatorRemove(DreamValue b) {
        if (b.TryGetValueAsIDreamList(out var bList)) {
            if (bList == this) {
                Cut();
            } else {
                foreach (DreamValue key in bList.EnumerateKeys()) {
                    RemoveKey(key);
                }
            }
        } else {
            RemoveKey(b);
        }

        IncRef();
        return new DreamValue(this);
    }

    public override DreamValue OperatorCombine(DreamValue b) {
        if (b.TryGetValueAsIDreamList(out var bList)) {
            foreach (DreamValue key in bList.EnumerateKeys()) {
                if (ContainsKey(key))
                    continue;

                if (bList.TryGetAssocValue(key, out var _assocValue)) {
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

    public override DreamValue OperatorMask(DreamValue b) {
        if (b.TryGetValueAsIDreamList(out var bList)) {
            var remainingValues = new Dictionary<DreamValue, int>(bList.GetLength());
            foreach (DreamValue value in bList.EnumerateKeys()) {
                remainingValues.TryGetValue(value, out int count);
                remainingValues[value] = count + 1;
            }

            for (int i = 1; i <= GetLength(); i++) {
                using var value = GetElement(new DreamValue(i));

                if (!remainingValues.TryGetValue(value, out int count) || count == 0) {
                    Cut(i, i + 1);
                    i--;
                } else {
                    remainingValues[value] = count - 1;
                }
            }
        } else {
            bool scalarAvailable = true;
            for (int i = 1; i <= GetLength(); i++) {
                using var value = GetElement(new DreamValue(i));

                if (!scalarAvailable || value != b) {
                    Cut(i, i + 1);
                    i--;
                } else {
                    scalarAvailable = false;
                }
            }
        }

        IncRef();
         return new DreamValue(this);
    }

    public override DreamValue OperatorEquivalent(DreamValue b) {
        if (!b.TryGetValueAsIDreamList(out var secondList))
            return DreamValue.False;

        if (GetLength() != secondList.GetLength())
            return DreamValue.False;

        var firstValues = GetKeys();
        var secondValues = secondList.GetKeys();

        var firstListAssoc = GetPairs();
        var secondListAssoc = secondList.GetPairs();

        for (var i = 0; i < firstValues.Count; i++) {
            // Starting with 516, equivalence checks assoc values
            if (IsAssociative || secondList.IsAssociative) {
                if(!firstListAssoc.TryGetValue(firstValues[i], out var firstAssocVal)) firstAssocVal = DreamValue.Null;
                if(!secondListAssoc.TryGetValue(firstValues[i], out var secondAssocVal)) secondAssocVal = DreamValue.Null;
                if (!firstAssocVal.Equals(secondAssocVal))
                    return DreamValue.False;
            }

            if (!firstValues[i].Equals(secondValues[i]))
                return DreamValue.False;
        }

        return DreamValue.True;
    }

    public override void OperatorOutput(DreamValue b) {
        HashSet<DreamConnection> passedConnections = new(); // BYOND only outputs to a client once per list

        foreach(var value in EnumerateKeys()) {
            DreamConnection? connection = null;
            if(value.TryGetValueAsDreamObject<DreamObjectClient>(out var dreamClient))
                connection = dreamClient.Connection;
            else if(value.TryGetValueAsDreamObject<DreamObjectMob>(out var dreamMob))
                connection = dreamMob.Connection;

            if(connection is null || !passedConnections.Add(connection))
                continue;

            connection.OutputDreamValue(b);
        }
    }

    #endregion Operators
}
