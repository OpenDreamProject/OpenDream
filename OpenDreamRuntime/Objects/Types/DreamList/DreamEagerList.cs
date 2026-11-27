using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

[Virtual]
public class DreamEagerList : DreamList {
    #region Variables

    protected static readonly Stack<List<DreamValue>> ListPool = new();

    public override bool IsAssociative => false;

    protected readonly List<DreamValue> Elements;

    #if TOOLS
    private ProfilerMemory? _tracyContentsMemoryId;
    #endif

    #endregion

    #region Constructors/Initializers/Destructors

    public DreamEagerList(DreamObjectDefinition listDef, int size) : base(listDef) {
        if (size >= DreamManager.ListPoolThreshold && ListPool.TryPop(out var poppedValues)) {
            Elements = poppedValues;
            Elements.EnsureCapacity(size);
        } else {
            Elements = new List<DreamValue>(size);
        }
    }

    /// <summary>
    /// Create a new DreamList using an existing list of values (does not copy them)
    /// </summary>
    public DreamEagerList(DreamObjectDefinition listDef, List<DreamValue> values) : base(listDef) {
        Elements = values;

        foreach (var value in Elements)
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

                            list.Add(new DreamValue(newList));
                            newLists[i * size + j] = newList;
                            newList.DecRef();
                        } else {
                            list.Add(DreamValue.Null);
                        }
                    }
                }

                lists = newLists;
            }
        }
    }

    // Hold on to large lists for reuse later
    protected override void HandleDeletion() {
        foreach (var value in Elements)
            value.DecRef();

        if (Elements.Capacity > DreamManager.ListPoolThreshold && ListPool.Count < DreamManager.ListPoolSize) {
            Elements.Clear();
            ListPool.Push(Elements);
        }

        base.HandleDeletion();
    }

    #endregion

    #region Pure Methods

    public override IEnumerable<DreamValue> Enumerate() {
        return Elements;
    }

    /// <summary>
    /// Returns a copy of the list of array values. Doesn't include the associative values indexable by some of these.
    /// </summary>
    [Obsolete("Deprecated. Use Enumerate() or ToArray() instead.")]
    public override List<DreamValue> CopyKeys() {
        return [..Elements];
    }

    public override List<DreamValue> GetKeysCollection() {
        return Elements;
    }

    public override int GetLength() {
        return Elements.Count;
    }

    [MustDisposeResource]
    public override DreamValue GetElement(DreamValue key) {
        if (key.TryGetValueAsInteger(out int keyInteger)) {
            var value = Elements[keyInteger - 1]; //1-indexed

            value.IncRef();
            return value;
        }

        return DreamValue.Null;
    }

    public override bool ContainsKey(DreamValue value) {
        var count = Elements.Count;

        for (int i = 0; i < count; i++) {
            if (Elements[i].Equals(value))
                return true;
        }

        return false;
    }

    public override int FindIndex(DreamValue value, int start = 1, int end = 0) {
        if (end == 0 || end > Elements.Count) end = Elements.Count + 1;

        for (int i = start; i < end; i++) {
            if (Elements[i - 1].Equals(value)) return i;
        }

        return 0;
    }

    public DantomList Union(DreamList other) {
        DantomList newList = new DantomList(ObjectTree.List.ObjectDefinition, Elements.Union(other.CopyKeys()).ToList(), null);
        if(!(IsAssociative && other.IsAssociative)) {
            return newList;
        }

        Dictionary<DreamValue, DreamValue> ourAssociations = GetTightPairs();
        Dictionary<DreamValue, DreamValue> otherAssociations = other.GetTightPairs();

        foreach (DreamValue value in newList.Enumerate()) {
            // Existing left pairs win, only values newly contributed by the right list use its association
            if (ourAssociations.TryGetValue(value, out DreamValue leftAssociatedValue))
                newList.Set(value, leftAssociatedValue);
            else if (otherAssociations.TryGetValue(value, out DreamValue rightAssociatedValue))
                newList.Set(value, rightAssociatedValue);
        }

        return newList;
    }

    public override string ToString() {
        string assoc = IsAssociative ? ", assoc" : "";
        return $"/list{{len={GetLength()}{assoc}}}";
    }

    public override DreamValue[] ToArray() {
        return Elements.ToArray();
    }

    #endregion

    #region Impure Methods

    public override void SetElementByIndex(DreamValue index, DreamValue element, bool allowGrowth = false) {
        if (!index.TryGetValueAsInteger(out int keyInteger)) {
            throw new InvalidOperationException("Attempted set element by index with a non-integer value.");
        }

        if (allowGrowth && keyInteger == GetLength() + 1) {
            Add(element);
            return;
        }

        if (keyInteger < 1 || keyInteger > Elements.Count) {
            throw new IndexOutOfRangeException();
        }

        DreamValue oldElement = Elements[keyInteger - 1];
        Elements[keyInteger - 1] = element;
        element.IncRef();
        oldElement.DecRef();
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
        }

        UpdateTracyContentsMemory();
    }

    public override void Remove(DreamValue key) {
        int valueIndex = Elements.LastIndexOf(key);

        if (valueIndex != -1) {
            Elements.RemoveAt(valueIndex);
            key.DecRef();
        }

        UpdateTracyContentsMemory();
    }

    public override void Add(DreamValue value) {
        Elements.Add(value);
        value.IncRef();
        UpdateTracyContentsMemory();
    }

    public override void Cut(int start = 1, int end = 0) {
        start = start switch {
            < 0 => throw new ArgumentOutOfRangeException(nameof(start), start, "Parameter start is less than zero."),
            0 => 1,
            _ => start
        };
        if (end == 0 || end > (Elements.Count + 1)) end = Elements.Count + 1;

        if (end > start) {
            for (int i = start; i < end; i++) {
                Elements[i - 1].DecRef();
            }

            Elements.RemoveRange(start - 1, end - start);
        }

        UpdateTracyContentsMemory();
    }

    public override void Insert(int index, DreamValue value) {
        Elements.Insert(index - 1, value);
        value.IncRef();
        UpdateTracyContentsMemory();
    }

    public override void Swap(int index1, int index2) {
        using var first = GetElement(new DreamValue(index1));
        using var second = GetElement(new DreamValue(index2));

        Set(new DreamValue(index1), second);
        Set(new DreamValue(index2), first);
    }

    public void Resize(int size) {
        if (size > Elements.Count) {
            Elements.EnsureCapacity(size);

            for (int i = Elements.Count; i < size; i++) {
                Add(DreamValue.Null);
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

    #if TOOLS
    /// <summary>
    /// The number of DreamValues this list stores, for memory profiling.
    /// </summary>
    protected virtual int TracyContentsCount => Elements.Count;
    #endif

    [Conditional("TOOLS")]
    protected void UpdateTracyContentsMemory() {
        #if TOOLS
        var valueCount = TracyContentsCount;
        var totalSize = Unsafe.SizeOf<DantomList>() + valueCount * Unsafe.SizeOf<DreamValue>();

        _tracyContentsMemoryId?.ReleaseMemory();
        _tracyContentsMemoryId = Profiler.BeginMemoryZone((ulong)totalSize, "/list contents");
        #endif
    }

    #endregion

    #region Operators

    public override DreamValue OperatorIndex(DreamValue index, DMProcState state) {
        return GetElement(index);
    }

    public override DreamValue OperatorSubtract(DreamValue b, DMProcState state) {
        DantomList listCopy = ToDantomList();

        if (b.TryGetValueAsDreamList(out var bList)) {
            foreach (DreamValue value in bList.Enumerate()) {
                listCopy.Remove(value);
            }
        } else {
            listCopy.Remove(b);
        }

        return new DreamValue(listCopy);
    }

    public override DreamValue OperatorOr(DreamValue b, DMProcState state) {
        DantomList list;

        if (b.TryGetValueAsDreamList(out var bList)) {  // List | List
            list = Union(bList);
        } else {                                        // List | x
            list = ToDantomList();
            list.Add(b);
        }

        return new DreamValue(list);
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

    public override DreamValue OperatorMask(DreamValue b, DMProcState state) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            var remainingValues = new Dictionary<DreamValue, int>(bList.GetLength());
            foreach (DreamValue value in bList.Enumerate()) {
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
        if (!b.TryGetValueAsDreamList(out var secondList))
            return DreamValue.False;

        if (GetLength() != secondList.GetLength())
            return DreamValue.False;

        var firstValues = CopyKeys();
        var secondValues = secondList.CopyKeys();

        var firstListAssoc = GetTightPairs();
        var secondListAssoc = secondList.GetTightPairs();

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

        foreach(var value in Enumerate()) {
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
