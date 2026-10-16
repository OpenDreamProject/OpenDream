using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using OpenDreamRuntime.Map;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
using Robust.Server.GameStates;
using Dependency = Robust.Shared.IoC.DependencyAttribute;
namespace OpenDreamRuntime.Objects.Types;

[Virtual]
public class DreamList : DreamObject, IDreamList {
    private static readonly Stack<List<DreamValue>> ListPool = new();

    public override bool ShouldCallNew => false;

    public virtual bool IsAssociative => _associativeValues is { Count: > 0 };

    private readonly List<DreamValue> _elements;
    private Dictionary<DreamValue, DreamValue>? _associativeValues;

    #if TOOLS
    private ProfilerMemory? _tracyContentsMemoryId;
    #endif

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
    /// Returns the list of array values. Doesn't include the associative values indexable by some of these.
    /// </summary>
    [Obsolete("Deprecated. Use EnumerateValues() instead.")]
    public virtual List<DreamValue> GetKeys() {
        return _elements;
    }

    public virtual IEnumerable<DreamValue> EnumerateKeys() {
        return _elements;
    }

    public IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumerateAssocValues() {
        foreach (var value in _elements) {
            if (_associativeValues?.TryGetValue(value, out var associativeValue) is true) {
                yield return new(value, associativeValue);
            } else {
                yield return new(value, DreamValue.Null);
            }
        }
    }

    public Dictionary<DreamValue, DreamValue> CopyAssocValues() {
        if (_associativeValues is null)
            return new();

        return new(_associativeValues);
    }

    public Dictionary<DreamValue, DreamValue> GetAssociativeValues() {
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

    public virtual void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (key.TryGetValueAsInteger(out int keyInteger)) {
            value.IncRef();

            if (allowGrowth && keyInteger == _elements.Count + 1) {
                _elements.Add(value);
            } else {
                _elements[keyInteger - 1].DecRef();
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
        using var temp = GetElement(new DreamValue(index1));

        SetKeyValuePair(new DreamValue(index1), GetElement(new DreamValue(index2)));
        SetKeyValuePair(new DreamValue(index2), temp);
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

    public DreamList Union(DreamList other) {
        DreamList newList = new DreamList(ObjectDefinition, _elements.Union(other.GetKeys()).ToList(), null);
        Dictionary<DreamValue, DreamValue> otherAssociations = other.GetAssociativeValues();
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

    public override void OperatorIndexAssign(DreamValue index, DMProcState state, DreamValue value) {
        SetKeyValuePair(index, value);
    }

    public override DreamValue OperatorAdd(DreamValue b, DMProcState state) {
        DreamList listCopy = (DreamList)CreateCopy();

        if (b.TryGetValueAsDreamList(out var bList)) {
            foreach (DreamValue value in bList.EnumerateKeys()) {
                if (bList._associativeValues?.TryGetValue(value, out var assocValue) is true) {
                    listCopy.SetKeyValuePair(value, assocValue);
                } else {
                    listCopy.AddKey(value);
                }
            }
        } else {
            listCopy.AddKey(b);
        }

        return new DreamValue(listCopy);
    }

    public override DreamValue OperatorSubtract(DreamValue b, DMProcState state) {
        DreamList listCopy = (DreamList)CreateCopy();

        if (b.TryGetValueAsDreamList(out var bList)) {
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

        if (b.TryGetValueAsDreamList(out var bList)) {  // List | List
            list = Union(bList);
        } else {                                        // List | x
            list = (DreamList)CreateCopy();
            list.AddKey(b);
        }

        return new DreamValue(list);
    }

    public override DreamValue OperatorAppend(DreamValue b) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            var values = bList.GetKeys();
            var valueCount = values.Count; // Some lists return a reference to their internal values list which could change with each loop
            for (int i = 0; i < valueCount; i++) {
                var value = values[i];
                AddKey(value); // Always add the value
                if (bList._associativeValues?.TryGetValue(value, out var assocValue) is true) { // Ensure the associated value is correct
                    _associativeValues ??= new();
                    _associativeValues[value] = assocValue;
                    assocValue.IncRef();
                }
            }
        } else {
            AddKey(b);
        }

        IncRef();
        return new(this);
    }

    public override DreamValue OperatorRemove(DreamValue b) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            DreamValue[] values = bList.GetKeys().ToArray();

            foreach (DreamValue value in values) {
                RemoveKey(value);
            }
        } else {
            RemoveKey(b);
        }

        IncRef();
        return new(this);
    }

    public override DreamValue OperatorCombine(DreamValue b) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            foreach (DreamValue value in bList.EnumerateKeys()) {
                if (ContainsKey(value))
                    continue;

                if (bList._associativeValues?.TryGetValue(value, out var associatedValue) is true)
                    SetKeyValuePair(value, associatedValue);
                else
                    AddKey(value);
            }
        } else if (!ContainsKey(b)) {
            AddKey(b);
        }

