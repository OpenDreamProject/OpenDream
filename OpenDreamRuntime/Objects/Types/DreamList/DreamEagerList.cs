using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using OpenDreamRuntime.Procs;
namespace OpenDreamRuntime.Objects.Types;

[Virtual]
public class DreamEagerList : DreamListAbstract {
    #region Variables
    protected static readonly Stack<List<DreamValue>> ListPool = new();

    public override bool IsAssociative => false;

    protected readonly List<DreamValue> _elements;


    #if TOOLS
    protected ProfilerMemory? _tracyContentsMemoryId;
    #endif

    #endregion
    #region Constructors/Initializers/Destructors

    public DreamEagerList(DreamObjectDefinition listDef, int size) : base(listDef) {
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
    public DreamEagerList(DreamObjectDefinition listDef, List<DreamValue> values) : base(listDef) {
        _elements = values;

        foreach (var value in _elements)
            value.IncRef();

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
            DreamEagerList[] lists = [this];

            int dimensions = args.Count;
            for (int argIndex = 0; argIndex < dimensions; argIndex++) {
                DreamValue arg = args.GetArgument(argIndex);

                // BYOND coerces negative/non-num values to zero for multidimensional lists
                if (!arg.TryGetValueAsInteger(out size) || size < 0)
                    size = 0;

                DreamEagerList[] newLists = new DreamEagerList[size * lists.Length];

                for (int i = 0; i < lists.Length; i++) {
                    DreamEagerList list = lists[i];

                    for (int j = 0; j < size; j++) {
                        if (argIndex < dimensions - 1) {
                            DreamEagerList newList = ObjectTree.CreateList();

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

        if (_elements.Capacity > DreamManager.ListPoolThreshold && ListPool.Count < DreamManager.ListPoolSize) {
            _elements.Clear();
            ListPool.Push(_elements);
        }

        base.HandleDeletion();
    }

    #endregion
    #region Pure Methods

    public override IEnumerable<DreamValue> EnumerateKeys() {
        return _elements;
    }

    /// <summary>
    /// Returns a copy of the list of array values. Doesn't include the associative values indexable by some of these.
    /// </summary>
    [Obsolete("Deprecated. Use EnumerateValues() instead.")]
    public override List<DreamValue> GetKeys() {
        return [.._elements]; // Returns a copy for parity with DreamAssocList.
    }

    public override List<DreamValue> GetKeysFast() {
        return _elements;
    }

    public override int GetLength() {
        return _elements.Count;
    }

    public override DreamEagerList ToCopy(int start = 1, int end = 0) {
        if (start == 0) ++start; //start being 0 and start being 1 are equivalent

        if (end > _elements.Count + 1 || start > _elements.Count + 1) throw new DMException("list index out of bounds");
        if (end == 0) end = _elements.Count + 1;
        if (end <= start)
            return new DreamEagerList(ObjectDefinition, 0);

        List<DreamValue> copyValues = _elements.GetRange(start - 1, end - start);

        return new DreamEagerList(ObjectDefinition, copyValues);
    }

    [MustDisposeResource]
    public override DreamValue GetElement(DreamValue key) {
        if (key.TryGetValueAsInteger(out int keyInteger)) {
            var value = _elements[keyInteger - 1]; //1-indexed

            value.IncRef();
            return value;
        }
        return DreamValue.Null;
    }

    public override bool ContainsKey(DreamValue value) {
        var count = _elements.Count;

        for (int i = 0; i < count; i++) {
            if (_elements[i].Equals(value))
                return true;
        }

        return false;
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        if (end == 0 || end > _elements.Count) end = _elements.Count + 1;

        for (int i = start; i < end; i++) {
            if (_elements[i - 1].Equals(value)) return i;
        }

        return 0;
    }

    public DreamEagerList Union(DreamListAbstract other) {
        DreamEagerList newList = new DreamEagerList(ObjectDefinition, _elements.Union(other.GetKeys()).ToList());
        if(!(IsAssociative && other.IsAssociative)) {
            return newList;
        }
        Dictionary<DreamValue, DreamValue> ourAssociations = GetTightPairs();
        Dictionary<DreamValue, DreamValue> otherAssociations = other.GetTightPairs();

        foreach (DreamValue value in newList.EnumerateKeys()) {
            // Existing left pairs win, only values newly contributed by the right list use its association
            if (ourAssociations.TryGetValue(value, out DreamValue leftAssociatedValue) is true)
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

    #endregion
    #region Impure Methods
    public override void SetElementByIndex(DreamValue index, DreamValue element, bool allowGrowth = false) {
        if (!index.TryGetValueAsInteger(out int keyInteger)) {
            throw new InvalidOperationException("Attempted set element by index with a non-integer value.");
        }

        if (allowGrowth && keyInteger == GetLength() + 1) {
            element.IncRef();
            AddKey(element);
            return;
        }

        if(!TryGetElementByIndex(keyInteger - 1, out var _oldValue)) {
            throw new IndexOutOfRangeException();
        }

        DreamValue oldElement = _oldValue.Value;
        oldElement.DecRef();

        _elements[keyInteger - 1] = element;
        element.IncRef();
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
            value.IncRef();
        }

        UpdateTracyContentsMemory();
    }

    public override void RemoveKey(DreamValue key) {
        int valueIndex = _elements.LastIndexOf(key);

        if (valueIndex != -1) {
            _elements.RemoveAt(valueIndex);
            key.DecRef();
        }

        UpdateTracyContentsMemory();
    }

    public override void AddKey(DreamValue value) {
        _elements.Add(value);
        value.IncRef();
        UpdateTracyContentsMemory();
    }

    public override void Cut(int start = 1, int end = 0) {
        start = start switch {
            < 0 => throw new ArgumentOutOfRangeException(nameof(start), start, "Parameter start is less than zero."),
            0 => 1,
            _ => start
        };
        if (end == 0 || end > (_elements.Count + 1)) end = _elements.Count + 1;

        if (end > start) {
            for (int i = start; i < end; i++) {
                _elements[i - 1].DecRef();
            }

            _elements.RemoveRange(start - 1, end - start);
        }

        UpdateTracyContentsMemory();
    }

    public override void Insert(int index, DreamValue value) {
        _elements.Insert(index - 1, value);
        value.IncRef();
        UpdateTracyContentsMemory();
    }

    public override void Swap(int index1, int index2) {
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

    protected override void SetVar(string varName, DreamValue value) {
        if (varName == "len") {
            value.TryGetValueAsInteger(out var newLen);

            Resize(newLen);
        } else {
            base.SetVar(varName, value);
        }
    }

    [Conditional("TOOLS")]
    protected virtual void UpdateTracyContentsMemory() {
        #if TOOLS
        var valueCount = _elements.Count;
        var totalSize = Unsafe.SizeOf<DreamList>() + valueCount * Unsafe.SizeOf<DreamValue>();

        _tracyContentsMemoryId?.ReleaseMemory();
        _tracyContentsMemoryId = Profiler.BeginMemoryZone((ulong)totalSize, "/list contents");
        #endif
    }

    #endregion
    #region Operators

    public override DreamValue OperatorIndex(DreamValue index, DMProcState state) {
        return GetElement(index);
    }

    public override DreamValue OperatorAdd(DreamValue b, DMProcState state) {
        DreamList listCopy = (DreamList)ToCopy();

        if (b.TryGetValueAsDreamListAbstract(out var bList)) {
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
        DreamList listCopy = (DreamList)ToCopy();

        if (b.TryGetValueAsDreamListAbstract(out var bList)) {
            foreach (DreamValue value in bList.EnumerateKeys()) {
                listCopy.RemoveKey(value);
            }
        } else {
            listCopy.RemoveKey(b);
        }

        return new DreamValue(listCopy);
    }

    public override DreamValue OperatorOr(DreamValue b, DMProcState state) {
        DreamEagerList list;

        if (b.TryGetValueAsDreamListAbstract(out var bList)) {  // List | List
            list = Union(bList);
        } else {                                        // List | x
            list = ToCopy();
            list.AddKey(b);
        }

        return new DreamValue(list);
    }

    public override DreamValue OperatorAppend(DreamValue b) {
        if (b.TryGetValueAsDreamListAbstract(out var bList)) {
            var keys = bList.GetKeys();
            var valueCount = keys.Count; // Some lists return a reference to their internal values list which could change with each loop
            for (int i = 0; i < valueCount; i++) {
                var key = keys[i];

                AddKey(key); // Always add the value
            }
        } else {
            AddKey(b);
        }

        IncRef();
        return new DreamValue(this);
    }

    public override DreamValue OperatorRemove(DreamValue b) {
        if (b.TryGetValueAsDreamListAbstract(out var bList)) {
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
        if (b.TryGetValueAsDreamListAbstract(out var bList)) {
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
        if (b.TryGetValueAsDreamListAbstract(out var bList)) {
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
        if (!b.TryGetValueAsDreamListAbstract(out var secondList))
            return DreamValue.False;

        if (GetLength() != secondList.GetLength())
            return DreamValue.False;

        var firstValues = GetKeys();
        var secondValues = secondList.GetKeys();

        var firstListAssoc = GetLoosePairs();
        var secondListAssoc = secondList.GetLoosePairs();

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
