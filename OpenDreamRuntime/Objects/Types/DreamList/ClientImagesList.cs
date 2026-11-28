using OpenDreamRuntime.Rendering;

namespace OpenDreamRuntime.Objects.Types;

// client.images list
public sealed class ClientImagesList(DreamObjectTree objectTree, ServerClientImagesSystem? clientImagesSystem, DreamConnection connection) : DreamEagerList(objectTree.List.ObjectDefinition, 0) {
    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var imageIndex) || imageIndex < 1 || imageIndex > Elements.Count)
            throw new DMException($"Invalid index into client images list: {key}");

        var value = Elements[imageIndex - 1];
        value.IncRef();
        return value;
    }

    public override void Set(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException("Cannot write to an index of a client images list");
    }

    public override void Add(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectImage>(out var image))
            return;

        clientImagesSystem?.AddImageObject(connection, image);
        Elements.Add(value);
    }

    public override void Remove(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectImage>(out var image))
            return;

        clientImagesSystem?.RemoveImageObject(connection, image);
        Elements.Remove(value);
    }

    public override void Cut(int start = 1, int end = 0) {
        if (end == 0 || end > Elements.Count + 1) end = Elements.Count + 1;

        for (int i = start - 1; i < end - 1; i++) {
            if (!Elements[i].TryGetValueAsDreamObject<DreamObjectImage>(out var image))
                continue;

            clientImagesSystem?.RemoveImageObject(connection, image);
        }

        Elements.RemoveRange(start - 1, end - start);
    }
}
