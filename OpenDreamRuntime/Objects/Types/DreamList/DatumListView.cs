using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

/// <summary>
/// A script-facing wrapper around a list owned by a datum (e.g. atom.overlays).
/// </summary>
/// <remarks>
/// The owner caches the view and holds its initial ref, which is not forwarded to the owner, so no cycle is formed.
/// Every ref after that also counts as a ref to the owner, so the owner stays alive while its list is referenced.
/// DM code can't del() the view; it's only deleted along with its owner.
/// </remarks>
public sealed class DatumListView : DreamList {
    private readonly DreamList InternalList;
    private readonly DreamObject Owner;

    private DatumListView(DreamObjectDefinition listDef, DreamList internalList, DreamObject owner) : base(listDef) {
        InternalList = internalList;
        Owner = owner;
        ForwardsRefs = true;
    }

    /// <summary>
    /// Returns the view cached in <paramref name="view"/>, creating it first if needed.
    /// </summary>
    public static DatumListView GetOrCreate(ref DatumListView? view, DreamList internalList, DreamObject owner) {
        return view ??= new(internalList.ObjectDefinition, internalList, owner);
    }

    public override bool IsDeletable => false;

    public override bool IsAssociative => InternalList.IsAssociative;

    protected override void OnIncRef() => Owner.IncRef();

    protected override void OnDecRef() => Owner.DecRef();

    /// <summary>
    /// Replaces this view with the internal list when it's an operand, so self-referencing operations
    /// (e.g. "L -= L") see the list they're operating on.
    /// </summary>
    private DreamValue Unwrap(DreamValue value) {
        return value.TryGetValueAsDreamObject(out var obj) && obj == this ? new DreamValue(InternalList) : value;
    }

    /// <summary>
    /// Replaces the internal list with this view when an operator returns it (in-place operators return their list),
    /// so script never holds a ref to the internal list that isn't forwarded to the owner.
    /// </summary>
    [MustDisposeResource]
    private DreamValue Rewrap([HandlesResourceDisposal] DreamValue result) {
        if (!result.TryGetValueAsDreamObject(out var obj) || obj != InternalList)
            return result;

        result.Dispose();
        IncRef();
        return new DreamValue(this);
    }

    #region Operators

    public override DreamValue OperatorAdd(DreamValue b, DMProcState state) => Rewrap(InternalList.OperatorAdd(Unwrap(b), state));

    public override DreamValue OperatorAppend(DreamValue b) => Rewrap(InternalList.OperatorAppend(Unwrap(b)));

    public override DreamValue OperatorSubtract(DreamValue b, DMProcState state) => Rewrap(InternalList.OperatorSubtract(Unwrap(b), state));

    public override DreamValue OperatorRemove(DreamValue b) => Rewrap(InternalList.OperatorRemove(Unwrap(b)));

    public override DreamValue OperatorOr(DreamValue b, DMProcState state) => Rewrap(InternalList.OperatorOr(Unwrap(b), state));

    public override DreamValue OperatorCombine(DreamValue b, DMProcState state) => Rewrap(InternalList.OperatorCombine(Unwrap(b), state));

    public override DreamValue OperatorAnd(DreamValue b, DMProcState state) => Rewrap(InternalList.OperatorAnd(Unwrap(b), state));

    public override DreamValue OperatorMask(DreamValue b, DMProcState state) => Rewrap(InternalList.OperatorMask(Unwrap(b), state));

    public override DreamValue OperatorEquivalent(DreamValue b) => InternalList.OperatorEquivalent(Unwrap(b));

    public override void OperatorOutput(DreamValue b) => InternalList.OperatorOutput(Unwrap(b));

    public override DreamValue OperatorIndex(DreamValue index, DMProcState state) => Rewrap(InternalList.OperatorIndex(index, state));

    #endregion

    public override List<DreamValue> GetKeysCollection() {
        return InternalList.GetKeysCollection();
    }

    [MustDisposeResource] public override DreamValue GetElement(DreamValue key) {
        return InternalList.GetElement(key);
    }

    public override bool TryGetElementByIndex(int index, [MustDisposeResource][NotNullWhen(true)] out DreamValue? element) {
        return InternalList.TryGetElementByIndex(index, out element);
    }

    [MustDisposeResource] public override DreamValue MustGetElementByIndex(int index) {
        return InternalList.MustGetElementByIndex(index);
    }

    public override bool TryGetElementByKey(DreamValue key, [MustDisposeResource] out DreamValue element) {
        return InternalList.TryGetElementByKey(key, out element);
    }

    public override bool ContainsKey(DreamValue element) {
        return InternalList.ContainsKey(element);
    }

    public override int FindIndex(DreamValue element, int start = 1, int end = 0) {
        return InternalList.FindIndex(element, start, end);
    }

    public override bool ElementHasAssociation(DreamValue key) {
        return InternalList.ElementHasAssociation(key);
    }

    public override int GetLength() {
        return InternalList.GetLength();
    }

    public override DreamValue[] ToArray() {
        return InternalList.ToArray();
    }

    [Obsolete("Deprecated. Use Enumerate() or ToArray() instead.")]
    public override List<DreamValue> CopyKeys() {
        return InternalList.CopyKeys();
    }

    public override IEnumerable<DreamValue> Enumerate() {
        return InternalList.Enumerate();
    }

    public override Dictionary<DreamValue, DreamValue> GetTightPairs() {
        return InternalList.GetTightPairs();
    }

    public override Dictionary<DreamValue, DreamValue> GetLoosePairs() {
        return InternalList.GetLoosePairs();
    }

    public override IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumerateLoosePairs() {
        return InternalList.EnumerateLoosePairs();
    }

    public override IEnumerable<KeyValuePair<DreamValue, DreamValue>> EnumerateTightPairs() {
        return InternalList.EnumerateTightPairs();
    }

    public override void Set(DreamValue key, DreamValue value, bool allowGrowth = false) {
        InternalList.Set(key, value, allowGrowth);
    }

    public override void SetElementByIndex(DreamValue index, DreamValue element, bool allowGrowth = false) {
        InternalList.SetElementByIndex(index, element, allowGrowth);
    }

    public override void Add(DreamValue newElement) {
        InternalList.Add(newElement);
    }

    public override void Remove(DreamValue element) {
        InternalList.Remove(element);
    }

    public override void Cut(int start = 1, int end = 0) {
        InternalList.Cut(start, end);
    }

    public override void Insert(int index, DreamValue value) {
        InternalList.Insert(index, value);
    }

    public override void Swap(int index1, int index2) {
        InternalList.Swap(index1, index2);
    }

    public override DantomList ToDantomList(int start = 1, int end = 0) {
        return InternalList.ToDantomList(start, end);
    }

}
