// client.verbs list
using System.Linq;
namespace OpenDreamRuntime.Objects.Types;

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
