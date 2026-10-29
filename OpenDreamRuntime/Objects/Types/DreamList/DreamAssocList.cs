using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

/// <summary>
/// Associative-only alternative to DreamList. Any time a key is added without an explicit value, DreamValue.Null will be implied.
/// </summary>
/// <param name="aListDef"></param>
/// <param name="size"></param>
// TODO: An arglist given to New() can be used to initialize an alist with values
public sealed class DreamAssocList(DreamObjectDefinition aListDef, int size) : DreamList(aListDef)
{
    public override bool IsAssociative => true;

    private readonly Dictionary<DreamValue, DreamValue> _pairs = new(size);

    public DreamAssocList(DreamObjectDefinition listDef, Dictionary<DreamValue, DreamValue>? pairs) : this(listDef, pairs?.Count ?? 0)
    {
        if (pairs != null)
        {
            _pairs = pairs;
        }
    }

    protected override void HandleDeletion()
    {
        foreach (var pair in _pairs)
        {
            pair.Key.DecRef();
            pair.Value.DecRef();
        }

        base.HandleDeletion();
    }

    public override void Set(DreamValue key, DreamValue value, bool allowGrowth = false)
    {
        if (_pairs.TryGetValue(key, out var oldValue))
            oldValue.DecRef();
        else
            key.IncRef();

        _pairs[key] = value;
        value.IncRef();
    }

    [MustDisposeResource]
    public override DreamValue GetElement(DreamValue key)
    {
        if (!_pairs.TryGetValue(key, out var value))
            return DreamValue.Null;

        value.IncRef();
        return value;
    }

    public override bool TryGetElementByKey(DreamValue key, [MustDisposeResource][MaybeNullWhen(false)] out DreamValue value) {
        if (!_pairs.TryGetValue(key, out var _value)) {
            value = DreamValue.Null;
            return true;
        }
        value = _value;
        value.IncRef();
        return true;
    }

    public override bool ElementHasAssociation(DreamValue key) {
        return _pairs.ContainsKey(key);
    }

    public override bool ContainsKey(DreamValue value)
    {
        return _pairs.ContainsKey(value);
    }

    public override IEnumerable<DreamValue> Enumerate()
    {
        return _pairs.Keys;
    }

    public override int GetLength()
    {
        return _pairs.Count;
    }

    public override void Cut(int start = 1, int end = 0)
    {
        if (start != 1 && start != 0)
        {
            throw new DMException($"Cut() was called with non-default start value of {start}.");
        }

        if (end != 0)
        {
            throw new DMException($"Cut() was called with non-default end value of {end}.");
        }

        foreach (var pair in _pairs)
        {
            pair.Key.DecRef();
            pair.Value.DecRef();
        }

        _pairs.Clear();
    }

    public override List<DreamValue> CopyKeys()
    {
        return _pairs.Keys.ToList();
    }

    public override Dictionary<DreamValue, DreamValue> GetLoosePairs()
    {
        return _pairs;
    }

    public override Dictionary<DreamValue, DreamValue> GetTightPairs()
    {
        return _pairs;
    }

    public override IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumerateTightPairs()
    {
        return _pairs;
    }

    public override IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumerateLoosePairs()
    {
        return _pairs;
    }

    public override void Remove(DreamValue value)
    {
        if (_pairs.Remove(value, out var associatedValue))
        {
            value.DecRef();
            associatedValue.DecRef();
        }
    }


    public override DreamValue[] ToArray()
    {
        var array = new DreamValue[_pairs.Count];

        _pairs.Keys.CopyTo(array, 0);
        return array;
    }

    public Dictionary<DreamValue, DreamValue> CopyPairs()
    {
        return new(_pairs);
    }

    public override void Add(DreamValue value)
    {
        if (ElementHasAssociation(value))
        {
            return; // calling Add("c") on alist("c" = 5) does not change anything
        }

        _pairs[value] = DreamValue.Null;
        value.IncRef();
    }

    [Obsolete("Consider ToDantomList")]
    public override DreamAssocList ToCopy(int start = 1, int end = 0)
    {
        if (start != 1 || end != 0)
        {
            throw new DMException("list index out of bounds");
        }

        var copyValues = new Dictionary<DreamValue, DreamValue>(_pairs);
        foreach (var value in _pairs)
        {
            value.Key.IncRef();
            value.Value.IncRef();
        }

        return new DreamAssocList(ObjectDefinition, copyValues);
    }