        IncRef();
        return new(this);
    }

    public override DreamValue OperatorMask(DreamValue b) {
        if (b.TryGetValueAsDreamList(out var bList)) {
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
        return new(this);
    }

    public override DreamValue OperatorEquivalent(DreamValue b) {
        if (!b.TryGetValueAsDreamList(out var secondList))
            return DreamValue.False;
        if (GetLength() != secondList.GetLength())
            return DreamValue.False;

        var firstValues = GetKeys();
        var secondValues = secondList.GetKeys();

        var firstListAssoc = GetAssociativeValues();
        var secondListAssoc = secondList.GetAssociativeValues();

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

// /datum.vars list
internal sealed class DreamListVars(DreamObjectDefinition listDef, DreamObject dreamObject) : DreamList(listDef, 0) {
    public readonly DreamObject DreamObject = dreamObject;

    public override bool IsAssociative =>
        true; // We don't use the associative array but, yes, we behave like an associative list

    public override int GetLength() {
        return DreamObject.GetVariableNames().Concat(DreamObject.ObjectDefinition.GlobalVariables.Keys).Count();
    }

    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        return DreamObject.GetVariableNames().Concat(DreamObject.ObjectDefinition.GlobalVariables.Keys).Select(name => new DreamValue(name));
    }

    public override bool ElementHasAssociation(DreamValue value) {
        if (!value.TryGetValueAsString(out var varName)) {
            return false;
        }

        return DreamObject.HasVariable(varName);
    }

    public override bool ContainsKey(DreamValue value) {
        return ElementHasAssociation(value);
    }

    public override DreamValue GetElement(DreamValue key) {
        if (key.TryGetValueAsInteger(out int keyInteger)) {
            return new DreamValue(DreamObject.GetVariableNames().ElementAt(keyInteger - 1)); //1-indexed
        } else if (key.TryGetValueAsString(out var varName)) {
            if (DreamObject.TryGetVariable(varName, out var objectVar)) {
                return objectVar;
            }

            throw new DMException($"Cannot get value of undefined var \"{key}\" on type {DreamObject.ObjectDefinition.Type}");
        } else {
            throw new DMException($"Invalid var index {key}");
        }
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (key.TryGetValueAsString(out var varName)) {
            if (!DreamObject.HasVariable(varName)) {
                throw new DMException(
                    $"Cannot set value of undefined var \"{varName}\" on type {DreamObject.ObjectDefinition.Type}");
            }

            DreamObject.SetVariable(varName, value);
        } else {
            throw new DMException($"Invalid var index {key}");
        }
    }

    public override bool IsSaved(string name) {
        return DreamObject.IsSaved(name);
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        return GetKeys().IndexOf(value)+1; // IndexOf is 0 indexed, returns -1 on fail, DM is 1 indexed and returns 0 on fail, so +1
    }
}

// global.vars list
internal sealed class DreamGlobalVars(DreamObjectDefinition listDef) : DreamList(listDef, 0) {
    public override bool IsAssociative =>
        true; // We don't use the associative array but, yes, we behave like an associative list

    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        var root = ObjectTree.Root.ObjectDefinition;

        foreach (var key in root.GlobalVariables.Keys) {
            yield return new DreamValue(key);
        }
    }

    public override bool ElementHasAssociation(DreamValue value) {
        if (!value.TryGetValueAsString(out var varName)) {
            return false;
        }

        return ObjectTree.Root.ObjectDefinition.GlobalVariables.ContainsKey(varName);
    }

    public override bool ContainsKey(DreamValue value) {
        return ElementHasAssociation(value);
    }

    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsString(out var varName)) {
            throw new DMException($"Invalid var index {key}");
        }

        var root = ObjectTree.Root.ObjectDefinition;
        if (!root.GlobalVariables.TryGetValue(varName, out var globalId)) {
            throw new DMException($"Invalid global {varName}");
        }

        var value = DreamManager.Globals[globalId];
        value.IncRef();
        return value;
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (key.TryGetValueAsString(out var varName)) {
            var root = ObjectTree.Root.ObjectDefinition;
            if (!root.GlobalVariables.TryGetValue(varName, out var globalId)) {
                throw new DMException($"Cannot set value of undefined global \"{varName}\"");
            }

            DreamManager.SetGlobal(globalId, value);
        } else {
            throw new DMException($"Invalid var index {key}");
        }
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }
}

// client.verbs list
// Keeps track of a client's verbs
public sealed class ClientVerbsList : DreamList {
    public readonly List<DreamProc> Verbs = new();

    private readonly DreamObjectClient _client;

    public ClientVerbsList(DreamObjectTree objectTree, DreamObjectClient client) : base(objectTree.List.ObjectDefinition, 0) {
        _client = client;

        var verbs = _client.ObjectDefinition.Verbs?.Values;
        if (verbs == null)
            return;

        Verbs.EnsureCapacity(verbs.Count);
        foreach (int verbId in verbs) {
            Verbs.Add(objectTree.Procs[verbId]);
        }
    }

    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new DMException($"Invalid index into verbs list: {key}");
        if (index < 1 || index > Verbs.Count)
            throw new DMException($"Out of bounds index on verbs list: {index}");

