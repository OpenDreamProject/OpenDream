using System.Linq;
namespace OpenDreamRuntime.Objects.Types;

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
