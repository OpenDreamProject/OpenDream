using OpenDreamRuntime.Rendering;
namespace OpenDreamRuntime.Objects.Types;
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