        return new DreamValue(Verbs[index - 1]);
    }

    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        foreach (DreamProc verb in Verbs)
            yield return new(verb);
    }

    public override bool ContainsKey(DreamValue value) {
        if (!value.TryGetValueAsProc(out var verb))
            return false;

        return Verbs.Contains(verb);
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException("Cannot set the values of a verbs list");
    }

    public override void AddKey(DreamValue value) {
        if (!value.TryGetValueAsProc(out var verb))
            throw new DMException($"Cannot add {value} to verbs list");
        if (Verbs.Contains(verb))
            return; // Even += won't add the verb if it's already in this list

        Verbs.Add(verb);
        VerbSystem?.RegisterVerb(verb);
        VerbSystem?.UpdateClientVerbs(_client);
    }

    public override void RemoveKey(DreamValue value) {
        if (!value.TryGetValueAsProc(out var verb))
            return;

        var valueIndex = Verbs.LastIndexOf(verb);

        if (valueIndex != -1) {
            Verbs.RemoveAt(valueIndex);
            VerbSystem?.UpdateClientVerbs(_client);
        }
    }

    public override void Cut(int start = 1, int end = 0) {
        int verbCount = Verbs.Count + 1;
        if (end == 0 || end > verbCount) end = verbCount;

        Verbs.RemoveRange(start - 1, end - start);
        VerbSystem?.UpdateClientVerbs(_client);
    }

    public override int GetLength() {
        return Verbs.Count;
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }
}

// atom's verbs list
// Keeps track of an appearance's verbs (atom.verbs, mutable_appearance.verbs, etc)
public sealed class VerbsList(DreamObjectTree objectTree, AtomManager atomManager, DreamObjectAtom atom) : DreamList(objectTree.List.ObjectDefinition, 0) {
    public override DreamValue GetElement(DreamValue key) {
        if (VerbSystem == null)
            return DreamValue.Null;
        if (!key.TryGetValueAsInteger(out var index))
            throw new DMException($"Invalid index into verbs list: {key}");

        var verbs = GetVerbs();
        if (index < 1 || index > verbs.Length)
            throw new DMException($"Out of bounds index on verbs list: {index}");

        return new DreamValue(VerbSystem.GetVerb(verbs[index - 1]));
    }

    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        var appearance = atomManager.MustGetAppearance(atom);
        if (VerbSystem == null)
            yield break;

        foreach (var verbId in appearance.Verbs) {
            var verb = VerbSystem.GetVerb(verbId);

            yield return new(verb);
        }
    }

    public override bool ContainsKey(DreamValue value) {
        if (!value.TryGetValueAsProc(out var verb))
            return false;
        if (verb.VerbId == null)
            return false;

        return GetVerbs().Contains(verb.VerbId.Value);
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException("Cannot set the values of a verbs list");
    }

    public override void AddKey(DreamValue value) {
        if (!value.TryGetValueAsProc(out var verb))
            throw new DMException($"Cannot add {value} to verbs list");

        atomManager.UpdateAppearance(atom, appearance => {
            if (!verb.VerbId.HasValue)
                VerbSystem?.RegisterVerb(verb);
            if (!verb.VerbId.HasValue || appearance.Verbs.Contains(verb.VerbId.Value))
                return; // Even += won't add the verb if it's already in this list

            appearance.Verbs.Add(verb.VerbId.Value);
        });
    }

    public override void RemoveKey(DreamValue value) {
        if (!value.TryGetValueAsProc(out var verb))
            return;
        if (verb.VerbId == null) {
            return;
        }

        atomManager.UpdateAppearance(atom, appearance => {
            var valueIndex = appearance.Verbs.LastIndexOf(verb.VerbId.Value);

            if (valueIndex != -1)
                appearance.Verbs.RemoveAt(valueIndex);
        });
    }

    public override void Cut(int start = 1, int end = 0) {
        atomManager.UpdateAppearance(atom, appearance => {
            int count = appearance.Verbs.Count + 1;
            if (end == 0 || end > count) end = count;

            appearance.Verbs.RemoveRange(start - 1, end - start);
        });
    }

    public override int GetLength() {
        return GetVerbs().Length;
    }

    private int[] GetVerbs() {
        var appearance = atomManager.MustGetAppearance(atom);
        if (appearance == null)
            throw new DMException("Atom has no appearance");

        return appearance.Verbs;
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }
}

// atom.overlays or atom.underlays list
// Operates on an object's appearance
public sealed class DreamOverlaysList(DreamObjectDefinition listDef, DreamObject owner, ServerAppearanceSystem? appearanceSystem, bool isUnderlays) : DreamList(listDef, 0) {
    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        var appearance = AtomManager.MustGetAppearance(owner);
        if (appearanceSystem == null)
            yield break;

