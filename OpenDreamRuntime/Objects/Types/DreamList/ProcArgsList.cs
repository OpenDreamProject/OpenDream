using System.Linq;

namespace OpenDreamRuntime.Objects.Types;

// proc args list
internal sealed class ProcArgsList(DreamObjectDefinition listDef, ProcState state) : DreamList(listDef) {
    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var index))
            throw new DMException($"Invalid index into args list: {key}");
        if (index < 1 || index > state.ArgumentCount)
            throw new DMException($"Out of bounds index on args list: {index}");

        var value = state.GetArguments()[index - 1];
        value.IncRef();
        return value;
    }

    public override List<DreamValue> CopyKeys() {
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
public sealed class SavefileDirList(DreamObjectDefinition listDef, DreamObjectSavefile backedSaveFile) : DantomList(listDef, 0) {
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

    public override List<DreamValue> CopyKeys() {
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
