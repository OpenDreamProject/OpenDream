using System.Linq;
namespace OpenDreamRuntime.Objects.Types;


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