        foreach (var overlay in GetOverlaysArray(appearance)) {
            yield return new(overlay.ToMutable());
        }
    }

    public override void Cut(int start = 1, int end = 0) {
        AtomManager.UpdateAppearance(owner, appearance => {
            var overlaysList = GetOverlaysList(appearance);
            int count = overlaysList.Count + 1;
            if (end == 0 || end > count) end = count;
            overlaysList.RemoveRange(start - 1, end - start);
        });
    }

    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var overlayIndex) || overlayIndex < 1)
            throw new DMException($"Invalid index into {(isUnderlays ? "underlays" : "overlays")} list: {key}");

        ImmutableAppearance appearance = AtomManager.MustGetAppearance(owner);
        var overlaysList = GetOverlaysArray(appearance);
        if (overlayIndex > overlaysList.Length)
            throw new DMException($"Atom only has {overlaysList.Length} {(isUnderlays ? "underlay" : "overlay")}(s), cannot index {overlayIndex}");

        if (appearanceSystem == null)
            return DreamValue.Null;

        var overlayAppearance = overlaysList[overlayIndex - 1].ToMutable();
        return new DreamValue(overlayAppearance);
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException($"Cannot write to an index of an {(isUnderlays ? "underlays" : "overlays")} list");
    }

    public override void AddKey(DreamValue value) {
        if (appearanceSystem == null)
            return;

        var overlayAppearance = CreateOverlayAppearance(AtomManager, value, AtomManager.MustGetAppearance(owner).Icon);
        var immutableOverlay = appearanceSystem.AddAppearance(overlayAppearance ?? MutableAppearance.Default);
        overlayAppearance?.Dispose();

        //after UpdateAppearance is done, the atom is set with a new immutable appearance containing a hard ref to the overlay
        //only /mutable_appearance handles it differently, and that's done in DreamObjectImage
        AtomManager.UpdateAppearance(owner, appearance => {
            GetOverlaysList(appearance).Add(immutableOverlay);
        });
    }

    public override void RemoveKey(DreamValue value) {
        if (appearanceSystem == null)
            return;

        MutableAppearance? overlayAppearance = CreateOverlayAppearance(AtomManager, value, AtomManager.MustGetAppearance(owner).Icon);
        if (overlayAppearance == null)
            return;

        AtomManager.UpdateAppearance(owner, appearance => {
            GetOverlaysList(appearance).Remove(appearanceSystem.AddAppearance(overlayAppearance, registerAppearance:false));
            overlayAppearance.Dispose();
        });
    }

    public override int GetLength() {
        return GetOverlaysArray(AtomManager.MustGetAppearance(owner)).Length;
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private List<ImmutableAppearance> GetOverlaysList(MutableAppearance appearance) =>
        isUnderlays ? appearance.Underlays : appearance.Overlays;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ImmutableAppearance[] GetOverlaysArray(ImmutableAppearance appearance) =>
        isUnderlays ? appearance.Underlays : appearance.Overlays;

    public static MutableAppearance? CreateOverlayAppearance(AtomManager atomManager, DreamValue value, int? defaultIcon) {
        MutableAppearance overlay;

        if (value.TryGetValueAsString(out var iconState)) {
            overlay = MutableAppearance.Get();
            overlay.IconState = iconState;
            overlay.Icon ??= defaultIcon;
        } else if (atomManager.TryCreateAppearanceFrom(value, out var overlayAppearance)) {
            overlay = overlayAppearance;
        } else {
            return null; // Not a valid overlay
        }

        return overlay;
    }
}

// atom or image vis_contents list
// Operates on an object's appearance
public sealed class DreamVisContentsList : DreamList {
    private readonly PvsOverrideSystem? _pvsOverrideSystem;

    private readonly List<DreamObjectAtom> _visContents = new();
    private readonly DreamObject _owner; // atom or image