    public override int FindIndex(DreamValue value, int start = 1, int end = 0)
    {
        // Unlike list.Find(), alist.Find() doesn't pay attention to start and end, and returns a boolean 0/1 instead of the position of the found object
        if (ElementHasAssociation(value))
        {
            return 1;
        }

        return 0;
    }

    protected override bool TryGetVar(string varName, out DreamValue value)
    {
        if (varName == "len")
        {
            value = new(GetLength());
            return true;
        }

        return base.TryGetVar(varName, out value);
    }

    protected override void SetVar(string varName, DreamValue value)
    {
        if (varName == "len")
        {
            // Non-nums become 0 which is parity w/ BYOND
            if (value.TryGetValueAsInteger(out var newLen) && newLen != 0)
            {
                throw new DMException("length of strict associative list can only be set to 0");
            }

            // alists specifically will always either runtime or get set to 0 and cut
            Cut();
        }
        else
        {
            base.SetVar(varName, value);
        }
    }

    #region Operators

    public override DreamValue OperatorIndex(DreamValue index, DMProcState state) {
        return GetElement(index);
    }

    public override DreamValue OperatorAdd(DreamValue b, DMProcState state) {
        var listCopy = ToCopy();

        if (b.TryGetValueAsDreamList(out var bList)) {
            foreach (var pair in bList.EnumerateLoosePairs()) {
                if (listCopy.ElementHasAssociation(pair.Key)) {
                    continue;
                }

                listCopy.Set(pair.Key, pair.Value);
            }
        } else {
            listCopy.Add(b);
        }

        return new DreamValue(listCopy);
    }

    public override DreamValue OperatorSubtract(DreamValue b, DMProcState state)
    {
        DreamAssocList listCopy;

        if (b.TryGetValueAsDreamList(out var bList))
        {
            if (bList == this)
            {
                return new DreamValue(ObjectTree.CreateAssocList());
            }

            listCopy = ToCopy();
            foreach (DreamValue value in bList.Enumerate())
            {
                listCopy.Remove(value);
            }
        }
        else
        {
            listCopy = ToCopy();
            listCopy.Remove(b);
        }

        return new DreamValue(listCopy);
    }

    public override DreamValue OperatorOr(DreamValue b, DMProcState state)
    {
        return OperatorAdd(b, state);
    }

    public override DreamValue OperatorCombine(DreamValue b)
    {
        return OperatorAppend(b);
    }

    public override DreamValue OperatorAppend(DreamValue b)
    {
        if (b.TryGetValueAsDreamList(out var bList))
        {
            foreach (var pair in bList.EnumerateLoosePairs())
            {
                if (ElementHasAssociation(pair.Key))
                {
                    continue;
                }

                Set(pair.Key, pair.Value);
            }
        }
        else
        {
            Add(b);
        }

        IncRef();
         return new DreamValue(this);
    }

    public override DreamValue OperatorRemove(DreamValue b)
    {
        if (b.TryGetValueAsDreamList(out var bList))
        {
            if (bList == this)
            {
                Cut();
            }
            else
            {
                foreach (var value in bList.Enumerate())
                {
                    Remove(value);
                }
            }
        }
        else
        {
            Remove(b);
        }

        IncRef();
        return new DreamValue(this);
    }

    public override DreamValue OperatorMask(DreamValue b)
    {
        if (b.TryGetValueAsDreamList(out var bList))
        {
            if (bList != this)
            {
                foreach (var value in ToArray())
                {
                    if (!bList.ContainsKey(value))
                    {
                        Remove(value);
                    }
                }
            }
        }
        else
        {
            if (!ElementHasAssociation(b))
            {
                Cut();
            }
            else
            {
                using var item = GetElement(b);
                Cut();
                Set(b, item);
            }
        }

        IncRef();
         return new DreamValue(this);
    }

    public override DreamValue OperatorEquivalent(DreamValue b)
    {
        if (!b.TryGetValueAsDreamList(out var secondList))
        {
            return DreamValue.False;
        }

        if (secondList == this)
        {
            return DreamValue.True;
        }

        if (GetLength() != secondList.GetLength())
        {
            return DreamValue.False;
        }

        foreach (var pair in secondList.EnumerateLoosePairs())
        {
            if (!ElementHasAssociation(pair.Key))
            {
                return DreamValue.False;
            }

            using var temp = GetElement(pair.Key);
            if (!temp.Equals(pair.Value))
            {
                return DreamValue.False;
            }
        }

        return DreamValue.True;
    }
}
#endregion
