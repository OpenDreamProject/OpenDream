using OpenDreamRuntime.Rendering;

namespace OpenDreamRuntime.Objects.Types;

// client.screen list
public sealed class ClientScreenList(DreamObjectTree objectTree, ServerScreenOverlaySystem? screenOverlaySystem, DreamConnection connection)
    : DreamEagerList(objectTree.List.ObjectDefinition, 0) {
    protected override void HandleDeletion() {
        Cut();
        base.HandleDeletion();
    }

    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var screenIndex) || screenIndex < 1 || screenIndex > Elements.Count)
            throw new DMException($"Invalid index into screen list: {key}");

        var value = Elements[screenIndex - 1];
        value.IncRef();
        return value;
    }

    public override void Set(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException("Cannot write to an index of a screen list");
    }

    public override void Add(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable))
            return;

        screenOverlaySystem?.AddScreenObject(connection, movable);
        Elements.Add(value);
        value.IncRef();
    }

    public override void Remove(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable))
            return;

        screenOverlaySystem?.RemoveScreenObject(connection, movable);
        if (Elements.Remove(value))
            value.DecRef();
    }

    public override void Cut(int start = 1, int end = 0) {
        if (end == 0 || end > Elements.Count + 1) end = Elements.Count + 1;

        for (int i = start - 1; i < end - 1; i++) {
            using var value = Elements[i];
            if (!value.TryGetValueAsDreamObject<DreamObjectMovable>(out var movable))
                continue;

            screenOverlaySystem?.RemoveScreenObject(connection, movable);
        }

        Elements.RemoveRange(start - 1, end - start);
    }
}