    public DreamVisContentsList(DreamObjectDefinition listDef, PvsOverrideSystem? pvsOverrideSystem, DreamObject atom) : base(listDef, 0) {
        IoCManager.InjectDependencies(this);

        _pvsOverrideSystem = pvsOverrideSystem;
        _owner = atom;
    }

    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        foreach (var visContent in _visContents)
            yield return new(visContent);
    }

    public override void Cut(int start = 1, int end = 0) {
        int count = _visContents.Count + 1;
        if (end == 0 || end > count) end = count;

        _visContents.RemoveRange(start - 1, end - start);
        AtomManager.UpdateAppearance(_owner, appearance => {
            appearance.VisContents.RemoveRange(start - 1, end - start);
        });
    }

    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var visContentsIndex) || visContentsIndex < 1)
            throw new DMException($"Invalid index into vis_contents list: {key}");
        if (visContentsIndex > _visContents.Count)
            throw new DMException($"Atom only has {_visContents.Count} vis_contents element(s), cannot index {visContentsIndex}");

        var value = _visContents[visContentsIndex - 1];
        value.IncRef();
        return new DreamValue(value);
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException("Cannot write to an index of a vis_contents list");
    }

    public override void AddKey(DreamValue value) {
        EntityUid entity;
        if (value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable)) {
            if (_visContents.Contains(movable))
                return; // vis_contents cannot contain duplicates
            _visContents.Add(movable);
            entity = movable.Entity;
        } else if (value.TryGetValueAsDreamObject<DreamObjectTurf>(out var turf)) {
            if (_visContents.Contains(turf))
                return; // vis_contents cannot contain duplicates
            _visContents.Add(turf);
            entity = EntityUid.Invalid; // TODO: Support turfs in vis_contents
        } else if (value == DreamValue.Null) {
            return; // vis_contents cannot contain nulls
        } else {
            throw new DMException($"Cannot add {value} to a vis_contents list");
        }

        // TODO: Only override the entity's visibility if its parent atom is visible
        if (entity != EntityUid.Invalid)
            _pvsOverrideSystem?.AddGlobalOverride(entity);

        AtomManager.UpdateAppearance(_owner, appearance => {
            // Add even an invalid UID to keep this and _visContents in sync
            appearance.VisContents.Add(EntityManager.GetNetEntity(entity));
        });
    }

    public override void RemoveKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable))
            return;

        _visContents.Remove(movable);
        AtomManager.UpdateAppearance(_owner, appearance => {
            appearance.VisContents.Remove(EntityManager.GetNetEntity(movable.Entity));
        });
    }

    public override int GetLength() {
        return _visContents.Count;
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }

    public override bool ContainsKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectAtom>(out var dreamObject))
            return false;

        return _visContents.Contains(dreamObject);
    }
}

// atom.filters list
// Operates on an object's appearance
public sealed class DreamFilterList(DreamObjectDefinition listDef, DreamObject owner) : DreamList(listDef, 0) {
    public override void Cut(int start = 1, int end = 0) {
        AtomManager.UpdateAppearance(owner, appearance => {
            int filterCount = appearance.Filters.Count + 1;
            if (end == 0 || end > filterCount) end = filterCount;

            appearance.Filters.RemoveRange(start - 1, end - start);
        });
    }

    public int GetIndexOfFilter(DreamFilter filter) {
        ImmutableAppearance appearance = GetAppearance();
        for (int i = 0; i < appearance.Filters.Length; i++) {
            if (appearance.Filters[i] == filter)
                return i;
        }

        return -1;
    }

    public int GetIndexOfFilter(string filterName) {
        ImmutableAppearance appearance = GetAppearance();
        for (int i = 0; i < appearance.Filters.Length; i++) {
            if (appearance.Filters[i].FilterName == filterName)
                return i;
        }

        return -1;
    }

    public void SetFilter(int index, DreamFilter? filter) {
        AtomManager.UpdateAppearance(owner, appearance => {
            if (index < 1 || index > appearance.Filters.Count)
                throw new DMException($"Cannot index {index} on filter list");

            DreamFilter oldFilter = appearance.Filters[index - 1];

            DreamObjectFilter.FilterAttachedTo.Remove(oldFilter);

            if (filter == null) { // Setting an index to null is the same as removing it ("filters[1] = null")
                appearance.Filters.RemoveAt(index - 1);
            } else {
                appearance.Filters[index - 1] = filter;
                DreamObjectFilter.FilterAttachedTo[filter] = this;
            }
        });
    }

    public override DreamValue GetElement(DreamValue key) {
        int filterIndex;
        if (key.TryGetValueAsString(out var filterName)) {
            filterIndex = GetIndexOfFilter(filterName) + 1; // We're 1-indexed while GetIndexOfFilter() is not
            if (filterIndex < 1) // If a filter by the name doesn't exist, it returns null
                return DreamValue.Null;
        } else {
            key.TryGetValueAsInteger(out filterIndex);
        }

        ImmutableAppearance appearance = GetAppearance();
        if (filterIndex < 1 || filterIndex > appearance.Filters.Length)
            throw new DMException($"Atom only has {appearance.Filters.Length} filter(s), cannot index {filterIndex}");

        DreamObjectFilter filterObject = ObjectTree.CreateObject<DreamObjectFilter>(ObjectTree.Filter);
        filterObject.Filter = appearance.Filters[filterIndex - 1];
        return new DreamValue(filterObject);
    }

    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        ImmutableAppearance appearance = GetAppearance();

        foreach (var filter in appearance.Filters) {
            DreamObjectFilter filterObject = ObjectTree.CreateObject<DreamObjectFilter>(ObjectTree.Filter);
            filterObject.Filter = filter;

            yield return new DreamValue(filterObject);
            filterObject.DecRef();
        }
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (!value.TryGetValueAsDreamObject<DreamObjectFilter>(out var filterObject) && !value.IsNull)
            throw new DMException($"Cannot set value of filter list to {value}");
        if (!key.TryGetValueAsInteger(out var filterIndex) || filterIndex < 1)
            throw new DMException($"Invalid index into filter list: {key}");

