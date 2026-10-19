using System.Linq;
using Robust.Server.GameStates;
namespace OpenDreamRuntime.Objects.Types;

// atom or image vis_contents list
// Operates on an object's appearance
public sealed class DreamVisContentsList : DreamListAbstract {
    private readonly PvsOverrideSystem? _pvsOverrideSystem;

    private readonly List<DreamObjectAtom> _visContents = new();
    private readonly DreamObject _owner; // atom or image

    public DreamVisContentsList(DreamObjectDefinition listDef, PvsOverrideSystem? pvsOverrideSystem, DreamObject atom) : base(listDef) {
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
