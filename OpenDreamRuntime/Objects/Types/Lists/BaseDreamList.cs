using System.Linq;
using JetBrains.Annotations;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

public abstract class BaseDreamList(DreamObjectDefinition objectDefinition) : DreamObject(objectDefinition), IDreamList {
    public sealed override bool ShouldCallNew => false;

    protected sealed override bool TryGetVar(string varName, out DreamValue value) {
        if (varName == "len") {
            value = new(GetLength());
            return true;
        }

        // Note that invalid vars on /list will give null and not error in BYOND
        // We don't replicate that
        return base.TryGetVar(varName, out value);
    }

    protected sealed override void SetVar(string varName, DreamValue value) {
        if (varName == "len") {
            value.TryGetValueAsInteger(out var newLen);

            Resize(newLen);
        } else {
            base.SetVar(varName, value);
        }
    }

    public override string ToString() {
        string assoc = IsAssociative ? ", assoc" : "";
        return $"/list{{len={GetLength()}{assoc}}}";
    }

    #region List Operations

    public abstract void AddValue(DreamValue value);
    public abstract void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false);
    public abstract void RemoveValue(DreamValue value);
    public abstract int FindValue(DreamValue value, int start = 1, int end = 0);
    [MustDisposeResource]
    public abstract DreamValue GetValue(DreamValue key);
    public abstract bool ContainsValue(DreamValue value);
    public abstract int GetLength();
    public abstract IEnumerable<DreamValue> EnumerateValues();
    [Obsolete($"Prefer {nameof(EnumerateValues)} instead")]
    public abstract List<DreamValue> GetValues();

    public abstract IDreamList CreateCopy(int start = 1, int end = 0);
    public virtual void Cut(int start = 1, int end = 0) => throw new NotSupportedException($"{GetType()} does not support Cut");
    public virtual void Resize(int size) => throw new NotSupportedException($"{GetType()} cannot be resized");
    public virtual void Insert(int index, DreamValue value) => throw new NotSupportedException($"{GetType()} does not support Insert");
    public virtual void Swap(int index1, int index2) => throw new NotSupportedException($"{GetType()} does not support Swap");
    public DreamList Union(BaseDreamList other) {
        DreamList newList = new(ObjectDefinition, EnumerateValues().Union(other.EnumerateValues()).ToList(), null);
        foreach ((DreamValue key, DreamValue value) in other.GetAssociativeValues()) {
            newList.SetValue(key, value);
        }

        return newList;
    }

    public virtual bool IsAssociative { get => false; }
    public virtual bool ContainsKey(DreamValue key) => false;
    public virtual IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumerateAssocValues() => [];
    public virtual Dictionary<DreamValue, DreamValue> GetAssociativeValues() => [];

    public DreamValue[] CopyToArray() => [.. EnumerateValues()];
    public virtual Dictionary<DreamValue, DreamValue> CopyAssocValues() => new(EnumerateAssocValues());

    #endregion List Operations

    #region Operators

    public sealed override DreamValue OperatorIndex(DreamValue index, DMProcState state) {
        return GetValue(index);
    }

    public sealed override void OperatorIndexAssign(DreamValue index, DMProcState state, DreamValue value) {
        SetValue(index, value);
    }

    // TODO: the following operators should be sealed, but alists have special behaviours
    public override DreamValue OperatorAdd(DreamValue b, DMProcState state) {
        DreamList listCopy = (DreamList)CreateCopy();

        if (b.TryGetValueAsDreamList(out var bList)) {
            var bAssocValues = bList.GetAssociativeValues();
            foreach (DreamValue value in bList.EnumerateValues()) {
                if (bAssocValues?.TryGetValue(value, out var assocValue) is true) {
                    listCopy.SetValue(value, assocValue);
                } else {
                    listCopy.AddValue(value);
                }
            }
        } else {
            listCopy.AddValue(b);
        }

        return new DreamValue(listCopy);
    }

    public override DreamValue OperatorSubtract(DreamValue b, DMProcState state) {
        DreamList listCopy = (DreamList)CreateCopy();

        if (b.TryGetValueAsDreamList(out var bList)) {
            foreach (DreamValue value in bList.EnumerateValues()) {
                listCopy.RemoveValue(value);
            }
        } else {
            listCopy.RemoveValue(b);
        }

        return new DreamValue(listCopy);
    }

    public override DreamValue OperatorOr(DreamValue b, DMProcState state) {
        DreamList list;

        if (b.TryGetValueAsDreamList(out var bList)) {  // List | List
            list = Union(bList);
        } else {                                        // List | x
            list = (DreamList)CreateCopy();
            list.AddValue(b);
        }

        return new DreamValue(list);
    }

    public override DreamValue OperatorAppend(DreamValue b) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            var values = bList.GetValues();
            var valueCount = values.Count; // Some lists return a reference to their internal values list which could change with each loop

            var assocValues = GetAssociativeValues();
            var bAssocValues = bList.GetAssociativeValues();
            for (int i = 0; i < valueCount; i++) {
                var value = values[i];
                AddValue(value); // Always add the value

                if (bAssocValues.TryGetValue(value, out var aValue) is true) { // Ensure the associated value is correct
                    assocValues[value] = aValue;
                    aValue.IncRef();
                }
            }
        } else {
            AddValue(b);
        }

        IncRef();
        return new(this);
    }

    public override DreamValue OperatorRemove(DreamValue b) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            DreamValue[] values = bList.CopyToArray();

            foreach (DreamValue value in values) {
                RemoveValue(value);
            }
        } else {
            RemoveValue(b);
        }

        IncRef();
        return new(this);
    }

    public override DreamValue OperatorCombine(DreamValue b) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            var assocValues = bList.GetAssociativeValues();
            foreach (DreamValue value in bList.EnumerateValues()) {
                if (ContainsValue(value))
                    continue;

                if (assocValues.TryGetValue(value, out var associatedValue) is true)
                    SetValue(value, associatedValue);
                else
                    AddValue(value);
            }
        } else if (!ContainsValue(b)) {
            AddValue(b);
        }

        IncRef();
        return new(this);
    }

    public override DreamValue OperatorMask(DreamValue b) {
        if (b.TryGetValueAsDreamList(out var bList)) {
            for (int i = 1; i <= GetLength(); i++) {
                using var value = GetValue(new DreamValue(i));

                if (!bList.ContainsValue(value)) {
                    Cut(i, i + 1);
                    i--;
                }
            }
        } else {
            for (int i = 1; i <= GetLength(); i++) {
                using var value = GetValue(new DreamValue(i));

                if (value != b) {
                    Cut(i, i + 1);
                    i--;
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

        var firstValues = GetValues();
        var secondValues = secondList.GetValues();

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

    public sealed override void OperatorOutput(DreamValue b) {
        HashSet<DreamConnection> passedConnections = new(); // BYOND only outputs to a client once per list

        foreach(var value in EnumerateValues()) {
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