        SetFilter(filterIndex, filterObject?.Filter);
    }

    public override void AddKey(DreamValue value) {
        if (value.IsNull) // "filters += null" is just ignored
            return;
        if (!value.TryGetValueAsDreamObject<DreamObjectFilter>(out var filterObject))
            throw new DMException($"Cannot add {value} to filter list");

        //This is dynamic to prevent the compiler from optimising the SerializationManager.CreateCopy() call to the DreamFilter type
        //so we can preserve the subclass information. Setting it to DreamFilter instead will cause filter parameters to stop working.
        dynamic filter = filterObject.Filter;
        DreamFilter copy = SerializationManager.CreateCopy(filter, notNullableOverride: true); // Adding a filter creates a copy

        DreamObjectFilter.FilterAttachedTo[copy] = this;
        AtomManager.UpdateAppearance(owner, appearance => {
            appearance.Filters.Add(copy);
        });
    }

    public override void RemoveKey(DreamValue value) {
        int filterIndex = -1;
        if (value.TryGetValueAsString(out var filterName)) { // You can also do atom.filters -= "name"
            filterIndex = GetIndexOfFilter(filterName);
        } else if (value.TryGetValueAsDreamObject<DreamObjectFilter>(out var filterObject)) {
            filterIndex = GetIndexOfFilter(filterObject.Filter);
        }

        if (filterIndex < 0) // Failed to find the filter in the list
            return;

        AtomManager.UpdateAppearance(owner, appearance => {
            DreamObjectFilter.FilterAttachedTo.Remove(appearance.Filters[filterIndex]);
            appearance.Filters.RemoveAt(filterIndex);
        });
    }

    public override int GetLength() {
        return GetAppearance().Filters.Length;
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }

    private ImmutableAppearance GetAppearance() {
        ImmutableAppearance? appearance = AtomManager.MustGetAppearance(owner);
        if (appearance == null)
            throw new DMException("Atom has no appearance");

        return appearance;
    }
}

// client.screen list
public sealed class ClientScreenList(DreamObjectTree objectTree, ServerScreenOverlaySystem? screenOverlaySystem, DreamConnection connection)
    : DreamList(objectTree.List.ObjectDefinition, 0) {
    private readonly List<DreamValue> _screenObjects = new();

    protected override void HandleDeletion() {
        Cut();
        base.HandleDeletion();
    }

    public override bool ContainsKey(DreamValue value) {
        return _screenObjects.Contains(value);
    }

    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var screenIndex) || screenIndex < 1 || screenIndex > _screenObjects.Count)
            throw new DMException($"Invalid index into screen list: {key}");

        var value = _screenObjects[screenIndex - 1];
        value.IncRef();
        return value;
    }

    public override List<DreamValue> GetKeys() {
        return _screenObjects;
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        return _screenObjects;
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException("Cannot write to an index of a screen list");
    }

    public override void AddKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable))
            return;

        screenOverlaySystem?.AddScreenObject(connection, movable);
        _screenObjects.Add(value);
        value.IncRef();
    }

    public override void RemoveKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable))
            return;

        screenOverlaySystem?.RemoveScreenObject(connection, movable);
        if (_screenObjects.Remove(value))
            value.DecRef();
    }

    public override void Cut(int start = 1, int end = 0) {
        if (end == 0 || end > _screenObjects.Count + 1) end = _screenObjects.Count + 1;

        for (int i = start - 1; i < end - 1; i++) {
            using var value = _screenObjects[i];
            if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable))
                continue;

            screenOverlaySystem?.RemoveScreenObject(connection, movable);
        }

        _screenObjects.RemoveRange(start - 1, end - start);
    }

    public override int GetLength() {
        return _screenObjects.Count;
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }
}

// client.images list
public sealed class ClientImagesList(DreamObjectTree objectTree, ServerClientImagesSystem? clientImagesSystem, DreamConnection connection) : DreamList(objectTree.List.ObjectDefinition, 0) {
    private readonly List<DreamValue> _imageObjects = new();

    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var imageIndex) || imageIndex < 1 || imageIndex > _imageObjects.Count)
            throw new DMException($"Invalid index into client images list: {key}");

        var value = _imageObjects[imageIndex - 1];
        value.IncRef();
        return value;
    }

    public override List<DreamValue> GetKeys() {
        return _imageObjects;
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        return _imageObjects;
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException("Cannot write to an index of a client images list");
    }

    public override void AddKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectImage>(out var image))
            return;

        clientImagesSystem?.AddImageObject(connection, image);
        _imageObjects.Add(value);
    }

    public override void RemoveKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectImage>(out var image))
            return;

        clientImagesSystem?.RemoveImageObject(connection, image);
        _imageObjects.Remove(value);
    }

    public override void Cut(int start = 1, int end = 0) {
        if (end == 0 || end > _imageObjects.Count + 1) end = _imageObjects.Count + 1;

        for (int i = start - 1; i < end - 1; i++) {
            if (!_imageObjects[i].TryGetValueAsDreamObject<DreamObjectImage>(out var image))
                continue;

            clientImagesSystem?.RemoveImageObject(connection, image);
        }

        _imageObjects.RemoveRange(start - 1, end - start);
    }

    public override int GetLength() {
        return _imageObjects.Count;
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }

    public override bool ContainsKey(DreamValue value) => _imageObjects.Contains(value);
}

