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
public sealed class DreamAssocList(DreamObjectDefinition aListDef, int size) : DreamObject(aListDef), IDreamList
{
    public bool IsAssociative => true;

    private readonly Dictionary<DreamValue, DreamValue> _values = new(size);

    public DreamAssocList(DreamObjectDefinition listDef, Dictionary<DreamValue, DreamValue>? values) : this(listDef, values?.Count ?? 0)
    {
        if (values != null)
        {
            _values = values;
        }
    }

    protected override void HandleDeletion()
    {
        foreach (var pair in _values)
        {
            pair.Key.DecRef();
            pair.Value.DecRef();
        }

        base.HandleDeletion();
    }

    public void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false)
    {
        if (_values.TryGetValue(key, out var oldValue))
            oldValue.DecRef();
        else
            key.IncRef();

        _values[key] = value;
        value.IncRef();
    }

    [MustDisposeResource]
    public DreamValue GetKey(DreamValue key)
    {
        if (!_values.TryGetValue(key, out var value))
            return DreamValue.Null;

        value.IncRef();
        return value;
    }

    public bool ContainsKey(DreamValue key)
    {
        return _values.ContainsKey(key);
    }

    public override DreamValue OperatorIndex(DreamValue index, DMProcState state)
    {
        return GetKey(index);
    }

    public IEnumerable<DreamValue> EnumerateValues()
    {
        return _values.Keys; // The keys, counter-intuitively
    }

    public int GetLength()
    {
        return _values.Count;
    }

    public void Cut(int start = 1, int end = 0)
    {
        if (start != 1 && start != 0)
        {
            throw new DMException($"Cut() was called with non-default start value of {start}.");
        }

        if (end != 0)
        {
            throw new DMException($"Cut() was called with non-default end value of {end}.");
        }

        foreach (var pair in _values)
        {
            pair.Key.DecRef();
            pair.Value.DecRef();
        }

        _values.Clear();
    }

    public List<DreamValue> GetValues()
    {
        return _values.Keys.ToList();
    }

    public Dictionary<DreamValue, DreamValue> GetAssociativeValues()
    {
        return _values;
    }

    public void RemoveElement(DreamValue value)
    {
        if (_values.Remove(value, out var associatedValue))
        {
            value.DecRef();
            associatedValue.DecRef();
        }
    }

    public IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumerateAssocValues()
    {
        return _values;
    }

    public DreamValue[] CopyToArray()
    {
        var array = new DreamValue[_values.Count];

        _values.Keys.CopyTo(array, 0);
        return array;
    }

    public Dictionary<DreamValue, DreamValue> CopyAssocValues()
    {
        return new(_values);
    }

    public void AddElement(DreamValue value)
    {
        if (ContainsValue(value))
        {
            return; // calling Add("c") on alist("c" = 5) does not change anything
        }

        _values[value] = DreamValue.Null;
        value.IncRef();
    }

    public IDreamList CreateCopy(int start = 1, int end = 0)
    {
        if (start != 1 || end != 0)
        {
            throw new DMException("list index out of bounds");
        }

        var copyValues = new Dictionary<DreamValue, DreamValue>(_values);
        foreach (var value in _values)
        {
            value.Key.IncRef();
            value.Value.IncRef();
        }

        return new DreamAssocList(ObjectDefinition, copyValues);
    }

    public int FindKey(DreamValue value, int start = 1, int end = 0)
    {
        // Unlike list.Find(), alist.Find() doesn't pay attention to start and end, and returns a boolean 0/1 instead of the position of the found object
        if (ContainsValue(value))
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

    public void Insert(int index, DreamValue value)
    {
        throw new DMException("insert not allowed for this list");
    }

    public void Swap(int index1, int index2)
    {
        throw new DMException("swap not allowed for this list");
    }

    public bool ContainsValue(DreamValue value)
    {
        return _values.ContainsKey(value);
    }

    public override DreamValue OperatorAdd(DreamValue b, DMProcState state)
    {
        var listCopy = (DreamAssocList)CreateCopy();

        if (b.TryGetValueAsIDreamList(out var bList))
        {
            foreach (var pair in bList.EnumerateAssocValues())
            {
                if (listCopy.ContainsKey(pair.Key))
                {
                    continue;
                }

                listCopy.SetKeyValuePair(pair.Key, pair.Value);
            }
        }
        else
        {
            listCopy.AddElement(b);
        }

        return new DreamValue(listCopy);
    }

    public override DreamValue OperatorSubtract(DreamValue b, DMProcState state)
    {
        DreamAssocList listCopy;

        if (b.TryGetValueAsIDreamList(out var bList))
        {
            if (bList == this)
            {
                return new DreamValue(ObjectTree.CreateAssocList());
            }

            listCopy = (DreamAssocList)CreateCopy();
            foreach (DreamValue value in bList.EnumerateValues())
            {
                listCopy.RemoveElement(value);
            }
        }
        else
        {
            listCopy = (DreamAssocList)CreateCopy();
            listCopy.RemoveElement(b);
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
        if (b.TryGetValueAsIDreamList(out var bList))
        {
            foreach (var pair in bList.EnumerateAssocValues())
            {
                if (ContainsKey(pair.Key))
                {
                    continue;
                }

                SetKeyValuePair(pair.Key, pair.Value);
            }
        }
        else
        {
            AddElement(b);
        }

        IncRef();
        return new(this);
    }

    public override DreamValue OperatorRemove(DreamValue b)
    {
        if (b.TryGetValueAsIDreamList(out var bList))
        {
            if (bList == this)
            {
                Cut();
            }
            else
            {
                foreach (var value in bList.EnumerateValues())
                {
                    RemoveElement(value);
                }
            }
        }
        else
        {
            RemoveElement(b);
        }

        IncRef();
        return new(this);
    }

    public override DreamValue OperatorMask(DreamValue b)
    {
        if (b.TryGetValueAsIDreamList(out var bList))
        {
            if (bList != this)
            {
                foreach (var value in CopyToArray())
                {
                    if (!bList.ContainsValue(value))
                    {
                        RemoveElement(value);
                    }
                }
            }
        }
        else
        {
            if (!ContainsKey(b))
            {
                Cut();
            }
            else
            {
                using var item = GetKey(b);
                Cut();
                SetKeyValuePair(b, item);
            }
        }

        IncRef();
        return new(this);
    }

    public override DreamValue OperatorEquivalent(DreamValue b)
    {
        if (!b.TryGetValueAsIDreamList(out var secondList))
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

        foreach (var pair in secondList.EnumerateAssocValues())
        {
            if (!ContainsKey(pair.Key))
            {
                return DreamValue.False;
            }

            using var temp = GetKey(pair.Key);
            if (!temp.Equals(pair.Value))
            {
                return DreamValue.False;
            }
        }

        return DreamValue.True;
    }
}