// world.contents list
// Operates on a list of all atoms
public sealed class WorldContentsList(DreamObjectDefinition listDef, AtomManager atomManager) : DreamList(listDef, 0) {
    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new DMException($"Invalid index into world contents list: {key}");
        if (index < 1 || index > atomManager.AtomCount)
            throw new DMException($"Out of bounds index on world contents list: {index}");

        var element = atomManager.EnumerateAtoms().ElementAt(index - 1); // Ouch
        element.IncRef();
        return new DreamValue(element);
    }

    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        return AtomManager.EnumerateAtoms().Select(atom => new DreamValue(atom));
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException("Cannot set the value of world contents list");
    }

    public override void AddKey(DreamValue value) {
        throw new DMException("Cannot append to world contents list");
    }

    public override void Cut(int start = 1, int end = 0) {
        throw new DMException("Cannot cut world contents list");
    }

    public override int GetLength() {
        return atomManager.AtomCount;
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }

    public override bool ContainsKey(DreamValue value) {
        // world.contents is a list of every atom that exists,
        // so this should be equivalent across the board
        return value.TryGetValueAsDreamObject<DreamObjectAtom>(out _);
    }
}

// turf.contents list
public sealed class TurfContentsList(DreamObjectDefinition listDef, DreamObjectTurf turf) : DreamList(listDef, 0) {
    private IDreamMapManager.Cell Cell => turf.Cell;

    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new DMException($"Invalid index into turf contents list: {key}");
        if (index < 1 || index > Cell.Movables.Count)
            throw new DMException($"Out of bounds index on turf contents list: {index}");

        var value = Cell.Movables[index - 1];
        value.IncRef();
        return new DreamValue(value);
    }

    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        foreach (var movable in Cell.Movables)
            yield return new(movable);
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException("Cannot set an index of turf contents list");
    }

    public override void AddKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable))
            throw new DMException($"Cannot add {value} to turf contents");

        movable.SetLoc(Cell.Turf);
    }

    public override void Cut(int start = 1, int end = 0) {
        int movableCount = Cell.Movables.Count + 1;
        if (end == 0 || end > movableCount) end = movableCount;

        for (int i = start; i < end; i++) {
            Cell.Movables[i - 1].SetLoc(null);
        }
    }

    public override int GetLength() {
        return Cell.Movables.Count;
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }

    public override bool ContainsKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var dreamObject))
            return false;

        return dreamObject.Loc == turf;
    }
}

// area.contents list
public sealed class AreaContentsList(DreamObjectDefinition listDef, DreamObjectArea area) : DreamList(listDef, 0) {
    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new DMException($"Invalid index into area contents list: {key}");

        foreach (var turf in area.Turfs) {
            if (index < 1)
                break;

            if (index == 1) { // The index references this turf
                turf.IncRef();
                return new(turf);
            }

            index -= 1;
            int contentsLength = turf.Contents.GetLength();

            if (index <= contentsLength) { // The index references one of the turf's contents
                var contentsItem = turf.Contents.GetElement(new(index));
                return contentsItem;
            }

            index -= contentsLength;
        }

        throw new DMException($"Out of bounds index on turf contents list: {key}");
    }

    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        foreach (var turf in area.Turfs) {
            yield return new(turf);

            foreach (var content in turf.Contents.EnumerateKeys())
                yield return content;
        }
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException("Cannot set an index of area contents list");
    }

    public override void AddKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectTurf>(out var turf))
            throw new DMException($"Cannot add {value} to area contents");

        turf.Cell.Area = area;
    }

    public override void RemoveKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectTurf>(out var turf))
            throw new DMException($"Cannot remove {value} from area contents");

        turf.Cell.Area = DreamMapManager.DefaultArea;
    }

    public override void Cut(int start = 1, int end = 0) {
        // TODO
    }

    public override int GetLength() {
        int length = area.Turfs.Count;

        foreach (var turf in area.Turfs)
            length += turf.Contents.GetLength();

        return length;
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }

    public override bool ContainsKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectAtom>(out var atom))
            return false;

        if (atom is DreamObjectArea) // areas do not contain themselves
            return false;

        var (x, y, z) = AtomManager.GetAtomPosition(atom);
        if (!DreamMapManager.TryGetCellAt((x, y), z, out var cell))
            return false;

        return cell.Area == area;
    }
}

// mob.contents, obj.contents list
public sealed class MovableContentsList(DreamObjectDefinition listDef, DreamObjectMovable owner, TransformComponent transform) : DreamList(listDef, 0) {
    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new DMException($"Invalid index into movable contents list: {key}");
        if (index < 1 || index > transform.ChildCount)
            throw new DMException($"Out of bounds index on movable contents list: {index}");

        using var childEnumerator = transform.ChildEnumerator;
        while (index >= 1) {
            childEnumerator.MoveNext(out EntityUid child);

            if (index == 1) {
                if (AtomManager.TryGetMovableFromEntity(child, out var childObject)) {
                    childObject.IncRef();
                    return new DreamValue(childObject);
                } else
                    throw new DMException($"Invalid child in movable contents list: {child}");
            }

            index--;
        }

        throw new DMException($"Out of bounds index on movable contents list after iterating: {key}");
    }

    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        using var childEnumerator = transform.ChildEnumerator;

        while (childEnumerator.MoveNext(out EntityUid child)) {
            if (!AtomManager.TryGetMovableFromEntity(child, out var childObject))
                continue;

            yield return new DreamValue(childObject);
        }
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        if (end == 0 || end > transform.ChildCount) end = transform.ChildCount;

        using var childEnumerator = transform.ChildEnumerator;

        int i = 0;
        while (childEnumerator.MoveNext(out EntityUid child)) {
            if (!AtomManager.TryGetMovableFromEntity(child, out var childObject))
                continue;
            i++;
            if (i >= start && new DreamValue(childObject).Equals(value))
                return i;
            if (i > end)
                return 0;
        }

        return 0;
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException("Cannot set an index of movable contents list");
    }

    public override void AddKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var dreamObject))
            throw new DMException($"Cannot add {value} to movable contents");

        dreamObject.SetLoc(owner);
    }

    public override void RemoveKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable))
            throw new DMException($"Cannot remove {value} from movable contents");
        if (movable.Loc != owner)
            return; // This object wasn't in our contents to begin with

        movable.SetLoc(null);
    }

    public override bool ContainsKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var dreamObject))
            return false;

        return dreamObject.Loc == owner;
    }

    public override void Cut(int start = 1, int end = 0) {
        // TODO
    }

    public override int GetLength() {
        return transform.ChildCount;
    }
}

// proc args list
internal sealed class ProcArgsList(DreamObjectDefinition listDef, ProcState state) : DreamList(listDef, 0) {
    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new DMException($"Invalid index into args list: {key}");
        if (index < 1 || index > state.ArgumentCount)
            throw new DMException($"Out of bounds index on args list: {index}");

        var value = state.GetArguments()[index - 1];
        value.IncRef();
        return value;
    }

    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        for (int i = 0; i < state.ArgumentCount; i++)
            yield return state.GetArguments()[i];
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new DMException($"Invalid index into args list: {key}");
        if (index < 1 || index > state.ArgumentCount)
            throw new DMException($"Out of bounds index on args list: {index}");

        state.SetArgument(index - 1, value);
    }

    public override void AddKey(DreamValue value) {
        throw new DMException("Cannot add new values to args list");
    }

    public override void RemoveKey(DreamValue value) {
        throw new DMException("Cannot remove values to args list");
    }

    public override void Cut(int start = 1, int end = 0) {
        throw new DMException("Cannot cut args list");
    }

    public override int GetLength() {
        return state.ArgumentCount;
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }
}

// Savefile Dir List - always synced with Savefiles currentDir. Only stores keys.
public sealed class SavefileDirList(DreamObjectDefinition listDef, DreamObjectSavefile backedSaveFile) : DreamList(listDef, 0) {
    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new DMException($"Invalid index on savefile dir list: {key}");
        if (index < 1 || index > backedSaveFile.CurrentDir.Count)
            throw new DMException($"Out of bounds index on savefile dir list: {index}");
        return new DreamValue(backedSaveFile.CurrentDir.Keys.ElementAt(index - 1));
    }

    public override bool ContainsKey(DreamValue value) {
        return value.TryGetValueAsString(out var str) && backedSaveFile.CurrentDir.ContainsKey(str);
    }

    public override List<DreamValue> GetKeys() {
        return EnumerateKeys().ToList();
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        foreach (string value in backedSaveFile.CurrentDir.Keys.OrderBy(x => x))
            yield return new DreamValue(value);
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new DMException($"Invalid index on savefile dir list: {key}");
        if (!value.TryGetValueAsString(out var valueStr))
            throw new DMException($"Invalid value on savefile dir name: {value}");
        if (index < 1 || index > backedSaveFile.CurrentDir.Count)
            throw new DMException($"Out of bounds index on savefile dir list: {index}");

        backedSaveFile.RenameSavefileValue(backedSaveFile.CurrentDir.Keys.ElementAt(index - 1), valueStr);
    }

    public override void AddKey(DreamValue value) {
        if (!value.TryGetValueAsString(out var valueStr))
            throw new DMException($"Invalid value on savefile dir name: {value}");
        backedSaveFile.AddSavefileDir(valueStr);
    }

    public override void RemoveKey(DreamValue value) {
        if (!value.TryGetValueAsString(out var valueStr))
            throw new DMException($"Invalid value on savefile dir name: {value}");
        backedSaveFile.RemoveSavefileValue(valueStr);
    }

    public override void Cut(int start = 1, int end = 0) {
        throw new DMException("Cannot cut savefile dir list"); //BYOND actually throws undefined proc for this
    }

    public override int GetLength() {
        return backedSaveFile.CurrentDir.Count;
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }
}
